namespace FrankenTui.Runtime;

public sealed record Subscription<TMessage>(string Key, Func<IEnumerable<TMessage>> CreateMessages, string EffectKind = "subscription")
{
    public IEnumerable<TMessage> Invoke() => CreateMessages();

    public Subscription<TMessage> WithEffectKind(string effectKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(effectKind);
        return this with { EffectKind = effectKind };
    }
}
