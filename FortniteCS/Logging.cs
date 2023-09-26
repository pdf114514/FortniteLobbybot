using System.Text.Json;

namespace FortniteCS;

internal static class Logging {
    public static void InfoSerialized(object obj) => Info(JsonSerializer.Serialize(obj, new JsonSerializerOptions() { WriteIndented = true }));
    public static void Info(string message) {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}:INF]{message}");
        Console.ResetColor();
    }

    public static void WarnSerialized(object obj) => Warn(JsonSerializer.Serialize(obj, new JsonSerializerOptions() { WriteIndented = true }));
    public static void Warn(string message) {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}:WRN]{message}");
        Console.ResetColor();
    }

    public static void ErrorSerialized(object obj) => Error(JsonSerializer.Serialize(obj, new JsonSerializerOptions() { WriteIndented = true }));
    public static void Error(string message) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}:ERR]{message}");
        Console.ResetColor();
    }

    public static void DebugSerialized(object obj) => Debug(JsonSerializer.Serialize(obj, new JsonSerializerOptions() { WriteIndented = true }));
    public static void Debug(string message) {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}:DBG]{message}");
        Console.ResetColor();
    }
}