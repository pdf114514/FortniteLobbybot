global using static Lobbybot.Server.Global;
using Lobbybot.Shared;
using System.Text.Json;

namespace Lobbybot.Server;

public static class Global {
    public static LobbybotConfig Config { get; set; } = null!;

    static Global() {
        LoadConfig();
        Console.WriteLine($"Version: {System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).ProductVersion}");
    }
    
    public static void LoadConfig() {
        var filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lobbybot", "config.json");
        if (!File.Exists(filepath)) {
            Directory.CreateDirectory(Path.GetDirectoryName(filepath)!);
            File.WriteAllText(filepath, JsonSerializer.Serialize(new LobbybotConfig(), new JsonSerializerOptions() { WriteIndented = true }));
        }
        Console.WriteLine($"Loading config from {filepath}");
        using var file = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Config = JsonSerializer.Deserialize<LobbybotConfig>(file) ?? new();
    }
}