namespace FortniteCS;

[Flags]
public enum EPermissions {
    None = 0,
    Create = 1,
    Read = 2,
    Update = 4,
    Delete = 8,
    Deny = 16
}