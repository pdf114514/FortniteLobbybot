using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Lobbybot.Client;
using Microsoft.JSInterop;

namespace Lobbybot.Client;

public static class ClientProgram {
    public static WebAssemblyHost CreateHost(string[]? args = null) {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var builder = WebAssemblyHostBuilder.CreateDefault(args ?? Array.Empty<string>());
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        builder.Services.AddScoped(sp => new Localization("en"));
        builder.Services.AddAuthorizationCore();
        builder.Services.AddScoped<LobbybotAuthenticationStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(x => x.GetRequiredService<LobbybotAuthenticationStateProvider>());

        return builder.Build();
    }

    public static async Task Main(string[] args) {
        var host = CreateHost(args);
        var js = host.Services.GetRequiredService<IJSRuntime>();
        if (await js.LSGetItem("language") is var language && language is not null) host.Services.GetRequiredService<Localization>().SwitchLanguage(language);
        await host.RunAsync();
    }
}
