// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/event_trace.rs
// (EventTraceReader, TraceFile, EventReplayer, and EvidenceVerifier).
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace FrankenTui.Runtime;

/// <summary>Reads plain or gzip-compressed event-trace JSONL.</summary>
public static class EventTraceReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
    };

    public static TraceFile Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return FromBytes(File.ReadAllBytes(path));
    }

    public static TraceFile FromBytes(ReadOnlySpan<byte> data)
    {
        using var input = new MemoryStream(data.ToArray(), writable: false);
        var isGzip = data.Length >= 2 && data[0] == 0x1F && data[1] == 0x8B;
        using Stream payload = isGzip
            ? new GZipStream(input, CompressionMode.Decompress, leaveOpen: false)
            : input;
        using var reader = new StreamReader(
            payload,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: false);

        var records = new List<TraceRecord>();
        var lineNumber = 0;
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var record = JsonSerializer.Deserialize<TraceRecord>(line, JsonOptions)
                    ?? throw new JsonException("Trace record deserialized to null.");
                records.Add(record);
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException($"Invalid event-trace JSON at line {lineNumber}.", exception);
            }
        }

        return new TraceFile(records);
    }
}

/// <summary>A parsed event-trace file.</summary>
public sealed class TraceFile
{
    private readonly TraceRecord[] _records;

    public TraceFile(IEnumerable<TraceRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        _records = records.ToArray();
    }

    public IReadOnlyList<TraceRecord> Records => _records;

    public TraceHeader? Header => _records.FirstOrDefault() as TraceHeader;

    public TraceSummary? Summary => _records.LastOrDefault() as TraceSummary;

    /// <summary>
    /// All records other than the header and summary. This intentionally mirrors
    /// upstream and therefore includes frame-time, seed, and evidence metadata.
    /// </summary>
    public IReadOnlyList<TraceRecord> EventRecords =>
        _records.Where(record => record is not TraceHeader and not TraceSummary).ToArray();

    /// <summary>
    /// Input records suitable for deterministic replay. The managed replayer
    /// yields the lossless trace union because the narrower <c>TerminalEvent</c>
    /// layer does not represent key release/repeat, IME, clipboard, or tick.
    /// </summary>
    public IReadOnlyList<TimestampedTraceRecord> EventsWithTimestamps =>
        _records
            .Where(TraceRecordMetadata.IsReplayEvent)
            .Select(record => new TimestampedTraceRecord(record, TraceRecordMetadata.GetTimestamp(record)))
            .ToArray();

    public ulong? Seed => Header?.Seed;

    public (ushort Columns, ushort Rows)? TerminalSize =>
        Header is { TerminalSize.Length: >= 2 } header
            ? (header.TerminalSize[0], header.TerminalSize[1])
            : null;

    public ulong? TotalEvents => Summary?.TotalEvents;

    public ulong? TotalEvidence => Summary?.TotalEvidence;

    public IReadOnlyList<TimestampedEvidenceEntry> EvidenceEntries =>
        _records
            .OfType<TraceEvidence>()
            .Select(record => new TimestampedEvidenceEntry(record.Entry, record.TsNs))
            .ToArray();
}

public readonly record struct TimestampedTraceRecord(TraceRecord Record, ulong TimestampNs);

public readonly record struct TimestampedEvidenceEntry(EvidenceEntryJson Entry, ulong TimestampNs);

/// <summary>Yields recorded input events in timestamp order without imposing sleeps.</summary>
public sealed class EventReplayer
{
    private readonly TimestampedTraceRecord[] _events;
    private int _position;

    public EventReplayer(IEnumerable<TimestampedTraceRecord> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        _events = events.ToArray();
    }

    public static EventReplayer FromTrace(TraceFile trace)
    {
        ArgumentNullException.ThrowIfNull(trace);
        return new EventReplayer(trace.EventsWithTimestamps);
    }

    public int Position => _position;

    public int Total => _events.Length;

    public bool IsDone => _position >= _events.Length;

    public TimestampedTraceRecord? NextEvent()
    {
        if (IsDone)
        {
            return null;
        }

        return _events[_position++];
    }

    public TimestampedTraceRecord? Peek() => IsDone ? null : _events[_position];

    public ulong? DelayToNextNs
    {
        get
        {
            if (IsDone)
            {
                return null;
            }

            if (_position == 0)
            {
                return 0;
            }

            var next = _events[_position].TimestampNs;
            var previous = _events[_position - 1].TimestampNs;
            return next >= previous ? next - previous : 0;
        }
    }

    public IReadOnlyList<TimestampedTraceRecord> Remaining => _events[_position..];

    public void Reset() => _position = 0;

    public IReadOnlyList<TraceRecord> AdvanceUntil(ulong untilNs)
    {
        var result = new List<TraceRecord>();
        while (Peek() is { } next && next.TimestampNs <= untilNs)
        {
            result.Add(NextEvent()!.Value.Record);
        }

        return result;
    }

    public IReadOnlyList<TraceRecord> DrainAll()
    {
        var result = new List<TraceRecord>(_events.Length - _position);
        while (NextEvent() is { } next)
        {
            result.Add(next.Record);
        }

        return result;
    }
}

public sealed record EvidenceMismatch(int Index, string Field, string Recorded, string Replayed);

/// <summary>Checks replayed Bayesian decisions against trace testimony.</summary>
public sealed class EvidenceVerifier
{
    private readonly double _epsilon;
    private readonly List<EvidenceEntryJson> _recorded = [];
    private readonly List<EvidenceMismatch> _mismatches = [];
    private int _verifiedCount;

    public EvidenceVerifier(double epsilon)
    {
        if (!double.IsFinite(epsilon) || epsilon < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(epsilon), epsilon, "Epsilon must be finite and non-negative.");
        }

        _epsilon = epsilon;
    }

    public static EvidenceVerifier FromTrace(TraceFile trace, double epsilon)
    {
        ArgumentNullException.ThrowIfNull(trace);
        var verifier = new EvidenceVerifier(epsilon);
        verifier.LoadRecorded(trace.EvidenceEntries.Select(item => item.Entry));
        return verifier;
    }

    public IReadOnlyList<EvidenceMismatch> Mismatches => _mismatches;

    public int VerifiedCount => _verifiedCount;

    public int ExpectedCount => _recorded.Count;

    public bool IsDeterministic => _mismatches.Count == 0 && _verifiedCount == _recorded.Count;

    public void LoadRecorded(IEnumerable<EvidenceEntryJson> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        _recorded.Clear();
        _recorded.AddRange(entries);
        Reset();
    }

    public bool Verify(EvidenceEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return Verify(ToSerializable(entry));
    }

    public bool Verify(EvidenceEntryJson replayed)
    {
        ArgumentNullException.ThrowIfNull(replayed);
        var index = _verifiedCount++;
        if (index >= _recorded.Count)
        {
            return true;
        }

        var recorded = _recorded[index];
        var ok = true;
        ok &= Compare(index, "decision_id", recorded.DecisionId, replayed.DecisionId);
        ok &= Compare(index, "domain", recorded.Domain, replayed.Domain);
        ok &= Compare(index, "action", recorded.Action, replayed.Action);
        ok &= CompareDouble(index, "log_posterior", recorded.LogPosterior, replayed.LogPosterior);
        ok &= CompareDouble(index, "loss_avoided", recorded.LossAvoided, replayed.LossAvoided);
        ok &= CompareConfidenceInterval(index, recorded.ConfidenceInterval, replayed.ConfidenceInterval);
        ok &= CompareEvidence(index, recorded.Evidence, replayed.Evidence);
        return ok;
    }

    public string SummaryText => _mismatches.Count switch
    {
        0 when _verifiedCount == _recorded.Count =>
            $"PASS: all {_verifiedCount} evidence entries are deterministic",
        0 =>
            $"INCOMPLETE: verified {_verifiedCount}/{_recorded.Count} evidence entries (no mismatches so far)",
        _ =>
            $"FAIL: {_mismatches.Count} mismatches in {_verifiedCount} verified entries (of {_recorded.Count} recorded)",
    };

    public string DetailReport()
    {
        if (_mismatches.Count == 0)
        {
            return SummaryText;
        }

        var output = new StringBuilder(SummaryText).AppendLine();
        foreach (var mismatch in _mismatches)
        {
            output.Append("  [")
                .Append(mismatch.Index)
                .Append("] ")
                .Append(mismatch.Field)
                .Append(": recorded=")
                .Append(mismatch.Recorded)
                .Append(", replayed=")
                .Append(mismatch.Replayed)
                .AppendLine();
        }

        return output.ToString();
    }

    public void Reset()
    {
        _mismatches.Clear();
        _verifiedCount = 0;
    }

    private bool Compare<T>(int index, string field, T recorded, T replayed)
    {
        if (EqualityComparer<T>.Default.Equals(recorded, replayed))
        {
            return true;
        }

        AddMismatch(index, field, recorded?.ToString() ?? "null", replayed?.ToString() ?? "null");
        return false;
    }

    private bool CompareDouble(int index, string field, double recorded, double replayed)
    {
        if (Math.Abs(recorded - replayed) <= _epsilon)
        {
            return true;
        }

        AddMismatch(index, field, Format(recorded), Format(replayed));
        return false;
    }

    private bool CompareConfidenceInterval(int index, double[] recorded, double[] replayed)
    {
        if (recorded.Length == 2 && replayed.Length == 2 &&
            Math.Abs(recorded[0] - replayed[0]) <= _epsilon &&
            Math.Abs(recorded[1] - replayed[1]) <= _epsilon)
        {
            return true;
        }

        AddMismatch(index, "confidence_interval", FormatPair(recorded), FormatPair(replayed));
        return false;
    }

    private bool CompareEvidence(
        int index,
        IReadOnlyList<EvidenceTermJson> recorded,
        IReadOnlyList<EvidenceTermJson> replayed)
    {
        if (recorded.Count != replayed.Count)
        {
            AddMismatch(index, "evidence.len", recorded.Count.ToString(CultureInfo.InvariantCulture), replayed.Count.ToString(CultureInfo.InvariantCulture));
            return false;
        }

        var ok = true;
        for (var termIndex = 0; termIndex < recorded.Count; termIndex++)
        {
            var expected = recorded[termIndex];
            var actual = replayed[termIndex];
            ok &= Compare(index, $"evidence[{termIndex}].label", expected.Label, actual.Label);
            ok &= CompareDouble(index, $"evidence[{termIndex}].bayes_factor", expected.BayesFactor, actual.BayesFactor);
        }

        return ok;
    }

    private void AddMismatch(int index, string field, string recorded, string replayed) =>
        _mismatches.Add(new EvidenceMismatch(index, field, recorded, replayed));

    private static string Format(double value) => value.ToString("F6", CultureInfo.InvariantCulture);

    private static string FormatPair(IReadOnlyList<double> values) => values.Count == 2
        ? $"({Format(values[0])}, {Format(values[1])})"
        : $"[{string.Join(", ", values.Select(Format))}]";

    private static EvidenceEntryJson ToSerializable(EvidenceEntry entry) => new()
    {
        DecisionId = entry.DecisionId,
        Domain = DecisionDomainMeta.AsStr(entry.Domain),
        LogPosterior = entry.LogPosterior,
        Evidence = entry.TopEvidence
            .Where(term => term is not null)
            .Select(term => new EvidenceTermJson
            {
                Label = term!.Label,
                BayesFactor = term.BayesFactor,
            })
            .ToList(),
        Action = entry.Action,
        LossAvoided = entry.LossAvoided,
        ConfidenceInterval = [entry.ConfidenceInterval.Lower, entry.ConfidenceInterval.Upper],
    };
}

internal static class TraceRecordMetadata
{
    public static bool IsReplayEvent(TraceRecord record) => record is
        TraceKey or TraceMouse or TraceResize or TracePaste or TraceIme or
        TraceFocus or TraceClipboard or TraceTick;

    public static ulong GetTimestamp(TraceRecord record) => record switch
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
        _ => throw new ArgumentException("The record has no timestamp.", nameof(record)),
    };
}
