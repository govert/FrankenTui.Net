namespace FrankenTui.Style;

public sealed class StyleSheet
{
    private readonly Dictionary<string, UiStyle> _styles = new(StringComparer.OrdinalIgnoreCase);

    public StyleSheet Add(string name, UiStyle style)
    {
        _styles[name] = style;
        return this;
    }

    public UiStyle Resolve(string name, UiStyle? fallback = null) =>
        _styles.TryGetValue(name, out var style) ? style : fallback ?? UiStyle.Default;

    public IReadOnlyDictionary<string, UiStyle> Entries => _styles;
}
