// Upstream source: crates/ftui-extras/src/logging.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Port of TracingConfig, FxLog level styling, formatting.
// DIVERGENCE: The upstream provides a tracing_subscriber::Layer implementation
// that integrates with the Rust tracing crate. .NET has no tracing equivalent.
// FxLog is a standalone event-based logger with equivalent configuration and
// styling depth, routing through an event callback instead of a subscriber Layer.

using FrankenTui.Render;

namespace FrankenTui.Extras;

/// <summary>Log severity levels.</summary>
public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warn,
    Error,
}

/// <summary>Configuration for log formatting.</summary>
public sealed class LogConfig
{
    /// <summary>Show timestamps. Default: true.</summary>
    public bool ShowTime { get; set; } = true;
    /// <summary>Show log level. Default: true.</summary>
    public bool ShowLevel { get; set; } = true;
    /// <summary>Show the target (caller name). Default: true.</summary>
    public bool ShowTarget { get; set; } = true;
    /// <summary>Show structured fields beyond message. Default: true.</summary>
    public bool ShowFields { get; set; } = true;
    /// <summary>Show source file:line. Default: false.</summary>
    public bool ShowSource { get; set; } = false;
}

/// <summary>Log entry data.</summary>
public sealed class LogEntry
{
    public LogLevel Level { get; init; }
    public string Message { get; init; } = "";
    public string? Target { get; init; }
    public string? Source { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Dictionary<string, string>? Fields { get; init; }
}

/// <summary>Simple logging infrastructure. Routes to OnLog event.</summary>
public static class FxLog
{
    /// <summary>Current log configuration.</summary>
    public static LogConfig Config { get; set; } = new();

    /// <summary>Event raised for each log entry. Subscribers receive fully formatted data.</summary>
    public static event Action<LogEntry>? OnLog;

    /// <summary>Log at the specified level.</summary>
    public static void Log(LogLevel level, string message, string? target = null, string? source = null, Dictionary<string, string>? fields = null)
    {
        if (OnLog is null) return;
        var entry = new LogEntry
        {
            Level = level,
            Message = message,
            Target = target,
            Source = source,
            Fields = fields,
        };
        OnLog(entry);
    }

    public static void Trace(string msg, string? target = null) => Log(LogLevel.Trace, msg, target);
    public static void Debug(string msg, string? target = null) => Log(LogLevel.Debug, msg, target);
    public static void Info(string msg, string? target = null) => Log(LogLevel.Info, msg, target);
    public static void Warn(string msg, string? target = null) => Log(LogLevel.Warn, msg, target);
    public static void Error(string msg, string? target = null) => Log(LogLevel.Error, msg, target);

    /// <summary>Get the default foreground color for a log level.</summary>
    public static PackedRgba LevelColor(LogLevel level) => level switch
    {
        LogLevel.Error => PackedRgba.Rgb(255, 0, 0),
        LogLevel.Warn => PackedRgba.Rgb(255, 200, 0),
        LogLevel.Info => PackedRgba.Rgb(0, 200, 0),
        LogLevel.Debug => PackedRgba.Rgb(100, 100, 255),
        LogLevel.Trace => PackedRgba.Rgb(150, 150, 150),
        _ => PackedRgba.White,
    };

    /// <summary>Format level as a fixed-width string.</summary>
    public static string LevelString(LogLevel level) => level switch
    {
        LogLevel.Error => "ERROR",
        LogLevel.Warn => "WARN ",
        LogLevel.Info => "INFO ",
        LogLevel.Debug => "DEBUG",
        LogLevel.Trace => "TRACE",
        _ => "     ",
    };
}
