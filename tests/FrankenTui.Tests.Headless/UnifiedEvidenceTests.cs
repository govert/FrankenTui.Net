// Tests for .external/frankentui/crates/ftui-runtime/src/unified_evidence.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
//
// DIVERGENCE: 3 tests skipped — flush_to_sink (needs EvidenceSink),
// diff_strategy_evidence_format (needs ftui_render::diff_strategy),
// jsonl_roundtrip/jsonl_schema/jsonl_backward_compat (needs serde_json).

using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class UnifiedEvidenceTests
{
    private static EvidenceEntry MakeEntry(DecisionDomain domain, string action) => new()
    {
        TimestampNs = 1_000_000,
        Domain = domain,
        LogPosterior = 1.386,
        TopEvidence = new EvidenceTerm?[]
        {
            new("change_rate", 4.0),
            new("dirty_rows", 2.5),
            null,
        },
        Action = action,
        LossAvoided = 0.15,
        ConfidenceInterval = (0.72, 0.95),
    };

    [Fact] public void EmptyLedger()
    {
        var ledger = new UnifiedEvidenceLedger(100);
        Assert.True(ledger.IsEmpty);
        Assert.Equal(0, ledger.Count);
        Assert.Equal(0UL, ledger.TotalRecorded);
        Assert.Null(ledger.LastEntry());
    }

    [Fact] public void RecordSingle()
    {
        var ledger = new UnifiedEvidenceLedger(100);
        var id = ledger.Record(MakeEntry(DecisionDomain.DiffStrategy, "dirty_rows"));
        Assert.Equal(0UL, id);
        Assert.Equal(1, ledger.Count);
        Assert.Equal(1UL, ledger.TotalRecorded);
        Assert.Equal("dirty_rows", ledger.LastEntry()!.Action);
    }

    [Fact] public void RecordMultipleDomains()
    {
        var ledger = new UnifiedEvidenceLedger(100);
        ledger.Record(MakeEntry(DecisionDomain.DiffStrategy, "dirty_rows"));
        ledger.Record(MakeEntry(DecisionDomain.ResizeCoalescing, "coalesce"));
        ledger.Record(MakeEntry(DecisionDomain.HintRanking, "rank_3"));
        Assert.Equal(3, ledger.Count);
        Assert.Equal(1UL, ledger.DomainCount(DecisionDomain.DiffStrategy));
        Assert.Equal(1UL, ledger.DomainCount(DecisionDomain.ResizeCoalescing));
        Assert.Equal(1UL, ledger.DomainCount(DecisionDomain.HintRanking));
        Assert.Equal(0UL, ledger.DomainCount(DecisionDomain.FrameBudget));
    }

    [Fact] public void RingBufferWraps()
    {
        var ledger = new UnifiedEvidenceLedger(5);
        for (ulong i = 0; i < 10; i++)
        {
            var e = MakeEntry(DecisionDomain.DiffStrategy, "full");
            e.TimestampNs = i * 1000;
            ledger.Record(e);
        }
        Assert.Equal(5, ledger.Count);
        Assert.Equal(10UL, ledger.TotalRecorded);
        var ids = ledger.Entries().Select(e => e.DecisionId).ToList();
        Assert.Equal(new ulong[] { 5, 6, 7, 8, 9 }, ids);
    }

    [Fact] public void EntriesForDomain()
    {
        var ledger = new UnifiedEvidenceLedger(100);
        ledger.Record(MakeEntry(DecisionDomain.DiffStrategy, "full"));
        ledger.Record(MakeEntry(DecisionDomain.ResizeCoalescing, "apply"));
        ledger.Record(MakeEntry(DecisionDomain.DiffStrategy, "dirty_rows"));
        var actions = ledger.EntriesForDomain(DecisionDomain.DiffStrategy).Select(e => e.Action).ToList();
        Assert.Equal(new[] { "full", "dirty_rows" }, actions);
    }

    [Fact] public void LastEntryForDomain()
    {
        var ledger = new UnifiedEvidenceLedger(100);
        ledger.Record(MakeEntry(DecisionDomain.DiffStrategy, "full"));
        ledger.Record(MakeEntry(DecisionDomain.ResizeCoalescing, "apply"));
        ledger.Record(MakeEntry(DecisionDomain.DiffStrategy, "dirty_rows"));
        Assert.Equal("dirty_rows", ledger.LastEntryForDomain(DecisionDomain.DiffStrategy)!.Action);
        Assert.Equal("apply", ledger.LastEntryForDomain(DecisionDomain.ResizeCoalescing)!.Action);
        Assert.Null(ledger.LastEntryForDomain(DecisionDomain.FrameBudget));
    }

    [Fact] public void PosteriorProbability()
    {
        var entry = MakeEntry(DecisionDomain.DiffStrategy, "full");
        var prob = entry.PosteriorProbability();
        Assert.InRange(prob, 0.79, 0.81);
    }

    [Fact] public void PosteriorProbabilityExtremeLogOddsStaysFinite()
    {
        var high = MakeEntry(DecisionDomain.DiffStrategy, "full");
        high.LogPosterior = 1000.0;
        Assert.True(double.IsFinite(high.PosteriorProbability()));
        Assert.True(high.PosteriorProbability() > 0.999_999);

        var low = MakeEntry(DecisionDomain.DiffStrategy, "full");
        low.LogPosterior = -1000.0;
        Assert.True(double.IsFinite(low.PosteriorProbability()));
        Assert.True(low.PosteriorProbability() < 0.000_001);
    }

    [Fact] public void EvidenceCount()
    {
        var entry = MakeEntry(DecisionDomain.DiffStrategy, "full");
        Assert.Equal(2, entry.EvidenceCount);
    }

    [Fact] public void CombinedLogBf()
    {
        var entry = MakeEntry(DecisionDomain.DiffStrategy, "full");
        var expected = Math.Log(4.0) + Math.Log(2.5);
        Assert.InRange(entry.CombinedLogBf - expected, -1e-10, 1e-10);
    }

    [Fact] public void JsonlOutput()
    {
        var entry = MakeEntry(DecisionDomain.DiffStrategy, "dirty_rows");
        var jsonl = entry.ToJsonl();
        Assert.Contains("\"schema\":\"ftui-evidence-v2\"", jsonl);
        Assert.Contains("\"domain\":\"diff_strategy\"", jsonl);
        Assert.Contains("\"action\":\"dirty_rows\"", jsonl);
        Assert.Contains("\"change_rate\"", jsonl);
        Assert.Contains("\"bf\":4.0", jsonl);
        Assert.Contains("\"ci\":[", jsonl);
        Assert.DoesNotContain("\n", jsonl);
    }

    [Fact] public void ExportJsonl()
    {
        var ledger = new UnifiedEvidenceLedger(100);
        ledger.Record(MakeEntry(DecisionDomain.DiffStrategy, "full"));
        ledger.Record(MakeEntry(DecisionDomain.ResizeCoalescing, "apply"));
        var output = ledger.ExportJsonl();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Contains("diff_strategy", lines[0]);
        Assert.Contains("resize_coalescing", lines[1]);
    }

    [Fact] public void Clear()
    {
        var ledger = new UnifiedEvidenceLedger(100);
        ledger.Record(MakeEntry(DecisionDomain.DiffStrategy, "full"));
        ledger.Record(MakeEntry(DecisionDomain.DiffStrategy, "dirty_rows"));
        ledger.Clear();
        Assert.True(ledger.IsEmpty);
        Assert.Equal(2UL, ledger.TotalRecorded);
        Assert.Null(ledger.LastEntry());
    }

    [Fact] public void Summary()
    {
        var ledger = new UnifiedEvidenceLedger(100);
        for (int i = 0; i < 5; i++) ledger.Record(MakeEntry(DecisionDomain.DiffStrategy, "full"));
        for (int i = 0; i < 3; i++) ledger.Record(MakeEntry(DecisionDomain.HintRanking, "rank_1"));
        var s = ledger.Summary();
        Assert.Equal(8UL, s.TotalDecisions);
        Assert.Equal(8UL, s.StoredDecisions);
        Assert.Equal(2, s.Domains.Count);
        var diff = s.Domains.Single(d => d.Domain == DecisionDomain.DiffStrategy);
        Assert.Equal(5UL, diff.DecisionCount);
        Assert.True(diff.MeanPosterior > 0.0);
    }

    [Fact] public void SummaryMeanPosteriorIsFiniteForExtremeLogOdds()
    {
        var ledger = new UnifiedEvidenceLedger(10);
        var entry = MakeEntry(DecisionDomain.DiffStrategy, "full");
        entry.LogPosterior = 1000.0;
        ledger.Record(entry);
        ledger.Record(entry);
        var diff = ledger.Summary().Domains.Single(d => d.Domain == DecisionDomain.DiffStrategy);
        Assert.True(double.IsFinite(diff.MeanPosterior));
        Assert.True(diff.MeanPosterior > 0.999_999);
    }

    [Fact] public void BuilderSelectsTop3()
    {
        var entry = new EvidenceEntryBuilder(DecisionDomain.PaletteScoring, 0, 1000)
            .LogPosterior(2.0)
            .Evidence("match_type", 9.0)
            .Evidence("position", 1.5)
            .Evidence("word_boundary", 2.0)
            .Evidence("gap_penalty", 0.5)
            .Evidence("tag_match", 3.0)
            .Action("exact")
            .LossAvoided(0.8)
            .ConfidenceInterval(0.90, 0.99)
            .Build();
        Assert.Equal(3, entry.EvidenceCount);
        Assert.Equal("match_type", entry.TopEvidence[0]!.Label);
        Assert.Equal("tag_match", entry.TopEvidence[1]!.Label);
        var third = entry.TopEvidence[2]!.Label;
        Assert.True(third is "word_boundary" or "gap_penalty");
    }

    [Fact] public void BuilderFewerThan3()
    {
        var entry = new EvidenceEntryBuilder(DecisionDomain.FrameBudget, 0, 1000)
            .Evidence("frame_time", 2.0)
            .Action("hold")
            .Build();
        Assert.Equal(1, entry.EvidenceCount);
        Assert.Null(entry.TopEvidence[1]);
        Assert.Null(entry.TopEvidence[2]);
    }

    [Fact] public void DomainAllCoversSeven() => Assert.Equal(7, DecisionDomainMeta.All.Length);

    [Fact] public void DomainAsStrRoundtrip()
    {
        foreach (var d in DecisionDomainMeta.All)
        {
            var s = DecisionDomainMeta.AsStr(d);
            Assert.NotEmpty(s);
            Assert.True(s.All(c => char.IsAsciiLetterLower(c) || c == '_'));
        }
    }

    [Fact] public void MinimumCapacity()
    {
        var ledger = new UnifiedEvidenceLedger(0);
        ledger.Record(MakeEntry(DecisionDomain.DiffStrategy, "full"));
        Assert.Equal(1, ledger.Count);
        ledger.Record(MakeEntry(DecisionDomain.DiffStrategy, "dirty_rows"));
        Assert.Equal(1, ledger.Count);
        Assert.Equal("dirty_rows", ledger.LastEntry()!.Action);
    }

    [Fact] public void EntriesOrderBeforeWrap()
    {
        var ledger = new UnifiedEvidenceLedger(10);
        for (ulong i = 0; i < 5; i++)
        {
            var e = MakeEntry(DecisionDomain.DiffStrategy, "full");
            e.TimestampNs = i;
            ledger.Record(e);
        }
        var ids = ledger.Entries().Select(e => e.DecisionId).ToList();
        Assert.Equal(new ulong[] { 0, 1, 2, 3, 4 }, ids);
    }

    [Fact] public void EvidenceTermLogBf()
    {
        var term = new EvidenceTerm("test", 4.0);
        Assert.InRange(term.LogBf - Math.Log(4.0), -1e-10, 1e-10);
    }

    [Fact] public void LossAvoidedNonNegativeForOptimal()
    {
        var entry = MakeEntry(DecisionDomain.DiffStrategy, "full");
        Assert.True(entry.LossAvoided >= 0.0);
    }

    [Fact] public void ConfidenceIntervalBounds()
    {
        var entry = MakeEntry(DecisionDomain.DiffStrategy, "full");
        Assert.True(entry.ConfidenceInterval.Lower <= entry.ConfidenceInterval.Upper);
        Assert.True(entry.ConfidenceInterval.Lower >= 0.0);
        Assert.True(entry.ConfidenceInterval.Upper <= 1.0);
    }

    [Fact] public void SimulateMixedDomains()
    {
        var ledger = new UnifiedEvidenceLedger(10_000);
        var domains = DecisionDomainMeta.All;
        string[] actions = ["full", "coalesce", "hold", "degrade_1", "sample", "rank_1", "exact"];
        for (ulong i = 0; i < 1000; i++)
        {
            var domain = domains[i % 7];
            var action = actions[i % 7];
            var e = MakeEntry(domain, action);
            e.TimestampNs = i * 16_000;
            ledger.Record(e);
        }
        Assert.Equal(1000, ledger.Count);
        Assert.Equal(1000UL, ledger.TotalRecorded);
        foreach (var d in domains)
        {
            var count = ledger.DomainCount(d);
            Assert.True(count is 142 or 143, $"{d}: expected ~142, got {count}");
        }
        var jsonl = ledger.ExportJsonl();
        Assert.Equal(1000, jsonl.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
    }
}
