using System.Net.Http.Json;
using Lobbybot.Shared;

namespace Lobbybot.Client;

public class LobbybotApi {
    private readonly HttpClient Http;
    private readonly LobbybotAuthenticationStateProvider Auth;

    public LobbybotApi(HttpClient http, LobbybotAuthenticationStateProvider auth) {
        Http = http;
        Auth = auth;
    }

    public Task<LobbybotConfig?> GetConfig() => Http.GetFromJsonAsync<LobbybotConfig>("api/config");
    public Task SaveConfig(LobbybotConfig config) => Http.PutAsJsonAsync("api/config", config);
}