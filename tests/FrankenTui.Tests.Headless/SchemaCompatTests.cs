// Tests for .external/frankentui/crates/ftui-runtime/src/schema_compat.rs and metrics_registry.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c

using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class SchemaCompatTests
{
    // ── MetricsRegistry tests ──────────────────────────────────────────────

    [Fact] public void CounterIncAndGet()
    {
        var c = new Counter();
        Assert.Equal(0UL, c.Get());
        c.Inc();
        Assert.Equal(1UL, c.Get());
        c.IncBy(5);
        Assert.Equal(6UL, c.Get());
    }

    [Fact] public void GaugeSetIncDec()
    {
        var g = new Gauge();
        Assert.Equal(0, g.Get());
        g.Set(42);
        Assert.Equal(42, g.Get());
        g.Inc();
        Assert.Equal(43, g.Get());
        g.Dec();
        Assert.Equal(42, g.Get());
        g.Set(-10);
        Assert.Equal(-10, g.Get());
    }

    [Fact] public void HistogramObserveBuckets()
    {
        var h = new Histogram();
        h.Observe(30);
        h.Observe(75);
        h.Observe(200);
        h.Observe(20_000);

        Assert.Equal(4UL, h.Count);
        Assert.Equal(30UL + 75 + 200 + 20_000, h.Sum);
        var counts = h.BucketCounts();
        Assert.Equal(1UL, counts[0]);
        Assert.Equal(2UL, counts[1]);
        Assert.Equal(3UL, counts[2]);
        Assert.Equal(4UL, counts[9]);
    }

    [Fact] public void HistogramBoundaryValues()
    {
        var h = new Histogram();
        h.Observe(50);
        h.Observe(100);
        h.Observe(16_000);
        var counts = h.BucketCounts();
        Assert.Equal(1UL, counts[0]);
        Assert.Equal(2UL, counts[1]);
        Assert.Equal(3UL, counts[8]);
    }

    [Fact] public void RegistryCounterAccess()
    {
        var reg = new BuiltinMetricsRegistry();
        reg.Counter(BuiltinCounter.RenderFramesTotal).Inc();
        reg.Counter(BuiltinCounter.RenderFramesTotal).IncBy(4);
        Assert.Equal(5UL, reg.Counter(BuiltinCounter.RenderFramesTotal).Get());
    }

    [Fact] public void RegistryGaugeAccess()
    {
        var reg = new BuiltinMetricsRegistry();
        reg.Gauge(BuiltinGauge.TerminalActive).Set(3);
        Assert.Equal(3, reg.Gauge(BuiltinGauge.TerminalActive).Get());
        reg.Gauge(BuiltinGauge.TerminalActive).Dec();
        Assert.Equal(2, reg.Gauge(BuiltinGauge.TerminalActive).Get());
    }

    [Fact] public void RegistryHistogramAccess()
    {
        var reg = new BuiltinMetricsRegistry();
        reg.Histogram(BuiltinHistogram.RenderFrameDurationUs).Observe(1500);
        Assert.Equal(1UL, reg.Histogram(BuiltinHistogram.RenderFrameDurationUs).Count);
        Assert.Equal(1500UL, reg.Histogram(BuiltinHistogram.RenderFrameDurationUs).Sum);
    }

    [Fact] public void RenderContainsAllMetricTypes()
    {
        var reg = new BuiltinMetricsRegistry();
        reg.Counter(BuiltinCounter.RenderFramesTotal).Inc();
        reg.Gauge(BuiltinGauge.TerminalActive).Set(1);
        reg.Histogram(BuiltinHistogram.RenderFrameDurationUs).Observe(500);

        var output = reg.Render();
        Assert.Contains("# TYPE ftui_render_frames_total counter", output);
        Assert.Contains("ftui_render_frames_total 1", output);
        Assert.Contains("# TYPE ftui_terminal_active gauge", output);
        Assert.Contains("ftui_terminal_active 1", output);
        Assert.Contains("# TYPE ftui_render_frame_duration_us histogram", output);
        Assert.Contains("ftui_render_frame_duration_us_bucket{le=\"500\"} 1", output);
        Assert.Contains("ftui_render_frame_duration_us_count 1", output);
        Assert.Contains("ftui_render_frame_duration_us_sum 500", output);
    }

    [Fact] public void RenderFormatIsPrometheusCompatible()
    {
        var reg = new BuiltinMetricsRegistry();
        var output = reg.Render();
        foreach (var line in output.Split('\n'))
        {
            if (line.StartsWith('#') && line.Length > 0)
            {
                Assert.True(line.StartsWith("# HELP ") || line.StartsWith("# TYPE "),
                    $"Comment lines must be HELP or TYPE: {line}");
            }
        }
    }

    [Fact] public void ResetClearsAll()
    {
        var reg = new BuiltinMetricsRegistry();
        reg.Counter(BuiltinCounter.AnsiMalformedTotal).Inc();
        reg.Gauge(BuiltinGauge.EProcessWealth).Set(100);
        reg.Histogram(BuiltinHistogram.AnimationDurationMs).Observe(50);
        reg.Reset();
        Assert.Equal(0UL, reg.Counter(BuiltinCounter.AnsiMalformedTotal).Get());
        Assert.Equal(0, reg.Gauge(BuiltinGauge.EProcessWealth).Get());
        Assert.Equal(0UL, reg.Histogram(BuiltinHistogram.AnimationDurationMs).Count);
    }

    // ── SchemaCompat tests ─────────────────────────────────────────────────

    [Fact] public void ExactMatchAllKinds()
    {
        foreach (var kind in Enum.GetValues<SchemaKind>())
        {
            var current = SchemaVersions.CurrentVersion(kind);
            var result = SchemaCompat.ClassifySchemaCompat(kind, current);
            Assert.IsType<Compatibility.Exact>(result.Comp);
            Assert.True(result.IsCompatible);
        }
    }

    [Fact] public void ForwardCompatEvidence()
    {
        var result = SchemaCompat.ClassifySchemaCompat(SchemaKind.Evidence, "ftui-evidence-v1");
        var fw = Assert.IsType<Compatibility.Forward>(result.Comp);
        Assert.Equal(2U, fw.ReaderVersion);
        Assert.Equal(1U, fw.WriterVersion);
        Assert.True(result.IsCompatible);
    }

    [Fact] public void BackwardIncompatEvidence()
    {
        var result = SchemaCompat.ClassifySchemaCompat(SchemaKind.Evidence, "ftui-evidence-v3");
        Assert.IsType<Compatibility.Backward>(result.Comp);
        Assert.False(result.IsCompatible);
    }

    [Fact] public void UnknownVersionFormat()
    {
        var result = SchemaCompat.ClassifySchemaCompat(SchemaKind.Evidence, "garbage-string");
        Assert.IsType<Compatibility.Unknown>(result.Comp);
        Assert.False(result.IsCompatible);
    }

    [Fact] public void ForwardCompatTelemetrySemver()
    {
        var result = SchemaCompat.ClassifySchemaCompat(SchemaKind.Telemetry, "0.9.0");
        var fw = Assert.IsType<Compatibility.Forward>(result.Comp);
        Assert.Equal(1U, fw.ReaderVersion);
        Assert.Equal(0U, fw.WriterVersion);
        Assert.True(result.IsCompatible);
    }

    [Fact] public void BackwardIncompatTelemetrySemver()
    {
        var result = SchemaCompat.ClassifySchemaCompat(SchemaKind.Telemetry, "2.0.0");
        Assert.IsType<Compatibility.Backward>(result.Comp);
        Assert.False(result.IsCompatible);
    }

    [Fact] public void DefaultMatrixAllPass()
    {
        var matrix = SchemaCompat.DefaultCompatibilityMatrix().ToArray();
        var results = SchemaCompat.RunCompatibilityMatrix(matrix);
        foreach (var (entry, result) in results)
        {
            Assert.Equal(entry.ExpectedCompatible, result.IsCompatible);
        }
    }

    [Fact] public void CompatFailuresCounterIncrements()
    {
        var before = Metrics.Registry.Counter(BuiltinCounter.TraceCompatFailuresTotal).Get();
        SchemaCompat.CheckSchemaCompat(SchemaKind.Evidence, "ftui-evidence-v99");
        var after = Metrics.Registry.Counter(BuiltinCounter.TraceCompatFailuresTotal).Get();
        Assert.True(after > before);
    }

    [Fact] public void ExactMatchDoesNotIncrementCounter()
    {
        var before = Metrics.Registry.Counter(BuiltinCounter.TraceCompatFailuresTotal).Get();
        SchemaCompat.CheckSchemaCompat(SchemaKind.Evidence, "ftui-evidence-v2");
        var after = Metrics.Registry.Counter(BuiltinCounter.TraceCompatFailuresTotal).Get();
        Assert.Equal(before, after);
    }
}
