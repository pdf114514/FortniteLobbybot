using System.Reflection;

namespace FortniteCS;

public enum FortniteClientEvents {
    Ready,

    FriendMessage,
    FriendPresence,
    FriendOnline,
    FriendOffline,
    FriendRemoved, // Fired when you removed a friend? not friend removed you
    FriendRequestReceived,
    FriendRequestSent,
    FriendRequestCancelled,
    FriendRequestAccepted,
    FriendRequestRejected,

    PartyUpdated,
    PartyMessage,
    PartyInvite,
    PartyJoinRequest,
    PartyJoinConfirmation,

    PartyMemberJoined,
    PartyMemberUpdated,
    PartyMemberLeft,
    PartyMemberExpired,
    PartyMemberKicked,
    PartyMemberDisconnected,
    PartyMemberPromoted,
    PartyMemberOutfitUpdated,
    PartyMemberEmoteUpdated,
    PartyMemberBackpackUpdated,
    PartyMemberPickaxeUpdated,
    PartyMemberReadinessUpdated,
    PartyMemberMatchStateUpdated,
}

public class FortniteClientEventAttribute : Attribute {
    public FortniteClientEvents Event { get; }

    public FortniteClientEventAttribute(FortniteClientEvents @event) {
        Event = @event;
    }
}

public partial class FortniteClient {
    public event Action? Ready;

    public event Action<FortniteFriend>? FriendRemoved;
    public event Action<IncomingPendingFriend>? FriendRequestReceived;
    public event Action<OutgoingPendingFriend>? FriendRequestSent;
    public event Action<PendingFriend>? FriendRequestCancelled;
    public event Action<FortniteFriend>? FriendRequestAccepted;
    public event Action<PendingFriend>? FriendRequestRejected;

    private void RegisterEvents() {
        Logging.Debug("Registering events");
        var t = GetType();
        var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var method in methods) {
            if (method.GetCustomAttribute<FortniteClientEventAttribute>() is var attribute && attribute is not null) {
                var @eventName = attribute.Event;
                if (t.GetEvent(@eventName.ToString()) is var @event && @event is null) {
                    Logging.Debug($"Event {@event} does not exist!");
                    continue;
                }
                var @delegate = Delegate.CreateDelegate(@event.EventHandlerType!, this, method);
                if (@delegate is null) {
                    Logging.Debug($"Could not create delegate for event {@event}");
                    continue;
                }
                @event.AddEventHandler(this, @delegate);
                Logging.Debug($"Registered event {@event}");
            }
        }
    }

    [FortniteClientEvent(FortniteClientEvents.Ready)]
    internal void OnReady() {
        Logging.Debug("Client is ready");
    }

    internal void OnFriendRemoved(FortniteFriend friend) {
        Logging.Debug($"Friend removed {friend.DisplayName}");
        _Friends.RemoveAll(x => x.AccountId == friend.AccountId);
        FriendRemoved?.Invoke(friend);
    }

    internal void OnFriendRequestReceived(IncomingPendingFriend friend) {
        Logging.Debug($"Friend request received from {friend.AccountId}");
        _PendingFriends.Add(friend);
        FriendRequestReceived?.Invoke(friend);
    }

    internal void OnFriendRequestSent(OutgoingPendingFriend friend) {
        Logging.Debug($"Friend request sent to {friend.AccountId}");
        _PendingFriends.Add(friend);
        FriendRequestSent?.Invoke(friend);
    }

    internal void OnFriendRequestCancelled(PendingFriend friend) {
        Logging.Debug($"Friend request cancelled to {friend.AccountId}");
        _PendingFriends.RemoveAll(x => x.AccountId == friend.AccountId);
        FriendRequestCancelled?.Invoke(friend);
    }

    internal void OnFriendRequestAccepted(FortniteFriend friend) {
        Logging.Debug($"Friend request accepted from {friend.DisplayName}");
        _Friends.Add(friend);
        _PendingFriends.RemoveAll(x => x.AccountId == friend.AccountId);
        FriendRequestAccepted?.Invoke(friend);
    }

    internal void OnFriendRequestRejected(PendingFriend friend) {
        Logging.Debug($"Friend request rejected from {friend.AccountId}");
        _PendingFriends.RemoveAll(x => x.AccountId == friend.AccountId);
        FriendRequestRejected?.Invoke(friend);
    }
}