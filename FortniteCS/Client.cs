using System.Collections.ObjectModel;
using System.Net;
using System.Text.Json;

namespace FortniteCS;

public partial class FortniteClient : IDisposable {
    private HttpClient Http { get; init; } = new();
    public ForntiteConfig Config { get; init; } = new();

    private AuthBase<FortniteAuthSession, FortniteAuthData> Auth { get; init; }
    public FortniteAuthSession Session { get; private set; } = null!;
    public FortniteXMPP XMPP { get; private set; } = null!;
    public bool IsReady => Session is not null;

    public FortniteClientUser User { get; private set; } = null!;
    private List<FortniteFriend> _Friends { get; } = new();
    public ReadOnlyCollection<FortniteFriend> Friends => _Friends.AsReadOnly();
    private List<PendingFriend> _PendingFriends { get; } = new();
    public ReadOnlyCollection<PendingFriend> PendingFriends => _PendingFriends.AsReadOnly();

    public FortniteClient(AuthBase<FortniteAuthSession, FortniteAuthData> auth) {
        Auth = auth;
    }

    public async Task Start() {
        RegisterEvents();
        Session = await Auth.Login();
        User = await GetAccountByAccountId<FortniteClientUser, FortniteClientUserData>(Session.AccountId) ?? throw new Exception("Failed to get client user data!");
        await Update();
        XMPP = new(this);
        await XMPP.Connect();
        Ready?.Invoke();
    }

    public async Task Update() {
        _Friends.Clear();
        _PendingFriends.Clear();

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://friends-public-service-prod.ol.epicgames.com/friends/api/v1/{User.AccountId}/summary");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK) throw new Exception("Failed to get friends!");
        var friendsSummary = JsonSerializer.Deserialize<FriendsSummaryData>(await response.Content.ReadAsStringAsync()) ?? throw new Exception("Failed to deserialize json!");
        foreach (var friend in friendsSummary.Friends) _Friends.Add(new(friend));
        foreach (var incoming in friendsSummary.Incoming) _PendingFriends.Add(new IncomingPendingFriend(incoming));
        foreach (var outgoing in friendsSummary.Outgoing) _PendingFriends.Add(new OutgoingPendingFriend(outgoing));
    }

    private async Task<T1?> GetAccountByAccountId<T1, T2>(string accountId) where T1 : FortniteUser where T2 : FortniteUserData {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://account-public-service-prod.ol.epicgames.com/account/api/public/account/{accountId}");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        var json = await response.Content.ReadAsStringAsync();
        return (T1?)Activator.CreateInstance(typeof(T1), JsonSerializer.Deserialize<T2>(json));
    }

    public Task<FortniteUser?> GetUserByAccountId(string accountId) => GetAccountByAccountId<FortniteUser, FortniteUserData>(accountId);

    public async Task<FortniteUser?> GetUserByDisplayName(string displayName) {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://account-public-service-prod.ol.epicgames.com/account/api/public/account/displayName/{displayName}");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        var json = await response.Content.ReadAsStringAsync();
        return new(JsonSerializer.Deserialize<FortniteUserData>(json) ?? throw new Exception("Failed to deserialize json!"));
    }

    public void Dispose() {
        Session?.Dispose();
        XMPP?.Dispose();
    }
}