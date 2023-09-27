global using K = System.Text.Json.Serialization.JsonPropertyNameAttribute;
global using MetaDict = System.Collections.Generic.Dictionary<string, string>;

namespace FortniteCS;

public static class Utils {
    public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public static DateTime ConvertToDateTime(string str) => DateTime.ParseExact(str, DateTimeFormat, null);
    public static string ConvertToString(DateTime dt) => dt.ToString(DateTimeFormat);
}