using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Lobbybot.Client;

namespace Lobbybot.Client;

public static class ClientProgram {
    public static WebAssemblyHost CreateApp(string[]? args = null) {
        var builder = WebAssemblyHostBuilder.CreateDefault(args ?? Array.Empty<string>());
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        return builder.Build();
    }

    public static async Task Main(string[] args) => await CreateApp(args).RunAsync();
}
