using System.Collections.ObjectModel;

namespace FortniteCS;

public partial class FortniteClient : IAsyncDisposable {
    internal HttpClient Http { get; init; } = new();
    public FortniteConfig Config { get; init; } = new();

    private AuthBase<FortniteAuthSession, FortniteAuthData> Auth { get; init; }
    public FortniteAuthSession Session { get; private set; } = null!;
    public EOSAuthSession EOSSession { get; private set; } = null!;
    public FortniteXMPP XMPP { get; private set; } = null!;
    public EOSStomp Stomp { get; private set; } = null!;
    public bool IsReady => Session is not null;

    public FortniteClientUser User { get; private set; } = null!;
    private List<FortniteUser> _Users { get; } = new();
    public ReadOnlyCollection<FortniteUser> Users => _Users.AsReadOnly();
    private List<FortniteFriend> _Friends { get; } = new();
    public ReadOnlyCollection<FortniteFriend> Friends => _Friends.AsReadOnly();
    private List<PendingFriend> _PendingFriends { get; } = new();
    public ReadOnlyCollection<PendingFriend> PendingFriends => _PendingFriends.AsReadOnly();

    // invites
    private List<FortnitePartyJoinRequest> _PartyJoinRequests { get; } = new();
    public ReadOnlyCollection<FortnitePartyJoinRequest> PartyJoinRequests => _PartyJoinRequests.AsReadOnly();
    public FortniteClientParty? Party { get; private set; }

    public FortniteClient(AuthBase<FortniteAuthSession, FortniteAuthData> auth) {
        Auth = auth;
    }

    public async Task Start() {
        RegisterEvents();
        Session = await Auth.Login();
        Session.Perms = await Session.GetPermissions();
        EOSSession = await Session.LoginEOS();
        User = await GetAccountByAccountId<FortniteClientUser, FortniteClientUserData>(Session.AccountId) ?? throw new Exception("Failed to get client user data!");
        await UpdateFriends();

        XMPP = new(this);
        await XMPP.Connect();
        await XMPP.WaitForReady();
        Stomp = new(this);
        await Stomp.Connect();
        await InitializeParty(Config.CreateParty, Config.ForceNewParty);
        Ready?.Invoke();
    }

    public async ValueTask DisposeAsync() {
        XMPP.Dispose();
        await Stomp.DisposeAsync();
        await Session.DisposeAsync();
        await EOSSession.DisposeAsync();
        Http.Dispose();
        Logging.Debug("Client disposed!");
    }
}