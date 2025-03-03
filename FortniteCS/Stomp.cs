using System.Text.Json;
using WebSocket4Net;

namespace FortniteCS;

public class EOSStomp : IAsyncDisposable {
    private bool Disposed;
    private WebSocket? Connection;
    private FortniteClient Client;
    private bool HeartBeat;
    private Timer? HeartBeatTimer;

    public bool IsConnected => Connection?.State == WebSocketState.Open;
    public bool AutoReconnect { get; set; } = true;

    public EOSStomp(FortniteClient client) {
        Client = client;
    }

    public Task Connect() {
        Connection = new WebSocket(
            "wss://connect.epicgames.dev/",
            customHeaderItems: new() {
                new("Authorization", $"bearer {Client.EOSSession.AccessToken}"),
                new("Epic-Connect-Device-Id", " "),
                new("Epic-Connect-Protocol", "stomp"),
                new("Sec-WebSocket-Protocol", "v10.stomp,v11.stomp,v12.stomp")
            }
        ) {
            EnableAutoSendPing = false
        };
        // Connection.MessageReceived += (sender, e) => Logging.Debug($"Received message: {e.Message}"); // MessageReceived can only get MESSAGE type data
        Connection.DataReceived += async (sender, e) => await ParseData(System.Text.Encoding.UTF8.GetString(e.Data));
        Connection.Opened += (sender, e) => {
            Logging.Debug("Stomp connection opened");
            Connection.Send("CONNECT\nheart-beat:30000,0\naccept-version:1.0,1.1,1.2\n\n\0");
        };
        Connection.Closed += async (sender, e) => {
            Logging.Debug("Stomp connection closed");
            HeartBeatTimer?.Dispose();
            HeartBeatTimer = null;
            HeartBeat = false;
            if (AutoReconnect && !Disposed) await Connect();
        };
        Connection.Error += (sender, e) => Logging.Error($"Stomp error: {e.Exception}");
        Logging.Debug("Stomp connecting...");
        return Connection.OpenAsync();
    }

    private void SetHeartBeatTimer(int delaySec) {
        if (HeartBeatTimer is not null) {
            HeartBeatTimer.Dispose();
            HeartBeatTimer = null;
        }
        HeartBeatTimer = new(_ => {
            if (Connection == null || Connection.State != WebSocketState.Open) {
                HeartBeatTimer?.Dispose();
                HeartBeatTimer = null;
                return;
            }
            Connection.Send("\n");
        }, null, TimeSpan.FromSeconds(delaySec), TimeSpan.FromSeconds(delaySec));
    }

    public async Task ParseData(string raw) {
        var splitIndex = raw.IndexOf("\n\n");
        var rawHeaders = raw.Substring(0, splitIndex);
        var rawJson = raw.Substring(splitIndex + 2).Trim('\0');

        var headerLines = rawHeaders.Split('\n');
        var headers = new Dictionary<string, string>();
        var messageType = headerLines[0];

        foreach (var line in headerLines.Skip(1)) {
            var keyValue = line.Split(':');
            headers[keyValue[0]] = keyValue[1];
        }
        var data = string.IsNullOrWhiteSpace(rawJson) ? new() : JsonSerializer.Deserialize<JsonElement>(rawJson)!;
        Logging.Debug($"Stomp: {messageType} / {JsonSerializer.Serialize(headers)} / {rawJson}");

        if (messageType == "CONNECTED" && !HeartBeat) {
            HeartBeat = true;
            var delay = int.Parse(headers["heart-beat"].Split(',')[1]) / 1000;
            SetHeartBeatTimer(delay);
            // Which one should be used? - SUBSCRIBE\nid:0\ndestination:launcher\n\n\0
            Connection!.Send($"SUBSCRIBE\nid:sub-0\ndestination:{FortniteUtils.EOSDeploymentId}/account/{Client.User.AccountId}\n\n\0");
        } else if (messageType == "MESSAGE") {
            if (!data.TryGetProperty("type", out var typeJsonElement)) return;
            switch (typeJsonElement.GetString()!) {
                case "core.connect.v1.connected": {
                    await Client.SendPresence(data.GetProperty("connectionId").GetString()!);
                    break;
                }
                case "social.chat.v1.NEW_WHISPER": {
                    var message = data.GetProperty("payload").GetProperty("message");
                    var senderId = message.GetProperty("senderId").GetString();
                    var friend = Client.Friends.FirstOrDefault(x => x.AccountId == senderId) ?? (senderId is null ? null : await Client.GetFriend(senderId));
                    var body = message.GetProperty("body").GetString()!;
                    if (friend is null) {
                        Logging.Warn($"Received message from unknown friend: {senderId}");
                        return;
                    }
                    Client.OnFriendMessage(new(friend, body));
                    break;
                }
                case var type: {
                    Logging.Warn($"Unhandled message type: {type}");
                    break;
                }
            }
        }
    }

    public async ValueTask DisposeAsync() {
        HeartBeatTimer?.Dispose();
        HeartBeatTimer = null;
        if (Connection is not null) {
            await Connection.CloseAsync();
            Connection.Dispose();
            Connection = null;
        }
        Disposed = true;
        GC.SuppressFinalize(this);
    }
}

public class FortniteFriendMessage(FortniteFriend friend, string message) {
    public FortniteFriend Friend { get; init; } = friend;
    public string Message { get; init; } = message;
}

public class FortnitePartyMessage(FortniteParty party, FortnitePartyMember member, string message) {
    public FortniteParty Party { get; init; } = party;
    public FortnitePartyMember Member { get; init; } = member;
    public string Message { get; init; } = message;
}