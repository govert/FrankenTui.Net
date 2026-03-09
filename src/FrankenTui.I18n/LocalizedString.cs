namespace FrankenTui.I18n;

public readonly record struct LocalizedString(string Key, string DefaultValue)
{
    public override string ToString() => DefaultValue;
}
