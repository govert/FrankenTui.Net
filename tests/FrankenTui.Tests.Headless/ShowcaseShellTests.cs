using FrankenTui.Core;
using FrankenTui.Demo.Showcase;
using FrankenTui.Extras;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Style;
using FrankenTui.Runtime;
using FrankenTui.Widgets;
using System.Text;
using System.Text.Json;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public sealed class ShowcaseShellTests
{
    [Fact]
    public void ShowcaseViewFactoryRendersUpstreamScreenCatalogSurface()
    {
        var buffer = new RenderBuffer(72, 18);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 5, frame: 1)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(72, 18), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Widget", screen);
        Assert.Contains("render completeness", screen);
        Assert.Contains("45", screen);
    }

    [Fact]
    public void ShowcaseCommandPaletteEvidenceLabRendersRankingEvidencePanels()
    {
        var buffer = new RenderBuffer(120, 30);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 39, frame: 1)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(120, 30), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Evidence Ledger", screen);
        Assert.Contains("Selected Result", screen);
        Assert.Contains("Type to filter", screen);
        Assert.Contains("Bench", screen);
        Assert.Contains("Hint Ranker", screen);
        Assert.Contains("match_type", screen);
        Assert.Contains("cmd:logs", screen);
    }

    [Fact]
    public void ShowcaseTerminalCapabilitiesRendersMatrixEvidenceAndSimulationPanels()
    {
        var buffer = new RenderBuffer(120, 30);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 12, frame: 1)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(120, 30), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Capability Matrix", screen);
        Assert.Contains("Evidence Ledger", screen);
        Assert.Contains("Profile Simulation", screen);
        Assert.Contains("view_mode_changed", screen);
        Assert.Contains("profile_cycled", screen);
        Assert.Contains("environment", screen);
        Assert.Contains("sync_output", screen);
        Assert.Contains("0-5 quick profile", screen);
    }

    [Fact]
    public void ShowcaseMacroRecorderRendersTimelineDetailAndScenarioRunnerPanels()
    {
        var buffer = new RenderBuffer(120, 30);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 13, frame: 1)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(120, 30), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Macro Recorder", screen);
        Assert.Contains("Timeline", screen);
        Assert.Contains("Event Detail", screen);
        Assert.Contains("Scenario Runner", screen);
        Assert.Contains("Tab Tour", screen);
        Assert.Contains("Search Flow", screen);
        Assert.Contains("Space/r record/stop", screen);
        Assert.Contains("macro_event", screen);
    }

    [Fact]
    public void ShowcasePerformanceRendersVirtualizedListStatsAndNavigationStatus()
    {
        var buffer = new RenderBuffer(120, 30);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 14, frame: 2)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(120, 30), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Virtualized List", screen);
        Assert.Contains("10000 items", screen);
        Assert.Contains("Performance Stats", screen);
        Assert.Contains("Only visible rows are rendered", screen);
        Assert.Contains("Event #", screen);
        Assert.Contains("Ctrl+D/U: page", screen);
        Assert.Contains("g/G: jump", screen);
    }

    [Fact]
    public void ShowcaseMarkdownRendersRendererStreamingAndAuxiliaryPanels()
    {
        var buffer = new RenderBuffer(132, 34);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 15, frame: 3)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(132, 34), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Markdown Renderer", screen);
        Assert.Contains("LLM Streaming Simulation", screen);
        Assert.Contains("Markdown Detection", screen);
        Assert.Contains("Style Sampler", screen);
        Assert.Contains("Unicode Showcase", screen);
        Assert.Contains("Wrap: Word", screen);
        Assert.Contains("GitHub-Flavored Markdown", screen);
        Assert.Contains("Detection:", screen);
    }

    [Fact]
    public void ShowcaseMermaidRendersControlsMetricsAndStatusDepth()
    {
        var buffer = new RenderBuffer(140, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 16, frame: 1)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(140, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Mermaid Showcase", screen);
        Assert.Contains("Library", screen);
        Assert.Contains("Viewport", screen);
        Assert.Contains("Controls", screen);
        Assert.Contains("Metrics", screen);
        Assert.Contains("Status Log", screen);
        Assert.Contains("Palette: default", screen);
        Assert.Contains("Guard: default", screen);
        Assert.Contains("Zoom: 100%", screen);
        Assert.Contains("Crossings:", screen);
        Assert.Contains("mermaid_render", screen);
    }

    [Fact]
    public void ShowcaseMermaidMegaRendersSampleLibraryControlsAndRecomputeEvidence()
    {
        var buffer = new RenderBuffer(150, 38);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 17, frame: 1)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(150, 38), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Mermaid Showcase", screen);
        Assert.Contains("Mega Sample Library", screen);
        Assert.Contains("Mega Controls", screen);
        Assert.Contains("Node Detail", screen);
        Assert.Contains("mermaid_mega_recompute", screen);
        Assert.Contains("Sample filter", screen);
        Assert.Contains("Layout budget: 16ms", screen);
        Assert.Contains("Node navigation", screen);
        Assert.Contains("parse_ms", screen);
    }

    [Fact]
    public void ShowcaseResponsiveLayoutRendersBreakpointVisibilityAndAdaptivePanels()
    {
        var buffer = new RenderBuffer(190, 30);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 19, frame: 4)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(190, 30), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Breakpoint:", screen);
        Assert.Contains("Thresholds: sm>=60 md>=90 lg>=120 xl>=160", screen);
        Assert.Contains("Sidebar", screen);
        Assert.Contains("Aside", screen);
        Assert.Contains("The layout adapts", screen);
    }

    [Fact]
    public void ShowcaseLogSearchRendersStreamingControlsAndDiagnostics()
    {
        var buffer = new RenderBuffer(150, 34);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 20, frame: 2)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(150, 34), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Log Search", screen);
        Assert.Contains("Live Stream", screen);
        Assert.Contains("Search Controls", screen);
        Assert.Contains("Diagnostics", screen);
        Assert.Contains("FTUI_LOGSEARCH_DIAGNOSTICS", screen);
        Assert.Contains("Max lines: 5000", screen);
        Assert.Contains("n/N next/prev", screen);
        Assert.Contains("search_opened", screen);
        Assert.Contains("filter_applied", screen);
        Assert.Contains("match_navigation", screen);
    }

    [Fact]
    public void ShowcaseNotificationsRendersQueueLifecycleControlsAndToastStack()
    {
        var buffer = new RenderBuffer(150, 34);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 21, frame: 3)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(150, 34), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Notification Demo", screen);
        Assert.Contains("Notification Stack", screen);
        Assert.Contains("Toast Queue Lifecycle", screen);
        Assert.Contains("max_visible=4", screen);
        Assert.Contains("max_queued=20", screen);
        Assert.Contains("TopRight", screen);
        Assert.Contains("Ack / Snooze", screen);
        Assert.Contains("Retry", screen);
        Assert.Contains("push -> display", screen);
        Assert.Contains("Tick: queue expiry", screen);
    }

    [Fact]
    public void ShowcaseActionTimelineRendersFiltersTimelineDetailsAndDiagnostics()
    {
        var buffer = new RenderBuffer(160, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 22, frame: 5)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(160, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Filters + Follow", screen);
        Assert.Contains("Event Timeline", screen);
        Assert.Contains("Event Detail", screen);
        Assert.Contains("Max events: 500", screen);
        Assert.Contains("Burst: every 2 ticks", screen);
        Assert.Contains("action_timeline::tick", screen);
        Assert.Contains("buffer_eviction", screen);
        Assert.Contains("Follow[F]: ON", screen);
        Assert.Contains("Severity", screen);
        Assert.Contains("ansi_bytes", screen);
    }

    [Fact]
    public void ShowcaseIntrinsicSizingRendersScenariosControlsAndPaneStudio()
    {
        var buffer = new RenderBuffer(170, 34);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 23, frame: 2)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 34), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Intrinsic Sizing Demo", screen);
        Assert.Contains("Scenarios", screen);
        Assert.Contains("Adaptive Sidebar", screen);
        Assert.Contains("Flexible Cards", screen);
        Assert.Contains("Auto-Sizing Table", screen);
        Assert.Contains("Responsive Form", screen);
        Assert.Contains("Effective width:", screen);
        Assert.Contains("w cycle width preset", screen);
        Assert.Contains("Embedded Pane Studio", screen);
        Assert.Contains("Drag panes", screen);
    }

    [Fact]
    public void ShowcaseLayoutInspectorRendersScenarioStepsOverlayTreeAndPaneStudio()
    {
        var buffer = new RenderBuffer(170, 34);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 24, frame: 4)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 34), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Layout Inspector", screen);
        Assert.Contains("Constraint Overlay", screen);
        Assert.Contains("Layout Tree", screen);
        Assert.Contains("Pane Studio", screen);
        Assert.Contains("Tight Grid", screen);
        Assert.Contains("Step: Allocation", screen);
        Assert.Contains("Overlay: on", screen);
        Assert.Contains("Requested", screen);
        Assert.Contains("UNDER", screen);
        Assert.Contains("ConstraintOverlay", screen);
    }

    [Fact]
    public void ShowcaseAdvancedTextEditorRendersSearchHistoryAndDiagnostics()
    {
        var buffer = new RenderBuffer(170, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 25, frame: 3)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Advanced Text Editor", screen);
        Assert.Contains("Search / Replace", screen);
        Assert.Contains("Undo History", screen);
        Assert.Contains("Diagnostics", screen);
        Assert.Contains("FTUI_TEXTEDITOR_DIAGNOSTICS", screen);
        Assert.Contains("FTUI_TEXTEDITOR_DETERMINISTIC", screen);
        Assert.Contains("replace_all_performed", screen);
        Assert.Contains("history_panel_toggled", screen);
        Assert.Contains("Ctrl+Left/Right", screen);
        Assert.Contains("Ln 9, Col 26", screen);
    }

    [Fact]
    public void ShowcaseMousePlaygroundRendersTargetsEventsStatsAndDiagnostics()
    {
        var buffer = new RenderBuffer(170, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 26, frame: 6)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Hit-Test Targets", screen);
        Assert.Contains("Event Log", screen);
        Assert.Contains("Stats + Overlay", screen);
        Assert.Contains("Controls + Diagnostics", screen);
        Assert.Contains("FTUI_MOUSE_DIAGNOSTICS", screen);
        Assert.Contains("FTUI_MOUSE_DETERMINISTIC", screen);
        Assert.Contains("hover_change", screen);
        Assert.Contains("jitter_stats_toggle", screen);
        Assert.Contains("Telemetry hooks", screen);
        Assert.Contains("Grid: 4 cols x 3 rows", screen);
    }

    [Fact]
    public void ShowcaseFormValidationRendersFieldsRulesStateAndDiagnostics()
    {
        var buffer = new RenderBuffer(170, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 27, frame: 4)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Registration Form", screen);
        Assert.Contains("Mode: Real-time", screen);
        Assert.Contains("Error Summary", screen);
        Assert.Contains("Validation Rules", screen);
        Assert.Contains("Notifications", screen);
        Assert.Contains("Mouse + Diagnostics", screen);
        Assert.Contains("Username", screen);
        Assert.Contains("Confirm Password", screen);
        Assert.Contains("Accept Terms", screen);
        Assert.Contains("Validation Failed", screen);
        Assert.Contains("errors_injected", screen);
        Assert.Contains("ValidationMode", screen);
    }

    [Fact]
    public void ShowcaseVirtualizedSearchRendersSearchResultsStatsAndDiagnostics()
    {
        var buffer = new RenderBuffer(170, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 28, frame: 5)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Search (/ to focus, Esc to clear)", screen);
        Assert.Contains("Results (1250 of 10000 match)", screen);
        Assert.Contains("Configuration ::", screen);
        Assert.Contains("CoreService", screen);
        Assert.Contains("Stats", screen);
        Assert.Contains("Query:    \"cfg\"", screen);
        Assert.Contains("FTUI_VSEARCH_DIAGNOSTICS", screen);
        Assert.Contains("FTUI_VSEARCH_DETERMINISTIC", screen);
        Assert.Contains("query_change", screen);
        Assert.Contains("fuzzy_match", screen);
        Assert.Contains("TelemetryHooks", screen);
        Assert.Contains("MatchPos", screen);
    }

    [Fact]
    public void ShowcaseAsyncTasksRendersSchedulerQueueHazardAndDiagnostics()
    {
        var buffer = new RenderBuffer(170, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 29, frame: 5)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Scheduler", screen);
        Assert.Contains("Task Queue", screen);
        Assert.Contains("Task Details", screen);
        Assert.Contains("Activity", screen);
        Assert.Contains("Policy + Evidence", screen);
        Assert.Contains("Hazard + Diagnostics", screen);
        Assert.Contains("SRPT", screen);
        Assert.Contains("Smith", screen);
        Assert.Contains("RoundRobin", screen);
        Assert.Contains("bounded_concurrency", screen);
        Assert.Contains("scheduling_decision", screen);
        Assert.Contains("cancellation_decision", screen);
        Assert.Contains("n:spawn", screen);
    }

    [Fact]
    public void ShowcaseThemeStudioRendersPresetsTokensExportsAndDiagnostics()
    {
        var buffer = new RenderBuffer(170, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 30, frame: 3)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Presets", screen);
        Assert.Contains("Token Inspector", screen);
        Assert.Contains("Cyberpunk Aurora", screen);
        Assert.Contains("High Contrast", screen);
        Assert.Contains("fg::PRIMARY", screen);
        Assert.Contains("accent::SUCCESS", screen);
        Assert.Contains("WCAG", screen);
        Assert.Contains("Export", screen);
        Assert.Contains("Ghostty", screen);
        Assert.Contains("FTUI_THEME_STUDIO_DIAGNOSTICS", screen);
        Assert.Contains("theme_exported", screen);
        Assert.Contains("TelemetryHooks", screen);
        Assert.Contains("Ctrl+T", screen);
    }

    [Fact]
    public void ShowcaseSnapshotPlayerRendersTimelineCompareInfoAndDiagnostics()
    {
        var buffer = new RenderBuffer(170, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 31, frame: 8)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Timeline (9/50)", screen);
        Assert.Contains("Frame Preview", screen);
        Assert.Contains("Frame A/B Compare", screen);
        Assert.Contains("Frame Info", screen);
        Assert.Contains("Controls", screen);
        Assert.Contains("Diagnostics + Export", screen);
        Assert.Contains("Checksum", screen);
        Assert.Contains("Chain hash", screen);
        Assert.Contains("Heatmap: Overlay", screen);
        Assert.Contains("time_travel_report", screen);
        Assert.Contains("diff_cells", screen);
        Assert.Contains("playback determinism", screen);
        Assert.Contains("Click timeline", screen);
    }

    [Fact]
    public void ShowcasePerformanceChallengeRendersMetricsSparklineBudgetAndStressEvidence()
    {
        var buffer = new RenderBuffer(170, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 32, frame: 7)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("PERFORMANCE CHALLENGE MODE", screen);
        Assert.Contains("Real-Time Metrics", screen);
        Assert.Contains("Tick Intervals", screen);
        Assert.Contains("Render Budget", screen);
        Assert.Contains("Stress Harness", screen);
        Assert.Contains("Degradation Tiers", screen);
        Assert.Contains("JSONL + Mouse Evidence", screen);
        Assert.Contains("perf_challenge_tier_change", screen);
        Assert.Contains("tier_from", screen);
        Assert.Contains("penalty_ms", screen);
        Assert.Contains("1-4:tier", screen);
        Assert.Contains("scroll budget", screen);
    }

    [Fact]
    public void ShowcaseExplainabilityRendersEvidencePanelsTimelineAndControls()
    {
        var buffer = new RenderBuffer(170, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 33, frame: 6)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Explainability Cockpit", screen);
        Assert.Contains("Diff Strategy", screen);
        Assert.Contains("Resize Regime", screen);
        Assert.Contains("Budget Decisions", screen);
        Assert.Contains("Decision Timeline", screen);
        Assert.Contains("Source + Controls", screen);
        Assert.Contains("diff_decision", screen);
        Assert.Contains("decision_evidence", screen);
        Assert.Contains("budget_decision", screen);
        Assert.Contains("log_bayes_factor", screen);
        Assert.Contains("1/2/3/4 focus panels", screen);
        Assert.Contains("FTUI_DEMO_EVIDENCE_JSONL", screen);
    }

    [Fact]
    public void ShowcaseI18nRendersLocalePluralRtlStressAndExportEvidence()
    {
        var buffer = new RenderBuffer(170, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 34, frame: 4)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("i18n Stress Lab", screen);
        Assert.Contains("String Lookup", screen);
        Assert.Contains("Pluralization Rules", screen);
        Assert.Contains("RTL Layout Mirroring", screen);
        Assert.Contains("Stress Lab", screen);
        Assert.Contains("Locales: en es fr ru ar ja", screen);
        Assert.Contains("zero/one/two/few/many/other", screen);
        Assert.Contains("display_width", screen);
        Assert.Contains("grapheme_count", screen);
        Assert.Contains("truncate_to_width_with_info", screen);
        Assert.Contains("i18n_stress_report", screen);
        Assert.Contains("Shift+Left/Right grapheme cursor", screen);
    }

    [Fact]
    public void ShowcaseVoiOverlayRendersSamplerSectionsLedgerAndControls()
    {
        var buffer = new RenderBuffer(170, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 35, frame: 5)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("VOI Overlay", screen);
        Assert.Contains("Decision", screen);
        Assert.Contains("Posterior", screen);
        Assert.Contains("Observation", screen);
        Assert.Contains("VOI Ledger", screen);
        Assert.Contains("Overlay Controls", screen);
        Assert.Contains("should_sample", screen);
        Assert.Contains("expected_variance_after", screen);
        Assert.Contains("VoiLogEntry::Decision", screen);
        Assert.Contains("inline_auto_voi_snapshot", screen);
        Assert.Contains("Tab cycle section", screen);
        Assert.Contains("selected_ledger_idx", screen);
    }

    [Fact]
    public void ShowcaseInlineModeRendersScrollbackCompareControlsAndStressState()
    {
        var buffer = new RenderBuffer(170, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 36, frame: 6)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Mode: Inline", screen);
        Assert.Contains("Scrollback preserved in inline mode", screen);
        Assert.Contains("Inline Mode Story", screen);
        Assert.Contains("INLINE MODE - SCROLLBACK PRESERVED", screen);
        Assert.Contains("Alt-screen Story", screen);
        Assert.Contains("ALT-SCREEN MODE - SCROLLBACK HIDDEN", screen);
        Assert.Contains("Controls + Mouse", screen);
        Assert.Contains("T scrollback stress burst", screen);
        Assert.Contains("LOG_RATE_OPTIONS", screen);
        Assert.Contains("UI_HEIGHT_OPTIONS", screen);
        Assert.Contains("inline_bar", screen);
        Assert.Contains("mouse hit regions preserve scrollback", screen);
    }

    [Fact]
    public void ShowcaseAccessibilityRendersControlPanelWcagPreviewTelemetryAndToggles()
    {
        var buffer = new RenderBuffer(170, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 37, frame: 8)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Accessibility Control Panel", screen);
        Assert.Contains("Toggles", screen);
        Assert.Contains("WCAG Contrast", screen);
        Assert.Contains("Live Preview", screen);
        Assert.Contains("A11y Telemetry", screen);
        Assert.Contains("High Contrast", screen);
        Assert.Contains("Reduced Motion", screen);
        Assert.Contains("Large Text", screen);
        Assert.Contains("AA >= 4.5, AAA >= 7.0", screen);
        Assert.Contains("A11yToggleAction", screen);
        Assert.Contains("A11yEventKind", screen);
        Assert.Contains("layout_toggles hit rows", screen);
    }

    [Fact]
    public void ShowcaseWidgetBuilderRendersPresetsTreePreviewPropsExportAndMouseHints()
    {
        var buffer = new RenderBuffer(170, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 38, frame: 9)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Widget Builder Sandbox", screen);
        Assert.Contains("Presets", screen);
        Assert.Contains("Widget Tree", screen);
        Assert.Contains("Live Preview", screen);
        Assert.Contains("Props", screen);
        Assert.Contains("Export + Mouse", screen);
        Assert.Contains("Starter Kit", screen);
        Assert.Contains("WidgetKind ids", screen);
        Assert.Contains("widget_builder_export", screen);
        Assert.Contains("props_hash", screen);
        Assert.Contains("WidgetSnapshot", screen);
        Assert.Contains("Right-click", screen);
    }

    [Fact]
    public void ShowcaseDeterminismLabRendersEquivalenceScenariosReportAndControls()
    {
        var buffer = new RenderBuffer(170, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 40, frame: 10)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Determinism Lab", screen);
        Assert.Contains("Equivalence", screen);
        Assert.Contains("FullRedraw", screen);
        Assert.Contains("DirtyRows", screen);
        Assert.Contains("Scene Preview", screen);
        Assert.Contains("Checks", screen);
        Assert.Contains("Baseline (10f)", screen);
        Assert.Contains("Fault Injection", screen);
        Assert.Contains("Report + Determinism Env", screen);
        Assert.Contains("FTUI_DETERMINISM_LAB_REPORT", screen);
        Assert.Contains("determinism_report", screen);
        Assert.Contains("FNV-1a", screen);
        Assert.Contains("mouse hit regions", screen);
    }

    [Fact]
    public void ShowcaseHyperlinkPlaygroundRendersOsc8RegistryHitRegionsAndJsonl()
    {
        var buffer = new RenderBuffer(170, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 41, frame: 2)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Hyperlink Playground", screen);
        Assert.Contains("OSC-8 + Hit Regions", screen);
        Assert.Contains("Links (OSC-8)", screen);
        Assert.Contains("Details & Registry", screen);
        Assert.Contains("LinkRegistry", screen);
        Assert.Contains("HitRegion::Link", screen);
        Assert.Contains("OSC 8 open", screen);
        Assert.Contains("FTUI_LINK_REPORT_PATH", screen);
        Assert.Contains("focus_move", screen);
        Assert.Contains("mouse_activate", screen);
        Assert.Contains("LINK_HIT_BASE=8000", screen);
        Assert.Contains("keyboard and mouse accessibility", screen);
    }

    [Fact]
    public void ShowcaseKanbanBoardRendersColumnsDragHistoryAndEvidence()
    {
        var buffer = new RenderBuffer(170, 36);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 42, frame: 3)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Kanban Board", screen);
        Assert.Contains("Todo", screen);
        Assert.Contains("In Progress", screen);
        Assert.Contains("Done", screen);
        Assert.Contains("Design login page", screen);
        Assert.Contains("Add input validation", screen);
        Assert.Contains("Build nav component", screen);
        Assert.Contains("Project scaffolding", screen);
        Assert.Contains("h/l: column | j/k: card | H/L: move | u/r: undo/redo | mouse: drag | moves: 0", screen);
        Assert.Contains("Kanban Board [42/45] [h] [cmd] [p] [d]", screen);
    }

    [Fact]
    public void ShowcaseKanbanBoardKeyboardMovesUndoAndRedoMutateBoardState()
    {
        var timestamp = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(170, 36),
            screenNumber: 42,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Character, TerminalModifiers.Shift, new Rune('L')), timestamp);

        Assert.NotNull(state.KanbanBoard);
        var moved = state.KanbanBoard!;
        Assert.Equal(3, moved.Todo.Count);
        Assert.Equal(3, moved.InProgress.Count);
        Assert.Single(moved.History);
        Assert.True(moved.CanUndo);
        Assert.Equal(1, moved.FocusCol);
        Assert.Equal(2, moved.FocusRow);
        Assert.Equal(1, moved.InProgress[^1].Id);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('u')), timestamp.AddMilliseconds(10));

        var undone = state.KanbanBoard!;
        Assert.Equal(4, undone.Todo.Count);
        Assert.Equal(2, undone.InProgress.Count);
        Assert.Empty(undone.History);
        Assert.True(undone.CanRedo);
        Assert.Equal(1, undone.Todo[0].Id);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('r')), timestamp.AddMilliseconds(20));

        var redone = state.KanbanBoard!;
        Assert.Equal(3, redone.Todo.Count);
        Assert.Equal(3, redone.InProgress.Count);
        Assert.Single(redone.History);
        Assert.False(redone.CanRedo);

        var buffer = new RenderBuffer(170, 36);
        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("moves: 1", screen);
        Assert.Contains("> Design login page", screen);
    }

    [Fact]
    public void ShowcaseMarkdownLiveEditorRendersSearchPreviewDiffAndEvidence()
    {
        var buffer = new RenderBuffer(170, 40);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 43, frame: 5)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 40), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Live Markdown", screen);
        Assert.Contains("split editor + preview with search", screen);
        Assert.Contains("Search", screen);
        Assert.Contains("Query: preview", screen);
        Assert.Contains("search_ascii_case_insensitive", screen);
        Assert.Contains("Editor", screen);
        Assert.Contains("TextArea", screen);
        Assert.Contains("Preview", screen);
        Assert.Contains("Raw vs Rendered Width", screen);
        Assert.Contains("diff_mode=True", screen);
        Assert.Contains("MarkdownRenderer", screen);
        Assert.Contains("SyntaxHighlighter", screen);
        Assert.Contains("JSONL fields", screen);
        Assert.Contains("preview_scroll", screen);
    }

    [Fact]
    public void ShowcaseDragDropRendersModesListsKeyboardDragAndMouseEvidence()
    {
        var buffer = new RenderBuffer(170, 38);
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 44, frame: 2)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 38), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Drag & Drop Lab", screen);
        Assert.Contains("LIST_SIZE=8", screen);
        Assert.Contains("Sortable List", screen);
        Assert.Contains("Cross-Container", screen);
        Assert.Contains("Keyboard Drag", screen);
        Assert.Contains("List A", screen);
        Assert.Contains("List B", screen);
        Assert.Contains("Item 1 id=0", screen);
        Assert.Contains("File 1 id=8", screen);
        Assert.Contains("KeyboardDragManager", screen);
        Assert.Contains("Drop targets", screen);
        Assert.Contains("DragPayload::text", screen);
        Assert.Contains("right-click reorders or transfers", screen);
        Assert.Contains("JSONL fields", screen);
    }

    [Fact]
    public void ShowcaseVisualEffectsRendersDeterministicBrailleCanvas()
    {
        var first = new RenderBuffer(96, 24);
        var second = new RenderBuffer(96, 24);

        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 18, frame: 0)
            .Render(new RuntimeRenderContext(first, FrankenTui.Core.Rect.FromSize(96, 24), Theme.DefaultTheme));
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 18, frame: 1)
            .Render(new RuntimeRenderContext(second, FrankenTui.Core.Rect.FromSize(96, 24), Theme.DefaultTheme));

        var firstScreen = HeadlessBufferView.ScreenString(first);
        Assert.Contains("Effect", firstScreen);
        Assert.Contains("Harness", firstScreen);
        Assert.Contains(firstScreen, static ch => ch >= '\u2800' && ch <= '\u28ff');
        Assert.NotEqual(firstScreen, HeadlessBufferView.ScreenString(second));
    }

    [Fact]
    public void ShowcaseVisualEffectsCanvasRespondsToHarnessEffect()
    {
        var plasma = new RenderBuffer(96, 24);
        var matrix = new RenderBuffer(96, 24);

        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 18, frame: 2, vfxEffect: "plasma")
            .Render(new RuntimeRenderContext(plasma, FrankenTui.Core.Rect.FromSize(96, 24), Theme.DefaultTheme));
        ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 18, frame: 2, vfxEffect: "matrix")
            .Render(new RuntimeRenderContext(matrix, FrankenTui.Core.Rect.FromSize(96, 24), Theme.DefaultTheme));

        var plasmaScreen = HeadlessBufferView.ScreenString(plasma);
        var matrixScreen = HeadlessBufferView.ScreenString(matrix);
        Assert.Contains("Effect: plasma", plasmaScreen);
        Assert.Contains("Effect: matrix", matrixScreen);
        Assert.NotEqual(plasmaScreen, matrixScreen);
    }

    [Fact]
    public void ShowcaseVisualEffectsRecognizesUpstreamEffectKeysAndAliases()
    {
        foreach (var effect in ShowcaseVfxEffects.AllCanonicalKeys)
        {
            var buffer = new RenderBuffer(96, 24);
            ShowcaseViewFactory.Build(inlineMode: false, screenNumber: 18, frame: 3, vfxEffect: effect)
                .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(96, 24), Theme.DefaultTheme));

            var screen = HeadlessBufferView.ScreenString(buffer);
            Assert.Contains($"Effect: {effect}", screen);
            Assert.Contains(screen, static ch => ch >= '\u2800' && ch <= '\u28ff');
        }

        Assert.Equal("reaction-diffusion", ShowcaseVfxEffects.NormalizeName("rd"));
        Assert.Equal("strange-attractor", ShowcaseVfxEffects.NormalizeName("attractor"));
        Assert.Equal("flow-field", ShowcaseVfxEffects.NormalizeName("flow_field"));
        Assert.Equal("wave-interference", ShowcaseVfxEffects.NormalizeName("wave"));
        Assert.Equal("threejs-model", ShowcaseVfxEffects.NormalizeName("model-3d"));
        Assert.Equal("quake-e1m1", ShowcaseVfxEffects.NormalizeName("e1m1"));
    }

    [Fact]
    public void ShowcaseCommandPaletteEvidenceLabEmitsHintRankerEvidence()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-palette-lab-hints-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(["--screen=39", "--evidence-jsonl", path], _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(100, 24),
            screenNumber: 39,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var ledger = ShowcaseSurface.BuildPaletteLabHintLedger();

        Assert.NotEmpty(ledger);
        Assert.Equal("Enter Execute", ledger[0].Label);
        Assert.True(ledger[0].ExpectedUtility > 0.5);
        Assert.True(ledger[0].ValueOfInformation > 0);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteFrame("tick", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, state);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var frame = JsonDocument.Parse(line);
        Assert.Equal("Enter Execute", frame.RootElement.GetProperty("palette_lab_hint_top").GetString());
        Assert.Contains("Ctrl+P Open Palette", frame.RootElement.GetProperty("palette_lab_hint_ledger").GetString());
        Assert.Equal("0.833", frame.RootElement.GetProperty("palette_lab_hint_top_expected_utility").GetString());
    }

    [Fact]
    public void ShowcaseCommandPaletteEvidenceLabUsesUpstreamSampleActions()
    {
        var entries = ShowcaseCommandPalette.EvidenceLabEntries();

        Assert.Equal(12, entries.Count);
        Assert.Collection(
            entries,
            entry => Assert.Equal("cmd:open", entry.Id),
            entry => Assert.Equal("cmd:save", entry.Id),
            entry => Assert.Equal("cmd:find", entry.Id),
            entry => Assert.Equal("cmd:palette", entry.Id),
            entry => Assert.Equal("cmd:markdown", entry.Id),
            entry => Assert.Equal("cmd:logs", entry.Id),
            entry => Assert.Equal("cmd:perf", entry.Id),
            entry => Assert.Equal("cmd:inline", entry.Id),
            entry => Assert.Equal("cmd:theme", entry.Id),
            entry => Assert.Equal("cmd:help", entry.Id),
            entry => Assert.Equal("cmd:quit", entry.Id),
            entry => Assert.Equal("cmd:reload", entry.Id));

        var results = CommandPaletteController.Results(
            CommandPaletteState.Closed with { Query = "log" },
            entries);

        Assert.NotEmpty(results);
        Assert.Equal("cmd:logs", results[0].Entry.Id);
    }

    [Fact]
    public void ShowcaseCommandPaletteEvidenceLabMatchModeKeysFilterResults()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(100, 24),
            screenNumber: 39,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('3')),
            start);

        Assert.Equal(39, state.CurrentScreenNumber);
        Assert.Equal(ShowcasePaletteLabMatchFilter.WordStart, state.PaletteLabMatchFilter);

        var buffer = new RenderBuffer(120, 30);
        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(120, 30), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("active=WordStart", screen);
        Assert.Contains("Type to filter", screen);

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('m')),
            start + TimeSpan.FromMilliseconds(10));

        Assert.Equal(ShowcasePaletteLabMatchFilter.Substring, state.PaletteLabMatchFilter);
        Assert.False(state.MouseCaptureEnabled);
    }

    [Fact]
    public void ShowcaseCommandPaletteEvidenceLabBenchLoopAdvancesDeterministically()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-palette-lab-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(["--screen=39", "--evidence-jsonl", path], _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(100, 24),
            screenNumber: 39,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = state.ApplyTick(start, RuntimeFrameStats.Empty);

        Assert.False(state.PaletteLabBenchEnabled);
        Assert.Equal(0, state.PaletteLabBenchFrame);
        Assert.Equal("log", ShowcaseSurface.ResolvePaletteLabQuery(state, state.Session.CommandPalette));

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('b')),
            start + TimeSpan.FromMilliseconds(10));

        Assert.True(state.PaletteLabBenchEnabled);
        Assert.Equal("open", ShowcaseSurface.ResolvePaletteLabQuery(state, state.Session.CommandPalette));

        state = state.ApplyTick(start + TimeSpan.FromMilliseconds(20), RuntimeFrameStats.Empty);
        state = state.ApplyTick(start + TimeSpan.FromMilliseconds(30), RuntimeFrameStats.Empty);
        state = state.ApplyTick(start + TimeSpan.FromMilliseconds(40), RuntimeFrameStats.Empty);

        Assert.Equal(3, state.PaletteLabBenchFrame);
        Assert.Equal(1, state.PaletteLabBenchProcessed);
        Assert.Equal("theme", ShowcaseSurface.ResolvePaletteLabQuery(state, state.Session.CommandPalette));

        var buffer = new RenderBuffer(120, 30);
        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(120, 30), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("bench ON 003/001 'theme'", screen);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteFrame("tick", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, state);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var frame = JsonDocument.Parse(line);
        Assert.True(frame.RootElement.GetProperty("palette_lab_bench_enabled").GetBoolean());
        Assert.Equal(3, frame.RootElement.GetProperty("palette_lab_bench_frame").GetInt32());
        Assert.Equal(1, frame.RootElement.GetProperty("palette_lab_bench_processed").GetInt32());
        Assert.Equal(3, frame.RootElement.GetProperty("palette_lab_bench_step_ticks").GetInt32());
        Assert.Equal("theme", frame.RootElement.GetProperty("palette_lab_bench_query").GetString());
    }

    [Fact]
    public void ShowcaseCommandPaletteEvidenceLabMouseScrollsAndExecutesLocalPalette()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-palette-lab-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(["--screen=39", "--evidence-jsonl", path], _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(100, 24),
            screenNumber: 39,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight)
            with
            {
                Session = ShowcaseDemoState.Create(
                    inlineMode: false,
                    viewport: new FrankenTui.Core.Size(100, 24),
                    screenNumber: 39,
                    language: "en",
                    flowDirection: WidgetFlowDirection.LeftToRight).Session with
                {
                    CommandPalette = CommandPaletteState.Closed with { Query = "a" }
                }
            };
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var scroll = TerminalEvent.Mouse(
            new MouseGesture(2, 5, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp);
        var afterScroll = state.ApplyInput(Envelope(scroll, timestamp), RuntimeFrameStats.Empty);

        Assert.Equal(39, afterScroll.CurrentScreenNumber);
        Assert.Equal(3, afterScroll.Session.CommandPalette.SelectedIndex);

        var click = TerminalEvent.Mouse(
            new MouseGesture(2, 5, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(10));
        var afterClick = afterScroll.ApplyInput(
            Envelope(click, timestamp + TimeSpan.FromMilliseconds(10)),
            RuntimeFrameStats.Empty);

        Assert.Equal(39, afterClick.CurrentScreenNumber);
        Assert.StartsWith("cmd:", afterClick.Session.CommandPalette.LastExecutedCommandId);
        Assert.Contains("Executed", afterClick.Session.CommandPalette.Status);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, scroll, state, afterScroll);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, click, afterScroll, afterClick);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var scrollEvent = JsonDocument.Parse(lines[0]);
        Assert.Equal("palette_lab_scroll_down", scrollEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("palette_lab", scrollEvent.RootElement.GetProperty("hit_id").GetString());
        using var clickEvent = JsonDocument.Parse(lines[1]);
        Assert.Equal("palette_lab_execute", clickEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("palette_lab", clickEvent.RootElement.GetProperty("hit_id").GetString());
    }

    [Fact]
    public void ShowcaseCliOptionsHonorsUpstreamEnvironmentDefaults()
    {
        var environment = new Dictionary<string, string?>
        {
            ["FTUI_DEMO_SCREEN_MODE"] = "inline-auto",
            ["FTUI_DEMO_SCREEN"] = "5",
            ["FTUI_DEMO_TOUR"] = "true",
            ["FTUI_DEMO_TOUR_SPEED"] = "1.5",
            ["FTUI_DEMO_TOUR_START_STEP"] = "3",
            ["FTUI_DEMO_MOUSE"] = "off",
            ["FTUI_DEMO_UI_HEIGHT"] = "24",
            ["FTUI_DEMO_UI_MIN_HEIGHT"] = "10",
            ["FTUI_DEMO_UI_MAX_HEIGHT"] = "16",
            ["FTUI_DEMO_PANE_WORKSPACE"] = "/tmp/ftui-pane.json"
        };

        var options = ShowcaseCliOptions.Parse([], key => environment.GetValueOrDefault(key));

        Assert.Equal(ShowcaseScreenMode.InlineAuto, options.ScreenMode);
        Assert.True(options.InlineMode);
        Assert.Equal((ushort)16, options.Height);
        Assert.Equal((ushort)10, options.UiMinHeight);
        Assert.Equal((ushort)16, options.UiMaxHeight);
        Assert.Equal(5, options.ScreenNumber);
        Assert.True(options.Tour);
        Assert.Equal(1.5, options.TourSpeed);
        Assert.Equal(3, options.TourStartStep);
        Assert.False(options.UseMouseTracking);
        Assert.Equal("/tmp/ftui-pane.json", options.PaneWorkspacePath);
    }

    [Fact]
    public void ShowcaseCliOptionsLetCommandLineOverrideEnvironment()
    {
        var environment = new Dictionary<string, string?>
        {
            ["FTUI_DEMO_SCREEN_MODE"] = "inline",
            ["FTUI_DEMO_SCREEN"] = "3",
            ["FTUI_DEMO_MOUSE"] = "on",
            ["FTUI_DEMO_PANE_WORKSPACE"] = "/tmp/env-pane.json"
        };

        var options = ShowcaseCliOptions.Parse(
            ["--screen=7", "--screen-mode", "alt", "--no-mouse", "--frames=2", "--pane-workspace=/tmp/cli-pane.json"],
            key => environment.GetValueOrDefault(key));

        Assert.Equal(ShowcaseScreenMode.Alt, options.ScreenMode);
        Assert.False(options.InlineMode);
        Assert.Equal(7, options.ScreenNumber);
        Assert.Equal(2, options.Frames);
        Assert.False(options.InteractiveMode);
        Assert.Equal(ShowcaseMouseMode.Off, options.MouseMode);
        Assert.False(options.UseMouseTracking);
        Assert.Equal("/tmp/cli-pane.json", options.PaneWorkspacePath);
    }

    [Fact]
    public void ShowcaseViewportResolverUsesHostSizeForNormalAltScreenRuns()
    {
        var options = ShowcaseCliOptions.Parse(["--screen=42"], _ => null);

        var viewport = ShowcaseViewportResolver.Resolve(options, new Size(132, 43));

        Assert.Equal(new Size(132, 43), viewport);
        Assert.False(options.HasExplicitViewport);
    }

    [Fact]
    public void ShowcaseViewportResolverPreservesExplicitAndHarnessSizes()
    {
        var explicitOptions = ShowcaseCliOptions.Parse(["--screen=42", "--width=80", "--height=24"], _ => null);
        var vfxOptions = ShowcaseCliOptions.Parse(["--vfx-harness", "--vfx-cols=120", "--vfx-rows=40"], _ => null);

        Assert.Equal(new Size(80, 24), ShowcaseViewportResolver.Resolve(explicitOptions, new Size(132, 43)));
        Assert.Equal(new Size(120, 40), ShowcaseViewportResolver.Resolve(vfxOptions, new Size(132, 43)));
        Assert.True(explicitOptions.HasExplicitViewport);
    }

    [Fact]
    public void ShowcaseViewportResolverUsesTerminalWidthAndUiHeightForInlineRuns()
    {
        var inline = ShowcaseCliOptions.Parse(["--inline", "--ui-height=12"], _ => null);
        var inlineAuto = ShowcaseCliOptions.Parse(
            ["--screen-mode=inline-auto", "--ui-height=30", "--ui-min-height=10", "--ui-max-height=20"],
            _ => null);

        Assert.Equal(new Size(100, 12), ShowcaseViewportResolver.Resolve(inline, new Size(100, 40)));
        Assert.Equal(new Size(100, 20), ShowcaseViewportResolver.Resolve(inlineAuto, new Size(100, 40)));
        Assert.Equal(new Size(100, 8), ShowcaseViewportResolver.Resolve(inlineAuto, new Size(100, 8)));
    }

    [Fact]
    public void ShowcaseCliOptionsParseDeterministicAutomationEnvironment()
    {
        var environment = new Dictionary<string, string?>
        {
            ["FTUI_DEMO_DETERMINISTIC"] = "1",
            ["FTUI_DEMO_SEED"] = "12345",
            ["FTUI_DEMO_TICK_MS"] = "33",
            ["FTUI_DEMO_EXIT_AFTER_MS"] = "250",
            ["FTUI_DEMO_EXIT_AFTER_TICKS"] = "4",
            ["FTUI_DEMO_EVIDENCE_JSONL"] = "/tmp/ftui-evidence.jsonl"
        };

        var options = ShowcaseCliOptions.Parse([], key => environment.GetValueOrDefault(key));

        Assert.True(options.Deterministic);
        Assert.Equal((ulong)12345, options.DeterministicSeed);
        Assert.Equal((uint)33, options.TickIntervalMilliseconds);
        Assert.Equal((uint)250, options.ExitAfterMilliseconds);
        Assert.Equal((uint)4, options.ExitAfterTicks);
        Assert.Equal("/tmp/ftui-evidence.jsonl", options.EvidenceJsonlPath);
    }

    [Fact]
    public void ShowcaseCliHelpMentionsHarnessGoldenAndRunIdControls()
    {
        var help = ShowcaseCliHelp.Text;

        Assert.Contains("--vfx-run-id", help);
        Assert.Contains("--vfx-perf", help);
        Assert.Contains("--vfx-golden", help);
        Assert.Contains("--vfx-update-golden", help);
        Assert.Contains("--vfx-exit-after-ms", help);
        Assert.Contains("--vfx-cols", help);
        Assert.Contains("--vfx-seed", help);
        Assert.Contains("--mermaid-run-id", help);
        Assert.Contains("--mermaid-cols", help);
        Assert.Contains("--mermaid-seed", help);
        Assert.Contains("Windows uses the managed Windows console backend", help);
        Assert.Contains("crossterm-compat fallback guidance", help);
    }

    [Fact]
    public void ShowcaseCliOptionsLetAutomationArgumentsOverrideEnvironment()
    {
        var environment = new Dictionary<string, string?>
        {
            ["FTUI_DEMO_TICK_MS"] = "33",
            ["FTUI_DEMO_EXIT_AFTER_MS"] = "250",
            ["FTUI_DEMO_EXIT_AFTER_TICKS"] = "4",
            ["FTUI_DEMO_SEED"] = "111",
            ["FTUI_DEMO_EVIDENCE_JSONL"] = "/tmp/env-evidence.jsonl"
        };

        var options = ShowcaseCliOptions.Parse(
            ["--tick-ms=16", "--exit-after-ms=99", "--exit-after-ticks=2", "--deterministic", "--seed=222", "--evidence-jsonl=/tmp/cli-evidence.jsonl"],
            key => environment.GetValueOrDefault(key));

        Assert.True(options.Deterministic);
        Assert.Equal((ulong)222, options.DeterministicSeed);
        Assert.Equal((uint)16, options.TickIntervalMilliseconds);
        Assert.Equal((uint)99, options.ExitAfterMilliseconds);
        Assert.Equal((uint)2, options.ExitAfterTicks);
        Assert.Equal("/tmp/cli-evidence.jsonl", options.EvidenceJsonlPath);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsStableLaunchAndFrameRecords()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-evidence-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=5", "--width=72", "--height=18", "--evidence-jsonl", path],
            _ => null);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteLaunch(options);
            writer.WriteFrame(
                "scripted_frame",
                options,
                RuntimeFrameStats.Empty with
                {
                    LoadGovernorPidOutput = 0.75,
                    LoadGovernorPidP = 0.5,
                    LoadGovernorPidI = 0.2,
                    LoadGovernorPidD = 0.05,
                    LoadGovernorEProcessValue = 21,
                    LoadGovernorEProcessSigmaMs = 1.5,
                    LoadGovernorFramesObserved = 12,
                    LoadGovernorFramesSinceChange = 3,
                    LoadGovernorPidGateThreshold = 0.3,
                    LoadGovernorPidGateMargin = 0.45,
                    LoadGovernorEvidenceThreshold = 20,
                    LoadGovernorEvidenceMargin = 1,
                    LoadGovernorEProcessInWarmup = false,
                    LoadGovernorTransitionSeq = 2,
                    LoadGovernorTransitionCorrelationId = 8589934604,
                    CascadeDecision = "degrade",
                    CascadeLevelBefore = "FULL",
                    CascadeLevelAfter = "SIMPLE_BORDERS",
                    CascadeGuardState = "at_risk",
                    ConformalBucketKey = "altscreen:full:8",
                    ConformalUpperMicroseconds = 1200,
                    ConformalBudgetMicroseconds = 1000,
                    ConformalCalibrationSize = 3,
                    ConformalFallbackLevel = 0,
                    ConformalIntervalWidthMicroseconds = 400,
                    CascadeRecoveryStreak = 0,
                    CascadeRecoveryThreshold = 10
                },
                stepIndex: 1,
                frame: 1);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var launch = JsonDocument.Parse(lines[0]);
        using var frame = JsonDocument.Parse(lines[1]);
        Assert.Equal("launch", launch.RootElement.GetProperty("event").GetString());
        Assert.Equal(JsonValueKind.Null, launch.RootElement.GetProperty("pane_workspace_loaded").ValueKind);
        Assert.Equal(JsonValueKind.Null, launch.RootElement.GetProperty("pane_workspace_schema_version").ValueKind);
        Assert.Equal(JsonValueKind.Null, launch.RootElement.GetProperty("pane_workspace_migration_applied").ValueKind);
        Assert.Equal(JsonValueKind.Null, launch.RootElement.GetProperty("pane_workspace_migration_from_version").ValueKind);
        Assert.Equal(0, launch.RootElement.GetProperty("sequence").GetInt64());
        Assert.Equal(0, launch.RootElement.GetProperty("seq").GetInt64());
        Assert.Equal("frankentui-net-showcase", launch.RootElement.GetProperty("run_id").GetString());
        Assert.Equal((ulong)0, launch.RootElement.GetProperty("seed").GetUInt64());
        Assert.Equal("alt", launch.RootElement.GetProperty("screen_mode").GetString());
        Assert.Equal("Alt", launch.RootElement.GetProperty("launch_screen_mode").GetString());
        Assert.Equal(JsonValueKind.Null, launch.RootElement.GetProperty("deterministic_seed").ValueKind);
        Assert.Equal("test-jsonl-v1", launch.RootElement.GetProperty("upstream_schema_version").GetString());
        Assert.Equal("scripted_frame", frame.RootElement.GetProperty("event").GetString());
        Assert.Equal("widget_gallery", frame.RootElement.GetProperty("screen_slug").GetString());
        Assert.Equal(1, frame.RootElement.GetProperty("step_index").GetInt32());
        Assert.Equal(1, frame.RootElement.GetProperty("sequence").GetInt64());
        Assert.Equal(1, frame.RootElement.GetProperty("seq").GetInt64());
        Assert.Equal("frankentui-net-showcase", frame.RootElement.GetProperty("run_id").GetString());
        Assert.Equal((ulong)0, frame.RootElement.GetProperty("seed").GetUInt64());
        Assert.Equal("alt", frame.RootElement.GetProperty("screen_mode").GetString());
        Assert.Equal("test-jsonl-v1", frame.RootElement.GetProperty("upstream_schema_version").GetString());
        Assert.Equal(0.75, frame.RootElement.GetProperty("load_governor_pid_output").GetDouble());
        Assert.Equal(21, frame.RootElement.GetProperty("load_governor_e_value").GetDouble());
        Assert.Equal(12u, frame.RootElement.GetProperty("load_governor_frames_observed").GetUInt32());
        Assert.False(frame.RootElement.GetProperty("load_governor_in_warmup").GetBoolean());
        Assert.Equal((ulong)2, frame.RootElement.GetProperty("load_governor_transition_seq").GetUInt64());
        Assert.Equal((ulong)8589934604, frame.RootElement.GetProperty("load_governor_transition_correlation_id").GetUInt64());
        Assert.Equal("degrade", frame.RootElement.GetProperty("cascade_decision").GetString());
        Assert.Equal("SIMPLE_BORDERS", frame.RootElement.GetProperty("cascade_level_after").GetString());
        Assert.Equal("at_risk", frame.RootElement.GetProperty("cascade_guard_state").GetString());
        Assert.Equal("altscreen:full:8", frame.RootElement.GetProperty("conformal_bucket").GetString());
        Assert.Equal(1200, frame.RootElement.GetProperty("conformal_upper_us").GetDouble());
        Assert.Equal(1000, frame.RootElement.GetProperty("conformal_budget_us").GetDouble());
        Assert.Equal(3, frame.RootElement.GetProperty("conformal_calibration_size").GetInt32());
        Assert.Equal(0, frame.RootElement.GetProperty("conformal_fallback_level").GetByte());
        Assert.Equal(400, frame.RootElement.GetProperty("conformal_interval_width_us").GetDouble());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterIncludesPaneRecoveryEvidence()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-pane-evidence-{Guid.NewGuid():N}.jsonl");
        var invalidPath = Path.Combine(Path.GetTempPath(), $"ftui-showcase-pane-invalid-{Guid.NewGuid():N}.json.invalid");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=6", "--pane-workspace=workspace.json", "--evidence-jsonl", path],
            _ => null);
        var workspace = PaneWorkspaceState.CreateDemo();
        var load = new ShowcasePaneWorkspaceLoadResult(
            workspace,
            Loaded: false,
            Error: "invalid pane workspace JSON",
            InvalidSnapshotPath: invalidPath);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteLaunch(options, load);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var launch = JsonDocument.Parse(line);
        Assert.Equal("launch", launch.RootElement.GetProperty("event").GetString());
        Assert.False(launch.RootElement.GetProperty("pane_workspace_loaded").GetBoolean());
        Assert.Equal("invalid pane workspace JSON", launch.RootElement.GetProperty("pane_workspace_error").GetString());
        Assert.Equal(invalidPath, launch.RootElement.GetProperty("pane_workspace_invalid_snapshot").GetString());
        Assert.Equal(workspace.SnapshotHash(), launch.RootElement.GetProperty("pane_workspace_snapshot_hash").GetString());
        Assert.Equal(ShowcasePaneWorkspacePersistence.CurrentSchemaVersion, launch.RootElement.GetProperty("pane_workspace_schema_version").GetString());
        Assert.False(launch.RootElement.GetProperty("pane_workspace_migration_applied").GetBoolean());
        Assert.Equal(JsonValueKind.Null, launch.RootElement.GetProperty("pane_workspace_migration_from_version").ValueKind);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsScreenInitEvidenceAfterLaunch()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-screen-init-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=18", "--width=96", "--height=24", "--deterministic", "--seed=99", "--evidence-jsonl", path],
            _ => null);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteLaunch(options);
            writer.WriteScreenInit(options, initMilliseconds: 7, memoryEstimateBytes: 4096);
            writer.WriteFrame("scripted_frame", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var launch = JsonDocument.Parse(lines[0]);
        using var screenInit = JsonDocument.Parse(lines[1]);
        using var frame = JsonDocument.Parse(lines[2]);

        Assert.Equal("launch", launch.RootElement.GetProperty("event").GetString());
        Assert.Equal("screen_init", screenInit.RootElement.GetProperty("event").GetString());
        Assert.Equal("scripted_frame", frame.RootElement.GetProperty("event").GetString());
        Assert.Equal(0, launch.RootElement.GetProperty("sequence").GetInt64());
        Assert.Equal(1, screenInit.RootElement.GetProperty("sequence").GetInt64());
        Assert.Equal(1, screenInit.RootElement.GetProperty("seq").GetInt64());
        Assert.Equal(2, frame.RootElement.GetProperty("sequence").GetInt64());
        Assert.Equal("test-jsonl-v1", screenInit.RootElement.GetProperty("upstream_schema_version").GetString());
        Assert.Equal("frankentui-net-showcase", screenInit.RootElement.GetProperty("run_id").GetString());
        Assert.Equal((ulong)99, screenInit.RootElement.GetProperty("seed").GetUInt64());
        Assert.Equal("alt", screenInit.RootElement.GetProperty("screen_mode").GetString());
        Assert.Equal(18, screenInit.RootElement.GetProperty("screen_number").GetInt32());
        Assert.Equal("visual_effects", screenInit.RootElement.GetProperty("screen_slug").GetString());
        Assert.Equal("VisualEffects", screenInit.RootElement.GetProperty("screen_id").GetString());
        Assert.Equal("visuals", screenInit.RootElement.GetProperty("screen_category").GetString());
        Assert.Equal("demo_screen_init", screenInit.RootElement.GetProperty("diagnostics_stream").GetString());
        Assert.Equal("generic_evidence", screenInit.RootElement.GetProperty("source").GetString());
        Assert.Equal((ulong)7, screenInit.RootElement.GetProperty("init_ms").GetUInt64());
        Assert.Equal(ShowcaseVfxEffects.AllCanonicalKeys.Length, screenInit.RootElement.GetProperty("effect_count").GetInt32());
        Assert.Equal("4096", screenInit.RootElement.GetProperty("memory_estimate_bytes").GetString());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsPaneWorkspaceSaveAcknowledgment()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-pane-save-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=6", "--pane-workspace=workspace.json", "--evidence-jsonl", path],
            _ => null);
        var workspace = PaneWorkspaceState.CreateDemo();
        var save = new ShowcasePaneWorkspaceSaveResult(
            "workspace.json",
            Saved: true,
            SnapshotHash: workspace.SnapshotHash());

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WritePaneWorkspaceSaveEvent(options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 3, save);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var saveEvent = JsonDocument.Parse(line);
        Assert.Equal("pane_workspace_save", saveEvent.RootElement.GetProperty("event").GetString());
        Assert.True(saveEvent.RootElement.GetProperty("pane_workspace_saved").GetBoolean());
        Assert.Equal("workspace.json", saveEvent.RootElement.GetProperty("pane_workspace").GetString());
        Assert.Equal(workspace.SnapshotHash(), saveEvent.RootElement.GetProperty("pane_workspace_snapshot_hash").GetString());
        Assert.Equal(ShowcasePaneWorkspacePersistence.CurrentSchemaVersion, saveEvent.RootElement.GetProperty("pane_workspace_schema_version").GetString());
        Assert.Equal(JsonValueKind.Null, saveEvent.RootElement.GetProperty("pane_workspace_save_error").ValueKind);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterIncludesInteractiveTourState()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-tour-evidence-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=1", "--tour", "--tour-speed=1.25", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 1,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight,
            tour: true,
            tourSpeed: 1.25)
            with
            {
                EvidenceLedgerVisible = true,
                PerfHudVisible = true,
                A11yPanelVisible = true,
                A11yHighContrast = true
            };

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteFrame("tick", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 3, state);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var frame = JsonDocument.Parse(line);
        Assert.Equal("tick", frame.RootElement.GetProperty("event").GetString());
        Assert.True(frame.RootElement.GetProperty("tour_active").GetBoolean());
        Assert.False(frame.RootElement.GetProperty("tour_paused").GetBoolean());
        Assert.Equal("1.25", frame.RootElement.GetProperty("tour_speed").GetString());
        Assert.Equal(2, frame.RootElement.GetProperty("tour_start_screen").GetInt32());
        Assert.Equal(0, frame.RootElement.GetProperty("tour_step_index").GetInt32());
        Assert.Equal(16, frame.RootElement.GetProperty("tour_step_count").GetInt32());
        Assert.Equal("dashboard:overview", frame.RootElement.GetProperty("tour_callout_id").GetString());
        Assert.Equal("Dashboard", frame.RootElement.GetProperty("tour_callout_title").GetString());
        Assert.Equal("0.03,0.12,0.94,0.72", frame.RootElement.GetProperty("tour_highlight").GetString());
        Assert.Equal("2,4,68,11", frame.RootElement.GetProperty("tour_highlight_rect").GetString());
        Assert.True(frame.RootElement.GetProperty("evidence_ledger_visible").GetBoolean());
        Assert.True(frame.RootElement.GetProperty("perf_hud_visible").GetBoolean());
        Assert.False(frame.RootElement.GetProperty("debug_visible").GetBoolean());
        Assert.False(frame.RootElement.GetProperty("help_visible").GetBoolean());
        Assert.True(frame.RootElement.GetProperty("a11y_panel_visible").GetBoolean());
        Assert.True(frame.RootElement.GetProperty("a11y_high_contrast").GetBoolean());
        Assert.False(frame.RootElement.GetProperty("a11y_reduced_motion").GetBoolean());
        Assert.False(frame.RootElement.GetProperty("a11y_large_text").GetBoolean());
        Assert.False(frame.RootElement.GetProperty("mouse_capture_enabled").GetBoolean());
        Assert.Equal("dashboard", frame.RootElement.GetProperty("state_screen_slug").GetString());
        Assert.Equal(state.Session.PaneWorkspace.SnapshotHash(), frame.RootElement.GetProperty("pane_workspace_snapshot_hash").GetString());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsTourEventWhenTourStateChanges()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-tour-event-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=1", "--tour", "--evidence-jsonl", path],
            _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 1,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight,
            tour: true);
        var after = ApplyKey(
            before,
            new KeyGesture(TerminalKey.Right, TerminalModifiers.None),
            DateTimeOffset.Parse("2026-05-01T00:00:00Z"));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteTourEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, before, after);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var tourEvent = JsonDocument.Parse(line);
        Assert.Equal("tour_event", tourEvent.RootElement.GetProperty("event").GetString());
        Assert.Equal("input", tourEvent.RootElement.GetProperty("tour_trigger").GetString());
        Assert.Equal("next", tourEvent.RootElement.GetProperty("tour_action").GetString());
        Assert.Equal(2, tourEvent.RootElement.GetProperty("tour_from_screen_number").GetInt32());
        Assert.Equal(2, tourEvent.RootElement.GetProperty("tour_to_screen_number").GetInt32());
        Assert.Equal(1, tourEvent.RootElement.GetProperty("tour_step_index").GetInt32());
        Assert.Equal("dashboard:palette", tourEvent.RootElement.GetProperty("tour_callout_id").GetString());
        Assert.True(tourEvent.RootElement.GetProperty("tour_active").GetBoolean());
        Assert.True(tourEvent.RootElement.GetProperty("tour_was_active").GetBoolean());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsPaletteEventWhenPaletteStateChanges()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-palette-event-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=2", "--evidence-jsonl", path],
            _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var opened = ApplyKey(
            before,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('k')),
            DateTimeOffset.Parse("2026-05-01T00:00:00Z"));
        var after = ApplyKey(
            opened,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('f')),
            DateTimeOffset.Parse("2026-05-01T00:00:00.010Z"));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WritePaletteEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, opened, after);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var paletteEvent = JsonDocument.Parse(line);
        Assert.Equal("palette_event", paletteEvent.RootElement.GetProperty("event").GetString());
        Assert.Equal("input", paletteEvent.RootElement.GetProperty("palette_trigger").GetString());
        Assert.Equal("favorite_add", paletteEvent.RootElement.GetProperty("palette_action").GetString());
        Assert.True(paletteEvent.RootElement.GetProperty("palette_open").GetBoolean());
        Assert.True(paletteEvent.RootElement.GetProperty("palette_was_open").GetBoolean());
        Assert.Equal(1, paletteEvent.RootElement.GetProperty("palette_favorite_count").GetInt32());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsPaletteRankingEvidence()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-palette-ranking-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=2", "--evidence-jsonl", path],
            _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var opened = ApplyKey(
            before,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('k')),
            DateTimeOffset.Parse("2026-05-01T00:00:00Z"));
        var after = opened;
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00.010Z");
        foreach (var character in "dash")
        {
            after = ApplyKey(
                after,
                new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune(character)),
                timestamp);
            timestamp += TimeSpan.FromMilliseconds(10);
        }

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WritePaletteEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, opened, after);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var paletteEvent = JsonDocument.Parse(line);
        Assert.Equal("query_change", paletteEvent.RootElement.GetProperty("palette_action").GetString());
        Assert.Equal("screen:02", paletteEvent.RootElement.GetProperty("palette_top_command_id").GetString());
        Assert.Equal("Substring", paletteEvent.RootElement.GetProperty("palette_top_match_kind").GetString());
        Assert.Contains("match_type", paletteEvent.RootElement.GetProperty("palette_top_evidence").GetString());
        Assert.Contains("title_length", paletteEvent.RootElement.GetProperty("palette_top_evidence").GetString());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsMouseEventForDashboardPaneLink()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-mouse-event-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=2", "--evidence-jsonl", path],
            _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var terminalEvent = TerminalEvent.Mouse(
            new MouseGesture(45, 7, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var after = before.ApplyInput(
            Envelope(terminalEvent, timestamp),
            RuntimeFrameStats.Empty);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, terminalEvent, before, after);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var mouseEvent = JsonDocument.Parse(line);
        Assert.Equal("mouse_event", mouseEvent.RootElement.GetProperty("event").GetString());
        Assert.Equal("input", mouseEvent.RootElement.GetProperty("mouse_trigger").GetString());
        Assert.Equal("down_left", mouseEvent.RootElement.GetProperty("mouse_kind").GetString());
        Assert.Equal("pane_link_switch_screen", mouseEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal(45, mouseEvent.RootElement.GetProperty("mouse_column").GetInt32());
        Assert.Equal(7, mouseEvent.RootElement.GetProperty("mouse_row").GetInt32());
        Assert.Equal("down_left", mouseEvent.RootElement.GetProperty("kind").GetString());
        Assert.Equal(45, mouseEvent.RootElement.GetProperty("x").GetInt32());
        Assert.Equal(7, mouseEvent.RootElement.GetProperty("y").GetInt32());
        Assert.Equal("pane:16", mouseEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal("pane_link_switch_screen", mouseEvent.RootElement.GetProperty("action").GetString());
        Assert.Equal("Dashboard", mouseEvent.RootElement.GetProperty("current_screen").GetString());
        Assert.Equal("Mermaid Showcase", mouseEvent.RootElement.GetProperty("target_screen").GetString());
        Assert.Equal(2, mouseEvent.RootElement.GetProperty("mouse_current_screen_number").GetInt32());
        Assert.Equal(16, mouseEvent.RootElement.GetProperty("mouse_target_screen_number").GetInt32());
        Assert.Equal("mermaid_showcase", mouseEvent.RootElement.GetProperty("mouse_target_screen_slug").GetString());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsMouseEventForPalettePriority()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-palette-mouse-event-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=2", "--evidence-jsonl", path],
            _ => null);
        var closed = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var opened = ApplyKey(
            closed,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('k')),
            DateTimeOffset.Parse("2026-05-01T00:00:00Z"));
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00.010Z");
        var terminalEvent = TerminalEvent.Mouse(
            new MouseGesture(22, 1, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var after = opened.ApplyInput(
            Envelope(terminalEvent, timestamp),
            RuntimeFrameStats.Empty);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, terminalEvent, opened, after);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var mouseEvent = JsonDocument.Parse(line);
        Assert.Equal("mouse_event", mouseEvent.RootElement.GetProperty("event").GetString());
        Assert.Equal("palette_mouse", mouseEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("palette_mouse", mouseEvent.RootElement.GetProperty("action").GetString());
        Assert.Equal("palette", mouseEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal("Dashboard", mouseEvent.RootElement.GetProperty("current_screen").GetString());
        Assert.Equal("none", mouseEvent.RootElement.GetProperty("target_screen").GetString());
        Assert.Equal(2, mouseEvent.RootElement.GetProperty("mouse_current_screen_number").GetInt32());
        Assert.True(mouseEvent.RootElement.GetProperty("mouse_target_screen_number").ValueKind is JsonValueKind.Null);
        Assert.True(mouseEvent.RootElement.GetProperty("palette_open").GetBoolean());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsMouseCaptureToggleEvent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-mouse-capture-toggle-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=2", "--evidence-jsonl", path],
            _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var terminalEvent = TerminalEvent.Key(new KeyGesture(TerminalKey.F6, TerminalModifiers.None), timestamp);
        var after = before.ApplyInput(
            Envelope(terminalEvent, timestamp),
            RuntimeFrameStats.Empty);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseCaptureToggleEvent(
                "input",
                options,
                RuntimeFrameStats.Empty,
                stepIndex: 1,
                frame: 1,
                terminalEvent,
                before,
                after);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var toggleEvent = JsonDocument.Parse(line);
        Assert.Equal("mouse_capture_toggle", toggleEvent.RootElement.GetProperty("event").GetString());
        Assert.Equal("on", toggleEvent.RootElement.GetProperty("state").GetString());
        Assert.Equal("alt", toggleEvent.RootElement.GetProperty("mode").GetString());
        Assert.Equal("user", toggleEvent.RootElement.GetProperty("source").GetString());
        Assert.Equal("Dashboard", toggleEvent.RootElement.GetProperty("current_screen").GetString());
        Assert.Equal("off", toggleEvent.RootElement.GetProperty("mouse_capture_previous_state").GetString());
        Assert.True(toggleEvent.RootElement.GetProperty("mouse_capture_enabled").GetBoolean());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsA11yToggleEvents()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-a11y-toggle-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=37", "--evidence-jsonl", path],
            _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 37,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight)
            with
            {
                A11yPanelVisible = true,
                A11yHighContrast = false,
                A11yReducedMotion = false,
                A11yLargeText = false
            };
        var after = before with
        {
            A11yHighContrast = true,
            A11yReducedMotion = true,
            A11yLargeText = true
        };

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteA11yEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 4, frame: 5, before, after);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var highContrast = JsonDocument.Parse(lines[0]);
        using var reducedMotion = JsonDocument.Parse(lines[1]);
        using var largeText = JsonDocument.Parse(lines[2]);

        Assert.Equal("a11y_event", highContrast.RootElement.GetProperty("event").GetString());
        Assert.Equal("high_contrast_toggle", highContrast.RootElement.GetProperty("a11y_event").GetString());
        Assert.Equal("reduced_motion_toggle", reducedMotion.RootElement.GetProperty("a11y_event").GetString());
        Assert.Equal("large_text_toggle", largeText.RootElement.GetProperty("a11y_event").GetString());
        Assert.Equal("a11y", highContrast.RootElement.GetProperty("diagnostics_stream").GetString());
        Assert.Equal("input", highContrast.RootElement.GetProperty("a11y_trigger").GetString());
        Assert.Equal(5, highContrast.RootElement.GetProperty("tick").GetInt32());
        Assert.Equal("Accessibility", highContrast.RootElement.GetProperty("screen").GetString());
        Assert.Equal("true", highContrast.RootElement.GetProperty("panel_visible").GetString());
        Assert.Equal("true", highContrast.RootElement.GetProperty("high_contrast").GetString());
        Assert.Equal("false", highContrast.RootElement.GetProperty("high_contrast_previous").GetString());
        Assert.Equal("true", reducedMotion.RootElement.GetProperty("reduced_motion").GetString());
        Assert.Equal("false", reducedMotion.RootElement.GetProperty("reduced_motion_previous").GetString());
        Assert.Equal("true", largeText.RootElement.GetProperty("large_text").GetString());
        Assert.Equal("false", largeText.RootElement.GetProperty("large_text_previous").GetString());
        Assert.Equal("test-jsonl-v1", highContrast.RootElement.GetProperty("upstream_schema_version").GetString());
        Assert.Equal(0, highContrast.RootElement.GetProperty("sequence").GetInt64());
        Assert.Equal(1, reducedMotion.RootElement.GetProperty("sequence").GetInt64());
        Assert.Equal(2, largeText.RootElement.GetProperty("sequence").GetInt64());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsPerfHudToggleEvent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-perf-toggle-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=32", "--evidence-jsonl", path],
            _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 32,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var after = before with { PerfHudVisible = true };

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WritePerfHudEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 4, before, after);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var perfEvent = JsonDocument.Parse(line);
        Assert.Equal("perf_hud_event", perfEvent.RootElement.GetProperty("event").GetString());
        Assert.Equal("perf_hud", perfEvent.RootElement.GetProperty("diagnostics_stream").GetString());
        Assert.Equal("hud_toggle", perfEvent.RootElement.GetProperty("perf_hud_event").GetString());
        Assert.Equal("input", perfEvent.RootElement.GetProperty("perf_hud_trigger").GetString());
        Assert.Equal("on", perfEvent.RootElement.GetProperty("state").GetString());
        Assert.Equal("off", perfEvent.RootElement.GetProperty("previous_state").GetString());
        Assert.Equal(4, perfEvent.RootElement.GetProperty("tick").GetInt32());
        Assert.Equal("Performance Challenge", perfEvent.RootElement.GetProperty("screen").GetString());
        Assert.True(perfEvent.RootElement.GetProperty("perf_hud_visible").GetBoolean());
        Assert.Equal("test-jsonl-v1", perfEvent.RootElement.GetProperty("upstream_schema_version").GetString());
        Assert.Equal(0, perfEvent.RootElement.GetProperty("sequence").GetInt64());
        Assert.Equal(0, perfEvent.RootElement.GetProperty("seq").GetInt64());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsPerfHudTickStatsEverySixtyTicks()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-perf-stats-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=32", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 32,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight)
            with
            {
                PerfHudVisible = true
            };
        var stats = RuntimeFrameStats.Empty with
        {
            FrameDurationMs = 20,
            PresentDurationMs = 3,
            DiffDurationMs = 1.5,
            ChangedCells = 42,
            DirtyRows = 4,
            LoadGovernorFramesObserved = 12
        };

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WritePerfHudStatsEvent(options, stats, stepIndex: 59, frame: 59, state);
            writer.WritePerfHudStatsEvent(options, stats, stepIndex: 60, frame: 60, state);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var perfEvent = JsonDocument.Parse(line);
        Assert.Equal("perf_hud_event", perfEvent.RootElement.GetProperty("event").GetString());
        Assert.Equal("perf_hud", perfEvent.RootElement.GetProperty("diagnostics_stream").GetString());
        Assert.Equal("tick_stats", perfEvent.RootElement.GetProperty("perf_hud_event").GetString());
        Assert.Equal(60, perfEvent.RootElement.GetProperty("tick").GetInt32());
        Assert.Equal("Performance Challenge", perfEvent.RootElement.GetProperty("screen").GetString());
        Assert.Equal("50", perfEvent.RootElement.GetProperty("fps").GetString());
        Assert.Equal("50", perfEvent.RootElement.GetProperty("tps").GetString());
        Assert.Equal("20", perfEvent.RootElement.GetProperty("avg_ms").GetString());
        Assert.Equal("20", perfEvent.RootElement.GetProperty("p95_ms").GetString());
        Assert.Equal("20", perfEvent.RootElement.GetProperty("p99_ms").GetString());
        Assert.Equal("20", perfEvent.RootElement.GetProperty("min_ms").GetString());
        Assert.Equal("20", perfEvent.RootElement.GetProperty("max_ms").GetString());
        Assert.Equal(12, perfEvent.RootElement.GetProperty("samples").GetInt32());
        Assert.Equal("3", perfEvent.RootElement.GetProperty("present_ms").GetString());
        Assert.Equal("1.5", perfEvent.RootElement.GetProperty("diff_ms").GetString());
        Assert.Equal(42, perfEvent.RootElement.GetProperty("changed_cells").GetInt32());
        Assert.Equal(4, perfEvent.RootElement.GetProperty("dirty_rows").GetInt32());
        Assert.True(perfEvent.RootElement.GetProperty("perf_hud_visible").GetBoolean());
        Assert.Equal("test-jsonl-v1", perfEvent.RootElement.GetProperty("upstream_schema_version").GetString());
        Assert.Equal(0, perfEvent.RootElement.GetProperty("sequence").GetInt64());
        Assert.Equal(0, perfEvent.RootElement.GetProperty("seq").GetInt64());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsPerfHudTickStallEvent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-perf-stall-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=32", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 32,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight)
            with
            {
                PerfHudVisible = true,
                A11yReducedMotion = true
            };

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WritePerfHudStallEvent(options, RuntimeFrameStats.Empty, stepIndex: 7, frame: 8, state, sinceMilliseconds: 1250);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var perfEvent = JsonDocument.Parse(line);
        Assert.Equal("perf_hud_event", perfEvent.RootElement.GetProperty("event").GetString());
        Assert.Equal("perf_hud", perfEvent.RootElement.GetProperty("diagnostics_stream").GetString());
        Assert.Equal("tick_stall", perfEvent.RootElement.GetProperty("perf_hud_event").GetString());
        Assert.Equal(1250, perfEvent.RootElement.GetProperty("since_ms").GetInt64());
        Assert.Equal(8, perfEvent.RootElement.GetProperty("tick").GetInt32());
        Assert.Equal("Performance Challenge", perfEvent.RootElement.GetProperty("screen").GetString());
        Assert.Equal("true", perfEvent.RootElement.GetProperty("reduced_motion").GetString());
        Assert.True(perfEvent.RootElement.GetProperty("perf_hud_visible").GetBoolean());
        Assert.True(perfEvent.RootElement.GetProperty("a11y_reduced_motion").GetBoolean());
        Assert.Equal("test-jsonl-v1", perfEvent.RootElement.GetProperty("upstream_schema_version").GetString());
        Assert.Equal(0, perfEvent.RootElement.GetProperty("sequence").GetInt64());
        Assert.Equal(0, perfEvent.RootElement.GetProperty("seq").GetInt64());
    }

    [Fact]
    public void ShowcaseGuidedTourLandingKeyboardControlsAdjustStartAndSpeed()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 1,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyKey(state, new KeyGesture(TerminalKey.Down, TerminalModifiers.None), start);
        Assert.Equal(3, state.TourStartScreen);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('p')), start);
        Assert.Equal(2, state.TourStartScreen);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('n')), start);
        Assert.Equal(3, state.TourStartScreen);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('+')), start);
        Assert.Equal(1.25, state.TourSpeed);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('-')), start);
        Assert.Equal(1.0, state.TourSpeed);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('r')), start);
        Assert.Equal(2, state.TourStartScreen);
        Assert.Equal(1.0, state.TourSpeed);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Enter, TerminalModifiers.None), start);
        Assert.True(state.TourActive);
        Assert.Equal(2, state.CurrentScreenNumber);
    }

    [Fact]
    public void ShowcaseGuidedTourLandingMouseControlsAdjustStartAndStartTour()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 1,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(
            state,
            column: 20,
            row: 6,
            start,
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(3, state.TourStartScreen);

        state = ApplyMouse(
            state,
            column: 20,
            row: 6,
            start + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelUp,
            TerminalMouseKind.Scroll);
        Assert.Equal(2, state.TourStartScreen);

        state = ApplyMouse(state, column: 20, row: 6, start + TimeSpan.FromMilliseconds(20));

        Assert.True(state.TourActive);
        Assert.Equal(2, state.CurrentScreenNumber);
        Assert.Equal(0, state.TourStepIndex);
    }

    [Fact]
    public void ShowcaseGuidedTourActiveKeyboardControlsStepSpeedPauseAndExit()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 1,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight,
            tour: true);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        Assert.True(state.TourActive);
        Assert.Equal(2, state.CurrentScreenNumber);
        Assert.Equal(0, state.TourStepIndex);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Right, TerminalModifiers.None), start);
        Assert.Equal(2, state.CurrentScreenNumber);
        Assert.Equal(1, state.TourStepIndex);
        Assert.Equal("dashboard:palette", state.TourCallout?.StepId);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Left, TerminalModifiers.None), start);
        Assert.Equal(2, state.CurrentScreenNumber);
        Assert.Equal(0, state.TourStepIndex);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('+')), start);
        Assert.Equal(1.25, state.TourSpeed);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune(' ')), start);
        Assert.True(state.TourPaused);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune(' ')), start);
        Assert.False(state.TourPaused);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start);
        Assert.False(state.TourActive);
        Assert.Equal(1, state.CurrentScreenNumber);
    }

    [Fact]
    public void ShowcaseGuidedTourActiveMouseClickExitsToLanding()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 1,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight,
            tour: true);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyKey(state, new KeyGesture(TerminalKey.Right, TerminalModifiers.None), start);
        Assert.True(state.TourActive);
        Assert.Equal(1, state.TourStepIndex);

        state = ApplyMouse(state, column: 20, row: 6, start + TimeSpan.FromMilliseconds(10));

        Assert.False(state.TourActive);
        Assert.False(state.TourPaused);
        Assert.Equal(1, state.CurrentScreenNumber);
    }

    [Fact]
    public void ShowcaseGuidedTourMouseDoesNotHijackStatusRow()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 1,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, column: 2, row: 17, start);

        Assert.False(state.TourActive);
        Assert.Equal(1, state.CurrentScreenNumber);
        Assert.True(state.HelpVisible);
    }

    [Fact]
    public void ShowcaseChromeMouseCategoryTabSelectsFirstScreenInCategory()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, column: 15, row: 0, start);

        Assert.Equal(8, state.CurrentScreenNumber);
        Assert.Equal(ShowcaseScreenCategory.Visuals, state.CurrentScreen.Category);
    }

    [Fact]
    public void ShowcaseChromeMouseScreenTabSelectsVisibleScreen()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, column: 22, row: 1, start);

        Assert.Equal(5, state.CurrentScreenNumber);
    }

    [Fact]
    public void ShowcaseChromeMouseTabWheelCyclesScreensAndStopsTour()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 1,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight,
            tour: true);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        Assert.True(state.TourActive);

        state = ApplyMouse(
            state,
            column: 5,
            row: 1,
            start,
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);

        Assert.False(state.TourActive);
        Assert.False(state.TourPaused);
        Assert.Equal(3, state.CurrentScreenNumber);

        state = ApplyMouse(
            state,
            column: 5,
            row: 1,
            start + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelUp,
            TerminalMouseKind.Scroll);

        Assert.Equal(2, state.CurrentScreenNumber);
    }

    [Fact]
    public void ShowcaseChromeMouseDoesNotRouteWhileCommandPaletteIsOpen()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('k')),
            start);
        Assert.True(state.Session.CommandPalette.IsOpen);

        state = ApplyMouse(state, column: 22, row: 1, start + TimeSpan.FromMilliseconds(10));

        Assert.True(state.Session.CommandPalette.IsOpen);
        Assert.Equal(2, state.CurrentScreenNumber);
    }

    [Fact]
    public void ShowcaseDashboardPaneMouseRoutesHighlightLinks()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, column: 45, row: 7, start);

        Assert.Equal(16, state.CurrentScreenNumber);

        state = ApplyMouse(
            state,
            column: 45,
            row: 7,
            start + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.Left,
            TerminalMouseKind.Up);

        Assert.Equal(16, state.CurrentScreenNumber);
    }

    [Fact]
    public void ShowcaseDashboardPaneMouseIgnoresNonLinkAndNonLeftClick()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, column: 12, row: 7, start);
        Assert.Equal(2, state.CurrentScreenNumber);

        state = ApplyMouse(
            state,
            column: 45,
            row: 7,
            start + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.Right,
            TerminalMouseKind.Down);

        Assert.Equal(2, state.CurrentScreenNumber);
    }

    [Fact]
    public void ShowcaseGuidedTourCarriesStoryboardCalloutForActiveScreen()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 1,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight,
            tour: true);

        Assert.NotNull(state.TourCallout);
        Assert.Equal("dashboard:overview", state.TourCallout.StepId);
        Assert.Equal("Dashboard", state.TourCallout.Title);

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('n')),
            DateTimeOffset.Parse("2026-05-01T00:00:00Z"));
        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('n')),
            DateTimeOffset.Parse("2026-05-01T00:00:00.010Z"));

        Assert.NotNull(state.TourCallout);
        Assert.Equal("mermaid:mermaid", state.TourCallout.StepId);
        Assert.Equal(2, state.TourStepIndex);
        Assert.Equal(16, state.CurrentScreenNumber);
        Assert.Equal("0.40,0.18,0.58,0.72", state.TourCallout.Highlight);
    }

    [Fact]
    public void ShowcaseTourHighlightResolvesAgainstLocalContentArea()
    {
        var callout = ShowcaseTourStoryboard.At(2);
        var rect = ShowcaseTourStoryboard.ResolveHighlight(callout, new FrankenTui.Core.Size(72, 18));

        Assert.Equal(new FrankenTui.Core.Rect(29, 5, 42, 11), rect);
        Assert.Equal("29,5,42,11", ShowcaseTourStoryboard.FormatRect(rect));
    }

    [Fact]
    public void ShowcaseShiftHAndShiftLNavigateScreensAndStopTour()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 3,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight,
            tour: true);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = state with { CurrentScreenNumber = 3 };
        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Shift, new Rune('H')),
            start);

        Assert.False(state.TourActive);
        Assert.False(state.TourPaused);
        Assert.Equal(2, state.CurrentScreenNumber);

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Shift, new Rune('L')),
            start + TimeSpan.FromMilliseconds(10));

        Assert.Equal(3, state.CurrentScreenNumber);
    }

    [Fact]
    public void ShowcaseHelpTextListsImplementedControlPlaneShortcuts()
    {
        var help = ShowcaseSurface.BuildHelpText();

        Assert.Contains("Shift+H/L", help);
        Assert.Contains("F6 / m", help);
        Assert.Contains("Ctrl+K", help);
        Assert.Contains("Ctrl+F", help);
        Assert.Contains("Ctrl+Shift+F", help);
        Assert.Contains("Ctrl+0..N", help);
        Assert.Contains("Ctrl+I", help);
        Assert.Contains("Ctrl+P", help);
        Assert.Contains("F12", help);
        Assert.Contains("Shift+A", help);
    }

    [Fact]
    public void ShowcaseEscapeDismissesPaletteBeforeHelpOverlay()
    {
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight)
            with
            {
                HelpVisible = true,
                Session = ShowcaseDemoState.Create(
                    inlineMode: false,
                    viewport: new FrankenTui.Core.Size(72, 18),
                    screenNumber: 2,
                    language: "en",
                    flowDirection: WidgetFlowDirection.LeftToRight)
                    .Session with
                    {
                        CommandPalette = CommandPaletteController.Toggle(CommandPaletteState.Closed)
                    }
            };

        state = ApplyKey(state, new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start);

        Assert.False(state.Session.CommandPalette.IsOpen);
        Assert.True(state.HelpVisible);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(10));

        Assert.False(state.HelpVisible);
    }

    [Fact]
    public void ShowcaseF12TogglesDebugOverlayAndEscapeDismissesItBeforeHelp()
    {
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        state = ApplyKey(state, new KeyGesture(TerminalKey.F12, TerminalModifiers.None), start);
        Assert.True(state.DebugVisible);

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('?')),
            start + TimeSpan.FromMilliseconds(10));

        Assert.True(state.DebugVisible);
        Assert.True(state.HelpVisible);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(20));

        Assert.False(state.DebugVisible);
        Assert.True(state.HelpVisible);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(30));

        Assert.False(state.HelpVisible);
    }

    [Fact]
    public void ShowcasePerfHudAndA11yOverlayShortcutsFollowUpstreamDismissalOrder()
    {
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Shift, new Rune('A')),
            start);

        Assert.True(state.A11yPanelVisible);

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Shift, new Rune('H')),
            start + TimeSpan.FromMilliseconds(10));
        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Shift, new Rune('M')),
            start + TimeSpan.FromMilliseconds(20));
        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Shift, new Rune('L')),
            start + TimeSpan.FromMilliseconds(30));

        Assert.True(state.A11yHighContrast);
        Assert.True(state.A11yReducedMotion);
        Assert.True(state.A11yLargeText);

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('p')),
            start + TimeSpan.FromMilliseconds(40));

        Assert.True(state.PerfHudVisible);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(50));

        Assert.False(state.PerfHudVisible);
        Assert.True(state.A11yPanelVisible);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(60));

        Assert.False(state.A11yPanelVisible);
    }

    [Fact]
    public void ShowcaseCtrlITogglesEvidenceLedgerAbovePerfHud()
    {
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('p')),
            start);
        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('i')),
            start + TimeSpan.FromMilliseconds(10));

        Assert.True(state.PerfHudVisible);
        Assert.True(state.EvidenceLedgerVisible);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(20));

        Assert.False(state.EvidenceLedgerVisible);
        Assert.True(state.PerfHudVisible);

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('i')),
            start + TimeSpan.FromMilliseconds(30));

        Assert.True(state.EvidenceLedgerVisible);

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('i')),
            start + TimeSpan.FromMilliseconds(40));

        Assert.False(state.EvidenceLedgerVisible);
    }

    [Fact]
    public void ShowcaseMouseCaptureTogglesFromKeyboardAndStatusRow()
    {
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        state = ApplyKey(state, new KeyGesture(TerminalKey.F6, TerminalModifiers.None), start);
        Assert.True(state.MouseCaptureEnabled);

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('m')),
            start + TimeSpan.FromMilliseconds(10));
        Assert.False(state.MouseCaptureEnabled);

        state = ApplyMouse(state, column: 49, row: 17, start + TimeSpan.FromMilliseconds(20));
        Assert.True(state.MouseCaptureEnabled);
    }

    [Fact]
    public void ShowcaseStatusRowMouseTogglesChromeOverlaysWhenUncovered()
    {
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        state = ApplyMouse(state, column: 2, row: 17, start);
        Assert.True(state.HelpVisible);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(10));
        state = ApplyMouse(state, column: 9, row: 17, start + TimeSpan.FromMilliseconds(20));
        Assert.True(state.Session.CommandPalette.IsOpen);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(30));
        state = ApplyMouse(state, column: 18, row: 17, start + TimeSpan.FromMilliseconds(40));
        Assert.True(state.A11yPanelVisible);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(50));
        state = ApplyMouse(state, column: 25, row: 17, start + TimeSpan.FromMilliseconds(60));
        Assert.True(state.PerfHudVisible);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(70));
        state = ApplyMouse(state, column: 33, row: 17, start + TimeSpan.FromMilliseconds(80));
        Assert.True(state.DebugVisible);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(90));
        state = ApplyMouse(state, column: 42, row: 17, start + TimeSpan.FromMilliseconds(100));
        Assert.True(state.EvidenceLedgerVisible);
    }

    [Fact]
    public void ShowcaseCtrlKOpensAdvertisedCommandPalette()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('k')),
            start);

        Assert.True(state.Session.CommandPalette.IsOpen);
        Assert.Contains("Command palette opened", state.Session.InputState.LiveRegionText);
    }

    [Fact]
    public void ShowcaseCommandPaletteFavoriteShortcutsToggleFavoriteAndFilter()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('k')),
            start);
        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('f')),
            start + TimeSpan.FromMilliseconds(10));

        Assert.True(state.Session.CommandPalette.IsOpen);
        Assert.Contains("screen:01", state.Session.CommandPalette.FavoriteEntryIds ?? []);
        Assert.Contains("Favorited 01 Guided Tour", state.Session.CommandPalette.Status);

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control | TerminalModifiers.Shift, new Rune('F')),
            start + TimeSpan.FromMilliseconds(20));

        Assert.True(state.Session.CommandPalette.FavoritesOnly);
        var results = CommandPaletteController.Results(state.Session.CommandPalette, ShowcaseCommandPalette.Entries());
        var result = Assert.Single(results);
        Assert.Equal("screen:01", result.Entry.Id);
        Assert.Equal(1, result.Entry.ScreenNumber);
        Assert.Equal("guided_tour", result.Entry.ScreenSlug);
        Assert.Equal("Tour", result.Entry.ScreenCategory);
    }

    [Fact]
    public void ShowcaseCommandPaletteCategoryShortcutsFilterAndClearResults()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('k')),
            start);
        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('3')),
            start + TimeSpan.FromMilliseconds(10));

        Assert.Equal(CommandPaletteCategory.Actions, state.Session.CommandPalette.CategoryFilter);
        var filtered = CommandPaletteController.Results(state.Session.CommandPalette, ShowcaseCommandPalette.Entries());
        Assert.NotEmpty(filtered);
        Assert.All(filtered, result => Assert.Equal(CommandPaletteCategory.Actions, result.Entry.Category));
        Assert.All(filtered, result => Assert.NotNull(result.Entry.ScreenNumber));

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('0')),
            start + TimeSpan.FromMilliseconds(20));

        Assert.Null(state.Session.CommandPalette.CategoryFilter);
    }

    [Fact]
    public void ShowcaseCommandPaletteExecutesScreenIdCommand()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight,
            tour: true);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('k')),
            start);
        foreach (var character in "determinism")
        {
            state = ApplyKey(
                state,
                new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune(character)),
                start + TimeSpan.FromMilliseconds(character));
        }

        state = ApplyKey(
            state,
            new KeyGesture(TerminalKey.Enter, TerminalModifiers.None),
            start + TimeSpan.FromMilliseconds(100));

        Assert.False(state.Session.CommandPalette.IsOpen);
        Assert.False(state.TourActive);
        Assert.Equal(40, state.CurrentScreenNumber);
        Assert.Equal("screen:40", state.Session.CommandPalette.LastExecutedCommandId);
        Assert.Contains("Screen 40: Determinism Lab opened", state.Session.InputState.LiveRegionText);
    }

    [Fact]
    public void ShowcaseCommandPaletteSupportsScreenWordStartPrefixSearch()
    {
        var results = CommandPaletteController.Results(
            CommandPaletteState.Closed with { Query = "dl" },
            ShowcaseCommandPalette.Entries());

        Assert.NotEmpty(results);
        Assert.Equal("screen:40", results[0].Entry.Id);
        Assert.Equal(CommandPaletteMatchKind.WordStart, results[0].MatchKind);
        Assert.Equal([3, 15], results[0].MatchPositions);
    }

    [Fact]
    public void ShowcaseCliOptionsParseVfxHarnessEnvironmentAndCliOverrides()
    {
        var environment = new Dictionary<string, string?>
        {
            ["FTUI_DEMO_VFX_HARNESS"] = "true",
            ["FTUI_DEMO_VFX_EFFECT"] = "plasma",
            ["FTUI_DEMO_VFX_TICK_MS"] = "33",
            ["FTUI_DEMO_VFX_FRAMES"] = "9",
            ["FTUI_DEMO_VFX_SIZE"] = "110x33",
            ["FTUI_DEMO_VFX_SEED"] = "41",
            ["FTUI_DEMO_VFX_JSONL"] = "env-vfx.jsonl",
            ["FTUI_DEMO_VFX_RUN_ID"] = "env-run",
            ["FTUI_DEMO_VFX_PERF"] = "1",
            ["FTUI_DEMO_VFX_EXIT_AFTER_MS"] = "250",
            ["FTUI_DEMO_VFX_GOLDEN"] = "env-vfx-golden.json",
            ["FTUI_DEMO_VFX_UPDATE_GOLDEN"] = "true"
        };

        var options = ShowcaseCliOptions.Parse(
            ["--vfx-effect=doom", "--vfx-cols=120", "--vfx-rows=40", "--vfx-seed=42", "--vfx-jsonl=cli-vfx.jsonl", "--vfx-golden=cli-vfx-golden.json"],
            key => environment.GetValueOrDefault(key));

        Assert.True(options.VfxHarness.Enabled);
        Assert.True(options.VfxHarness.Perf);
        Assert.Equal("doom-e1m1", options.VfxHarness.Effect);
        Assert.Equal((uint)33, options.VfxHarness.TickMilliseconds);
        Assert.Equal((uint)9, options.VfxHarness.Frames);
        Assert.Equal((ushort)120, options.VfxHarness.Columns);
        Assert.Equal((ushort)40, options.VfxHarness.Rows);
        Assert.Equal((ulong)42, options.VfxHarness.Seed);
        Assert.Equal("cli-vfx.jsonl", options.VfxHarness.JsonlPath);
        Assert.Equal("env-run", options.VfxHarness.RunId);
        Assert.Equal((uint)250, options.VfxHarness.ExitAfterMilliseconds);
        Assert.Equal("cli-vfx-golden.json", options.VfxHarness.GoldenPath);
        Assert.True(options.VfxHarness.UpdateGolden);
        Assert.Equal(18, options.ScreenNumber);
        Assert.Equal((ushort)120, options.Width);
        Assert.Equal((ushort)40, options.Height);
        Assert.Equal(9, options.Frames);
        Assert.False(options.InteractiveMode);
        Assert.Equal((uint)33, options.TickIntervalMilliseconds);
        Assert.Equal((uint)250, options.ExitAfterMilliseconds);
    }

    [Fact]
    public void ShowcaseCliOptionsUsesUpstreamVfxJsonlDefaultWhenHarnessEnabled()
    {
        var options = ShowcaseCliOptions.Parse(["--vfx-harness"], _ => null);

        Assert.True(options.VfxHarness.Enabled);
        Assert.Equal("vfx_harness.jsonl", options.VfxHarness.JsonlPath);
    }

    [Fact]
    public void ShowcaseCliOptionsParseMermaidHarnessEnvironmentAndCliOverrides()
    {
        var environment = new Dictionary<string, string?>
        {
            ["FTUI_DEMO_MERMAID_HARNESS"] = "1",
            ["FTUI_DEMO_MERMAID_TICK_MS"] = "125",
            ["FTUI_DEMO_MERMAID_COLS"] = "90",
            ["FTUI_DEMO_MERMAID_ROWS"] = "28",
            ["FTUI_DEMO_MERMAID_SEED"] = "700",
            ["FTUI_DEMO_MERMAID_JSONL"] = "env-mermaid.jsonl",
            ["FTUI_DEMO_MERMAID_RUN_ID"] = "env-mermaid-run"
        };

        var options = ShowcaseCliOptions.Parse(
            ["--mermaid-tick-ms=100", "--mermaid-jsonl=cli-mermaid.jsonl", "--mermaid-run-id=cli-mermaid-run"],
            key => environment.GetValueOrDefault(key));

        Assert.True(options.MermaidHarness.Enabled);
        Assert.Equal((uint)100, options.MermaidHarness.TickMilliseconds);
        Assert.Equal((ushort)90, options.MermaidHarness.Columns);
        Assert.Equal((ushort)28, options.MermaidHarness.Rows);
        Assert.Equal((ulong)700, options.MermaidHarness.Seed);
        Assert.Equal("cli-mermaid.jsonl", options.MermaidHarness.JsonlPath);
        Assert.Equal("cli-mermaid-run", options.MermaidHarness.RunId);
        Assert.Equal(16, options.ScreenNumber);
        Assert.Equal((ushort)90, options.Width);
        Assert.Equal((ushort)28, options.Height);
        Assert.Equal(ShowcaseMouseMode.Off, options.MouseMode);
        Assert.False(options.UseMouseTracking);
        Assert.Equal((uint)100, options.TickIntervalMilliseconds);
    }

    [Fact]
    public void ShowcaseHarnessJsonlWriterEmitsDeterministicVfxRecords()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-vfx-harness-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--vfx-harness", "--vfx-jsonl", path, "--vfx-effect=doom", "--vfx-seed=42", "--vfx-frames=2"],
            _ => null);

        using (var writer = ShowcaseHarnessJsonlWriter.CreateVfx(options))
        {
            Assert.NotNull(writer);
            writer.WriteLaunch(options);
            writer.WriteFrame(options, 0, RuntimeFrameStats.Empty);
            writer.WriteFrame(options, 1, RuntimeFrameStats.Empty);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var launch = JsonDocument.Parse(lines[0]);
        using var frame0 = JsonDocument.Parse(lines[1]);
        using var frame1 = JsonDocument.Parse(lines[2]);
        Assert.Equal("vfx_harness_start", launch.RootElement.GetProperty("event").GetString());
        Assert.Equal("doom-e1m1", launch.RootElement.GetProperty("effect").GetString());
        Assert.Equal(options.Width, launch.RootElement.GetProperty("cols").GetUInt16());
        Assert.Equal(options.Height, launch.RootElement.GetProperty("rows").GetUInt16());
        Assert.False(launch.RootElement.GetProperty("perf").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(launch.RootElement.GetProperty("hash_key").GetString()));
        Assert.Equal("vfx_frame", frame0.RootElement.GetProperty("event").GetString());
        Assert.Equal("vfx", frame0.RootElement.GetProperty("harness").GetString());
        Assert.Equal("visual_effects", frame0.RootElement.GetProperty("screen_slug").GetString());
        Assert.Equal("doom-e1m1", frame0.RootElement.GetProperty("effect").GetString());
        Assert.False(string.IsNullOrWhiteSpace(frame0.RootElement.GetProperty("hash_key").GetString()));
        Assert.Equal(0, frame0.RootElement.GetProperty("frame_idx").GetInt32());
        Assert.Equal(options.Width, frame0.RootElement.GetProperty("cols").GetUInt16());
        Assert.Equal(options.Height, frame0.RootElement.GetProperty("rows").GetUInt16());
        Assert.Equal((ulong)42, frame0.RootElement.GetProperty("seed").GetUInt64());
        Assert.True(frame0.RootElement.GetProperty("time").GetDouble() >= 0.0);
        Assert.NotEqual((ulong)0, frame0.RootElement.GetProperty("hash").GetUInt64());
        Assert.Equal("harness_inputs", frame0.RootElement.GetProperty("checksum_source").GetString());
        Assert.Equal(JsonValueKind.Null, frame0.RootElement.GetProperty("render_checksum").ValueKind);
        Assert.Equal(
            frame0.RootElement.GetProperty("checksum").GetString(),
            frame0.RootElement.GetProperty("input_checksum").GetString());
        Assert.NotEqual(
            frame0.RootElement.GetProperty("checksum").GetString(),
            frame1.RootElement.GetProperty("checksum").GetString());
        Assert.NotEqual(
            frame0.RootElement.GetProperty("hash").GetUInt64(),
            frame1.RootElement.GetProperty("hash").GetUInt64());
    }

    [Fact]
    public void ShowcaseHarnessJsonlWriterEmitsScriptedFpsVfxInputRecords()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-vfx-input-harness-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--vfx-harness", "--vfx-jsonl", path, "--vfx-effect=quake", "--vfx-seed=42", "--vfx-frames=12"],
            _ => null);

        using (var writer = ShowcaseHarnessJsonlWriter.CreateVfx(options))
        {
            Assert.NotNull(writer);
            writer.WriteLaunch(options);
            for (var frame = 0; frame < 12; frame++)
            {
                writer.WriteScriptedVfxInputEvents(options, frame);
                writer.WriteFrame(options, frame, RuntimeFrameStats.Empty);
            }
        }

        var documents = File.ReadAllLines(path)
            .Select(static line => JsonDocument.Parse(line))
            .ToArray();
        try
        {
            var inputRecords = documents
                .Where(document => document.RootElement.GetProperty("event").GetString() == "vfx_input")
                .ToArray();

            Assert.Equal(11, inputRecords.Length);
            Assert.Equal("quake-e1m1", inputRecords[0].RootElement.GetProperty("effect").GetString());
            Assert.Equal(1, inputRecords[0].RootElement.GetProperty("frame_idx").GetInt32());
            Assert.Equal("w_down", inputRecords[0].RootElement.GetProperty("action").GetString());
            Assert.Equal("fire", inputRecords[4].RootElement.GetProperty("action").GetString());
            Assert.Equal("s_up", inputRecords[^1].RootElement.GetProperty("action").GetString());
            Assert.False(string.IsNullOrWhiteSpace(inputRecords[0].RootElement.GetProperty("hash_key").GetString()));
            Assert.Equal(
                documents[0].RootElement.GetProperty("hash_key").GetString(),
                inputRecords[0].RootElement.GetProperty("hash_key").GetString());
        }
        finally
        {
            foreach (var document in documents)
            {
                document.Dispose();
            }
        }
    }

    [Fact]
    public void ShowcaseHarnessJsonlWriterEmitsDeterministicMermaidRecords()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-mermaid-harness-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--mermaid-harness", "--mermaid-jsonl", path, "--mermaid-seed=700", "--mermaid-run-id=mermaid-run"],
            _ => null);

        using (var writer = ShowcaseHarnessJsonlWriter.CreateMermaid(options))
        {
            Assert.NotNull(writer);
            writer.WriteLaunch(options);
            writer.WriteFrame(options, 0, RuntimeFrameStats.Empty);
            writer.WriteFrame(options, 1, RuntimeFrameStats.Empty);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(4, lines.Length);
        using var launch = JsonDocument.Parse(lines[0]);
        using var frame0 = JsonDocument.Parse(lines[1]);
        using var frame1 = JsonDocument.Parse(lines[2]);
        using var done = JsonDocument.Parse(lines[3]);
        Assert.Equal("mermaid_harness_start", launch.RootElement.GetProperty("event").GetString());
        Assert.False(string.IsNullOrWhiteSpace(launch.RootElement.GetProperty("hash_key").GetString()));
        Assert.Equal(options.Width, launch.RootElement.GetProperty("cols").GetUInt16());
        Assert.Equal(options.Height, launch.RootElement.GetProperty("rows").GetUInt16());
        Assert.Equal((ulong)700, launch.RootElement.GetProperty("seed").GetUInt64());
        Assert.True(launch.RootElement.GetProperty("sample_count").GetInt32() > 0);
        Assert.Equal(JsonValueKind.Object, launch.RootElement.GetProperty("env").ValueKind);
        Assert.Equal("mermaid_frame", frame0.RootElement.GetProperty("event").GetString());
        Assert.Equal("mermaid", frame0.RootElement.GetProperty("harness").GetString());
        Assert.Equal("mermaid_showcase", frame0.RootElement.GetProperty("screen_slug").GetString());
        Assert.Equal("mermaid-run", frame0.RootElement.GetProperty("run_id").GetString());
        Assert.Equal(0, frame0.RootElement.GetProperty("sample_idx").GetInt32());
        Assert.NotEqual((ulong)0, frame0.RootElement.GetProperty("hash").GetUInt64());
        Assert.Equal(options.Width, frame0.RootElement.GetProperty("cols").GetUInt16());
        Assert.Equal(options.Height, frame0.RootElement.GetProperty("rows").GetUInt16());
        Assert.False(string.IsNullOrWhiteSpace(frame0.RootElement.GetProperty("sample_id").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(frame0.RootElement.GetProperty("tier").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(frame0.RootElement.GetProperty("glyph_mode").GetString()));
        Assert.Equal(JsonValueKind.False, frame0.RootElement.GetProperty("cache_hit").ValueKind);
        Assert.True(frame0.RootElement.GetProperty("render_time_ms").GetDouble() > 0);
        Assert.True(frame0.RootElement.GetProperty("config_hash").GetUInt64() > 0);
        Assert.True(frame0.RootElement.GetProperty("init_config_hash").GetUInt64() > 0);
        Assert.True(frame0.RootElement.GetProperty("link_count").GetInt32() >= 0);
        Assert.False(string.IsNullOrWhiteSpace(frame0.RootElement.GetProperty("link_mode").GetString()));
        Assert.True(frame0.RootElement.GetProperty("parse_ms").GetDouble() > 0);
        Assert.True(frame0.RootElement.GetProperty("layout_ms").GetDouble() > 0);
        Assert.True(frame0.RootElement.GetProperty("route_ms").GetDouble() > 0);
        Assert.True(frame0.RootElement.GetProperty("render_ms").GetDouble() > 0);
        Assert.Equal("harness_inputs", frame0.RootElement.GetProperty("checksum_source").GetString());
        Assert.Equal(JsonValueKind.Null, frame0.RootElement.GetProperty("render_checksum").ValueKind);
        Assert.NotEqual(
            frame0.RootElement.GetProperty("checksum").GetString(),
            frame1.RootElement.GetProperty("checksum").GetString());
        Assert.Equal("mermaid_harness_done", done.RootElement.GetProperty("event").GetString());
        Assert.Equal("mermaid-run", done.RootElement.GetProperty("run_id").GetString());
        Assert.Equal(2, done.RootElement.GetProperty("total_frames").GetInt32());
    }

    [Fact]
    public void ShowcaseHarnessJsonlWriterEmitsRenderedFrameChecksums()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-vfx-render-harness-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--vfx-harness", "--vfx-jsonl", path, "--vfx-effect=plasma", "--vfx-seed=99", "--width=72", "--height=18"],
            _ => null);
        var buffer0 = new RenderBuffer(options.Width, options.Height);
        var buffer1 = new RenderBuffer(options.Width, options.Height);
        ShowcaseViewFactory.Build(
                inlineMode: false,
                screenNumber: options.ScreenNumber,
                frame: 0,
                width: options.Width,
                height: options.Height)
            .Render(new RuntimeRenderContext(buffer0, FrankenTui.Core.Rect.FromSize(options.Width, options.Height), Theme.DefaultTheme));
        ShowcaseViewFactory.Build(
                inlineMode: false,
                screenNumber: options.ScreenNumber,
                frame: 1,
                width: options.Width,
                height: options.Height)
            .Render(new RuntimeRenderContext(buffer1, FrankenTui.Core.Rect.FromSize(options.Width, options.Height), Theme.DefaultTheme));

        using (var writer = ShowcaseHarnessJsonlWriter.CreateVfx(options))
        {
            Assert.NotNull(writer);
            writer.WriteFrame(options, 0, RuntimeFrameStats.Empty, buffer0);
            writer.WriteFrame(options, 1, RuntimeFrameStats.Empty, buffer1);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var frame0 = JsonDocument.Parse(lines[0]);
        using var frame1 = JsonDocument.Parse(lines[1]);
        Assert.Equal("render_buffer", frame0.RootElement.GetProperty("checksum_source").GetString());
        Assert.Equal("vfx_frame", frame0.RootElement.GetProperty("event").GetString());
        Assert.Equal("plasma", frame0.RootElement.GetProperty("effect").GetString());
        Assert.Equal("Plasma", frame0.RootElement.GetProperty("effect_label").GetString());
        Assert.Equal("local-effect-braille-canvas", frame0.RootElement.GetProperty("renderer").GetString());
        Assert.Equal("braille", frame0.RootElement.GetProperty("canvas_mode").GetString());
        Assert.False(frame0.RootElement.GetProperty("fps_effect").GetBoolean());
        Assert.Equal(0, frame0.RootElement.GetProperty("frame_idx").GetInt32());
        Assert.Equal(options.Width, frame0.RootElement.GetProperty("cols").GetUInt16());
        Assert.Equal(options.Height, frame0.RootElement.GetProperty("rows").GetUInt16());
        Assert.NotEqual((ulong)0, frame0.RootElement.GetProperty("hash").GetUInt64());
        Assert.Equal(
            frame0.RootElement.GetProperty("checksum").GetString(),
            frame0.RootElement.GetProperty("render_checksum").GetString());
        Assert.NotEqual(
            frame0.RootElement.GetProperty("input_checksum").GetString(),
            frame0.RootElement.GetProperty("render_checksum").GetString());
        Assert.NotEqual(
            frame0.RootElement.GetProperty("render_checksum").GetString(),
            frame1.RootElement.GetProperty("render_checksum").GetString());
    }

    [Fact]
    public void ShowcaseHarnessJsonlWriterEmitsVfxPerfRecords()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-vfx-perf-harness-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--vfx-harness", "--vfx-perf", "--vfx-jsonl", path, "--vfx-effect=matrix", "--vfx-seed=13", "--vfx-run-id=perf-run"],
            _ => null);

        using (var writer = ShowcaseHarnessJsonlWriter.CreateVfx(options))
        {
            Assert.NotNull(writer);
            writer.WriteFrame(options, 0, RuntimeFrameStats.Empty);
            writer.WriteFrame(options, 1, RuntimeFrameStats.Empty);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(5, lines.Length);
        using var frame = JsonDocument.Parse(lines[1]);
        using var summary = JsonDocument.Parse(lines[4]);
        Assert.Equal("vfx_perf_frame", frame.RootElement.GetProperty("event").GetString());
        Assert.Equal("perf-run", frame.RootElement.GetProperty("run_id").GetString());
        Assert.Equal("matrix", frame.RootElement.GetProperty("effect").GetString());
        Assert.Equal(0, frame.RootElement.GetProperty("frame_idx").GetInt32());
        Assert.True(frame.RootElement.GetProperty("update_ms").GetDouble() > 0.0);
        Assert.True(frame.RootElement.GetProperty("render_ms").GetDouble() > 0.0);
        Assert.True(frame.RootElement.GetProperty("diff_ms").GetDouble() > 0.0);
        Assert.True(frame.RootElement.GetProperty("present_ms").GetDouble() > 0.0);
        Assert.True(frame.RootElement.GetProperty("total_ms").GetDouble() > 0.0);
        Assert.Equal(options.Width, frame.RootElement.GetProperty("cols").GetUInt16());
        Assert.Equal(options.Height, frame.RootElement.GetProperty("rows").GetUInt16());
        Assert.Equal(options.VfxHarness.TickMilliseconds, frame.RootElement.GetProperty("tick_ms").GetUInt32());
        Assert.Equal((ulong)13, frame.RootElement.GetProperty("seed").GetUInt64());
        Assert.Equal("vfx_perf_summary", summary.RootElement.GetProperty("event").GetString());
        Assert.Equal(2, summary.RootElement.GetProperty("count").GetInt32());
        Assert.True(summary.RootElement.GetProperty("total_ms_p50").GetDouble() > 0.0);
        Assert.True(summary.RootElement.GetProperty("total_ms_p95").GetDouble() > 0.0);
        Assert.True(summary.RootElement.GetProperty("total_ms_p99").GetDouble() > 0.0);
        Assert.True(summary.RootElement.GetProperty("render_ms_p50").GetDouble() > 0.0);
        Assert.Equal("render", summary.RootElement.GetProperty("top_phase").GetString());
    }

    [Fact]
    public void ShowcaseQuakeScreenRendersDeterministicCanvasSurface()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(170, 38),
            screenNumber: 45,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight).AdvanceScript(3);
        var buffer = new RenderBuffer(170, 38);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 38), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Quake E1M1", screen);
        Assert.Contains("WASD move", screen);
        Assert.Contains("Player + Physics", screen);
        Assert.Contains("Mesh Raster Evidence", screen);
        Assert.Contains("QUAKE_E1M1_VERTS", screen);
        Assert.Contains("clip_triangle_near", screen);
        Assert.Contains("palette_quake_stone", screen);
        Assert.Contains("FxQuality", screen);
        Assert.Contains("JSONL fields", screen);
        Assert.Contains("local-fps-braille-canvas", screen);
        Assert.Contains("Mode: braille", screen);
        Assert.DoesNotContain("Final easter egg placeholder", screen);
        Assert.Contains('⣿', screen);
    }

    [Fact]
    public void ShowcaseVfxGoldenRegistryVerifiesSavedFrameHashes()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-vfx-golden-{Guid.NewGuid():N}.json");
        var options = ShowcaseCliOptions.Parse(
            ["--vfx-harness", "--vfx-effect=matrix", "--vfx-seed=13", "--vfx-size=80x24"],
            _ => null);
        var hashes = new ulong[] { 10, 20, 30 };

        ShowcaseVfxGoldenRegistry.Save(path, hashes);
        var loaded = ShowcaseVfxGoldenRegistry.Load(path);
        var result = ShowcaseVfxGoldenRegistry.Verify(hashes, loaded);

        Assert.Equal("vfx_matrix_80x24_16ms_seed13", ShowcaseVfxGoldenRegistry.ScenarioName(options.VfxHarness));
        Assert.Equal(hashes, loaded);
        Assert.Equal(ShowcaseVfxGoldenOutcome.Pass, result.Outcome);
        Assert.Equal(ShowcaseVfxGoldenOutcome.Missing, ShowcaseVfxGoldenRegistry.Verify(hashes, []).Outcome);
        var mismatch = ShowcaseVfxGoldenRegistry.Verify([10, 99, 30], loaded);
        Assert.Equal(ShowcaseVfxGoldenOutcome.Mismatch, mismatch.Outcome);
        Assert.Equal(1, mismatch.MismatchIndex);
        Assert.Equal((ulong)20, mismatch.Expected);
        Assert.Equal((ulong)99, mismatch.Actual);

        var updated = ShowcaseVfxGoldenRegistry.VerifyOrUpdate(path, [40, 50], update: true);
        Assert.Equal(ShowcaseVfxGoldenOutcome.Pass, updated.Outcome);
        Assert.Equal<ulong>([40, 50], ShowcaseVfxGoldenRegistry.Load(path));
        Assert.Equal(ShowcaseVfxGoldenOutcome.Pass, ShowcaseVfxGoldenRegistry.VerifyOrUpdate(path, [40, 50], update: false).Outcome);
    }

    [Fact]
    public void ShowcaseVfxGoldenRegistryExtractsFrameHashesFromJsonl()
    {
        var jsonl = Path.Combine(Path.GetTempPath(), $"ftui-vfx-golden-jsonl-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--vfx-harness", "--vfx-jsonl", jsonl, "--vfx-effect=plasma", "--vfx-seed=7"],
            _ => null);

        using (var writer = ShowcaseHarnessJsonlWriter.CreateVfx(options))
        {
            Assert.NotNull(writer);
            writer.WriteFrame(options, 0, RuntimeFrameStats.Empty);
            writer.WriteFrame(options, 1, RuntimeFrameStats.Empty);
        }

        var hashes = ShowcaseVfxGoldenRegistry.ExtractFrameHashesFromJsonl(jsonl);

        Assert.Equal(2, hashes.Count);
        Assert.All(hashes, static hash => Assert.NotEqual((ulong)0, hash));
        Assert.NotEqual(hashes[0], hashes[1]);
    }

    [Fact]
    public void ShowcasePaneWorkspacePersistenceRoundTripsWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-pane-workspace-{Guid.NewGuid():N}.json");
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var workspace = PaneWorkspaceState.CreateDemo()
            .Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.SelectNext, start, "test"))
            .Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.CycleMode, start + TimeSpan.FromMilliseconds(16), "test"));

        var save = ShowcasePaneWorkspacePersistence.Save(path, workspace);
        var loaded = ShowcasePaneWorkspacePersistence.Load(path);

        Assert.True(save.Saved);
        Assert.Null(save.Error);
        Assert.Equal(Path.GetFullPath(path), save.Path);
        Assert.Equal(workspace.SnapshotHash(), save.SnapshotHash);
        Assert.Equal(ShowcasePaneWorkspacePersistence.CurrentSchemaVersion, save.SchemaVersion);
        Assert.True(loaded.Loaded);
        Assert.Null(loaded.Error);
        Assert.Equal(ShowcasePaneWorkspacePersistence.CurrentSchemaVersion, loaded.SchemaVersion);
        Assert.False(loaded.MigrationApplied);
        Assert.Null(loaded.MigrationFromVersion);
        Assert.Equal(workspace.SnapshotHash(), loaded.Workspace.SnapshotHash());
    }

    [Fact]
    public void ShowcasePaneWorkspacePersistenceWritesVersionedEnvelope()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-pane-workspace-envelope-{Guid.NewGuid():N}.json");
        var workspace = PaneWorkspaceState.CreateDemo();

        var save = ShowcasePaneWorkspacePersistence.Save(path, workspace);

        Assert.True(save.Saved);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(
            ShowcasePaneWorkspacePersistence.CurrentSchemaVersion,
            document.RootElement.GetProperty("schema_version").GetString());
        Assert.Equal(JsonValueKind.Object, document.RootElement.GetProperty("workspace").ValueKind);
        Assert.False(document.RootElement.TryGetProperty("panes", out _));
    }

    [Fact]
    public void ShowcasePaneWorkspacePersistenceMigratesRawWorkspaceSnapshot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-pane-workspace-raw-{Guid.NewGuid():N}.json");
        var workspace = PaneWorkspaceState.CreateDemo()
            .Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.SelectNext, DateTimeOffset.Parse("2026-05-01T00:00:00Z"), "legacy"));
        File.WriteAllText(path, workspace.ToJson());

        var loaded = ShowcasePaneWorkspacePersistence.Load(path);

        Assert.True(loaded.Loaded);
        Assert.Null(loaded.Error);
        Assert.Equal(ShowcasePaneWorkspacePersistence.CurrentSchemaVersion, loaded.SchemaVersion);
        Assert.True(loaded.MigrationApplied);
        Assert.Equal("raw-pane-workspace-v1", loaded.MigrationFromVersion);
        Assert.Equal(workspace.SnapshotHash(), loaded.Workspace.SnapshotHash());
    }

    [Fact]
    public void ShowcasePaneWorkspacePersistenceCorpusUsesCanonicalWorkspaceJson()
    {
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var cases = new Dictionary<string, PaneWorkspaceState>
        {
            ["default"] = PaneWorkspaceState.CreateDemo(),
            ["resized"] = PaneWorkspaceState.CreateDemo()
                .Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.GrowPrimary, start, "corpus")),
            ["restored"] = PaneWorkspaceState.CreateDemo()
                .Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.SelectNext, start, "corpus"))
                .Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.CycleMode, start + TimeSpan.FromMilliseconds(16), "corpus"))
        };

        foreach (var (name, workspace) in cases)
        {
            var envelope = ShowcasePaneWorkspacePersistence.ToJson(workspace);
            var loaded = ShowcasePaneWorkspacePersistence.LoadJson(envelope);
            var reencoded = ShowcasePaneWorkspacePersistence.ToJson(loaded.Workspace);

            Assert.True(loaded.Loaded);
            Assert.False(loaded.MigrationApplied);
            Assert.Equal(ShowcasePaneWorkspacePersistence.CurrentSchemaVersion, loaded.SchemaVersion);
            Assert.Equal(workspace.SnapshotHash(), loaded.Workspace.SnapshotHash());
            Assert.Equal(envelope, reencoded);
        }
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterIncludesPaneWorkspaceMigrationEvidence()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-pane-migration-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=6", "--pane-workspace=workspace.json", "--evidence-jsonl", path],
            _ => null);
        var workspace = PaneWorkspaceState.CreateDemo();
        var load = new ShowcasePaneWorkspaceLoadResult(
            workspace,
            Loaded: true,
            SchemaVersion: ShowcasePaneWorkspacePersistence.CurrentSchemaVersion,
            MigrationApplied: true,
            MigrationFromVersion: "raw-pane-workspace-v1");

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteLaunch(options, load);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var launch = JsonDocument.Parse(line);
        Assert.True(launch.RootElement.GetProperty("pane_workspace_loaded").GetBoolean());
        Assert.Equal(ShowcasePaneWorkspacePersistence.CurrentSchemaVersion, launch.RootElement.GetProperty("pane_workspace_schema_version").GetString());
        Assert.True(launch.RootElement.GetProperty("pane_workspace_migration_applied").GetBoolean());
        Assert.Equal("raw-pane-workspace-v1", launch.RootElement.GetProperty("pane_workspace_migration_from_version").GetString());
        Assert.Equal(workspace.SnapshotHash(), launch.RootElement.GetProperty("pane_workspace_snapshot_hash").GetString());
    }

    [Fact]
    public void ShowcasePaneWorkspacePersistencePreservesInvalidSnapshot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-pane-workspace-invalid-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{ not-json");

        var loaded = ShowcasePaneWorkspacePersistence.Load(path);

        Assert.False(loaded.Loaded);
        Assert.NotNull(loaded.Error);
        Assert.NotNull(loaded.InvalidSnapshotPath);
        Assert.True(File.Exists(loaded.InvalidSnapshotPath));
        Assert.Equal("{ not-json", File.ReadAllText(loaded.InvalidSnapshotPath));
        Assert.Equal(PaneWorkspaceState.CreateDemo().SnapshotHash(), loaded.Workspace.SnapshotHash());
    }

    [Fact]
    public void ShowcaseLayoutLabSurfacesPaneWorkspaceRecoveryStatus()
    {
        var invalidPath = Path.Combine(Path.GetTempPath(), $"ftui-pane-workspace-invalid-{Guid.NewGuid():N}.json.invalid");
        var load = new ShowcasePaneWorkspaceLoadResult(
            PaneWorkspaceState.CreateDemo(),
            Loaded: false,
            Error: "JsonException",
            InvalidSnapshotPath: invalidPath);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(80, 20),
            screenNumber: 6,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight,
            paneWorkspaceLoad: load);
        var buffer = new RenderBuffer(80, 20);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(80, 20), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Load:", screen);
        Assert.Contains("recovered", screen);
        Assert.Contains("JsonException", screen);
    }

    private static ShowcaseDemoState ApplyKey(ShowcaseDemoState state, KeyGesture gesture, DateTimeOffset timestamp)
    {
        var terminalEvent = TerminalEvent.Key(gesture, timestamp);
        return state.ApplyInput(Envelope(terminalEvent, timestamp), RuntimeFrameStats.Empty);
    }

    private static ShowcaseDemoState ApplyMouse(
        ShowcaseDemoState state,
        ushort column,
        ushort row,
        DateTimeOffset timestamp,
        TerminalMouseButton button = TerminalMouseButton.Left,
        TerminalMouseKind kind = TerminalMouseKind.Down)
    {
        var terminalEvent = TerminalEvent.Mouse(
            new MouseGesture(column, row, button, kind),
            timestamp);
        return state.ApplyInput(Envelope(terminalEvent, timestamp), RuntimeFrameStats.Empty);
    }

    private static RuntimeInputEnvelope Envelope(TerminalEvent terminalEvent, DateTimeOffset timestamp) =>
        new(
            terminalEvent,
            terminalEvent,
            [],
            [],
            null,
            null,
            QuitRequested: false,
            HasWork: true,
            "test",
            timestamp);
}
