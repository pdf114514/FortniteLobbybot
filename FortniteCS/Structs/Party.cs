namespace FortniteCS;

public class EPartyType {
    public const string Public = "Public";
    public const string FriendsOnly = "FriendsOnly";
    public const string Private = "Private";
}

public class EInviteRestriction {
    public const string AnyMember = "AnyMember";
    public const string LeaderOnly = "LeaderOnly";
}

public class EPresencePermission {
    public const string Anyone = "Anyone";
    public const string Leader = "Leader";
    public const string Noone = "Noone";
}

public class PartyPrivacy {
    public string PartyType { get; set; } = EPartyType.Public;
    public string InviteRestriction { get; set; } = EInviteRestriction.AnyMember;
    public bool OnlyLeaderFriendsCanJoin { get; set; } = false;
    public string PresencePermission { get; set; } = EPresencePermission.Anyone;
    public bool AcceptingMembers { get; set; } = true;
}

public class PartyOptions {
    public bool? JoinConfirmation { get; set; }
    public string? Joinability { get; set; }
    public string? Discoverability { get; set; }
    public PartyPrivacy? Privacy { get; set; }
    public int? MaxSize { get; set; }
    public int? IntentionTTL { get; set; }
    public int? InviteTTL { get; set; }
    public bool? ChatEnabled { get; set; }
}