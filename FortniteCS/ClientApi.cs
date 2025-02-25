using System.Net;
using System.Text;
using System.Text.Json;

namespace FortniteCS;

public partial class FortniteClient {
    public SemaphoreSlim PartyLock { get; } = new(1, 1);
    public SemaphoreSlim PartyChatLock { get; } = new(1, 1);

    #region Accounts

    private async Task<T1?> GetAccountByAccountId<T1, T2>(string accountId) where T1 : FortniteUser where T2 : FortniteUserData {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://account-public-service-prod.ol.epicgames.com/account/api/public/account/{accountId}");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        var json = await response.Content.ReadAsStringAsync();
        var user = (T1?)Activator.CreateInstance(typeof(T1), JsonSerializer.Deserialize<T2>(json));
        if (user is not null && !_Users.Any(x => x.AccountId == user.AccountId)) _Users.Add(user);
        return user;
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

    #endregion

    #region Friends

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

    public async Task<FortniteFriend> GetFriend(string accountId) {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://friends-public-service-prod.ol.epicgames.com/friends/api/v1/{User.AccountId}/friends/{accountId}");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK) throw new Exception("Failed to get friend!");
        var json = await response.Content.ReadAsStringAsync();
        var friend = new FortniteFriend(JsonSerializer.Deserialize<FortniteFriendData>(json) ?? throw new Exception("Failed to deserialize json!"));
        if (!_Friends.Any(x => x.AccountId == friend.AccountId)) _Friends.Add(friend);
        return friend;
    }

    public async Task AddFriend(string accountId) {
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://friends-public-service-prod.ol.epicgames.com/friends/api/public/friends/{User.AccountId}/{accountId}");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.NoContent) {
            Logging.Warn($"Failed to accept friend request! {response.StatusCode} - {json}");
            throw new Exception("Failed to accept friend request!");
        }
    }

    public async Task AccpetFriendRequest(IncomingPendingFriend friend) {
        await AddFriend(friend.AccountId);
    }

    public async Task SendFriendRequest(string accountId) {
        if (Friends.Any(x => x.AccountId == accountId)) throw new Exception("User is already your friend!");
        if (PendingFriends.FirstOrDefault(x => x.AccountId == accountId) is var pendingFriend && pendingFriend is OutgoingPendingFriend) throw new Exception("User already has a pending friend request!");
        await AddFriend(accountId);
    }

    public Task SendFriendRequest(FortniteUser user) => SendFriendRequest(user.AccountId);

    #endregion

    #region Party

    public async Task<FortniteClientParty?> GetClientParty() {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://party-service-prod.ol.epicgames.com/party/api/v1/Fortnite/user/{User.AccountId}");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK) {
            Logging.Warn($"Failed to get client party! {response.StatusCode}");
            return null;
        }
        var json = await response.Content.ReadAsStringAsync();
        Logging.Debug($"GetClientParty: {json}");
        return JsonSerializer.Deserialize<Dictionary<string, List<JsonElement>>>(json)?.GetValueOrDefault("current") is var current && current?.Count > 0 ? new(this, current.FirstOrDefault().Deserialize<FortnitePartyData>() ?? throw new Exception("Failed to deserialize json!")) : null;
    }

    public async Task InitializeParty(bool createNew = true, bool forceNew = true) {
        Party = await GetClientParty();
        if (!forceNew && Party is not null) return;
        if (createNew) {
            await LeaveParty(false);
            await CreateParty();
        }
    }

    public async Task LeaveParty(bool createNew = true) {
        if (Party is null) return;

        await PartyLock.WaitAsync();

        var request = new HttpRequestMessage(HttpMethod.Delete, $"https://party-service-prod.ol.epicgames.com/party/api/v1/Fortnite/parties/{Party.PartyId}/members/{User.AccountId}");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode) Logging.Warn($"Failed to leave party! {response.StatusCode}");
        Party = null;
        PartyLock.Release();
        if (createNew) await CreateParty();
    }

    public async Task CreateParty(PartyOptions? options = null) {
        if (Party is not null) await LeaveParty(false);
        options ??= new();
        FortniteParty party;

        await PartyLock.WaitAsync();
        try {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://party-service-prod.ol.epicgames.com/party/api/v1/Fortnite/parties");
            request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
            request.Content = new StringContent(JsonSerializer.Serialize(new {
                config = new {
                    join_confirmation = options.JoinConfirmation,
                    joinability = options.Joinability,
                    max_size = options.MaxSize
                },
                join_info = new {
                    connection = new {
                        id = XMPP.Connection.Jid.FullJid,
                        meta = new MetaDict {
                            ["urn:epic:conn:platform_s"] = Config.Platform,
                            ["urn:epic:conn:type_s"] = "game",
                        },
                        yield_leadership = true // false
                    },
                    meta = new MetaDict {
                        ["urn:epic:member:dn_s"] = User.DisplayName
                    }
                },
                meta = new MetaDict {
                    ["urn:epic:cfg:party-type-id_s"] = "default",
                    ["urn:epic:cfg:build-id_s"] = "1:3:",
                    ["urn:epic:cfg:join-request-action_s"] = "Manual",
                    ["urn:epic:cfg:chat-enabled_b"] = options.ChatEnabled ? "true" : "false",
                    ["urn:epic:cfg:can-join_b"] = "true"
                }
            }), Encoding.UTF8, "application/json");
            var response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode) {
                Logging.Error($"Failed to create party! {await response.Content.ReadAsStringAsync()}");
                throw new Exception($"Failed to create party! {response.StatusCode}");
            }
            var json = await response.Content.ReadAsStringAsync();
                Logging.Debug($"CreateParty: {json}");
            party = new(this, JsonSerializer.Deserialize<FortnitePartyData>(json) ?? throw new Exception("Failed to deserialize json!"));
        } finally {
            PartyLock.Release();
        }

        Logging.Debug($"CreateParty: {party.PartyId}");
        Party = new(this, party);
    }

    public async Task<List<FortniteParty>> GetPartiesByPingerId(string pingerId) {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://party-service-prod.ol.epicgames.com/party/api/v1/Fortnite/user/{User.AccountId}/pings/{pingerId}/parties");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK) throw new Exception($"Failed to get parties! {await response.Content.ReadAsStringAsync()}");
        var json = await response.Content.ReadAsStringAsync();
        return (JsonSerializer.Deserialize<List<FortnitePartyData>>(json) ?? throw new Exception("Failed to deserialize json!")).Select(x => new FortniteParty(this, x)).ToList();
    }

    public async Task JoinParty(FortniteParty party) {
        if (Party is not null) await LeaveParty(false);
        await PartyLock.WaitAsync();
        Logging.Debug($"Joining party {party.PartyId}");
        try {
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://party-service-prod.ol.epicgames.com/party/api/v1/Fortnite/parties/{party.PartyId}/members/{User.AccountId}/join");
            request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
            request.Content = new StringContent(JsonSerializer.Serialize(new {
                connection = new {
                    id = XMPP.Connection.Jid.FullJid,
                    meta = new MetaDict {
                        ["urn:epic:conn:platform_s"] = Config.Platform,
                        ["urn:epic:conn:type_s"] = "game",
                    },
                    yield_leadership = false
                },
                meta = new MetaDict {
                    ["urn:epic:member:dn_s"] = User.DisplayName,
                    ["urn:epic:member:joinrequestusers_j"] = JsonSerializer.Serialize(new Dictionary<string, object>() {
                        ["users"] = new List<object>() {
                            new {
                                id = User.AccountId,
                                dn = User.DisplayName,
                                plat = Config.Platform,
                                data = JsonSerializer.Serialize(new {
                                    CrossplayPreference = "1",
                                    SubGame_u = "1",
                                })
                            }
                        }
                    })
                }
            }), Encoding.UTF8, "application/json");
            var response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode) {
                Logging.Error($"Failed to join party! {await response.Content.ReadAsStringAsync()}");
                throw new Exception($"Failed to join party! {response.StatusCode}");
            }
        } catch {
            await InitializeParty(true, false);
            PartyLock.Release();
            throw;
        }

        Party = new(this, party);
        PartyLock.Release();
    }

    public async Task AcceptInvite(FortnitePartyInvite invite) {
        await JoinParty((await GetPartiesByPingerId(invite.SentBy)).First());

        var request = new HttpRequestMessage(HttpMethod.Delete, $"https://party-service-prod.ol.epicgames.com/party/api/v1/Fortnite/user/{User.AccountId}/pings/{invite.SentBy}");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode) Logging.Warn($"Failed to accept invite! {response.StatusCode}");
    }

    public async Task AcceptJoinRequest(FortnitePartyJoinRequest joinRequest) {
        // todo member duplicate and party full check
        if (Party is null) throw new Exception("Not in a party!");
        if (Party.Privacy.PartyType == EFortnitePartyType.Private) {
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://party-service-prod.ol.epicgames.com/party/api/v1/Fortnite/parties/{Party.PartyId}/invites/{joinRequest.AccountId}?sendPing=true");
            request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
            request.Content = new StringContent(JsonSerializer.Serialize(new MetaDict {
                ["urn:epic:cfg:build-id_s"] = "1:3:",
                ["urn:epic:conn:platform_s"] = Config.Platform,
                ["urn:epic:conn:type_s"] = "game",
                ["urn:epic:invite:platformdata_s"] = "",
                ["urn:epic:member:dn_s"] = User.DisplayName
            }), Encoding.UTF8, "application/json");
            var response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode) Logging.Warn($"Failed to accept party join request! {response.StatusCode}");
        } else {
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://party-service-prod.ol.epicgames.com/party/api/v1/Fortnite/user/{joinRequest.AccountId}/pings/{User.AccountId}");
            request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
            request.Content = new StringContent(JsonSerializer.Serialize(new MetaDict {
                ["urn:epic:invite:platformdata_s"] = ""
            }), Encoding.UTF8, "application/json");
            var response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode) Logging.Warn($"Failed to accept party join request! {response.StatusCode}");
        }
    }

    public async Task AcceptJoinConfirmation(FortnitePartyJoinConfirmation joinConfirmation) {
        if (joinConfirmation.Handled) {
            Logging.Warn("Join confirmation already handled!");
            return;
        }
        if (Party is null) throw new Exception("Not in a party!");
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://party-service-prod.ol.epicgames.com/party/api/v1/Fortnite/parties/{Party.PartyId}/members/{joinConfirmation.AccountId}/confirm");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        joinConfirmation.Handled = true;
        if (!response.IsSuccessStatusCode) Logging.Warn($"Failed to accept party join confirmation! {response.StatusCode}");
    }

    public async Task RejectJoinConfirmation(FortnitePartyJoinConfirmation joinConfirmation) {
        if (joinConfirmation.Handled) {
            Logging.Warn("Join confirmation already handled!");
            return;
        }
        if (Party is null) throw new Exception("Not in a party!");
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://party-service-prod.ol.epicgames.com/party/api/v1/Fortnite/parties/{Party.PartyId}/members/{joinConfirmation.AccountId}/reject");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        joinConfirmation.Handled = true;
        if (!response.IsSuccessStatusCode) Logging.Warn($"Failed to reject party join confirmation! {response.StatusCode}");
    }

    #endregion
}