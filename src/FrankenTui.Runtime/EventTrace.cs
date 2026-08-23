// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/event_trace.rs
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
//
// Event trace recording and replay for deterministic debugging.
// Records all external events and Bayesian evidence entries with
// monotonic nanosecond timestamps to gzip-compressed JSONL.
//
// DIVERGENCE: Uses System.Text.Json instead of serde.
// DIVERGENCE: GZip compression via System.IO.Compression instead of flate2.

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    MediaPlayPause, MediaStop, MediaNextTrack, MediaPrevTrack,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SerKeyEventKind { Press, Release, Repeat }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SerMouseEventKind { Down, Up, Drag, Moved, ScrollUp, ScrollDown, ScrollLeft, ScrollRight }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SerMouseButton { Left, Right, Middle }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SerImePhase { Start, Update, Commit, Cancel }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SerClipboardSource { Osc52, Unknown }

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
    [property: JsonPropertyName("kind")] JsonElement Kind,
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
    [JsonPropertyName("decision_id")] public ulong DecisionId { get; init; }
    [JsonPropertyName("domain")] public string Domain { get; init; } = "";
    [JsonPropertyName("log_posterior")] public double LogPosterior { get; init; }
    [JsonPropertyName("evidence")] public List<EvidenceTermJson> Evidence { get; init; } = [];
    [JsonPropertyName("action")] public string Action { get; init; } = "";
    [JsonPropertyName("loss_avoided")] public double LossAvoided { get; init; }
    [JsonPropertyName("confidence_interval")] public double[] ConfidenceInterval { get; init; } = [0.0, 0.0];
}

public sealed record EvidenceTermJson
{
    [JsonPropertyName("label")] public string Label { get; init; } = "";
    [JsonPropertyName("bayes_factor")] public double BayesFactor { get; init; }
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
    private readonly ulong? _seed;
    private ulong _eventCount;
    private ulong _evidenceCount;
    private ulong? _firstTimestampNs;
    private ulong _lastTimestampNs;
    private bool _headerWritten;
    private bool _finished;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public EventTraceWriter(string path, string sessionName, (ushort, ushort) terminalSize, ulong? seed = null)
    {
        _sessionName = sessionName;
        _terminalSize = terminalSize;
        _seed = seed;
        var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        _gzip = new GZipStream(fs, CompressionLevel.Fastest);
        _writer = new StreamWriter(_gzip, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: false);
    }

    private void WriteHeader()
    {
        if (_headerWritten) return;
        var header = new TraceHeader(
            EventTraceSchema.SchemaVersion,
            _sessionName,
            [_terminalSize.Item1, _terminalSize.Item2],
            _seed);
        WriteLine(header);
        _headerWritten = true;
    }

    private void WriteLine(TraceRecord record)
    {
        var json = JsonSerializer.Serialize<TraceRecord>(record, JsonOpts);
        _writer.WriteLine(json);
        _writer.Flush();
    }

    /// <summary>Writes any non-header/summary trace record and updates summary counters.</summary>
    public void WriteRecord(TraceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        ThrowIfFinished();
        if (record is TraceHeader or TraceSummary)
        {
            throw new ArgumentException("Headers and summaries are owned by the writer lifecycle.", nameof(record));
        }

        WriteHeader();
        WriteLine(record);
        if (TryGetTimestamp(record, out var timestampNs))
        {
            _firstTimestampNs ??= timestampNs;
            _lastTimestampNs = timestampNs;
        }

        _eventCount++;
        if (record is TraceEvidence)
        {
            _evidenceCount++;
        }
    }

    public void RecordKey(ulong tsNs, SerKeyCode code, char? ch, byte modifiers, SerKeyEventKind kind)
    {
        WriteRecord(new TraceKey(tsNs, SerializeKeyCode(code, ch), modifiers, kind));
    }

    public void RecordMouse(
        ulong tsNs,
        SerMouseEventKind kind,
        ushort x,
        ushort y,
        byte modifiers,
        SerMouseButton button = SerMouseButton.Left)
    {
        WriteRecord(new TraceMouse(tsNs, SerializeMouseKind(kind, button), x, y, modifiers));
    }

    public void RecordResize(ulong tsNs, ushort cols, ushort rows)
    {
        WriteRecord(new TraceResize(tsNs, cols, rows));
    }

    public void RecordPaste(ulong tsNs, string text, bool bracketed)
    {
        WriteRecord(new TracePaste(tsNs, text, bracketed));
    }

    public void RecordIme(ulong tsNs, SerImePhase phase, string text)
    {
        WriteRecord(new TraceIme(tsNs, phase, text));
    }

    public void RecordFocus(ulong tsNs, bool gained)
    {
        WriteRecord(new TraceFocus(tsNs, gained));
    }

    public void RecordClipboard(ulong tsNs, string content, SerClipboardSource source)
    {
        WriteRecord(new TraceClipboard(tsNs, content, source));
    }

    public void RecordTick(ulong tsNs)
    {
        WriteRecord(new TraceTick(tsNs));
    }

    public void RecordFrameTime(ulong tsNs, ulong? renderUs = null) =>
        WriteRecord(new TraceFrameTime(tsNs, renderUs));

    public void RecordRngSeed(ulong tsNs, ulong seed) =>
        WriteRecord(new TraceRngSeed(tsNs, seed));

    public void RecordEvidence(ulong tsNs, EvidenceEntry entry)
    {
        var jsonEntry = new EvidenceEntryJson
        {
            DecisionId = entry.DecisionId,
            Domain = DecisionDomainMeta.AsStr(entry.Domain),
            LogPosterior = entry.LogPosterior,
            Evidence = entry.TopEvidence
                .Where(t => t != null)
                .Select(t => new EvidenceTermJson { Label = t!.Label, BayesFactor = t.BayesFactor })
                .ToList(),
            Action = entry.Action,
            LossAvoided = entry.LossAvoided,
            ConfidenceInterval = [entry.ConfidenceInterval.Lower, entry.ConfidenceInterval.Upper],
        };
        WriteRecord(new TraceEvidence(tsNs, jsonEntry));
    }

    public void Finish()
    {
        if (_finished)
        {
            return;
        }

        WriteHeader();
        var durationNs = _firstTimestampNs is { } first && _lastTimestampNs >= first
            ? _lastTimestampNs - first
            : 0;
        WriteLine(new TraceSummary(
            _eventCount,
            durationNs,
            _evidenceCount == 0 ? null : _evidenceCount));
        _writer.Flush();
        _finished = true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try { Finish(); } catch { }
        _writer.Dispose();
        _gzip.Dispose();
        _disposed = true;
    }

    private static JsonElement SerializeKeyCode(SerKeyCode code, char? character)
    {
        if (character is { } value)
        {
            return JsonSerializer.SerializeToElement(new SerCharKeyCode { Value = value }, JsonOpts);
        }

        var functionNumber = code is >= SerKeyCode.F1 and <= SerKeyCode.F12
            ? (int)code - (int)SerKeyCode.F1 + 1
            : 0;
        return functionNumber == 0
            ? JsonSerializer.SerializeToElement(new { type = code.ToString() }, JsonOpts)
            : JsonSerializer.SerializeToElement(new { type = "F", value = functionNumber }, JsonOpts);
    }

    private static JsonElement SerializeMouseKind(SerMouseEventKind kind, SerMouseButton button) =>
        kind is SerMouseEventKind.Down or SerMouseEventKind.Up or SerMouseEventKind.Drag
            ? JsonSerializer.SerializeToElement(new { type = kind.ToString(), button = button.ToString() }, JsonOpts)
            : JsonSerializer.SerializeToElement(new { type = kind.ToString() }, JsonOpts);

    private static bool TryGetTimestamp(TraceRecord record, out ulong timestampNs)
    {
        timestampNs = record switch
        {
            TraceKey value => value.TsNs,
            TraceMouse value => value.TsNs,
            TraceResize value => value.TsNs,
            TracePaste value => value.TsNs,
            TraceIme value => value.TsNs,
            TraceFocus value => value.TsNs,
            TraceClipboard value => value.TsNs,
            TraceTick value => value.TsNs,
            TraceFrameTime value => value.TsNs,
            TraceRngSeed value => value.TsNs,
            TraceEvidence value => value.TsNs,
            _ => 0,
        };
        return record is not TraceHeader and not TraceSummary;
    }

    private void ThrowIfFinished()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_finished)
        {
            throw new InvalidOperationException("The event trace has already been finished.");
        }
    }
}

// ── EventTraceReader / EventReplayer / EvidenceVerifier ────────────────────

// Reader, replay, and evidence-verification support lives in EventTraceReplay.cs.
