namespace FrankenTui.Runtime;

public sealed record AppCommand<TMessage>(IReadOnlyList<TMessage> Messages)
{
    public static AppCommand<TMessage> None { get; } = new([]);

    public static AppCommand<TMessage> Emit(TMessage message) => new([message]);

    public static AppCommand<TMessage> Batch(params TMessage[] messages) => new(messages);
}
