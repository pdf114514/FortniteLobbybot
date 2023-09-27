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
        Connection = new(new($"{Client.User.AccountId}@prod.ol.epicgames.com/V2:Fortnite:{Client.Config.Platform}::{Guid.NewGuid().ToString("N").ToUpper()}"), Client.Session.AccessToken, "wss://xmpp-service-prod.ol.epicgames.com");

        Connection.SignedIn += (XmppConnection sender, SignedInArgs e) => {
            Logging.Debug($"XMPP signed in! {e.Jid}");
            _ReadyTasks.ForEach(x => x.SetResult(true));
            _ReadyTasks.Clear();
        };

        Connection.Presence += (XmppConnection sender, XMPPPresence e) => {
            Logging.Debug($"XMPP presence: {e.Value}");
        };

        Connection.Message += async (XmppConnection sender, XMPPMessage e) => {
            Logging.Debug($"XMPP message: {e.Text}");
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
                case "FRIENDSHIP_REMOVE": {
                    var from = data.GetValueOrDefault("from").ToString();
                    var to = data.GetValueOrDefault("to").ToString();
                    var reason = data.GetValueOrDefault("reason").ToString();
                    if (new string[] { from, to, reason }.Any(string.IsNullOrEmpty)) {
                        Logging.Warn("XMPP message missing required fields");
                        return;
                    }

                    var accountId = from == Client.User.AccountId ? to : from;
                    if (reason == "ABORTED") {
                        if (Client.PendingFriends.FirstOrDefault(x => x.AccountId == accountId) is var pendingFriend && pendingFriend is null) return;
                        Client.OnFriendRequestCancelled(pendingFriend);
                    } else if (reason == "REJECTED") {
                        if (Client.PendingFriends.FirstOrDefault(x => x.AccountId == accountId) is var pendingFriend && pendingFriend is null) return;
                        Client.OnFriendRequestRejected(pendingFriend);
                    } else if (reason == "REMOVED") {
                        if (Client.Friends.FirstOrDefault(x => x.AccountId == accountId) is var friend && friend is null) return;
                        Client.OnFriendRemoved(friend);
                    } else Logging.Warn($"Unknown friend request reason {reason}");
                    break;
                }
                case "com.epicgames.friends.core.apiobjects.Friend": {
                    var payload = data.GetValueOrDefault("payload").Deserialize<Dictionary<string, JsonElement>>();
                    if (payload is null) {
                        Logging.Warn($"XMPP message missing `payload`");
                        return;
                    }
                    var status = payload.GetValueOrDefault("status").ToString();
                    var accountId = payload.GetValueOrDefault("accountId").ToString();
                    var favorite = payload.GetValueOrDefault("favorite").GetBoolean();
                    var created = payload.GetValueOrDefault("created").ToString();
                    var direction = payload.GetValueOrDefault("direction").ToString();

                    if (new string[] { status, accountId, created, direction }.Any(string.IsNullOrEmpty)) {
                        Logging.Warn("XMPP message missing required fields");
                        return;
                    }

                    if (await Client.GetUserByAccountId(accountId) is var user && user is null) {
                        Logging.Warn($"Failed to get user by account id {accountId}");
                        return;
                    }

                    if (status == "ACCEPTED") {
                        var friend = new FortniteFriend(new() {
                            AccountId = accountId,
                            Groups = new(),
                            Mutual = 0,
                            Alias = "",
                            Note = "",
                            Favorite = favorite,
                            Created = created
                        }) { DisplayName = user.DisplayName };

                        Client.OnFriendRequestAccepted(friend);
                    } else if (status == "PENDING") {
                        var pendingFriend = new PendingFriendData() {
                            AccountId = accountId,
                            Mutual = 0,
                            Created = created,
                            Favorite = favorite
                        };
                        if (direction == "INBOUND") {
                            Client.OnFriendRequestReceived(new(pendingFriend));
                        } else if (direction == "OUTBOUND") {
                            Client.OnFriendRequestSent(new(pendingFriend));
                        } else {
                            Logging.Warn($"Unknown friend request direction {direction}");
                        }
                    } else {
                        Logging.Warn($"Unknown friend request status {status}");
                    }
                    break;
                }
                case "USER_BLOCKLIST_UPDATE": break; // TODO
                case "com.epicgames.friends.core.apiobjects.FriendRemoval": break;
                case "com.epicgames.friends.core.apiobjects.BlockListEntryAdded": break;
                case "com.epicgames.friends.core.apiobjects.BlockListEntryRemoved": break;
                case "com.epicgames.social.party.notification.v0.PING": break;
                case "com.epicgames.social.party.notification.v0.MEMBER_JOINED": break;
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
        // Connection.BookmarkManager.Join(new() {
        //     JID = new($"Party-{party.PartyId}@muc.prod.ol.epicgames.com"),
        //     Nick = $"{Client.User.DisplayName}:{Client.User.AccountId}:{Connection.Jid.Resource}"
        // });
        var presence = new XMPPPresence(Connection.Capabilities) { To = new($"Party-{party.PartyId}@muc.prod.ol.epicgames.com/{Client.User.DisplayName}:{Client.User.AccountId}:{Connection.Jid.Resource}") };
        var xElement = new XElement(XNamespace.Get("http://jabber.org/protocol/muc") + "x");
        // if (password thing is present) xElement.Add(new XElement(XNamespace.Get("http://jabber.org/protocol/muc") + "password", password));

        presence.Add(xElement);
        Connection.Send(presence);
    }

    public void LeaveMUC() { // TODO implement
        Logging.Warn("TODO: LeaveMUC");
    }

    public void Dispose() {
        Connection.Dispose();
    }
}