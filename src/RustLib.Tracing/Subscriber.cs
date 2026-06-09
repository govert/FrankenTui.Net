// Port of the Rust `tracing_subscriber` types that FrankenTUI depends on.
// Upstream: https://github.com/tokio-rs/tracing (tracing-subscriber crate)
// Closure: Layer, Context, Registry, Dispatch

namespace RustLib.Tracing;

// ============================================================================
// LayerContext — mirrors tracing_subscriber::layer::Context
// ============================================================================

/// <summary>
/// Context provided to a layer during event processing
/// (mirrors tracing_subscriber::layer::Context).
/// </summary>
public readonly struct LayerContext<T>
    where T : ISubscriber
{
    /// <summary>The subscriber this layer is attached to.</summary>
    public T Subscriber { get; }

    public LayerContext(T subscriber)
    {
        Subscriber = subscriber;
    }
}

// ============================================================================
// ILayer — mirrors tracing_subscriber::Layer<S> trait
// ============================================================================

/// <summary>
/// A composable layer that processes events (mirrors tracing_subscriber::Layer<S>).
/// Layers are stacked onto a subscriber to add behavior like formatting and output.
/// </summary>
public interface ILayer<T>
    where T : ISubscriber
{
    /// <summary>
    /// Called when an event occurs (mirrors Layer::on_event()).
    /// </summary>
    void OnEvent(Event @event, ISubscriber subscriber);
}

// ============================================================================
// Registry — mirrors tracing_subscriber::Registry
// ============================================================================

/// <summary>
/// A simple subscriber that holds a stack of layers and dispatches events to them
/// in order (mirrors tracing_subscriber::Registry).
/// </summary>
public sealed class Registry : ISubscriber
{
    private readonly List<object> _layers = new();

    /// <summary>Maximum level to record. Events above this level are filtered out. Null = no filter.</summary>
    public Level? MaxLevel { get; set; }

    /// <summary>Register a layer with this registry.</summary>
    public Registry With(ILayer<Registry> layer)
    {
        _layers.Add(layer);
        return this;
    }

    /// <summary>Dispatch an event to all registered layers, respecting MaxLevel.</summary>
    public void OnEvent(Event @event)
    {
        if (MaxLevel.HasValue && @event.GetMetadata().GetLevel() < MaxLevel.Value)
            return;
        foreach (var layer in _layers)
        {
            if (layer is ILayer<Registry> l)
            {
                l.OnEvent(@event, this);
            }
        }
    }
}

// ============================================================================
// Dispatch — mirrors tracing::Dispatch (scoped subscriber management)
// ============================================================================

/// <summary>
/// Manages the currently active subscriber, mirroring tracing::Dispatch.
/// Supports scoped dispatch for thread-local subscriber overrides.
/// </summary>
public static class Dispatch
{
    private static ISubscriber? _globalSubscriber;
    private static readonly object _lock = new();

    /// <summary>Set the global subscriber. Pass null to clear (mirrors tracing::subscriber::set_global_default()).</summary>
    public static void SetGlobalDefault(ISubscriber? subscriber)
    {
        lock (_lock) { _globalSubscriber = subscriber; }
    }

    /// <summary>Get the currently active subscriber, or null if none set.</summary>
    public static ISubscriber? GetDefault()
    {
        lock (_lock) { return _globalSubscriber; }
    }

    /// <summary>
    /// Execute an action with the given subscriber as default,
    /// restoring the previous subscriber afterward
    /// (mirrors tracing::dispatcher::with_default()).
    /// </summary>
    public static void WithDefault(ISubscriber subscriber, Action action)
    {
        var previous = GetDefault();
        SetGlobalDefault(subscriber);
        try { action(); }
        finally { SetGlobalDefault(previous); }
    }

    /// <summary>
    /// Record an event through the currently active subscriber
    /// (mirrors tracing::Event::dispatch()).
    /// </summary>
    internal static void DispatchEvent(Event @event)
    {
        var subscriber = GetDefault();
        subscriber?.OnEvent(@event);
    }
}
