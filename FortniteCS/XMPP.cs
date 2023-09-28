using SharpXMPP;
using SharpXMPP.XMPP.Client.Elements;
using System.Text.Json;
using System.Xml.Linq;

namespace FortniteCS;

public class FortniteXMPP : IDisposable {
    private FortniteClient Client { get; }
    public XmppWebSocketConnection Connection { get; }

    private List<TaskCompletionSource<bool>> _ReadyTasks { get; } = new();

    public FortniteXMPP(FortniteClient client) {
        Client = client;
        Connection = new(new($"{Client.User.AccountId}@prod.ol.epicgames.com/V2:Fortnite:{Client.Config.Platform}::{Guid.NewGuid().ToString("N").ToUpper()}"), Client.Session.AccessToken, "wss://xmpp-service-prod.ol.epicgames.com", false);

        Connection.SignedIn += (XmppConnection sender, SignedInArgs e) => {
            Logging.Debug($"XMPP signed in! {e.Jid}");
            _ReadyTasks.ForEach(x => x.SetResult(true));
            _ReadyTasks.Clear();
        };

        Connection.Presence += (XmppConnection sender, XMPPPresence e) => {
            Logging.Debug($"XMPP presence: {e.From} - {(e.Attribute("type")?.Value == "unavailable" ? "OFFLINE" : e.Value)}");
        };

        Connection.Message += async (XmppConnection sender, XMPPMessage e) => {
            Logging.Debug($"XMPP message: {e.From} - {e.Text}");
            if (e.From.User != "xmpp-admin") {
                Logging.Warn($"XMPP message from unknown user: {e.From.User}");
                return;
            }
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(e.Text);
            if (data is null) {
                Logging.Warn($"Failed to deserialize XMPP message");
                return;
            }
            if (!data.ContainsKey("type")) {
                Logging.Warn($"XMPP message missing `type`");
                return;
            }
            var type = data["type"].ToString();
            switch (type) {
                case "FRIENDSHIP_REQUEST": break;
                case "FRIENDSHIP_REMOVE": break;
                case "USER_BLOCKLIST_UPDATE": break;
                case "com.epicgames.friends.core.apiobjects.Friend": {
                    var payload = data.GetValueOrDefault("payload").Deserialize<FortniteFriendPayload>();
                    if (payload is null) {
                        Logging.Warn("XMPP message missing `payload`");
                        return;
                    }

                    if (await Client.GetUserByAccountId(payload.AccountId) is var user && user is null) {
                        Logging.Warn($"Failed to get user by account id {payload.AccountId}");
                        return;
                    }

                    if (payload.Status == EFortniteFriendStatus.Accepted) {
                        var friend = await Client.GetFriend(payload.AccountId);
                        if (friend.DisplayName is null) friend.DisplayName = user.DisplayName;
                        Client.OnFriendRequestAccepted(friend);
                    } else if (payload.Status == EFortniteFriendStatus.Pending) {
                        var pendingFriend = new PendingFriendData() {
                            AccountId = payload.AccountId,
                            Mutual = 0,
                            Created = payload.Created,
                            Favorite = payload.Favorite
                        };
                        if (payload.Direction == EFortniteFriendDirection.Inbound) {
                            Client.OnFriendRequestReceived(new(pendingFriend));
                        } else if (payload.Direction == EFortniteFriendDirection.Outbound) {
                            Client.OnFriendRequestSent(new(pendingFriend));
                        } else {
                            Logging.Warn($"Unknown friend request direction {payload.Direction}");
                        }
                    } else {
                        Logging.Warn($"Unknown friend request status {payload.Status}");
                    }
                    break;
                }
                case "com.epicgames.friends.core.apiobjects.FriendRemoval": {
                    var payload = data.GetValueOrDefault("payload").Deserialize<FortniteFriendRemovalPayload>();
                    if (payload is null) {
                        Logging.Warn("XMPP message missing `payload`");
                        return;
                    }

                    if (Client.Friends.Any(x => x.AccountId == payload.AccountId)) {
                        Logging.Warn($"Failed to get user by account id {payload.AccountId}");
                        return;
                    }

                    Client.OnFriendRemoved(Client.Friends.First(x => x.AccountId == payload.AccountId));
                    break;
                }
                case "com.epicgames.friends.core.apiobjects.BlockListEntryAdded": break;
                case "com.epicgames.friends.core.apiobjects.BlockListEntryRemoved": break;
                case "com.epicgames.social.party.notification.v0.PING": break;
                case "com.epicgames.social.party.notification.v0.MEMBER_JOINED": {
                    var payload = JsonSerializer.Deserialize<FortnitePartyMemberJoinedPayload>(e.Text);
                    if (payload is null) {
                        Logging.Warn("XMPP message failed to deserialize to FortnitePartyMemberJoinedPayload");
                        return;
                    }

                    if (payload.AccountId == Client.User.AccountId) Logging.Debug($"Joined party {payload.PartyId}");

                    if (Client.Party is null) {
                        Logging.Warn($"Joined party {payload.PartyId} but client party is NULL");
                        return;
                    }
                    if (Client.Party.PartyId != payload.PartyId) {
                        Logging.Warn($"Joined party {payload.PartyId} but client party is {Client.Party.PartyId}");
                        return;
                    }

                    var member = Client.Party.Members.FirstOrDefault(x => x.AccountId == payload.AccountId, new(Client.Party, new() {
                        AccountId = payload.AccountId,
                        Connections = [payload.Connection],
                        Revision = payload.Revision,
                        Meta = payload.MemberStateUpdated,
                        JoinedAt = payload.JoinedAt,
                        UpdatedAt = payload.UpdatedAt,
                        Role = "MEMBER" // ?
                    }));

                    Client.OnPartyMemberJoined(member);
                    break;
                }
                case "com.epicgames.social.party.notification.v0.MEMBER_STATE_UPDATED": break;
                case "com.epicgames.social.party.notification.v0.MEMBER_LEFT": break;
                case "com.epicgames.social.party.notification.v0.MEMBER_EXPIRED": break;
                case "com.epicgames.social.party.notification.v0.MEMBER_KICKED": break;
                case "com.epicgames.social.party.notification.v0.MEMBER_CONNECTED": break;
                case "com.epicgames.social.party.notification.v0.MEMBER_DISCONNECTED": break;
                case "com.epicgames.social.party.notification.v0.MEMBER_NEW_CAPTAIN": break;
                case "com.epicgames.social.party.notification.v0.MEMBER_REQUIRE_CONFIRMATION": break;
                case "com.epicgames.social.party.notification.v0.PARTY_UPDATED": break;
                case "com.epicgames.social.party.notification.v0.INITIAL_INTENTION": break;
                case "com.epicgames.social.party.notification.v0.INVITE_DECLINED": break;
                default: Logging.Warn($"Unknown XMPP message type: {type}"); break;
            }
        };

        Connection.Element += (XmppConnection sender, ElementArgs e) => {
            // Logging.Debug($"XMPP element {(e.IsInput ? "received" : "sent")}:\n{e.Stanza}");
        };
    }

    public Task Connect() => Connection.ConnectAsync();

    public Task WaitForReady() {
        var tcs = new TaskCompletionSource<bool>();
        _ReadyTasks.Add(tcs);
        return tcs.Task;
    }

    public void JoinMUC(FortniteParty party) { // TODO support password
        var presence = new XMPPPresence(Connection.Capabilities) { To = new($"Party-{party.PartyId}@muc.prod.ol.epicgames.com/{Client.User.DisplayName}:{Client.User.AccountId}:{Connection.Jid.Resource}") };
        var xElement = new XElement(XNamespace.Get("http://jabber.org/protocol/muc") + "x");
        // if (password thing is present) xElement.Add(new XElement(XNamespace.Get("http://jabber.org/protocol/muc") + "password", password));

        presence.Add(xElement);
        Connection.Send(presence);
    }

    public void LeaveMUC(FortniteParty party) {
        var presence = new XMPPPresence(Connection.Capabilities) { To = new($"Party-{party.PartyId}@muc.prod.ol.epicgames.com/{Client.User.DisplayName}:{Client.User.AccountId}:{Connection.Jid.Resource}") };
        presence.SetAttributeValue("type", "unavailable");
        presence.Add(new XElement(XNamespace.Get("http://jabber.org/protocol/muc") + "x"));
        Connection.Send(presence);
    }

    public void Dispose() {
        Connection.Dispose();
    }
}

#region Enums

public static class EFortniteFriendStatus {
    public const string Accepted = "ACCEPTED";
    public const string Pending = "PENDING";
}

public static class EFortniteFriendDirection {
    public const string Inbound = "INBOUND";
    public const string Outbound = "OUTBOUND";
}

public static class EFortniteFriendRemovalReason {
    public const string Deleted = "DELETED";
}

#endregion

#region Payloads

public class FortniteFriendPayload {
    [K("accountId")] public required string AccountId { get; init; }
    [K("status")] public required string Status { get; init; }
    [K("direction")] public required string Direction { get; init; }
    [K("created")] public required string Created { get; init; }
    [K("favorite")] public required bool Favorite { get; init; }
}

public class FortniteFriendRemovalPayload {
    [K("accountId")] public required string AccountId { get; init; }
    [K("reason")] public required string Reason { get; init; }
}

public class FortnitePartyMemberJoinedPayload {
    [K("sent")] public required string Sent { get; init; }
    [K("type")] public required string Type { get; init; }
    [K("connection")] public required FortnitePartyMemberConnectionData Connection { get; init; }
    [K("revision")] public required int Revision { get; init; }
    [K("ns")] public required string Ns { get; init; } // Fortnite
    [K("party_id")] public required string PartyId { get; init; }
    [K("account_id")] public required string AccountId { get; init; }
    [K("account_dn")] public required string AccountDn { get; init; }
    [K("member_state_updated")] public required MetaDict MemberStateUpdated { get; init; }
    [K("joined_at")] public required string JoinedAt { get; init; }
    [K("updated_at")] public required string UpdatedAt { get; init; }
}

#endregion

public class FortnitePresence {}