using Microsoft.JSInterop;

namespace Lobbybot.Client;

public static class IJSRuntimeExtension {
    public static void InvokeVoid(this IJSRuntime jsRuntime, string identifier, params object?[]? args) => jsRuntime.InvokeVoidAsync(identifier, args);
    public static async void CLog(this IJSRuntime jsRuntime, params object?[]? args) => await jsRuntime.InvokeVoidAsync("console.log", args);
    public static async void CError(this IJSRuntime jsRuntime, params object?[]? args) => await jsRuntime.InvokeVoidAsync("console.error", args);
    public static async void Alert(this IJSRuntime jsRuntime, params object?[]? args) => await jsRuntime.InvokeVoidAsync("alert", args);
    public static async void LSSetItem(this IJSRuntime jsRuntime, string key, string value) => await jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
    public static async ValueTask<string?> LSGetItem(this IJSRuntime jsRuntime, string key) => await jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
    public static async void LSRemoveItem(this IJSRuntime jsRuntime, string key) => await jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
}