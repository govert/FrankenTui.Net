// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/metrics_registry.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
//
// Prometheus-compatible metrics registry.
// Provides counters, gauges, and histograms exportable in Prometheus text format.

namespace FrankenTui.Runtime;

/// <summary>Global metrics registry instance.</summary>
public static class Metrics
{
    public static readonly BuiltinMetricsRegistry Registry = new();
}

/// <summary>A monotonic counter (can only increase).</summary>
public sealed class Counter
{
    private long _value;

    /// <summary>Increment by 1.</summary>
    public void Inc() => Interlocked.Increment(ref _value);

    /// <summary>Increment by n.</summary>
    public void IncBy(ulong n) => Interlocked.Add(ref _value, (long)n);

    /// <summary>Current value.</summary>
    public ulong Get() => (ulong)Interlocked.Read(ref _value);
}

/// <summary>A gauge (can go up or down).</summary>
public sealed class Gauge
{
    private long _value;

    /// <summary>Set the gauge to a specific value.</summary>
    public void Set(long v) => Interlocked.Exchange(ref _value, v);

    /// <summary>Increment by 1.</summary>
    public void Inc() => Interlocked.Increment(ref _value);

    /// <summary>Decrement by 1.</summary>
    public void Dec() => Interlocked.Decrement(ref _value);

    /// <summary>Current value.</summary>
    public long Get() => Interlocked.Read(ref _value);
}

/// <summary>Fixed-bucket histogram for latency measurements.</summary>
public sealed class Histogram
{
    private static readonly ulong[] Bounds = [50, 100, 250, 500, 1_000, 2_000, 4_000, 8_000, 16_000];

    private readonly long[] _buckets = new long[10];
    private long _sum;
    private long _count;

    /// <summary>Record a value.</summary>
    public void Observe(ulong value)
    {
        int idx = 9; // +Inf
        for (int i = 0; i < Bounds.Length; i++)
        {
            if (value <= Bounds[i]) { idx = i; break; }
        }
        Interlocked.Increment(ref _buckets[idx]);
        Interlocked.Add(ref _sum, (long)value);
        Interlocked.Increment(ref _count);
    }

    /// <summary>Current count of observations.</summary>
    public ulong Count => (ulong)Interlocked.Read(ref _count);

    /// <summary>Current sum of all observations.</summary>
    public ulong Sum => (ulong)Interlocked.Read(ref _sum);

    /// <summary>Snapshot of cumulative bucket counts.</summary>
    public ulong[] BucketCounts()
    {
        var counts = new ulong[10];
        ulong cumulative = 0;
        for (int i = 0; i < 10; i++)
        {
            cumulative += (ulong)Interlocked.Read(ref _buckets[i]);
            counts[i] = cumulative;
        }
        return counts;
    }
}

// ── Builtin enums ─────────────────────────────────────────────────────────

public enum BuiltinCounter : byte
{
    RenderFramesTotal, AnsiSequencesParsedTotal, AnsiMalformedTotal,
    RuntimeMessagesProcessedTotal, EffectsCommandTotal, EffectsSubscriptionTotal,
    SloBreachesTotal, TerminalResizeEventsTotal, IncrementalCacheHitsTotal,
    IncrementalCacheMissesTotal, VoiSamplesTakenTotal, VoiSamplesSkippedTotal,
    BocpdChangePointsTotal, EProcessRejectionsTotal, TraceCompatFailuresTotal,
}

public enum BuiltinGauge : byte
{
    TerminalActive, EProcessWealth, DegradationLevel,
}

public enum BuiltinHistogram : byte
{
    RenderFrameDurationUs, DiffStrategyDurationUs, LayoutComputeDurationUs,
    WidgetRenderDurationUs, ConformalIntervalWidthUs, AnimationDurationMs,
}

// ── Builtin metadata ──────────────────────────────────────────────────────

internal static class BuiltinMeta
{
    public const int CounterCount = 15;
    public const int GaugeCount = 3;
    public const int HistogramCount = 6;

    public static string CounterName(BuiltinCounter c) => c switch
    {
        BuiltinCounter.RenderFramesTotal => "ftui_render_frames_total",
        BuiltinCounter.AnsiSequencesParsedTotal => "ftui_ansi_sequences_parsed_total",
        BuiltinCounter.AnsiMalformedTotal => "ftui_ansi_malformed_total",
        BuiltinCounter.RuntimeMessagesProcessedTotal => "ftui_runtime_messages_processed_total",
        BuiltinCounter.EffectsCommandTotal => "ftui_effects_command_total",
        BuiltinCounter.EffectsSubscriptionTotal => "ftui_effects_subscription_total",
        BuiltinCounter.SloBreachesTotal => "ftui_slo_breaches_total",
        BuiltinCounter.TerminalResizeEventsTotal => "ftui_terminal_resize_events_total",
        BuiltinCounter.IncrementalCacheHitsTotal => "ftui_incremental_cache_hits_total",
        BuiltinCounter.IncrementalCacheMissesTotal => "ftui_incremental_cache_misses_total",
        BuiltinCounter.VoiSamplesTakenTotal => "ftui_voi_samples_taken_total",
        BuiltinCounter.VoiSamplesSkippedTotal => "ftui_voi_samples_skipped_total",
        BuiltinCounter.BocpdChangePointsTotal => "ftui_bocpd_change_points_total",
        BuiltinCounter.EProcessRejectionsTotal => "ftui_eprocess_rejections_total",
        BuiltinCounter.TraceCompatFailuresTotal => "ftui_trace_compat_failures_total",
        _ => throw new ArgumentOutOfRangeException(nameof(c)),
    };

    public static string CounterHelp(BuiltinCounter c) => c switch
    {
        BuiltinCounter.RenderFramesTotal => "Total render frames produced.",
        BuiltinCounter.AnsiSequencesParsedTotal => "Total ANSI sequences parsed.",
        BuiltinCounter.AnsiMalformedTotal => "Malformed ANSI sequences encountered.",
        BuiltinCounter.RuntimeMessagesProcessedTotal => "Runtime messages processed.",
        BuiltinCounter.EffectsCommandTotal => "Command effects executed.",
        BuiltinCounter.EffectsSubscriptionTotal => "Subscription effects started.",
        BuiltinCounter.SloBreachesTotal => "SLO breaches detected.",
        BuiltinCounter.TerminalResizeEventsTotal => "Terminal resize events received.",
        BuiltinCounter.IncrementalCacheHitsTotal => "Incremental computation cache hits.",
        BuiltinCounter.IncrementalCacheMissesTotal => "Incremental computation cache misses.",
        BuiltinCounter.VoiSamplesTakenTotal => "VOI samples taken.",
        BuiltinCounter.VoiSamplesSkippedTotal => "VOI samples skipped.",
        BuiltinCounter.BocpdChangePointsTotal => "BOCPD change points detected.",
        BuiltinCounter.EProcessRejectionsTotal => "E-process rejections triggered.",
        BuiltinCounter.TraceCompatFailuresTotal => "Trace/evidence schema compatibility failures.",
        _ => throw new ArgumentOutOfRangeException(nameof(c)),
    };

    public static string GaugeName(BuiltinGauge g) => g switch
    {
        BuiltinGauge.TerminalActive => "ftui_terminal_active",
        BuiltinGauge.EProcessWealth => "ftui_eprocess_wealth",
        BuiltinGauge.DegradationLevel => "ftui_degradation_level",
        _ => throw new ArgumentOutOfRangeException(nameof(g)),
    };

    public static string GaugeHelp(BuiltinGauge g) => g switch
    {
        BuiltinGauge.TerminalActive => "Currently active terminal instances.",
        BuiltinGauge.EProcessWealth => "Current e-process wealth value.",
        BuiltinGauge.DegradationLevel => "Current degradation level (0=Full, 4=Skeleton).",
        _ => throw new ArgumentOutOfRangeException(nameof(g)),
    };

    public static string HistogramName(BuiltinHistogram h) => h switch
    {
        BuiltinHistogram.RenderFrameDurationUs => "ftui_render_frame_duration_us",
        BuiltinHistogram.DiffStrategyDurationUs => "ftui_diff_strategy_duration_us",
        BuiltinHistogram.LayoutComputeDurationUs => "ftui_layout_compute_duration_us",
        BuiltinHistogram.WidgetRenderDurationUs => "ftui_widget_render_duration_us",
        BuiltinHistogram.ConformalIntervalWidthUs => "ftui_conformal_interval_width_us",
        BuiltinHistogram.AnimationDurationMs => "ftui_animation_duration_ms",
        _ => throw new ArgumentOutOfRangeException(nameof(h)),
    };

    public static string HistogramHelp(BuiltinHistogram h) => h switch
    {
        BuiltinHistogram.RenderFrameDurationUs => "Render frame duration in microseconds.",
        BuiltinHistogram.DiffStrategyDurationUs => "Diff strategy computation duration in microseconds.",
        BuiltinHistogram.LayoutComputeDurationUs => "Layout computation duration in microseconds.",
        BuiltinHistogram.WidgetRenderDurationUs => "Widget render duration in microseconds.",
        BuiltinHistogram.ConformalIntervalWidthUs => "Conformal prediction interval width in microseconds.",
        BuiltinHistogram.AnimationDurationMs => "Animation duration in milliseconds.",
        _ => throw new ArgumentOutOfRangeException(nameof(h)),
    };

    public static readonly BuiltinCounter[] AllCounters = Enum.GetValues<BuiltinCounter>();
    public static readonly BuiltinGauge[] AllGauges = Enum.GetValues<BuiltinGauge>();
    public static readonly BuiltinHistogram[] AllHistograms = Enum.GetValues<BuiltinHistogram>();
}

// ── MetricsRegistry ───────────────────────────────────────────────────────

public sealed class BuiltinMetricsRegistry
{
    private readonly Counter[] _counters;
    private readonly Gauge[] _gauges;
    private readonly Histogram[] _histograms;

    public BuiltinMetricsRegistry()
    {
        _counters = new Counter[BuiltinMeta.CounterCount];
        _gauges = new Gauge[BuiltinMeta.GaugeCount];
        _histograms = new Histogram[BuiltinMeta.HistogramCount];
        for (int i = 0; i < _counters.Length; i++) _counters[i] = new Counter();
        for (int i = 0; i < _gauges.Length; i++) _gauges[i] = new Gauge();
        for (int i = 0; i < _histograms.Length; i++) _histograms[i] = new Histogram();
    }

    public Counter Counter(BuiltinCounter c) => _counters[(int)c];
    public Gauge Gauge(BuiltinGauge g) => _gauges[(int)g];
    public Histogram Histogram(BuiltinHistogram h) => _histograms[(int)h];

    public string Render()
    {
        var sb = new System.Text.StringBuilder(4096);
        RenderTo(sb);
        return sb.ToString();
    }

    public void RenderTo(System.Text.StringBuilder sb)
    {
        foreach (var c in BuiltinMeta.AllCounters)
        {
            var name = BuiltinMeta.CounterName(c);
            var help = BuiltinMeta.CounterHelp(c);
            sb.Append("# HELP ").Append(name).Append(' ').Append(help).Append('\n');
            sb.Append("# TYPE ").Append(name).Append(" counter\n");
            sb.Append(name).Append(' ').Append(Counter(c).Get()).Append('\n');
        }
        foreach (var g in BuiltinMeta.AllGauges)
        {
            var name = BuiltinMeta.GaugeName(g);
            var help = BuiltinMeta.GaugeHelp(g);
            sb.Append("# HELP ").Append(name).Append(' ').Append(help).Append('\n');
            sb.Append("# TYPE ").Append(name).Append(" gauge\n");
            sb.Append(name).Append(' ').Append(Gauge(g).Get()).Append('\n');
        }
        foreach (var h in BuiltinMeta.AllHistograms)
        {
            var hist = Histogram(h);
            var counts = hist.BucketCounts();
            var name = BuiltinMeta.HistogramName(h);
            var help = BuiltinMeta.HistogramHelp(h);
            sb.Append("# HELP ").Append(name).Append(' ').Append(help).Append('\n');
            sb.Append("# TYPE ").Append(name).Append(" histogram\n");

            var bounds = new ulong[] { 50, 100, 250, 500, 1_000, 2_000, 4_000, 8_000, 16_000 };
            for (int j = 0; j < bounds.Length; j++)
                sb.Append(name).Append("_bucket{le=\"").Append(bounds[j]).Append("\"} ").Append(counts[j]).Append('\n');
            sb.Append(name).Append("_bucket{le=\"+Inf\"} ").Append(counts[9]).Append('\n');
            sb.Append(name).Append("_sum ").Append(hist.Sum).Append('\n');
            sb.Append(name).Append("_count ").Append(hist.Count).Append('\n');
        }
    }

    public void Reset()
    {
        for (int i = 0; i < _counters.Length; i++) _counters[i] = new Counter();
        for (int i = 0; i < _gauges.Length; i++) _gauges[i] = new Gauge();
        for (int i = 0; i < _histograms.Length; i++) _histograms[i] = new Histogram();
    }
}
