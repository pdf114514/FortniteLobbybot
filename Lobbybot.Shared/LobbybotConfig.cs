namespace Lobbybot.Shared;

public class LobbybotConfig {
    public WebConfig Web { get; set; } = new();
    public Dictionary<string, BotConfig> Bots { get; set; } = new(); // AccountId: <BotConfig>
}

public class WebConfig {
    public bool PasswordEnabled { get; set; } = false;
    public string Password { get; set; } = "password";
}

public class BotConfig {
    public bool AutoStart { get; set; } = false;
    public string Outfit { get; set; } = "CID_001_Athena_Commando_F_Default";
    public string Backpack { get; set; } = "";
    public string Pickaxe { get; set; } = "DefaultPickaxe";
    public string Emote { get; set; } = "EID_DanceMoves";
}