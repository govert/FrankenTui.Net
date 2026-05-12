namespace FrankenTui.Runtime;

public sealed record AppCommand<TMessage>(
    IReadOnlyList<TMessage> Messages,
    string EffectKind = "command",
    IReadOnlyDictionary<int, RuntimeQueueTaskSpec>? QueueSpecs = null)
{
    public static AppCommand<TMessage> None { get; } = new([]);

    public static AppCommand<TMessage> Emit(TMessage message, string effectKind = "command") => new([message], effectKind);

    public static AppCommand<TMessage> EmitQueued(
        TMessage message,
        RuntimeQueueTaskSpec queueSpec,
        string effectKind = "command") =>
        new([message], effectKind, new Dictionary<int, RuntimeQueueTaskSpec> { [0] = queueSpec });

    public static AppCommand<TMessage> Batch(string effectKind = "command", params TMessage[] messages) => new(messages, effectKind);

    public AppCommand<TMessage> WithEffectKind(string effectKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(effectKind);
        return this with { EffectKind = effectKind };
    }

    public AppCommand<TMessage> WithQueueSpec(int index, RuntimeQueueTaskSpec queueSpec)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentNullException.ThrowIfNull(queueSpec);
        var specs = QueueSpecs is null
            ? new Dictionary<int, RuntimeQueueTaskSpec>()
            : new Dictionary<int, RuntimeQueueTaskSpec>(QueueSpecs);
        specs[index] = queueSpec;
        return this with { QueueSpecs = specs };
    }
}
