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
    public event Action Ready;

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
    public void OnReady() {
        Logging.Debug("Client is ready");
    }
}