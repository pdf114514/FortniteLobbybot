using System.Text;
using System.Text.Json;
using System.Web;
using K = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace FortniteCS;

#region AuthData

public class AuthData {
    [K("access_token")] public required string AccessToken { get; set; }
    [K("account_id")] public required string AccountId { get; set; }
    [K("client_id")] public required string ClientId { get; set; }
    [K("expires_at")] public required string ExpiresAt { get; set; }
    [K("expires_in")] public required int ExpiresIn { get; set; }
    [K("token_type")] public required string TokenType { get; set; }
}

public class FortniteAuthData : AuthData {
    [K("refresh_expires")] public required int RefreshExpires { get; set; }
    [K("refresh_expires_at")] public required string RefreshExpiresAt { get; set; }
    [K("refresh_token")] public required string RefreshToken { get; set; }
    [K("internal_client")] public required bool InternalClient { get; set; }
    [K("client_service")] public required string ClientService { get; set; }
    [K("displayName")] public required string DisplayName { get; set; }
    [K("app")] public required string App { get; set; }
    [K("in_app_id")] public required string InAppId { get; set; }
    [K("device_id")] public required string DeviceId { get; set; }
}

public class FortniteClientCredentialsAuthData : AuthData {
    [K("internal_client")] public required bool InternalClient { get; set; }
    [K("client_service")] public required string ClientService { get; set; }
    [K("product_id")] public required string ProductId { get; set; }
    [K("application_id")] public required string ApplicationId { get; set; }
}

#endregion

#region Auth

public abstract class AuthBase<T1, T2> where T1 : AuthSession<T2> where T2 : AuthData {
    protected HttpClient Http = new();
    public abstract AuthClient Client { get; }
    public abstract Task<T1> Login();

    protected async Task<T1> Authenticate(Dictionary<string, string> body) {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/token");
        request.Headers.Add("Authorization", $"basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Client.ClientId}:{Client.Secret}"))}");
        if (body.Keys.Contains("token_type")) body.Add("token_type" , "eg1");
        request.Content = new FormUrlEncodedContent(body);
        var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new Exception("Failed to authenticate");
        var data = JsonSerializer.Deserialize<T2>(await response.Content.ReadAsStringAsync()) ?? throw new Exception("Failed to deserialize authentication data");
        var session = Activator.CreateInstance(typeof(T1), data, Client.Secret) as T1 ?? throw new Exception("Failed to create authentication session");
        return session;
    }
}

public class AuthorizationCodeAuth : AuthBase<AuthSession<FortniteAuthData>, FortniteAuthData> {
    public override AuthClient Client => AuthClients.FortnitePCGameClient;
    private string AuthorizationCode { get; }

    public AuthorizationCodeAuth(string authorizationCode) {
        if (authorizationCode.Contains("?code=")) AuthorizationCode = HttpUtility.ParseQueryString(authorizationCode[authorizationCode.IndexOf('?')..]).Get("code") ?? throw new Exception("Invalid authorization code");
        else if (authorizationCode.Length == 32) AuthorizationCode = authorizationCode;
        else throw new Exception("Invalid authorization code");
    }

    public override Task<AuthSession<FortniteAuthData>> Login() {
        return Authenticate(new() {
            { "grant_type", "authorization_code" },
            { "code", AuthorizationCode }
        });
    }
}

#endregion

#region AuthSession

public class AuthSession<T> where T : AuthData {
    public string AccessToken { get; protected set; }
    public DateTime ExpiresAt { get; protected set; }
    public string AccountId { get; protected set; }
    public string ClientId { get; protected set; }
    public string ClientSecret { get; init; }

    public SemaphoreSlim RefreshLock { get; } = new(1, 1);

    public AuthSession(T data, string clientSecret) {
        AccessToken = data.AccessToken;
        ExpiresAt = Utils.ConvertToDateTime(data.ExpiresAt);
        AccountId = data.AccountId;
        ClientId = data.ClientId;
        ClientSecret = clientSecret;
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}

public class FortniteAuthSession : AuthSession<FortniteAuthData>, IDisposable {
    public string App { get; private set; }
    public string ClientService { get; private set; }
    public string DisplayName { get; private set; }
    public bool IsInternalClient { get; private set; }
    public string InAppId { get; private set; }
    public string DeviceId { get; private set; }
    public string RefreshToken { get; private set; }
    public DateTime RefreshExpiresAt { get; private set; }

    private HttpClient Http = new();
    public Timer? RefreshTimer { get; private set; }

    public FortniteAuthSession(FortniteAuthData data, string clientSecret) : base(data, clientSecret) {
        App = data.App;
        ClientService = data.ClientService;
        DisplayName = data.DisplayName;
        IsInternalClient = data.InternalClient;
        InAppId = data.InAppId;
        DeviceId = data.DeviceId;
        RefreshToken = data.RefreshToken;
        RefreshExpiresAt = Utils.ConvertToDateTime(data.RefreshExpiresAt);

        SetRefreshTimer();
    }

    private void SetRefreshTimer() {
        RefreshTimer = new(new((state) => Refresh()), null, ExpiresAt - DateTime.UtcNow - TimeSpan.FromMinutes(15), Timeout.InfiniteTimeSpan);
    }

    public async Task<bool> Verify(bool forceVerify = false) {
        if (!forceVerify && !IsExpired) return false;

        var request = new HttpRequestMessage(HttpMethod.Get, "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/verify");
        request.Headers.Add("Authorization", $"bearer {AccessToken}");
        var response = await new HttpClient().SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async void Refresh() {
        await RefreshLock.WaitAsync();
        try {
            RefreshTimer?.Dispose();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/token");
            request.Headers.Add("Authorization", $"basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}"))}");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>() {
                { "grant_type", "refresh_token" },
                { "refresh_token", RefreshToken },
                { "token_type", "eg1" }
            });
            var response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode) throw new Exception("Failed to refresh authentication");
            var data = JsonSerializer.Deserialize<FortniteAuthData>(await response.Content.ReadAsStringAsync()) ?? throw new Exception("Failed to deserialize authentication data");
            AccessToken = data.AccessToken;
            ExpiresAt = Utils.ConvertToDateTime(data.ExpiresAt);
            RefreshToken = data.RefreshToken;
            RefreshExpiresAt = Utils.ConvertToDateTime(data.RefreshExpiresAt);
            SetRefreshTimer();
        } finally {
            RefreshLock.Release();
        }
    }

    public async void Dispose() {
        RefreshTimer?.Dispose();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"https://account-public-service-prod.ol.epicgames.com/account/api/oauth/sessions/kill/{AccessToken}");
        request.Headers.Add("Authorization", $"bearer {AccessToken}");
        await Http.SendAsync(request);
        Http.Dispose();
    }
}

#endregion