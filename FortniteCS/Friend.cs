namespace FortniteCS;

public class FortniteFriendData {
    [K("accountId")] public required string AccountId { get; init; }
    [K("groups")] public required List<object> Groups { get; init; }
    [K("mutual")] public int? Mutual { get; init; }
    [K("alias")] public required string Alias { get; init; }
    [K("note")] public required string Note { get; init; }
    [K("favorite")] public required bool Favorite { get; init; }
    [K("created")] public required string Created { get; init; }
}

public class FriendsSummaryData {
    [K("friends")] public required List<FortniteFriendData> Friends { get; init; }
    [K("incoming")] public required List<PendingFriendData> Incoming { get; init; }
    [K("outgoing")] public required List<PendingFriendData> Outgoing { get; init; }
    // suggests
    // blocklist
    // settings
    [K("limitsReached")] public required _LimitsReached LimitsReached { get; init; }

    public class _LimitsReached {
        [K("accepted")] public required bool Accepted { get; init; }
        [K("incoming")] public required bool Incoming { get; init; }
        [K("outgoing")] public required bool Outgoing { get; init; }
    }
}

public class EPendingFriendDirection {
    public const string INCOMING = "INCOMING";
    public const string OUTGOING = "OUTGOING";
}

public class PendingFriendData {
    [K("accountId")] public required string AccountId { get; init; }
    [K("mutual")] public required int Mutual { get; init; }
    [K("created")] public required string Created { get; init; }
    [K("favorite")] public required bool Favorite { get; init; }
}

public class FortniteFriend {
    public string AccountId { get; init; }
    public string? DisplayName { get; internal set; }
    public List<object> Groups { get; init; }
    public int? Mutual { get; init; }
    public string Alias { get; init; }
    public string Note { get; init; }
    public bool Favorite { get; init; }
    public DateTime CreatedAt { get; init; }

    public FortniteFriend(FortniteFriendData data) {
        AccountId = data.AccountId;
        Groups = data.Groups;
        Mutual = data.Mutual;
        Alias = data.Alias;
        Note = data.Note;
        Favorite = data.Favorite;
        CreatedAt = FortniteUtils.ConvertToDateTime(data.Created);
    }
}

public abstract class PendingFriend {
    public string AccountId { get; init; }
    public int Mutual { get; init; }
    public abstract string Direction { get; }
    public DateTime CreatedAt { get; init; }

    public PendingFriend(PendingFriendData data) {
        AccountId = data.AccountId;
        Mutual = data.Mutual;
        CreatedAt = FortniteUtils.ConvertToDateTime(data.Created);
    }
}

public class IncomingPendingFriend : PendingFriend {
    public override string Direction => EPendingFriendDirection.INCOMING;

    public IncomingPendingFriend(PendingFriendData data) : base(data) {}
}

public class OutgoingPendingFriend : PendingFriend {
    public override string Direction => EPendingFriendDirection.OUTGOING;

    public OutgoingPendingFriend(PendingFriendData data) : base(data) {}
}