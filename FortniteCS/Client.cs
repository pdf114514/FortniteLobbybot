namespace FortniteCS;

public partial class FortniteClient {
    public ForntiteConfig Config { get; init; } = new();

    private AuthBase<AuthSession<FortniteAuthData>, FortniteAuthData> Auth { get; init; }
    public AuthSession<FortniteAuthData>? Session { get; private set; }

    public FortniteClient(AuthBase<AuthSession<FortniteAuthData>, FortniteAuthData> auth) {
        Auth = auth;
    }

    public async Task Start() {
        RegisterEvents();
        Session = await Auth.Login();
        Ready.Invoke();
    }
}