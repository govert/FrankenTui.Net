// SPDX-License-Identifier: Apache-2.0
// Behavioral port of .external/frankentui/crates/ftui-runtime/src/event_trace.rs tests.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using System.IO.Compression;
using System.Text;
using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public sealed class EventTraceTests
{
    [Fact]
    public void GzipWriterAndReaderRoundTripEveryRecordFamily()
    {
        var path = Path.Combine(Path.GetTempPath(), $"frankentui-trace-{Guid.NewGuid():N}.jsonl.gz");
        try
        {
            using (var writer = new EventTraceWriter(path, "roundtrip", (80, 24), seed: 42))
            {
                writer.RecordKey(100, SerKeyCode.Null, 'x', 0, SerKeyEventKind.Press);
                writer.RecordKey(150, SerKeyCode.F5, null, 2, SerKeyEventKind.Repeat);
                writer.RecordMouse(200, SerMouseEventKind.Down, 3, 4, 1, SerMouseButton.Right);
                writer.RecordResize(300, 120, 40);
                writer.RecordPaste(400, "hello", bracketed: true);
                writer.RecordIme(500, SerImePhase.Update, "compose");
                writer.RecordFocus(600, gained: true);
                writer.RecordClipboard(700, "clip", SerClipboardSource.Osc52);
                writer.RecordTick(800);
                writer.RecordFrameTime(900, 125);
                writer.RecordRngSeed(1_000, 1234);
                writer.RecordEvidence(1_100, BuildEvidence(logPosterior: -0.5));
                writer.Finish();
            }

            var trace = EventTraceReader.Open(path);

            Assert.Equal("roundtrip", trace.Header?.SessionName);
            Assert.Equal((ushort)80, trace.TerminalSize?.Columns);
            Assert.Equal((ushort)24, trace.TerminalSize?.Rows);
            Assert.Equal((ulong)42, trace.Seed);
            Assert.Equal((ulong)12, trace.TotalEvents);
            Assert.Equal((ulong)1, trace.TotalEvidence);
            Assert.Equal((ulong)1_000, trace.Summary?.TotalDurationNs);
            Assert.Equal(12, trace.EventRecords.Count);
            Assert.Equal(9, trace.EventsWithTimestamps.Count);

            var character = Assert.IsType<TraceKey>(trace.Records[1]);
            Assert.Equal("Char", character.Code.GetProperty("type").GetString());
            Assert.Equal("x", character.Code.GetProperty("value").GetString());

            var function = Assert.IsType<TraceKey>(trace.Records[2]);
            Assert.Equal("F", function.Code.GetProperty("type").GetString());
            Assert.Equal(5, function.Code.GetProperty("value").GetInt32());

            var mouse = Assert.IsType<TraceMouse>(trace.Records[3]);
            Assert.Equal("Down", mouse.Kind.GetProperty("type").GetString());
            Assert.Equal("Right", mouse.Kind.GetProperty("button").GetString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void WriterEmitsRustCompatibleTaggedShapesAndIdempotentSummary()
    {
        var path = Path.Combine(Path.GetTempPath(), $"frankentui-trace-shape-{Guid.NewGuid():N}.jsonl.gz");
        try
        {
            using (var writer = new EventTraceWriter(path, "shape", (40, 10), seed: 7))
            {
                writer.RecordKey(10, SerKeyCode.Enter, null, 0, SerKeyEventKind.Press);
                writer.RecordMouse(20, SerMouseEventKind.Drag, 1, 2, 0, SerMouseButton.Middle);
                writer.RecordEvidence(30, BuildEvidence(logPosterior: 0.25));
                writer.Finish();
                writer.Finish();
            }

            var jsonl = ReadGzipText(path);

            Assert.Contains("\"code\":{\"type\":\"Enter\"}", jsonl, StringComparison.Ordinal);
            Assert.Contains("\"kind\":{\"type\":\"Drag\",\"button\":\"Middle\"}", jsonl, StringComparison.Ordinal);
            Assert.Contains("\"decision_id\":7", jsonl, StringComparison.Ordinal);
            Assert.Contains("\"bayes_factor\":2", jsonl, StringComparison.Ordinal);
            Assert.Contains("\"confidence_interval\":[0.1,0.9]", jsonl, StringComparison.Ordinal);
            Assert.Single(jsonl.Split('\n', StringSplitOptions.RemoveEmptyEntries), line => line.Contains("\"event\":\"trace_summary\"", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReaderAcceptsPlainRustCompatibleJsonlAndRejectsMalformedRows()
    {
        const string jsonl = """
            {"event":"trace_header","schema_version":"event-trace-v1","session_name":"plain","terminal_size":[80,24],"seed":9}

            {"event":"key","ts_ns":10,"code":{"type":"Enter"},"modifiers":0,"kind":"Press"}
            {"event":"tick","ts_ns":20}
            {"event":"trace_summary","total_events":2,"total_duration_ns":10}
            """;

        var trace = EventTraceReader.FromBytes(Encoding.UTF8.GetBytes(jsonl));

        Assert.Equal(4, trace.Records.Count);
        Assert.Equal((ulong)9, trace.Seed);
        Assert.Equal(2, trace.EventsWithTimestamps.Count);
        Assert.Throws<InvalidDataException>(() =>
            EventTraceReader.FromBytes(Encoding.UTF8.GetBytes("{not-json}\n")));
    }

    [Fact]
    public void ReplayerPreservesOrderSupportsPeekResetAdvanceAndSaturatingDelay()
    {
        var records = new TimestampedTraceRecord[]
        {
            new(new TraceTick(10), 10),
            new(new TraceFocus(5, true), 5),
            new(new TraceResize(20, 80, 24), 20),
        };
        var replayer = new EventReplayer(records);

        Assert.Equal((ulong)0, replayer.DelayToNextNs);
        Assert.IsType<TraceTick>(replayer.Peek()?.Record);
        Assert.IsType<TraceTick>(replayer.NextEvent()?.Record);
        Assert.Equal((ulong)0, replayer.DelayToNextNs);
        Assert.Single(replayer.AdvanceUntil(5));
        Assert.Equal(2, replayer.Position);
        Assert.Single(replayer.Remaining);
        Assert.Single(replayer.DrainAll());
        Assert.True(replayer.IsDone);
        Assert.Null(replayer.DelayToNextNs);

        replayer.Reset();
        Assert.Equal(0, replayer.Position);
        Assert.Equal(3, replayer.DrainAll().Count);
    }

    [Fact]
    public void EvidenceVerifierAcceptsEpsilonAndReportsFieldMismatches()
    {
        var recorded = ToSerializable(BuildEvidence(logPosterior: 0.5));
        var verifier = new EvidenceVerifier(0.01);
        verifier.LoadRecorded([recorded]);

        Assert.True(verifier.Verify(BuildEvidence(logPosterior: 0.505)));
        Assert.True(verifier.IsDeterministic);
        Assert.StartsWith("PASS:", verifier.SummaryText, StringComparison.Ordinal);

        verifier.Reset();
        var mismatch = recorded with
        {
            DecisionId = 99,
            Action = "full",
            Evidence = [new EvidenceTermJson { Label = "other", BayesFactor = 8.0 }],
        };

        Assert.False(verifier.Verify(mismatch));
        Assert.False(verifier.IsDeterministic);
        Assert.Contains(verifier.Mismatches, item => item.Field == "decision_id");
        Assert.Contains(verifier.Mismatches, item => item.Field == "action");
        Assert.Contains(verifier.Mismatches, item => item.Field == "evidence[0].label");
        Assert.Contains("recorded=7", verifier.DetailReport(), StringComparison.Ordinal);
    }

    [Fact]
    public void VerifierRequiresPositiveCoverageOfEveryRecordedEntry()
    {
        var entry = ToSerializable(BuildEvidence(logPosterior: 0.5));
        var verifier = new EvidenceVerifier(1e-10);
        verifier.LoadRecorded([entry, entry with { DecisionId = 8 }]);

        Assert.True(verifier.Verify(entry));

        Assert.False(verifier.IsDeterministic);
        Assert.Equal(1, verifier.VerifiedCount);
        Assert.Equal(2, verifier.ExpectedCount);
        Assert.StartsWith("INCOMPLETE:", verifier.SummaryText, StringComparison.Ordinal);
    }

    private static EvidenceEntry BuildEvidence(double logPosterior) =>
        new EvidenceEntryBuilder(DecisionDomain.DiffStrategy, decisionId: 7, timestampNs: 123)
            .LogPosterior(logPosterior)
            .Evidence("density", 2.0)
            .Action("incremental")
            .LossAvoided(3.0)
            .ConfidenceInterval(0.1, 0.9)
            .Build();

    private static EvidenceEntryJson ToSerializable(EvidenceEntry entry) => new()
    {
        DecisionId = entry.DecisionId,
        Domain = DecisionDomainMeta.AsStr(entry.Domain),
        LogPosterior = entry.LogPosterior,
        Evidence = entry.TopEvidence
            .Where(term => term is not null)
            .Select(term => new EvidenceTermJson { Label = term!.Label, BayesFactor = term.BayesFactor })
            .ToList(),
        Action = entry.Action,
        LossAvoided = entry.LossAvoided,
        ConfidenceInterval = [entry.ConfidenceInterval.Lower, entry.ConfidenceInterval.Upper],
    };

    private static string ReadGzipText(string path)
    {
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
