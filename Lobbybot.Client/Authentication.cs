using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Lobbybot.Client;

public class LobbybotAuthenticationStateProvider : AuthenticationStateProvider {
    public override Task<AuthenticationState> GetAuthenticationStateAsync() {
        var identity = new ClaimsIdentity();
        var user = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
    }
}