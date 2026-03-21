namespace FrankenTui.Runtime;

public sealed record AppCommand<TMessage>(IReadOnlyList<TMessage> Messages, string EffectKind = "command")
{
    public static AppCommand<TMessage> None { get; } = new([]);

    public static AppCommand<TMessage> Emit(TMessage message, string effectKind = "command") => new([message], effectKind);

    public static AppCommand<TMessage> Batch(string effectKind = "command", params TMessage[] messages) => new(messages, effectKind);

    public AppCommand<TMessage> WithEffectKind(string effectKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(effectKind);
        return this with { EffectKind = effectKind };
    }
}
