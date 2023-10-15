namespace Lobbybot.Shared;

public class LobbybotConfig {
    public WebConfig Web { get; set; } = new();
}

public class WebConfig {
    public bool PasswordEnabled { get; set; } = false;
    public string Password { get; set; } = "";
}