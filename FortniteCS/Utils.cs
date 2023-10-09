global using K = System.Text.Json.Serialization.JsonPropertyNameAttribute;
global using MetaDict = System.Collections.Generic.Dictionary<string, string>;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FortniteCS;

public static class FortniteUtils {
    public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public static DateTime ConvertToDateTime(string str) => DateTime.ParseExact(str, DateTimeFormat, null);
    public static string ConvertToString(DateTime dt) => dt.ToString(DateTimeFormat);

    public static T? JsonDeserialize<T>(string json) => JsonSerializer.Deserialize<T>(json);
    public static string JsonSerialize<T>(T obj) => JsonSerializer.Serialize(obj, new JsonSerializerOptions() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

    public static readonly Regex OutfitPattern = new Regex(@"\/Game\/Athena\/Items\/Cosmetics\/Characters\/(.*)\.(.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public static string GetOutfitId(string path) => OutfitPattern.Match(path).Groups[1].Value;
    public static readonly Regex BackpackPattern = new Regex(@"\/Game\/Athena\/Items\/Cosmetics\/Backpacks\/(.*)\.(.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public static string GetBackpackId(string path) => BackpackPattern.Match(path).Groups[1].Value;
    public static readonly Regex PickaxePattern = new Regex(@"\/Game\/Athena\/Items\/Cosmetics\/Pickaxes\/(.*)\.(.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public static string GetPickaxeId(string path) => PickaxePattern.Match(path).Groups[1].Value;
}