using System.Reflection;
using System.Text.Json;

namespace Lobbybot.Client;

public class ESupportedLocalizations {
    public const string EN = "en";
    public const string JA = "ja";
}

public class Localization {
    private readonly Dictionary<string, string> _Localizations = new();

    public Localization(string language = ESupportedLocalizations.EN) => SwitchLanguage(language);

    public void SwitchLanguage(string language = ESupportedLocalizations.EN) {
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
}