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
    public FortniteClientParty Party { get; private set; } = null!;

    public FortniteClient(AuthBase<FortniteAuthSession, FortniteAuthData> auth) {
        Auth = auth;
    }

    public async Task Start() {
        RegisterEvents();
        Session = await Auth.Login();
        User = await GetAccountByAccountId<FortniteClientUser, FortniteClientUserData>(Session.AccountId) ?? throw new Exception("Failed to get client user data!");
        await UpdateFriends();
        XMPP = new(this);
        await XMPP.Connect();
        await InitializeParty(Config.CreateParty, Config.ForceNewParty);
        Ready?.Invoke();
    }

    public void Dispose() {
        Session?.Dispose();
        XMPP?.Dispose();
    }
}