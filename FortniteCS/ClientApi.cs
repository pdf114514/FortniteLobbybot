using System.Net;
using System.Text.Json;

namespace FortniteCS;

public partial class FortniteClient {
    public async Task UpdateFriends() {
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

    public async Task<FortniteClientParty?> GetClientParty() {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://party-service-prod.ol.epicgames.com/party/api/v1/Fortnite/user/{User.AccountId}");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK) {
            Logging.Warn($"Failed to get client party! {response.StatusCode}");
            return null;
        }
        var json = await response.Content.ReadAsStringAsync();
        return new(this, JsonSerializer.Deserialize<FortnitePartyData>(json) ?? throw new Exception("Failed to deserialize json!"));
    }

    public async Task InitializeParty(bool createNew = true, bool forceNew = true) {
        Party = (await GetClientParty())!;
        if (!forceNew && Party is not null) return;
        if (createNew) {
            await LeaveParty(false);
            await CreateParty();
        }
    }

    public async Task LeaveParty(bool createNew = true) {
        if (Party is null) return;
    }

    public async Task CreateParty() {}
}