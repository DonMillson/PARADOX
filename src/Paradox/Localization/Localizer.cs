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

    // Exposed for automated coverage checks: unlike Get(), no language
    // fallback hides missing Polish or English translations.
    public bool Has(Language language, string key) =>
        _strings.TryGetValue(language, out var table) &&
        table.TryGetValue(key, out var value) &&
        !string.IsNullOrWhiteSpace(value);

    public string? GetExact(Language language, string key) =>
        _strings.TryGetValue(language, out var table) &&
        table.TryGetValue(key, out var value) ? value : null;

    public IReadOnlyCollection<string> Keys(Language language) =>
        _strings.TryGetValue(language, out var table)
            ? table.Keys.ToArray()
            : Array.Empty<string>();

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
