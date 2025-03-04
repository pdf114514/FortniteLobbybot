using System.Reflection;

namespace FortniteCS;

public enum FortniteClientEvent {
    Ready,

    FriendMessage,
    FriendPresence,
    FriendOnline,
    FriendOffline,
    FriendRemoved,
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
    public FortniteClientEvent Event { get; }

    public FortniteClientEventAttribute(FortniteClientEvent @event) {
        Event = @event;
    }
}

public partial class FortniteClient {
    public event Action? Ready;

    public event Action<FortniteFriendMessage>? FriendMessage;
    public event Action<FortnitePresence>? FriendPresence;
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
    public event Action<FortnitePartyInvite>? PartyInvite;
    public event Action<FortnitePartyJoinRequest>? PartyJoinRequest;
    public event Action<FortnitePartyJoinRequest>? PartyJoinRequestExpired;
    public event Action<FortnitePartyJoinConfirmation>? PartyJoinConfirmation;

    public event Action<FortnitePartyMember>? PartyMemberJoined;
    public event Action<FortnitePartyMember>? PartyMemberUpdated;
    public event Action<FortnitePartyMember>? PartyMemberLeft;
    public event Action<FortnitePartyMember>? PartyMemberExpired;
    public event Action<FortnitePartyMember>? PartyMemberKicked;
    // public event Action<FortnitePartyMember>? PartyMemberDisconnected;
    // public event Action<FortnitePartyMember>? PartyMemberPromoted;
    // public event Action<FortnitePartyMember>? PartyMemberOutfitUpdated;
    public event Action<FortnitePartyMember>? PartyMemberEmoteUpdated;
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
                var eventName = attribute.Event;
                if (t.GetEvent(eventName.ToString()) is var @event && @event is null) {
                    Logging.Debug($"Event {eventName} does not exist!");
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

    internal async Task<T> WaitForEvent<T>(FortniteClientEvent eventName, Predicate<T> condition, TimeSpan? timeout = null) {
        var @event = GetType().GetEvent(eventName.ToString());
        if (@event is null) throw new Exception($"Event {eventName} does not exist!");
        var tcs = new TaskCompletionSource<T>();
        void handler(T obj) {
            if (condition(obj)) {
                @event.RemoveEventHandler(this, handler);
                tcs.SetResult(obj);
            }
        }
        @event.AddEventHandler(this, handler);
        var task = tcs.Task;
        if (timeout is not null) {
            await Task.WhenAny(task, ((Func<Task>)(async () => {
                await Task.Delay(timeout.Value);
                @event.RemoveEventHandler(this, handler);
            }))());
        }
        return await task;
    }

    [FortniteClientEvent(FortniteClientEvent.Ready)]
    internal void OnReady() {
        Logging.Debug("Client is ready");
    }

    internal void OnFriendMessage(FortniteFriendMessage message) {
        Logging.Debug($"Friend message from {message.Friend.DisplayName}");
        FriendMessage?.Invoke(message);
    }

    internal void OnFriendPresence(FortnitePresence presence) {
        // Logging.Debug($"Friend presence {presence.DisplayName}");
        FriendPresence?.Invoke(presence);
    }

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

    internal void OnPartyInvite(FortnitePartyInvite invite) {
        Logging.Debug($"Party invite from {invite.SentBy}");
        // add to invites list?
        PartyInvite?.Invoke(invite);
    }

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

    internal async void OnPartyJoinConfirmation(FortnitePartyJoinConfirmation confirmation) {
        Logging.Debug($"Party join confirmation from {confirmation.AccountId}");
        PartyJoinConfirmation?.Invoke(confirmation);
        Logging.Debug($"Party join confirmation handled: {confirmation.Handled}");
        if (!confirmation.Handled) await AcceptJoinConfirmation(confirmation);
    }

    internal void OnPartyMemberJoined(FortnitePartyMember member) {
        Logging.Debug($"Party member joined {member.DisplayName}");
        Party?._Members.Append(member);
        PartyMemberJoined?.Invoke(member);
    }

    internal void OnPartyMemberUpdated(FortnitePartyMember member) {
        Logging.Debug($"Party member updated {member.DisplayName}");
        PartyMemberUpdated?.Invoke(member);
    }

    internal void OnPartyMemberLeft(FortnitePartyMember member) {
        Logging.Debug($"Party member left {member.DisplayName}");
        Party?._Members.Remove(member);
        PartyMemberLeft?.Invoke(member);
    }

    internal void OnPartyMemberExpired(FortnitePartyMember member) {
        Logging.Debug($"Party member expired {member.DisplayName}");
        Party?._Members.Remove(member);
        PartyMemberExpired?.Invoke(member);
    }

    internal void OnPartyMemberKicked(FortnitePartyMember member) {
        Logging.Debug($"Party member kicked {member.DisplayName}");
        Party?._Members.Remove(member);
        PartyMemberKicked?.Invoke(member);
    }

    // PartyMemberDisconnected
    // PartyMemberPromoted
    // PartyMemberOutfitUpdated

    internal void OnPartyMemberEmoteUpdated(FortnitePartyMember member) {
        Logging.Debug($"Party member emote updated {member.DisplayName}");
        PartyMemberEmoteUpdated?.Invoke(member);
    }

    // PartyMemberBackpackUpdated
    // PartyMemberPickaxeUpdated
    // PartyMemberReadinessUpdated
    // PartyMemberMatchStateUpdated
}