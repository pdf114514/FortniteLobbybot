using System.Net;
using System.Text;
using System.Text.Json;

namespace FortniteCS;

public partial class FortniteClient {
    public SemaphoreSlim PartyLock { get; } = new(1, 1);
    public SemaphoreSlim PartyChatLock { get; } = new(1, 1);

    public async Task UpdateFriends() {
        _Friends.Clear();
        _PendingFriends.Clear();

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://friends-public-service-prod.ol.epicgames.com/friends/api/v1/{User.AccountId}/summary");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK) throw new Exception("Failed to get friends!");
        var friendsSummary = JsonSerializer.Deserialize<FriendsSummaryData>(await response.Content.ReadAsStringAsync()) ?? throw new Exception("Failed to deserialize json!");
        foreach (var friend in friendsSummary.Friends) _Friends.Add(new(friend));
        foreach (var incoming in friendsSummary.Incoming) _PendingFriends.Add(new IncomingPendingFriend(incoming));
        foreach (var outgoing in friendsSummary.Outgoing) _PendingFriends.Add(new OutgoingPendingFriend(outgoing));
    }

    private async Task<T1?> GetAccountByAccountId<T1, T2>(string accountId) where T1 : FortniteUser where T2 : FortniteUserData {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://account-public-service-prod.ol.epicgames.com/account/api/public/account/{accountId}");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        var json = await response.Content.ReadAsStringAsync();
        return (T1?)Activator.CreateInstance(typeof(T1), JsonSerializer.Deserialize<T2>(json));
    }

    public Task<FortniteUser?> GetUserByAccountId(string accountId) => GetAccountByAccountId<FortniteUser, FortniteUserData>(accountId);

    public async Task<FortniteUser?> GetUserByDisplayName(string displayName) {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://account-public-service-prod.ol.epicgames.com/account/api/public/account/displayName/{displayName}");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        var json = await response.Content.ReadAsStringAsync();
        return new(JsonSerializer.Deserialize<FortniteUserData>(json) ?? throw new Exception("Failed to deserialize json!"));
    }

    public async Task<FortniteClientParty?> GetClientParty() {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://party-service-prod.ol.epicgames.com/party/api/v1/Fortnite/user/{User.AccountId}");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK) {
            Logging.Warn($"Failed to get client party! {response.StatusCode}");
            return null;
        }
        var json = await response.Content.ReadAsStringAsync();
        Logging.Info(json);
        return JsonSerializer.Deserialize<Dictionary<string, List<JsonElement>>>(json)?.GetValueOrDefault("current") is var current && current?.Count > 0 ? new(this, current.FirstOrDefault().Deserialize<FortnitePartyData>() ?? throw new Exception("Failed to deserialize json!")) : null;
    }

    public async Task InitializeParty(bool createNew = true, bool forceNew = true) {
        Party = await GetClientParty();
        if (!forceNew && Party is not null) return;
        if (createNew) {
            await LeaveParty(false);
            await CreateParty();
        }
    }

    public async Task LeaveParty(bool createNew = true) {
        if (Party is null) return;

        await PartyLock.WaitAsync();
        XMPP.LeaveMUC();

        var request = new HttpRequestMessage(HttpMethod.Delete, $"https://party-service-prod.ol.epicgames.com/party/api/v1/Fortnite/parties/{Party.PartyId}/members/{User.AccountId}");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode) Logging.Warn($"Failed to leave party! {response.StatusCode}");
        Party = null;
        PartyLock.Release();
        if (createNew) await CreateParty();
    }

    public async Task CreateParty(PartyOptions? options = null) {
        if (Party is not null) await LeaveParty(false);
        options ??= new();
        FortniteParty party;

        await PartyLock.WaitAsync();
        try {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://party-service-prod.ol.epicgames.com/party/api/v1/Fortnite/parties");
            request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
            request.Content = new StringContent(JsonSerializer.Serialize(new {
                config = new {
                    join_confirmation = options.JoinConfirmation,
                    joinability = options.Joinability,
                    max_size = options.MaxSize
                },
                join_info = new {
                    connection = new {
                        id = XMPP.Connection.Jid.FullJid,
                        meta = new MetaDict {
                            ["urn:epic:conn:platform_s"] = Config.Platform,
                            ["urn:epic:conn:type_s"] = "game",
                        },
                        yield_leadership = true // false
                    },
                    meta = new MetaDict {
                        ["urn:epic:member:dn_s"] = User.DisplayName
                    }
                },
                meta = new MetaDict {
                    ["urn:epic:cfg:party-type-id_s"] = "default",
                    ["urn:epic:cfg:build-id_s"] = "1:3:",
                    ["urn:epic:cfg:join-request-action_s"] = "Manual",
                    ["urn:epic:cfg:chat-enabled_b"] = options.ChatEnabled ? "true" : "false",
                    ["urn:epic:cfg:can-join_b"] = "true"
                }
            }), Encoding.UTF8, "application/json");
            var response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode) {
                Logging.Error($"Failed to create party! {await response.Content.ReadAsStringAsync()}");
                throw new Exception($"Failed to create party! {response.StatusCode}");
            }
            var json = await response.Content.ReadAsStringAsync();
            Logging.Debug(json);
            party = new(this, JsonSerializer.Deserialize<FortnitePartyData>(json) ?? throw new Exception("Failed to deserialize json!"));
        } finally {
            PartyLock.Release();
        }

        Party = new(this, party);
        XMPP.JoinMUC(Party);
    }
}