using System.Reflection;
using System.Text.Json;

namespace Zeymera.Scoreboard.Client.Localization;

public static class Translations
{
    public const string DefaultLanguage = "tr";

    public static readonly Dictionary<string, Dictionary<string, string>> Values = Load();

    private static Dictionary<string, Dictionary<string, string>> Load()
    {
        var assembly = typeof(Translations).Assembly;
        var prefix = $"{assembly.GetName().Name}.Localization.";
        var result = new Dictionary<string, Dictionary<string, string>>();

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(prefix, StringComparison.Ordinal) ||
                !resourceName.EndsWith(".json", StringComparison.Ordinal))
            {
                continue;
            }

            var language = resourceName[prefix.Length..^".json".Length];

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? new();
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
