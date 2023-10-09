namespace FortniteCS;

public class FortniteConfig {
    public bool SavePartyMemberMeta { get; set; } = true;
    public string DefaultStatus { get; set; } = "Battle Royale Lobby";
    public string DefaultOnlineType { get; set; } = EFortnitePresenceOnlineType.Online;
    public string Platform { get; set; } = EFortnitePlatform.Windows;
    public MetaDict DefaultPartyMemberMeta { get; set; } = new();
    public PartyOptions PartyConfig { get; set; } = new();
    public bool CreateParty { get; set; } = true;
    public bool ForceNewParty { get; set; } = false;
    public int FriendOfflineTimeout { get; set; } = 300; // in seconds
    public bool RestartOnInvalidRefresh { get; set; } = true;
    public string Language { get; set; } = "en-US"; // Accept-Language header
    public bool KillOtherTokens { get; set; } = true;

    public string DefaultOutfit { get; set; } = "CID_001_Athena_Commando_F_Default";
    public string DefaultBackpack { get; set; } = "BID_001_Default";
    public string DefaultPickaxe { get; set; } = "Pickaxe_Lockjaw";
    public string DefaultEmote { get; set; } = "EID_Floss";
}