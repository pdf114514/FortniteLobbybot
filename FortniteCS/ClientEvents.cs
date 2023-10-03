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

    public event Action<FortniteFriend>? FriendMessage;
    public event Action<FortniteFriend>? FriendPresence;
    public event Action<FortniteFriend>? FriendOnline;
    public event Action<FortniteFriend>? FriendOffline;
    public event Action<FortniteFriend>? FriendRemoved;
    public event Action<IncomingPendingFriend>? FriendRequestReceived;
    public event Action<OutgoingPendingFriend>? FriendRequestSent;
    public event Action<PendingFriend>? FriendRequestCancelled;
    public event Action<FortniteFriend>? FriendRequestAccepted;
    public event Action<PendingFriend>? FriendRequestRejected;

    // public event Action<FortniteParty>? PartyUpdated;
    // public event Action<FortniteParty>? PartyMessage;
    // public event Action<FortniteParty>? PartyInvite;
    public event Action<FortnitePartyJoinRequest>? PartyJoinRequest;
    public event Action<FortnitePartyJoinRequest>? PartyJoinRequestExpired;
    // public event Action<FortniteParty>? PartyJoinConfirmation;

    public event Action<FortnitePartyMember>? PartyMemberJoined;
    // public event Action<FortnitePartyMember>? PartyMemberUpdated;
    public event Action<FortnitePartyMember>? PartyMemberLeft;
    // public event Action<FortnitePartyMember>? PartyMemberExpired;
    // public event Action<FortnitePartyMember>? PartyMemberKicked;
    // public event Action<FortnitePartyMember>? PartyMemberDisconnected;
    // public event Action<FortnitePartyMember>? PartyMemberPromoted;
    // public event Action<FortnitePartyMember>? PartyMemberOutfitUpdated;
    // public event Action<FortnitePartyMember>? PartyMemberEmoteUpdated;
    // public event Action<FortnitePartyMember>? PartyMemberBackpackUpdated;
    // public event Action<FortnitePartyMember>? PartyMemberPickaxeUpdated;
    // public event Action<FortnitePartyMember>? PartyMemberReadinessUpdated;
    // public event Action<FortnitePartyMember>? PartyMemberMatchStateUpdated;

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

    // FriendMessage
    // FriendPresence
    // FriendOnline
    // FriendOffline

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

    // PartyUpdated
    // PartyMessage
    // PartyInvite

    internal void OnPartyJoinRequest(FortnitePartyJoinRequest request) {
        Logging.Debug($"Party join request from {request.AccountId}");
        _PartyJoinRequests.Add(request);
        PartyJoinRequest?.Invoke(request);
    }

    internal void OnPartyJoinRequestExpired(FortnitePartyJoinRequest request) {
        Logging.Debug($"Party join request expired from {request.AccountId}");
        _PartyJoinRequests.RemoveAll(x => x.AccountId == request.AccountId);
        PartyJoinRequestExpired?.Invoke(request);
    }

    // PartyJoinConfirmation

    internal void OnPartyMemberJoined(FortnitePartyMember member) {
        Logging.Debug($"Party member joined {member.DisplayName}");
        Party?._Members.Append(member);
        PartyMemberJoined?.Invoke(member);
    }

    // PartyMemberUpdated

    internal void OnPartyMemberLeft(FortnitePartyMember member) {
        Logging.Debug($"Party member left {member.DisplayName}");
        Party?._Members.Remove(member);
        PartyMemberLeft?.Invoke(member);
    }
}