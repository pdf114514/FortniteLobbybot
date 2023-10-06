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
        using var client = new FortniteClient(auth);
        client.Ready += async () => {
            Console.WriteLine($"Ready! {client.User.AccountId} / {client.User.DisplayName}");
            if (!File.Exists("deviceAuth.json")) {
                File.WriteAllText("deviceAuth.json", JsonSerializer.Serialize(await client.Session.CreateDeviceAuth()));
            }
        };
        client.FriendRequestReceived += async friend => await client.AccpetFriendRequest(friend);
        client.PartyInvite += async invite => await client.AcceptInvite(invite); // doesnt work :/
        client.PartyMemberJoined += member => {
            Console.WriteLine($"{member.DisplayName} joined the party!");
            client.XMPP.SendMessage(client.Party!.PartyId, $"Hello {(member.AccountId == client.User.AccountId ? "I'm here" : member.DisplayName)}!");
        };
        client.PartyMemberLeft += member => Console.WriteLine($"{member.DisplayName} left the party!");
        client.Start().Wait();
        Console.WriteLine("Press enter to exit");
        Console.ReadLine();
    }
}
