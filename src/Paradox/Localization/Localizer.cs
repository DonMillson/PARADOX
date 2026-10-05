namespace Paradox.Localization;

public sealed class Localizer
{
    private readonly Dictionary<Language, Dictionary<string, string>> _strings = new();

    public Language CurrentLanguage { get; set; } = Language.English;

    public void Add(Language language, string key, string value)
    {
        if (!_strings.TryGetValue(language, out var table))
            _strings[language] = table = new Dictionary<string, string>();

        table[key] = value;
    }

    public string Get(string key)
    {
        if (_strings.TryGetValue(CurrentLanguage, out var current) &&
            current.TryGetValue(key, out var translated))
            return translated;

        if (_strings.TryGetValue(Language.English, out var fallback) &&
            fallback.TryGetValue(key, out translated))
            return translated;

        return key;
    }
}
