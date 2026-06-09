// Upstream source: crates/ftui-widgets/src/drift_visualization.rs (tests module)
// Full 1-1 port of all upstream drift_visualization tests.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class DriftVisualizationTests
{
    // ── Test helpers (mirrors Rust test helpers) ──────────────────────────

    private static DriftSnapshot MakeSnapshot(ulong frameId, double confidence, bool inFallback)
    {
        return new DriftSnapshot
        {
            FrameId = frameId,
            Domains =
            [
                new DomainSnapshot
                {
                    Domain = DecisionDomain.DiffStrategy,
                    Confidence = confidence,
                    Signal = confidence >= 0.7 ? TrafficLight.Green
                           : confidence >= 0.3 ? TrafficLight.Yellow
                           : TrafficLight.Red,
                    InFallback = inFallback,
                    RegimeLabel = inFallback ? "deterministic" : "bayesian",
                },
                new DomainSnapshot
                {
                    Domain = DecisionDomain.ResizeCoalescing,
                    Confidence = confidence * 0.9,
                    Signal = TrafficLight.Green,
                    InFallback = false,
                    RegimeLabel = "bayesian",
                },
            ],
        };
    }

    private static DriftTimeline MakeDriftTimeline()
    {
        var tl = new DriftTimeline(60);
        // Normal operation (frames 0-29)
        for (ulong i = 0; i < 30; i++)
            tl.Push(MakeSnapshot(i, 0.85, false));
        // Drift onset (frames 30-39)
        for (ulong i = 30; i < 40; i++)
        {
            double conf = 0.85 - (i - 30) * 0.07;
            tl.Push(MakeSnapshot(i, conf, false));
        }
        // Fallback trigger (frames 40-49)
        for (ulong i = 40; i < 50; i++)
            tl.Push(MakeSnapshot(i, 0.15, true));
        // Recovery (frames 50-59)
        for (ulong i = 50; i < 60; i++)
        {
            double conf = 0.15 + (i - 50) * 0.07;
            tl.Push(MakeSnapshot(i, conf, false));
        }
        return tl;
    }

    // ── Timeline tests ────────────────────────────────────────────────────

    [Fact]
    public void TimelinePushAndLen()
    {
        var tl = new DriftTimeline(10);
        Assert.True(tl.IsEmpty());
        Assert.Equal(0, tl.Len());

        tl.Push(MakeSnapshot(0, 0.8, false));
        Assert.Equal(1, tl.Len());
        Assert.False(tl.IsEmpty());
    }

    [Fact]
    public void TimelineWrapsAtCapacity()
    {
        var tl = new DriftTimeline(5);
        for (ulong i = 0; i < 10; i++)
            tl.Push(MakeSnapshot(i, 0.5, false));
        Assert.Equal(5, tl.Len());

        // Latest should be frame 9
        Assert.Equal(9UL, tl.Latest()!.FrameId);
    }

    [Fact]
    public void TimelineChronologicalOrder()
    {
        var tl = new DriftTimeline(5);
        for (ulong i = 0; i < 8; i++)
            tl.Push(MakeSnapshot(i, 0.5, false));
        var ids = tl.IterChronological().Select(s => s.FrameId).ToList();
        Assert.Equal([3UL, 4UL, 5UL, 6UL, 7UL], ids);
    }

    [Fact]
    public void ConfidenceSeriesExtraction()
    {
        var tl = MakeDriftTimeline();
        var series = tl.ConfidenceSeries(DecisionDomain.DiffStrategy);
        Assert.Equal(60, series.Count);
        // First value should be ~0.85 (normal)
        Assert.True(Math.Abs(series[0] - 0.85) < 0.01);
        // At frame 40: should be 0.15 (fallback)
        Assert.True(Math.Abs(series[40] - 0.15) < 0.01);
    }

    [Fact]
    public void FallbackTriggerDetection()
    {
        var tl = MakeDriftTimeline();
        var trigger = tl.LastFallbackTrigger(DecisionDomain.DiffStrategy);
        // First fallback entry is at index 40
        Assert.Equal(40, trigger);
    }

    [Fact]
    public void NoFallbackTriggerWhenNone()
    {
        var tl = new DriftTimeline(10);
        for (ulong i = 0; i < 10; i++)
            tl.Push(MakeSnapshot(i, 0.8, false));
        Assert.Null(tl.LastFallbackTrigger(DecisionDomain.DiffStrategy));
    }

    [Fact]
    public void FallbackTriggerAtStartOfVisibleTimeline()
    {
        var tl = new DriftTimeline(5);
        for (ulong i = 0; i < 5; i++)
            tl.Push(MakeSnapshot(i, 0.15, true));

        Assert.Equal(0, tl.LastFallbackTrigger(DecisionDomain.DiffStrategy));
    }

    // ── Render tests ──────────────────────────────────────────────────────

    [Fact]
    public void RenderEmptyTimeline()
    {
        var tl = new DriftTimeline(60);
        var viz = new DriftVisualization(tl);
        var pool = new GraphemePool();
        var frame = new Frame(80, 24, pool);
        viz.Render(new Rect(0, 0, 80, 24), frame);
        // Should not panic
    }

    [Fact]
    public void RenderPopulatedTimeline()
    {
        var tl = MakeDriftTimeline();
        var viz = new DriftVisualization(tl);
        var pool = new GraphemePool();
        var frame = new Frame(80, 20, pool);
        viz.Render(new Rect(0, 0, 80, 20), frame);

        // Check title is present
        bool foundTitle = false;
        for (ushort x = 0; x < 80; x++)
        {
            var cell = frame.Buffer.Get(x, 1);
            if (cell is { } c && c.Content.AsChar() == 'D')
            {
                foundTitle = true;
                break;
            }
        }
        Assert.True(foundTitle, "should render title row");
    }

    [Fact]
    public void RenderNoStylingDropsBorderAndLabelStyles()
    {
        var tl = MakeDriftTimeline();
        var viz = new DriftVisualization(tl);
        var pool = new GraphemePool();
        var frame = new Frame(80, 20, pool);
        frame.SetDegradation(DegradationLevel.NoStyling);

        viz.Render(new Rect(0, 0, 80, 20), frame);

        var border = frame.Buffer.Get(0, 0)!.Value;
        var borderDefault = Cell.FromChar(border.Content.AsChar() ?? ' ');
        Assert.Equal(border.Foreground, borderDefault.Foreground);
        Assert.Equal(border.Background, borderDefault.Background);
        Assert.Equal(border.Attributes, borderDefault.Attributes);

        var title = frame.Buffer.Get(1, 1)!.Value;
        var titleDefault = Cell.FromChar('D');
        Assert.Equal('D', title.Content.AsChar());
        Assert.Equal(title.Foreground, titleDefault.Foreground);
        Assert.Equal(title.Background, titleDefault.Background);
        Assert.Equal(title.Attributes, titleDefault.Attributes);

        var label = frame.Buffer.Get(1, 2)!.Value;
        var labelDefault = Cell.FromChar('d');
        Assert.Equal('d', label.Content.AsChar());
        Assert.Equal(label.Foreground, labelDefault.Foreground);
        Assert.Equal(label.Background, labelDefault.Background);
        Assert.Equal(label.Attributes, labelDefault.Attributes);
    }

    [Fact]
    public void RenderShowsFallbackIndicator()
    {
        var tl = MakeDriftTimeline();
        var viz = new DriftVisualization(tl).Domains([DecisionDomain.DiffStrategy]);
        var pool = new GraphemePool();
        var frame = new Frame(80, 12, pool);
        viz.Render(new Rect(0, 0, 80, 12), frame);

        // Check that FALLBACK text appears (latest snapshot has in_fallback=false
        // after recovery, so we won't see FALLBACK badge on the label row)
        // but the sparkline should show the trigger marker
    }

    [Fact]
    public void RenderRegimeBannerInFallback()
    {
        // Create timeline where latest is in fallback
        var tl = new DriftTimeline(10);
        for (ulong i = 0; i < 10; i++)
            tl.Push(MakeSnapshot(i, 0.15, true));
        var viz = new DriftVisualization(tl);
        var pool = new GraphemePool();
        var frame = new Frame(80, 12, pool);
        viz.Render(new Rect(0, 0, 80, 12), frame);

        // Should contain "REGIME" text
        bool foundRegime = false;
        for (ushort y = 0; y < 12; y++)
        {
            var row = new System.Text.StringBuilder();
            for (ushort x = 0; x < 80; x++)
            {
                var cell = frame.Buffer.Get(x, y);
                if (cell is { } c)
                {
                    char? ch = c.Content.AsChar();
                    if (ch.HasValue)
                        row.Append(ch.Value);
                }
            }
            if (row.ToString().Contains("REGIME"))
            {
                foundRegime = true;
                break;
            }
        }
        Assert.True(foundRegime, "should show regime banner when in fallback");
    }

    [Fact]
    public void RenderRegimeBannerNormal()
    {
        var tl = new DriftTimeline(10);
        for (ulong i = 0; i < 10; i++)
            tl.Push(MakeSnapshot(i, 0.85, false));
        var viz = new DriftVisualization(tl);
        var pool = new GraphemePool();
        var frame = new Frame(80, 12, pool);
        viz.Render(new Rect(0, 0, 80, 12), frame);

        // Should contain "Bayesian (normal)" text
        bool foundNormal = false;
        for (ushort y = 0; y < 12; y++)
        {
            var row = new System.Text.StringBuilder();
            for (ushort x = 0; x < 80; x++)
            {
                var cell = frame.Buffer.Get(x, y);
                if (cell is { } c)
                {
                    char? ch = c.Content.AsChar();
                    if (ch.HasValue)
                        row.Append(ch.Value);
                }
            }
            if (row.ToString().Contains("Bayesian"))
            {
                foundNormal = true;
                break;
            }
        }
        Assert.True(foundNormal, "should show normal regime banner");
    }

    [Fact]
    public void TinyAreaNoPanic()
    {
        var tl = MakeDriftTimeline();
        var viz = new DriftVisualization(tl);
        var pool = new GraphemePool();
        var frame = new Frame(5, 3, pool);
        // Should not panic with tiny area
        viz.Render(new Rect(0, 0, 5, 3), frame);
    }

    [Fact]
    public void MinHeightCalculation()
    {
        var tl = new DriftTimeline(5);
        tl.Push(MakeSnapshot(0, 0.8, false)); // 2 domains
        var viz = new DriftVisualization(tl);
        // border_top + title + 2*domain_rows + banner + border_bottom
        // = 1 + 1 + 2*2 + 1 + 1 = 8
        Assert.Equal(8, viz.MinHeight());
    }

    [Fact]
    public void MinHeightNoBanner()
    {
        var tl = new DriftTimeline(5);
        tl.Push(MakeSnapshot(0, 0.8, false));
        var viz = new DriftVisualization(tl).ShowRegimeBanner(false);
        Assert.Equal(7, viz.MinHeight());
    }

    [Fact]
    public void ConfidenceColorZones()
    {
        var tl = new DriftTimeline(1);
        var viz = new DriftVisualization(tl);

        var green = viz.ConfidenceColor(0.9);
        Assert.Equal(DriftColors.ZoneGreen, green);

        var redIsh = viz.ConfidenceColor(0.1);
        // Should be near ZONE_RED
        Assert.True(redIsh.R > 100);
        Assert.True(redIsh.G < 100);
    }

    [Fact]
    public void ConfidenceColorHandlesDegenerateThresholds()
    {
        var tl = new DriftTimeline(1);
        var viz = new DriftVisualization(tl)
            .FallbackThreshold(0.0)
            .CautionThreshold(0.0);

        Assert.Equal(DriftColors.ZoneGreen, viz.ConfidenceColor(0.0));
        var low = viz.ConfidenceColor(-1.0);
        Assert.True(low.R >= DriftColors.ZoneRed.R);
    }

    [Fact]
    public void BuilderChain()
    {
        var tl = new DriftTimeline(10);
        var viz = new DriftVisualization(tl)
            .WithBorderType(BorderType.Double)
            .WithStyle(new WidgetStyle(null, PackedRgba.Rgba(10, 10, 10, 255), null))
            .ShowRegimeBanner(false)
            .FallbackThreshold(0.2)
            .CautionThreshold(0.8)
            .Domains([DecisionDomain.DiffStrategy]);

        var pool = new GraphemePool();
        var frame = new Frame(80, 24, pool);
        viz.Render(new Rect(0, 0, 80, 24), frame);
    }

    [Fact]
    public void IsNotEssential()
    {
        var tl = new DriftTimeline(1);
        var viz = new DriftVisualization(tl);
        Assert.False(viz.IsEssential());
    }

    [Fact]
    public void LerpColorEndpoints()
    {
        var a = PackedRgba.Rgba(0, 0, 0, 255);
        var b = PackedRgba.Rgba(255, 255, 255, 255);
        Assert.Equal(a, DriftHelpers.LerpColor(a, b, 0.0));
        Assert.Equal(b, DriftHelpers.LerpColor(a, b, 1.0));
    }

    [Fact]
    public void LerpColorClamps()
    {
        var a = PackedRgba.Rgba(0, 0, 0, 255);
        var b = PackedRgba.Rgba(255, 255, 255, 255);
        Assert.Equal(a, DriftHelpers.LerpColor(a, b, -1.0));
        Assert.Equal(b, DriftHelpers.LerpColor(a, b, 2.0));
    }

    [Fact]
    public void RenderWithSingleDomainFilter()
    {
        var tl = MakeDriftTimeline();
        var viz = new DriftVisualization(tl).Domains([DecisionDomain.DiffStrategy]);
        var pool = new GraphemePool();
        var frame = new Frame(80, 10, pool);
        viz.Render(new Rect(0, 0, 80, 10), frame);

        // Should render DiffStrategy label
        bool foundDiff = false;
        for (ushort y = 0; y < 10; y++)
        {
            var row = new System.Text.StringBuilder();
            for (ushort x = 0; x < 80; x++)
            {
                var cell = frame.Buffer.Get(x, y);
                if (cell is { } c)
                {
                    char? ch = c.Content.AsChar();
                    if (ch.HasValue)
                        row.Append(ch.Value);
                }
            }
            if (row.ToString().Contains("diff_strategy"))
            {
                foundDiff = true;
                break;
            }
        }
        Assert.True(foundDiff, "should show DiffStrategy domain");
    }

    [Fact]
    public void RenderFallbackBadgeOnLabelRow()
    {
        var tl = new DriftTimeline(5);
        for (ulong i = 0; i < 5; i++)
            tl.Push(MakeSnapshot(i, 0.1, true));
        var viz = new DriftVisualization(tl).Domains([DecisionDomain.DiffStrategy]);
        var pool = new GraphemePool();
        var frame = new Frame(80, 10, pool);
        viz.Render(new Rect(0, 0, 80, 10), frame);

        bool foundFallback = false;
        for (ushort y = 0; y < 10; y++)
        {
            var row = new System.Text.StringBuilder();
            for (ushort x = 0; x < 80; x++)
            {
                var cell = frame.Buffer.Get(x, y);
                if (cell is { } c)
                {
                    char? ch = c.Content.AsChar();
                    if (ch.HasValue)
                        row.Append(ch.Value);
                }
            }
            if (row.ToString().Contains("FALLBACK"))
            {
                foundFallback = true;
                break;
            }
        }
        Assert.True(foundFallback, "should show FALLBACK badge when in fallback");
    }

    [Fact]
    public void RenderClearsGapBeforeConfidenceBadge()
    {
        var tl = MakeDriftTimeline();
        var viz = new DriftVisualization(tl).Domains([DecisionDomain.DiffStrategy]);
        var pool = new GraphemePool();
        var frame = new Frame(80, 10, pool);
        frame.Buffer.SetFast(14, 2, Cell.FromChar('X'));

        viz.Render(new Rect(0, 0, 80, 10), frame);

        Assert.Equal(' ', frame.Buffer.Get(14, 2)!.Value.Content.AsChar());
    }

    [Fact]
    public void RenderSkeletonClearsPreviousVisualization()
    {
        var tl = MakeDriftTimeline();
        var viz = new DriftVisualization(tl).ShowRegimeBanner(false);
        var area = new Rect(0, 0, 80, 10);
        var pool = new GraphemePool();
        var frame = new Frame(80, 10, pool);

        viz.Render(area, frame);
        frame.SetDegradation(DegradationLevel.Skeleton);
        viz.Render(area, frame);

        for (ushort y = 0; y < area.Height; y++)
        {
            for (ushort x = 0; x < area.Width; x++)
            {
                Assert.Equal(' ', frame.Buffer.Get(x, y)!.Value.Content.AsChar());
            }
        }
    }

    [Fact]
    public void RenderWithFewerDomainsClearsStaleRows()
    {
        var tl = MakeDriftTimeline();
        var full = new DriftVisualization(tl).ShowRegimeBanner(false);
        var filtered = new DriftVisualization(tl)
            .Domains([DecisionDomain.DiffStrategy])
            .ShowRegimeBanner(false);
        var area = new Rect(0, 0, 80, 10);
        var pool = new GraphemePool();
        var frame = new Frame(80, 10, pool);

        full.Render(area, frame);
        filtered.Render(area, frame);

        for (ushort y = 4; y < 6; y++)
        {
            for (ushort x = 1; x < area.Width - 1; x++)
            {
                Assert.Equal(' ', frame.Buffer.Get(x, y)!.Value.Content.AsChar());
            }
        }
    }
}
