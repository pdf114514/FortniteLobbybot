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

#region Meta

public class FortniteAthenaBannerInfoMeta {
    [K("bannerIconId")] public string BannerIconId { get; set; } = "standardbanner15";
    [K("bannerColorId")] public string BannerColorId { get; set; } = "defaultcolor15";
    [K("seasonLevel")] public int SeasonLevel { get; set; } = 1;
}

public class FortniteVariantMeta {
    [K("c")] public required string Channel { get; init; }
    [K("v")] public required string VariantName { get; init; }
    [K("dE")] public int DE { get; init; } = 0;
}

public class FortniteVariantContainerMeta {
    [K("i")] public required FortniteVariantMeta Variant { get; init; }
}

public class FortniteAthenaCosmeticLoadoutVariantsMeta {
    [K("vL")] public Dictionary<string, FortniteVariantContainerMeta> VariantLoadout { get; init; } = new();
    [K("fT")] public bool FT { get; set; } = false;
}

public class FortniteCosmeticStatMeta {
    [K("statName")] public string StatName { get; init; } = "";
    [K("statValue")] public decimal StatValue { get; set; } = 0;
}

public class FortniteAthenaCosmeticLoadoutMeta {
    [K("characterDef")] public string CharacterDef { get; set; } = "";
    [K("characterEKey")] public string CharacterEKey { get; set; } = "";
    [K("backpackDef")] public string BackpackDef { get; set; } = "";
    [K("backpackEKey")] public string BackpackEKey { get; set; } = "";
    [K("pickaxeDef")] public string PickaxeDef { get; set; } = "";
    [K("pickaxeEKey")] public string PickaxeEKey { get; set; } = "";
    [K("contrailDef")] public string ContrailDef { get; set; } = "";
    [K("contrailEKey")] public string ContrailEKey { get; set; } = "";
    [K("scratchpad")] public List<object> Scratchpad { get; set; } = new();
    [K("cosmeticStats")] public List<FortniteCosmeticStatMeta> CosmeticStats { get; init; } = new() {
        new() { StatName = "HabaneroProgression" },
        new() { StatName = "TotalVictoryCrowns" },
        new() { StatName = "TotalRoyalRoyales" },
        new() { StatName = "HasCrown" }
    };
}

public class FortniteBattlePassInfoMeta {
    [K("bHasPurchasedPass")] public bool HasPurchasedPass { get; set; } = false;
    [K("passLevel")] public int PassLevel { get; set; } = 1;
    [K("selfBoostXp")] public int SelfBoostXp { get; set; } = 0;
    [K("friendBoostXp")] public int FriendBoostXp { get; set; } = 0;
}

public class FortniteCampaignHeroMeta {
    [K("heroItemInstanceId")] public string HeroItemInstanceId { get; set; } = "";
    [K("heroType")] public string HeroType { get; set; } = "";
}

public class FortniteCampaignInfoMeta {
    [K("matchmakingLevel")] public int MatchmakingLevel { get; set; } = 0;
    [K("zoneInstanceId")] public string ZoneInstanceId { get; set; } = "";
    [K("homeBaseVersion")] public int HomeBaseVersion { get; set; } = 1;
}

public class FortniteFortCommonMatchmakingDataMeta {
    [K("request")] public _Request Request { get; init; } = new();
    [K("response")] public string Response { get; set; } = "NONE";
    [K("version")] public int Version { get; set; } = 0;

    public class _Request {
        [K("linkId")] public _LinkId LinkId { get; init; } = new();
        [K("matchmakingTransaction")] public string MatchmakingTransaction { get; set; } = "NotReady";
        [K("requester")] public string Requester { get; set; } = "INVALID";
        [K("version")] public int Version { get; set; } = 0;

        public class _LinkId {
            [K("mnemonic")] public string Mnemonic { get; init; } = "";
            [K("version")] public int Version { get; set; } = -1;
        }
    }
}

public class FortniteFortMatchmakingMemberDataMeta {
    [K("request")] public _Request Request { get; init; } = new();
    [K("response")] public string Response { get; set; } = "NONE";
    [K("version")] public int Version { get; set; } = 0;

    public class _Request {
        [K("members")] public List<_Member> Members { get; init; } = new();
        [K("requester")] public string Requester { get; set; } = "INVALID";
        [K("version")] public int Version { get; set; } = 0;

        public class _Member {
            [K("player")] public string Player { get; init; } = "";
            [K("readiness")] public string Readiness { get; set; } = "NotReady";
            [K("currentGameId")] public _CurrentGameId CurrentGameId { get; init; } = new();
            [K("currentGameType")] public string CurrentGameType { get; set; } = "UNDEFINED";
            [K("currentGameSessionId")] public string CurrentGameSessionId { get; set; } = "";
            [K("version")] public int Version { get; set; } = 101;

            public class _CurrentGameId {
                [K("mnemonic")] public string Mnemonic { get; init; } = "";
                [K("version")] public int Version { get; set; } = -1;
            }
        }
    }
}

public class FortniteFrontEndMapMarkerMeta {
    [K("markerLocation")] public _MarkerLocation MarkerLocation { get; init; } = new();
    [K("bIsSet")] public bool IsSet { get; set; } = false;

    public class _MarkerLocation {
        [K("x")] public decimal X { get; init; } = 0; // int ?
        [K("y")] public decimal Y { get; init; } = 0;
    }
}

public class FortniteFrontendEmoteMeta {
    [K("emoteItemDef")] public string EmoteItemDef { get; set; } = "None";
    [K("emoteEKey")] public string EmoteEKey { get; set; } = "";
    [K("emoteSection")] public int EmoteSection { get; set; } = -1;
}

public class FortniteJoinInProgressDataMeta {
    [K("request")] public _Request Request { get; init; } = new();
    [K("responses")] public List<string> Responses { get; set; } = new();

    public class _Request {
        [K("target")] public string Target { get; set; } = "INVALID";
        [K("time")] public int Time { get; set; } = 0; // int ?
    }
}

public class FortniteLobbyStateMeta {
    [K("inGameReadyCheckStatus")] public string InGameReadyCheckStatus { get; set; } = "None";
    [K("gameReadiness")] public string GameReadiness { get; set; } = "NotReady";
    [K("readyInputType")] public string ReadyInputType { get; set; } = "Count";
    [K("currentInputType")] public string CurrentInputType { get; set; } = "MouseAndKeyboard";
    [K("hiddenMatchmakingDelayMax")] public int HiddenMatchmakingDelayMax { get; set; } = 0;
    [K("hasPreloadedAthena")] public bool HasPreloadedAthena { get; set; } = false;
}

public class FortniteMemberSquadAssignmentRequestMeta {
    [K("startingAbsoluteIdx")] public int StartingAbsoluteIdx { get; set; } = -1;
    [K("targetAbsoluteIdx")] public int TargetAbsoluteIdx { get; set; } = -1;
    [K("swapTargetMemberId")] public string SwapTargetMemberId { get; set; } = "INVALID";
    [K("version")] public int Version { get; set; } = 0;
}

public class FortnitePackedStateMeta {
    [K("subGame")] public string SubGame { get; set; } = "Athena";
    [K("location")] public string Location { get; set; } = "PreLobby";
    [K("gameMode")] public string GameMode { get; set; } = "None";
    [K("voiceChatStatus")] public string VoiceChatStatus { get; set; } = "PartyVoice";
    [K("hasCompletedSTWTutorial")] public bool HasCompletedSTWTutorial { get; set; } = false;
    [K("hasPurchasedSTW")] public bool HasPurchasedSTW { get; set; } = false;
    [K("platformSupportsSTW")] public bool PlatformSupportsSTW { get; set; } = true;
    [K("bReturnToLobbyAndReadyUp")] public bool ReturnToLobbyAndReadyUp { get; set; } = false;
    [K("bHideReadyUp")] public bool HideReadyUp { get; set; } = false;
    [K("bDownloadOnDemandActive")] public bool DownloadOnDemandActive { get; set; } = false;
    [K("bIsPartyLFG")] public bool IsPartyLFG { get; set; } = false;
    [K("bShouldRecordPartyChannel")] public bool ShouldRecordPartyChannel { get; set; } = false;
}

public class FortnitePlatformDataMeta {
    [K("platform")] public _Platform Platform { get; init; } = new();
    [K("uniqueId")] public string UniqueId { get; set; } = "INVALID";
    [K("sessionId")] public string SessionId { get; set; } = "";

    public class _Platform {
        [K("platformDescription")] public _PlatformDescription PlatformDescription { get; init; } = new();

        public class _PlatformDescription {
            [K("name")] public string Name { get; set; } = "";
            [K("platformType")] public string PlatformType { get; set; } = "DESKTOP";
            [K("onlineSubsystem")] public string OnlineSubsystem { get; set; } = "None";
            [K("sessionType")] public string SessionType { get; set; } = "";
            [K("externalAccountType")] public string ExternalAccountType { get; set; } = "";
            [K("crossplayPool")] public string CrossplayPool { get; set; } = "DESKTOP";
        }
    }
}

public class FortniteSharedQuestsMeta {
    [K("bcktMap")] public object BcktMap { get; init; } = new();
    [K("pndQst")] public string PndQst { get; init; } = "";
}

public class FortniteSpectateInfoMeta {
    [K("gameSessionId")] public string GameSessionId { get; set; } = "";
    [K("gameSessionKey")] public string GameSessionKey { get; set; } = "";
}

public class FortnitePartyMemberMeta : MetaDict {
    public FortnitePartyMemberMeta() : base() => Initialize();
    public FortnitePartyMemberMeta(MetaDict meta) : base(meta) => Initialize();

    private void Initialize() {
        ArbitraryCustomDataStore = new();
        AthenaBannerInfo = new();
        AthenaCosmeticLoadoutVariants = new();
        AthenaCosmeticLoadout = new();
        BattlePassInfo = new();
        IsPartyUsingPartySignal = false;
        CampaignHero = new();
        CampaignInfo = new();
        CrossplayPreference = "OptedIn";
        DownloadOnDemandProgress = 0;
        FeatDefinition = "None";
        FortCommonCoreMatchmakingData = new();
        FortMatchmakingMemberData = new();
        FrontEndMapMarker = new();
        FrontendEmote = new();
        JoinInProgressData = new();
        JoinMethod = "Creation";
        LobbyState = new();
        MemberSquadAssignmentRequest = new();
        NumAthenaPlayersLeft = 0;
        PackedState = new();
        PlatformData = new();
        SharedQuests = new();
        SpectateInfo = new();
        UtcTimeStartedMatchAthena = "0001-01-01T00:00:00.000Z";
        VoiceChatEnabled = true;
    }

    private bool GetBool(string key, string? prefix = "Default") => bool.Parse(this.GetValueOrDefault(prefix is not null ? $"{prefix}:{key}_b" : $"{key}_b") ?? "false");
    private void SetBool(string key, bool value, string? prefix = "Default") => this[prefix is not null ? $"{prefix}:{key}_b" : $"{key}_b"] = value.ToString().ToLower();

    private string GetString(string key, string? prefix = "Default") => this.GetValueOrDefault(prefix is not null ? $"{prefix}:{key}_s" : $"{key}_s") ?? string.Empty;
    private void SetString(string key, string value, string? prefix = "Default") => this[prefix is not null ? $"{prefix}:{key}_s" : $"{key}_s"] = value;

    private decimal GetDecimal(string key, string? prefix = "Default") => decimal.Parse(this.GetValueOrDefault(prefix is not null ? $"{prefix}:{key}_d" : $"{key}_d") ?? "0");
    private void SetDecimal(string key, decimal value, string? prefix = "Default") => this[prefix is not null ? $"{prefix}:{key}_d" : $"{key}_d"] = value.ToString();

    private uint GetUint(string key, string? prefix = "Default") => uint.Parse(this.GetValueOrDefault(prefix is not null ? $"{prefix}:{key}_u" : $"{key}_U") ?? "0");
    private void SetUint(string key, uint value, string? prefix = "Default") => this[prefix is not null ? $"{prefix}:{key}_u" : $"{key}_U"] = value.ToString();

    private T GetObject<T>(string key, string? prefix = "Default") where T : class, new() => JsonSerializer.Deserialize<Dictionary<string, T>>(this.GetValueOrDefault(prefix is not null ? $"{prefix}:{key}" : $"{key}_j") ?? string.Empty)?.GetValueOrDefault(key) ?? new();
    private void SetObject<T>(string key, T value, string? prefix = "Default") where T : class, new() => this[prefix is not null ? $"{prefix}:{key}" : $"{key}_j"] = JsonSerializer.Serialize(new Dictionary<string, T>() { { key, value } });

    public List<object> ArbitraryCustomDataStore { get => GetObject<List<object>>(nameof(ArbitraryCustomDataStore)); set => SetObject(nameof(ArbitraryCustomDataStore), value); }
    public FortniteAthenaBannerInfoMeta AthenaBannerInfo { get => GetObject<FortniteAthenaBannerInfoMeta>(nameof(AthenaBannerInfo)); set => SetObject(nameof(AthenaBannerInfo), value); }
    public FortniteAthenaCosmeticLoadoutVariantsMeta AthenaCosmeticLoadoutVariants { get => GetObject<FortniteAthenaCosmeticLoadoutVariantsMeta>(nameof(AthenaCosmeticLoadoutVariants)); set => SetObject(nameof(AthenaCosmeticLoadoutVariants), value); }
    public FortniteAthenaCosmeticLoadoutMeta AthenaCosmeticLoadout { get => GetObject<FortniteAthenaCosmeticLoadoutMeta>(nameof(AthenaCosmeticLoadout)); set => SetObject(nameof(AthenaCosmeticLoadout), value); }
    public FortniteBattlePassInfoMeta BattlePassInfo { get => GetObject<FortniteBattlePassInfoMeta>(nameof(BattlePassInfo)); set => SetObject(nameof(BattlePassInfo), value); }
    public bool IsPartyUsingPartySignal { get => GetBool(nameof(IsPartyUsingPartySignal)); set => SetBool(nameof(IsPartyUsingPartySignal), value); }
    public FortniteCampaignHeroMeta CampaignHero { get => GetObject<FortniteCampaignHeroMeta>(nameof(CampaignHero)); set => SetObject(nameof(CampaignHero), value); }
    public FortniteCampaignInfoMeta CampaignInfo { get => GetObject<FortniteCampaignInfoMeta>(nameof(CampaignInfo)); set => SetObject(nameof(CampaignInfo), value); }
    public string CrossplayPreference { get => GetString(nameof(CrossplayPreference)); set => SetString(nameof(CrossplayPreference), value); }
    public decimal DownloadOnDemandProgress { get => GetDecimal(nameof(DownloadOnDemandProgress)); set => SetDecimal(nameof(DownloadOnDemandProgress), value); }
    public string FeatDefinition { get => GetString(nameof(FeatDefinition)); set => SetString(nameof(FeatDefinition), value); }
    public FortniteFortCommonMatchmakingDataMeta FortCommonCoreMatchmakingData { get => GetObject<FortniteFortCommonMatchmakingDataMeta>(nameof(FortCommonCoreMatchmakingData)); set => SetObject(nameof(FortCommonCoreMatchmakingData), value); }
    public FortniteFortMatchmakingMemberDataMeta FortMatchmakingMemberData { get => GetObject<FortniteFortMatchmakingMemberDataMeta>(nameof(FortMatchmakingMemberData)); set => SetObject(nameof(FortMatchmakingMemberData), value); }
    public FortniteFrontEndMapMarkerMeta FrontEndMapMarker { get => GetObject<FortniteFrontEndMapMarkerMeta>(nameof(FrontEndMapMarker)); set => SetObject(nameof(FrontEndMapMarker), value); }
    public FortniteFrontendEmoteMeta FrontendEmote { get => GetObject<FortniteFrontendEmoteMeta>(nameof(FrontendEmote)); set => SetObject(nameof(FrontendEmote), value); }
    public FortniteJoinInProgressDataMeta JoinInProgressData { get => GetObject<FortniteJoinInProgressDataMeta>(nameof(JoinInProgressData)); set => SetObject(nameof(JoinInProgressData), value); }
    public string JoinMethod { get => GetString(nameof(JoinMethod)); set => SetString(nameof(JoinMethod), value); }
    public FortniteLobbyStateMeta LobbyState { get => GetObject<FortniteLobbyStateMeta>(nameof(LobbyState)); set => SetObject(nameof(LobbyState), value); }
    public FortniteMemberSquadAssignmentRequestMeta MemberSquadAssignmentRequest { get => GetObject<FortniteMemberSquadAssignmentRequestMeta>(nameof(MemberSquadAssignmentRequest)); set => SetObject(nameof(MemberSquadAssignmentRequest), value); }
    public uint NumAthenaPlayersLeft { get => GetUint(nameof(NumAthenaPlayersLeft)); set => SetUint(nameof(NumAthenaPlayersLeft), value); }
    public FortnitePackedStateMeta PackedState { get => GetObject<FortnitePackedStateMeta>(nameof(PackedState)); set => SetObject(nameof(PackedState), value); }
    public FortnitePlatformDataMeta PlatformData { get => GetObject<FortnitePlatformDataMeta>(nameof(PlatformData)); set => SetObject(nameof(PlatformData), value); }
    public FortniteSharedQuestsMeta SharedQuests { get => GetObject<FortniteSharedQuestsMeta>(nameof(SharedQuests)); set => SetObject(nameof(SharedQuests), value); }
    public FortniteSpectateInfoMeta SpectateInfo { get => GetObject<FortniteSpectateInfoMeta>(nameof(SpectateInfo)); set => SetObject(nameof(SpectateInfo), value); }
    public string UtcTimeStartedMatchAthena { get => GetString(nameof(UtcTimeStartedMatchAthena)); set => SetString(nameof(UtcTimeStartedMatchAthena), value); }
    public bool VoiceChatEnabled { get => GetBool(nameof(VoiceChatEnabled)); set => SetBool(nameof(VoiceChatEnabled), value); }

    public string Platform { get => PlatformData.Platform.PlatformDescription.Name; set => PlatformData.Platform.PlatformDescription.Name = value; }

    public string Outfit { get => FortniteUtils.GetOutfitId(AthenaCosmeticLoadout.CharacterDef); set => AthenaCosmeticLoadout.CharacterDef = value.StartsWith("/") ? value : $"/Game/Athena/Items/Cosmetics/Characters/{value}.{value}"; }
    public string Backpack { get => FortniteUtils.GetBackpackId(AthenaCosmeticLoadout.BackpackDef); set => AthenaCosmeticLoadout.BackpackDef = value.StartsWith("/") ? value : $"/Game/Athena/Items/Cosmetics/Backpacks/{value}.{value}"; }
    public string Pickaxe { get => FortniteUtils.GetPickaxeId(AthenaCosmeticLoadout.PickaxeDef); set => AthenaCosmeticLoadout.PickaxeDef = value.StartsWith("/") ? value : $"/Game/Athena/Items/Cosmetics/Pickaxes/{value}.{value}"; }
    public string Emote { get => FrontendEmote.EmoteItemDef; set => FrontendEmote.EmoteItemDef = value; }

    // todo implement cosmetic variant accessors
}

#endregion

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
        ConnectedAt = FortniteUtils.ConvertToDateTime(data.ConnectedAt);
        UpdatedAt = FortniteUtils.ConvertToDateTime(data.UpdatedAt);
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
        JoinedAt = FortniteUtils.ConvertToDateTime(data.JoinedAt);
        UpdatedAt = FortniteUtils.ConvertToDateTime(data.UpdatedAt);
        Meta = new(data.Meta);
        Revision = data.Revision;
    }

    public FortnitePartyMember(FortniteParty party, FortnitePartyMemberJoinedData data) {
        Party = party;
        AccountId = data.AccountId;
        DisplayName = data.AccountDn;
        Connections = new() { new(data.Connection) };
        Role = EFortnitePartyMemberRole.Member; // How leader joins?
        JoinedAt = FortniteUtils.ConvertToDateTime(data.JoinedAt);
        UpdatedAt = FortniteUtils.ConvertToDateTime(data.UpdatedAt);
        Meta = new();
        Revision = data.Revision;
    }

    public bool IsLeader => Role == EFortnitePartyMemberRole.Captain;
}

public class FortniteClientPartyMember : FortnitePartyMember {
    public FortniteClientPartyMember(FortniteParty party, FortnitePartyMemberData data) : base(party, data) => Initialize();
    public FortniteClientPartyMember(FortniteParty party, FortnitePartyMemberJoinedData data) : base(party, data) => Initialize();

    private void Initialize() {
        Meta.Add("urn:epic:member:dn_s", Party.Client.User.DisplayName);
    }

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
        SentAt = FortniteUtils.ConvertToDateTime(data.SentAt);
        UpdatedAt = FortniteUtils.ConvertToDateTime(data.UpdatedAt);
        ExpiresAt = FortniteUtils.ConvertToDateTime(data.ExpiresAt);
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
        CreatedAt = FortniteUtils.ConvertToDateTime(data.CreatedAt);
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
        SentAt = FortniteUtils.ConvertToDateTime(data.SentAt);
        ExpiresAt = FortniteUtils.ConvertToDateTime(data.ExpiresAt);
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
        SentAt = FortniteUtils.ConvertToDateTime(confirmation.Sent);
        JoinedAt = FortniteUtils.ConvertToDateTime(confirmation.JoinedAt);
        Connection = new(confirmation.Connection);
    }
}