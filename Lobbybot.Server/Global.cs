global using static Lobbybot.Server.Global;
using Lobbybot.Shared;
using System.Text.Json;

namespace Lobbybot.Server;

public static class Global {
    private static readonly string ConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lobbybot", "config.json");
    public static LobbybotConfig Config { get; private set; } = null!;

    static Global() {
        LoadConfig();
        Console.WriteLine($"Version: {System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).ProductVersion}");
    }
    
    public static void LoadConfig() {
        if (!File.Exists(ConfigPath)) {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(new LobbybotConfig(), new JsonSerializerOptions() { WriteIndented = true }));
        }
        Console.WriteLine($"Loading config from {ConfigPath}");
        using var file = File.Open(ConfigPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Config = JsonSerializer.Deserialize<LobbybotConfig>(file) ?? new();
    }

    public static void SaveConfig(LobbybotConfig config) {
        Config = config;
        Console.WriteLine($"Saving config to {ConfigPath}");
        using var file = File.Open(ConfigPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(file, Config, new JsonSerializerOptions() { WriteIndented = true });
    }
}