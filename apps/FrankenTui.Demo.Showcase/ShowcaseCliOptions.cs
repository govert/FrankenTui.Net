using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

public enum ShowcaseScreenMode
{
    Alt,
    Inline,
    InlineAuto
}

public enum ShowcaseMouseMode
{
    On,
    Off,
    Auto
}

public sealed record ShowcaseVfxHarnessOptions(
    bool Enabled,
    string? Effect,
    uint TickMilliseconds,
    uint Frames,
    ushort Columns,
    ushort Rows,
    ulong? Seed,
    string? JsonlPath,
    string? RunId,
    bool Perf,
    uint ExitAfterMilliseconds,
    string? GoldenPath,
    bool UpdateGolden);

public sealed record ShowcaseMermaidHarnessOptions(
    bool Enabled,
    uint TickMilliseconds,
    ushort Columns,
    ushort Rows,
    ulong? Seed,
    string? JsonlPath,
    string? RunId);

public sealed record ShowcaseCliOptions(
    ShowcaseScreenMode ScreenMode,
    ushort Width,
    ushort Height,
    int? Frames,
    bool InteractiveMode,
    int ScreenNumber,
    bool Tour,
    double TourSpeed,
    int TourStartStep,
    string Language,
    WidgetFlowDirection FlowDirection,
    ShowcaseMouseMode MouseMode,
    ushort UiHeight,
    ushort UiMinHeight,
    ushort UiMaxHeight,
    uint TickIntervalMilliseconds,
    uint ExitAfterMilliseconds,
    uint ExitAfterTicks,
    bool Deterministic,
    ulong? DeterministicSeed,
    string? PaneWorkspacePath,
    string? EvidenceJsonlPath,
    ShowcaseVfxHarnessOptions VfxHarness,
    ShowcaseMermaidHarnessOptions MermaidHarness,
    bool WidthExplicit,
    bool HeightExplicit)
{
    public bool InlineMode => ScreenMode is ShowcaseScreenMode.Inline or ShowcaseScreenMode.InlineAuto;

    public bool HasExplicitViewport => WidthExplicit || HeightExplicit;

    public bool UseMouseTracking => MouseMode switch
    {
        ShowcaseMouseMode.On => true,
        ShowcaseMouseMode.Off => false,
        _ => !InlineMode
    };

    public static ShowcaseCliOptions Parse(string[] arguments, Func<string, string?>? getEnvironmentVariable = null)
    {
        getEnvironmentVariable ??= Environment.GetEnvironmentVariable;

        var screenMode = ParseScreenMode(
            ParseOption(arguments, "--screen-mode") ??
            getEnvironmentVariable("FTUI_DEMO_SCREEN_MODE"),
            ShowcaseScreenMode.Alt);
        if (HasFlag(arguments, "--inline"))
        {
            screenMode = ShowcaseScreenMode.Inline;
        }

        var widthExplicit = ParseOption(arguments, "--width") is not null;
        var heightExplicit = ParseOption(arguments, "--height") is not null;
        var width = ParseUShort(arguments, "--width", 64);
        var uiHeight = ParseUShort(
            ParseOption(arguments, "--ui-height") ??
            getEnvironmentVariable("FTUI_DEMO_UI_HEIGHT"),
            20);
        var uiMinHeight = ParseUShort(
            ParseOption(arguments, "--ui-min-height") ??
            getEnvironmentVariable("FTUI_DEMO_UI_MIN_HEIGHT"),
            8);
        var uiMaxHeight = ParseUShort(
            ParseOption(arguments, "--ui-max-height") ??
            getEnvironmentVariable("FTUI_DEMO_UI_MAX_HEIGHT"),
            30);
        if (uiMinHeight > uiMaxHeight)
        {
            (uiMinHeight, uiMaxHeight) = (uiMaxHeight, uiMinHeight);
        }

        var heightOption = ParseOption(arguments, "--height");
        var height = ParseUShort(heightOption, 18);
        if (heightOption is null && screenMode is ShowcaseScreenMode.Inline)
        {
            height = uiHeight;
        }

        if (screenMode is ShowcaseScreenMode.InlineAuto)
        {
            height = ClampToUShort(heightOption is null ? uiHeight : height, uiMinHeight, uiMaxHeight);
        }

        var frames = ParseNullableInt(arguments, "--frames");
        var interactiveMode = HasFlag(arguments, "--interactive") || frames is null;
        var language = ParseOption(arguments, "--lang") ?? "en-US";
        var flowDirection = HasFlag(arguments, "--rtl")
            ? WidgetFlowDirection.RightToLeft
            : WidgetFlowDirection.LeftToRight;
        var scenario = ParseOption(arguments, "--scenario");
        var requestedScreen = ParseInt(
            ParseOption(arguments, "--screen") ??
            getEnvironmentVariable("FTUI_DEMO_SCREEN"),
            0);
        var screenNumber = ShowcaseCatalog.ClampScreenNumber(
            requestedScreen > 0
                ? requestedScreen
                : ShowcaseCatalog.ResolveLegacyScenario(scenario));
        var tour = HasFlag(arguments, "--tour") ||
            ParseBool(getEnvironmentVariable("FTUI_DEMO_TOUR"), fallback: false);
        var tourSpeed = ParseDouble(
            ParseOption(arguments, "--tour-speed") ??
            getEnvironmentVariable("FTUI_DEMO_TOUR_SPEED"),
            1.0);
        var tourStartStep = ParseInt(
            ParseOption(arguments, "--tour-start-step") ??
            getEnvironmentVariable("FTUI_DEMO_TOUR_START_STEP"),
            1);
        var mouseMode = ParseMouseMode(
            ParseOption(arguments, "--mouse") ??
            getEnvironmentVariable("FTUI_DEMO_MOUSE"),
            ShowcaseMouseMode.Auto);
        if (HasFlag(arguments, "--no-mouse"))
        {
            mouseMode = ShowcaseMouseMode.Off;
        }
        var tickIntervalMilliseconds = ParseUInt(
            ParseOption(arguments, "--tick-ms") ??
            getEnvironmentVariable("FTUI_DEMO_TICK_MS"),
            20);
        var exitAfterMilliseconds = ParseUInt(
            ParseOption(arguments, "--exit-after-ms") ??
            getEnvironmentVariable("FTUI_DEMO_EXIT_AFTER_MS"),
            0);
        var exitAfterTicks = ParseUInt(
            ParseOption(arguments, "--exit-after-ticks") ??
            getEnvironmentVariable("FTUI_DEMO_EXIT_AFTER_TICKS"),
            0);
        var deterministic = HasFlag(arguments, "--deterministic") ||
            ParseBool(getEnvironmentVariable("FTUI_DEMO_DETERMINISTIC"), fallback: false);
        var deterministicSeed = ParseULong(
            ParseOption(arguments, "--seed") ??
            getEnvironmentVariable("FTUI_DEMO_SEED"));
        var paneWorkspacePath = NormalizeOptionalPath(
            ParseOption(arguments, "--pane-workspace") ??
            getEnvironmentVariable("FTUI_DEMO_PANE_WORKSPACE"));
        var evidenceJsonlPath = NormalizeOptionalPath(
            ParseOption(arguments, "--evidence-jsonl") ??
            getEnvironmentVariable("FTUI_DEMO_EVIDENCE_JSONL") ??
            getEnvironmentVariable("FTUI_HARNESS_EVIDENCE_JSONL"));
        var (vfxColumns, vfxRows) = ParseSize(
            ParseOption(arguments, "--vfx-size") ??
            getEnvironmentVariable("FTUI_DEMO_VFX_SIZE"),
            120,
            40);
        vfxColumns = ParseUShort(
            ParseOption(arguments, "--vfx-cols") ??
            getEnvironmentVariable("FTUI_DEMO_VFX_COLS"),
            vfxColumns);
        vfxRows = ParseUShort(
            ParseOption(arguments, "--vfx-rows") ??
            getEnvironmentVariable("FTUI_DEMO_VFX_ROWS"),
            vfxRows);
        var vfxHarnessEnabled = HasFlag(arguments, "--vfx-harness") ||
            ParseBool(getEnvironmentVariable("FTUI_DEMO_VFX_HARNESS"), fallback: false);
        var vfxJsonlPath = NormalizeOptionalPath(
            ParseOption(arguments, "--vfx-jsonl") ??
            getEnvironmentVariable("FTUI_DEMO_VFX_JSONL"));
        var vfxHarness = new ShowcaseVfxHarnessOptions(
            vfxHarnessEnabled,
            ShowcaseVfxEffects.NormalizeHarnessInput(
                ParseOption(arguments, "--vfx-effect") ??
                getEnvironmentVariable("FTUI_DEMO_VFX_EFFECT")),
            Math.Max(ParseUInt(
                ParseOption(arguments, "--vfx-tick-ms") ??
                getEnvironmentVariable("FTUI_DEMO_VFX_TICK_MS"),
                16), 1),
            ParseUInt(
                ParseOption(arguments, "--vfx-frames") ??
                getEnvironmentVariable("FTUI_DEMO_VFX_FRAMES"),
                0),
            vfxColumns,
            vfxRows,
            ParseULong(
                ParseOption(arguments, "--vfx-seed") ??
                getEnvironmentVariable("FTUI_DEMO_VFX_SEED")),
            vfxJsonlPath ?? (vfxHarnessEnabled ? "vfx_harness.jsonl" : null),
            NormalizeOptionalPath(
                ParseOption(arguments, "--vfx-run-id") ??
                getEnvironmentVariable("FTUI_DEMO_VFX_RUN_ID")),
            HasFlag(arguments, "--vfx-perf") ||
                ParseBool(getEnvironmentVariable("FTUI_DEMO_VFX_PERF"), fallback: false),
            ParseUInt(
                ParseOption(arguments, "--vfx-exit-after-ms") ??
                getEnvironmentVariable("FTUI_DEMO_VFX_EXIT_AFTER_MS"),
                0),
            NormalizeOptionalPath(
                ParseOption(arguments, "--vfx-golden") ??
                getEnvironmentVariable("FTUI_DEMO_VFX_GOLDEN")),
            HasFlag(arguments, "--vfx-update-golden") ||
                ParseBool(getEnvironmentVariable("FTUI_DEMO_VFX_UPDATE_GOLDEN"), fallback: false));
        var mermaidHarness = new ShowcaseMermaidHarnessOptions(
            HasFlag(arguments, "--mermaid-harness") ||
                ParseBool(getEnvironmentVariable("FTUI_DEMO_MERMAID_HARNESS"), fallback: false),
            Math.Max(ParseUInt(
                ParseOption(arguments, "--mermaid-tick-ms") ??
                getEnvironmentVariable("FTUI_DEMO_MERMAID_TICK_MS"),
                100), 1),
            ParseUShort(
                ParseOption(arguments, "--mermaid-cols") ??
                getEnvironmentVariable("FTUI_DEMO_MERMAID_COLS"),
                120),
            ParseUShort(
                ParseOption(arguments, "--mermaid-rows") ??
                getEnvironmentVariable("FTUI_DEMO_MERMAID_ROWS"),
                40),
            ParseULong(
                ParseOption(arguments, "--mermaid-seed") ??
                getEnvironmentVariable("FTUI_DEMO_MERMAID_SEED")),
            NormalizeOptionalPath(
                ParseOption(arguments, "--mermaid-jsonl") ??
                getEnvironmentVariable("FTUI_DEMO_MERMAID_JSONL")),
            NormalizeOptionalPath(
                ParseOption(arguments, "--mermaid-run-id") ??
                getEnvironmentVariable("FTUI_DEMO_MERMAID_RUN_ID")));

        var effectiveWidth = width;
        var effectiveHeight = height;
        var effectiveFrames = frames;
        var effectiveInteractiveMode = interactiveMode;
        var effectiveScreenNumber = screenNumber;
        var effectiveMouseMode = mouseMode;
        var effectiveTickIntervalMilliseconds = Math.Max(tickIntervalMilliseconds, 1);
        var effectiveExitAfterMilliseconds = exitAfterMilliseconds;

        if (vfxHarness.Enabled)
        {
            effectiveScreenNumber = 18;
            effectiveWidth = vfxHarness.Columns;
            effectiveHeight = vfxHarness.Rows;
            effectiveTickIntervalMilliseconds = vfxHarness.TickMilliseconds;
            if (vfxHarness.Frames > 0)
            {
                effectiveFrames = ToPositiveInt(vfxHarness.Frames);
                effectiveInteractiveMode = false;
            }

            if (vfxHarness.ExitAfterMilliseconds > 0)
            {
                effectiveExitAfterMilliseconds = vfxHarness.ExitAfterMilliseconds;
            }
        }
        else if (mermaidHarness.Enabled)
        {
            effectiveScreenNumber = 16;
            effectiveWidth = mermaidHarness.Columns;
            effectiveHeight = mermaidHarness.Rows;
            effectiveTickIntervalMilliseconds = mermaidHarness.TickMilliseconds;
            effectiveMouseMode = ShowcaseMouseMode.Off;
        }

        return new ShowcaseCliOptions(
            screenMode,
            effectiveWidth,
            effectiveHeight,
            effectiveFrames,
            effectiveInteractiveMode,
            effectiveScreenNumber,
            tour,
            tourSpeed,
            tourStartStep,
            language,
            flowDirection,
            effectiveMouseMode,
            uiHeight,
            uiMinHeight,
            uiMaxHeight,
            effectiveTickIntervalMilliseconds,
            effectiveExitAfterMilliseconds,
            exitAfterTicks,
            deterministic,
            deterministicSeed,
            paneWorkspacePath,
            evidenceJsonlPath,
            vfxHarness,
            mermaidHarness,
            widthExplicit,
            heightExplicit);
    }

    private static ushort ParseUShort(string[] arguments, string name, ushort fallback) =>
        ParseUShort(ParseOption(arguments, name), fallback);

    private static ushort ParseUShort(string? value, ushort fallback) =>
        ushort.TryParse(value, out var parsed) ? parsed : fallback;

    private static int ParseInt(string? value, int fallback) =>
        int.TryParse(value, out var parsed) ? parsed : fallback;

    private static uint ParseUInt(string? value, uint fallback) =>
        uint.TryParse(value, out var parsed) ? parsed : fallback;

    private static ulong? ParseULong(string? value) =>
        ulong.TryParse(value, out var parsed) ? parsed : null;

    private static int ToPositiveInt(uint value) =>
        value > int.MaxValue ? int.MaxValue : Math.Max((int)value, 1);

    private static int? ParseNullableInt(string[] arguments, string name) =>
        int.TryParse(ParseOption(arguments, name), out var parsed) ? parsed : null;

    private static double ParseDouble(string? value, double fallback) =>
        double.TryParse(value, out var parsed) ? parsed : fallback;

    private static bool ParseBool(string? value, bool fallback) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "yes" or "on" => true,
            "0" or "false" or "no" or "off" => false,
            _ => fallback
        };

    private static string? NormalizeOptionalPath(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static (ushort Columns, ushort Rows) ParseSize(string? value, ushort fallbackColumns, ushort fallbackRows)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (fallbackColumns, fallbackRows);
        }

        var parts = value.Split('x', 'X');
        if (parts.Length == 2 &&
            ushort.TryParse(parts[0], out var columns) &&
            ushort.TryParse(parts[1], out var rows))
        {
            return (columns, rows);
        }

        return (fallbackColumns, fallbackRows);
    }

    private static ShowcaseScreenMode ParseScreenMode(string? value, ShowcaseScreenMode fallback) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "alt" => ShowcaseScreenMode.Alt,
            "inline" => ShowcaseScreenMode.Inline,
            "inline-auto" => ShowcaseScreenMode.InlineAuto,
            _ => fallback
        };

    private static ShowcaseMouseMode ParseMouseMode(string? value, ShowcaseMouseMode fallback) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "on" => ShowcaseMouseMode.On,
            "off" => ShowcaseMouseMode.Off,
            "auto" => ShowcaseMouseMode.Auto,
            _ => fallback
        };

    private static ushort ClampToUShort(ushort value, ushort min, ushort max) =>
        (ushort)Math.Clamp(value, min, max);

    private static bool HasFlag(string[] arguments, string name) =>
        Array.Exists(arguments, argument =>
            argument.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            argument.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase));

    private static string? ParseOption(string[] arguments, string name)
    {
        for (var index = 0; index < arguments.Length - 1; index++)
        {
            if (arguments[index].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return arguments[index + 1];
            }
        }

        var prefix = name + "=";
        foreach (var argument in arguments)
        {
            if (argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return argument[prefix.Length..];
            }
        }

        return null;
    }
}
