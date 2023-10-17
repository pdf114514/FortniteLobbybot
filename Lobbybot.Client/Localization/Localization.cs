using System.Reflection;
using System.Text.Json;

namespace Lobbybot.Client;

public class Localization {
    private readonly Dictionary<string, string> _Localizations = new();
    public string Language { get; private set; } = "en";

    public Localization(string language = "en") => SwitchLanguage(language);

    public void SwitchLanguage(string language = "en") {
        Language = language;
        var assembly = Assembly.GetExecutingAssembly();
        // <Assembly Name>.<Directory Name>.<File Name>
        using var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Localization.{language}.json") ?? throw new Exception($"Language {language} not found");
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? throw new Exception($"Language {language} not found");
        _Localizations.Clear();
        foreach (var (key, value) in strings) _Localizations.Add(key, value);
    }

    public string this[string key] => _Localizations.GetValueOrDefault(key, $"MISSING: {key}");

    public static string[] SupportedLanguages { get {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceNames().Where(x => x.StartsWith($"{assembly.GetName().Name}.Localization") && x.EndsWith(".json")).Select(x => x.Split(".")[^2]).ToArray();
    } }
}