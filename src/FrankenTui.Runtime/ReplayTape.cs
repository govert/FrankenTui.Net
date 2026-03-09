namespace FrankenTui.Runtime;

public sealed class ReplayTape<TMessage>
{
    private readonly List<ReplayEntry<TMessage>> _entries = [];

    public IReadOnlyList<ReplayEntry<TMessage>> Entries => _entries;

    public void Add(TMessage message, string frame) => _entries.Add(new ReplayEntry<TMessage>(message, frame));
}

public readonly record struct ReplayEntry<TMessage>(TMessage Message, string Frame);
