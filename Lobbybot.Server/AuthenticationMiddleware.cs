using System.Text.Json;
using Lobbybot.Shared;

namespace Lobbybot.Server;

public class LobbybotAuthenticationMiddleware {
    public static List<string> AuthenticatedSessionIds { get; } = new();
    public static LobbybotConfig Config { get; private set; } = null!;
    private readonly RequestDelegate Next;

    static LobbybotAuthenticationMiddleware() => LoadConfig();

    public LobbybotAuthenticationMiddleware(RequestDelegate next) {
        Next = next;
    }

    public static void LoadConfig() {
        var filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lobbybot", "config.json");
        if (!File.Exists(filepath)) {
            Directory.CreateDirectory(Path.GetDirectoryName(filepath)!);
            File.WriteAllText(filepath, JsonSerializer.Serialize(new LobbybotConfig()));
        }
        using var file = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Config = JsonSerializer.Deserialize<LobbybotConfig>(file) ?? new();
    }

    public static string NewSessionId() {
        var sessionId = Guid.NewGuid().ToString("N");
        AuthenticatedSessionIds.Add(sessionId);
        return sessionId;
    }

    public async Task InvokeAsync(HttpContext context) {
        if (Config.Web.PasswordEnabled) {
            if (context.Request.Path.Value != "/api/auth/login" && !context.Request.Cookies.ContainsKey("sessionId") || !AuthenticatedSessionIds.Contains(context.Request.Cookies["sessionId"]!)) {
                if (context.Request.Cookies.ContainsKey("password") && context.Request.Cookies["password"] == Config.Web.Password) {
                    var sessionId = Guid.NewGuid().ToString("N");
                    AuthenticatedSessionIds.Add(sessionId);
                    context.Response.Cookies.Append("sessionId", sessionId, new() { Expires = DateTime.Now.AddDays(7) });
                    context.Response.Cookies.Delete("password");
                } else {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }
            }
        }
        await Next(context);
    }
}