namespace FortniteCS;

public static class EJoinability {
    public const string Open = "OPEN";
    public const string InviteAndFormer = "INVITE_AND_FORMER";
}

public static class EDiscoverability {
    public const string All = "ALL";
    public const string InvitedOnly = "INVITED_ONLY";
}

public static class EPartyType {
    public const string Public = "Public";
    public const string FriendsOnly = "FriendsOnly";
    public const string Private = "Private";
}

public static class EInviteRestriction {
    public const string AnyMember = "AnyMember";
    public const string LeaderOnly = "LeaderOnly";
}

public static class EPresencePermission {
    public const string Anyone = "Anyone";
    public const string Leader = "Leader";
    public const string Noone = "Noone";
}

public static class EInvitePermission {
    public const string Anyone = "Anyone";
    public const string AnyMember = "AnyMember";
    public const string Leader = "Leader";
}

public class PartyPrivacy {
    public string PartyType { get; set; } = EPartyType.Public;
    public string InviteRestriction { get; set; } = EInviteRestriction.AnyMember;
    public bool OnlyLeaderFriendsCanJoin { get; set; } = false;
    public string PresencePermission { get; set; } = EPresencePermission.Anyone;
    public string InvitePermission { get; set; } = EInvitePermission.Anyone;
    public bool AcceptingMembers { get; set; } = true;
    
    public static readonly PartyPrivacy Public = new() {
        PartyType = EPartyType.Public,
        InviteRestriction = EInviteRestriction.AnyMember,
        OnlyLeaderFriendsCanJoin = false,
        PresencePermission = EPresencePermission.Anyone,
        InvitePermission = EInvitePermission.Anyone,
        AcceptingMembers = true
    };

    public static readonly PartyPrivacy FriendsOnly = new() {
        PartyType = EPartyType.FriendsOnly,
        InviteRestriction = EInviteRestriction.AnyMember,
        OnlyLeaderFriendsCanJoin = false,
        PresencePermission = EPresencePermission.Anyone,
        InvitePermission = EInvitePermission.AnyMember,
        AcceptingMembers = true
    };

    public static readonly PartyPrivacy Private = new() {
        PartyType = EPartyType.Private,
        InviteRestriction = EInviteRestriction.AnyMember,
        OnlyLeaderFriendsCanJoin = false,
        PresencePermission = EPresencePermission.Anyone,
        InvitePermission = EInvitePermission.Anyone,
        AcceptingMembers = true
    };

    public static readonly PartyPrivacy StrictPrivate = new() {
        PartyType = EPartyType.Private,
        InviteRestriction = EInviteRestriction.LeaderOnly,
        OnlyLeaderFriendsCanJoin = false,
        PresencePermission = EPresencePermission.Leader,
        InvitePermission = EInvitePermission.Leader,
        AcceptingMembers = true
    };
}

public class PartyOptions {
    public bool JoinConfirmation { get; set; } = true;
    public string Joinability { get; set; } = EJoinability.Open;
    public string Discoverability { get; set; } = EDiscoverability.All;
    public PartyPrivacy Privacy { get; set; } = PartyPrivacy.Public;
    public int MaxSize { get; set; } = 16;
    public int? IntentionTTL { get; set; }
    public int? InviteTTL { get; set; }
    public bool ChatEnabled { get; set; } = true;
}