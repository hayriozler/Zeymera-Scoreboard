using System.Text.Json;

namespace Zeymera.Scoreboard.Client.Services;

public class LocalizationService
{
    public static readonly Dictionary<string, Dictionary<string, string>> Values = Load();
    public const string DefaultLanguage = "tr";
    public string Language { get; private set; } = DefaultLanguage;

    public event Action? LanguageChanged;

    public void SetLanguage(string? language)
    {
        var normalized = string.IsNullOrWhiteSpace(language)
            ? DefaultLanguage
            : language.Trim().ToLowerInvariant();

        if (!Values.ContainsKey(normalized))
        {
            normalized = DefaultLanguage;
        }

        if (normalized != Language)
        {
            Language = normalized;
            LanguageChanged?.Invoke();
        }
    }

    public string T(string key) => Get(Language, key);



    private static Dictionary<string, Dictionary<string, string>> Load()
    {
        var result = new Dictionary<string, Dictionary<string, string>>();
        string rootpath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Localization");
        foreach (var file in Directory.GetFiles(rootpath, "*.json"))
        {
            var language = Path.GetFileNameWithoutExtension(file);
            var json = File.ReadAllText(file);
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
            result[language] = values;
        }
        return result;
    }

    public static string Get(string language, string key)
    {
        if (Values.TryGetValue(language, out var dict) && dict.TryGetValue(key, out var value))
        {
            return value;
        }

        return Values[DefaultLanguage].GetValueOrDefault(key, key);
    }
}
