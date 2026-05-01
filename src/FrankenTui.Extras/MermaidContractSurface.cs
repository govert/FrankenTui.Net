using System.Text.Json;
using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Text;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

public enum MermaidGlyphMode
{
    Unicode,
    Ascii
}

public enum MermaidTier
{
    Auto,
    Compact,
    Normal,
    Rich
}

public enum MermaidWrapMode
{
    None,
    Word,
    Char,
    WordChar
}

public enum MermaidLinkMode
{
    Off,
    Inline,
    Footnote
}

public enum MermaidSanitizeMode
{
    Strict,
    Lenient
}

public enum MermaidErrorMode
{
    Panel,
    Raw,
    Both
}

public enum MermaidLayoutMode
{
    Auto,
    Dense,
    Spacious
}

public sealed record MermaidConfig(
    bool Enabled,
    MermaidGlyphMode GlyphMode,
    MermaidTier TierOverride,
    int MaxNodes,
    int MaxEdges,
    int RouteBudget,
    int LayoutIterationBudget,
    int MaxLabelChars,
    int MaxLabelLines,
    MermaidWrapMode WrapMode,
    bool EnableStyles,
    bool EnableInitDirectives,
    bool EnableLinks,
    MermaidLinkMode LinkMode,
    MermaidSanitizeMode SanitizeMode,
    MermaidErrorMode ErrorMode,
    string? LogPath,
    bool CacheEnabled,
    string? CapabilityProfile)
{
    public static MermaidConfig Default { get; } = new(
        Enabled: true,
        GlyphMode: MermaidGlyphMode.Unicode,
        TierOverride: MermaidTier.Auto,
        MaxNodes: 200,
        MaxEdges: 400,
        RouteBudget: 4000,
        LayoutIterationBudget: 200,
        MaxLabelChars: 48,
        MaxLabelLines: 3,
        WrapMode: MermaidWrapMode.WordChar,
        EnableStyles: true,
        EnableInitDirectives: false,
        EnableLinks: false,
        LinkMode: MermaidLinkMode.Off,
        SanitizeMode: MermaidSanitizeMode.Strict,
        ErrorMode: MermaidErrorMode.Panel,
        LogPath: null,
        CacheEnabled: true,
        CapabilityProfile: null);

    public static MermaidConfig FromEnvironment(IReadOnlyDictionary<string, string?>? environment = null)
    {
        environment ??= Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(
                static entry => (string)entry.Key,
                static entry => entry.Value?.ToString(),
                StringComparer.OrdinalIgnoreCase);

        return Default with
        {
            Enabled = ParseBool(Get(environment, "FTUI_MERMAID_ENABLE"), Default.Enabled),
            GlyphMode = ParseEnum(Get(environment, "FTUI_MERMAID_GLYPH_MODE"), Default.GlyphMode),
            TierOverride = ParseEnum(Get(environment, "FTUI_MERMAID_TIER"), Default.TierOverride),
            MaxNodes = ParseInt(Get(environment, "FTUI_MERMAID_MAX_NODES"), Default.MaxNodes),
            MaxEdges = ParseInt(Get(environment, "FTUI_MERMAID_MAX_EDGES"), Default.MaxEdges),
            RouteBudget = ParseInt(Get(environment, "FTUI_MERMAID_ROUTE_BUDGET"), Default.RouteBudget),
            LayoutIterationBudget = ParseInt(Get(environment, "FTUI_MERMAID_LAYOUT_ITER_BUDGET"), Default.LayoutIterationBudget),
            MaxLabelChars = ParseInt(Get(environment, "FTUI_MERMAID_MAX_LABEL_CHARS"), Default.MaxLabelChars),
            MaxLabelLines = ParseInt(Get(environment, "FTUI_MERMAID_MAX_LABEL_LINES"), Default.MaxLabelLines),
            WrapMode = ParseEnum(Get(environment, "FTUI_MERMAID_WRAP_MODE"), Default.WrapMode),
            EnableStyles = ParseBool(Get(environment, "FTUI_MERMAID_ENABLE_STYLES"), Default.EnableStyles),
            EnableInitDirectives = ParseBool(Get(environment, "FTUI_MERMAID_ENABLE_INIT_DIRECTIVES"), Default.EnableInitDirectives),
            EnableLinks = ParseBool(Get(environment, "FTUI_MERMAID_ENABLE_LINKS"), Default.EnableLinks),
            LinkMode = ParseEnum(Get(environment, "FTUI_MERMAID_LINK_MODE"), Default.LinkMode),
            SanitizeMode = ParseEnum(Get(environment, "FTUI_MERMAID_SANITIZE_MODE"), Default.SanitizeMode),
            ErrorMode = ParseEnum(Get(environment, "FTUI_MERMAID_ERROR_MODE"), Default.ErrorMode),
            LogPath = Trim(Get(environment, "FTUI_MERMAID_LOG_PATH")),
            CacheEnabled = ParseBool(Get(environment, "FTUI_MERMAID_CACHE_ENABLED"), Default.CacheEnabled),
            CapabilityProfile = Trim(Get(environment, "FTUI_MERMAID_CAPABILITY_PROFILE")) ??
                                Trim(Get(environment, "FTUI_MERMAID_CAPS_PROFILE"))
        };
    }

    public IReadOnlyList<MermaidConfigError> Validate()
    {
        var errors = new List<MermaidConfigError>();
        ValidatePositive(errors, nameof(MaxNodes), MaxNodes);
        ValidatePositive(errors, nameof(MaxEdges), MaxEdges);
        ValidatePositive(errors, nameof(RouteBudget), RouteBudget);
        ValidatePositive(errors, nameof(LayoutIterationBudget), LayoutIterationBudget);
        ValidatePositive(errors, nameof(MaxLabelChars), MaxLabelChars);
        ValidatePositive(errors, nameof(MaxLabelLines), MaxLabelLines);

        if (!EnableLinks && LinkMode is not MermaidLinkMode.Off)
        {
            errors.Add(new MermaidConfigError(nameof(LinkMode), "LinkMode must be Off when links are disabled."));
        }

        return errors;
    }

    public MermaidConfigSummary ToSummary(int sampleCount = 0)
    {
        var validation = Validate();
        return new MermaidConfigSummary(
            validation.Count == 0 ? "ready" : "invalid",
            Enabled,
            GlyphMode.ToString().ToLowerInvariant(),
            TierOverride.ToString().ToLowerInvariant(),
            LinkMode.ToString().ToLowerInvariant(),
            EnableStyles,
            EnableInitDirectives,
            CacheEnabled,
            sampleCount,
            validation.Select(static error => $"{error.Field}: {error.Message}").ToArray());
    }

    public string ToJson() =>
        JsonSerializer.Serialize(
            this,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true
            });

    private static void ValidatePositive(List<MermaidConfigError> errors, string field, int value)
    {
        if (value < 1)
        {
            errors.Add(new MermaidConfigError(field, "Value must be >= 1."));
        }
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(Trim(value), ignoreCase: true, out var parsed) ? parsed : fallback;

    private static bool ParseBool(string? value, bool fallback)
    {
        var trimmed = Trim(value);
        if (trimmed is null)
        {
            return fallback;
        }

        return trimmed.ToLowerInvariant() switch
        {
            "1" or "true" or "yes" or "on" => true,
            "0" or "false" or "no" or "off" => false,
            _ => fallback
        };
    }

    private static int ParseInt(string? value, int fallback) =>
        int.TryParse(Trim(value), out var parsed) ? parsed : fallback;

    private static string? Get(IReadOnlyDictionary<string, string?> environment, string name) =>
        environment.TryGetValue(name, out var value) ? value : null;

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record MermaidConfigError(string Field, string Message);

public sealed record MermaidConfigSummary(
    string Status,
    bool Enabled,
    string GlyphMode,
    string TierOverride,
    string LinkMode,
    bool StylesEnabled,
    bool InitDirectivesEnabled,
    bool CacheEnabled,
    int SampleCount,
    IReadOnlyList<string> ValidationIssues);

public sealed record MermaidSample(
    string Id,
    string Name,
    string Category,
    IReadOnlyList<string> Tags,
    string ComplexityHint,
    int NodeCount,
    int EdgeCount,
    string Source,
    string UnicodePreview,
    string AsciiPreview);

public sealed record MermaidRenderSummary(
    double ParseMs,
    double LayoutMs,
    double RenderMs,
    int LayoutIterations,
    double ObjectiveScore,
    int ConstraintViolations,
    string FallbackTier,
    string Status);

public sealed record MermaidStatusLogEntry(
    string SchemaVersion,
    ulong TsMs,
    string Sample,
    string Mode,
    string Dims,
    string LayoutMode,
    string Fidelity,
    string Event,
    string Status,
    string Message);

public sealed record MermaidShowcaseState(
    MermaidConfig Config,
    MermaidSample Sample,
    MermaidDiagram Diagram,
    MermaidViewport Viewport,
    MermaidShowcasePreferences Preferences,
    MermaidLayoutMode LayoutMode,
    MermaidTier Fidelity,
    MermaidGlyphMode GlyphMode,
    MermaidWrapMode WrapMode,
    bool StylesEnabled,
    bool MetricsVisible,
    bool ControlsVisible,
    MermaidRenderSummary Summary,
    IReadOnlyList<MermaidStatusLogEntry> StatusLog,
    IReadOnlyList<MermaidConfigError> ValidationErrors,
    IReadOnlyList<MermaidDiagnostic> Diagnostics);

public static class MermaidShowcaseSurface
{
    private static readonly IReadOnlyList<MermaidSample> Samples =
    [
        new MermaidSample(
            "flow-01",
            "Flow-01",
            "Flow",
            ["cluster", "labels"],
            "M",
            5,
            4,
            """
            graph TD
              A[Plan] --> B{Audit}
              B --> C[Port]
              C --> D[Test]
              D --> E[Ship]
            """,
            """
            ╭──────╮   ╭──────╮   ╭──────╮
            │ Plan │──▶│Audit?│──▶│ Port │
            ╰──────╯   ╰──────╯   ╰──────╯
                               │
                               ▼
                            ╭──────╮
                            │ Test │
                            ╰──────╯
                               │
                               ▼
                            ╭──────╮
                            │ Ship │
                            ╰──────╯
            """,
            """
            +------+   +------+   +------+
            | Plan |-->|Audit?|-->| Port |
            +------+   +------+   +------+
                                 |
                                 v
                              +------+
                              | Test |
                              +------+
                                 |
                                 v
                              +------+
                              | Ship |
                              +------+
            """),
        new MermaidSample(
            "sequence-01",
            "Sequence-01",
            "Sequence",
            ["links", "actors"],
            "S",
            3,
            3,
            """
            sequenceDiagram
              User->>CLI: run doctor
              CLI-->>Port: capture parity
              Port-->>User: report
            """,
            """
            User    CLI     Port
             │      │        │
             ├─────▶│        │ run doctor
             │      ├───────▶│ capture parity
             │      │◀───────┤ report
             │◀─────┤        │
            """,
            """
            User    CLI     Port
             |      |        |
             |----->|        | run doctor
             |      |------->| capture parity
             |      |<-------| report
             |<-----|        |
            """),
        new MermaidSample(
            "state-01",
            "State-01",
            "State",
            ["stress", "fallback"],
            "L",
            7,
            8,
            """
            stateDiagram-v2
              [*] --> Idle
              Idle --> Loading
              Loading --> Ready
              Ready --> Failed
              Failed --> Idle
            """,
            """
            [*] → Idle → Loading → Ready
                        │         │
                        └────────▶Failed
                                  │
                                  └──────▶ Idle
            """,
            """
            [*] -> Idle -> Loading -> Ready
                         |           |
                         +---------> Failed
                                      |
                                      +-----> Idle
            """)
    ];

    public static MermaidShowcaseState BuildState(
        HostedParitySession session,
        ushort width = 72,
        ushort height = 18,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        var config = MermaidConfig.FromEnvironment(environment);
        var preferences = session.Mermaid with
        {
            LayoutMode = session.Mermaid.LayoutMode == MermaidLayoutMode.Auto && session.OverlayVisible
                ? MermaidLayoutMode.Dense
                : session.Mermaid.LayoutMode == MermaidLayoutMode.Auto && session.ModalOpen
                    ? MermaidLayoutMode.Spacious
                    : session.Mermaid.LayoutMode,
            Fidelity = session.Mermaid.Fidelity == MermaidTier.Auto && config.TierOverride != MermaidTier.Auto
                ? config.TierOverride
                : session.Mermaid.Fidelity,
            GlyphMode = session.Mermaid.GlyphMode == MermaidGlyphMode.Unicode && config.GlyphMode == MermaidGlyphMode.Ascii
                ? MermaidGlyphMode.Ascii
                : session.Mermaid.GlyphMode,
            WrapMode = session.Mermaid.WrapMode == MermaidWrapMode.WordChar ? config.WrapMode : session.Mermaid.WrapMode,
            StylesEnabled = session.Mermaid.StylesEnabled && config.EnableStyles
        };

        var sample = Samples[Math.Abs(preferences.SelectedSampleIndex) % Samples.Count];
        var diagram = MermaidEngine.Parse(sample, config with
        {
            EnableStyles = preferences.StylesEnabled
        });
        var viewport = MermaidEngine.Render(
            diagram,
            config with
            {
                EnableStyles = preferences.StylesEnabled
            },
            preferences,
            width,
            height);
        var layoutMode = preferences.LayoutMode;
        var fidelity = preferences.Fidelity == MermaidTier.Auto
            ? sample.NodeCount > 6 ? MermaidTier.Normal : MermaidTier.Rich
            : preferences.Fidelity;
        var glyphMode = preferences.GlyphMode;
        var summary = BuildSummary(sample, diagram, viewport, config, layoutMode, fidelity, width, height);
        var statusLog = BuildStatusLog(sample, session, layoutMode, fidelity, width, height, config, diagram, viewport, summary);
        var validation = config.Validate();
        return new MermaidShowcaseState(
            config,
            sample,
            diagram,
            viewport,
            preferences,
            layoutMode,
            fidelity,
            glyphMode,
            preferences.WrapMode,
            preferences.StylesEnabled,
            preferences.MetricsVisible,
            preferences.ControlsVisible,
            summary,
            statusLog,
            validation,
            viewport.Diagnostics);
    }

    public static MermaidConfigSummary CreateSummary(IReadOnlyDictionary<string, string?>? environment = null) =>
        MermaidConfig.FromEnvironment(environment).ToSummary(Samples.Count);

    public static IWidget CreateWidget(MermaidShowcaseState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(1), new ParagraphWidget(
                    $"Mermaid Showcase  Sample: {state.Sample.Name}  Layout: {state.LayoutMode}  Tier: {state.Fidelity}  Status: {state.Summary.Status.ToUpperInvariant()}")),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(24), new PanelWidget
                        {
                                Title = "Library",
                                Child = new ListWidget
                                {
                                    Items = Samples.Select(static sample => $"{sample.Name} [{sample.Category}]").ToArray(),
                                    SelectedIndex = Array.FindIndex(Samples.ToArray(), sample => string.Equals(sample.Id, state.Sample.Id, StringComparison.Ordinal))
                                }
                            }),
                        (LayoutConstraint.Percentage(46), new PanelWidget
                        {
                            Title = "Viewport",
                            Child = new ParagraphWidget(BuildViewport(state))
                        }),
                        (LayoutConstraint.Fill(), new StackWidget(
                            LayoutDirection.Vertical,
                            [
                                (LayoutConstraint.Fixed((ushort)(state.ControlsVisible ? 12 : 4)), new PanelWidget
                                {
                                    Title = "Controls",
                                    Child = new ParagraphWidget(BuildControls(state))
                                }),
                                (LayoutConstraint.Fixed((ushort)(state.MetricsVisible ? 10 : 4)), new PanelWidget
                                {
                                    Title = "Metrics",
                                    Child = new ParagraphWidget(BuildMetricsText(state))
                                }),
                                (LayoutConstraint.Fill(), new PanelWidget
                                {
                                    Title = "Status Log",
                                    Child = new ParagraphWidget(string.Join(Environment.NewLine, state.StatusLog.Select(FormatStatusLog)))
                                })
                            ]))
                    ]))
            ]);
    }

    public static IReadOnlyList<HostedParityMetric> BuildMetrics(HostedParitySession session)
    {
        var state = BuildState(session);
        return
        [
            new HostedParityMetric("MermaidSample", state.Sample.Name),
            new HostedParityMetric("MermaidTier", state.Fidelity.ToString().ToLowerInvariant()),
            new HostedParityMetric("MermaidGlyph", state.GlyphMode.ToString().ToLowerInvariant()),
            new HostedParityMetric("MermaidNodes", state.Sample.NodeCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new HostedParityMetric("MermaidEdges", state.Diagram.Edges.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new HostedParityMetric("MermaidStatus", state.Summary.Status, state.ValidationErrors.Count == 0 && state.Diagnostics.All(static item => item.Severity is not MermaidDiagnosticSeverity.Error))
        ];
    }

    public static IReadOnlyList<MermaidSample> Catalog() => Samples;

    private static MermaidRenderSummary BuildSummary(
        MermaidSample sample,
        MermaidDiagram diagram,
        MermaidViewport viewport,
        MermaidConfig config,
        MermaidLayoutMode layoutMode,
        MermaidTier fidelity,
        ushort width,
        ushort height)
    {
        var parseMs = Math.Round(sample.Source.Length / 28.0 + diagram.Diagnostics.Count * 0.1, 2);
        var layoutMs = Math.Round(Math.Min(config.LayoutIterationBudget, Math.Max(diagram.Nodes.Count * Math.Max(diagram.Edges.Count, 1), 1)) / 18.0, 2);
        var renderMs = Math.Round((width + height + viewport.Rows.Sum(static row => row.Length)) / 64.0, 2);
        var iterations = Math.Min(config.LayoutIterationBudget, Math.Max(diagram.Nodes.Count * 4, 1));
        var objective = Math.Round(
            diagram.Edges.Count / 2.5 +
            (layoutMode == MermaidLayoutMode.Dense ? 1.4 : layoutMode == MermaidLayoutMode.Spacious ? 0.8 : 1.0) +
            (fidelity == MermaidTier.Compact ? 0.2 : fidelity == MermaidTier.Rich ? 0.5 : 0.35),
            2);
        var violations = config.Validate().Count + diagram.Diagnostics.Count(static item => item.Severity == MermaidDiagnosticSeverity.Error);
        var warnings = diagram.Diagnostics.Count(static item => item.Severity == MermaidDiagnosticSeverity.Warn);
        var status = violations == 0 && warnings == 0 && config.Enabled ? "ok" : violations > 0 ? "err" : "warn";
        return new MermaidRenderSummary(
            parseMs,
            layoutMs,
            renderMs,
            iterations,
            objective,
            violations,
            fidelity.ToString().ToLowerInvariant(),
            status);
    }

    private static IReadOnlyList<MermaidStatusLogEntry> BuildStatusLog(
        MermaidSample sample,
        HostedParitySession session,
        MermaidLayoutMode layoutMode,
        MermaidTier fidelity,
        ushort width,
        ushort height,
        MermaidConfig config,
        MermaidDiagram diagram,
        MermaidViewport viewport,
        MermaidRenderSummary summary)
    {
        var entries = new List<MermaidStatusLogEntry>
        {
            new(
                "mermaid-statuslog-v1",
                (ulong)(session.StepCount * 100),
                sample.Name,
                session.InlineMode ? "inline" : "altscreen",
                $"{width}x{height}",
                layoutMode.ToString().ToLowerInvariant(),
                fidelity.ToString().ToLowerInvariant(),
                "render_start",
                "ok",
                $"Preparing {sample.Name}."),
            new(
                "mermaid-statuslog-v1",
                (ulong)(session.StepCount * 100 + 16),
                sample.Name,
                session.InlineMode ? "inline" : "altscreen",
                $"{width}x{height}",
                layoutMode.ToString().ToLowerInvariant(),
                fidelity.ToString().ToLowerInvariant(),
                "render_done",
                summary.Status == "ok" ? "ok" : "warn",
                $"Rendered {diagram.Nodes.Count} nodes / {diagram.Edges.Count} edges.")
        };

        entries.Add(new MermaidStatusLogEntry(
            "mermaid-statuslog-v1",
            (ulong)(session.StepCount * 100 + 20),
            sample.Name,
            session.InlineMode ? "inline" : "altscreen",
            $"{width}x{height}",
            layoutMode.ToString().ToLowerInvariant(),
            fidelity.ToString().ToLowerInvariant(),
            "mermaid_render",
            summary.Status,
            $"palette=default guard=default zoom=100% viewport={width}x{height}."));

        if (!summary.Status.Equals("ok", StringComparison.Ordinal))
        {
            entries.Add(new MermaidStatusLogEntry(
                "mermaid-statuslog-v1",
                (ulong)(session.StepCount * 100 + 24),
                sample.Name,
                session.InlineMode ? "inline" : "altscreen",
                $"{width}x{height}",
                layoutMode.ToString().ToLowerInvariant(),
                fidelity.ToString().ToLowerInvariant(),
                "fallback_used",
                summary.Status == "err" ? "error" : "warn",
                $"Fidelity {summary.FallbackTier}; diagnostics={viewport.Diagnostics.Count}."));
        }

        if (!config.EnableStyles)
        {
            entries.Add(new MermaidStatusLogEntry(
                "mermaid-statuslog-v1",
                (ulong)(session.StepCount * 100 + 28),
                sample.Name,
                session.InlineMode ? "inline" : "altscreen",
                $"{width}x{height}",
                layoutMode.ToString().ToLowerInvariant(),
                fidelity.ToString().ToLowerInvariant(),
                "layout_warning",
                "warn",
                "Styles disabled; structural rendering only."));
        }

        var nextOffset = 32UL;
        foreach (var issue in config.Validate()
                     .Select(error => new MermaidDiagnostic("mermaid/config/error", MermaidDiagnosticSeverity.Error, error.Message))
                     .Concat(diagram.Diagnostics)
                     .Take(4))
        {
            entries.Add(new MermaidStatusLogEntry(
                "mermaid-statuslog-v1",
                (ulong)(session.StepCount * 100) + nextOffset,
                sample.Name,
                session.InlineMode ? "inline" : "altscreen",
                $"{width}x{height}",
                layoutMode.ToString().ToLowerInvariant(),
                fidelity.ToString().ToLowerInvariant(),
                issue.Severity == MermaidDiagnosticSeverity.Error ? "error" : "route_warning",
                issue.Severity == MermaidDiagnosticSeverity.Error ? "error" : "warn",
                issue.Message));
            nextOffset += 8;
        }

        return entries;
    }

    private static string BuildViewport(MermaidShowcaseState state) =>
        string.Join(Environment.NewLine, state.Viewport.Rows);

    private static string BuildControls(MermaidShowcaseState state) =>
        string.Join(
            Environment.NewLine,
            [
                $"Sample: {state.Sample.Name}",
                $"Glyph: {state.GlyphMode.ToString().ToLowerInvariant()}",
                "Render: adaptive",
                "Palette: default",
                "Guard: default",
                "Zoom: 100%  Pan: 0,0",
                "Viewport override: off",
                $"Layout: {state.LayoutMode.ToString().ToLowerInvariant()}",
                $"Tier: {state.Fidelity.ToString().ToLowerInvariant()}",
                $"Wrap: {state.WrapMode.ToString().ToLowerInvariant()}",
                $"Styles: {(state.StylesEnabled ? "on" : "off")}",
                $"Links: {state.Config.LinkMode.ToString().ToLowerInvariant()}",
                $"Cache: {(state.Config.CacheEnabled ? "on" : "off")}",
                "m metrics | c controls | i status | p palette | g guard"
            ]);

    private static string BuildMetricsText(MermaidShowcaseState state) =>
        string.Join(
            Environment.NewLine,
            [
                $"Parse: {state.Summary.ParseMs:0.00} ms",
                $"Layout: {state.Summary.LayoutMs:0.00} ms",
                $"Render: {state.Summary.RenderMs:0.00} ms",
                $"Iter: {state.Summary.LayoutIterations}",
                $"Score: {state.Summary.ObjectiveScore:0.00}",
                $"Violations: {state.Summary.ConstraintViolations}",
                $"Crossings: {Math.Max(0, state.Diagram.Edges.Count - state.Diagram.Nodes.Count / 2)}",
                $"Symmetry: {Math.Min(1.0, state.Diagram.Nodes.Count == 0 ? 0.0 : 1.0 / Math.Max(1, Math.Abs(state.Diagram.Nodes.Count - state.Diagram.Edges.Count))):0.00}",
                $"Compactness: {Math.Min(1.0, state.Diagram.Nodes.Count / 20.0):0.00}",
                $"Fallback: {state.Summary.FallbackTier}",
                $"Diag: {state.Diagnostics.Count}"
            ]);

    private static string FormatStatusLog(MermaidStatusLogEntry entry) =>
        $"{entry.Event} [{entry.Status}] {entry.Message}";
}
