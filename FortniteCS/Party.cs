using System.Collections.ObjectModel;

namespace FortniteCS;

#region Enums

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

#endregion

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

public class FortnitePartyConfigData {
    [K("type")] public required string Type { get; init; }
    [K("joinability")] public required string Joinability { get; init; }
    [K("discoverability")] public required string Discoverability { get; init; }
    [K("sub_type")] public string SubType { get; init; } = "default";
    [K("max_size")] public required int MaxSize { get; init; }
    [K("invite_ttl")] public required int InviteTTL { get; init; }
    [K("intention_ttl")] public required int IntentionTTL { get; init; }
    [K("join_confirmation")] public required bool JoinConfirmation { get; init; }
}

public class FortnitePartyMemberData {
    [K("id")] public required string Id { get; init; }
    [K("account_id")] public required string AccountId { get; init; }
    [K("account_dn")] public string? AccountDn { get; init; }
    [K("meta")] public required Dictionary<string, string> Meta { get; init; }
    [K("revision")] public required int Revision { get; init; }
    [K("updated_at")] public required string UpdatedAt { get; init; }
    [K("joined_at")] public required string JoinedAt { get; init; }
    [K("role")] public required string Role { get; init; }
}

public class FortnitePartyData {
    [K("id")] public required string Id { get; init; }
    [K("created_at")] public required string CreatedAt { get; init; }
    [K("updated_at")] public required string UpdatedAt { get; init; }
    [K("config")] public required FortnitePartyConfigData Config { get; init; }
    [K("members")] public required List<FortnitePartyMemberData> Members { get; init; }
    [K("meta")] public required Dictionary<string, string> Meta { get; init; }
    [K("invites")] public required List<object> Invites { get; init; }
    [K("revision")] public required int Revision { get; init; }
}

public class FortnitePartyMember {
    public FortniteParty Party { get; }
    public string AccountId { get; }
    public string? DisplayName { get; }
    public string Role { get; internal set; }
    public DateTime JoinedAt { get; set; }
    public DateTime UpdatedAt { get; internal set; }
    public Dictionary<string, string> Meta { get; }
    public int Revision { get; internal set; }
    public bool ReceivedIninitalStateUpdate { get; internal set; }

    public FortnitePartyMember(FortniteParty party, FortnitePartyMemberData data) {
        Party = party;
        AccountId = data.AccountId;
        DisplayName = data.AccountDn;
        Role = data.Role;
        JoinedAt = Utils.ConvertToDateTime(data.JoinedAt);
        UpdatedAt = Utils.ConvertToDateTime(data.UpdatedAt);
        Meta = data.Meta;
        Revision = data.Revision;
    }

    public bool IsLeader => Role == "CAPTAIN";
}

public class FortniteClientPartyMember : FortnitePartyMember {
    public FortniteClientPartyMember(FortniteParty party, FortnitePartyMemberData data) : base(party, data) {}
}

public class FortniteParty {
    public FortniteClient Client { get; }
    public string PartyId { get; }
    public DateTime CreatedAt { get; }
    public FortnitePartyConfigData Config { get; }
    public PartyPrivacy Privary { get; }
    private List<FortnitePartyMember> _Members { get; }
    public ReadOnlyCollection<FortnitePartyMember> Members { get; }
    public Dictionary<string, string> Meta { get; }
    public int Revision { get; }

    public FortniteParty(FortniteClient client, FortnitePartyData data) {
        Client = client;
        PartyId = data.Id;
        CreatedAt = Utils.ConvertToDateTime(data.CreatedAt);
        Config = data.Config;
        Privary = new() {
            PartyType = data.Config.Type,
            InviteRestriction = data.Config.SubType,
            OnlyLeaderFriendsCanJoin = data.Config.JoinConfirmation,
            PresencePermission = data.Config.Joinability,
            InvitePermission = data.Config.Discoverability,
            AcceptingMembers = true
        };
        _Members = new();
        foreach (var member in data.Members) _Members.Add(new(this, member));
        Members = _Members.AsReadOnly();
        Meta = data.Meta;
        Revision = data.Revision;
    }

    public int Size => Members.Count;
    public int MaxSize => Config.MaxSize;
}

public class FortniteClientParty : FortniteParty {
    public FortniteClientParty(FortniteClient client, FortnitePartyData data) : base(client, data) {}
}