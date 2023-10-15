using System.Text.Json;
using Lobbybot.Shared;

namespace Lobbybot.Server;

public class LobbybotAuthenticationMiddleware {
    public static List<string> AuthenticatedSessionIds { get; } = new();
    private readonly RequestDelegate Next;

    static LobbybotAuthenticationMiddleware() => LoadConfig();

    public LobbybotAuthenticationMiddleware(RequestDelegate next) {
        Next = next;
    }

    public static string NewSessionId() {
        var sessionId = Guid.NewGuid().ToString("N");
        AuthenticatedSessionIds.Add(sessionId);
        return sessionId;
    }

    public async Task InvokeAsync(HttpContext context) {
        if (Config.Web.PasswordEnabled && context.Request.Path.StartsWithSegments("/api")) {
            if (!context.Request.Cookies.ContainsKey("sessionId") || !AuthenticatedSessionIds.Contains(context.Request.Cookies["sessionId"]!)) {
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