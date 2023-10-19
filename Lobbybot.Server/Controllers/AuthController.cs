using Microsoft.AspNetCore.Mvc;
using Lobbybot.Shared;

namespace Lobbybot.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase {
    [HttpPost("login")]
    public IActionResult Login([FromBody] Dictionary<string, string> request) {
        if (request.TryGetValue("password", out var password) && password == Config.Web.Password) {
            Response.Cookies.Append("sessionId", LobbybotAuthenticationMiddleware.NewSessionId(), new() { Expires = DateTime.Now.AddDays(7) });
            return Ok();
        }
        return Unauthorized();
    }

    [HttpPost("logout")]
    public IActionResult Logout() {
        if (!Request.Cookies.TryGetValue("sessionId", out var sessionId)) return Unauthorized();
        Response.Cookies.Delete("sessionId");
        return Ok();
    }

    [HttpGet("status")]
    public IActionResult Status() {
        if (!Config.Web.PasswordEnabled || Request.Cookies.TryGetValue("sessionId", out var sessionId) && LobbybotAuthenticationMiddleware.AuthenticatedSessionIds.Contains(sessionId)) return Ok();
        return Unauthorized();
    }
}