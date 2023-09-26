using System.Collections.ObjectModel;

namespace FortniteCS;

public class FortniteUserData {
    [K("id")] public required string Id { get; init; }
    [K("displayName")] public string? DisplayName { get; init; }
    [K("externalAuths")] public ExternalAuths ExternalAuths { get; init; } = new();
}

public class ExternalAuth {
    public class AuthId {
        [K("id")] public required string Id { get; init; }
        [K("type")] public required string Type { get; init; }
    }

    [K("accountId")] public required string AccountId { get; init; }
    [K("displayName")] public required string Type { get; init; }
    [K("type")] public required string ExternalAuthId { get; init; }
    [K("externalAuthIdType")] public required string ExternalAuthIdType { get; init; }
    [K("externalDisplayName")] public string? ExternalDisplayName { get; init; }
    [K("authIds")] public required ReadOnlyCollection<AuthId> AuthIds { get; init; }
}

public class ExternalAuths {
    [K("github")] public ExternalAuth? Github { get; init; }
    [K("twitch")] public ExternalAuth? Twitch { get; init; }
    [K("steam")] public ExternalAuth? Steam { get; init; }
    [K("xbl")] public ExternalAuth? XBL { get; init; }
    [K("psn")] public ExternalAuth? PSN { get; init; }
    [K("nintendo")] public ExternalAuth? Nintendo { get; init; }
}

public class FortniteClientUserData : FortniteUserData {
    [K("name")] public string? Name { get; init; }
    [K("lastName")] public string? LastName { get; init; }
    [K("email")] public required string Email { get; init; }
    [K("failedLoginAttempts")] public required int FailedLoginAttempts { get; init; }
    [K("lastLogin")] public required string LastLogin { get; init; }
    [K("numberOfDisplayNameChanges")] public required int NumberOfDisplayNameChanges { get; init; }
    [K("ageGroup")] public required string AgeGroup { get; init; }
    [K("headless")] public required bool Headless { get; init; }
    [K("country")] public required string Country { get; init; }
    [K("preferredLanguage")] public required string PreferredLanguage { get; init; }
    [K("canUpdateDisplayName")] public required bool CanUpdateDisplayName { get; init; }
    [K("tfaEnabled")] public required bool TFAEnabled { get; init; }
    [K("emailVerified")] public required bool EmailVerified { get; init; }
    [K("minorVerified")] public required bool MinorVerified { get; init; }
    [K("minorExpected")] public required bool MinorExpected { get; init; }
    [K("minorStatus")] public required string MinorStatus { get; init; }
}

public class FriendConnection {
    [K("name")] public string? Name { get; init; }
}

public class FriendConnections {
    [K("epic")] public FriendConnection? Epic { get; init; }
    [K("psn")] public FriendConnection? PSN { get; init; }
    [K("nintendo")] public FriendConnection? Nintendo { get; init; }
}

public class FortniteUser {
    public string AccountId { get; init; }
    public string? EpicGamesDisplayName { get; private set; }
    public ExternalAuths ExternalAuths { get; private set; }

    public string DisplayName => EpicGamesDisplayName ?? ExternalAuths.GetType().GetProperties().Select(x => ((ExternalAuth?)x.GetValue(ExternalAuths))?.ExternalDisplayName).FirstOrDefault(x => x is not null) ?? throw new Exception("The user has no displayname!");
    public bool IsHeadless => EpicGamesDisplayName is null;

    public FortniteUser(FortniteUserData data) {
        AccountId = data.Id;
        EpicGamesDisplayName = data.DisplayName;
        ExternalAuths = data.ExternalAuths;
    }
}

public class FortniteClientUser : FortniteUser {
    public string? Name { get; init; }
    public string? LastName { get; init; }
    public string Email { get; init; }
    public int FailedLoginAttempts { get; init; }
    public DateTime LastLogin { get; init; }
    public int NumberOfDisplayNameChanges { get; init; }
    public string AgeGroup { get; init; }
    public bool Headless { get; init; }
    public string Country { get; init; }
    public string PreferredLanguage { get; init; }
    public bool CanUpdateDisplayName { get; init; }
    public bool TFAEnabled { get; init; }
    public bool EmailVerified { get; init; }
    public bool MinorVerified { get; init; }
    public bool MinorExpected { get; init; }
    public string MinorStatus { get; init; }

    public FortniteClientUser(FortniteClientUserData data) : base(data) {
        Name = data.Name;
        LastName = data.LastName;
        Email = data.Email;
        FailedLoginAttempts = data.FailedLoginAttempts;
        LastLogin = Utils.ConvertToDateTime(data.LastLogin);
        NumberOfDisplayNameChanges = data.NumberOfDisplayNameChanges;
        AgeGroup = data.AgeGroup;
        Headless = data.Headless;
        Country = data.Country;
        PreferredLanguage = data.PreferredLanguage;
        CanUpdateDisplayName = data.CanUpdateDisplayName;
        TFAEnabled = data.TFAEnabled;
        EmailVerified = data.EmailVerified;
        MinorVerified = data.MinorVerified;
        MinorExpected = data.MinorExpected;
        MinorStatus = data.MinorStatus;
    }
}