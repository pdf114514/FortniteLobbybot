using System.Text.Json;
using FortniteCS;

namespace Lobbybot;

public class Program {
    static void Main(string[] args) {
        Console.WriteLine("Hello World!");
        Console.WriteLine("Enter your authorization code:");
        var client = new FortniteClient(new AuthorizationCodeAuth(Console.ReadLine()!));
        client.Ready += () => {
            Console.WriteLine("Ready!");
            Console.WriteLine(JsonSerializer.Serialize(client.Config, new JsonSerializerOptions { WriteIndented = true }));
        };
        client.Start().Wait();
        Console.ReadLine();
    }
}
