using Zeymera.Scoreboard.Client.Localization;

namespace Zeymera.Scoreboard.Client.Services;

public class LocalizationService
{
    public string Language { get; private set; } = Translations.DefaultLanguage;

    public event Action? LanguageChanged;

    public void SetLanguage(string? language)
    {
        var normalized = string.IsNullOrWhiteSpace(language)
            ? Translations.DefaultLanguage
            : language.Trim().ToLowerInvariant();

        if (!Translations.Values.ContainsKey(normalized))
        {
            normalized = Translations.DefaultLanguage;
        }

        if (normalized != Language)
        {
            Language = normalized;
            LanguageChanged?.Invoke();
        }
    }

    public string T(string key) => Translations.Get(Language, key);
}
