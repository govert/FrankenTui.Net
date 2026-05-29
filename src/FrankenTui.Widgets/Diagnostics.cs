// Upstream source: crates/ftui-widgets/src/diagnostics.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Direct 1-1 port of DiagnosticLog, DiagnosticSupport, fnv1a_hash, and helper utilities.

using System.Runtime.CompilerServices;

namespace FrankenTui.Widgets;

// =============================================================================
// IDiagnosticRecord
// =============================================================================

/// <summary>
/// Trait for diagnostic entries that can be serialized to JSONL.
/// Consumers define their own entry structs with domain-specific fields
/// and implement this interface to plug into <see cref="DiagnosticLog{T}"/>.
/// </summary>
public interface IDiagnosticRecord
{
    /// <summary>Format this entry as a single JSONL line (no trailing newline).</summary>
    string ToJsonl();
}

// =============================================================================
// IDiagnosticHookDispatch
// =============================================================================

/// <summary>
/// Interface implemented by telemetry hook collections that can observe
/// diagnostic entries of type <typeparamref name="T"/>.
/// </summary>
public interface IDiagnosticHookDispatch<T> where T : IDiagnosticRecord
{
    /// <summary>Dispatch a single diagnostic entry to any registered hooks.</summary>
    void Dispatch(T entry);
}

// =============================================================================
// JsonStringLiteral
// =============================================================================

/// <summary>
/// Encode a string as a JSON string literal.
/// The returned value includes the surrounding quotes and correctly escapes
/// control characters so the result can be embedded directly into JSONL output.
/// </summary>
public static class JsonHelper
{
    public static string JsonStringLiteral(string value)
    {
        // Estimate output capacity: input length + 2 quotes + small escape overhead
        var out_ = new System.Text.StringBuilder(value.Length + 2);
        out_.Append('"');
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '"': out_.Append("\\\""); break;
                case '\\': out_.Append("\\\\"); break;
                case '\n': out_.Append("\\n"); break;
                case '\r': out_.Append("\\r"); break;
                case '\t': out_.Append("\\t"); break;
                case '\b': out_.Append("\\b"); break;
                case '\f': out_.Append("\\f"); break;
                default:
                    if (ch < '\u0020') // control character
                    {
                        out_.Append($"\\u{(int)ch:x4}");
                    }
                    else
                    {
                        out_.Append(ch);
                    }
                    break;
            }
        }
        out_.Append('"');
        return out_.ToString();
    }
}

// =============================================================================
// DiagnosticLog<T>
// =============================================================================

/// <summary>
/// Bounded in-memory diagnostic log with optional stderr mirroring.
/// Generic over the entry type <typeparamref name="T"/> so different
/// subsystems can use their own entry structs while sharing the log infrastructure.
/// </summary>
public sealed class DiagnosticLog<T> where T : IDiagnosticRecord
{
    private T[] _entries;
    private int _head;           // logical start index after bounded evictions
    private int _count;          // logical count
    private int _maxEntries;     // maximum entries to keep (0 = unlimited)
    private bool _writeStderr;   // whether to also write to stderr

    /// <summary>Create a new diagnostic log with a default capacity of 10 000 entries.</summary>
    public DiagnosticLog()
    {
        _entries = [];
        _head = 0;
        _count = 0;
        _maxEntries = 10_000;
        _writeStderr = false;
    }

    /// <summary>Enable stderr mirroring — each recorded entry is also written to stderr as a JSONL line.</summary>
    public DiagnosticLog<T> WithStderr()
    {
        _writeStderr = true;
        return this;
    }

    /// <summary>
    /// Set the maximum number of entries to keep. When the log is full,
    /// the oldest entry is evicted. Pass 0 for unlimited.
    /// </summary>
    public DiagnosticLog<T> WithMaxEntries(int max)
    {
        _maxEntries = max;
        return this;
    }

    /// <summary>Record a diagnostic entry.</summary>
    public void Record(T entry)
    {
        if (_writeStderr)
        {
            System.Console.Error.WriteLine(entry.ToJsonl());
        }

        // Append the entry
        if (_count == _entries.Length)
        {
            // Grow the array
            var newSize = _entries.Length == 0 ? 16 : _entries.Length * 2;
            var newArr = new T[newSize];
            for (var i = 0; i < _count; i++)
                newArr[i] = _entries[(_head + i) % _entries.Length];
            _entries = newArr;
            _head = 0;
        }

        var idx = (_head + _count) % _entries.Length;
        _entries[idx] = entry;
        _count++;

        // Evict oldest if exceeding max
        if (_maxEntries > 0 && _count > _maxEntries)
        {
            _head = (_head + 1) % _entries.Length;
            _count--;

            // Compact when head wraps around past half capacity
            if (_head >= _entries.Length / 2)
            {
                var compacted = new T[_count];
                for (var i = 0; i < _count; i++)
                    compacted[i] = _entries[(_head + i) % _entries.Length];
                _entries = compacted;
                _head = 0;
            }
        }
    }

    /// <summary>Get all entries as a span.</summary>
    public ReadOnlySpan<T> Entries()
    {
        if (_count == 0) return [];
        var result = new T[_count];
        for (var i = 0; i < _count; i++)
            result[i] = _entries[(_head + i) % _entries.Length];
        return result;
    }

    /// <summary>Get entries matching a predicate.</summary>
    public T[] EntriesMatching(Func<T, bool> predicate)
    {
        var matches = new System.Collections.Generic.List<T>(_count);
        for (var i = 0; i < _count; i++)
        {
            var entry = _entries[(_head + i) % _entries.Length];
            if (predicate(entry))
                matches.Add(entry);
        }
        return [.. matches];
    }

    /// <summary>Clear all entries.</summary>
    public void Clear()
    {
        _entries = [];
        _head = 0;
        _count = 0;
    }

    /// <summary>Export all entries as a JSONL string (newline-separated).</summary>
    public string ToJsonlString()
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < _count; i++)
        {
            if (i > 0) sb.Append('\n');
            sb.Append(_entries[(_head + i) % _entries.Length].ToJsonl());
        }
        return sb.ToString();
    }

    /// <summary>Number of recorded entries.</summary>
    public int Length => _count;

    /// <summary>Whether the log is empty.</summary>
    public bool IsEmpty => _count == 0;
}

// =============================================================================
// DiagnosticSupport<TDiagnostic, THooks>
// =============================================================================

/// <summary>
/// Shared state for optional diagnostic logging plus optional telemetry hooks.
/// This is the reusable control-flow skeleton shared by diagnostic-enabled
/// widgets and screens: optional bounded log, optional hook collection,
/// shared Record() ordering (hooks first, then log).
/// </summary>
public sealed class DiagnosticSupport<TDiagnostic, THooks>
    where TDiagnostic : IDiagnosticRecord
    where THooks : IDiagnosticHookDispatch<TDiagnostic>
{
    private DiagnosticLog<TDiagnostic>? _log;
    private THooks? _hooks;

    /// <summary>Create an empty diagnostic support bundle with no log and no hooks.</summary>
    public DiagnosticSupport() { }

    /// <summary>Enable logging with the provided diagnostic log.</summary>
    public DiagnosticSupport<TDiagnostic, THooks> WithLog(DiagnosticLog<TDiagnostic> log)
    {
        _log = log;
        return this;
    }

    /// <summary>Enable telemetry hooks with the provided hook set.</summary>
    public DiagnosticSupport<TDiagnostic, THooks> WithHooks(THooks hooks)
    {
        _hooks = hooks;
        return this;
    }

    /// <summary>Replace the diagnostic log.</summary>
    public void SetLog(DiagnosticLog<TDiagnostic> log) => _log = log;

    /// <summary>Replace the telemetry hooks.</summary>
    public void SetHooks(THooks hooks) => _hooks = hooks;

    /// <summary>Borrow the diagnostic log, if enabled.</summary>
    public DiagnosticLog<TDiagnostic>? Log => _log;

    /// <summary>Borrow the telemetry hooks, if enabled.</summary>
    public THooks? Hooks => _hooks;

    /// <summary>Returns true when either logging or hooks are enabled.</summary>
    public bool IsActive => _log is not null || _hooks is not null;

    /// <summary>
    /// Dispatch an entry to hooks first, then record it to the log.
    /// </summary>
    public void Record(TDiagnostic entry)
    {
        _hooks?.Dispatch(entry);
        _log?.Record(entry);
    }
}

// =============================================================================
// FNV-1a checksum utility
// =============================================================================

/// <summary>
/// FNV-1a 64-bit hash utilities for determinism verification.
/// Same algorithm used by both inspector and mouse_playground for
/// determinism verification checksums.
/// </summary>
public static class Fnv1aHash
{
    private const ulong FnvOffsetBasis = 0xcbf29ce484222325;
    private const ulong FnvPrime = 0x100000001b3;

    /// <summary>Compute an FNV-1a 64-bit hash of the given byte span.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Hash(ReadOnlySpan<byte> data)
    {
        var hash = FnvOffsetBasis;
        for (var i = 0; i < data.Length; i++)
        {
            hash ^= data[i];
            hash *= FnvPrime;
        }
        return hash;
    }
}

// =============================================================================
// Env flag helpers
// =============================================================================

/// <summary>
/// Helpers for environment-flag-based diagnostics.
/// </summary>
public static class EnvFlagHelper
{
    /// <summary>
    /// Check an environment variable as a boolean diagnostic flag.
    /// Returns true if the variable is set to "1" or "true" (case-insensitive).
    /// </summary>
    public static bool EnvFlagEnabled(string varName)
    {
        var value = System.Environment.GetEnvironmentVariable(varName);
        return value is not null && EnvFlagValueEnabled(value);
    }

    internal static bool EnvFlagValueEnabled(string value) =>
        value == "1" || string.Equals(value, "true", System.StringComparison.OrdinalIgnoreCase);
}
