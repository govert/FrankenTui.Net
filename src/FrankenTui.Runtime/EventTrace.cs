// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/event_trace.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
//
// Event trace recording and replay for deterministic debugging.
// Records all external events and Bayesian evidence entries with
// monotonic nanosecond timestamps to gzip-compressed JSONL.
//
// DIVERGENCE: Uses System.Text.Json instead of serde.
// DIVERGENCE: GZip compression via System.IO.Compression instead of flate2.

using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FrankenTui.Core;

namespace FrankenTui.Runtime;

// ── Schema ────────────────────────────────────────────────────────────────

public static class EventTraceSchema
{
    public const string SchemaVersion = "event-trace-v1";
}

// ── Serializable sub-types ────────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SerKeyCode
{
    Enter, Escape, Backspace, Tab, BackTab, Delete, Insert,
    Home, End, PageUp, PageDown,
    Up, Down, Left, Right,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    Null,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SerKeyEventKind { Press, Release, Repeat }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SerMouseEventKind { Down, Up, Drag, Moved, ScrollDown, ScrollUp, ScrollLeft, ScrollRight }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SerImePhase { Enabled, Disabled, Preedit, Commit }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SerClipboardSource { Osc52, Native, External, Unknown }

/// <summary>Serializable character key code wrapper.</summary>
public sealed class SerCharKeyCode
{
    [JsonPropertyName("type")] public string Type => "Char";
    [JsonPropertyName("value")] public char Value { get; set; }
}

// ── TraceRecord ───────────────────────────────────────────────────────────

/// <summary>A single record in an event trace JSONL file.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "event")]
[JsonDerivedType(typeof(TraceHeader), "trace_header")]
[JsonDerivedType(typeof(TraceKey), "key")]
[JsonDerivedType(typeof(TraceMouse), "mouse")]
[JsonDerivedType(typeof(TraceResize), "resize")]
[JsonDerivedType(typeof(TracePaste), "paste")]
[JsonDerivedType(typeof(TraceIme), "ime")]
[JsonDerivedType(typeof(TraceFocus), "focus")]
[JsonDerivedType(typeof(TraceClipboard), "clipboard")]
[JsonDerivedType(typeof(TraceTick), "tick")]
[JsonDerivedType(typeof(TraceFrameTime), "frame_time")]
[JsonDerivedType(typeof(TraceRngSeed), "rng_seed")]
[JsonDerivedType(typeof(TraceEvidence), "evidence")]
[JsonDerivedType(typeof(TraceSummary), "trace_summary")]
public abstract record TraceRecord;

public sealed record TraceHeader(
    [property: JsonPropertyName("schema_version")] string SchemaVersion,
    [property: JsonPropertyName("session_name")] string SessionName,
    [property: JsonPropertyName("terminal_size")] ushort[] TerminalSize,
    [property: JsonPropertyName("seed"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ulong? Seed
) : TraceRecord;

public sealed record TraceKey(
    [property: JsonPropertyName("ts_ns")] ulong TsNs,
    [property: JsonPropertyName("code")] JsonElement Code,
    [property: JsonPropertyName("modifiers")] byte Modifiers,
    [property: JsonPropertyName("kind")] SerKeyEventKind Kind
) : TraceRecord;

public sealed record TraceMouse(
    [property: JsonPropertyName("ts_ns")] ulong TsNs,
    [property: JsonPropertyName("kind")] SerMouseEventKind Kind,
    [property: JsonPropertyName("x")] ushort X,
    [property: JsonPropertyName("y")] ushort Y,
    [property: JsonPropertyName("modifiers")] byte Modifiers
) : TraceRecord;

public sealed record TraceResize(
    [property: JsonPropertyName("ts_ns")] ulong TsNs,
    [property: JsonPropertyName("cols")] ushort Cols,
    [property: JsonPropertyName("rows")] ushort Rows
) : TraceRecord;

public sealed record TracePaste(
    [property: JsonPropertyName("ts_ns")] ulong TsNs,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("bracketed")] bool Bracketed
) : TraceRecord;

public sealed record TraceIme(
    [property: JsonPropertyName("ts_ns")] ulong TsNs,
    [property: JsonPropertyName("phase")] SerImePhase Phase,
    [property: JsonPropertyName("text")] string Text
) : TraceRecord;

public sealed record TraceFocus(
    [property: JsonPropertyName("ts_ns")] ulong TsNs,
    [property: JsonPropertyName("gained")] bool Gained
) : TraceRecord;

public sealed record TraceClipboard(
    [property: JsonPropertyName("ts_ns")] ulong TsNs,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("source")] SerClipboardSource Source
) : TraceRecord;

public sealed record TraceTick(
    [property: JsonPropertyName("ts_ns")] ulong TsNs
) : TraceRecord;

public sealed record TraceFrameTime(
    [property: JsonPropertyName("ts_ns")] ulong TsNs,
    [property: JsonPropertyName("render_us"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ulong? RenderUs
) : TraceRecord;

public sealed record TraceRngSeed(
    [property: JsonPropertyName("ts_ns")] ulong TsNs,
    [property: JsonPropertyName("seed")] ulong Seed
) : TraceRecord;

public sealed record TraceEvidence(
    [property: JsonPropertyName("ts_ns")] ulong TsNs,
    [property: JsonPropertyName("entry")] EvidenceEntryJson Entry
) : TraceRecord;

public sealed record TraceSummary(
    [property: JsonPropertyName("total_events")] ulong TotalEvents,
    [property: JsonPropertyName("total_duration_ns")] ulong TotalDurationNs,
    [property: JsonPropertyName("total_evidence"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ulong? TotalEvidence
) : TraceRecord;

/// <summary>JSON-serializable evidence entry mirror.</summary>
public sealed record EvidenceEntryJson
{
    [JsonPropertyName("id")] public ulong Id { get; set; }
    [JsonPropertyName("ts_ns")] public ulong TsNs { get; set; }
    [JsonPropertyName("domain")] public string Domain { get; set; } = "";
    [JsonPropertyName("log_posterior")] public double LogPosterior { get; set; }
    [JsonPropertyName("evidence")] public List<EvidenceTermJson>? Evidence { get; set; }
    [JsonPropertyName("action")] public string Action { get; set; } = "";
    [JsonPropertyName("loss_avoided")] public double LossAvoided { get; set; }
    [JsonPropertyName("ci")] public double[]? Ci { get; set; }
}

public sealed record EvidenceTermJson
{
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("bf")] public double Bf { get; set; }
}

// ── EventTraceWriter ──────────────────────────────────────────────────────

/// <summary>
/// Writes event trace records to a gzip-compressed JSONL file.
/// </summary>
public sealed class EventTraceWriter : IDisposable
{
    private readonly GZipStream _gzip;
    private readonly StreamWriter _writer;
    private readonly string _sessionName;
    private readonly (ushort, ushort) _terminalSize;
    private ulong _eventCount;
    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private bool _headerWritten;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public EventTraceWriter(string path, string sessionName, (ushort, ushort) terminalSize, ulong? seed = null)
    {
        _sessionName = sessionName;
        _terminalSize = terminalSize;
        var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        _gzip = new GZipStream(fs, CompressionLevel.Optimal);
        _writer = new StreamWriter(_gzip, Encoding.UTF8, leaveOpen: false);
    }

    private void WriteHeader()
    {
        if (_headerWritten) return;
        var header = new TraceHeader(
            EventTraceSchema.SchemaVersion,
            _sessionName,
            [_terminalSize.Item1, _terminalSize.Item2],
            null);
        WriteLine(header);
        _headerWritten = true;
    }

    private void WriteLine(TraceRecord record)
    {
        var json = JsonSerializer.Serialize<TraceRecord>(record, JsonOpts);
        _writer.WriteLine(json);
        _writer.Flush();
    }

    public void RecordKey(ulong tsNs, SerKeyCode code, char? ch, byte modifiers, SerKeyEventKind kind)
    {
        WriteHeader();
        var keyRecord = new TraceKey(tsNs,
            ch.HasValue
                ? JsonSerializer.SerializeToElement(new SerCharKeyCode { Value = ch.Value })
                : JsonSerializer.SerializeToElement(new { type = code.ToString().ToLowerInvariant(), value = JsonSerializer.SerializeToElement("") }.GetType()),
            modifiers, kind);
        WriteLine(keyRecord);
        _eventCount++;
    }

    public void RecordMouse(ulong tsNs, SerMouseEventKind kind, ushort x, ushort y, byte modifiers)
    {
        WriteHeader();
        WriteLine(new TraceMouse(tsNs, kind, x, y, modifiers));
        _eventCount++;
    }

    public void RecordResize(ulong tsNs, ushort cols, ushort rows)
    {
        WriteHeader();
        WriteLine(new TraceResize(tsNs, cols, rows));
        _eventCount++;
    }

    public void RecordPaste(ulong tsNs, string text, bool bracketed)
    {
        WriteHeader();
        WriteLine(new TracePaste(tsNs, text, bracketed));
        _eventCount++;
    }

    public void RecordIme(ulong tsNs, SerImePhase phase, string text)
    {
        WriteHeader();
        WriteLine(new TraceIme(tsNs, phase, text));
        _eventCount++;
    }

    public void RecordFocus(ulong tsNs, bool gained)
    {
        WriteHeader();
        WriteLine(new TraceFocus(tsNs, gained));
        _eventCount++;
    }

    public void RecordClipboard(ulong tsNs, string content, SerClipboardSource source)
    {
        WriteHeader();
        WriteLine(new TraceClipboard(tsNs, content, source));
        _eventCount++;
    }

    public void RecordTick(ulong tsNs)
    {
        WriteHeader();
        WriteLine(new TraceTick(tsNs));
        _eventCount++;
    }

    public void RecordEvidence(ulong tsNs, EvidenceEntry entry)
    {
        WriteHeader();
        var jsonEntry = new EvidenceEntryJson
        {
            Id = entry.DecisionId,
            TsNs = entry.TimestampNs,
            Domain = DecisionDomainMeta.AsStr(entry.Domain),
            LogPosterior = entry.LogPosterior,
            Evidence = entry.TopEvidence
                .Where(t => t != null)
                .Select(t => new EvidenceTermJson { Label = t!.Label, Bf = t.BayesFactor })
                .ToList(),
            Action = entry.Action,
            LossAvoided = entry.LossAvoided,
            Ci = [entry.ConfidenceInterval.Lower, entry.ConfidenceInterval.Upper],
        };
        WriteLine(new TraceEvidence(tsNs, jsonEntry));
        _eventCount++;
    }

    public void Finish()
    {
        WriteHeader();
        WriteLine(new TraceSummary(_eventCount, (ulong)(_sw.Elapsed.TotalMilliseconds * 1_000_000), null));
        _writer.Flush();
    }

    public void Dispose()
    {
        try { Finish(); } catch { }
        _writer.Dispose();
        _gzip.Dispose();
    }
}

// ── EventTraceReader / EventReplayer / EvidenceVerifier ────────────────────

// DIVERGENCE: Reading/replay/verification deferred. The upstream EventTraceReader,
// EventReplayer, and EvidenceVerifier (bulk of the 2254 lines) depend on serde
// deserialization and event reconstruction. These will be ported when the
// deterministic replay test infrastructure is needed (Phase I verification).
