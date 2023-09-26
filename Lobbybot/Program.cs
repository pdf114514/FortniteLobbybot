using System.Text.Json;
using FortniteCS;

namespace Lobbybot;

public class Program {
    static void Main(string[] args) {
        AuthBase<FortniteAuthSession, FortniteAuthData> auth;
        if (File.Exists("deviceAuth.json")) {
            auth = new DeviceAuth(JsonSerializer.Deserialize<DeviceAuthObject>(File.ReadAllText("deviceAuth.json"))!);
        } else {
            Console.WriteLine("Enter your authorization code:");
            auth = new AuthorizationCodeAuth(Console.ReadLine()!);
        }
        var client = new FortniteClient(auth);
        client.Ready += async () => {
            Console.WriteLine($"Ready! {client.User.AccountId} / {client.User.DisplayName}");
            if (!File.Exists("deviceAuth.json")) {
                File.WriteAllText("deviceAuth.json", JsonSerializer.Serialize(await client.Session.CreateDeviceAuth()));
            }
        };
        client.Start().Wait();
        Console.WriteLine("Press enter to exit");
        Console.ReadLine();
    }
}
