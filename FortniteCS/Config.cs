namespace FortniteCS;

public class ForntiteConfig {
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
}