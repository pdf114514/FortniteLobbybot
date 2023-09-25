using System.Reflection;

namespace FortniteCS;

public enum FortniteClientEvents {
    Ready
}

public class FortniteClientEventAttribute : Attribute {
    public FortniteClientEvents Event { get; }

    public FortniteClientEventAttribute(FortniteClientEvents @event) {
        Event = @event;
    }
}

public partial class FortniteClient {
    public event EventHandler Ready;

    private void RegisterEvents() {
        Logging.Debug("Registering events");
        var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var method in methods) {
            if (method.GetCustomAttribute<FortniteClientEventAttribute>() is var attribute && attribute is not null) {
                var @eventName = attribute.Event;
                var @delegate = Delegate.CreateDelegate(typeof(EventHandler), this, method);
                if (@delegate is not null) {
                    if (GetType().GetEvent(@eventName.ToString()) is var @event && @event is null) {
                        Logging.Debug($"Event {@event} does not exist!");
                        continue;
                    }
                    @event.AddEventHandler(this, @delegate);
                } else Logging.Debug($"Failed to create delegate for {@eventName}");
            }
        }
    }

    [FortniteClientEvent(FortniteClientEvents.Ready)]
    private void OnReady() {
        Logging.Debug("Client is ready");
    }
}