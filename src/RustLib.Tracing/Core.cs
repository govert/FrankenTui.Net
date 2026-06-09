// Port of the Rust `tracing_core` types that FrankenTUI depends on.
// Upstream: https://github.com/tokio-rs/tracing (tracing-core crate)
// Closure: Level, Metadata, Event, Field, Visit, Subscriber

namespace RustLib.Tracing;

// ============================================================================
// Level — mirrors tracing_core::Level
// ============================================================================

/// <summary>Log severity levels (mirrors tracing_core::Level).</summary>
public enum Level
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warn = 3,
    Error = 4,
}

// ============================================================================
// Metadata — mirrors tracing_core::Metadata
// ============================================================================

/// <summary>
/// Metadata describing a span or event (mirrors tracing_core::Metadata).
/// FrankenTUI uses: level, target, file, line, module_path.
/// </summary>
public sealed class Metadata
{
    /// <summary>The verbosity level.</summary>
    public Level Level { get; }

    /// <summary>The tracing target (usually module path).</summary>
    public string Target { get; }

    /// <summary>Source file name, if available.</summary>
    public string? File { get; }

    /// <summary>Source line number, if available.</summary>
    public int? Line { get; }

    /// <summary>Module path, if available.</summary>
    public string? ModulePath { get; }

    public Metadata(Level level, string target, string? file = null, int? line = null, string? modulePath = null)
    {
        Level = level;
        Target = target;
        File = file;
        Line = line;
        ModulePath = modulePath;
    }

    /// <summary>Returns the metadata's level (mirrors tracing_core::Metadata::level()).</summary>
    public Level GetLevel() => Level;

    /// <summary>Returns the metadata's target (mirrors tracing_core::Metadata::target()).</summary>
    public string GetTarget() => Target;
}

// ============================================================================
// FieldValue — mirrors tracing_core::field::Field + its value
// ============================================================================

/// <summary>
/// A field name-value pair extracted from an event (mirrors tracing::field::Field + Visit).
/// </summary>
public readonly record struct FieldValue(string Name, string Value);

// ============================================================================
// Event — mirrors tracing_core::Event
// ============================================================================

/// <summary>
/// A tracing event (mirrors tracing_core::Event).
/// FrankenTUI uses: metadata(), record() via a visitor.
/// </summary>
public sealed class Event
{
    private readonly Metadata _metadata;
    private readonly IReadOnlyList<FieldValue> _fields;

    public Event(Metadata metadata, IReadOnlyList<FieldValue> fields)
    {
        _metadata = metadata;
        _fields = fields;
    }

    /// <summary>Returns the event's metadata (mirrors Event::metadata()).</summary>
    public Metadata GetMetadata() => _metadata;

    /// <summary>
    /// Visit all fields in the event (mirrors Event::record(&mut visitor)).
    /// The visitor receives each field name and value.
    /// </summary>
    public void Record(IVisit visitor)
    {
        foreach (var field in _fields)
        {
            visitor.RecordStr(field.Name, field.Value);
        }
    }
}

// ============================================================================
// IVisit — mirrors tracing_core::field::Visit trait
// ============================================================================

/// <summary>
/// Trait for visiting fields on an event (mirrors tracing_core::field::Visit).
/// FrankenTUI only uses record_str; other record_* methods are omitted.
/// </summary>
public interface IVisit
{
    /// <summary>Visit a string field (mirrors Visit::record_str()).</summary>
    void RecordStr(string name, string value);
}

// ============================================================================
// ISubscriber — mirrors tracing_core::Subscriber trait
// ============================================================================

/// <summary>
/// Trait representing a subscriber to tracing events (mirrors tracing_core::Subscriber).
/// </summary>
public interface ISubscriber
{
    /// <summary>Called when a new event is recorded.</summary>
    void OnEvent(Event @event);
}
