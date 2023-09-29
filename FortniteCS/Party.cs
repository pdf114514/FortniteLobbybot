using System.Collections.ObjectModel;

namespace FortniteCS;

#region Enums

public static class EFortnitePartyJoinability {
    public const string Open = "OPEN";
    public const string InviteAndFormer = "INVITE_AND_FORMER";
}

public static class EFortnitePartyDiscoverability {
    public const string All = "ALL";
    public const string InvitedOnly = "INVITED_ONLY";
}

public static class EFortnitePartyType {
    public const string Public = "Public";
    public const string FriendsOnly = "FriendsOnly";
    public const string Private = "Private";
}

public static class EFortnitePartyInviteRestriction {
    public const string AnyMember = "AnyMember";
    public const string LeaderOnly = "LeaderOnly";
}

public static class EFortnitePartyPresencePermission {
    public const string Anyone = "Anyone";
    public const string Leader = "Leader";
    public const string Noone = "Noone";
}

public static class EFortnitePartyInvitePermission {
    public const string Anyone = "Anyone";
    public const string AnyMember = "AnyMember";
    public const string Leader = "Leader";
}

public static class EFortnitePartyMemberRole {
    public const string Captain = "CAPTAIN";
    public const string Member = "MEMBER";
}

#endregion

public class PartyPrivacy {
    public string PartyType { get; set; } = EFortnitePartyType.Public;
    public string InviteRestriction { get; set; } = EFortnitePartyInviteRestriction.AnyMember;
    public bool OnlyLeaderFriendsCanJoin { get; set; } = false;
    public string PresencePermission { get; set; } = EFortnitePartyPresencePermission.Anyone;
    public string InvitePermission { get; set; } = EFortnitePartyInvitePermission.Anyone;
    public bool AcceptingMembers { get; set; } = true;
    
    public static readonly PartyPrivacy Public = new() {
        PartyType = EFortnitePartyType.Public,
        InviteRestriction = EFortnitePartyInviteRestriction.AnyMember,
        OnlyLeaderFriendsCanJoin = false,
        PresencePermission = EFortnitePartyPresencePermission.Anyone,
        InvitePermission = EFortnitePartyInvitePermission.Anyone,
        AcceptingMembers = true
    };

    public static readonly PartyPrivacy FriendsOnly = new() {
        PartyType = EFortnitePartyType.FriendsOnly,
        InviteRestriction = EFortnitePartyInviteRestriction.AnyMember,
        OnlyLeaderFriendsCanJoin = false,
        PresencePermission = EFortnitePartyPresencePermission.Anyone,
        InvitePermission = EFortnitePartyInvitePermission.AnyMember,
        AcceptingMembers = true
    };

    public static readonly PartyPrivacy Private = new() {
        PartyType = EFortnitePartyType.Private,
        InviteRestriction = EFortnitePartyInviteRestriction.AnyMember,
        OnlyLeaderFriendsCanJoin = false,
        PresencePermission = EFortnitePartyPresencePermission.Anyone,
        InvitePermission = EFortnitePartyInvitePermission.Anyone,
        AcceptingMembers = true
    };

    public static readonly PartyPrivacy StrictPrivate = new() {
        PartyType = EFortnitePartyType.Private,
        InviteRestriction = EFortnitePartyInviteRestriction.LeaderOnly,
        OnlyLeaderFriendsCanJoin = false,
        PresencePermission = EFortnitePartyPresencePermission.Leader,
        InvitePermission = EFortnitePartyInvitePermission.Leader,
        AcceptingMembers = true
    };
}

public class PartyOptions {
    public bool JoinConfirmation { get; set; } = true;
    public string Joinability { get; set; } = EFortnitePartyJoinability.Open;
    public string Discoverability { get; set; } = EFortnitePartyDiscoverability.All;
    public PartyPrivacy Privacy { get; set; } = PartyPrivacy.Public;
    public int MaxSize { get; set; } = 16;
    public int IntentionTTL { get; set; } = 60;
    public int InviteTTL { get; set; } = 14400; // 4 hours
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

public class FortnitePartyMemberConnectionData {
    [K("id")] public required string Id { get; init; } // JID
    [K("connected_at")] public required string ConnectedAt { get; init; }
    [K("updated_at")] public required string UpdatedAt { get; init; }
    [K("yield_leadership")] public required bool YieldLeadership { get; init; }
    [K("meta")] public required MetaDict Meta { get; init; }
}

public class FortnitePartyMemberData {
    [K("account_id")] public required string AccountId { get; init; }
    [K("meta")] public required MetaDict Meta { get; init; }
    [K("connections")] public required List<FortnitePartyMemberConnectionData> Connections { get; init; }
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
    [K("meta")] public required MetaDict Meta { get; init; }
    [K("invites")] public required List<object> Invites { get; init; }
    [K("revision")] public required int Revision { get; init; }
    // applicants
    // invites
    // intentions
}

public class FortnitePartyMemberConnection {
    public string Id { get; } // JID
    public DateTime ConnectedAt { get; }
    public DateTime UpdatedAt { get; }
    public bool YieldLeadership { get; }
    public MetaDict Meta { get; }

    public FortnitePartyMemberConnection(FortnitePartyMemberConnectionData data) {
        Id = data.Id;
        ConnectedAt = Utils.ConvertToDateTime(data.ConnectedAt);
        UpdatedAt = Utils.ConvertToDateTime(data.UpdatedAt);
        YieldLeadership = data.YieldLeadership;
        Meta = data.Meta;
    }
}

public class FortnitePartyMember {
    public FortniteParty Party { get; internal set; }
    public string AccountId { get; }
    public string? DisplayName { get; }
    public List<FortnitePartyMemberConnection> Connections { get; }
    public string Role { get; internal set; }
    public DateTime JoinedAt { get; set; }
    public DateTime UpdatedAt { get; internal set; }
    public MetaDict Meta { get; }
    public int Revision { get; internal set; }
    public bool ReceivedIninitalStateUpdate { get; internal set; }

    public FortnitePartyMember(FortniteParty party, FortnitePartyMemberData data) {
        Party = party;
        AccountId = data.AccountId;
        DisplayName = data.Meta.GetValueOrDefault("urn:epic:member:dn_s");
        Connections = data.Connections.Select(x => new FortnitePartyMemberConnection(x)).ToList();
        Role = data.Role;
        JoinedAt = Utils.ConvertToDateTime(data.JoinedAt);
        UpdatedAt = Utils.ConvertToDateTime(data.UpdatedAt);
        Meta = data.Meta;
        Revision = data.Revision;
    }

    public FortnitePartyMember(FortniteParty party, FortnitePartyMemberJoinedPayload data) {
        Party = party;
        AccountId = data.AccountId;
        DisplayName = data.AccountDn;
        Connections = new() { new(data.Connection) };
        Role = EFortnitePartyMemberRole.Member;
        JoinedAt = Utils.ConvertToDateTime(data.JoinedAt);
        UpdatedAt = Utils.ConvertToDateTime(data.UpdatedAt);
        Meta = new();
        Revision = data.Revision;
    }

    public bool IsLeader => Role == EFortnitePartyMemberRole.Captain;
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
    internal List<FortnitePartyMember> _Members { get; }
    public ReadOnlyCollection<FortnitePartyMember> Members { get; }
    public MetaDict Meta { get; }
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

    public FortniteParty(FortniteClient client, FortniteParty party) {
        Client = client;
        PartyId = party.PartyId;
        CreatedAt = party.CreatedAt;
        Config = party.Config;
        Privary = party.Privary;
        _Members = new();
        foreach (var member in party.Members) {
            member.Party = this;
            _Members.Add(member);
        }
        Members = _Members.AsReadOnly();
        Meta = party.Meta;
        Revision = party.Revision;
    }

    public int Size => Members.Count;
    public int MaxSize => Config.MaxSize;
}

public class FortniteClientParty : FortniteParty {
    public FortniteClientParty(FortniteClient client, FortnitePartyData data) : base(client, data) {}
    public FortniteClientParty(FortniteClient client, FortniteParty party) : base(client, party) {}
}