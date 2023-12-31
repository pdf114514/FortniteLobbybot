using Microsoft.AspNetCore.Mvc;
using Lobbybot.Shared;

namespace Lobbybot.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase {
    [HttpGet]
    public LobbybotConfig Get() => Config;

    [HttpPut]
    public IActionResult Put(LobbybotConfig config) {
        SaveConfig(config);
        return NoContent();
    }
}