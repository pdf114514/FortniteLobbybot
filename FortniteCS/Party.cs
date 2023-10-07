using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;

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

public class FortnitePartyMemberMeta : MetaDict {
    public string? Outfit { get => JsonSerializer.Deserialize<JsonElement>(this.GetValueOrDefault("Default:AthenaCosmeticLoadout_j") ?? "{}").GetProperty("AthenaCosmeticLoadout").GetProperty("characterDef").GetString(); }

    public FortnitePartyMemberMeta() : base() {}
    public FortnitePartyMemberMeta(MetaDict meta) : base(meta) {}
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

public class FortnitePartyInviteData {
    [K("party_id")] public required string PartyId { get; init; }
    [K("sent_by")] public required string SentBy { get; init; }
    [K("meta")] public required MetaDict Meta { get; init; }
    [K("sent_to")] public required string SentTo { get; init; }
    [K("sent_at")] public required string SentAt { get; init; }
    [K("updated_at")] public required string UpdatedAt { get; init; }
    [K("expires_at")] public required string ExpiresAt { get; init; }
    [K("status")] public required string Status { get; init; }
}

public class FortnitePartyData {
    [K("id")] public required string Id { get; init; }
    [K("created_at")] public required string CreatedAt { get; init; }
    [K("updated_at")] public required string UpdatedAt { get; init; }
    [K("config")] public required FortnitePartyConfigData Config { get; init; }
    [K("members")] public required List<FortnitePartyMemberData> Members { get; init; }
    [K("meta")] public required MetaDict Meta { get; init; }
    [K("invites")] public required List<FortnitePartyInviteData> Invites { get; init; }
    [K("revision")] public required int Revision { get; init; }
    // applicants
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
    public FortnitePartyMemberMeta Meta { get; }
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
        Meta = new(data.Meta);
        Revision = data.Revision;
    }

    public FortnitePartyMember(FortniteParty party, FortnitePartyMemberJoinedData data) {
        Party = party;
        AccountId = data.AccountId;
        DisplayName = data.AccountDn;
        Connections = new() { new(data.Connection) };
        Role = EFortnitePartyMemberRole.Member; // How leader joins?
        JoinedAt = Utils.ConvertToDateTime(data.JoinedAt);
        UpdatedAt = Utils.ConvertToDateTime(data.UpdatedAt);
        Meta = new();
        Revision = data.Revision;
    }

    public bool IsLeader => Role == EFortnitePartyMemberRole.Captain;
}

public class FortniteClientPartyMember : FortnitePartyMember {
    public FortniteClientPartyMember(FortniteParty party, FortnitePartyMemberData data) : base(party, data) {}
    public FortniteClientPartyMember(FortniteParty party, FortnitePartyMemberJoinedData data) : base(party, data) {}

    public async void SendPatch(MetaDict updated) {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"https://party-service-prod.ol.epicgames.com/party/api/v1/Fortnite/parties/{Party.PartyId}/members/{AccountId}/meta");
        request.Headers.Add("Authorization", $"bearer {Party.Client.Session.AccessToken}");
        request.Content = new StringContent(JsonSerializer.Serialize(new {
            delete = new List<string>(),
            revision = Revision,
            update = updated,
        }), Encoding.UTF8, "application/json");
        var response = await Party.Client.Http.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new Exception($"Failed to send patch: {response.StatusCode}");
        Revision++;
    }
}

// make sent and received one
public class FortnitePartyInvite {
    public string PartyId { get; }
    public string SentBy { get; }
    public MetaDict Meta { get; }
    public string SentTo { get; }
    public DateTime SentAt { get; }
    public DateTime UpdatedAt { get; }
    public DateTime ExpiresAt { get; }
    public string Status { get; }

    public FortnitePartyInvite(FortnitePartyInviteData data) {
        PartyId = data.PartyId;
        SentBy = data.SentBy;
        Meta = data.Meta;
        SentTo = data.SentTo;
        SentAt = Utils.ConvertToDateTime(data.SentAt);
        UpdatedAt = Utils.ConvertToDateTime(data.UpdatedAt);
        ExpiresAt = Utils.ConvertToDateTime(data.ExpiresAt);
        Status = data.Status;
    }
}

public class FortniteParty {
    public FortniteClient Client { get; }
    public string PartyId { get; }
    public DateTime CreatedAt { get; }
    public FortnitePartyConfigData Config { get; }
    public PartyPrivacy Privacy { get; }
    internal List<FortnitePartyMember> _Members { get; }
    public ReadOnlyDictionary<string, FortnitePartyMember> Members => _Members.ToDictionary(x => x.AccountId, x => x).AsReadOnly();
    public MetaDict Meta { get; }
    public List<FortnitePartyInvite> Invites { get; }
    public int Revision { get; }

    public FortniteParty(FortniteClient client, FortnitePartyData data) {
        Client = client;
        PartyId = data.Id;
        CreatedAt = Utils.ConvertToDateTime(data.CreatedAt);
        Config = data.Config;
        Privacy = new() {
            PartyType = data.Config.Type,
            InviteRestriction = data.Config.SubType,
            OnlyLeaderFriendsCanJoin = data.Config.JoinConfirmation,
            PresencePermission = data.Config.Joinability,
            InvitePermission = data.Config.Discoverability,
            AcceptingMembers = true
        };
        _Members = new();
        foreach (var member in data.Members) _Members.Add(new(this, member));
        Meta = data.Meta;
        Invites = data.Invites.Select(x => new FortnitePartyInvite(x)).ToList();
        Revision = data.Revision;
    }

    public FortniteParty(FortniteClient client, FortniteParty party) {
        Client = client;
        PartyId = party.PartyId;
        CreatedAt = party.CreatedAt;
        Config = party.Config;
        Privacy = party.Privacy;
        _Members = new();
        foreach (var member in party._Members) {
            member.Party = this;
            _Members.Add(member);
        }
        Meta = party.Meta;
        Invites = party.Invites;
        Revision = party.Revision;
    }

    public int Size => Members.Count;
    public int MaxSize => Config.MaxSize;
}

public class FortniteClientParty : FortniteParty {
    public FortniteClientParty(FortniteClient client, FortnitePartyData data) : base(client, data) {}
    public FortniteClientParty(FortniteClient client, FortniteParty party) : base(client, party) {}
}

public class FortnitePartyJoinRequest {
    public string PartyId { get; }
    public string AccountId { get; }
    public string DisplayName { get; }
    public DateTime SentAt { get; }
    public DateTime ExpiresAt { get; }

    public FortnitePartyJoinRequest(FortniteIntentionData data) {
        PartyId = data.PartyId;
        AccountId = data.RequesterId;
        DisplayName = data.RequesterDn;
        SentAt = Utils.ConvertToDateTime(data.SentAt);
        ExpiresAt = Utils.ConvertToDateTime(data.ExpiresAt);
    }
}

public class FortnitePartyJoinConfirmation {
    public int Revision { get; }
    public string PartyId { get; }
    public string AccountId { get; }
    public string DisplayName { get; }
    public DateTime SentAt { get; }
    public DateTime JoinedAt { get; }
    public FortnitePartyMemberConnection Connection { get; }
    internal bool Handled { get; set; } = false;

    public FortnitePartyJoinConfirmation(FortnitePartyMemberRequireConfirmationData confirmation) {
        Revision = confirmation.Revision;
        PartyId = confirmation.PartyId;
        AccountId = confirmation.AccountId;
        DisplayName = confirmation.AccountDn;
        SentAt = Utils.ConvertToDateTime(confirmation.Sent);
        JoinedAt = Utils.ConvertToDateTime(confirmation.JoinedAt);
        Connection = new(confirmation.Connection);
    }
}