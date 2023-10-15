using Microsoft.AspNetCore.Mvc;
using Lobbybot.Shared;

namespace Lobbybot.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase {
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] Dictionary<string, string> request) {
        if (request.TryGetValue("password", out var password) && password == LobbybotAuthenticationMiddleware.Config.Web.Password) {
            Response.Cookies.Append("sessionId", LobbybotAuthenticationMiddleware.NewSessionId(), new() { Expires = DateTime.Now.AddDays(7) });
            return Ok();
        }
        return Unauthorized();
    }
}