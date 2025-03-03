using System.Net;
using System.Text;
using System.Text.Json;

namespace FortniteCS;

public class PublicKeyData {
    [K("key")] public required string Key { get; init; } // `key` value from the request
    [K("account_id")] public required string AccountId { get; init; } // account id of the authenticated user
    [K("key_guid")] public required string KeyGuid { get; init; } // uuid
    [K("kid")] public required string Kid { get; init; } // 8 digit number
    [K("expiration")] public required string Expiration { get; init; } // ex: 2025-01-01T10:10:10.000000000Z
    [K("jwt")] public required string Jwt { get; init; }
    [K("type")] public required string Type { get; init; } // legacy
}

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

    public async Task<List<FortniteUser>> GetUsersByAccountIds(params IEnumerable<string> accountIds) {
        var chunks = accountIds.Select((x, i) => (Index: i, Value: x)).GroupBy(x => x.Index / 100).Select(x => x.Select(v => v.Value).ToArray()).ToArray();
        async Task<IEnumerable<FortniteUser>> createTask(IEnumerable<string> accountIds) {
            try {
                var uri = new UriBuilder("https://account-public-service-prod.ol.epicgames.com/account/api/public/account");
                uri.Query = string.Join("&", accountIds.Select(x => $"accountId={x}"));
                var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString());
                request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
                var response = await Http.SendAsync(request);
                if (response.StatusCode != HttpStatusCode.OK) throw new Exception("Failed to get users!");
                return JsonSerializer.Deserialize<IEnumerable<FortniteUserData>>(await response.Content.ReadAsStringAsync())?.Select(x => new FortniteUser(x)) ?? throw new Exception("Failed to deserialize json!");
            } catch (Exception e) {
                Logging.Error(e.ToString());
                throw;
            }
        }
        return (await Task.WhenAll(chunks.Select(createTask))).SelectMany(x => x).ToList();
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
        foreach (var friend in friendsSummary.Friends) _Friends.Add(new(this, friend));
        foreach (var incoming in friendsSummary.Incoming) _PendingFriends.Add(new IncomingPendingFriend(incoming));
        foreach (var outgoing in friendsSummary.Outgoing) _PendingFriends.Add(new OutgoingPendingFriend(outgoing));

        var users = await GetUsersByAccountIds(_Friends.Select(x => x.AccountId));
        foreach (var user in users) {
            try {
                var friend = _Friends.FirstOrDefault(x => x.AccountId == user.AccountId);
                if (friend is not null) friend.DisplayName = user.DisplayName;
            } catch (Exception e) {
                if (e.Message == "The user has no displayname!") continue;
                throw;
            }
        }
    }

    public async Task<FortniteFriend> GetFriend(string accountId) {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://friends-public-service-prod.ol.epicgames.com/friends/api/v1/{User.AccountId}/friends/{accountId}");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        var response = await Http.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK) throw new Exception("Failed to get friend!");
        var json = await response.Content.ReadAsStringAsync();
        var friend = new FortniteFriend(this, JsonSerializer.Deserialize<FortniteFriendData>(json) ?? throw new Exception("Failed to deserialize json!"));
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

    #region EOS

    public async Task SendPresence(string connectionId) {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"https://api.epicgames.dev/epic/presence/v1/{FortniteUtils.EOSDeploymentId}/{User.AccountId}/presence/{connectionId}");
        request.Headers.Add("Authorization", $"bearer {EOSSession.AccessToken}");
        request.Content = new StringContent(JsonSerializer.Serialize(new {
            status = "online",
            activity = new {
                value = ""
            },
            props = new {
                EOS_Platform = "WIN", // Config.Platform
                EOS_IntegratedPlatform = "EGS",
                EOS_OnlinePlatformType = 100,
                EOS_ProductVersion = FortniteUtils.Build,
                EOS_ProductName = "Fortnite",
                EOS_Session = "{\"version:3\"}",
                EOS_Lobby = "{\"version:3\"}"
            },
            conn = new {
                props = new {}
            }
        }), Encoding.UTF8, "application/json");
        var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode) Logging.Warn($"Failed SendEOSPresence! {response.StatusCode}");
    }

    private async Task<PublicKeyData> PublicKey(string key) {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://publickey-service-live.ecosec.on.epicgames.com/publickey/v2/publickey/");
        request.Headers.Add("Authorization", $"bearer {Session.AccessToken}");
        request.Content = new StringContent(JsonSerializer.Serialize(new {
            algorithm = "ed25519", // any value ok; may not effecting the result ?
            key = Convert.ToBase64String(Encoding.UTF8.GetBytes(key))
        }), Encoding.UTF8, "application/json");
        var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new Exception($"Failed to get public key! {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        return JsonSerializer.Deserialize<PublicKeyData>(await response.Content.ReadAsStringAsync()) ?? throw new Exception("Failed to deserialize json!");
    }

    // Cursed part 💀
    public async Task SendMessageToFriend(FortniteFriend friend, string message) {
        if (message.Length >= 2048) throw new Exception("Message is too long!");
        var keyBuffer = new byte[16];
        new Random(Encoding.UTF8.GetBytes(User.AccountId).Sum(x => x)).NextBytes(keyBuffer);
        var pk = await PublicKey(Convert.ToBase64String(keyBuffer));
        var pld = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
            mid = Guid.NewGuid().ToString("N").ToUpper(),
            sid = User.AccountId,
            rid = "None",
            msg = message,
            tst = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            seq = 1,
            rec = false,
            mts = new List<object>(),
            cty = "Whisper"
        }, new JsonSerializerOptions() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true, IndentCharacter = '\t', IndentSize = 1 })+'\0'));
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.epicgames.dev/epic/chat/v1/public/{FortniteUtils.EOSDeploymentId}/whisper/{User.AccountId}/{friend.AccountId}");
        request.Headers.Add("Authorization", $"bearer {EOSSession.AccessToken}");
        request.Content = new StringContent(JsonSerializer.Serialize(new {
            message = new {
                body = message
            },
            metadata = new {
                Pub = pk.Jwt,
                Sig = "",
                Pld = pld,
                PlfNm = "WIN",
                PlfId = User.AccountId,
                WaToRec = false
            }
        }, new JsonSerializerOptions() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }), Encoding.UTF8, "application/json");
        var response = await Http.SendAsync(request);
        Logging.Debug($"SendMessageToFriend: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        if (!response.IsSuccessStatusCode) Logging.Warn($"Failed to send message to friend! {response.StatusCode}");
    }

    public async Task SendMessageToParty(string message) {
        if (message.Length >= 2048) throw new Exception("Message is too long!");
        if (Party is null) throw new Exception("Not in a party!");
        if (Party.Members.Count <= 1) throw new Exception("No one in the party!");
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.epicgames.dev/epic/chat/v1/public/{FortniteUtils.EOSDeploymentId}/conversations/p-{Party.PartyId}/messages?fromAccountId={User.AccountId}");
        request.Headers.Add("Authorization", $"bearer {EOSSession.AccessToken}");
        request.Content = new StringContent(JsonSerializer.Serialize(new {
            allowedRecipients = Party.Members.Keys,
            message = new {
                body = message
            }
        }), Encoding.UTF8, "application/json");
        var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode) Logging.Warn($"Failed to send message to party! {response.StatusCode}");
    }

    #endregion
}