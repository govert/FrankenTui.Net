namespace FrankenTui.I18n;

public sealed class LocalizationCatalog
{
    private readonly Dictionary<string, string> _strings = new(StringComparer.OrdinalIgnoreCase);

    public LocalizationCatalog Add(string key, string value)
    {
        _strings[key] = value;
        return this;
    }

    public string Resolve(LocalizedString value) =>
        _strings.TryGetValue(value.Key, out var resolved) ? resolved : value.DefaultValue;
}
