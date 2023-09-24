namespace FortniteCS;

public record AuthClient(string ClientId, string Secret);

public static class AuthClients {
    public static readonly AuthClient FortnitePCGameClient = new("ec684b8c687f479fadea3cb2ad83f5c6", "e1f31c211f28413186262d37a13fc84d");
    public static readonly AuthClient FortniteIOSGameClient = new("3446cd72694c4a4485d81b77adbb2141", "9209d4a5e25a457fb9b07489d313b41a");
    public static readonly AuthClient FortniteAndroidGameClient = new("3f69e56c7649492c8cc29f1af08a8a12", "b51ee9cb12234f50a69efa67ef53812e");
    public static readonly AuthClient FortniteSwitchGameClient = new("5229dcd3ac3845208b496649092f251b", "e3bd2d3e-bf8c-4857-9e7d-f3d947d220c7");
    public static readonly AuthClient FortniteNewSwitchGameClient = new("98f7e42c2e3a4f86a74eb43fbb41ed39", "0a2449a2-001a-451e-afec-3e812901c4d7");
    public static readonly AuthClient FortniteCNGameClient = new("efe3cbb938804c74b20e109d0efc1548", "6e31bdbae6a44f258474733db74f39ba");
    public static readonly AuthClient LauncherAppClient2 = new("34a02cf8f4414e29b15921876da36f9a", "daafbccc737745039dffe53d94fc76cf");
    public static readonly AuthClient Diesel_Dauntless = new("b070f20729f84693b5d621c904fc5bc2", "HG@XE&TGCxEJsgT#&_p2]=aRo#~>=>+c6PhR)zXP");
}