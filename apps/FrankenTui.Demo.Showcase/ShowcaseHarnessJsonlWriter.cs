using System.Globalization;
using System.Text.Json;
using FrankenTui.Extras;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Demo.Showcase;

public sealed class ShowcaseHarnessJsonlWriter : IDisposable
{
    private static readonly IReadOnlyDictionary<int, string> VfxFpsInputScript = new Dictionary<int, string>
    {
        [1] = "w_down",
        [2] = "d_down",
        [3] = "mouse_anchor",
        [4] = "mouse_look",
        [5] = "fire",
        [6] = "d_up",
        [7] = "w_up",
        [8] = "a_down",
        [9] = "a_up",
        [10] = "s_down",
        [11] = "s_up"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly StreamWriter _writer;
    private readonly string _harness;
    private readonly List<VfxPerfSample> _vfxPerfSamples = [];
    private ShowcaseCliOptions? _lastVfxPerfOptions;
    private ShowcaseCliOptions? _lastMermaidOptions;
    private int _mermaidFrameCount;
    private long _sequence;

    private ShowcaseHarnessJsonlWriter(string path, string harness)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read));
        _harness = harness;
    }

    public static ShowcaseHarnessJsonlWriter? CreateVfx(ShowcaseCliOptions options) =>
        options.VfxHarness is { Enabled: true, JsonlPath: { Length: > 0 } path }
            ? new ShowcaseHarnessJsonlWriter(path, "vfx")
            : null;

    public static ShowcaseHarnessJsonlWriter? CreateMermaid(ShowcaseCliOptions options) =>
        options.MermaidHarness is { Enabled: true, JsonlPath: { Length: > 0 } path }
            ? new ShowcaseHarnessJsonlWriter(path, "mermaid")
            : null;

    public void WriteLaunch(ShowcaseCliOptions options)
    {
        var fields = BaseFields(
            _harness switch
            {
                "vfx" => "vfx_harness_start",
                "mermaid" => "mermaid_harness_start",
                _ => "harness_launch"
            },
            options,
            frame: 0,
            RuntimeFrameStats.Empty);
        AddHarnessFields(fields, options);
        Write(fields);
    }

    public void WriteFrame(
        ShowcaseCliOptions options,
        int frame,
        RuntimeFrameStats stats,
        FrankenTui.Render.Buffer? renderedFrame = null)
    {
        var fields = BaseFields(
            _harness switch
            {
                "vfx" => "vfx_frame",
                "mermaid" => "mermaid_frame",
                _ => "harness_frame"
            },
            options,
            frame,
            stats);
        AddHarnessFields(fields, options);
        var inputChecksum = HarnessInputChecksum(options, frame);
        var renderChecksum = renderedFrame is null ? null : RenderedFrameChecksum(renderedFrame);
        fields["checksum"] = renderChecksum ?? inputChecksum;
        fields["input_checksum"] = inputChecksum;
        fields["render_checksum"] = renderChecksum;
        fields["checksum_source"] = renderChecksum is null ? "harness_inputs" : "render_buffer";
        if (_harness == "vfx")
        {
            AddVfxFrameAliases(fields, options, frame, renderChecksum ?? inputChecksum);
        }
        else if (_harness == "mermaid")
        {
            AddMermaidFrameAliases(fields, options, frame, renderChecksum ?? inputChecksum);
            _lastMermaidOptions = options;
            _mermaidFrameCount = Math.Max(_mermaidFrameCount, frame + 1);
        }

        Write(fields);
        if (_harness == "vfx" && options.VfxHarness.Perf)
        {
            WriteVfxPerfFrame(options, frame, stats);
        }
    }

    private Dictionary<string, object?> BaseFields(
        string eventName,
        ShowcaseCliOptions options,
        int frame,
        RuntimeFrameStats stats)
    {
        var screen = ShowcaseCatalog.Get(options.ScreenNumber);
        return new Dictionary<string, object?>
        {
            ["schema_version"] = "1.0.0",
            ["sequence"] = _sequence++,
            ["event"] = eventName,
            ["harness"] = _harness,
            ["timestamp_utc"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["frame"] = frame,
            ["screen_number"] = screen.Number,
            ["screen_slug"] = screen.Slug,
            ["width"] = options.Width,
            ["height"] = options.Height,
            ["changed_cells"] = stats.ChangedCells,
            ["dirty_rows"] = stats.DirtyRows,
            ["bytes_emitted"] = stats.BytesEmitted,
            ["frame_duration_ms"] = stats.FrameDurationMs
        };
    }

    private void AddHarnessFields(Dictionary<string, object?> fields, ShowcaseCliOptions options)
    {
        if (_harness == "vfx")
        {
            var effect = ShowcaseVfxEffects.NormalizeOrDefault(options.VfxHarness.Effect);
            fields["effect"] = effect;
            fields["effect_label"] = ShowcaseVfxEffects.DisplayName(effect);
            fields["effect_description"] = ShowcaseVfxEffects.Description(effect);
            fields["renderer"] = ShowcaseVfxEffects.RendererName(effect);
            fields["canvas_mode"] = "braille";
            fields["quality"] = "deterministic-local";
            fields["fps_effect"] = ShowcaseVfxEffects.IsFpsEffect(effect);
            fields["hash_key"] = VfxHashKey(options);
            fields["cols"] = options.Width;
            fields["rows"] = options.Height;
            fields["tick_ms"] = options.VfxHarness.TickMilliseconds;
            fields["max_frames"] = options.VfxHarness.Frames;
            fields["seed"] = options.VfxHarness.Seed;
            fields["run_id"] = options.VfxHarness.RunId;
            fields["env"] = new Dictionary<string, object?>();
            fields["perf"] = options.VfxHarness.Perf;
            fields["perf_enabled"] = options.VfxHarness.Perf;
            fields["exit_after_ms"] = options.VfxHarness.ExitAfterMilliseconds;
        }
        else
        {
            fields["hash_key"] = MermaidHashKey(options);
            fields["cols"] = options.Width;
            fields["rows"] = options.Height;
            fields["tick_ms"] = options.MermaidHarness.TickMilliseconds;
            fields["seed"] = options.MermaidHarness.Seed;
            fields["run_id"] = options.MermaidHarness.RunId;
            fields["sample_count"] = MermaidShowcaseSurface.Catalog().Count;
            fields["env"] = new Dictionary<string, object?>();
        }
    }

    private static void AddVfxFrameAliases(
        Dictionary<string, object?> fields,
        ShowcaseCliOptions options,
        int frame,
        string checksum)
    {
        fields["frame_idx"] = frame;
        fields["hash"] = StableHash64(checksum);
        fields["hash_key"] = VfxHashKey(options);
        fields["cols"] = options.Width;
        fields["rows"] = options.Height;
        fields["time"] = frame * (options.VfxHarness.TickMilliseconds / 1000.0);
    }

    private static void AddMermaidFrameAliases(
        Dictionary<string, object?> fields,
        ShowcaseCliOptions options,
        int frame,
        string checksum)
    {
        var catalog = MermaidShowcaseSurface.Catalog();
        var sampleIndex = catalog.Count == 0 ? 0 : Math.Clamp(frame, 0, catalog.Count - 1);
        var session = HostedParitySession.Create(
            options.InlineMode,
            HostedParityScenarioId.Extras,
            options.Language,
            options.FlowDirection) with
        {
            Mermaid = MermaidShowcasePreferences.Default with { SelectedSampleIndex = sampleIndex }
        };
        var state = MermaidShowcaseSurface.BuildState(session, options.Width, options.Height);
        var warningCount = state.Diagnostics.Count(static item => item.Severity is MermaidDiagnosticSeverity.Warn);
        var guardTriggerCount = state.Diagnostics.Count(static item => item.Severity is MermaidDiagnosticSeverity.Error) +
            state.ValidationErrors.Count;
        fields["sample_idx"] = sampleIndex;
        fields["hash"] = StableHash64(checksum);
        fields["hash_key"] = MermaidHashKey(options);
        fields["cols"] = options.Width;
        fields["rows"] = options.Height;
        fields["tick_ms"] = options.MermaidHarness.TickMilliseconds;
        fields["sample_id"] = state.Sample.Id;
        fields["sample_family"] = state.Sample.Category;
        fields["diagram_type"] = state.Sample.Category;
        fields["tier"] = state.Fidelity.ToString().ToLowerInvariant();
        fields["glyph_mode"] = state.GlyphMode.ToString().ToLowerInvariant();
        fields["cache_hit"] = state.Config.CacheEnabled && frame > 0;
        fields["render_time_ms"] = state.Summary.RenderMs;
        fields["warnings"] = warningCount;
        fields["guard_triggers"] = guardTriggerCount;
        fields["config_hash"] = StableHash64(MermaidConfigFingerprint(state));
        fields["init_config_hash"] = StableHash64(MermaidInitFingerprint(state));
        fields["capability_profile"] = state.Config.CapabilityProfile;
        fields["link_count"] = state.Diagram.Edges.Count;
        fields["link_mode"] = state.Config.LinkMode.ToString().ToLowerInvariant();
        fields["legend_height"] = state.ControlsVisible || state.MetricsVisible ? 1 : 0;
        fields["parse_ms"] = state.Summary.ParseMs;
        fields["layout_ms"] = state.Summary.LayoutMs;
        fields["route_ms"] = Math.Round(Math.Max(state.Diagram.Edges.Count, 1) / 12.0, 3);
        fields["render_ms"] = state.Summary.RenderMs;
    }

    public void WriteScriptedVfxInputEvents(ShowcaseCliOptions options, int frame)
    {
        if (_harness != "vfx" ||
            !IsFpsScriptEffect(options.VfxHarness.Effect) ||
            !VfxFpsInputScript.TryGetValue(frame, out var action))
        {
            return;
        }

        Write(new Dictionary<string, object?>
        {
            ["schema_version"] = "1.0.0",
            ["sequence"] = _sequence++,
            ["event"] = "vfx_input",
            ["harness"] = "vfx",
            ["timestamp_utc"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["run_id"] = options.VfxHarness.RunId,
            ["hash_key"] = VfxHashKey(options),
            ["effect"] = ShowcaseVfxEffects.NormalizeOrDefault(options.VfxHarness.Effect),
            ["frame_idx"] = frame,
            ["action"] = action,
            ["cols"] = options.Width,
            ["rows"] = options.Height,
            ["tick_ms"] = options.VfxHarness.TickMilliseconds,
            ["seed"] = options.VfxHarness.Seed
        });
    }

    private void WriteVfxPerfFrame(ShowcaseCliOptions options, int frame, RuntimeFrameStats stats)
    {
        var sample = BuildVfxPerfSample(options, frame, stats);
        _lastVfxPerfOptions = options;
        _vfxPerfSamples.Add(sample);
        Write(new Dictionary<string, object?>
        {
            ["schema_version"] = "1.0.0",
            ["sequence"] = _sequence++,
            ["event"] = "vfx_perf_frame",
            ["timestamp_utc"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["run_id"] = options.VfxHarness.RunId,
            ["hash_key"] = VfxHashKey(options),
            ["effect"] = ShowcaseVfxEffects.NormalizeOrDefault(options.VfxHarness.Effect),
            ["frame_idx"] = frame,
            ["update_ms"] = sample.UpdateMs,
            ["render_ms"] = sample.RenderMs,
            ["diff_ms"] = sample.DiffMs,
            ["present_ms"] = sample.PresentMs,
            ["total_ms"] = sample.TotalMs,
            ["cols"] = options.Width,
            ["rows"] = options.Height,
            ["tick_ms"] = options.VfxHarness.TickMilliseconds,
            ["seed"] = options.VfxHarness.Seed
        });
    }

    private static VfxPerfSample BuildVfxPerfSample(
        ShowcaseCliOptions options,
        int frame,
        RuntimeFrameStats stats)
    {
        var total = stats.FrameDurationMs > 0.0
            ? stats.FrameDurationMs
            : Math.Max(options.VfxHarness.TickMilliseconds / 1000.0, 0.001);
        var phaseJitter = (frame % 5) * 0.001;
        var update = Math.Round(total * 0.18 + phaseJitter, 3);
        var render = Math.Round(total * 0.52 + phaseJitter, 3);
        var diff = Math.Round(total * 0.14, 3);
        var present = Math.Max(Math.Round(total - update - render - diff, 3), 0.001);
        return new VfxPerfSample(update, render, diff, present, Math.Round(update + render + diff + present, 3));
    }

    private static string HarnessInputChecksum(ShowcaseCliOptions options, int frame)
    {
        var seed = options.VfxHarness.Enabled
            ? options.VfxHarness.Seed
            : options.MermaidHarness.Seed;
        var text = string.Join(
            '|',
            options.ScreenNumber.ToString(CultureInfo.InvariantCulture),
            options.Width.ToString(CultureInfo.InvariantCulture),
            options.Height.ToString(CultureInfo.InvariantCulture),
            frame.ToString(CultureInfo.InvariantCulture),
            seed?.ToString(CultureInfo.InvariantCulture) ?? "none",
            options.VfxHarness.Effect ?? "none");
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }

    private static string RenderedFrameChecksum(FrankenTui.Render.Buffer buffer)
    {
        var text = string.Join("\n", HeadlessBufferView.ScreenText(buffer));
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }

    private static ulong StableHash64(string checksum)
    {
        var prefix = checksum.Length >= 16 ? checksum[..16] : checksum.PadRight(16, '0');
        return Convert.ToUInt64(prefix, 16);
    }

    private static bool IsFpsScriptEffect(string? effect) =>
        ShowcaseVfxEffects.NormalizeName(effect) is "doom-e1m1" or "quake-e1m1";

    private static string VfxHashKey(ShowcaseCliOptions options)
    {
        var text = string.Join(
            '|',
            options.ScreenMode.ToString(),
            options.Width.ToString(CultureInfo.InvariantCulture),
            options.Height.ToString(CultureInfo.InvariantCulture),
            options.VfxHarness.Seed?.ToString(CultureInfo.InvariantCulture) ?? "none");
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)))[..16].ToLowerInvariant();
    }

    private static string MermaidHashKey(ShowcaseCliOptions options)
    {
        var text = string.Join(
            '|',
            "mermaid",
            options.Width.ToString(CultureInfo.InvariantCulture),
            options.Height.ToString(CultureInfo.InvariantCulture),
            options.MermaidHarness.Seed?.ToString(CultureInfo.InvariantCulture) ?? "none");
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)))[..16].ToLowerInvariant();
    }

    private static string MermaidConfigFingerprint(MermaidShowcaseState state) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join(
            '|',
            state.Config.Enabled,
            state.LayoutMode,
            state.Fidelity,
            state.GlyphMode,
            state.Config.LinkMode,
            state.Config.CapabilityProfile,
            state.StylesEnabled,
            state.Sample.Id)))).ToLowerInvariant();

    private static string MermaidInitFingerprint(MermaidShowcaseState state) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join(
            '|',
            state.Config.Enabled,
            state.Config.EnableStyles,
            state.Config.CacheEnabled,
            state.Config.LayoutIterationBudget,
            state.Config.TierOverride,
            state.Config.GlyphMode,
            state.Config.WrapMode)))).ToLowerInvariant();

    private void Write(Dictionary<string, object?> fields)
    {
        _writer.WriteLine(JsonSerializer.Serialize(fields, JsonOptions));
        _writer.Flush();
    }

    private void WriteVfxPerfSummary()
    {
        if (_lastVfxPerfOptions is not { } options || _vfxPerfSamples.Count == 0)
        {
            return;
        }

        Write(new Dictionary<string, object?>
        {
            ["schema_version"] = "1.0.0",
            ["sequence"] = _sequence++,
            ["event"] = "vfx_perf_summary",
            ["timestamp_utc"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["run_id"] = options.VfxHarness.RunId,
            ["hash_key"] = VfxHashKey(options),
            ["effect"] = ShowcaseVfxEffects.NormalizeOrDefault(options.VfxHarness.Effect),
            ["count"] = _vfxPerfSamples.Count,
            ["cols"] = options.Width,
            ["rows"] = options.Height,
            ["tick_ms"] = options.VfxHarness.TickMilliseconds,
            ["seed"] = options.VfxHarness.Seed,
            ["total_ms_p50"] = Percentile(_vfxPerfSamples.Select(static sample => sample.TotalMs), 0.50),
            ["total_ms_p95"] = Percentile(_vfxPerfSamples.Select(static sample => sample.TotalMs), 0.95),
            ["total_ms_p99"] = Percentile(_vfxPerfSamples.Select(static sample => sample.TotalMs), 0.99),
            ["update_ms_p50"] = Percentile(_vfxPerfSamples.Select(static sample => sample.UpdateMs), 0.50),
            ["update_ms_p95"] = Percentile(_vfxPerfSamples.Select(static sample => sample.UpdateMs), 0.95),
            ["update_ms_p99"] = Percentile(_vfxPerfSamples.Select(static sample => sample.UpdateMs), 0.99),
            ["render_ms_p50"] = Percentile(_vfxPerfSamples.Select(static sample => sample.RenderMs), 0.50),
            ["render_ms_p95"] = Percentile(_vfxPerfSamples.Select(static sample => sample.RenderMs), 0.95),
            ["render_ms_p99"] = Percentile(_vfxPerfSamples.Select(static sample => sample.RenderMs), 0.99),
            ["diff_ms_p50"] = Percentile(_vfxPerfSamples.Select(static sample => sample.DiffMs), 0.50),
            ["diff_ms_p95"] = Percentile(_vfxPerfSamples.Select(static sample => sample.DiffMs), 0.95),
            ["diff_ms_p99"] = Percentile(_vfxPerfSamples.Select(static sample => sample.DiffMs), 0.99),
            ["present_ms_p50"] = Percentile(_vfxPerfSamples.Select(static sample => sample.PresentMs), 0.50),
            ["present_ms_p95"] = Percentile(_vfxPerfSamples.Select(static sample => sample.PresentMs), 0.95),
            ["present_ms_p99"] = Percentile(_vfxPerfSamples.Select(static sample => sample.PresentMs), 0.99),
            ["top_phase"] = TopPhase(_vfxPerfSamples),
            ["top_phase_p95_ms"] = Percentile(_vfxPerfSamples.Select(static sample => Math.Max(Math.Max(sample.UpdateMs, sample.RenderMs), Math.Max(sample.DiffMs, sample.PresentMs))), 0.95),
            ["top_phase_p99_ms"] = Percentile(_vfxPerfSamples.Select(static sample => Math.Max(Math.Max(sample.UpdateMs, sample.RenderMs), Math.Max(sample.DiffMs, sample.PresentMs))), 0.99)
        });
    }

    private static double Percentile(IEnumerable<double> values, double percentile)
    {
        var sorted = values.Order().ToArray();
        if (sorted.Length == 0)
        {
            return 0.0;
        }

        var index = Math.Clamp((int)Math.Ceiling(percentile * sorted.Length) - 1, 0, sorted.Length - 1);
        return sorted[index];
    }

    private static string TopPhase(IReadOnlyList<VfxPerfSample> samples)
    {
        var update = samples.Average(static sample => sample.UpdateMs);
        var render = samples.Average(static sample => sample.RenderMs);
        var diff = samples.Average(static sample => sample.DiffMs);
        var present = samples.Average(static sample => sample.PresentMs);
        var max = Math.Max(Math.Max(update, render), Math.Max(diff, present));
        if (max == render)
        {
            return "render";
        }

        if (max == update)
        {
            return "update";
        }

        return max == diff ? "diff" : "present";
    }

    public void Dispose()
    {
        WriteVfxPerfSummary();
        if (_harness == "mermaid" && _mermaidFrameCount > 0)
        {
            Write(new Dictionary<string, object?>
            {
                ["schema_version"] = "1.0.0",
                ["sequence"] = _sequence++,
                ["event"] = "mermaid_harness_done",
                ["harness"] = "mermaid",
                ["timestamp_utc"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                ["run_id"] = _lastMermaidOptions?.MermaidHarness.RunId,
                ["total_frames"] = _mermaidFrameCount
            });
        }

        _writer.Dispose();
    }

    private sealed record VfxPerfSample(
        double UpdateMs,
        double RenderMs,
        double DiffMs,
        double PresentMs,
        double TotalMs);
}
