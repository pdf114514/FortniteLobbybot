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

            // SendPresence(new()); // if you dont you will be shown as offline ?
            SendPresence(new() { Status = "…" + new string('\u2029', 100) + "…?" }, "dnd");
        };

        Connection.Presence += async (XmppConnection sender, XMPPPresence e) => {
            if (e is null) { Logging.Warn("XMPP presence is null!"); return; }
            if (e.From.User == Client.User.AccountId) return;
            // if (!e.From.User.Contains("Party", StringComparison.InvariantCultureIgnoreCase)) Logging.Debug($"XMPP presence: {e.From} - {(e.Attribute("type")?.Value == "unavailable" ? "OFFLINE" : e.Value)}");
            FortnitePresence presence;
            try {
                presence = JsonSerializer.Deserialize<FortnitePresence>(e.Elements().First(x => x.Name.LocalName == "status").Value)!;
            } catch {
                Logging.Warn($"XMPP presence failed to deserialize");
                return;
            }
            if (presence is null) { Logging.Warn("XMPP presence failed to deserialize!"); return; }
            presence.AccountId = e.From.User;
            var user = Client.Users.FirstOrDefault(x => x.AccountId == presence.AccountId) ?? await Client.GetUserByAccountId(presence.AccountId);
            presence.DisplayName = user?.DisplayName!;
            presence.IsOnline = e.Attribute("type")?.Value != "unavailable";
            Client.OnFriendPresence(presence);
        };

        Connection.Message += async (XmppConnection sender, XMPPMessage e) => {
            Logging.Debug($"XMPP message: {e.From} - {e.Text}");
            switch (e.Attributes().FirstOrDefault(x => x.Name == "type")?.Value) {
                case "chat": {
                    Logging.Debug($"XMPP message from {e.From.User} ({e.From.Resource}): {e.Text}");
                    return;
                }
                case "groupchat": {
                    Logging.Debug($"XMPP party message from {e.From.User} ({e.From.Resource}): {e.Text}");
                    return;
                }
                case "error": {
                    Logging.Warn($"XMPP message error from {e.From.User} ({e.From.Resource}): {e}");
                    return;
                }
            }
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
                case "com.epicgames.social.party.notification.v0.PING": {
                    var payload = JsonSerializer.Deserialize<FortnitePingData>(e.Text);
                    if (payload is null) {
                        Logging.Warn("XMPP message failed to deserialize to FortnitePingPayload");
                        return;
                    }

                    var party = (await Client.GetPartiesByPingerId(payload.PingerId)).FirstOrDefault();
                    if (party is null) {
                        Logging.Warn($"Failed to get party by pinger id {payload.PingerId}");
                        return;
                    }

                    var invite = party.Invites.FirstOrDefault(x => x.SentBy == payload.PingerId) ?? new(new() {
                        ExpiresAt = payload.Expires,
                        Meta = payload.Meta,
                        SentBy = payload.PingerId,
                        SentTo = Client.User.AccountId,
                        SentAt = payload.Sent,
                        PartyId = party.PartyId,
                        Status = "SENT",
                        UpdatedAt = payload.Sent
                    });

                    Client.OnPartyInvite(invite);
                    break;
                }
                case "com.epicgames.social.party.notification.v0.MEMBER_JOINED": {
                    var payload = JsonSerializer.Deserialize<FortnitePartyMemberJoinedData>(e.Text);
                    if (payload is null) {
                        Logging.Warn("XMPP message failed to deserialize to FortnitePartyMemberJoinedPayload");
                        return;
                    }

                    if (Client.Party is null) {
                        Logging.Warn($"Joined party {payload.PartyId} but client party is NULL");
                        return;
                    }
                    if (Client.Party.PartyId != payload.PartyId) {
                        Logging.Warn($"Joined party {payload.PartyId} but client party is {Client.Party.PartyId}");
                        return;
                    }

                    var member = new FortniteClientPartyMember(Client.Party, payload);
                    if (payload.AccountId == Client.User.AccountId) {
                        Logging.Debug($"Joined party {payload.PartyId}");
                        JoinMUC(payload.PartyId);
                        SendPresence(new());
                        // todo implement meta things
                        member.Meta.Outfit = Client.Config.DefaultOutfit;
                        member.Meta.Backpack = Client.Config.DefaultOutfit;
                        member.Meta.Pickaxe = Client.Config.DefaultPickaxe;

                        member.SendPatch(member.Meta);
                    }

                    if (Client.Party.Members.ContainsKey(payload.AccountId)) {
                        Logging.Warn($"Joined party {payload.PartyId} but member {payload.AccountId} already exists");
                        return;
                    }

                    // await Client.WaitForEvent<FortnitePartyMember>(FortniteClientEvent.PartyMemberUpdated, x => {Console.WriteLine($"CONDITION {x.AccountId} == {payload.AccountId} => {x.AccountId == payload.AccountId}"); return x.AccountId == payload.AccountId;}, TimeSpan.FromSeconds(2));
                    await Client.WaitForEvent<FortnitePartyMember>(FortniteClientEvent.PartyMemberUpdated, x => true, TimeSpan.FromSeconds(2));

                    Client.OnPartyMemberJoined(member);
                    break;
                }
                case "com.epicgames.social.party.notification.v0.MEMBER_STATE_UPDATED": {
                    var payload = JsonSerializer.Deserialize<FortnitePartyMemberStateUpdatedData>(e.Text);
                    if (payload is null) {
                        Logging.Warn("XMPP message failed to deserialize to FortnitePartyMemberStateUpdatedPayload");
                        return;
                    }

                    if (Client.Party is null) {
                        Logging.Warn($"party is {payload.PartyId} but client party is NULL");
                        return;
                    }

                    if (Client.Party.PartyId != payload.PartyId) {
                        Logging.Warn($"party is {payload.PartyId} but client party is {Client.Party.PartyId}");
                        return;
                    }

                    if (!Client.Party.Members.ContainsKey(payload.AccountId)) {
                        Logging.Warn($"party is {payload.PartyId} but member {payload.AccountId} does not exist");
                        return;
                    }

                    Client.OnPartyMemberUpdated(Client.Party.Members[payload.AccountId]);
                    break;
                }
                case "com.epicgames.social.party.notification.v0.MEMBER_LEFT": {
                    var payload = JsonSerializer.Deserialize<FortnitePartyMemberLeftData>(e.Text);
                    if (payload is null) {
                        Logging.Warn("XMPP message failed to deserialize to FortnitePartyMemberLeftPayload");
                        return;
                    }

                    if (Client.Party is null) {
                        Logging.Warn($"Left party {payload.PartyId} but client party is NULL");
                        return;
                    }

                    if (payload.AccountId == Client.User.AccountId) {
                        Logging.Debug($"Left party {payload.PartyId}");
                        LeaveMUC(payload.PartyId);
                    }

                    if (Client.Party.PartyId != payload.PartyId) {
                        Logging.Warn($"Left party {payload.PartyId} but client party is {Client.Party.PartyId}");
                        return;
                    }

                    if (!Client.Party.Members.ContainsKey(payload.AccountId)) {
                        Logging.Warn($"Left party {payload.PartyId} but member {payload.AccountId} does not exist");
                        return;
                    }

                    Client.OnPartyMemberLeft(Client.Party.Members[payload.AccountId]);
                    break;
                }
                case "com.epicgames.social.party.notification.v0.MEMBER_EXPIRED": {
                    var payload = JsonSerializer.Deserialize<FortnitePartyMemberExpiredData>(e.Text);
                    if (payload is null) {
                        Logging.Warn("XMPP message failed to deserialize to FortnitePartyMemberExpiredPayload");
                        return;
                    }

                    if (Client.Party?.Members.GetValueOrDefault(payload.AccountId) is var member && member is null) {
                        Logging.Warn($"Party member ${payload.AccountId} does not exist!");
                        return;
                    }

                    Client.OnPartyMemberExpired(member);
                    break;
                }
                case "com.epicgames.social.party.notification.v0.MEMBER_KICKED": {
                    var payload = JsonSerializer.Deserialize<FortnitePartyMemberKickedData>(e.Text);
                    if (payload is null) {
                        Logging.Warn("XMPP message failed to deserialize to FortnitePartyMemberKickedPayload");
                        return;
                    }

                    if (Client.Party?.Members.GetValueOrDefault(payload.AccountId) is var member && member is null) {
                        Logging.Warn($"Party member ${payload.AccountId} does not exist!");
                        return;
                    }

                    if (member.AccountId == Client.User.AccountId) {
                        Logging.Debug($"Kicked from party {payload.PartyId}");
                        LeaveMUC(payload.PartyId);
                        await Client.InitializeParty(true, false);
                    }

                    Client.OnPartyMemberKicked(member);
                    break;
                }
                case "com.epicgames.social.party.notification.v0.MEMBER_CONNECTED": break;
                case "com.epicgames.social.party.notification.v0.MEMBER_DISCONNECTED": break;
                case "com.epicgames.social.party.notification.v0.MEMBER_NEW_CAPTAIN": break;
                case "com.epicgames.social.party.notification.v0.MEMBER_REQUIRE_CONFIRMATION": {
                    var payload = JsonSerializer.Deserialize<FortnitePartyMemberRequireConfirmationData>(e.Text);
                    if (payload is null) {
                        Logging.Warn("XMPP message failed to deserialize to FortnitePartyMemberRequireConfirmationPayload");
                        return;
                    }

                    if (Client.Party is null) {
                        Logging.Warn($"Party {payload.PartyId} requires confirmation but client party is NULL");
                        return;
                    }
                    if (Client.Party.PartyId != payload.PartyId) {
                        Logging.Warn($"Party {payload.PartyId} requires confirmation but client party is {Client.Party.PartyId}");
                        return;
                    }

                    Client.OnPartyJoinConfirmation(new(payload));
                    break;
                }
                case "com.epicgames.social.party.notification.v0.PARTY_UPDATED": break;
                case "com.epicgames.social.party.notification.v0.INITIAL_INTENTION": {
                    var payload = JsonSerializer.Deserialize<FortniteInitialIntentionData>(e.Text);
                    if (payload is null) {
                        Logging.Warn("XMPP message failed to deserialize to FortniteInitialIntentionPayload");
                        return;
                    }

                    Client.OnPartyJoinRequest(new(payload));
                    break;
                }
                case "com.epicgames.social.party.notification.v0.INTENTION_EXPIRED": {
                    var payload = JsonSerializer.Deserialize<FortniteIntentionExpiredData>(e.Text);
                    if (payload is null) {
                        Logging.Warn("XMPP message failed to deserialize to FortniteIntentionExpiredPayload");
                        return;
                    }

                    Client.OnPartyJoinRequestExpired(new(payload));
                    break;
                }
                case "com.epicgames.social.party.notification.v0.INITIAL_INVITE": {
                    var payload = JsonSerializer.Deserialize<FortniteInitialInviteData>(e.Text);
                    if (payload is null) {
                        Logging.Warn("XMPP message failed to deserialize to FortniteInitialInvitePayload");
                        return;
                    }

                    Logging.Debug($"Initial invite from {payload.InviterDn}");
                    break;
                }
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

    public void JoinMUC(FortniteParty party) => JoinMUC(party.PartyId);
    public void JoinMUC(string partyId) { // TODO support password
        Logging.Debug($"Joining MUC {partyId}");
        var presence = new XMPPPresence(Connection.Capabilities) { To = new($"Party-{partyId}@muc.prod.ol.epicgames.com/{Client.User.DisplayName}:{Client.User.AccountId}:{Connection.Jid.Resource}") };
        var xElement = new XElement(XNamespace.Get("http://jabber.org/protocol/muc") + "x");
        // if (password thing is present) xElement.Add(new XElement(XNamespace.Get("http://jabber.org/protocol/muc") + "password", password));

        presence.Add(xElement);
        Connection.Send(presence);
    }

    public void LeaveMUC(FortniteClientParty party) => LeaveMUC(party.PartyId);
    public void LeaveMUC(string partyId) {
        Logging.Debug($"Leaving MUC {partyId}");
        var presence = new XMPPPresence(Connection.Capabilities) { To = new($"Party-{partyId}@muc.prod.ol.epicgames.com/{Client.User.DisplayName}:{Client.User.AccountId}:{Connection.Jid.Resource}") };
        presence.SetAttributeValue("type", "unavailable");
        presence.Add(new XElement(XNamespace.Get("http://jabber.org/protocol/muc") + "x"));
        Connection.Send(presence);
    }

    public void SendPresence(FortnitePresence fortnitePresence, string? show = null, string? type = null) {
        var presence = new XMPPPresence();
        if (show is not null) presence.Add(new XElement(XNamespace.Get("jabber:client") + "show", show));
        if (type is not null) presence.Add(new XElement(XNamespace.Get("jabber:client") + "type", type)); // "available" or "unavailable" ?
        presence.Add(new XElement(XNamespace.Get("jabber:client") + "status", JsonSerializer.Serialize(fortnitePresence)));
        presence.Add(new XElement(XNamespace.Get("urn:xmpp:delay") + "delay", new XAttribute("stamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))));
        Connection.Send(presence);
    }

    public void SendMessage(string partyId, string messageString) {
        var message = new XMPPMessage() {
            To = new($"Party-{partyId}@muc.prod.ol.epicgames.com"),
            Text = messageString
        };
        message.SetAttributeValue("id", Guid.NewGuid().ToString("N"));
        message.SetAttributeValue("type", "groupchat");
        Connection.Send(message);
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

#endregion

#region Data

public abstract class FortniteXMPPDataBase {
    [K("sent")] public required string Sent { get; init; }
    [K("type")] public required string Type { get; init; }
    [K("ns")] public required string Ns { get; init; } // Fortnite
}

public class FortnitePartyMemberJoinedData : FortniteXMPPDataBase {
    [K("revision")] public required int Revision { get; init; }
    [K("connection")] public required FortnitePartyMemberConnectionData Connection { get; init; }
    [K("party_id")] public required string PartyId { get; init; }
    [K("account_id")] public required string AccountId { get; init; }
    [K("account_dn")] public required string AccountDn { get; init; }
    [K("member_state_updated")] public required MetaDict MemberStateUpdated { get; init; }
    [K("joined_at")] public required string JoinedAt { get; init; }
    [K("updated_at")] public required string UpdatedAt { get; init; }
}

public class FortnitePartyMemberStateUpdatedData : FortniteXMPPDataBase {
    [K("revision")] public required int Revision { get; init; }
    [K("party_id")] public required string PartyId { get; init; }
    [K("account_id")] public required string AccountId { get; init; }
    [K("account_dn")] public required string AccountDn { get; init; }
    [K("member_state_removed")] public required List<string> MemberStateRemoved { get; init; }
    [K("member_state_updated")] public required MetaDict MemberStateUpdated { get; init; }
    [K("member_state_overridden")] public required MetaDict MemberStateOverridden { get; init; }
    [K("joined_at")] public required string JoinedAt { get; init; }
    [K("updated_at")] public required string UpdatedAt { get; init; }
}

public class FortnitePartyMemberLeftData : FortniteXMPPDataBase {
    [K("revision")] public required int Revision { get; init; }
    [K("party_id")] public required string PartyId { get; init; }
    [K("account_id")] public required string AccountId { get; init; }
    [K("member_state_updated")] public required MetaDict MemberStateUpdated { get; init; }
}

public class FortnitePartyMemberExpiredData : FortniteXMPPDataBase {
    [K("revision")] public required int Revision { get; init; }
    [K("party_id")] public required string PartyId { get; init; }
    [K("account_id")] public required string AccountId { get; init; }
    [K("member_state_updated")] public required MetaDict MemberStateUpdated { get; init; }
}

public class FortnitePartyMemberKickedData : FortniteXMPPDataBase {
    [K("revision")] public required int Revision { get; init; }
    [K("party_id")] public required string PartyId { get; init; }
    [K("account_id")] public required string AccountId { get; init; }
    [K("member_state_updated")] public required MetaDict MemberStateUpdated { get; init; }
}

public class FortnitePartyMemberRequireConfirmationData : FortniteXMPPDataBase {
    [K("revision")] public required int Revision { get; init; }
    [K("party_id")] public required string PartyId { get; init; }
    [K("account_id")] public required string AccountId { get; init; }
    [K("account_dn")] public required string AccountDn { get; init; }
    [K("member_state_updated")] public required MetaDict MemberStateUpdated { get; init; }
    [K("connection")] public required FortnitePartyMemberConnectionData Connection { get; init; }
    [K("joined_at")] public required string JoinedAt { get; init; }
    [K("updated_at")] public required string UpdatedAt { get; init; }
}

public class FortnitePingData : FortniteXMPPDataBase {
    [K("pinger_id")] public required string PingerId { get; init; }
    [K("pinger_dn")] public required string PingerDn { get; init; }
    [K("expires")] public required string Expires { get; init; } // ExpiresAt
    [K("meta")] public required MetaDict Meta { get; init; }
}

public class FortniteIntentionData : FortniteXMPPDataBase {
    [K("party_id")] public required string PartyId { get; init; }
    [K("requester_id")] public required string RequesterId { get; init; }
    [K("requester_dn")] public required string RequesterDn { get; init; }
    [K("requestee_id")] public required string RequesteeId { get; init; }
    [K("sent_at")] public required string SentAt { get; init; }
    [K("expires_at")] public required string ExpiresAt { get; init; }
    [K("meta")] public required MetaDict Meta { get; init; } // {"urn:epic:invite:platformdata_s": "RequestToJoin"}
}

public class FortniteInitialIntentionData : FortniteIntentionData {}
public class FortniteIntentionExpiredData : FortniteIntentionData {}

public class FortniteInitialInviteData : FortniteXMPPDataBase {
    [K("meta")] public required MetaDict Meta { get; init; }
    [K("party_id")] public required string PartyId { get; init; }
    [K("inviter_id")] public required string InviterId { get; init; }
    [K("inviter_dn")] public required string InviterDn { get; init; }
    [K("invitee_id")] public required string InviteeId { get; init; }
    [K("sent_at")] public required string SentAt { get; init; }
    [K("updated_at")] public required string UpdatedAt { get; init; }
    [K("friends_ids")] public required List<string> FriendsIds { get; init; }
    [K("members_count")] public required int MembersCount { get; init; }
}

#endregion

public class FortnitePresence {
    [K("Status")] public string Status { get; set; } = "Playing Battle Royale";
    [K("bIsPlaying")] public bool IsPlaying { get; set; } = true;
    [K("bIsJoinable")] public bool IsJoinable { get; set; } = false;
    [K("bHasVoiceSupport")] public bool HasVoiceSupport { get; set; } = true;
    [K("SessionId")] public string SessionId { get; set; } = "";
    [K("ProductName")] public string? ProductName { get; set; } = "Fortnite";
    [K("Properties")] public Dictionary<string, object> Properties { get; set; } = new();
    public string AccountId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsOnline { get; set; } = true;
}