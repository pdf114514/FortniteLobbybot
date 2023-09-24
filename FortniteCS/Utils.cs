namespace FortniteCS;

public static class Utils {
    public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public static DateTime ConvertToDateTime(this string str) => DateTime.ParseExact(str, DateTimeFormat, null);
}