using System.Globalization;

namespace FrankenTui.Runtime;

/// <summary>Retry policy with exponential backoff. Matches upstream retry.</summary>
public sealed class RetryPolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;
    private readonly double _backoffMultiplier;
    private readonly TimeSpan _maxDelay;

    public RetryPolicy(int maxRetries = 3, double initialDelayMs = 100, double backoffMultiplier = 2.0, double maxDelayMs = 10000)
    {
        _maxRetries = maxRetries;
        _initialDelay = TimeSpan.FromMilliseconds(initialDelayMs);
        _backoffMultiplier = backoffMultiplier;
        _maxDelay = TimeSpan.FromMilliseconds(maxDelayMs);
    }

    public async Task<T?> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
    {
        var delay = _initialDelay;
        for (var i = 0; i <= _maxRetries; i++)
        {
            try { return await action(ct); }
            catch when (i < _maxRetries)
            {
                await Task.Delay(delay, ct);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * _backoffMultiplier, _maxDelay.TotalMilliseconds));
            }
        }
        return default;
    }
}

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

/// <summary>Debug trace support. Matches upstream debug_trace.</summary>
public static class DebugTrace
{
    public static bool Enabled { get; set; }
    private static readonly List<string> _entries = [];

    public static void Trace(string message)
    {
        if (!Enabled) return;
        _entries.Add($"[{DateTimeOffset.UtcNow:O}] {message}");
        while (_entries.Count > 1000) _entries.RemoveAt(0);
    }

    public static IReadOnlyList<string> Entries => _entries;
    public static void Clear() => _entries.Clear();
}

/// <summary>Log sink for structured logging. Matches upstream log_sink.</summary>
public sealed class LogSink
{
    private readonly List<(DateTimeOffset Time, string Level, string Message)> _entries = [];
    public int MaxEntries { get; init; } = 1000;

    public void Write(string level, string message)
    {
        _entries.Add((DateTimeOffset.UtcNow, level, message));
        while (_entries.Count > MaxEntries) _entries.RemoveAt(0);
    }

    public IEnumerable<string> Recent(int count = 100) =>
        _entries.TakeLast(count).Select(e => $"[{e.Time:HH:mm:ss}] {e.Level}: {e.Message}");
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

/// <summary>Policy registry for named configurations. Matches upstream policy_registry.</summary>
public sealed class PolicyRegistry
{
    private readonly Dictionary<string, object> _policies = [];

    public void Register<T>(string name, T policy) where T : class => _policies[name] = policy;
    public T? Get<T>(string name) where T : class => _policies.TryGetValue(name, out var p) ? p as T : null;
    public IEnumerable<string> Names => _policies.Keys;
}

/// <summary>Schema compatibility checker. Matches upstream schema_compat.</summary>
public sealed class SchemaCompat
{
    public static bool IsCompatible(int currentVersion, int storedVersion, int minSupportedVersion) =>
        storedVersion >= minSupportedVersion && storedVersion <= currentVersion;

    public static string MigrationRequired(int from, int to) =>
        from < to ? $"Migration required: v{from} → v{to}" : "Up to date";
}
