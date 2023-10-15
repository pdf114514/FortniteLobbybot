using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Lobbybot.Client;

public class LobbybotAuthenticationStateProvider : AuthenticationStateProvider {
    private readonly HttpClient Http;

    public LobbybotAuthenticationStateProvider(HttpClient http) => Http = http;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync() {
        var authenticated = (await Http.GetAsync("api/auth/status")).IsSuccessStatusCode;
        if (authenticated) {
            var claims = new List<Claim> { new(ClaimTypes.Name, "Lobbybot") };
            return new(new(new ClaimsIdentity(claims, "Lobbybot")));
        }
        return new(new());
    }

    public async Task Login(string password) {
        var response = await Http.PostAsJsonAsync("api/auth/login", new Dictionary<string, string> { ["password"] = password });
        if (response.IsSuccessStatusCode) NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void NotifyAuthenticationStateChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}