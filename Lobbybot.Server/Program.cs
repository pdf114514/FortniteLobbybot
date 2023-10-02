using Microsoft.AspNetCore.ResponseCompression;

namespace Lobbybot;

public static class ServerProgram {
    public static WebApplication CreateApp(string[]? args = null) {
        var builder = WebApplication.CreateBuilder(args ?? Array.Empty<string>());

        // Add services to the container.

        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment()) {
            app.UseWebAssemblyDebugging();
        } else {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.UseRouting();


        app.MapRazorPages();
        app.MapControllers();
        app.MapFallbackToFile("index.html");

        return app;
    }

    public static void Main(string[] args) => CreateApp(args).Run();
}
