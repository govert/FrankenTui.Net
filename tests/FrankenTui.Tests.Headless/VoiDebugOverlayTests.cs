// Upstream source: .external/frankentui/crates/ftui-widgets/src/voi_debug_overlay.rs (tests module)
// Full 1-1 port of all upstream voi_debug_overlay tests.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Widgets;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class VoiDebugOverlayTests
{
    // ── Test helpers (mirrors Rust test helpers) ──────────────────────────

    private static VoiPosteriorSummary SamplePosterior() => new()
    {
        Alpha = 3.2,
        Beta = 7.4,
        Mean = 0.301,
        Variance = 0.0123,
        ExpectedVarianceAfter = 0.0101,
        VoiGain = 0.0022,
    };

    private static VoiOverlayData SampleData() => new()
    {
        Title = "VOI Overlay",
        Tick = 42,
        Source = "budget",
        Posterior = SamplePosterior(),
        Decision = new VoiDecisionSummary
        {
            EventIdx = 7,
            ShouldSample = true,
            Reason = "voi_gain > cost",
            Score = 0.123456,
            Cost = 0.045,
            LogBayesFactor = 0.437,
            EValue = 1.23,
            EThreshold = 0.95,
            BoundaryScore = 0.77,
        },
        Observation = new VoiObservationSummary
        {
            SampleIdx = 4,
            Violated = false,
            PosteriorMean = 0.312,
            Alpha = 3.9,
            Beta = 8.2,
        },
        Ledger = new List<VoiLedgerEntry>
        {
            new VoiLedgerEntry.Decision
            {
                EventIdx = 5,
                ShouldSample = true,
                VoiGain = 0.0042,
                LogBayesFactor = 0.31,
            },
            new VoiLedgerEntry.Observation
            {
                SampleIdx = 3,
                Violated = true,
                PosteriorMean = 0.4,
            },
        },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Fact]
    public void BuildLinesWithoutDecisionOrLedger()
    {
        var data = new VoiOverlayData
        {
            Title = "VOI",
            Tick = null,
            Source = null,
            Posterior = SamplePosterior(),
            Decision = null,
            Observation = null,
            Ledger = new List<VoiLedgerEntry>(),
        };
        var overlay = new VoiDebugOverlay(data);
        var lines = overlay.BuildLines(24);

        Assert.True(lines[0].Contains("VOI"), $"header missing title: {string.Join(", ", lines)}");
        Assert.Equal(24, lines[1].Length);
        Assert.True(lines.Any(line => line.Contains("Decision: —")),
            $"missing default decision line: {string.Join(", ", lines)}");
        Assert.True(lines.Any(line => line.Contains("Posterior Core")),
            $"missing posterior section: {string.Join(", ", lines)}");
        Assert.False(lines.Any(line => line.Contains("Evidence Ledger")),
            $"unexpected ledger section: {string.Join(", ", lines)}");
    }

    [Fact]
    public void BuildLinesWithDecisionAndObservation()
    {
        var overlay = new VoiDebugOverlay(SampleData());
        var lines = overlay.BuildLines(30);

        Assert.True(lines.Any(line => line.Contains("Decision: SAMPLE")),
            $"missing decision summary: {string.Join(", ", lines)}");
        Assert.True(lines.Any(line => line.Contains("Last Sample")),
            $"missing observation summary: {string.Join(", ", lines)}");
        Assert.True(lines.Any(line => line.Contains("Evidence Ledger")),
            $"missing ledger header: {string.Join(", ", lines)}");
        Assert.True(lines.Any(line => line.Contains("D#  5")),
            $"missing decision ledger entry: {string.Join(", ", lines)}");
        Assert.True(lines.Any(line => line.Contains("O#  3")),
            $"missing observation ledger entry: {string.Join(", ", lines)}");
    }

    [Fact]
    public void RenderAppliesBackgroundAndBorder()
    {
        var bg = PackedRgba.Rgb(12, 34, 56);
        var style = new VoiOverlayStyle
        {
            Background = bg,
        };
        var overlay = new VoiDebugOverlay(SampleData()).WithStyle(style);

        var pool = new GraphemePool();
        var frame = new Frame(80, 32, pool);
        var area = new Rect(0, 0, 80, 32);

        overlay.Render(area, frame);

        var topLeft = frame.Buffer.Get(0, 0)!.Value;
        Assert.Equal('╭', topLeft.Content.AsRune()!.Value.ToString()[0]);

        var inner = new Rect((ushort)(area.X + 1), (ushort)(area.Y + 1), (ushort)(area.Width - 2), (ushort)(area.Height - 2));
        var overlayLines = overlay.BuildLines((int)inner.Width - 2);
        ushort extraRow = (ushort)(inner.Y + (ushort)overlayLines.Count + 1);
        var bgCell = frame.Buffer.Get((ushort)(inner.X + 1), extraRow)!.Value;
        Assert.Equal(bg, bgCell.Background);
    }

    [Fact]
    public void RenderSmallAreaClearsPreviousContent()
    {
        var overlay = new VoiDebugOverlay(SampleData());
        var pool = new GraphemePool();
        var frame = new Frame(10, 4, pool);
        var sentinel = Cell.Empty.WithBackground(PackedRgba.Rgb(1, 2, 3)).WithChar('X');
        frame.Buffer.Fill(new Rect(0, 0, 10, 4), sentinel);

        overlay.Render(new Rect(0, 0, 10, 4), frame);

        Assert.Equal(' ', frame.Buffer.Get(0, 0)!.Value.Content.AsRune()!.Value.ToString()[0]);
        Assert.Equal(' ', frame.Buffer.Get(9, 3)!.Value.Content.AsRune()!.Value.ToString()[0]);
    }

    [Fact]
    public void RenderNoStylingDropsBackgroundFill()
    {
        var bg = PackedRgba.Rgb(12, 34, 56);
        var style = new VoiOverlayStyle
        {
            Background = bg,
        };
        var overlay = new VoiDebugOverlay(SampleData()).WithStyle(style);

        var pool = new GraphemePool();
        var frame = new Frame(80, 32, pool);
        frame.SetDegradation(DegradationLevel.NoStyling);
        var area = new Rect(0, 0, 80, 32);

        overlay.Render(area, frame);

        var bgCell = frame.Buffer.Get(2, 2)!.Value;
        var defaultCell = Cell.Empty;
        Assert.Equal(defaultCell.Background, bgCell.Background);
    }

    [Fact]
    public void RenderSkeletonClearsPreviousOverlay()
    {
        var overlay = new VoiDebugOverlay(SampleData());

        var pool = new GraphemePool();
        var frame = new Frame(80, 32, pool);
        overlay.Render(new Rect(0, 0, 80, 32), frame);
        frame.SetDegradation(DegradationLevel.Skeleton);
        var area = new Rect(0, 0, 80, 32);

        overlay.Render(area, frame);

        var defaultCell = Cell.Empty;
        var corner = frame.Buffer.Get(0, 0)!.Value;
        var inner = frame.Buffer.Get(10, 10)!.Value;
        Assert.Equal(' ', corner.Content.AsRune()!.Value.ToString()[0]);
        Assert.Equal(defaultCell.Foreground, corner.Foreground);
        Assert.Equal(defaultCell.Background, corner.Background);
        Assert.Equal(' ', inner.Content.AsRune()!.Value.ToString()[0]);
        Assert.Equal(defaultCell.Foreground, inner.Foreground);
        Assert.Equal(defaultCell.Background, inner.Background);
    }

    // --- Style defaults ---

    [Fact]
    public void OverlayStyleDefault()
    {
        var style = VoiOverlayStyle.Default;
        Assert.Null(style.Background);
        Assert.Equal(BorderType.Rounded, style.BorderType);
    }

    // --- Header formatting ---

    [Fact]
    public void BuildLinesHeaderWithTickAndSource()
    {
        var data = new VoiOverlayData
        {
            Title = "Test",
            Tick = 100,
            Source = "resize",
            Posterior = SamplePosterior(),
            Decision = null,
            Observation = null,
            Ledger = new List<VoiLedgerEntry>(),
        };
        var overlay = new VoiDebugOverlay(data);
        var lines = overlay.BuildLines(40);
        Assert.Contains("Test (tick 100) [resize]", lines[0]);
    }

    [Fact]
    public void BuildLinesHeaderNoTickNoSource()
    {
        var data = new VoiOverlayData
        {
            Title = "Plain",
            Tick = null,
            Source = null,
            Posterior = SamplePosterior(),
            Decision = null,
            Observation = null,
            Ledger = new List<VoiLedgerEntry>(),
        };
        var overlay = new VoiDebugOverlay(data);
        var lines = overlay.BuildLines(20);
        Assert.Equal("Plain", lines[0]);
    }

    // --- Decision verdict ---

    [Fact]
    public void BuildLinesSkipVerdict()
    {
        var data = new VoiOverlayData
        {
            Title = "Test",
            Tick = null,
            Source = null,
            Posterior = SamplePosterior(),
            Decision = new VoiDecisionSummary
            {
                EventIdx = 1,
                ShouldSample = false,
                Reason = "cost_too_high",
                Score = 0.01,
                Cost = 0.1,
                LogBayesFactor = -1.0,
                EValue = 0.5,
                EThreshold = 0.95,
                BoundaryScore = 0.2,
            },
            Observation = null,
            Ledger = new List<VoiLedgerEntry>(),
        };
        var overlay = new VoiDebugOverlay(data);
        var lines = overlay.BuildLines(40);
        Assert.True(lines.Any(l => l.Contains("Decision: SKIP")),
            $"expected SKIP verdict: {string.Join(", ", lines)}");
    }

    // --- Observation only (no decision) ---

    [Fact]
    public void BuildLinesObservationOnly()
    {
        var data = new VoiOverlayData
        {
            Title = "T",
            Tick = null,
            Source = null,
            Posterior = SamplePosterior(),
            Decision = null,
            Observation = new VoiObservationSummary
            {
                SampleIdx = 10,
                Violated = true,
                PosteriorMean = 0.456,
                Alpha = 5.0,
                Beta = 10.0,
            },
            Ledger = new List<VoiLedgerEntry>(),
        };
        var overlay = new VoiDebugOverlay(data);
        var lines = overlay.BuildLines(40);
        Assert.True(lines.Any(l => l.Contains("violated: true")),
            $"missing violated observation: {string.Join(", ", lines)}");
        Assert.True(lines.Any(l => l.Contains("mu=0.456")),
            $"missing posterior mean: {string.Join(", ", lines)}");
    }

    // --- Ledger formatting ---

    [Fact]
    public void BuildLinesLedgerSkipEntry()
    {
        var data = new VoiOverlayData
        {
            Title = "T",
            Tick = null,
            Source = null,
            Posterior = SamplePosterior(),
            Decision = null,
            Observation = null,
            Ledger = new List<VoiLedgerEntry>
            {
                new VoiLedgerEntry.Decision
                {
                    EventIdx = 99,
                    ShouldSample = false,
                    VoiGain = 0.001,
                    LogBayesFactor = -0.5,
                },
            },
        };
        var overlay = new VoiDebugOverlay(data);
        var lines = overlay.BuildLines(40);
        Assert.True(lines.Any(l => l.Contains("D# 99 -")),
            $"expected skip marker: {string.Join(", ", lines)}");
    }

    // --- Posterior formatting ---

    [Fact]
    public void BuildLinesPosteriorValues()
    {
        var data = new VoiOverlayData
        {
            Title = "T",
            Tick = null,
            Source = null,
            Posterior = new VoiPosteriorSummary
            {
                Alpha = 1.0,
                Beta = 1.0,
                Mean = 0.5,
                Variance = 0.0833,
                ExpectedVarianceAfter = 0.0500,
                VoiGain = 0.0333,
            },
            Decision = null,
            Observation = null,
            Ledger = new List<VoiLedgerEntry>(),
        };
        var overlay = new VoiDebugOverlay(data);
        var lines = overlay.BuildLines(40);
        Assert.True(lines.Any(l => l.Contains("a=1.00") && l.Contains("b=1.00")),
            $"missing alpha/beta: {string.Join(", ", lines)}");
        Assert.True(lines.Any(l => l.Contains("mu=0.5000")),
            $"missing mean: {string.Join(", ", lines)}");
    }

    // --- with_style builder ---

    [Fact]
    public void WithStyleReplacesStyle()
    {
        var overlay = new VoiDebugOverlay(SampleData());
        var custom = new VoiOverlayStyle
        {
            Background = PackedRgba.Rgb(255, 0, 0),
            BorderType = BorderType.Square,
        };
        var styled = overlay.WithStyle(custom);
        Assert.Equal(PackedRgba.Rgb(255, 0, 0), styled.StyleField.Background);
    }

    [Fact]
    public void RenderEmptyAreaIsNoop()
    {
        var overlay = new VoiDebugOverlay(SampleData());
        var pool = new GraphemePool();
        var frame = new Frame(40, 10, pool);

        // Zero-width area
        overlay.Render(new Rect(0, 0, 0, 10), frame);
        // Zero-height area
        overlay.Render(new Rect(0, 0, 40, 0), frame);
        // Both zero — should not panic
    }

    [Fact]
    public void RenderNarrowAreaWhereInnerIsEmpty()
    {
        var overlay = new VoiDebugOverlay(SampleData());
        var pool = new GraphemePool();
        var frame = new Frame(80, 40, pool);
        // Area has borders consuming all space (width=20 passes threshold, height=6 passes)
        // Inner is 18x4 which is non-empty, so use exactly at threshold
        overlay.Render(new Rect(0, 0, 20, 6), frame);
        // Should render without panic
    }

    [Fact]
    public void BuildLinesLedgerObservationEntry()
    {
        var data = new VoiOverlayData
        {
            Title = "T",
            Tick = null,
            Source = null,
            Posterior = SamplePosterior(),
            Decision = null,
            Observation = null,
            Ledger = new List<VoiLedgerEntry>
            {
                new VoiLedgerEntry.Observation
                {
                    SampleIdx = 42,
                    Violated = false,
                    PosteriorMean = 0.789,
                },
            },
        };
        var overlay = new VoiDebugOverlay(data);
        var lines = overlay.BuildLines(40);
        Assert.True(lines.Any(l => l.Contains("O# 42")),
            $"missing observation ledger entry: {string.Join(", ", lines)}");
        Assert.True(lines.Any(l => l.Contains("viol=false")),
            $"missing violated=false: {string.Join(", ", lines)}");
        Assert.True(lines.Any(l => l.Contains("mu=0.789")),
            $"missing posterior mean in ledger: {string.Join(", ", lines)}");
    }

    [Fact]
    public void BuildLinesDecisionEquationSection()
    {
        var overlay = new VoiDebugOverlay(SampleData());
        var lines = overlay.BuildLines(50);
        Assert.True(lines.Any(l => l.Contains("Decision Equation")),
            $"missing decision equation header: {string.Join(", ", lines)}");
        Assert.True(lines.Any(l => l.Contains("score=") && l.Contains("cost=")),
            $"missing score/cost line: {string.Join(", ", lines)}");
    }

    [Fact]
    public void BuildLinesVoiEquationFormat()
    {
        var data = new VoiOverlayData
        {
            Title = "T",
            Tick = null,
            Source = null,
            Posterior = new VoiPosteriorSummary
            {
                Alpha = 2.0,
                Beta = 3.0,
                Mean = 0.4,
                Variance = 0.04,
                ExpectedVarianceAfter = 0.03,
                VoiGain = 0.01,
            },
            Decision = null,
            Observation = null,
            Ledger = new List<VoiLedgerEntry>(),
        };
        var overlay = new VoiDebugOverlay(data);
        var lines = overlay.BuildLines(50);
        // VOI = Var[p] - E[Var|1] header
        Assert.True(lines.Any(l => l.Contains("VOI = Var[p] - E[Var|1]")),
            $"missing VOI equation label: {string.Join(", ", lines)}");
        // VOI = 0.040000 - 0.030000 = 0.010000
        Assert.True(lines.Any(l => l.Contains("0.040000") && l.Contains("0.030000") && l.Contains("0.010000")),
            $"missing VOI computation line: {string.Join(", ", lines)}");
    }

    [Fact]
    public void OverlayDataClone()
    {
        var data = SampleData();
        // C# uses reference semantics; test that a copy of the data object has identical field values.
        // DIVERGENCE: Rust derives Clone; C# record-like copy is via object initializer or manual copy.
        // We verify field-by-field equivalence rather than structural clone.
        var cloned = new VoiOverlayData
        {
            Title = data.Title,
            Tick = data.Tick,
            Source = data.Source,
            Posterior = data.Posterior,
            Decision = data.Decision,
            Observation = data.Observation,
            Ledger = new List<VoiLedgerEntry>(data.Ledger),
        };
        Assert.Equal(cloned.Title, data.Title);
        Assert.Equal(cloned.Tick, data.Tick);
        Assert.Equal(cloned.Ledger.Count, data.Ledger.Count);
    }

    // ─── Edge-case tests (bd-3szd1) ────────────────────────────────────

    [Fact]
    public void BuildLinesWidthZero()
    {
        var overlay = new VoiDebugOverlay(SampleData());
        var lines = overlay.BuildLines(0);
        // Should not panic; divider is empty string
        Assert.True(lines.Count > 0);
    }

    [Fact]
    public void BuildLinesWidthOne()
    {
        var overlay = new VoiDebugOverlay(SampleData());
        var lines = overlay.BuildLines(1);
        Assert.Equal("-", lines[1]);
    }

    [Fact]
    public void BuildLinesEmptyTitle()
    {
        var data = new VoiOverlayData
        {
            Title = "",
            Tick = null,
            Source = null,
            Posterior = SamplePosterior(),
            Decision = null,
            Observation = null,
            Ledger = new List<VoiLedgerEntry>(),
        };
        var overlay = new VoiDebugOverlay(data);
        var lines = overlay.BuildLines(20);
        Assert.Equal("", lines[0]);
    }

    [Fact]
    public void BuildLinesTickOnlyNoSource()
    {
        var data = new VoiOverlayData
        {
            Title = "T",
            Tick = 0,
            Source = null,
            Posterior = SamplePosterior(),
            Decision = null,
            Observation = null,
            Ledger = new List<VoiLedgerEntry>(),
        };
        var overlay = new VoiDebugOverlay(data);
        var lines = overlay.BuildLines(30);
        Assert.Contains("(tick 0)", lines[0]);
        Assert.DoesNotContain('[', lines[0]);
    }

    [Fact]
    public void BuildLinesSourceOnlyNoTick()
    {
        var data = new VoiOverlayData
        {
            Title = "T",
            Tick = null,
            Source = "src",
            Posterior = SamplePosterior(),
            Decision = null,
            Observation = null,
            Ledger = new List<VoiLedgerEntry>(),
        };
        var overlay = new VoiDebugOverlay(data);
        var lines = overlay.BuildLines(30);
        Assert.Contains("[src]", lines[0]);
        Assert.DoesNotContain("tick", lines[0]);
    }

    [Fact]
    public void RenderWidthBelowThreshold()
    {
        var overlay = new VoiDebugOverlay(SampleData());
        var pool = new GraphemePool();
        var frame = new Frame(80, 40, pool);
        // width=19 is below the 20 threshold
        overlay.Render(new Rect(0, 0, 19, 10), frame);
        // Should be noop — verify no border rendered
        var cell = frame.Buffer.Get(0, 0)!.Value;
        Assert.NotEqual('╭', cell.Content.AsRune()?.ToString()[0] ?? ' ');
    }

    [Fact]
    public void RenderHeightBelowThreshold()
    {
        var overlay = new VoiDebugOverlay(SampleData());
        var pool = new GraphemePool();
        var frame = new Frame(80, 40, pool);
        // height=5 is below the 6 threshold
        overlay.Render(new Rect(0, 0, 40, 5), frame);
        var cell = frame.Buffer.Get(0, 0)!.Value;
        Assert.NotEqual('╭', cell.Content.AsRune()?.ToString()[0] ?? ' ');
    }

    [Fact]
    public void RenderExactMinimumSize()
    {
        var overlay = new VoiDebugOverlay(SampleData());
        var pool = new GraphemePool();
        var frame = new Frame(80, 40, pool);
        // Exactly at threshold: width=20, height=6
        overlay.Render(new Rect(0, 0, 20, 6), frame);
        var cell = frame.Buffer.Get(0, 0)!.Value;
        Assert.Equal('╭', cell.Content.AsRune()!.Value.ToString()[0]);
    }

    [Fact]
    public void PosteriorWithNanValues()
    {
        var data = new VoiOverlayData
        {
            Title = "T",
            Tick = null,
            Source = null,
            Posterior = new VoiPosteriorSummary
            {
                Alpha = double.NaN,
                Beta = double.PositiveInfinity,
                Mean = double.NegativeInfinity,
                Variance = 0.0,
                ExpectedVarianceAfter = 0.0,
                VoiGain = -0.0,
            },
            Decision = null,
            Observation = null,
            Ledger = new List<VoiLedgerEntry>(),
        };
        var overlay = new VoiDebugOverlay(data);
        var lines = overlay.BuildLines(50);
        // Should format without panic
        Assert.True(lines.Any(l => l.Contains("NaN") || l.Contains("nan")),
            $"NaN alpha should appear in output: {string.Join(", ", lines)}");
    }

    [Fact]
    public void LargeEventIdxInLedger()
    {
        var data = new VoiOverlayData
        {
            Title = "T",
            Tick = null,
            Source = null,
            Posterior = SamplePosterior(),
            Decision = null,
            Observation = null,
            Ledger = new List<VoiLedgerEntry>
            {
                new VoiLedgerEntry.Decision
                {
                    EventIdx = ulong.MaxValue,
                    ShouldSample = true,
                    VoiGain = 0.0,
                    LogBayesFactor = 0.0,
                },
            },
        };
        var overlay = new VoiDebugOverlay(data);
        var lines = overlay.BuildLines(80);
        Assert.True(lines.Any(l => l.Contains(ulong.MaxValue.ToString())),
            $"large event_idx should appear: {string.Join(", ", lines)}");
    }

    [Fact]
    public void MultipleLedgerEntriesSameType()
    {
        var data = new VoiOverlayData
        {
            Title = "T",
            Tick = null,
            Source = null,
            Posterior = SamplePosterior(),
            Decision = null,
            Observation = null,
            Ledger = new List<VoiLedgerEntry>
            {
                new VoiLedgerEntry.Decision { EventIdx = 1, ShouldSample = true, VoiGain = 0.01, LogBayesFactor = 0.5 },
                new VoiLedgerEntry.Decision { EventIdx = 2, ShouldSample = false, VoiGain = 0.001, LogBayesFactor = -0.3 },
                new VoiLedgerEntry.Decision { EventIdx = 3, ShouldSample = true, VoiGain = 0.02, LogBayesFactor = 1.0 },
            },
        };
        var overlay = new VoiDebugOverlay(data);
        var lines = overlay.BuildLines(50);
        var decisionLines = lines.Where(l => l.StartsWith("D#")).ToList();
        Assert.Equal(3, decisionLines.Count);
    }

    [Fact]
    public void NegativeLogBayesFactorFormat()
    {
        var data = new VoiOverlayData
        {
            Title = "T",
            Tick = null,
            Source = null,
            Posterior = SamplePosterior(),
            Decision = new VoiDecisionSummary
            {
                EventIdx = 1,
                ShouldSample = false,
                Reason = "negative",
                Score = 0.001,
                Cost = 0.1,
                LogBayesFactor = -2.345,
                EValue = 0.1,
                EThreshold = 0.95,
                BoundaryScore = 0.05,
            },
            Observation = null,
            Ledger = new List<VoiLedgerEntry>(),
        };
        var overlay = new VoiDebugOverlay(data);
        var lines = overlay.BuildLines(50);
        Assert.True(lines.Any(l => l.Contains("-2.345")),
            $"negative log BF should appear: {string.Join(", ", lines)}");
    }

    [Fact]
    public void VoiLedgerEntryClone()
    {
        // DIVERGENCE: Rust derives Clone on enum variants; C# uses reference semantics.
        // We verify the variant type and field identity instead of structural clone.
        var entry = new VoiLedgerEntry.Decision
        {
            EventIdx = 5,
            ShouldSample = true,
            VoiGain = 0.01,
            LogBayesFactor = 0.5,
        };
        // Type is a Decision variant
        Assert.IsType<VoiLedgerEntry.Decision>(entry);
        Assert.Equal(5UL, entry.EventIdx);
    }

    [Fact]
    public void VoiDecisionSummaryClone()
    {
        // DIVERGENCE: Rust derives Clone; C# uses init-only properties on a sealed class.
        // We verify field round-trip via an explicit copy.
        var d = new VoiDecisionSummary
        {
            EventIdx = 1,
            ShouldSample = true,
            Reason = "test",
            Score = 1.0,
            Cost = 0.5,
            LogBayesFactor = 0.3,
            EValue = 1.0,
            EThreshold = 0.95,
            BoundaryScore = 0.5,
        };
        var cloned = new VoiDecisionSummary
        {
            EventIdx = d.EventIdx,
            ShouldSample = d.ShouldSample,
            Reason = d.Reason,
            Score = d.Score,
            Cost = d.Cost,
            LogBayesFactor = d.LogBayesFactor,
            EValue = d.EValue,
            EThreshold = d.EThreshold,
            BoundaryScore = d.BoundaryScore,
        };
        Assert.Equal("test", cloned.Reason);
        Assert.Equal(1UL, cloned.EventIdx);
    }

    [Fact]
    public void VoiObservationSummaryClone()
    {
        // DIVERGENCE: Rust derives Clone; C# uses init-only properties on a sealed class.
        var o = new VoiObservationSummary
        {
            SampleIdx = 42,
            Violated = true,
            PosteriorMean = 0.5,
            Alpha = 3.0,
            Beta = 7.0,
        };
        var cloned = new VoiObservationSummary
        {
            SampleIdx = o.SampleIdx,
            Violated = o.Violated,
            PosteriorMean = o.PosteriorMean,
            Alpha = o.Alpha,
            Beta = o.Beta,
        };
        Assert.True(cloned.Violated);
        Assert.Equal(42UL, cloned.SampleIdx);
    }

    [Fact]
    public void WithStyleCustomBorderType()
    {
        var overlay = new VoiDebugOverlay(SampleData()).WithStyle(new VoiOverlayStyle
        {
            BorderType = BorderType.Double,
        });
        Assert.Equal(BorderType.Double, overlay.StyleField.BorderType);
    }

    [Fact]
    public void RenderNoBackground()
    {
        var data = SampleData();
        var overlay = new VoiDebugOverlay(data);
        var pool = new GraphemePool();
        var frame = new Frame(80, 32, pool);
        // Default style has no background
        overlay.Render(new Rect(0, 0, 80, 32), frame);
        // Should render border without panic
        var cell = frame.Buffer.Get(0, 0)!.Value;
        Assert.Equal('╭', cell.Content.AsRune()!.Value.ToString()[0]);
    }

    [Fact]
    public void BuildLinesDividerMatchesWidth()
    {
        var overlay = new VoiDebugOverlay(SampleData());
        const int width = 37;
        var lines = overlay.BuildLines(width);
        // line[1] is the first divider
        Assert.Equal(width, lines[1].Length);
    }

    // ─── End edge-case tests (bd-3szd1) ──────────────────────────────

    // --- Struct Debug impls ---

    [Fact]
    public void StructsImplementDebug()
    {
        // DIVERGENCE: Rust derives Debug; C# uses ToString(). We verify objects are constructible
        // and can be referenced without exceptions, which is the behavioral equivalent.
        var posterior = SamplePosterior();
        _ = posterior.ToString();

        var data = SampleData();
        _ = data.ToString();

        var overlay = new VoiDebugOverlay(data);
        _ = overlay.ToString();

        var style = VoiOverlayStyle.Default;
        _ = style.ToString();
    }
}
