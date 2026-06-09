using System.Globalization;

namespace FrankenTui.Runtime;

/// <summary>Cancellation token source wrapper. Matches upstream cancellation.</summary>
public sealed class CancellationScope : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    public CancellationToken Token => _cts.Token;
    public void Cancel() => _cts.Cancel();
    public void CancelAfter(TimeSpan delay) => _cts.CancelAfter(delay);
    public void Dispose() => _cts.Dispose();
}

/// <summary>Locale support. Matches upstream locale.</summary>
public static class RuntimeLocale
{
    public static CultureInfo Current { get; set; } = CultureInfo.InvariantCulture;
    public static string FormatNumber(double value, int decimals = 2) =>
        value.ToString($"F{decimals}", Current);
    public static string FormatPercent(double value, int decimals = 1) =>
        (value * 100).ToString($"F{decimals}", Current) + "%";
}

/// <summary>Subscription system for event-driven updates. Matches upstream subscription.</summary>
public sealed class EventSubscription<T>
{
    private readonly List<Func<T, CancellationToken, Task>> _handlers = [];

    public IDisposable Subscribe(Func<T, CancellationToken, Task> handler)
    {
        _handlers.Add(handler);
        return new Unsubscriber(() => _handlers.Remove(handler));
    }

    public async Task PublishAsync(T value, CancellationToken ct = default)
    {
        foreach (var handler in _handlers.ToArray())
            await handler(value, ct);
    }

    private sealed class Unsubscriber(Action action) : IDisposable { public void Dispose() => action(); }
}
