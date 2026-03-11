namespace FrankenTui.Backend;

public interface ITerminalSessionLifecycle
{
    string Name { get; }

    TerminalLifecycleState State { get; }

    ValueTask InitializeAsync(CancellationToken cancellationToken = default);

    ValueTask ConfigureSessionAsync(
        TerminalSessionConfiguration configuration,
        CancellationToken cancellationToken = default);
}
