using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace FortniteCS;

public class DeviceAuthObject {
    public class _Created {
        [K("location")] public required string Location { get; init; }
        [K("ipAddress")] public required string IpAddress { get; init; }
        [K("dateTime")] public required string DateTime { get; init; }
    }
    [K("deviceId")] public required string DeviceId { get; init; }
    [K("accountId")] public required string AccountId { get; init; }
    [K("secret")] public required string Secret { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] [K("userAgent")] public string? UserAgent { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] [K("created")] public _Created? Created { get; init; }
}

public class ExchangeCodeObject {
    [K("expiresInSeconds")] public required int ExpiresInSeconds { get; init; }
    [K("code")] public required string Code { get; init; }
    [K("creatingClientId")] public required string CreatingClientId { get; init; }
}

#region AuthData

public class AuthData {
    [K("access_token")] public required string AccessToken { get; set; }
    [K("client_id")] public required string ClientId { get; set; }
    [K("expires_at")] public required string ExpiresAt { get; set; }
    [K("expires_in")] public required int ExpiresIn { get; set; }
    [K("token_type")] public required string TokenType { get; set; }
}

public class FortniteAuthData : AuthData {
    [K("account_id")] public required string AccountId { get; set; }
    [K("refresh_expires")] public required int RefreshExpires { get; set; }
    [K("refresh_expires_at")] public required string RefreshExpiresAt { get; set; }
    [K("refresh_token")] public required string RefreshToken { get; set; }
    [K("internal_client")] public required bool InternalClient { get; set; }
    [K("client_service")] public required string ClientService { get; set; }
    [K("displayName")] public required string DisplayName { get; set; }
    [K("app")] public required string App { get; set; }
    [K("in_app_id")] public required string InAppId { get; set; }
    [K("device_id")] public string? DeviceId { get; set; } // not included?
}

public class FortniteClientCredentialsAuthData : AuthData {
    [K("internal_client")] public required bool InternalClient { get; set; }
    [K("client_service")] public required string ClientService { get; set; }
    [K("product_id")] public required string ProductId { get; set; }
    [K("application_id")] public required string ApplicationId { get; set; }
}

public class EOSAuthData : AuthData {
    [K("application_id")] public required string ApplicationId { get; set; }
    [K("refresh_token")] public required string RefreshToken { get; set; }
    [K("scope")] public required string Scope { get; set; }
    [K("merged_accounts")] public List<object>? MergedAccounts { get; set; }
    [K("acr")] public string? Acr { get; set; }
    [K("auth_time")] public string? AuthTime { get; set; }
}

#endregion

#region Auth

public abstract class AuthBase<T1, T2> where T1 : AuthSession<T2> where T2 : AuthData {
    protected HttpClient Http = new();
    public AuthClient Client { get; internal set; }

    public AuthBase(AuthClient? client = null) => Client = client ?? AuthClients.FortniteAndroidGameClient;

    public abstract Task<T1> Login();

    protected async Task<T1> Authenticate(Dictionary<string, string> body) {
        Logging.Debug($"Class {GetType().Name} authenticating with {Client.ClientId}...");
        var request = new HttpRequestMessage(HttpMethod.Post, "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/token");
        request.Headers.Add("Authorization", $"basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Client.ClientId}:{Client.Secret}"))}");
        if (!body.Keys.Contains("token_type")) body.Add("token_type" , "eg1");
        request.Content = new FormUrlEncodedContent(body);
        var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode) {
            Logging.Error($"Failed to authenticate:\n{JsonSerializer.Serialize(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { WriteIndented = true })}");
            throw new Exception("Failed to authenticate");
        }
        var data = JsonSerializer.Deserialize<T2>(await response.Content.ReadAsStringAsync()) ?? throw new Exception("Failed to deserialize authentication data");
        var session = Activator.CreateInstance(typeof(T1), data, Client.Secret) as T1 ?? throw new Exception("Failed to create authentication session");
        Logging.Debug($"Class {GetType().Name} authenticated with {Client.ClientId}");
        return session;
    }
}

// https://www.epicgames.com/id/api/redirect?clientId=3f69e56c7649492c8cc29f1af08a8a12&responseType=code
public class AuthorizationCodeAuth : AuthBase<FortniteAuthSession, FortniteAuthData> {
    private string AuthorizationCode { get; }

    public AuthorizationCodeAuth(string authorizationCode, AuthClient? client = null) : base(client) {
        if (authorizationCode.Contains("?code=")) AuthorizationCode = HttpUtility.ParseQueryString(authorizationCode[authorizationCode.IndexOf('?')..]).Get("code") ?? throw new Exception("Invalid authorization code");
        else if (authorizationCode.Length == 32) AuthorizationCode = authorizationCode;
        else throw new Exception("Invalid authorization code");
    }

    public override Task<FortniteAuthSession> Login() {
        return Authenticate(new() {
            { "grant_type", "authorization_code" },
            { "code", AuthorizationCode }
        });
    }
}

public class DeviceAuth : AuthBase<FortniteAuthSession, FortniteAuthData> {
    private string DeviceId { get; }
    private string AccountId { get; }
    private string Secret { get; }

    public DeviceAuth(string deviceId, string accountId, string secret, AuthClient? client = null) : base(client) {
        if (deviceId.Length != 32) throw new Exception("Invalid device ID");
        DeviceId = deviceId;
        if (accountId.Length != 32) throw new Exception("Invalid account ID");
        AccountId = accountId;
        if (secret.Length != 32) throw new Exception("Invalid secret");
        Secret = secret;
    }

    public DeviceAuth(DeviceAuthObject deviceAuth, AuthClient? client = null) : this(deviceAuth.DeviceId, deviceAuth.AccountId, deviceAuth.Secret, client) {}

    public async override Task<FortniteAuthSession> Login() {
        var prevClient = Client;
        Client = FortniteUtils.DeviceAuthClient;
        var session = await Authenticate(new() {
            { "grant_type", "device_auth" },
            { "device_id", DeviceId },
            { "account_id", AccountId },
            { "secret", Secret }
        });
        Client = prevClient;
        return prevClient == FortniteUtils.DeviceAuthClient ? session : await session.SwitchClient(Client);
    }
}

public class ExchangeCodeAuth : AuthBase<FortniteAuthSession, FortniteAuthData> {
    private string ExchangeCode { get; }

    public ExchangeCodeAuth(string exchangeCode, AuthClient? client = null) : base(client) {
        if (exchangeCode.Length != 32) throw new Exception("Invalid exchange code");
        ExchangeCode = exchangeCode;
    }

    public override Task<FortniteAuthSession> Login() {
        return Authenticate(new() {
            { "grant_type", "exchange_code" },
            { "exchange_code", ExchangeCode }
        });
    }
}

#endregion

#region AuthSession

public class AuthSession<T> where T : AuthData {
    public string AccessToken { get; protected set; }
    public string TokenType { get; protected set; }
    public DateTime ExpiresAt { get; protected set; }
    public int ExpiresIn { get; protected set; }
    public string ClientId { get; protected set; }
    public string ClientSecret { get; init; }

    public AuthSession(T data, string clientSecret) {
        AccessToken = data.AccessToken;
        TokenType = data.TokenType;
        ExpiresAt = FortniteUtils.ConvertToDateTime(data.ExpiresAt);
        ExpiresIn = data.ExpiresIn;
        ClientId = data.ClientId;
        ClientSecret = clientSecret;
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}

public class FortniteAuthSession : AuthSession<FortniteAuthData>, IAsyncDisposable {
    public string AccountId { get; private set; }
    public string App { get; private set; }
    public string ClientService { get; private set; }
    public string DisplayName { get; private set; }
    public bool IsInternalClient { get; private set; }
    public string InAppId { get; private set; }
    public string? DeviceId { get; private set; }
    public string RefreshToken { get; private set; }
    public DateTime RefreshExpiresAt { get; private set; }
    public Dictionary<string, int>? Perms { get; internal set; }

    private HttpClient Http = new();
    public SemaphoreSlim RefreshLock { get; } = new(1, 1);
    public Timer? RefreshTimer { get; private set; }

    public FortniteAuthSession(FortniteAuthData data, string clientSecret) : base(data, clientSecret) {
        AccountId = data.AccountId;
        App = data.App;
        ClientService = data.ClientService;
        DisplayName = data.DisplayName;
        IsInternalClient = data.InternalClient;
        InAppId = data.InAppId;
        DeviceId = data.DeviceId;
        RefreshToken = data.RefreshToken;
        RefreshExpiresAt = FortniteUtils.ConvertToDateTime(data.RefreshExpiresAt);

        SetRefreshTimer();
    }

    private void SetRefreshTimer() {
        // Should ExpiresAt be used instead of ExpiresIn?
        RefreshTimer = new(new(async (state) => await Refresh()), null, TimeSpan.FromSeconds(ExpiresIn), Timeout.InfiniteTimeSpan);
    }

    public async Task<bool> Verify(bool forceVerify = false) {
        if (!forceVerify && !IsExpired) return false;

        var request = new HttpRequestMessage(HttpMethod.Get, "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/verify");
        request.Headers.Add("Authorization", $"bearer {AccessToken}");
        var response = await new HttpClient().SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<Dictionary<string, int>?> GetPermissions(bool force = false) {
        if (!force && Perms is not null) return Perms;
        var request = new HttpRequestMessage(HttpMethod.Get, "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/permissions");
        request.Headers.Add("Authorization", $"bearer {AccessToken}");
        var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new Exception("Failed to get permissions");
        var data = JsonSerializer.Deserialize<JsonDocument>(await response.Content.ReadAsStringAsync()) ?? throw new Exception("Failed to deserialize permissions data");
        return data.RootElement.EnumerateArray().ToDictionary(x => x.GetProperty("resource").GetString() ?? "", x => x.GetProperty("action").GetInt32());
    }

    public async Task Refresh() {
        await RefreshLock.WaitAsync();
        Logging.Debug($"{AccountId} / {DisplayName} refreshing authentication...");
        string raw = string.Empty;
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
            raw = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<FortniteAuthData>(raw) ?? throw new Exception("Failed to deserialize authentication data");
            AccessToken = data.AccessToken;
            ExpiresAt = FortniteUtils.ConvertToDateTime(data.ExpiresAt);
            RefreshToken = data.RefreshToken;
            RefreshExpiresAt = FortniteUtils.ConvertToDateTime(data.RefreshExpiresAt);
            SetRefreshTimer();
            Logging.Debug($"{AccountId} / {DisplayName} refreshed authentication");
        } catch {
            Logging.Error($"{AccountId} / {DisplayName} failed to refresh authentication: {raw}");
        } finally {
            RefreshLock.Release();
        }
    }

    public async Task<ExchangeCodeObject> CreateExchangeCode() {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/exchange");
        request.Headers.Add("Authorization", $"bearer {AccessToken}");
        var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode) {
            Logging.Error($"Failed to create exchange code:\n{JsonSerializer.Serialize(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { WriteIndented = true })}");
            throw new Exception("Failed to create exchange code");
        }
        var data = JsonSerializer.Deserialize<ExchangeCodeObject>(await response.Content.ReadAsStringAsync()) ?? throw new Exception("Failed to deserialize exchange code data");
        return data;
    }

    public async Task<DeviceAuthObject> CreateDeviceAuth(bool force = false) {
        if (!force && ((await GetPermissions())!.GetValueOrDefault("account:public:account:deviceAuths", 0) & (int)EPermissions.Create) == 0) throw new Exception("Missing permissions to create device auth");
        FortniteAuthSession session = this;
        var switched = false;
        if (ClientId != FortniteUtils.DeviceAuthClient.ClientId) {
            session = await SwitchClient(FortniteUtils.DeviceAuthClient);
            switched = true;
        }
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://account-public-service-prod.ol.epicgames.com/account/api/public/account/{AccountId}/deviceAuth");
        request.Headers.Add("Authorization", $"bearer {session.AccessToken}");
        var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode) {
            Logging.Error($"Failed to create device auth:\n{JsonSerializer.Serialize(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { WriteIndented = true })}");
            throw new Exception("Failed to create device auth");
        }
        if (switched) await session.DisposeAsync();
        var data = JsonSerializer.Deserialize<DeviceAuthObject>(await response.Content.ReadAsStringAsync()) ?? throw new Exception("Failed to deserialize device auth data");
        return data;
    }

    public async Task<FortniteAuthSession> SwitchClient(AuthClient authClient) {
        var exchangeCode = await CreateExchangeCode();
        return await new ExchangeCodeAuth(exchangeCode.Code) { Client = authClient }.Login();
    }

    public async Task<EOSAuthSession> LoginEOS() {
        // This is also ok ?
        // grant_type=external_auth&external_auth_type=epicgames_access_token&external_auth_token=TOKEN&deployment_id=62a9473a2dca46b29ccf17577fcf42d7&nonce=RANDOM_STRING
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.epicgames.dev/epic/oauth/v2/token");
        request.Headers.Add("Authorization", $"basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}"))}");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>() {
            { "grant_type", "refresh_token" },
            { "refresh_token", RefreshToken },
            { "deployment_id", FortniteUtils.EOSDeploymentId }
        });
        var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new Exception("Failed to login EOS");
        var data = JsonSerializer.Deserialize<EOSAuthData>(await response.Content.ReadAsStringAsync()) ?? throw new Exception("Failed to deserialize EOS authentication data");
        return new EOSAuthSession(data, ClientSecret);
    }

    public async ValueTask DisposeAsync() {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"https://account-public-service-prod.ol.epicgames.com/account/api/oauth/sessions/kill/{AccessToken}");
        request.Headers.Add("Authorization", $"bearer {AccessToken}");
        await Http.SendAsync(request);
        Http.Dispose();
        RefreshTimer?.Dispose();
        RefreshLock.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class EOSAuthSession : AuthSession<EOSAuthData>, IAsyncDisposable {
    public string ApplicationId { get; private set; }
    public string RefreshToken { get; private set; }
    public string Scope { get; private set; }

    private HttpClient Http = new();
    public SemaphoreSlim RefreshLock { get; } = new(1, 1);
    public Timer? RefreshTimer { get; private set; }

    public EOSAuthSession(EOSAuthData data, string clientSecret) : base(data, clientSecret) {
        ApplicationId = data.ApplicationId;
        RefreshToken = data.RefreshToken;
        Scope = data.Scope;
    }

    private void SetRefreshTimer() {
        RefreshTimer = new(new(async (state) => await Refresh()), null, TimeSpan.FromSeconds(ExpiresIn), Timeout.InfiniteTimeSpan);
    }

    public async Task Refresh() {
        await RefreshLock.WaitAsync();
        Logging.Debug($"{ApplicationId} refreshing authentication...");
        try {
            RefreshTimer?.Dispose();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.epicgames.dev/epic/oauth/v2/token");
            request.Headers.Add("Authorization", $"basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}"))}");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>() {
                { "grant_type", "refresh_token" },
                { "refresh_token", RefreshToken },
                { "deployment_id", FortniteUtils.EOSDeploymentId }
            });
            var response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode) throw new Exception("Failed to refresh authentication");
            var data = JsonSerializer.Deserialize<EOSAuthData>(await response.Content.ReadAsStringAsync()) ?? throw new Exception("Failed to deserialize authentication data");
            AccessToken = data.AccessToken;
            ExpiresAt = FortniteUtils.ConvertToDateTime(data.ExpiresAt);
            RefreshToken = data.RefreshToken;
            ExpiresIn = data.ExpiresIn;
            SetRefreshTimer();
            Logging.Debug($"{ApplicationId} refreshed authentication");
        } finally {
            RefreshLock.Release();
        }
    }

    public async ValueTask DisposeAsync() {
        var request = new HttpRequestMessage(HttpMethod.Delete, "https://api.epicgames.dev/epic/oauth/v2/revoke");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>() {
            { "token", AccessToken }
        });
        await Http.SendAsync(request);
        Http.Dispose();
        RefreshTimer?.Dispose();
        RefreshLock.Dispose();
        GC.SuppressFinalize(this);
    }
}

#endregion