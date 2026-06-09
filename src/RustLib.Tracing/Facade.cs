// Port of the Rust `tracing` facade macros that FrankenTUI depends on.
// Upstream: https://github.com/tokio-rs/tracing (tracing crate)
// Closure: trace!, debug!, info!, warn!, error! plus Event construction helpers.
//
// These static methods create Events and dispatch them through the global
// subscriber set via Dispatch::SetGlobalDefault().

namespace RustLib.Tracing;

/// <summary>
/// Static entry points for recording tracing events
/// (mirrors the Rust tracing::info!(), tracing::warn!(), etc. macros).
/// </summary>
public static class Tracing
{
    /// <summary>Record a TRACE-level event (mirrors tracing::trace!()).</summary>
    public static void Trace(string message, string? target = null,
        IReadOnlyList<FieldValue>? fields = null,
        [System.Runtime.CompilerServices.CallerFilePath] string? file = null,
        [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
    {
        Record(Level.Trace, message, target, fields, file, line);
    }

    /// <summary>Record a DEBUG-level event (mirrors tracing::debug!()).</summary>
    public static void Debug(string message, string? target = null,
        IReadOnlyList<FieldValue>? fields = null,
        [System.Runtime.CompilerServices.CallerFilePath] string? file = null,
        [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
    {
        Record(Level.Debug, message, target, fields, file, line);
    }

    /// <summary>Record an INFO-level event (mirrors tracing::info!()).</summary>
    public static void Info(string message, string? target = null,
        IReadOnlyList<FieldValue>? fields = null,
        [System.Runtime.CompilerServices.CallerFilePath] string? file = null,
        [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
    {
        Record(Level.Info, message, target, fields, file, line);
    }

    /// <summary>Record a WARN-level event (mirrors tracing::warn!()).</summary>
    public static void Warn(string message, string? target = null,
        IReadOnlyList<FieldValue>? fields = null,
        [System.Runtime.CompilerServices.CallerFilePath] string? file = null,
        [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
    {
        Record(Level.Warn, message, target, fields, file, line);
    }

    /// <summary>Record an ERROR-level event (mirrors tracing::error!()).</summary>
    public static void Error(string message, string? target = null,
        IReadOnlyList<FieldValue>? fields = null,
        [System.Runtime.CompilerServices.CallerFilePath] string? file = null,
        [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
    {
        Record(Level.Error, message, target, fields, file, line);
    }

    private static void Record(Level level, string message, string? target,
        IReadOnlyList<FieldValue>? fields, string? file, int line)
    {
        var allFields = new List<FieldValue>();
        if (!string.IsNullOrEmpty(message))
            allFields.Add(new FieldValue("message", message));
        if (fields is not null)
            allFields.AddRange(fields);

        var metadata = new Metadata(level,
            target ?? "unknown",
            file is not null ? Path.GetFileName(file) : null,
            line > 0 ? line : null,
            target);

        var @event = new Event(metadata, allFields);
        Dispatch.DispatchEvent(@event);
    }
}
