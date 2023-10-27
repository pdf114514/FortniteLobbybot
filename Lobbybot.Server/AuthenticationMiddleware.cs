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
        if (Config.Web.PasswordEnabled && context.Request.Path.StartsWithSegments("/api") && !context.Request.Path.StartsWithSegments("/api/auth")) {
            if (!(context.Request.Cookies.TryGetValue("sessionId", out var sessionId) && AuthenticatedSessionIds.Contains(sessionId))) {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }
        }
        await Next(context);
    }
}