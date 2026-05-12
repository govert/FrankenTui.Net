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
        Assert.Contains("Widget Gallery", screen);
        Assert.Contains("render completeness", screen);
        Assert.Contains("45", screen);
        Assert.Contains("1: Tour", screen);
        Assert.Contains("5: Widgets", screen);
        Assert.Equal("Interact", ShowcaseCatalog.CategoryShortLabel(ShowcaseScreenCategory.Interaction));
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
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(170, 34),
            screenNumber: 24,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            LayoutInspectorScenarioIndex = 1,
            LayoutInspectorStepIndex = 1
        };

        ShowcaseSurface.Create(state)
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
        Assert.Contains("minimizes E[T]", screen);
        Assert.Contains("E[Loss_continue]", screen);
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
        Assert.Contains("Kanban Board [42/45]  default [h] [cmd] [p] [d]  Mouse: AUTO", screen);
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
                    RuntimeMode = "degraded",
                    RuntimeModeBefore = "stressed",
                    RuntimePressureClass = "hard_overload",
                    RuntimeWorkDisposition = "defer_background_drop_best_effort",
                    RuntimeGovernorReason = "queue_degraded_watermark",
                    RuntimeGovernorTransition = true,
                    RuntimeStrictSemanticsPreserved = true,
                    RuntimeQueueInFlight = 8,
                    RuntimeQueueMaxDepth = 10,
                    RuntimeQueueDroppedDelta = 1,
                    RuntimeResizeCoalescingActive = true,
                    RuntimeRecoveryIntervalsObserved = 0,
                    RuntimeRecoveryIntervalsRequired = 3,
                    RuntimeDeferredWorkTotal = 2,
                    RuntimeCoalescedWorkTotal = 1,
                    RuntimeDroppedWorkTotal = 1,
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
        Assert.Equal("degraded", frame.RootElement.GetProperty("runtime_mode").GetString());
        Assert.Equal("stressed", frame.RootElement.GetProperty("runtime_mode_before").GetString());
        Assert.Equal("hard_overload", frame.RootElement.GetProperty("pressure_class").GetString());
        Assert.Equal("defer_background_drop_best_effort", frame.RootElement.GetProperty("work_disposition").GetString());
        Assert.Equal("queue_degraded_watermark", frame.RootElement.GetProperty("governor_reason").GetString());
        Assert.True(frame.RootElement.GetProperty("governor_transition").GetBoolean());
        Assert.True(frame.RootElement.GetProperty("strict_semantics_preserved").GetBoolean());
        Assert.Equal((ulong)8, frame.RootElement.GetProperty("queue_in_flight").GetUInt64());
        Assert.Equal(10, frame.RootElement.GetProperty("queue_max_depth").GetInt32());
        Assert.Equal((ulong)1, frame.RootElement.GetProperty("queue_dropped_delta").GetUInt64());
        Assert.True(frame.RootElement.GetProperty("resize_coalescing_active").GetBoolean());
        Assert.Equal(0, frame.RootElement.GetProperty("recovery_intervals_observed").GetByte());
        Assert.Equal(3, frame.RootElement.GetProperty("recovery_intervals_required").GetByte());
        Assert.Equal((ulong)2, frame.RootElement.GetProperty("deferred_work_total").GetUInt64());
        Assert.Equal((ulong)1, frame.RootElement.GetProperty("coalesced_work_total").GetUInt64());
        Assert.Equal((ulong)1, frame.RootElement.GetProperty("dropped_work_total").GetUInt64());
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
    public void ShowcaseEvidenceJsonlWriterEmitsTourLandingScrollMouseEvent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-tour-scroll-event-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=1", "--evidence-jsonl", path],
            _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 1,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var terminalEvent = TerminalEvent.Mouse(
            new MouseGesture(20, 6, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
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
        Assert.Equal("tour_landing_step_next", mouseEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("overlay:tour", mouseEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.OverlayTour, mouseEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
        Assert.Equal("overlay", mouseEvent.RootElement.GetProperty("hit_layer").GetString());
        Assert.Equal(3, mouseEvent.RootElement.GetProperty("tour_start_screen").GetInt32());
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

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var fallbackEvent = JsonDocument.Parse(lines[0]);
        using var mouseEvent = JsonDocument.Parse(lines[1]);
        Assert.Equal("mouse_event", fallbackEvent.RootElement.GetProperty("event").GetString());
        Assert.Equal("down_click_fallback", fallbackEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("pane:4", fallbackEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.PaneRawId(4), fallbackEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
        Assert.Equal("mouse_event", mouseEvent.RootElement.GetProperty("event").GetString());
        Assert.Equal("input", mouseEvent.RootElement.GetProperty("mouse_trigger").GetString());
        Assert.Equal("down_left", mouseEvent.RootElement.GetProperty("mouse_kind").GetString());
        Assert.Equal("switch_screen", mouseEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal(45, mouseEvent.RootElement.GetProperty("mouse_column").GetInt32());
        Assert.Equal(7, mouseEvent.RootElement.GetProperty("mouse_row").GetInt32());
        Assert.Equal("down_left", mouseEvent.RootElement.GetProperty("kind").GetString());
        Assert.Equal(45, mouseEvent.RootElement.GetProperty("x").GetInt32());
        Assert.Equal(7, mouseEvent.RootElement.GetProperty("y").GetInt32());
        Assert.Equal("pane:4", mouseEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.PaneRawId(4), mouseEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
        Assert.Equal("pane", mouseEvent.RootElement.GetProperty("hit_layer").GetString());
        Assert.Equal(4, mouseEvent.RootElement.GetProperty("hit_target_screen_number").GetInt32());
        Assert.Equal(JsonValueKind.Null, mouseEvent.RootElement.GetProperty("hit_target_category").ValueKind);
        Assert.Equal("switch_screen", mouseEvent.RootElement.GetProperty("action").GetString());
        Assert.Equal("Dashboard", mouseEvent.RootElement.GetProperty("current_screen").GetString());
        Assert.Equal("Code Explorer", mouseEvent.RootElement.GetProperty("target_screen").GetString());
        Assert.Equal(2, mouseEvent.RootElement.GetProperty("mouse_current_screen_number").GetInt32());
        Assert.Equal(4, mouseEvent.RootElement.GetProperty("mouse_target_screen_number").GetInt32());
        Assert.Equal("code_explorer", mouseEvent.RootElement.GetProperty("mouse_target_screen_slug").GetString());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsMouseEventForDashboardLeftPanelPaneLink()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-left-pane-mouse-event-{Guid.NewGuid():N}.jsonl");
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
            new MouseGesture(12, 5, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var after = before.ApplyInput(
            Envelope(terminalEvent, timestamp),
            RuntimeFrameStats.Empty);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, terminalEvent, before, after);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var fallbackEvent = JsonDocument.Parse(lines[0]);
        using var mouseEvent = JsonDocument.Parse(lines[1]);
        Assert.Equal("down_click_fallback", fallbackEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("pane:18", fallbackEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.PaneRawId(18), fallbackEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
        Assert.Equal("switch_screen", mouseEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("pane:18", mouseEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.PaneRawId(18), mouseEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
        Assert.Equal("pane", mouseEvent.RootElement.GetProperty("hit_layer").GetString());
        Assert.Equal(18, mouseEvent.RootElement.GetProperty("hit_target_screen_number").GetInt32());
        Assert.Equal("Visual Effects", mouseEvent.RootElement.GetProperty("target_screen").GetString());
        Assert.Equal(18, mouseEvent.RootElement.GetProperty("mouse_target_screen_number").GetInt32());
        Assert.Equal("visual_effects", mouseEvent.RootElement.GetProperty("mouse_target_screen_slug").GetString());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsPaletteScrollMouseEvent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-palette-scroll-event-{Guid.NewGuid():N}.jsonl");
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
            new MouseGesture(22, 8, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
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
        Assert.Equal("palette_scroll", mouseEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("palette_scroll", mouseEvent.RootElement.GetProperty("action").GetString());
        Assert.Equal("palette", mouseEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.OverlayHitBase, mouseEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
        Assert.Equal("overlay", mouseEvent.RootElement.GetProperty("hit_layer").GetString());
        Assert.True(mouseEvent.RootElement.GetProperty("palette_open").GetBoolean());
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
        Assert.Equal(ShowcaseFrameHitRegistry.OverlayHitBase, mouseEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
        Assert.Equal("overlay", mouseEvent.RootElement.GetProperty("hit_layer").GetString());
        Assert.Equal(JsonValueKind.Null, mouseEvent.RootElement.GetProperty("hit_target_screen_number").ValueKind);
        Assert.Equal("Dashboard", mouseEvent.RootElement.GetProperty("current_screen").GetString());
        Assert.Equal("none", mouseEvent.RootElement.GetProperty("target_screen").GetString());
        Assert.Equal(2, mouseEvent.RootElement.GetProperty("mouse_current_screen_number").GetInt32());
        Assert.True(mouseEvent.RootElement.GetProperty("mouse_target_screen_number").ValueKind is JsonValueKind.Null);
        Assert.True(mouseEvent.RootElement.GetProperty("palette_open").GetBoolean());
    }

    [Fact]
    public void ShowcaseFrameHitRegistryGivesCommandPalettePriorityOverStatusRow()
    {
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

        var hit = ShowcaseFrameHitRegistry.HitTest(opened, 40, 17);

        Assert.True(opened.Session.CommandPalette.IsOpen);
        Assert.Equal(ShowcaseHitLayer.Overlay, hit.Layer);
        Assert.Equal("palette", hit.LocalHitId);
        Assert.Equal(ShowcaseFrameHitRegistry.OverlayHitBase, hit.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryUsesUpstreamShapedHitBands()
    {
        Assert.Equal(1000u, ShowcaseFrameHitRegistry.TabHitBase);
        Assert.Equal(2000u, ShowcaseFrameHitRegistry.CategoryHitBase);
        Assert.Equal(4000u, ShowcaseFrameHitRegistry.PaneHitBase);
        Assert.Equal(5000u, ShowcaseFrameHitRegistry.OverlayHitBase);
        Assert.Equal(6000u, ShowcaseFrameHitRegistry.StatusHitBase);
        Assert.Equal(1015u, ShowcaseFrameHitRegistry.TabRawId(16));
        Assert.Equal(4015u, ShowcaseFrameHitRegistry.PaneRawId(16));
        Assert.Equal(ShowcaseScreenCategory.Visuals, ShowcaseFrameHitRegistry.CategoryFromRawId(2002));
        Assert.Equal(16, ShowcaseFrameHitRegistry.ScreenFromRawId(1015));
        Assert.Equal(16, ShowcaseFrameHitRegistry.ScreenFromRawId(4015));
        Assert.Equal(5, ShowcaseFrameHitRegistry.ScreenFromRawId(2001));
    }

    [Fact]
    public void ShowcaseFrameHitRegistryRoutesLiveMarkdownPanes()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(82, 26),
            screenNumber: 43,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var search = ShowcaseFrameHitRegistry.HitTest(state, 3, 5);
        Assert.Equal(ShowcaseHitLayer.Content, search.Layer);
        Assert.Equal("live_markdown:search", search.LocalHitId);
        Assert.Equal((uint)43_000, search.UpstreamHitId);

        var editor = ShowcaseFrameHitRegistry.HitTest(state, 3, 7);
        Assert.Equal(ShowcaseHitLayer.Content, editor.Layer);
        Assert.Equal("live_markdown:editor", editor.LocalHitId);
        Assert.Equal((uint)43_001, editor.UpstreamHitId);

        var preview = ShowcaseFrameHitRegistry.HitTest(state, 45, 7);
        Assert.Equal(ShowcaseHitLayer.Content, preview.Layer);
        Assert.Equal("live_markdown:preview", preview.LocalHitId);
        Assert.Equal((uint)43_002, preview.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsLiveMarkdownMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-live-markdown-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(["--screen=43", "--evidence-jsonl", path], _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(82, 26),
            screenNumber: 43,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var editorEvent = TerminalEvent.Mouse(new MouseGesture(3, 7, TerminalMouseButton.Left, TerminalMouseKind.Down), timestamp);
        var previewScroll = TerminalEvent.Mouse(new MouseGesture(45, 7, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll), timestamp.AddMilliseconds(10));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, editorEvent, before, before);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, previewScroll, before, before);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var editor = JsonDocument.Parse(lines[0]);
        using var scroll = JsonDocument.Parse(lines[1]);
        Assert.Equal("live_markdown_focus_editor", editor.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("live_markdown:editor", editor.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal((uint)43_001, editor.RootElement.GetProperty("target_id").GetUInt32());
        Assert.Equal("live_markdown_preview_scroll_down", scroll.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("live_markdown:preview", scroll.RootElement.GetProperty("hit_id").GetString());
    }

    [Fact]
    public void ShowcaseMarkdownLiveEditorMouseMutatesFocusSearchEditorPreview()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(82, 26),
            screenNumber: 43,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 5, timestamp);
        Assert.Equal(1, state.MarkdownLiveFocusIndex);
        Assert.Equal(1, state.MarkdownLiveSearchMatchIndex);

        state = ApplyMouse(
            state,
            3,
            7,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(0, state.MarkdownLiveFocusIndex);
        Assert.Equal(7, state.MarkdownLiveCursorLine);

        state = ApplyMouse(state, 45, 7, timestamp + TimeSpan.FromMilliseconds(20));
        Assert.Equal(2, state.MarkdownLiveFocusIndex);
        Assert.True(state.MarkdownLiveDiffMode);

        state = ApplyMouse(
            state,
            45,
            7,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(2, state.MarkdownLiveFocusIndex);
        Assert.Equal(1, state.MarkdownLivePreviewScroll);
    }

    [Fact]
    public void ShowcaseMarkdownLiveEditorRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(170, 40),
            screenNumber: 43,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            MarkdownLiveFocusIndex = 1,
            MarkdownLivePreviewScroll = 3,
            MarkdownLiveSearchMatchIndex = 2,
            MarkdownLiveCursorLine = 9,
            MarkdownLiveDiffMode = true
        };
        var buffer = new RenderBuffer(170, 40);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 40), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Search [focus match 3/4]", screen);
        Assert.Contains("3/4 matches", screen);
        Assert.Contains("cursor_line=9", screen);
        Assert.Contains("diff_mode=True", screen);
        Assert.Contains("preview_scroll=3", screen);
        Assert.Contains("focus=Search focus_idx=1 match=3/4", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryRoutesDragDropTabsAndItems()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(82, 26),
            screenNumber: 44,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var tab = ShowcaseFrameHitRegistry.HitTest(state, 3, 2);
        Assert.Equal(ShowcaseHitLayer.Content, tab.Layer);
        Assert.Equal("drag_drop:tab:0", tab.LocalHitId);
        Assert.Equal((uint)44_000, tab.UpstreamHitId);

        var leftItem = ShowcaseFrameHitRegistry.HitTest(state, 3, 5);
        Assert.Equal(ShowcaseHitLayer.Content, leftItem.Layer);
        Assert.Equal("drag_drop:item:0:0", leftItem.LocalHitId);
        Assert.Equal((uint)0, leftItem.UpstreamHitId);

        var rightItem = ShowcaseFrameHitRegistry.HitTest(state, 45, 5);
        Assert.Equal(ShowcaseHitLayer.Content, rightItem.Layer);
        Assert.Equal("drag_drop:item:1:0", rightItem.LocalHitId);
        Assert.Equal((uint)8, rightItem.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsDragDropMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-drag-drop-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(["--screen=44", "--evidence-jsonl", path], _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(82, 26),
            screenNumber: 44,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var tabEvent = TerminalEvent.Mouse(new MouseGesture(3, 2, TerminalMouseButton.Left, TerminalMouseKind.Down), timestamp);
        var itemEvent = TerminalEvent.Mouse(new MouseGesture(45, 5, TerminalMouseButton.Right, TerminalMouseKind.Down), timestamp.AddMilliseconds(10));
        var scrollEvent = TerminalEvent.Mouse(new MouseGesture(45, 5, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll), timestamp.AddMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, tabEvent, before, before);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, itemEvent, before, before);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, scrollEvent, before, before);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var tab = JsonDocument.Parse(lines[0]);
        using var context = JsonDocument.Parse(lines[1]);
        using var scroll = JsonDocument.Parse(lines[2]);
        Assert.Equal("drag_drop_tab_select", tab.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("drag_drop:tab:0", tab.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal((uint)44_000, tab.RootElement.GetProperty("target_id").GetUInt32());
        Assert.Equal("drag_drop_context_action", context.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal((uint)8, context.RootElement.GetProperty("target_id").GetUInt32());
        Assert.Equal("drag_drop_scroll_down", scroll.RootElement.GetProperty("mouse_action").GetString());
    }

    [Fact]
    public void ShowcaseDragDropMouseMutatesModeSelectionAndContextAction()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(82, 26),
            screenNumber: 44,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 58, 2, timestamp);
        Assert.Equal(2, state.DragDropModeIndex);
        Assert.True(state.DragDropKeyboardActive);

        state = ApplyMouse(state, 45, 7, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.Equal(1, state.DragDropFocusedList);
        Assert.Equal(2, state.DragDropSelectedIndex);
        Assert.False(state.DragDropContextAction);

        state = ApplyMouse(
            state,
            45,
            7,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.Right);
        Assert.Equal(1, state.DragDropFocusedList);
        Assert.Equal(2, state.DragDropSelectedIndex);
        Assert.Equal(1, state.DragDropMoveCount);
        Assert.True(state.DragDropContextAction);

        state = ApplyMouse(
            state,
            45,
            7,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(3, state.DragDropSelectedIndex);
        Assert.False(state.DragDropContextAction);
    }

    [Fact]
    public void ShowcaseDragDropRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(170, 38),
            screenNumber: 44,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            DragDropModeIndex = 2,
            DragDropSelectedIndex = 3,
            DragDropFocusedList = 1,
            DragDropMoveCount = 4,
            DragDropKeyboardActive = true,
            DragDropContextAction = true
        };
        var buffer = new RenderBuffer(170, 38);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 38), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("[Keyboard Drag]", screen);
        Assert.Contains("List B [focus]", screen);
        Assert.Contains("> File 4 id=11", screen);
        Assert.Contains("KeyboardDragManager active=True", screen);
        Assert.Contains("Context action applied 3 in list 1; moves=4", screen);
        Assert.Contains("source_id=11", screen);
        Assert.Contains("mode=Keyboard Drag selected_index=3 focused_list=1 context=True", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryRoutesKanbanCards()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(82, 26),
            screenNumber: 42,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var first = ShowcaseFrameHitRegistry.HitTest(state, 3, 4);
        Assert.Equal(ShowcaseHitLayer.Content, first.Layer);
        Assert.Equal("kanban:0:0:1", first.LocalHitId);
        Assert.Equal((uint)1, first.UpstreamHitId);

        var progress = ShowcaseFrameHitRegistry.HitTest(state, 30, 4);
        Assert.Equal(ShowcaseHitLayer.Content, progress.Layer);
        Assert.Equal("kanban:1:0:5", progress.LocalHitId);
        Assert.Equal((uint)5, progress.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseKanbanMouseFocusAndDropMutatesBoard()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(82, 26),
            screenNumber: 42,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var down = TerminalEvent.Mouse(new MouseGesture(3, 4, TerminalMouseButton.Left, TerminalMouseKind.Down), timestamp);
        var afterDown = state.ApplyInput(Envelope(down, timestamp), RuntimeFrameStats.Empty);
        Assert.Equal(0, afterDown.KanbanBoard!.FocusCol);
        Assert.Equal(0, afterDown.KanbanBoard!.FocusRow);

        var up = TerminalEvent.Mouse(new MouseGesture(30, 4, TerminalMouseButton.Left, TerminalMouseKind.Up), timestamp.AddMilliseconds(10));
        var afterUp = afterDown.ApplyInput(Envelope(up, timestamp.AddMilliseconds(10)), RuntimeFrameStats.Empty);
        Assert.Equal(3, afterUp.KanbanBoard!.Todo.Count);
        Assert.Equal(3, afterUp.KanbanBoard!.InProgress.Count);
        Assert.Equal(1, afterUp.KanbanBoard!.InProgress[^1].Id);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsKanbanMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-kanban-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(["--screen=42", "--evidence-jsonl", path], _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(82, 26),
            screenNumber: 42,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var downEvent = TerminalEvent.Mouse(new MouseGesture(3, 4, TerminalMouseButton.Left, TerminalMouseKind.Down), timestamp);
        var after = before.ApplyInput(Envelope(downEvent, timestamp), RuntimeFrameStats.Empty);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, downEvent, before, after);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var record = JsonDocument.Parse(line);
        Assert.Equal("kanban_drag_start", record.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("kanban:0:0:1", record.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal((uint)1, record.RootElement.GetProperty("target_id").GetUInt32());
        Assert.Equal("content", record.RootElement.GetProperty("hit_layer").GetString());
    }

    [Fact]
    public void ShowcaseFrameHitRegistryRoutesMousePlaygroundTargets()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 26,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var first = ShowcaseFrameHitRegistry.HitTest(state, 2, 3);
        Assert.Equal(ShowcaseHitLayer.Content, first.Layer);
        Assert.Equal("target:1", first.LocalHitId);
        Assert.Equal((uint)1, first.UpstreamHitId);

        var seventh = ShowcaseFrameHitRegistry.HitTest(state, 16, 7);
        Assert.Equal(ShowcaseHitLayer.Content, seventh.Layer);
        Assert.Equal("target:7", seventh.LocalHitId);
        Assert.Equal((uint)7, seventh.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsMousePlaygroundTargetClick()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-mouse-target-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=26", "--evidence-jsonl", path],
            _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 26,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var downEvent = TerminalEvent.Mouse(
            new MouseGesture(16, 7, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var after = before.ApplyInput(Envelope(downEvent, timestamp), RuntimeFrameStats.Empty);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, downEvent, before, after);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var record = JsonDocument.Parse(line);
        Assert.Equal("target_click", record.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("target:7", record.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal((uint)7, record.RootElement.GetProperty("hit_raw_id").GetUInt32());
        Assert.Equal("content", record.RootElement.GetProperty("hit_layer").GetString());
        Assert.Equal((uint)7, record.RootElement.GetProperty("target_id").GetUInt32());
    }

    [Fact]
    public void ShowcaseMousePlaygroundMouseMutatesTargetOverlayAndJitterState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 26,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 16, 7, timestamp);
        Assert.Equal(6, state.MousePlaygroundSelectedTargetIndex);
        Assert.Equal(1, state.MousePlaygroundSelectedTargetClicks);
        Assert.Equal(7, state.MousePlaygroundEventIndex);
        Assert.Equal(0, state.MousePlaygroundFocusIndex);
        Assert.False(state.MousePlaygroundContextArmed);

        state = ApplyMouse(
            state,
            16,
            7,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.Right);
        Assert.True(state.MousePlaygroundOverlayVisible);
        Assert.Equal(8, state.MousePlaygroundEventIndex);
        Assert.Equal(1, state.MousePlaygroundFocusIndex);
        Assert.True(state.MousePlaygroundContextArmed);

        state = ApplyMouse(
            state,
            16,
            7,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.True(state.MousePlaygroundJitterStatsVisible);
        Assert.Equal(9, state.MousePlaygroundEventIndex);
        Assert.Equal(2, state.MousePlaygroundFocusIndex);
        Assert.False(state.MousePlaygroundContextArmed);
    }

    [Fact]
    public void ShowcaseMousePlaygroundRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(120, 32),
            screenNumber: 26,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            MousePlaygroundSelectedTargetIndex = 6,
            MousePlaygroundSelectedTargetClicks = 3,
            MousePlaygroundFocusIndex = 1,
            MousePlaygroundEventIndex = 8,
            MousePlaygroundOverlayVisible = true,
            MousePlaygroundJitterStatsVisible = true,
            MousePlaygroundContextArmed = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Hover: T7", screen);
        Assert.Contains("Overlay: ON", screen);
        Assert.Contains("Focus: 1 ctx", screen);
        Assert.Contains("Jitter Stats: ON", screen);
        Assert.Contains("Selected clicks: 3", screen);
        Assert.Contains("Stats + Overlay [ctx]", screen);
        Assert.Contains("Controls + Diagnostics", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryRoutesHyperlinkRows()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 41,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var first = ShowcaseFrameHitRegistry.HitTest(state, 3, 7);
        Assert.Equal(ShowcaseHitLayer.Link, first.Layer);
        Assert.Equal("link:1", first.LocalHitId);
        Assert.Equal(ShowcaseFrameHitRegistry.LinkRawId(0), first.UpstreamHitId);

        var fifth = ShowcaseFrameHitRegistry.HitTest(state, 3, 11);
        Assert.Equal(ShowcaseHitLayer.Link, fifth.Layer);
        Assert.Equal("link:5", fifth.LocalHitId);
        Assert.Equal(ShowcaseFrameHitRegistry.LinkRawId(4), fifth.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsHyperlinkMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-hyperlink-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=41", "--evidence-jsonl", path],
            _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 41,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var downEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 7, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var upEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 7, TerminalMouseButton.Left, TerminalMouseKind.Up),
            timestamp.AddMilliseconds(10));
        var afterDown = before.ApplyInput(Envelope(downEvent, timestamp), RuntimeFrameStats.Empty);
        var afterUp = afterDown.ApplyInput(Envelope(upEvent, timestamp.AddMilliseconds(10)), RuntimeFrameStats.Empty);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, downEvent, before, afterDown);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, upEvent, afterDown, afterUp);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var down = JsonDocument.Parse(lines[0]);
        using var up = JsonDocument.Parse(lines[1]);
        Assert.Equal("mouse_select", down.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("link:1", down.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.LinkRawId(0), down.RootElement.GetProperty("hit_raw_id").GetUInt32());
        Assert.Equal("link", down.RootElement.GetProperty("hit_layer").GetString());
        Assert.Equal((uint)1, down.RootElement.GetProperty("link_id").GetUInt32());
        Assert.Equal("mouse_activate", up.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("mouse_activate", up.RootElement.GetProperty("action").GetString());
    }

    [Fact]
    public void ShowcaseHyperlinkPlaygroundMouseMutatesHoverFocusCopyAndActivation()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 41,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(
            state,
            3,
            8,
            timestamp,
            TerminalMouseButton.Left,
            TerminalMouseKind.Move);
        Assert.Equal(1, state.HyperlinkHoverIndex);
        Assert.Equal(1, state.HyperlinkLastActionIndex);

        state = ApplyMouse(state, 3, 9, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.Equal(2, state.HyperlinkFocusIndex);
        Assert.Equal(2, state.HyperlinkHoverIndex);
        Assert.Equal(2, state.HyperlinkLastActionIndex);

        state = ApplyMouse(
            state,
            3,
            9,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.Left,
            TerminalMouseKind.Up);
        Assert.Equal(2, state.HyperlinkFocusIndex);
        Assert.Equal(4, state.HyperlinkLastActionIndex);
        Assert.Equal(1, state.HyperlinkActivationCount);

        state = ApplyMouse(
            state,
            3,
            10,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.Right);
        Assert.Equal(3, state.HyperlinkFocusIndex);
        Assert.Equal(3, state.HyperlinkHoverIndex);
        Assert.Equal(3, state.HyperlinkLastActionIndex);
        Assert.True(state.HyperlinkCopied);
    }

    [Fact]
    public void ShowcaseHyperlinkPlaygroundRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(170, 36),
            screenNumber: 41,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            HyperlinkFocusIndex = 3,
            HyperlinkHoverIndex = 4,
            HyperlinkLastActionIndex = 4,
            HyperlinkActivationCount = 2,
            HyperlinkCopied = true
        };
        var buffer = new RenderBuffer(170, 36);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(170, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Links (OSC-8) [hover 5]", screen);
        Assert.Contains(">   OSC 8 Spec", screen);
        Assert.Contains("* ANSI Reference", screen);
        Assert.Contains("Selected: OSC 8 Spec", screen);
        Assert.Contains("Action: mouse_activate", screen);
        Assert.Contains("Copied: yes", screen);
        Assert.Contains("Activations: 2", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryRoutesDashboardPaneLinks()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var leftPanelHit = ShowcaseFrameHitRegistry.HitTest(state, 12, 5);
        Assert.Equal(ShowcaseHitLayer.Pane, leftPanelHit.Layer);
        Assert.Equal("pane:18", leftPanelHit.LocalHitId);
        Assert.Equal(ShowcaseFrameHitRegistry.PaneRawId(18), leftPanelHit.UpstreamHitId);
        Assert.Equal(18, leftPanelHit.TargetScreenNumber);

        var leftPanelSecondHit = ShowcaseFrameHitRegistry.HitTest(state, 12, 6);
        Assert.Equal(ShowcaseHitLayer.Pane, leftPanelSecondHit.Layer);
        Assert.Equal("pane:8", leftPanelSecondHit.LocalHitId);
        Assert.Equal(ShowcaseFrameHitRegistry.PaneRawId(8), leftPanelSecondHit.UpstreamHitId);
        Assert.Equal(8, leftPanelSecondHit.TargetScreenNumber);

        var hit = ShowcaseFrameHitRegistry.HitTest(state, 45, 7);

        Assert.Equal(ShowcaseHitLayer.Pane, hit.Layer);
        Assert.Equal("pane:4", hit.LocalHitId);
        Assert.Equal(ShowcaseFrameHitRegistry.PaneRawId(4), hit.UpstreamHitId);
        Assert.Equal(4, hit.TargetScreenNumber);

        var afterLeftPanelClick = state.ApplyInput(
            Envelope(
                TerminalEvent.Mouse(
                    new MouseGesture(12, 5, TerminalMouseButton.Left, TerminalMouseKind.Down),
                    DateTimeOffset.Parse("2026-05-01T00:00:00Z")),
                DateTimeOffset.Parse("2026-05-01T00:00:00Z")),
            RuntimeFrameStats.Empty);

        Assert.Equal(18, afterLeftPanelClick.CurrentScreenNumber);

        var afterSecondLeftPanelClick = state.ApplyInput(
            Envelope(
                TerminalEvent.Mouse(
                    new MouseGesture(12, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
                    DateTimeOffset.Parse("2026-05-01T00:00:00Z")),
                DateTimeOffset.Parse("2026-05-01T00:00:00Z")),
            RuntimeFrameStats.Empty);

        Assert.Equal(8, afterSecondLeftPanelClick.CurrentScreenNumber);

        var after = state.ApplyInput(
            Envelope(
                TerminalEvent.Mouse(
                    new MouseGesture(45, 7, TerminalMouseButton.Left, TerminalMouseKind.Down),
                    DateTimeOffset.Parse("2026-05-01T00:00:00Z")),
                DateTimeOffset.Parse("2026-05-01T00:00:00Z")),
            RuntimeFrameStats.Empty);

        Assert.Equal(4, after.CurrentScreenNumber);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryRoutesChromeAndStatusClicks()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        var firstTabHit = ShowcaseFrameHitRegistry.HitTest(state, 5, 0);
        Assert.Equal(ShowcaseHitLayer.Tab, firstTabHit.Layer);
        Assert.Equal(1, firstTabHit.TargetScreenNumber);

        var gapHit = ShowcaseFrameHitRegistry.HitTest(state, 9, 0);
        Assert.Equal(ShowcaseHitLayer.Unknown, gapHit.Layer);

        var tabHit = ShowcaseFrameHitRegistry.HitTest(state, 45, 0);
        Assert.Equal(ShowcaseHitLayer.Tab, tabHit.Layer);
        Assert.Equal(5, tabHit.TargetScreenNumber);

        var afterTabClick = state.ApplyInput(
            Envelope(
                TerminalEvent.Mouse(new MouseGesture(45, 0, TerminalMouseButton.Left, TerminalMouseKind.Down), timestamp),
                timestamp),
            RuntimeFrameStats.Empty);
        Assert.Equal(5, afterTabClick.CurrentScreenNumber);

        var statusHit = ShowcaseFrameHitRegistry.HitTest(state, 50, 17);
        Assert.Equal(ShowcaseHitLayer.StatusToggle, statusHit.Layer);
        Assert.Equal(ShowcaseFrameHitRegistry.StatusMouseToggle, statusHit.UpstreamHitId);

        var afterStatusClick = state.ApplyInput(
            Envelope(
                TerminalEvent.Mouse(new MouseGesture(50, 17, TerminalMouseButton.Left, TerminalMouseKind.Down), timestamp),
                timestamp),
            RuntimeFrameStats.Empty);
        Assert.True(afterStatusClick.MouseCaptureEnabled);
        Assert.False(afterStatusClick.EvidenceLedgerVisible);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryRegistersCurrentScreenBodyPane()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 45,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        var hit = ShowcaseFrameHitRegistry.HitTest(state, 20, 8);

        Assert.Equal(ShowcaseHitLayer.Pane, hit.Layer);
        Assert.Equal("pane:45", hit.LocalHitId);
        Assert.Equal(ShowcaseFrameHitRegistry.PaneRawId(45), hit.UpstreamHitId);
        Assert.Equal(45, hit.TargetScreenNumber);

        var after = state.ApplyInput(
            Envelope(
                TerminalEvent.Mouse(new MouseGesture(20, 8, TerminalMouseButton.Left, TerminalMouseKind.Down), timestamp),
                timestamp),
            RuntimeFrameStats.Empty);

        Assert.Equal(45, after.CurrentScreenNumber);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsOnlyHoverChangeForFirstChromeMove()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-chrome-move-hover-{Guid.NewGuid():N}.jsonl");
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
            new MouseGesture(15, 0, TerminalMouseButton.Left, TerminalMouseKind.Move),
            timestamp);
        var after = before.ApplyInput(
            Envelope(terminalEvent, timestamp),
            RuntimeFrameStats.Empty);

        Assert.Equal(2, after.CurrentScreenNumber);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, terminalEvent, before, after);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var hoverEvent = JsonDocument.Parse(line);
        Assert.Equal("hover_change", hoverEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("hover_change", hoverEvent.RootElement.GetProperty("action").GetString());
        Assert.Equal("tab:2", hoverEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal("tab", hoverEvent.RootElement.GetProperty("hit_layer").GetString());
        Assert.Equal(2, hoverEvent.RootElement.GetProperty("hit_target_screen_number").GetInt32());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterCoalescesRepeatedChromeMoveHover()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-chrome-move-coalesce-{Guid.NewGuid():N}.jsonl");
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
        var firstMove = TerminalEvent.Mouse(
            new MouseGesture(15, 0, TerminalMouseButton.Left, TerminalMouseKind.Move),
            timestamp);
        var secondMove = TerminalEvent.Mouse(
            new MouseGesture(16, 0, TerminalMouseButton.Left, TerminalMouseKind.Move),
            timestamp.AddMilliseconds(10));
        var afterFirst = before.ApplyInput(
            Envelope(firstMove, timestamp),
            RuntimeFrameStats.Empty);
        var afterSecond = afterFirst.ApplyInput(
            Envelope(secondMove, timestamp.AddMilliseconds(10)),
            RuntimeFrameStats.Empty);

        Assert.Equal(2, afterFirst.CurrentScreenNumber);
        Assert.Equal(2, afterSecond.CurrentScreenNumber);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, firstMove, before, afterFirst);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, secondMove, afterFirst, afterSecond);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var hoverEvent = JsonDocument.Parse(lines[0]);
        using var moveEvent = JsonDocument.Parse(lines[1]);
        Assert.Equal("hover_change", hoverEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("tab:2", hoverEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal("move", moveEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("move", moveEvent.RootElement.GetProperty("action").GetString());
        Assert.Equal("tab:2", moveEvent.RootElement.GetProperty("hit_id").GetString());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsHoverThenDragForwardForChromeDrag()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-chrome-drag-{Guid.NewGuid():N}.jsonl");
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
            new MouseGesture(15, 0, TerminalMouseButton.Left, TerminalMouseKind.Drag),
            timestamp);
        var after = before.ApplyInput(
            Envelope(terminalEvent, timestamp),
            RuntimeFrameStats.Empty);

        Assert.Equal(2, after.CurrentScreenNumber);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, terminalEvent, before, after);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var hoverEvent = JsonDocument.Parse(lines[0]);
        using var mouseEvent = JsonDocument.Parse(lines[1]);
        Assert.Equal("hover_change", hoverEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("hover_change", hoverEvent.RootElement.GetProperty("action").GetString());
        Assert.Equal("tab:2", hoverEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal("drag_forward", mouseEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("drag_forward", mouseEvent.RootElement.GetProperty("action").GetString());
        Assert.Equal("tab:2", mouseEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal("tab", mouseEvent.RootElement.GetProperty("hit_layer").GetString());
        Assert.Equal(2, mouseEvent.RootElement.GetProperty("hit_target_screen_number").GetInt32());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsTabNoChangeForCurrentTabClick()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-tab-no-change-{Guid.NewGuid():N}.jsonl");
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
            new MouseGesture(12, 0, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var after = before.ApplyInput(
            Envelope(terminalEvent, timestamp),
            RuntimeFrameStats.Empty);

        Assert.Equal(2, after.CurrentScreenNumber);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, terminalEvent, before, after);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var fallbackEvent = JsonDocument.Parse(lines[0]);
        using var mouseEvent = JsonDocument.Parse(lines[1]);
        Assert.Equal("down_click_fallback", fallbackEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("tab:2", fallbackEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal("tab_no_change", mouseEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("tab_no_change", mouseEvent.RootElement.GetProperty("action").GetString());
        Assert.Equal("tab:2", mouseEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.TabRawId(2), mouseEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterForwardsDragAfterDownClickFallback()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-fallback-drag-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=4", "--evidence-jsonl", path],
            _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 4,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var downEvent = TerminalEvent.Mouse(
            new MouseGesture(15, 0, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var afterDown = before.ApplyInput(
            Envelope(downEvent, timestamp),
            RuntimeFrameStats.Empty);
        var dragEvent = TerminalEvent.Mouse(
            new MouseGesture(16, 0, TerminalMouseButton.Left, TerminalMouseKind.Drag),
            timestamp.AddMilliseconds(10));
        var afterDrag = afterDown.ApplyInput(
            Envelope(dragEvent, timestamp.AddMilliseconds(10)),
            RuntimeFrameStats.Empty);

        Assert.Equal(2, afterDown.CurrentScreenNumber);
        Assert.Equal(2, afterDrag.CurrentScreenNumber);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, downEvent, before, afterDown);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, dragEvent, afterDown, afterDrag);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(4, lines.Length);
        using var fallbackEvent = JsonDocument.Parse(lines[0]);
        using var downActionEvent = JsonDocument.Parse(lines[1]);
        using var hoverEvent = JsonDocument.Parse(lines[2]);
        using var dragEventJson = JsonDocument.Parse(lines[3]);
        Assert.Equal("down_click_fallback", fallbackEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("switch_screen", downActionEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("hover_change", hoverEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("drag_forward", dragEventJson.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("drag_forward", dragEventJson.RootElement.GetProperty("action").GetString());
        Assert.Equal("tab:2", dragEventJson.RootElement.GetProperty("hit_id").GetString());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsNonLeftClickTargetEvidence()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-non-left-mouse-{Guid.NewGuid():N}.jsonl");
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
            new MouseGesture(45, 7, TerminalMouseButton.Right, TerminalMouseKind.Down),
            timestamp);
        var after = before.ApplyInput(
            Envelope(terminalEvent, timestamp),
            RuntimeFrameStats.Empty);

        Assert.Equal(2, after.CurrentScreenNumber);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, terminalEvent, before, after);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var mouseEvent = JsonDocument.Parse(line);
        Assert.Equal("down_non_left_click_target", mouseEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("down_non_left_click_target", mouseEvent.RootElement.GetProperty("action").GetString());
        Assert.Equal("pane:4", mouseEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.PaneRawId(4), mouseEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
        Assert.Equal("pane", mouseEvent.RootElement.GetProperty("hit_layer").GetString());
        Assert.Equal(4, mouseEvent.RootElement.GetProperty("hit_target_screen_number").GetInt32());
        Assert.Equal("Dashboard", mouseEvent.RootElement.GetProperty("current_screen").GetString());
        Assert.Equal("none", mouseEvent.RootElement.GetProperty("target_screen").GetString());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsNonLeftUpClickTargetEvidence()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-non-left-up-mouse-{Guid.NewGuid():N}.jsonl");
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
            new MouseGesture(15, 0, TerminalMouseButton.Right, TerminalMouseKind.Up),
            timestamp);
        var after = before.ApplyInput(
            Envelope(terminalEvent, timestamp),
            RuntimeFrameStats.Empty);

        Assert.Equal(2, after.CurrentScreenNumber);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, terminalEvent, before, after);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var mouseEvent = JsonDocument.Parse(line);
        Assert.Equal("click_non_left", mouseEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("click_non_left", mouseEvent.RootElement.GetProperty("action").GetString());
        Assert.Equal("tab:2", mouseEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.TabRawId(2), mouseEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
        Assert.Equal("tab", mouseEvent.RootElement.GetProperty("hit_layer").GetString());
        Assert.Equal(2, mouseEvent.RootElement.GetProperty("hit_target_screen_number").GetInt32());
        Assert.Equal(JsonValueKind.Null, mouseEvent.RootElement.GetProperty("hit_target_category").ValueKind);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsCurrentPaneForwardEvent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-current-pane-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=45", "--evidence-jsonl", path],
            _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 45,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var terminalEvent = TerminalEvent.Mouse(
            new MouseGesture(20, 8, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var after = before.ApplyInput(
            Envelope(terminalEvent, timestamp),
            RuntimeFrameStats.Empty);

        Assert.Equal(45, after.CurrentScreenNumber);
        Assert.Equal(ShowcaseHitLayer.Unknown, ShowcaseFrameHitRegistry.HitTest(before, 0, 8).Layer);
        Assert.Equal(ShowcaseHitLayer.Unknown, ShowcaseFrameHitRegistry.HitTest(before, 20, 16).Layer);
        Assert.Equal(ShowcaseHitLayer.Pane, ShowcaseFrameHitRegistry.HitTest(before, 20, 8).Layer);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, terminalEvent, before, after);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var mouseEvent = JsonDocument.Parse(line);
        Assert.Equal("down_forward", mouseEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("pane:45", mouseEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.PaneRawId(45), mouseEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
        Assert.Equal("pane", mouseEvent.RootElement.GetProperty("hit_layer").GetString());
        Assert.Equal(45, mouseEvent.RootElement.GetProperty("hit_target_screen_number").GetInt32());
    }

    [Fact]
    public void ShowcaseFrameHitRegistryRoutesOverlayDismissal()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
            {
                HelpVisible = true
            };
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        var contentHit = ShowcaseFrameHitRegistry.HitTest(state, 20, 5);
        Assert.Equal(ShowcaseHitLayer.Overlay, contentHit.Layer);
        Assert.Equal(ShowcaseFrameHitRegistry.OverlayHelpContent, contentHit.UpstreamHitId);

        var closeHit = ShowcaseFrameHitRegistry.HitTest(state, 20, 2);
        Assert.Equal(ShowcaseHitLayer.Overlay, closeHit.Layer);
        Assert.Equal(ShowcaseFrameHitRegistry.OverlayHelpClose, closeHit.UpstreamHitId);
        Assert.Equal("overlay:help_close", closeHit.LocalHitId);

        var outsideHit = ShowcaseFrameHitRegistry.HitTest(state, 10, 5);
        Assert.NotEqual(ShowcaseHitLayer.Overlay, outsideHit.Layer);
        Assert.NotEqual(ShowcaseFrameHitRegistry.OverlayHelpContent, outsideHit.UpstreamHitId);

        var after = state.ApplyInput(
            Envelope(
                TerminalEvent.Mouse(new MouseGesture(20, 5, TerminalMouseButton.Left, TerminalMouseKind.Down), timestamp),
                timestamp),
            RuntimeFrameStats.Empty);

        Assert.False(after.HelpVisible);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsSuppressedUpAfterDownClickFallback()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-up-suppressed-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=2", "--evidence-jsonl", path],
            _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 4,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var downEvent = TerminalEvent.Mouse(
            new MouseGesture(15, 0, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var afterDown = before.ApplyInput(
            Envelope(downEvent, timestamp),
            RuntimeFrameStats.Empty);
        var upEvent = TerminalEvent.Mouse(
            new MouseGesture(15, 0, TerminalMouseButton.Left, TerminalMouseKind.Up),
            timestamp.AddMilliseconds(10));
        var afterUp = afterDown.ApplyInput(
            Envelope(upEvent, timestamp.AddMilliseconds(10)),
            RuntimeFrameStats.Empty);

        Assert.Equal(2, afterDown.CurrentScreenNumber);
        Assert.Equal(2, afterUp.CurrentScreenNumber);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, downEvent, before, afterDown);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, upEvent, afterDown, afterUp);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var fallbackEvent = JsonDocument.Parse(lines[0]);
        using var downActionEvent = JsonDocument.Parse(lines[1]);
        using var suppressedUpEvent = JsonDocument.Parse(lines[2]);
        Assert.Equal("down_click_fallback", fallbackEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("switch_screen", downActionEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("up_suppressed_after_down_click", suppressedUpEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("up_suppressed_after_down_click", suppressedUpEvent.RootElement.GetProperty("action").GetString());
        Assert.Equal("tab:2", suppressedUpEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.TabRawId(2), suppressedUpEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsOverlayScrollForwardEvent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-overlay-scroll-event-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=2", "--evidence-jsonl", path],
            _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
            {
                HelpVisible = true
            };
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var terminalEvent = TerminalEvent.Mouse(
            new MouseGesture(20, 5, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp);
        var after = before.ApplyInput(
            Envelope(terminalEvent, timestamp),
            RuntimeFrameStats.Empty);

        Assert.True(after.HelpVisible);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, terminalEvent, before, after);
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var mouseEvent = JsonDocument.Parse(line);
        Assert.Equal("scroll_forward", mouseEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("scroll_forward", mouseEvent.RootElement.GetProperty("action").GetString());
        Assert.Equal("overlay:help", mouseEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.OverlayHelpContent, mouseEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
        Assert.Equal("overlay", mouseEvent.RootElement.GetProperty("hit_layer").GetString());
        Assert.True(mouseEvent.RootElement.GetProperty("help_visible").GetBoolean());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsOverlayMouseEvent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-overlay-mouse-event-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=2", "--evidence-jsonl", path],
            _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
            {
                HelpVisible = true
            };
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var terminalEvent = TerminalEvent.Mouse(
            new MouseGesture(20, 2, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var after = before.ApplyInput(
            Envelope(terminalEvent, timestamp),
            RuntimeFrameStats.Empty);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, terminalEvent, before, after);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var fallbackEvent = JsonDocument.Parse(lines[0]);
        using var mouseEvent = JsonDocument.Parse(lines[1]);
        Assert.Equal("mouse_event", fallbackEvent.RootElement.GetProperty("event").GetString());
        Assert.Equal("down_click_fallback", fallbackEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("down_click_fallback", fallbackEvent.RootElement.GetProperty("action").GetString());
        Assert.Equal("overlay:help_close", fallbackEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.OverlayHelpClose, fallbackEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
        Assert.Equal("mouse_event", mouseEvent.RootElement.GetProperty("event").GetString());
        Assert.Equal("overlay_help_close", mouseEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("overlay_help_close", mouseEvent.RootElement.GetProperty("action").GetString());
        Assert.Equal("overlay:help_close", mouseEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.OverlayHelpClose, mouseEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
        Assert.Equal("overlay", mouseEvent.RootElement.GetProperty("hit_layer").GetString());
        Assert.False(mouseEvent.RootElement.GetProperty("help_visible").GetBoolean());
    }

    [Fact]
    public void ShowcaseFrameHitRegistryMatchesPerfAndDebugOverlayGeometry()
    {
        var perfState = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
            {
                PerfHudVisible = true
            };
        var perfHit = ShowcaseFrameHitRegistry.HitTest(perfState, 1, 1);
        Assert.Equal(ShowcaseHitLayer.Overlay, perfHit.Layer);
        Assert.Equal(ShowcaseFrameHitRegistry.OverlayPerfHud, perfHit.UpstreamHitId);
        Assert.NotEqual(ShowcaseFrameHitRegistry.OverlayPerfHud, ShowcaseFrameHitRegistry.HitTest(perfState, 49, 1).UpstreamHitId);

        var debugState = perfState with
        {
            PerfHudVisible = false,
            DebugVisible = true
        };
        var debugHit = ShowcaseFrameHitRegistry.HitTest(debugState, 31, 1);
        Assert.Equal(ShowcaseHitLayer.Overlay, debugHit.Layer);
        Assert.Equal(ShowcaseFrameHitRegistry.OverlayDebug, debugHit.UpstreamHitId);
        Assert.NotEqual(ShowcaseFrameHitRegistry.OverlayDebug, ShowcaseFrameHitRegistry.HitTest(debugState, 30, 1).UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsOverlayUnknownForMismatchedOverlayHit()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-overlay-unknown-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=2", "--evidence-jsonl", path],
            _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
            {
                HelpVisible = true,
                A11yPanelVisible = true
            };
        var after = before with { A11yPanelVisible = false };
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var terminalEvent = TerminalEvent.Mouse(
            new MouseGesture(20, 2, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, terminalEvent, before, after);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var fallbackEvent = JsonDocument.Parse(lines[0]);
        using var mouseEvent = JsonDocument.Parse(lines[1]);
        Assert.Equal("down_click_fallback", fallbackEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("overlay:help_close", fallbackEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.OverlayHelpClose, fallbackEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
        Assert.Equal("overlay_unknown", mouseEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("overlay_unknown", mouseEvent.RootElement.GetProperty("action").GetString());
        Assert.Equal("overlay:help_close", mouseEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.OverlayHelpClose, mouseEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsStatusUnknownForMismatchedStatusHit()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-status-unknown-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=2", "--evidence-jsonl", path],
            _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(90, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var after = before with { MouseCaptureEnabled = true };
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var terminalEvent = TerminalEvent.Mouse(
            new MouseGesture(27, 17, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);

        Assert.Equal(ShowcaseFrameHitRegistry.StatusHelpToggle,
            ShowcaseFrameHitRegistry.HitTest(before, 27, 17).UpstreamHitId);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, terminalEvent, before, after);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var fallbackEvent = JsonDocument.Parse(lines[0]);
        using var mouseEvent = JsonDocument.Parse(lines[1]);
        Assert.Equal("down_click_fallback", fallbackEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("status:help", fallbackEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal("status_unknown", mouseEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("status_unknown", mouseEvent.RootElement.GetProperty("action").GetString());
        Assert.Equal("status:help", mouseEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.StatusHelpToggle, mouseEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
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
    public void ShowcaseEvidenceJsonlWriterEmitsStatusToggleA11yMouseEvent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-a11y-status-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=2", "--evidence-jsonl", path],
            _ => null);
        var before = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(90, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
            {
                A11yHighContrast = true
            };
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var terminalEvent = TerminalEvent.Mouse(
            new MouseGesture(68, 17, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var after = before.ApplyInput(
            Envelope(terminalEvent, timestamp),
            RuntimeFrameStats.Empty);

        Assert.True(after.A11yPanelVisible);

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, terminalEvent, before, after);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var fallbackEvent = JsonDocument.Parse(lines[0]);
        using var mouseEvent = JsonDocument.Parse(lines[1]);
        Assert.Equal("down_click_fallback", fallbackEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("status:a11y", fallbackEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.StatusA11yToggle, fallbackEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
        Assert.Equal("status_toggle_a11y", mouseEvent.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("status_toggle_a11y", mouseEvent.RootElement.GetProperty("action").GetString());
        Assert.Equal("status:a11y", mouseEvent.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(ShowcaseFrameHitRegistry.StatusA11yToggle, mouseEvent.RootElement.GetProperty("hit_raw_id").GetUInt32());
        Assert.Equal("statustoggle", mouseEvent.RootElement.GetProperty("hit_layer").GetString());
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
            row: 17,
            start,
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(2, state.TourStartScreen);

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
    public void ShowcaseGuidedTourLandingMouseClickRequiresTourOverlayHit()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 1,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        var outsideHit = ShowcaseFrameHitRegistry.HitTest(state, 1, 2);
        Assert.NotEqual(ShowcaseFrameHitRegistry.OverlayTour, outsideHit.UpstreamHitId);
        var borderHit = ShowcaseFrameHitRegistry.HitTest(state, 20, 1);
        Assert.NotEqual(ShowcaseFrameHitRegistry.OverlayTour, borderHit.UpstreamHitId);

        state = ApplyMouse(state, column: 1, row: 2, start);
        Assert.False(state.TourActive);
        Assert.Equal(1, state.CurrentScreenNumber);

        state = ApplyMouse(state, column: 20, row: 6, start + TimeSpan.FromMilliseconds(10));
        Assert.True(state.TourActive);
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
    public void ShowcaseGuidedTourActiveOverlayHasPriorityOverChromeTabs()
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
        var chromeHit = ShowcaseFrameHitRegistry.HitTest(state, 20, 0);
        Assert.Equal(ShowcaseHitLayer.Tab, chromeHit.Layer);
        var hit = ShowcaseFrameHitRegistry.HitTest(state, 20, 2);
        Assert.Equal(ShowcaseHitLayer.Overlay, hit.Layer);
        Assert.Equal(ShowcaseFrameHitRegistry.OverlayTour, hit.UpstreamHitId);

        state = ApplyMouse(state, column: 20, row: 2, start);

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

        state = ApplyMouse(state, column: 29, row: 17, start);

        Assert.False(state.TourActive);
        Assert.Equal(1, state.CurrentScreenNumber);
        Assert.True(state.HelpVisible);
    }

    [Fact]
    public void ShowcaseChromeMouseTopTabSelectsFirstScreen()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, column: 5, row: 0, start);

        Assert.Equal(1, state.CurrentScreenNumber);
        Assert.Equal(ShowcaseScreenCategory.Tour, state.CurrentScreen.Category);
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

        state = ApplyMouse(state, column: 45, row: 0, start);

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
            column: 12,
            row: 0,
            start,
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);

        Assert.False(state.TourActive);
        Assert.False(state.TourPaused);
        Assert.Equal(3, state.CurrentScreenNumber);

        state = ApplyMouse(
            state,
            column: 12,
            row: 0,
            start + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelUp,
            TerminalMouseKind.Scroll);

        Assert.Equal(2, state.CurrentScreenNumber);
    }

    [Fact]
    public void ShowcaseChromeMouseTabWheelRequiresTabHitRegion()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        var gapHit = ShowcaseFrameHitRegistry.HitTest(state, 9, 0);
        Assert.Equal(ShowcaseHitLayer.Unknown, gapHit.Layer);

        state = ApplyMouse(
            state,
            column: 9,
            row: 0,
            start,
            TerminalMouseButton.WheelDown,
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

        Assert.Equal(4, state.CurrentScreenNumber);

        state = ApplyMouse(
            state,
            column: 45,
            row: 7,
            start + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.Left,
            TerminalMouseKind.Up);

        Assert.Equal(4, state.CurrentScreenNumber);
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

        state = ApplyMouse(state, column: 12, row: 10, start);
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
    public void ShowcaseStatusRowMouseTogglesUpstreamChromeControlsWhenUncovered()
    {
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(72, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        state = ApplyMouse(state, column: 27, row: 17, start);
        Assert.True(state.HelpVisible);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(10));
        state = ApplyMouse(state, column: 31, row: 17, start + TimeSpan.FromMilliseconds(20));
        Assert.True(state.Session.CommandPalette.IsOpen);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(30));
        state = ApplyMouse(state, column: 20, row: 17, start + TimeSpan.FromMilliseconds(40));
        Assert.False(state.A11yPanelVisible);

        state = ApplyMouse(state, column: 37, row: 17, start + TimeSpan.FromMilliseconds(60));
        Assert.True(state.PerfHudVisible);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(70));
        state = ApplyMouse(state, column: 41, row: 17, start + TimeSpan.FromMilliseconds(80));
        Assert.True(state.DebugVisible);

        state = ApplyKey(state, new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(90));
        state = ApplyMouse(state, column: 50, row: 17, start + TimeSpan.FromMilliseconds(100));
        Assert.True(state.MouseCaptureEnabled);
        Assert.False(state.EvidenceLedgerVisible);
    }

    [Fact]
    public void ShowcaseStatusRowMouseLabelReflectsInlineMode()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: true,
            viewport: new FrankenTui.Core.Size(90, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var buffer = new RenderBuffer(90, 18);
        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(90, 18), Theme.DefaultTheme));
        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Mouse: AUTO (inline:OFF)", screen);
        Assert.DoesNotContain("Mouse: AUTO (alt:OFF)", screen);

        var hit = ShowcaseFrameHitRegistry.HitTest(state, 50, 17);
        Assert.Equal(ShowcaseHitLayer.StatusToggle, hit.Layer);
        Assert.Equal(ShowcaseFrameHitRegistry.StatusMouseToggle, hit.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseStatusRowA11yHitAppearsWhenFlagsAreVisible()
    {
        var start = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new FrankenTui.Core.Size(90, 18),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
            {
                A11yHighContrast = true
            };
        var buffer = new RenderBuffer(90, 18);
        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, FrankenTui.Core.Rect.FromSize(90, 18), Theme.DefaultTheme));
        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("A11y:HC", screen);

        var hit = ShowcaseFrameHitRegistry.HitTest(state, 68, 17);
        Assert.Equal(ShowcaseHitLayer.StatusToggle, hit.Layer);
        Assert.Equal(ShowcaseFrameHitRegistry.StatusA11yToggle, hit.UpstreamHitId);

        state = ApplyMouse(state, column: 68, row: 17, start);
        Assert.True(state.A11yPanelVisible);
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

    [Fact]
    public void ShowcaseFrameHitRegistryExposesFileBrowserTreeAndPreviewRegions()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(80, 20),
            screenNumber: 9,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var tree = ShowcaseFrameHitRegistry.HitTest(state, 3, 5);
        var preview = ShowcaseFrameHitRegistry.HitTest(state, 45, 5);

        Assert.Equal(ShowcaseHitLayer.Content, tree.Layer);
        Assert.Equal("file_browser:tree:1", tree.LocalHitId);
        Assert.Equal((uint)9_001, tree.UpstreamHitId);
        Assert.Equal(ShowcaseHitLayer.Content, preview.Layer);
        Assert.Equal("file_browser:preview", preview.LocalHitId);
        Assert.Equal((uint)9_100, preview.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsFileBrowserMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-file-browser-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=9", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(80, 20),
            screenNumber: 9,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var treeEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 5, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var previewEvent = TerminalEvent.Mouse(
            new MouseGesture(45, 5, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, treeEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, previewEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var treeRecord = JsonDocument.Parse(lines[0]);
        using var previewRecord = JsonDocument.Parse(lines[1]);
        Assert.Equal("file_browser_tree_select", treeRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("file_browser:tree:1", treeRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(9_001, treeRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("file_browser_preview_scroll_down", previewRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("file_browser:preview", previewRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(9_100, previewRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseFileBrowserMouseMutatesSelectionTreeAndPreviewScroll()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(80, 20),
            screenNumber: 9,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 5, timestamp);
        Assert.Equal(0, state.FileBrowserFocusIndex);
        Assert.Equal(1, state.FileBrowserSelectedRowIndex);

        state = ApplyMouse(
            state,
            3,
            5,
            timestamp + TimeSpan.FromMilliseconds(5),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(0, state.FileBrowserFocusIndex);
        Assert.Equal(1, state.FileBrowserTreeScroll);

        state = ApplyMouse(
            state,
            45,
            5,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.FileBrowserFocusIndex);
        Assert.Equal(1, state.FileBrowserPreviewScroll);
    }

    [Fact]
    public void ShowcaseFileBrowserRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(80, 20),
            screenNumber: 9,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            FileBrowserSelectedRowIndex = 4,
            FileBrowserFocusIndex = 0,
            FileBrowserTreeScroll = 3,
            FileBrowserPreviewScroll = 2
        };
        var buffer = new RenderBuffer(80, 20);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(80, 20), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Files [row 4 scroll 3]", screen);
        Assert.Contains("Preview [scroll 2]", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesNotificationTriggerStackAndLifecycleRegions()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(80, 20),
            screenNumber: 21,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var trigger = ShowcaseFrameHitRegistry.HitTest(state, 3, 5);
        var toast = ShowcaseFrameHitRegistry.HitTest(state, 45, 5);
        var lifecycle = ShowcaseFrameHitRegistry.HitTest(state, 45, 14);

        Assert.Equal("notifications:trigger:success", trigger.LocalHitId);
        Assert.Equal((uint)21_000, trigger.UpstreamHitId);
        Assert.Equal("notifications:toast:1", toast.LocalHitId);
        Assert.Equal((uint)21_101, toast.UpstreamHitId);
        Assert.Equal("notifications:lifecycle", lifecycle.LocalHitId);
        Assert.Equal((uint)21_200, lifecycle.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsNotificationMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-notifications-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=21", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(80, 20),
            screenNumber: 21,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var triggerEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 5, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var toastEvent = TerminalEvent.Mouse(
            new MouseGesture(45, 5, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(10));
        var lifecycleEvent = TerminalEvent.Mouse(
            new MouseGesture(45, 14, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, triggerEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, toastEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, lifecycleEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var triggerRecord = JsonDocument.Parse(lines[0]);
        using var toastRecord = JsonDocument.Parse(lines[1]);
        using var lifecycleRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("notifications_trigger_success", triggerRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("notifications:trigger:success", triggerRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(21_000, triggerRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("notifications_toast_click", toastRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("notifications:toast:1", toastRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(21_101, toastRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("notifications_lifecycle_scroll_down", lifecycleRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("notifications:lifecycle", lifecycleRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(21_200, lifecycleRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseNotificationsMouseMutatesTriggerToastAndLifecycleState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(80, 20),
            screenNumber: 21,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 5, timestamp);
        Assert.Equal(0, state.NotificationsTriggerIndex);
        Assert.Equal(0, state.NotificationsFocusIndex);
        Assert.False(state.NotificationsContextArmed);

        state = ApplyMouse(state, 45, 5, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.Equal(1, state.NotificationsToastIndex);
        Assert.Equal(1, state.NotificationsFocusIndex);

        state = ApplyMouse(
            state,
            45,
            14,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.NotificationsLifecycleScroll);
        Assert.Equal(2, state.NotificationsFocusIndex);

        state = ApplyMouse(
            state,
            45,
            14,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.Right,
            TerminalMouseKind.Down);
        Assert.Equal(2, state.NotificationsFocusIndex);
        Assert.True(state.NotificationsContextArmed);
    }

    [Fact]
    public void ShowcaseNotificationsRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(80, 20),
            screenNumber: 21,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            NotificationsTriggerIndex = 4,
            NotificationsToastIndex = 2,
            NotificationsLifecycleScroll = 3,
            NotificationsFocusIndex = 2,
            NotificationsContextArmed = true
        };
        var buffer = new RenderBuffer(80, 20);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(80, 20), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Notification Demo [urgent]", screen);
        Assert.Contains("Notification Stack [toast 2]", screen);
        Assert.Contains("Toast Queue Lifecycle [ctx 3]", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesFormsInputFieldAndTextAreaRegions()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(80, 20),
            screenNumber: 7,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var field = ShowcaseFrameHitRegistry.HitTest(state, 3, 4);
        var textArea = ShowcaseFrameHitRegistry.HitTest(state, 45, 5);

        Assert.Equal(ShowcaseHitLayer.Content, field.Layer);
        Assert.Equal("forms_input:field:1", field.LocalHitId);
        Assert.Equal((uint)7_001, field.UpstreamHitId);
        Assert.Equal(ShowcaseHitLayer.Content, textArea.Layer);
        Assert.Equal("forms_input:text_area", textArea.LocalHitId);
        Assert.Equal((uint)7_100, textArea.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsFormsInputMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-forms-input-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=7", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(80, 20),
            screenNumber: 7,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var fieldEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 4, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var textEvent = TerminalEvent.Mouse(
            new MouseGesture(45, 5, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, fieldEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, textEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var fieldRecord = JsonDocument.Parse(lines[0]);
        using var textRecord = JsonDocument.Parse(lines[1]);
        Assert.Equal("forms_input_field_focus", fieldRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("forms_input:field:1", fieldRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(7_001, fieldRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("forms_input_text_scroll_down", textRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("forms_input:text_area", textRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(7_100, textRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseFormsInputMouseMutatesSelectedFieldAndTextArea()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(80, 20),
            screenNumber: 7,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 3, timestamp);
        Assert.Equal(0, state.FormsInputFocusIndex);
        Assert.Equal(0, state.FormsInputSelectedFieldIndex);

        state = ApplyMouse(state, 45, 5, timestamp + TimeSpan.FromMilliseconds(5));
        Assert.Equal(1, state.FormsInputFocusIndex);
        Assert.Equal(0, state.FormsInputSelectedFieldIndex);

        state = ApplyMouse(
            state,
            45,
            5,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.FormsInputFocusIndex);
        Assert.Equal(1, state.FormsInputTextScroll);
    }

    [Fact]
    public void ShowcaseFormsInputRendersMouseSelectedFieldAndTextArea()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(80, 20),
            screenNumber: 7,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            FormsInputSelectedFieldIndex = 2,
            FormsInputFocusIndex = 1,
            FormsInputTextScroll = 4
        };
        var buffer = new RenderBuffer(80, 20);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(80, 20), Theme.DefaultTheme));

        var rows = HeadlessBufferView.ScreenText(buffer);
        var seedRow = rows
            .Select((text, index) => (text, index))
            .Single(row => row.text.Contains("Seed:", StringComparison.Ordinal));
        var seedColumn = (ushort)seedRow.text.IndexOf("Seed:", StringComparison.Ordinal);
        var seedCell = buffer.Get(seedColumn, (ushort)seedRow.index);

        Assert.NotNull(seedCell);
        Assert.Equal("S", buffer.ResolveText(seedCell.Value));
        Assert.Equal(Theme.DefaultTheme.Selection.Foreground, seedCell.Value.Foreground);
        Assert.Equal(Theme.DefaultTheme.Selection.Background, seedCell.Value.Background);
        Assert.Equal(Theme.DefaultTheme.Selection.Flags, seedCell.Value.Attributes.Flags);
        Assert.Contains(rows, row => row.Contains("forms mouse focus=1", StringComparison.Ordinal));
        Assert.Contains(rows, row => row.Contains("ext_scroll=4", StringComparison.Ordinal));
        Assert.Contains(rows, row => row.Contains("scroll=4", StringComparison.Ordinal));
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesFormValidationPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 30),
            screenNumber: 27,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var mode = ShowcaseFrameHitRegistry.HitTest(state, 3, 2);
        var field = ShowcaseFrameHitRegistry.HitTest(state, 3, 8);
        var error = ShowcaseFrameHitRegistry.HitTest(state, 55, 5);
        var rules = ShowcaseFrameHitRegistry.HitTest(state, 55, 18);
        var controls = ShowcaseFrameHitRegistry.HitTest(state, 95, 2);
        var notifications = ShowcaseFrameHitRegistry.HitTest(state, 95, 11);
        var diagnostics = ShowcaseFrameHitRegistry.HitTest(state, 95, 19);

        Assert.Equal("form_validation:mode", mode.LocalHitId);
        Assert.Equal((uint)27_000, mode.UpstreamHitId);
        Assert.Equal("form_validation:field:2", field.LocalHitId);
        Assert.Equal((uint)27_012, field.UpstreamHitId);
        Assert.Equal("form_validation:error:1", error.LocalHitId);
        Assert.Equal((uint)27_101, error.UpstreamHitId);
        Assert.Equal("form_validation:rules", rules.LocalHitId);
        Assert.Equal((uint)27_130, rules.UpstreamHitId);
        Assert.Equal("form_validation:controls", controls.LocalHitId);
        Assert.Equal((uint)27_200, controls.UpstreamHitId);
        Assert.Equal("form_validation:notifications", notifications.LocalHitId);
        Assert.Equal((uint)27_210, notifications.UpstreamHitId);
        Assert.Equal("form_validation:diagnostics", diagnostics.LocalHitId);
        Assert.Equal((uint)27_220, diagnostics.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsFormValidationMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-form-validation-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=27", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 30),
            screenNumber: 27,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var fieldEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 8, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var errorEvent = TerminalEvent.Mouse(
            new MouseGesture(55, 5, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(10));
        var diagnosticsEvent = TerminalEvent.Mouse(
            new MouseGesture(95, 19, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, fieldEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, errorEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, diagnosticsEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var fieldRecord = JsonDocument.Parse(lines[0]);
        using var errorRecord = JsonDocument.Parse(lines[1]);
        using var diagnosticsRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("form_validation_field_focus", fieldRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("form_validation:field:2", fieldRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(27_012, fieldRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("form_validation_error_select", errorRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("form_validation:error:1", errorRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(27_101, errorRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("form_validation_diagnostics_scroll_down", diagnosticsRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("form_validation:diagnostics", diagnosticsRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(27_220, diagnosticsRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseFormValidationMouseMutatesFieldModeSubmittedAndScrollState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 30),
            screenNumber: 27,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 8, timestamp);
        Assert.Equal(2, state.FormValidationSelectedFieldIndex);
        Assert.Equal(1, state.FormValidationFocusIndex);

        state = ApplyMouse(state, 55, 5, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.Equal(1, state.FormValidationSelectedErrorIndex);
        Assert.True(state.FormValidationOnSubmitMode);
        Assert.Equal(3, state.FormValidationFocusIndex);

        state = ApplyMouse(state, 95, 2, timestamp + TimeSpan.FromMilliseconds(20));
        Assert.True(state.FormValidationSubmitted);
        Assert.Equal(5, state.FormValidationFocusIndex);

        state = ApplyMouse(
            state,
            95,
            19,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.FormValidationDiagnosticsScroll);
        Assert.Equal(7, state.FormValidationFocusIndex);
    }

    [Fact]
    public void ShowcaseFormValidationRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 30),
            screenNumber: 27,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            FormValidationSelectedFieldIndex = 2,
            FormValidationSelectedErrorIndex = 1,
            FormValidationFocusIndex = 7,
            FormValidationRulesScroll = 2,
            FormValidationDiagnosticsScroll = 3,
            FormValidationOnSubmitMode = true,
            FormValidationSubmitted = true
        };
        var buffer = new RenderBuffer(120, 30);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 30), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Mode: On Submit", screen);
        Assert.Contains("Registration Form [Password]", screen);
        Assert.Contains("Submitted: true", screen);
        Assert.Contains("Panel: 7", screen);
        Assert.Contains("Selected error: 1", screen);
        Assert.Contains("Validation Rules [scroll 2]", screen);
        Assert.Contains("Diagnostics scroll: 3", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesMacroRecorderPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 30),
            screenNumber: 13,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var controls = ShowcaseFrameHitRegistry.HitTest(state, 3, 3);
        var timeline = ShowcaseFrameHitRegistry.HitTest(state, 3, 12);
        var detail = ShowcaseFrameHitRegistry.HitTest(state, 90, 12);
        var scenario = ShowcaseFrameHitRegistry.HitTest(state, 90, 23);

        Assert.Equal("macro_recorder:controls", controls.LocalHitId);
        Assert.Equal((uint)13_000, controls.UpstreamHitId);
        Assert.Equal("macro_recorder:timeline:2", timeline.LocalHitId);
        Assert.Equal((uint)13_102, timeline.UpstreamHitId);
        Assert.Equal("macro_recorder:event_detail", detail.LocalHitId);
        Assert.Equal((uint)13_200, detail.UpstreamHitId);
        Assert.Equal("macro_recorder:scenario_runner", scenario.LocalHitId);
        Assert.Equal((uint)13_300, scenario.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsMacroRecorderMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-macro-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=13", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 30),
            screenNumber: 13,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var timelineEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 12, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var detailEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 12, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(10));
        var scenarioEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 23, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, timelineEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, detailEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, scenarioEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var timelineRecord = JsonDocument.Parse(lines[0]);
        using var detailRecord = JsonDocument.Parse(lines[1]);
        using var scenarioRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("macro_timeline_select", timelineRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("macro_recorder:timeline:2", timelineRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(13_102, timelineRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("macro_event_detail_focus", detailRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("macro_recorder:event_detail", detailRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(13_200, detailRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("macro_panel_scroll_down", scenarioRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("macro_recorder:scenario_runner", scenarioRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(13_300, scenarioRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseMacroRecorderMouseMutatesTimelineAndScenarioSelection()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 30),
            screenNumber: 13,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 12, timestamp);
        Assert.Equal(2, state.MacroRecorderTimelineIndex);
        Assert.Equal(1, state.MacroRecorderFocusIndex);
        Assert.False(state.MacroRecorderContextArmed);

        state = ApplyMouse(
            state,
            90,
            23,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.MacroRecorderScenarioIndex);
        Assert.Equal(3, state.MacroRecorderFocusIndex);

        state = ApplyMouse(
            state,
            90,
            16,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.Right,
            TerminalMouseKind.Down);
        Assert.Equal(2, state.MacroRecorderFocusIndex);
        Assert.True(state.MacroRecorderContextArmed);
    }

    [Fact]
    public void ShowcaseMacroRecorderRendersMouseSelectedTimelineAndScenario()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 30),
            screenNumber: 13,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            MacroRecorderTimelineIndex = 3,
            MacroRecorderScenarioIndex = 2,
            MacroRecorderFocusIndex = 3,
            MacroRecorderContextArmed = true
        };
        var buffer = new RenderBuffer(120, 30);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 30), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Scenario Runner [context 2]", screen);
        Assert.Contains("Selected: #003", screen);
        Assert.Contains("> Layout Lab - screens and n/p", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesLogSearchPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 20,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var result = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var stream = ShowcaseFrameHitRegistry.HitTest(state, 80, 4);
        var controls = ShowcaseFrameHitRegistry.HitTest(state, 80, 10);
        var diagnostic = ShowcaseFrameHitRegistry.HitTest(state, 80, 20);

        Assert.Equal("log_search:result:4", result.LocalHitId);
        Assert.Equal((uint)20_004, result.UpstreamHitId);
        Assert.Equal("log_search:live_stream", stream.LocalHitId);
        Assert.Equal((uint)20_100, stream.UpstreamHitId);
        Assert.Equal("log_search:controls", controls.LocalHitId);
        Assert.Equal((uint)20_200, controls.UpstreamHitId);
        Assert.Equal("log_search:diagnostic:2", diagnostic.LocalHitId);
        Assert.Equal((uint)20_302, diagnostic.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsLogSearchMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-log-search-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=20", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 20,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var resultEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var controlsEvent = TerminalEvent.Mouse(
            new MouseGesture(80, 10, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(10));
        var diagnosticEvent = TerminalEvent.Mouse(
            new MouseGesture(80, 20, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, resultEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, controlsEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, diagnosticEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var resultRecord = JsonDocument.Parse(lines[0]);
        using var controlsRecord = JsonDocument.Parse(lines[1]);
        using var diagnosticRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("log_search_result_select", resultRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("log_search:result:4", resultRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(20_004, resultRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("log_search_controls_focus", controlsRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("log_search:controls", controlsRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(20_200, controlsRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("log_search_diagnostics_scroll_down", diagnosticRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("log_search:diagnostic:2", diagnosticRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(20_302, diagnosticRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseLogSearchMouseMutatesSelectionScrollFocusAndPause()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 20,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 6, timestamp);
        Assert.Equal(0, state.LogSearchFocusIndex);
        Assert.Equal(4, state.LogSearchSelectedResultIndex);

        state = ApplyMouse(
            state,
            3,
            6,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(0, state.LogSearchFocusIndex);
        Assert.Equal(1, state.LogSearchResultScroll);

        state = ApplyMouse(state, 80, 4, timestamp + TimeSpan.FromMilliseconds(20));
        Assert.Equal(1, state.LogSearchFocusIndex);
        Assert.True(state.LogSearchPaused);

        state = ApplyMouse(
            state,
            80,
            20,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(3, state.LogSearchFocusIndex);
        Assert.Equal(1, state.LogSearchDiagnosticsScroll);

        state = ApplyMouse(state, 80, 20, timestamp + TimeSpan.FromMilliseconds(40));
        Assert.Equal(3, state.LogSearchFocusIndex);
        Assert.Equal(3, state.LogSearchSelectedDiagnosticIndex);
    }

    [Fact]
    public void ShowcaseLogSearchRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(140, 34),
            screenNumber: 20,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            LogSearchFocusIndex = 3,
            LogSearchSelectedResultIndex = 4,
            LogSearchSelectedDiagnosticIndex = 2,
            LogSearchResultScroll = 5,
            LogSearchDiagnosticsScroll = 6,
            LogSearchPaused = true
        };
        var buffer = new RenderBuffer(180, 36);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(180, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Paused: true", screen);
        Assert.Contains("result_scroll=5", screen);
        Assert.Contains("diagnostics_scroll=6", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesAdvancedTextEditorPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 25,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var editorLine = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var search = ShowcaseFrameHitRegistry.HitTest(state, 70, 4);
        var history = ShowcaseFrameHitRegistry.HitTest(state, 70, 13);
        var diagnostic = ShowcaseFrameHitRegistry.HitTest(state, 70, 24);

        Assert.Equal("advanced_text_editor:line:4", editorLine.LocalHitId);
        Assert.Equal((uint)25_004, editorLine.UpstreamHitId);
        Assert.Equal("advanced_text_editor:search", search.LocalHitId);
        Assert.Equal((uint)25_100, search.UpstreamHitId);
        Assert.Equal("advanced_text_editor:history:1", history.LocalHitId);
        Assert.Equal((uint)25_201, history.UpstreamHitId);
        Assert.Equal("advanced_text_editor:diagnostic:1", diagnostic.LocalHitId);
        Assert.Equal((uint)25_301, diagnostic.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsAdvancedTextEditorMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-advanced-text-editor-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=25", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 25,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var editorEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var searchEvent = TerminalEvent.Mouse(
            new MouseGesture(70, 4, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(10));
        var diagnosticsEvent = TerminalEvent.Mouse(
            new MouseGesture(70, 24, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, editorEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, searchEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, diagnosticsEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var editorRecord = JsonDocument.Parse(lines[0]);
        using var searchRecord = JsonDocument.Parse(lines[1]);
        using var diagnosticsRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("advanced_text_editor_line_select", editorRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("advanced_text_editor:line:4", editorRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(25_004, editorRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("advanced_text_editor_search_focus", searchRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("advanced_text_editor:search", searchRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(25_100, searchRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("advanced_text_editor_diagnostics_scroll_down", diagnosticsRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("advanced_text_editor:diagnostic:1", diagnosticsRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(25_301, diagnosticsRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseAdvancedTextEditorMouseMutatesFocusCursorHistoryAndDiagnostics()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 25,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 6, timestamp);
        Assert.Equal(4, state.AdvancedTextEditorCursorLine);
        Assert.Equal(0, state.AdvancedTextEditorFocusIndex);

        state = ApplyMouse(state, 70, 4, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.Equal(1, state.AdvancedTextEditorFocusIndex);

        state = ApplyMouse(state, 70, 13, timestamp + TimeSpan.FromMilliseconds(20));
        Assert.Equal(1, state.AdvancedTextEditorHistoryIndex);
        Assert.Equal(2, state.AdvancedTextEditorFocusIndex);

        state = ApplyMouse(
            state,
            70,
            24,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.AdvancedTextEditorDiagnosticsIndex);
        Assert.Equal(3, state.AdvancedTextEditorFocusIndex);
    }

    [Fact]
    public void ShowcaseAdvancedTextEditorRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 25,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            AdvancedTextEditorCursorLine = 4,
            AdvancedTextEditorFocusIndex = 3,
            AdvancedTextEditorHistoryIndex = 2,
            AdvancedTextEditorDiagnosticsIndex = 7
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Ln 5, Col 26 | Focus: diagnostics", screen);
        Assert.Contains("Focus: diagnostics | Cursor line: 5", screen);
        Assert.Contains("Selected history row: 2", screen);
        Assert.Contains("Diagnostics [row 7]", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesVirtualizedSearchPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 28,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var search = ShowcaseFrameHitRegistry.HitTest(state, 3, 3);
        var result = ShowcaseFrameHitRegistry.HitTest(state, 3, 8);
        var stats = ShowcaseFrameHitRegistry.HitTest(state, 90, 8);
        var diagnostic = ShowcaseFrameHitRegistry.HitTest(state, 90, 20);

        Assert.Equal("virtualized_search:search_bar", search.LocalHitId);
        Assert.Equal((uint)28_000, search.UpstreamHitId);
        Assert.Equal("virtualized_search:result:3", result.LocalHitId);
        Assert.Equal((uint)28_103, result.UpstreamHitId);
        Assert.Equal("virtualized_search:stats", stats.LocalHitId);
        Assert.Equal((uint)28_300, stats.UpstreamHitId);
        Assert.Equal("virtualized_search:diagnostic:3", diagnostic.LocalHitId);
        Assert.Equal((uint)28_403, diagnostic.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsVirtualizedSearchMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-virtualized-search-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=28", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 28,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var searchEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 3, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var resultEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 8, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(10));
        var diagnosticsEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 20, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, searchEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, resultEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, diagnosticsEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var searchRecord = JsonDocument.Parse(lines[0]);
        using var resultRecord = JsonDocument.Parse(lines[1]);
        using var diagnosticsRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("virtualized_search_focus_search", searchRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("virtualized_search:search_bar", searchRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(28_000, searchRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("virtualized_search_result_select", resultRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("virtualized_search:result:3", resultRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(28_103, resultRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("virtualized_search_diagnostics_scroll_down", diagnosticsRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("virtualized_search:diagnostic:3", diagnosticsRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(28_403, diagnosticsRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseVirtualizedSearchMouseMutatesFocusSelectionAndDiagnostics()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 28,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 3, timestamp);
        Assert.True(state.VirtualizedSearchFocusSearch);

        state = ApplyMouse(state, 3, 8, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.Equal(3, state.VirtualizedSearchSelectedIndex);
        Assert.False(state.VirtualizedSearchFocusSearch);

        state = ApplyMouse(
            state,
            3,
            8,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(6, state.VirtualizedSearchSelectedIndex);

        state = ApplyMouse(
            state,
            90,
            20,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.VirtualizedSearchDiagnosticsScroll);
        Assert.Equal(1, state.VirtualizedSearchDiagnosticIndex);
        Assert.True(state.VirtualizedSearchStatsFocused);
    }

    [Fact]
    public void ShowcaseVirtualizedSearchRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 28,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            VirtualizedSearchSelectedIndex = 6,
            VirtualizedSearchDiagnosticIndex = 4,
            VirtualizedSearchDiagnosticsScroll = 4,
            VirtualizedSearchFocusSearch = false,
            VirtualizedSearchStatsFocused = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Selected: 7", screen);
        Assert.Contains("Focus:    Stats", screen);
        Assert.Contains("Selected diagnostic: 4", screen);
        Assert.Contains("Diagnostics scroll: 4", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesAsyncTaskPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 29,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var scheduler = ShowcaseFrameHitRegistry.HitTest(state, 3, 3);
        var task = ShowcaseFrameHitRegistry.HitTest(state, 3, 9);
        var details = ShowcaseFrameHitRegistry.HitTest(state, 90, 7);
        var activity = ShowcaseFrameHitRegistry.HitTest(state, 90, 14);
        var evidence = ShowcaseFrameHitRegistry.HitTest(state, 90, 20);
        var hazard = ShowcaseFrameHitRegistry.HitTest(state, 90, 24);
        var footer = ShowcaseFrameHitRegistry.HitTest(state, 3, 29);

        Assert.Equal("async_tasks:scheduler", scheduler.LocalHitId);
        Assert.Equal((uint)29_000, scheduler.UpstreamHitId);
        Assert.Equal("async_tasks:task:3", task.LocalHitId);
        Assert.Equal((uint)29_103, task.UpstreamHitId);
        Assert.Equal("async_tasks:details", details.LocalHitId);
        Assert.Equal((uint)29_200, details.UpstreamHitId);
        Assert.Equal("async_tasks:activity", activity.LocalHitId);
        Assert.Equal((uint)29_210, activity.UpstreamHitId);
        Assert.Equal("async_tasks:evidence", evidence.LocalHitId);
        Assert.Equal((uint)29_220, evidence.UpstreamHitId);
        Assert.Equal("async_tasks:hazard", hazard.LocalHitId);
        Assert.Equal((uint)29_230, hazard.UpstreamHitId);
        Assert.Equal("async_tasks:footer", footer.LocalHitId);
        Assert.Equal((uint)29_300, footer.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsAsyncTaskMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-async-tasks-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=29", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 29,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var taskEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 9, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var evidenceEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 20, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(10));
        var hazardEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 24, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, taskEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, evidenceEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, hazardEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var taskRecord = JsonDocument.Parse(lines[0]);
        using var evidenceRecord = JsonDocument.Parse(lines[1]);
        using var hazardRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("async_tasks_task_select", taskRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("async_tasks:task:3", taskRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(29_103, taskRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("async_tasks_evidence_focus", evidenceRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("async_tasks:evidence", evidenceRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(29_220, evidenceRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("async_tasks_hazard_scroll_down", hazardRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("async_tasks:hazard", hazardRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(29_230, hazardRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseAsyncTasksMouseMutatesSelectionFocusPolicyAndHazard()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 29,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 9, timestamp);
        Assert.Equal(3, state.AsyncTasksSelectedIndex);
        Assert.Equal(1, state.AsyncTasksFocusedPanelIndex);

        state = ApplyMouse(state, 3, 3, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.Equal(3, state.AsyncTasksPolicyIndex);
        Assert.Equal(0, state.AsyncTasksFocusedPanelIndex);

        state = ApplyMouse(
            state,
            90,
            24,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.AsyncTasksHazardScroll);
        Assert.Equal(4, state.AsyncTasksFocusedPanelIndex);

        state = ApplyMouse(state, 3, 29, timestamp + TimeSpan.FromMilliseconds(30));
        Assert.False(state.AsyncTasksAgingEnabled);
        Assert.Equal(6, state.AsyncTasksFocusedPanelIndex);
    }

    [Fact]
    public void ShowcaseAsyncTasksRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 29,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            AsyncTasksSelectedIndex = 3,
            AsyncTasksFocusedPanelIndex = 5,
            AsyncTasksHazardScroll = 2,
            AsyncTasksPolicyIndex = 3,
            AsyncTasksAgingEnabled = false
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Smith[Weighted SJF (w/p)]", screen);
        Assert.Contains("Aging:off", screen);
        Assert.Contains("ID: 4", screen);
        Assert.Contains("Name: Async Build #1", screen);
        Assert.Contains("Policy + Evidence [focus]", screen);
        Assert.Contains("Hazard scroll: 2", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesThemeStudioPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 30,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var preset = ShowcaseFrameHitRegistry.HitTest(state, 3, 5);
        var token = ShowcaseFrameHitRegistry.HitTest(state, 40, 8);
        var export = ShowcaseFrameHitRegistry.HitTest(state, 40, 17);
        var diagnostics = ShowcaseFrameHitRegistry.HitTest(state, 40, 24);
        var footer = ShowcaseFrameHitRegistry.HitTest(state, 3, 29);

        Assert.Equal("theme_studio:preset:3", preset.LocalHitId);
        Assert.Equal((uint)30_003, preset.UpstreamHitId);
        Assert.Equal("theme_studio:token:6", token.LocalHitId);
        Assert.Equal((uint)30_106, token.UpstreamHitId);
        Assert.Equal("theme_studio:export", export.LocalHitId);
        Assert.Equal((uint)30_300, export.UpstreamHitId);
        Assert.Equal("theme_studio:diagnostics", diagnostics.LocalHitId);
        Assert.Equal((uint)30_310, diagnostics.UpstreamHitId);
        Assert.Equal("theme_studio:footer", footer.LocalHitId);
        Assert.Equal((uint)30_400, footer.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsThemeStudioMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-theme-studio-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=30", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 30,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var presetEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 5, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var tokenEvent = TerminalEvent.Mouse(
            new MouseGesture(40, 8, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(10));
        var diagnosticsEvent = TerminalEvent.Mouse(
            new MouseGesture(40, 24, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, presetEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, tokenEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, diagnosticsEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var presetRecord = JsonDocument.Parse(lines[0]);
        using var tokenRecord = JsonDocument.Parse(lines[1]);
        using var diagnosticsRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("theme_studio_preset_select", presetRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("theme_studio:preset:3", presetRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(30_003, presetRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("theme_studio_token_select", tokenRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("theme_studio:token:6", tokenRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(30_106, tokenRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("theme_studio_diagnostics_scroll_down", diagnosticsRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("theme_studio:diagnostics", diagnosticsRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(30_310, diagnosticsRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseThemeStudioMouseMutatesPresetTokenDiagnosticsAndExport()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 30,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 5, timestamp);
        Assert.Equal(3, state.ThemeStudioPresetIndex);
        Assert.Equal(0, state.ThemeStudioFocusIndex);

        state = ApplyMouse(state, 40, 8, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.Equal(6, state.ThemeStudioTokenIndex);
        Assert.Equal(1, state.ThemeStudioFocusIndex);

        state = ApplyMouse(
            state,
            40,
            24,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.ThemeStudioDiagnosticsScroll);
        Assert.Equal(3, state.ThemeStudioFocusIndex);

        state = ApplyMouse(state, 40, 17, timestamp + TimeSpan.FromMilliseconds(30));
        Assert.True(state.ThemeStudioExportArmed);
        Assert.Equal(2, state.ThemeStudioFocusIndex);
    }

    [Fact]
    public void ShowcaseThemeStudioRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 30,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            ThemeStudioPresetIndex = 3,
            ThemeStudioTokenIndex = 24,
            ThemeStudioFocusIndex = 1,
            ThemeStudioDiagnosticsScroll = 4,
            ThemeStudioExportArmed = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Token Inspector [focus PriorityP4]", screen);
        Assert.Contains("Status: Export ready for Nord", screen);
        Assert.Contains("Diagnostics scroll: 4", screen);
        Assert.Contains("selected=Nord token=PriorityP4 export=ready", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesSnapshotPlayerPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 31,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var timeline = ShowcaseFrameHitRegistry.HitTest(state, 3, 3);
        var preview = ShowcaseFrameHitRegistry.HitTest(state, 3, 8);
        var compare = ShowcaseFrameHitRegistry.HitTest(state, 40, 8);
        var frameInfo = ShowcaseFrameHitRegistry.HitTest(state, 90, 6);
        var controls = ShowcaseFrameHitRegistry.HitTest(state, 90, 16);
        var diagnostics = ShowcaseFrameHitRegistry.HitTest(state, 90, 24);

        Assert.Equal("snapshot_player:timeline", timeline.LocalHitId);
        Assert.Equal((uint)31_000, timeline.UpstreamHitId);
        Assert.Equal("snapshot_player:preview", preview.LocalHitId);
        Assert.Equal((uint)31_100, preview.UpstreamHitId);
        Assert.Equal("snapshot_player:compare", compare.LocalHitId);
        Assert.Equal((uint)31_110, compare.UpstreamHitId);
        Assert.Equal("snapshot_player:frame_info", frameInfo.LocalHitId);
        Assert.Equal((uint)31_200, frameInfo.UpstreamHitId);
        Assert.Equal("snapshot_player:controls", controls.LocalHitId);
        Assert.Equal((uint)31_210, controls.UpstreamHitId);
        Assert.Equal("snapshot_player:diagnostics", diagnostics.LocalHitId);
        Assert.Equal((uint)31_220, diagnostics.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsSnapshotPlayerMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-snapshot-player-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=31", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 31,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var timelineEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 3, TerminalMouseButton.Right, TerminalMouseKind.Down),
            timestamp);
        var previewEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 8, TerminalMouseButton.Right, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(10));
        var diagnosticsEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 24, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, timelineEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, previewEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, diagnosticsEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var timelineRecord = JsonDocument.Parse(lines[0]);
        using var previewRecord = JsonDocument.Parse(lines[1]);
        using var diagnosticsRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("snapshot_player_marker_toggle", timelineRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("snapshot_player:timeline", timelineRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(31_000, timelineRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("snapshot_player_heatmap_toggle", previewRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("snapshot_player:preview", previewRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(31_100, previewRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("snapshot_player_diagnostics_scroll_down", diagnosticsRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("snapshot_player:diagnostics", diagnosticsRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(31_220, diagnosticsRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseSnapshotPlayerMouseMutatesTimelinePreviewControlsAndDiagnostics()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 31,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 60, 3, timestamp);
        Assert.Equal(42, state.SnapshotPlayerFrameIndex);
        Assert.Equal(0, state.SnapshotPlayerFocusIndex);
        Assert.True(state.SnapshotPlayerTimelineScrubbing);

        state = ApplyMouse(
            state,
            200,
            3,
            timestamp + TimeSpan.FromMilliseconds(5),
            TerminalMouseButton.Left,
            TerminalMouseKind.Drag);
        Assert.Equal(49, state.SnapshotPlayerFrameIndex);
        Assert.Equal(0, state.SnapshotPlayerFocusIndex);
        Assert.True(state.SnapshotPlayerTimelineScrubbing);

        state = ApplyMouse(
            state,
            1,
            3,
            timestamp + TimeSpan.FromMilliseconds(8),
            TerminalMouseButton.Left,
            TerminalMouseKind.Up);
        Assert.Equal(0, state.SnapshotPlayerFrameIndex);
        Assert.False(state.SnapshotPlayerTimelineScrubbing);

        state = ApplyMouse(
            state,
            3,
            8,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.Right);
        Assert.False(state.SnapshotPlayerHeatmapEnabled);
        Assert.Equal(1, state.SnapshotPlayerFocusIndex);

        state = ApplyMouse(state, 40, 8, timestamp + TimeSpan.FromMilliseconds(20));
        Assert.Equal(1, state.SnapshotPlayerCompareIndex);
        Assert.Equal(2, state.SnapshotPlayerFocusIndex);

        state = ApplyMouse(state, 90, 16, timestamp + TimeSpan.FromMilliseconds(30));
        Assert.True(state.SnapshotPlayerPlaying);
        Assert.Equal(4, state.SnapshotPlayerFocusIndex);

        state = ApplyMouse(
            state,
            90,
            24,
            timestamp + TimeSpan.FromMilliseconds(40),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.SnapshotPlayerDiagnosticsScroll);
        Assert.Equal(5, state.SnapshotPlayerFocusIndex);
    }

    [Fact]
    public void ShowcaseSnapshotPlayerRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 31,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            SnapshotPlayerFrameIndex = 12,
            SnapshotPlayerFocusIndex = 2,
            SnapshotPlayerDiagnosticsScroll = 4,
            SnapshotPlayerCompareIndex = 1,
            SnapshotPlayerMarkerEnabled = true,
            SnapshotPlayerHeatmapEnabled = false,
            SnapshotPlayerPlaying = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Timeline (13/50)", screen);
        Assert.Contains("Frame A/B Compare [focus slot B]", screen);
        Assert.Contains("Status: Playing", screen);
        Assert.Contains("Heatmap: Off", screen);
        Assert.Contains("Diagnostics scroll: 4", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesPerformanceChallengePanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 32,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var header = ShowcaseFrameHitRegistry.HitTest(state, 3, 2);
        var metrics = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var sparkline = ShowcaseFrameHitRegistry.HitTest(state, 45, 6);
        var evidence = ShowcaseFrameHitRegistry.HitTest(state, 45, 20);
        var budget = ShowcaseFrameHitRegistry.HitTest(state, 90, 6);
        var stress = ShowcaseFrameHitRegistry.HitTest(state, 90, 16);
        var tier = ShowcaseFrameHitRegistry.HitTest(state, 90, 23);
        var footer = ShowcaseFrameHitRegistry.HitTest(state, 3, 29);

        Assert.Equal("performance_challenge:header", header.LocalHitId);
        Assert.Equal((uint)32_000, header.UpstreamHitId);
        Assert.Equal("performance_challenge:metrics", metrics.LocalHitId);
        Assert.Equal((uint)32_100, metrics.UpstreamHitId);
        Assert.Equal("performance_challenge:sparkline", sparkline.LocalHitId);
        Assert.Equal((uint)32_200, sparkline.UpstreamHitId);
        Assert.Equal("performance_challenge:evidence", evidence.LocalHitId);
        Assert.Equal((uint)32_210, evidence.UpstreamHitId);
        Assert.Equal("performance_challenge:budget", budget.LocalHitId);
        Assert.Equal((uint)32_300, budget.UpstreamHitId);
        Assert.Equal("performance_challenge:stress", stress.LocalHitId);
        Assert.Equal((uint)32_310, stress.UpstreamHitId);
        Assert.Equal("performance_challenge:tier:3", tier.LocalHitId);
        Assert.Equal((uint)32_403, tier.UpstreamHitId);
        Assert.Equal("performance_challenge:footer", footer.LocalHitId);
        Assert.Equal((uint)32_500, footer.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsPerformanceChallengeMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-performance-challenge-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=32", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 32,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var stressEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 16, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var tierEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 23, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(10));
        var budgetEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 6, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, stressEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, tierEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, budgetEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var stressRecord = JsonDocument.Parse(lines[0]);
        using var tierRecord = JsonDocument.Parse(lines[1]);
        using var budgetRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("performance_challenge_stress_toggle", stressRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("performance_challenge:stress", stressRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(32_310, stressRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("performance_challenge_tier_select", tierRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("performance_challenge:tier:3", tierRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(32_403, tierRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("performance_challenge_budget_scroll_down", budgetRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("performance_challenge:budget", budgetRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(32_300, budgetRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcasePerformanceChallengeMouseMutatesStressTierBudgetAndEvidence()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 32,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 90, 16, timestamp);
        Assert.Equal(2, state.PerformanceChallengeStressModeIndex);
        Assert.Equal(25, state.PerformanceChallengeStressLoad);
        Assert.Equal(5, state.PerformanceChallengeFocusIndex);

        state = ApplyMouse(state, 90, 23, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.Equal(3, state.PerformanceChallengeForcedTierIndex);
        Assert.Equal(6, state.PerformanceChallengeFocusIndex);

        state = ApplyMouse(
            state,
            90,
            6,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(16, state.PerformanceChallengeBudgetMs);
        Assert.Equal(4, state.PerformanceChallengeFocusIndex);

        state = ApplyMouse(
            state,
            45,
            6,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.PerformanceChallengeSparklineModeIndex);
        Assert.Equal(2, state.PerformanceChallengeFocusIndex);

        state = ApplyMouse(
            state,
            45,
            20,
            timestamp + TimeSpan.FromMilliseconds(40),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.PerformanceChallengeEvidenceScroll);
        Assert.Equal(3, state.PerformanceChallengeFocusIndex);
    }

    [Fact]
    public void ShowcasePerformanceChallengeRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 32,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            PerformanceChallengeFocusIndex = 6,
            PerformanceChallengeForcedTierIndex = 3,
            PerformanceChallengeStressModeIndex = 2,
            PerformanceChallengeStressLoad = 50,
            PerformanceChallengeBudgetMs = 24,
            PerformanceChallengeSparklineModeIndex = 1,
            PerformanceChallengeEvidenceScroll = 4,
            PerformanceChallengePaused = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("PERFORMANCE CHALLENGE MODE - PAUSED", screen);
        Assert.Contains("Mode: fps", screen);
        Assert.Contains("Budget: 24.00ms", screen);
        Assert.Contains("Mode: Peak | Load 50%", screen);
        Assert.Contains("Degradation Tiers [forced SAFETY MODE]", screen);
        Assert.Contains("Evidence scroll: 4", screen);
        Assert.Contains("budget:24ms", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesExplainabilityPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 33,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var header = ShowcaseFrameHitRegistry.HitTest(state, 3, 2);
        var diff = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var resize = ShowcaseFrameHitRegistry.HitTest(state, 45, 6);
        var budget = ShowcaseFrameHitRegistry.HitTest(state, 90, 6);
        var timeline = ShowcaseFrameHitRegistry.HitTest(state, 3, 18);
        var source = ShowcaseFrameHitRegistry.HitTest(state, 3, 25);

        Assert.Equal("explainability:header", header.LocalHitId);
        Assert.Equal((uint)33_000, header.UpstreamHitId);
        Assert.Equal("explainability:diff_strategy", diff.LocalHitId);
        Assert.Equal((uint)33_100, diff.UpstreamHitId);
        Assert.Equal("explainability:resize_regime", resize.LocalHitId);
        Assert.Equal((uint)33_110, resize.UpstreamHitId);
        Assert.Equal("explainability:budget_decisions", budget.LocalHitId);
        Assert.Equal((uint)33_120, budget.UpstreamHitId);
        Assert.Equal("explainability:timeline", timeline.LocalHitId);
        Assert.Equal((uint)33_200, timeline.UpstreamHitId);
        Assert.Equal("explainability:source_controls", source.LocalHitId);
        Assert.Equal((uint)33_300, source.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsExplainabilityMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-explainability-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=33", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 33,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var diffEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var budgetEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(10));
        var timelineEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 18, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, diffEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, budgetEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, timelineEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var diffRecord = JsonDocument.Parse(lines[0]);
        using var budgetRecord = JsonDocument.Parse(lines[1]);
        using var timelineRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("explainability_diff_focus", diffRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("explainability:diff_strategy", diffRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(33_100, diffRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("explainability_budget_focus", budgetRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("explainability:budget_decisions", budgetRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(33_120, budgetRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("explainability_timeline_scroll_down", timelineRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("explainability:timeline", timelineRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(33_200, timelineRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseExplainabilityMouseMutatesFocusTimelineSourceAndPause()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 33,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 2, timestamp);
        Assert.True(state.ExplainabilityPaused);
        Assert.False(state.ExplainabilityAutoRefresh);
        Assert.Equal(0, state.ExplainabilityFocusIndex);

        state = ApplyMouse(state, 90, 6, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.Equal(3, state.ExplainabilityFocusIndex);

        state = ApplyMouse(
            state,
            3,
            18,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.ExplainabilityTimelineScroll);
        Assert.Equal(4, state.ExplainabilityFocusIndex);

        state = ApplyMouse(
            state,
            3,
            25,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.ExplainabilitySourceScroll);
        Assert.Equal(5, state.ExplainabilityFocusIndex);

        state = ApplyMouse(state, 3, 25, timestamp + TimeSpan.FromMilliseconds(40));
        Assert.True(state.ExplainabilityOverlayMode);
        Assert.Equal(5, state.ExplainabilityFocusIndex);
    }

    [Fact]
    public void ShowcaseExplainabilityRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 33,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            ExplainabilityFocusIndex = 5,
            ExplainabilityTimelineScroll = 4,
            ExplainabilitySourceScroll = 2,
            ExplainabilityPaused = true,
            ExplainabilityOverlayMode = true,
            ExplainabilityAutoRefresh = false
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("state=paused", screen);
        Assert.Contains("Timeline scroll: 4", screen);
        Assert.Contains("Source + Controls [scroll 2]", screen);
        Assert.Contains("State: paused | Auto-refresh: off | Overlay: on", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesI18nPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 34,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var locale = ShowcaseFrameHitRegistry.HitTest(state, 3, 3);
        var lookup = ShowcaseFrameHitRegistry.HitTest(state, 3, 8);
        var plurals = ShowcaseFrameHitRegistry.HitTest(state, 3, 20);
        var rtl = ShowcaseFrameHitRegistry.HitTest(state, 70, 8);
        var stress = ShowcaseFrameHitRegistry.HitTest(state, 70, 20);
        var footer = ShowcaseFrameHitRegistry.HitTest(state, 3, 29);

        Assert.Equal("i18n:locale_bar", locale.LocalHitId);
        Assert.Equal((uint)34_000, locale.UpstreamHitId);
        Assert.Equal("i18n:string_lookup", lookup.LocalHitId);
        Assert.Equal((uint)34_100, lookup.UpstreamHitId);
        Assert.Equal("i18n:plural_rules", plurals.LocalHitId);
        Assert.Equal((uint)34_110, plurals.UpstreamHitId);
        Assert.Equal("i18n:rtl_layout", rtl.LocalHitId);
        Assert.Equal((uint)34_120, rtl.UpstreamHitId);
        Assert.Equal("i18n:stress_lab", stress.LocalHitId);
        Assert.Equal((uint)34_130, stress.UpstreamHitId);
        Assert.Equal("i18n:footer", footer.LocalHitId);
        Assert.Equal((uint)34_200, footer.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsI18nMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-i18n-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=34", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 34,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var localeEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 3, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var pluralEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 20, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));
        var stressEvent = TerminalEvent.Mouse(
            new MouseGesture(70, 20, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, localeEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, pluralEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, stressEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var localeRecord = JsonDocument.Parse(lines[0]);
        using var pluralRecord = JsonDocument.Parse(lines[1]);
        using var stressRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("i18n_locale_select", localeRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("i18n:locale_bar", localeRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(34_000, localeRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("i18n_plural_count_increment", pluralRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("i18n:plural_rules", pluralRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(34_110, pluralRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("i18n_stress_lab_focus", stressRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("i18n:stress_lab", stressRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(34_130, stressRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseI18nMouseMutatesLocalePluralRtlStressAndExport()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 34,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 90, 3, timestamp);
        Assert.Equal(4, state.I18nLocaleIndex);
        Assert.True(state.I18nRtlEnabled);
        Assert.Equal(0, state.I18nFocusIndex);

        state = ApplyMouse(
            state,
            3,
            20,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(2, state.I18nPluralCount);
        Assert.Equal(2, state.I18nFocusIndex);

        state = ApplyMouse(state, 70, 8, timestamp + TimeSpan.FromMilliseconds(20));
        Assert.False(state.I18nRtlEnabled);
        Assert.Equal(3, state.I18nFocusIndex);

        state = ApplyMouse(state, 70, 20, timestamp + TimeSpan.FromMilliseconds(30));
        Assert.Equal(1, state.I18nStressSampleIndex);
        Assert.Equal(4, state.I18nFocusIndex);

        state = ApplyMouse(state, 3, 29, timestamp + TimeSpan.FromMilliseconds(40));
        Assert.True(state.I18nExportArmed);
        Assert.Equal(5, state.I18nFocusIndex);
    }

    [Fact]
    public void ShowcaseI18nRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 34,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            I18nLocaleIndex = 4,
            I18nFocusIndex = 4,
            I18nPluralCount = 5,
            I18nStressSampleIndex = 2,
            I18nRtlEnabled = true,
            I18nExportArmed = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("locale=ar", screen);
        Assert.Contains("[العربية]", screen);
        Assert.Contains("Locale: ar (العربية)", screen);
        Assert.Contains("count = 5", screen);
        Assert.Contains("Flow: Rtl", screen);
        Assert.Contains("Stress Lab [focus RTL Text]", screen);
        Assert.Contains("export=ready", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesVoiOverlayPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 35,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var header = ShowcaseFrameHitRegistry.HitTest(state, 3, 2);
        var decision = ShowcaseFrameHitRegistry.HitTest(state, 3, 5);
        var posterior = ShowcaseFrameHitRegistry.HitTest(state, 3, 20);
        var observation = ShowcaseFrameHitRegistry.HitTest(state, 45, 5);
        var ledger = ShowcaseFrameHitRegistry.HitTest(state, 45, 20);
        var controls = ShowcaseFrameHitRegistry.HitTest(state, 90, 5);
        var footer = ShowcaseFrameHitRegistry.HitTest(state, 3, 29);

        Assert.Equal("voi_overlay:header", header.LocalHitId);
        Assert.Equal((uint)35_000, header.UpstreamHitId);
        Assert.Equal("voi_overlay:decision", decision.LocalHitId);
        Assert.Equal((uint)35_100, decision.UpstreamHitId);
        Assert.Equal("voi_overlay:posterior", posterior.LocalHitId);
        Assert.Equal((uint)35_110, posterior.UpstreamHitId);
        Assert.Equal("voi_overlay:observation", observation.LocalHitId);
        Assert.Equal((uint)35_120, observation.UpstreamHitId);
        Assert.Equal("voi_overlay:ledger", ledger.LocalHitId);
        Assert.Equal((uint)35_130, ledger.UpstreamHitId);
        Assert.Equal("voi_overlay:controls", controls.LocalHitId);
        Assert.Equal((uint)35_200, controls.UpstreamHitId);
        Assert.Equal("voi_overlay:footer", footer.LocalHitId);
        Assert.Equal((uint)35_300, footer.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsVoiOverlayMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-voi-overlay-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=35", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 35,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var decisionEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 5, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var ledgerEvent = TerminalEvent.Mouse(
            new MouseGesture(45, 20, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));
        var controlsEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 5, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, decisionEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, ledgerEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, controlsEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var decisionRecord = JsonDocument.Parse(lines[0]);
        using var ledgerRecord = JsonDocument.Parse(lines[1]);
        using var controlsRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("voi_overlay_decision_focus", decisionRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("voi_overlay:decision", decisionRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(35_100, decisionRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("voi_overlay_ledger_scroll_down", ledgerRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("voi_overlay:ledger", ledgerRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(35_130, ledgerRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("voi_overlay_controls_focus", controlsRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("voi_overlay:controls", controlsRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(35_200, controlsRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseVoiOverlayMouseMutatesFocusLedgerControlsAndReset()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 35,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 5, timestamp);
        Assert.Equal(1, state.VoiOverlayFocusIndex);

        state = ApplyMouse(
            state,
            45,
            20,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.VoiOverlayLedgerIndex);
        Assert.Equal(4, state.VoiOverlayFocusIndex);

        state = ApplyMouse(state, 90, 5, timestamp + TimeSpan.FromMilliseconds(20));
        Assert.True(state.VoiOverlayDetailExpanded);
        Assert.Equal(5, state.VoiOverlayFocusIndex);

        state = ApplyMouse(
            state,
            90,
            5,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.VoiOverlayControlsScroll);
        Assert.Equal(5, state.VoiOverlayFocusIndex);

        state = ApplyMouse(state, 3, 29, timestamp + TimeSpan.FromMilliseconds(40));
        Assert.Equal(1, state.VoiOverlayResetCount);
        Assert.Equal(0, state.VoiOverlayLedgerIndex);
        Assert.Equal(6, state.VoiOverlayFocusIndex);
    }

    [Fact]
    public void ShowcaseVoiOverlayRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 35,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            VoiOverlayFocusIndex = 4,
            VoiOverlayLedgerIndex = 2,
            VoiOverlayControlsScroll = 3,
            VoiOverlayResetCount = 2,
            VoiOverlayDetailExpanded = true,
            VoiOverlayVisible = false
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("visible=false", screen);
        Assert.Contains("VOI Ledger [selected 2]", screen);
        Assert.Contains("> Decision", screen);
        Assert.Contains("detail=expanded", screen);
        Assert.Contains("selected_ledger_idx=2", screen);
        Assert.Contains("r reset(2)", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesInlineModeStoryPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 36,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var header = ShowcaseFrameHitRegistry.HitTest(state, 3, 2);
        var inlineStory = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var altStory = ShowcaseFrameHitRegistry.HitTest(state, 45, 6);
        var controls = ShowcaseFrameHitRegistry.HitTest(state, 90, 6);
        var stateLimits = ShowcaseFrameHitRegistry.HitTest(state, 90, 20);
        var footer = ShowcaseFrameHitRegistry.HitTest(state, 3, 29);

        Assert.Equal("inline_mode:header", header.LocalHitId);
        Assert.Equal((uint)36_000, header.UpstreamHitId);
        Assert.Equal("inline_mode:inline_story", inlineStory.LocalHitId);
        Assert.Equal((uint)36_100, inlineStory.UpstreamHitId);
        Assert.Equal("inline_mode:alt_story", altStory.LocalHitId);
        Assert.Equal((uint)36_110, altStory.UpstreamHitId);
        Assert.Equal("inline_mode:controls", controls.LocalHitId);
        Assert.Equal((uint)36_200, controls.UpstreamHitId);
        Assert.Equal("inline_mode:state_limits", stateLimits.LocalHitId);
        Assert.Equal((uint)36_210, stateLimits.UpstreamHitId);
        Assert.Equal("inline_mode:footer", footer.LocalHitId);
        Assert.Equal((uint)36_300, footer.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsInlineModeStoryMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-inline-mode-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=36", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 36,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var headerEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 2, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var inlineEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));
        var stateEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 20, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, headerEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, inlineEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, stateEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var headerRecord = JsonDocument.Parse(lines[0]);
        using var inlineRecord = JsonDocument.Parse(lines[1]);
        using var stateRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("inline_mode_compare_toggle", headerRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("inline_mode:header", headerRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(36_000, headerRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("inline_mode_log_rate_increment", inlineRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("inline_mode:inline_story", inlineRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(36_100, inlineRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("inline_mode_state_focus", stateRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("inline_mode:state_limits", stateRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(36_210, stateRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseInlineModeMouseMutatesComparePauseRateHeightAndScroll()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 36,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 2, timestamp);
        Assert.True(state.InlineModeCompareEnabled);
        Assert.Equal(0, state.InlineModeFocusIndex);

        state = ApplyMouse(state, 3, 6, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.True(state.InlineModePaused);
        Assert.Equal(1, state.InlineModeFocusIndex);

        state = ApplyMouse(
            state,
            3,
            6,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.InlineModeLogRateIndex);
        Assert.Equal(1, state.InlineModeFocusIndex);

        state = ApplyMouse(state, 90, 6, timestamp + TimeSpan.FromMilliseconds(30));
        Assert.Equal(1, state.InlineModeUiHeightIndex);
        Assert.Equal(3, state.InlineModeFocusIndex);

        state = ApplyMouse(
            state,
            90,
            20,
            timestamp + TimeSpan.FromMilliseconds(40),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.InlineModeStateScroll);
        Assert.Equal(4, state.InlineModeFocusIndex);
    }

    [Fact]
    public void ShowcaseInlineModeRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 36,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            InlineModeFocusIndex = 4,
            InlineModeLogRateIndex = 3,
            InlineModeUiHeightIndex = 2,
            InlineModeStateScroll = 4,
            InlineModeCompareEnabled = true,
            InlineModePaused = true,
            InlineModeAnchorBottom = false
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Compare: ON", screen);
        Assert.Contains("Anchor: Top", screen);
        Assert.Contains("Status: Paused", screen);
        Assert.Contains("Rate: 10/tick", screen);
        Assert.Contains("State scroll: 4", screen);
        Assert.Contains("C compare(on)", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesAccessibilityPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 37,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var overview = ShowcaseFrameHitRegistry.HitTest(state, 3, 2);
        var toggles = ShowcaseFrameHitRegistry.HitTest(state, 3, 10);
        var preview = ShowcaseFrameHitRegistry.HitTest(state, 3, 22);
        var wcag = ShowcaseFrameHitRegistry.HitTest(state, 70, 10);
        var telemetry = ShowcaseFrameHitRegistry.HitTest(state, 70, 22);
        var footer = ShowcaseFrameHitRegistry.HitTest(state, 3, 29);

        Assert.Equal("accessibility:overview", overview.LocalHitId);
        Assert.Equal((uint)37_000, overview.UpstreamHitId);
        Assert.Equal("accessibility:toggles", toggles.LocalHitId);
        Assert.Equal((uint)37_100, toggles.UpstreamHitId);
        Assert.Equal("accessibility:preview", preview.LocalHitId);
        Assert.Equal((uint)37_110, preview.UpstreamHitId);
        Assert.Equal("accessibility:wcag", wcag.LocalHitId);
        Assert.Equal((uint)37_200, wcag.UpstreamHitId);
        Assert.Equal("accessibility:telemetry", telemetry.LocalHitId);
        Assert.Equal((uint)37_210, telemetry.UpstreamHitId);
        Assert.Equal("accessibility:footer", footer.LocalHitId);
        Assert.Equal((uint)37_300, footer.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsAccessibilityMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-accessibility-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=37", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 37,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var togglesEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 10, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var previewEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 22, TerminalMouseButton.WheelUp, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));
        var telemetryEvent = TerminalEvent.Mouse(
            new MouseGesture(70, 22, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, togglesEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, previewEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, telemetryEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var togglesRecord = JsonDocument.Parse(lines[0]);
        using var previewRecord = JsonDocument.Parse(lines[1]);
        using var telemetryRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("accessibility_toggle_select", togglesRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("accessibility:toggles", togglesRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(37_100, togglesRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("accessibility_preview_scroll_up", previewRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("accessibility:preview", previewRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(37_110, previewRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("accessibility_telemetry_focus", telemetryRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("accessibility:telemetry", telemetryRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(37_210, telemetryRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseAccessibilityMouseMutatesTogglesPreviewAndTelemetry()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 37,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 10, timestamp);
        Assert.True(state.A11yHighContrast);
        Assert.Equal(0, state.AccessibilitySelectedToggleIndex);
        Assert.Equal(1, state.AccessibilityFocusIndex);

        state = ApplyMouse(state, 3, 12, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.True(state.A11yReducedMotion);
        Assert.Equal(1, state.AccessibilitySelectedToggleIndex);

        state = ApplyMouse(state, 3, 14, timestamp + TimeSpan.FromMilliseconds(20));
        Assert.True(state.A11yLargeText);
        Assert.Equal(2, state.AccessibilitySelectedToggleIndex);

        state = ApplyMouse(
            state,
            3,
            22,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.AccessibilityPreviewScroll);
        Assert.Equal(2, state.AccessibilityFocusIndex);

        state = ApplyMouse(
            state,
            70,
            22,
            timestamp + TimeSpan.FromMilliseconds(40),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.AccessibilityTelemetryScroll);
        Assert.Equal(4, state.AccessibilityFocusIndex);
    }

    [Fact]
    public void ShowcaseAccessibilityRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 37,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            A11yHighContrast = true,
            A11yReducedMotion = true,
            A11yLargeText = true,
            AccessibilityFocusIndex = 4,
            AccessibilitySelectedToggleIndex = 2,
            AccessibilityPreviewScroll = 3,
            AccessibilityTelemetryScroll = 4
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Toggles", screen);
        Assert.Contains("> [l] Large Text: ON", screen);
        Assert.Contains("Live Preview [scroll 3]", screen);
        Assert.Contains("Telemetry scroll: 4", screen);
        Assert.Contains("selected=2 focus=4", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesWidgetBuilderPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 38,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var header = ShowcaseFrameHitRegistry.HitTest(state, 3, 2);
        var presets = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var tree = ShowcaseFrameHitRegistry.HitTest(state, 3, 20);
        var preview = ShowcaseFrameHitRegistry.HitTest(state, 45, 6);
        var props = ShowcaseFrameHitRegistry.HitTest(state, 90, 6);
        var export = ShowcaseFrameHitRegistry.HitTest(state, 90, 20);
        var footer = ShowcaseFrameHitRegistry.HitTest(state, 3, 29);

        Assert.Equal("widget_builder:header", header.LocalHitId);
        Assert.Equal((uint)38_000, header.UpstreamHitId);
        Assert.Equal("widget_builder:presets", presets.LocalHitId);
        Assert.Equal((uint)38_100, presets.UpstreamHitId);
        Assert.Equal("widget_builder:tree", tree.LocalHitId);
        Assert.Equal((uint)38_110, tree.UpstreamHitId);
        Assert.Equal("widget_builder:preview", preview.LocalHitId);
        Assert.Equal((uint)38_200, preview.UpstreamHitId);
        Assert.Equal("widget_builder:props", props.LocalHitId);
        Assert.Equal((uint)38_300, props.UpstreamHitId);
        Assert.Equal("widget_builder:export", export.LocalHitId);
        Assert.Equal((uint)38_310, export.UpstreamHitId);
        Assert.Equal("widget_builder:footer", footer.LocalHitId);
        Assert.Equal((uint)38_400, footer.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsWidgetBuilderMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-widget-builder-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=38", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 38,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var presetsEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.Right, TerminalMouseKind.Down),
            timestamp);
        var treeEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 20, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));
        var previewEvent = TerminalEvent.Mouse(
            new MouseGesture(45, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, presetsEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, treeEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, previewEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var presetsRecord = JsonDocument.Parse(lines[0]);
        using var treeRecord = JsonDocument.Parse(lines[1]);
        using var previewRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("widget_builder_preset_save", presetsRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("widget_builder:presets", presetsRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(38_100, presetsRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("widget_builder_tree_scroll_down", treeRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("widget_builder:tree", treeRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(38_110, treeRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("widget_builder_preview_toggle", previewRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("widget_builder:preview", previewRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(38_200, previewRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseWidgetBuilderMouseMutatesPresetTreePreviewPropsAndExport()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 38,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 5, timestamp);
        Assert.Equal(1, state.WidgetBuilderPresetIndex);
        Assert.Equal(1, state.WidgetBuilderFocusIndex);

        state = ApplyMouse(
            state,
            3,
            6,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.Right);
        Assert.True(state.WidgetBuilderPresetSaved);
        Assert.Equal(1, state.WidgetBuilderFocusIndex);

        state = ApplyMouse(
            state,
            3,
            20,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.WidgetBuilderSelectedIndex);
        Assert.Equal(1, state.WidgetBuilderTreeScroll);
        Assert.Equal(2, state.WidgetBuilderFocusIndex);

        state = ApplyMouse(state, 45, 6, timestamp + TimeSpan.FromMilliseconds(30));
        Assert.False(state.WidgetBuilderPreviewEnabled);
        Assert.Equal(3, state.WidgetBuilderFocusIndex);

        state = ApplyMouse(
            state,
            90,
            6,
            timestamp + TimeSpan.FromMilliseconds(40),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.WidgetBuilderPropsScroll);
        Assert.True(state.WidgetBuilderValue >= 0);
        Assert.Equal(4, state.WidgetBuilderFocusIndex);

        state = ApplyMouse(state, 90, 20, timestamp + TimeSpan.FromMilliseconds(50));
        Assert.True(state.WidgetBuilderExportArmed);
        Assert.Equal(5, state.WidgetBuilderFocusIndex);
    }

    [Fact]
    public void ShowcaseWidgetBuilderRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 38,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            WidgetBuilderPresetIndex = 1,
            WidgetBuilderSelectedIndex = 4,
            WidgetBuilderFocusIndex = 5,
            WidgetBuilderTreeScroll = 2,
            WidgetBuilderPropsScroll = 3,
            WidgetBuilderValue = 85,
            WidgetBuilderPreviewEnabled = false,
            WidgetBuilderBorderEnabled = false,
            WidgetBuilderPresetSaved = true,
            WidgetBuilderExportArmed = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Preset: Status Wall", screen);
        Assert.Contains("preview=off", screen);
        Assert.Contains("> 05. Badge [off]", screen);
        Assert.Contains("Border: off", screen);
        Assert.Contains("status=ready", screen);
        Assert.Contains("X export(ready)", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesDeterminismLabPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 40,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var header = ShowcaseFrameHitRegistry.HitTest(state, 3, 2);
        var equivalence = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var report = ShowcaseFrameHitRegistry.HitTest(state, 3, 22);
        var preview = ShowcaseFrameHitRegistry.HitTest(state, 80, 6);
        var checks = ShowcaseFrameHitRegistry.HitTest(state, 80, 22);
        var footer = ShowcaseFrameHitRegistry.HitTest(state, 3, 29);

        Assert.Equal("determinism:header", header.LocalHitId);
        Assert.Equal((uint)40_000, header.UpstreamHitId);
        Assert.Equal("determinism:equivalence", equivalence.LocalHitId);
        Assert.Equal((uint)40_100, equivalence.UpstreamHitId);
        Assert.Equal("determinism:report", report.LocalHitId);
        Assert.Equal((uint)40_110, report.UpstreamHitId);
        Assert.Equal("determinism:preview", preview.LocalHitId);
        Assert.Equal((uint)40_200, preview.UpstreamHitId);
        Assert.Equal("determinism:checks", checks.LocalHitId);
        Assert.Equal((uint)40_210, checks.UpstreamHitId);
        Assert.Equal("determinism:footer", footer.LocalHitId);
        Assert.Equal((uint)40_300, footer.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsDeterminismLabMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-determinism-lab-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=40", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 40,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var equivalenceEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var reportEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 22, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));
        var checksEvent = TerminalEvent.Mouse(
            new MouseGesture(80, 22, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, equivalenceEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, reportEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, checksEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var equivalenceRecord = JsonDocument.Parse(lines[0]);
        using var reportRecord = JsonDocument.Parse(lines[1]);
        using var checksRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("determinism_strategy_select", equivalenceRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("determinism:equivalence", equivalenceRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(40_100, equivalenceRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("determinism_report_scroll_down", reportRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("determinism:report", reportRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(40_110, reportRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("determinism_scenario_run", checksRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("determinism:checks", checksRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(40_210, checksRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseDeterminismLabMouseMutatesStrategyReportPreviewChecksAndReset()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 40,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 6, timestamp);
        Assert.Equal(2, state.DeterminismStrategyIndex);
        Assert.Equal(1, state.DeterminismFocusIndex);

        state = ApplyMouse(
            state,
            3,
            22,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.DeterminismReportScroll);
        Assert.Equal(2, state.DeterminismFocusIndex);

        state = ApplyMouse(state, 80, 6, timestamp + TimeSpan.FromMilliseconds(20));
        Assert.Equal(1, state.DeterminismSeedOffset);
        Assert.Equal(3, state.DeterminismFocusIndex);

        state = ApplyMouse(state, 80, 22, timestamp + TimeSpan.FromMilliseconds(30));
        Assert.Equal(1, state.DeterminismScenarioIndex);
        Assert.Equal(1, state.DeterminismRunCount);
        Assert.Equal(4, state.DeterminismFocusIndex);

        state = ApplyMouse(state, 3, 29, timestamp + TimeSpan.FromMilliseconds(40));
        Assert.Equal(1, state.DeterminismStrategyIndex);
        Assert.Equal(0, state.DeterminismScenarioIndex);
        Assert.Equal(0, state.DeterminismSeedOffset);
        Assert.Equal(5, state.DeterminismFocusIndex);
    }

    [Fact]
    public void ShowcaseDeterminismLabRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 40,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            DeterminismFocusIndex = 4,
            DeterminismStrategyIndex = 2,
            DeterminismScenarioIndex = 2,
            DeterminismSeedOffset = 3,
            DeterminismReportScroll = 2,
            DeterminismChecksScroll = 4,
            DeterminismRunCount = 5,
            DeterminismFaultEnabled = true,
            DeterminismExportArmed = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("active=FullRedraw", screen);
        Assert.Contains("fault=ON", screen);
        Assert.Contains("> FullRedraw", screen);
        Assert.Contains("Report scroll: 2 | export=ready", screen);
        Assert.Contains("Checks [scroll 4]", screen);
        Assert.Contains("run_count=5", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesVisualEffectsPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 18,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var canvas = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var harness = ShowcaseFrameHitRegistry.HitTest(state, 70, 6);

        Assert.Equal("visual_effects:canvas", canvas.LocalHitId);
        Assert.Equal((uint)18_000, canvas.UpstreamHitId);
        Assert.Equal("visual_effects:harness", harness.LocalHitId);
        Assert.Equal((uint)18_100, harness.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsVisualEffectsMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-visual-effects-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=18", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 18,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var canvasEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp);
        var harnessEvent = TerminalEvent.Mouse(
            new MouseGesture(70, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(10));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, canvasEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, harnessEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var canvasRecord = JsonDocument.Parse(lines[0]);
        using var harnessRecord = JsonDocument.Parse(lines[1]);
        Assert.Equal("visual_effects_effect_next", canvasRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("visual_effects:canvas", canvasRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(18_000, canvasRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("visual_effects_harness_focus", harnessRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("visual_effects:harness", harnessRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(18_100, harnessRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseVisualEffectsMouseMutatesEffectFocusAndHarnessScroll()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 18,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(
            state,
            3,
            6,
            timestamp,
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(0, state.VisualEffectsFocusIndex);
        Assert.Equal(3, state.VisualEffectsEffectIndex);

        state = ApplyMouse(state, 70, 6, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.Equal(1, state.VisualEffectsFocusIndex);

        state = ApplyMouse(
            state,
            70,
            6,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.VisualEffectsFocusIndex);
        Assert.Equal(1, state.VisualEffectsHarnessScroll);
    }

    [Fact]
    public void ShowcaseVisualEffectsRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 18,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            VisualEffectsFocusIndex = 1,
            VisualEffectsEffectIndex = 4,
            VisualEffectsHarnessScroll = 3
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("vfx mouse focus=1 effect_idx=4 effect=matrix harness_scroll=3", screen);
        Assert.Contains("Effect: matrix", screen);
        Assert.Contains("harness_scroll=3", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesResponsiveLayoutPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 19,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var indicator = ShowcaseFrameHitRegistry.HitTest(state, 3, 2);
        var sidebar = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var content = ShowcaseFrameHitRegistry.HitTest(state, 45, 6);
        var aside = ShowcaseFrameHitRegistry.HitTest(state, 100, 6);

        Assert.Equal("responsive:indicator", indicator.LocalHitId);
        Assert.Equal((uint)19_000, indicator.UpstreamHitId);
        Assert.Equal("responsive:sidebar", sidebar.LocalHitId);
        Assert.Equal((uint)19_200, sidebar.UpstreamHitId);
        Assert.Equal("responsive:content", content.LocalHitId);
        Assert.Equal((uint)19_210, content.UpstreamHitId);
        Assert.Equal("responsive:aside", aside.LocalHitId);
        Assert.Equal((uint)19_220, aside.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsResponsiveLayoutMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-responsive-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=19", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 19,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var indicatorEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 2, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var contentEvent = TerminalEvent.Mouse(
            new MouseGesture(45, 6, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));
        var asideEvent = TerminalEvent.Mouse(
            new MouseGesture(100, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, indicatorEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, contentEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, asideEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var indicatorRecord = JsonDocument.Parse(lines[0]);
        using var contentRecord = JsonDocument.Parse(lines[1]);
        using var asideRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("responsive_breakpoints_toggle", indicatorRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("responsive:indicator", indicatorRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(19_000, indicatorRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("responsive_width_increment", contentRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("responsive:content", contentRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(19_210, contentRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("responsive_aside_toggle", asideRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("responsive:aside", asideRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(19_220, asideRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseResponsiveMouseMutatesBreakpointWidthFocusAndAside()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 19,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 2, timestamp);
        Assert.Equal(0, state.ResponsiveFocusIndex);
        Assert.True(state.ResponsiveCustomBreakpoints);

        state = ApplyMouse(
            state,
            45,
            6,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(4, state.ResponsiveFocusIndex);
        Assert.Equal(10, state.ResponsiveWidthOffset);

        state = ApplyMouse(state, 100, 6, timestamp + TimeSpan.FromMilliseconds(20));
        Assert.Equal(5, state.ResponsiveFocusIndex);
        Assert.True(state.ResponsiveAsideForcedVisible);

        state = ApplyMouse(
            state,
            3,
            2,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.Right);
        Assert.Equal(0, state.ResponsiveFocusIndex);
        Assert.Equal(0, state.ResponsiveWidthOffset);
        Assert.False(state.ResponsiveCustomBreakpoints);
        Assert.False(state.ResponsiveAsideForcedVisible);
    }

    [Fact]
    public void ShowcaseResponsiveRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(100, 32),
            screenNumber: 19,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            ResponsiveFocusIndex = 5,
            ResponsiveWidthOffset = 20,
            ResponsiveCustomBreakpoints = true,
            ResponsiveAsideForcedVisible = true
        };
        var buffer = new RenderBuffer(140, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(140, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("responsive mouse focus=5 width_offset=20 custom_bp=on aside=forced", screen);
        Assert.Contains("[Current: custom]", screen);
        Assert.Contains("Offset: 20", screen);
        Assert.Contains("Aside mode: forced", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesIntrinsicSizingPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 23,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var header = ShowcaseFrameHitRegistry.HitTest(state, 3, 2);
        var scenarios = ShowcaseFrameHitRegistry.HitTest(state, 3, 8);
        var detail = ShowcaseFrameHitRegistry.HitTest(state, 50, 8);
        var controls = ShowcaseFrameHitRegistry.HitTest(state, 95, 8);

        Assert.Equal("intrinsic_sizing:header", header.LocalHitId);
        Assert.Equal((uint)23_000, header.UpstreamHitId);
        Assert.Equal("intrinsic_sizing:scenarios", scenarios.LocalHitId);
        Assert.Equal((uint)23_100, scenarios.UpstreamHitId);
        Assert.Equal("intrinsic_sizing:detail", detail.LocalHitId);
        Assert.Equal((uint)23_200, detail.UpstreamHitId);
        Assert.Equal("intrinsic_sizing:controls", controls.LocalHitId);
        Assert.Equal((uint)23_300, controls.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsIntrinsicSizingMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-intrinsic-sizing-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=23", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 23,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var scenarioEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 8, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var detailEvent = TerminalEvent.Mouse(
            new MouseGesture(50, 8, TerminalMouseButton.WheelUp, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));
        var controlsEvent = TerminalEvent.Mouse(
            new MouseGesture(95, 8, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, scenarioEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, detailEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, controlsEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var scenarioRecord = JsonDocument.Parse(lines[0]);
        using var detailRecord = JsonDocument.Parse(lines[1]);
        using var controlsRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("intrinsic_sizing_scenario_select", scenarioRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("intrinsic_sizing:scenarios", scenarioRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(23_100, scenarioRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("intrinsic_sizing_width_decrement", detailRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("intrinsic_sizing:detail", detailRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(23_200, detailRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("intrinsic_sizing_controls_focus", controlsRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("intrinsic_sizing:controls", controlsRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(23_300, controlsRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseIntrinsicSizingMouseMutatesScenarioWidthAndDetailScroll()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 23,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 8, timestamp);
        Assert.Equal(1, state.IntrinsicSizingScenarioIndex);
        Assert.Equal(1, state.IntrinsicSizingFocusIndex);
        Assert.False(state.IntrinsicSizingContextArmed);

        state = ApplyMouse(state, 95, 8, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.Equal(3, state.IntrinsicSizingWidthPresetIndex);
        Assert.Equal(3, state.IntrinsicSizingFocusIndex);

        state = ApplyMouse(
            state,
            50,
            8,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.IntrinsicSizingDetailScroll);
        Assert.Equal(2, state.IntrinsicSizingFocusIndex);

        state = ApplyMouse(
            state,
            95,
            8,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.Right,
            TerminalMouseKind.Down);
        Assert.Equal(3, state.IntrinsicSizingFocusIndex);
        Assert.True(state.IntrinsicSizingContextArmed);
    }

    [Fact]
    public void ShowcaseIntrinsicSizingRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 23,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            IntrinsicSizingScenarioIndex = 2,
            IntrinsicSizingWidthPresetIndex = 1,
            IntrinsicSizingDetailScroll = 3,
            IntrinsicSizingFocusIndex = 3,
            IntrinsicSizingContextArmed = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Scenario: Auto-Sizing Table (3/4)", screen);
        Assert.Contains("Width preset: 1", screen);
        Assert.Contains("Focus: 3 ctx", screen);
        Assert.Contains("Auto-Sizing Table [scroll 3]", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesLayoutInspectorPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 24,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var info = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var overlay = ShowcaseFrameHitRegistry.HitTest(state, 45, 6);
        var tree = ShowcaseFrameHitRegistry.HitTest(state, 45, 18);
        var pane = ShowcaseFrameHitRegistry.HitTest(state, 95, 6);

        Assert.Equal("layout_inspector:info", info.LocalHitId);
        Assert.Equal((uint)24_000, info.UpstreamHitId);
        Assert.Equal("layout_inspector:overlay", overlay.LocalHitId);
        Assert.Equal((uint)24_100, overlay.UpstreamHitId);
        Assert.Equal("layout_inspector:tree", tree.LocalHitId);
        Assert.Equal((uint)24_110, tree.UpstreamHitId);
        Assert.Equal("layout_inspector:pane_studio", pane.LocalHitId);
        Assert.Equal((uint)24_200, pane.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsLayoutInspectorMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-layout-inspector-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=24", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 24,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var infoEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var overlayEvent = TerminalEvent.Mouse(
            new MouseGesture(45, 6, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));
        var paneEvent = TerminalEvent.Mouse(
            new MouseGesture(95, 6, TerminalMouseButton.Right, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, infoEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, overlayEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, paneEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var infoRecord = JsonDocument.Parse(lines[0]);
        using var overlayRecord = JsonDocument.Parse(lines[1]);
        using var paneRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("layout_inspector_scenario_select", infoRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("layout_inspector:info", infoRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(24_000, infoRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("layout_inspector_step_next", overlayRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("layout_inspector:overlay", overlayRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(24_100, overlayRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("layout_inspector_pane_mode", paneRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("layout_inspector:pane_studio", paneRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(24_200, paneRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseLayoutInspectorMouseMutatesScenarioStepOverlayAndTree()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 24,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 6, timestamp);
        Assert.Equal(1, state.LayoutInspectorScenarioIndex);
        Assert.Equal(0, state.LayoutInspectorFocusIndex);
        Assert.False(state.LayoutInspectorContextArmed);

        state = ApplyMouse(state, 45, 6, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.Equal(1, state.LayoutInspectorStepIndex);
        Assert.Equal(1, state.LayoutInspectorFocusIndex);

        state = ApplyMouse(
            state,
            45,
            6,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.Right);
        Assert.False(state.LayoutInspectorOverlayVisible);
        Assert.Equal(1, state.LayoutInspectorFocusIndex);
        Assert.True(state.LayoutInspectorContextArmed);

        state = ApplyMouse(state, 45, 18, timestamp + TimeSpan.FromMilliseconds(30));
        Assert.False(state.LayoutInspectorTreeVisible);
        Assert.Equal(2, state.LayoutInspectorFocusIndex);
        Assert.False(state.LayoutInspectorContextArmed);
    }

    [Fact]
    public void ShowcaseLayoutInspectorRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 24,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            LayoutInspectorScenarioIndex = 2,
            LayoutInspectorStepIndex = 2,
            LayoutInspectorFocusIndex = 1,
            LayoutInspectorOverlayVisible = false,
            LayoutInspectorTreeVisible = false,
            LayoutInspectorContextArmed = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Scenario: FitContent Clamp", screen);
        Assert.Contains("Step: Final", screen);
        Assert.Contains("Overlay: off", screen);
        Assert.Contains("Focus: 1 ctx", screen);
        Assert.Contains("Tree: off", screen);
        Assert.Contains("Pane Studio [overlay off]", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesActionTimelinePanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 22,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var filters = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var timeline = ShowcaseFrameHitRegistry.HitTest(state, 3, 20);
        var detail = ShowcaseFrameHitRegistry.HitTest(state, 80, 20);

        Assert.Equal("action_timeline:filters", filters.LocalHitId);
        Assert.Equal((uint)22_000, filters.UpstreamHitId);
        Assert.Equal("action_timeline:timeline", timeline.LocalHitId);
        Assert.Equal((uint)22_100, timeline.UpstreamHitId);
        Assert.Equal("action_timeline:detail", detail.LocalHitId);
        Assert.Equal((uint)22_200, detail.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsActionTimelineMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-action-timeline-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=22", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 22,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var filtersEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var timelineEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 20, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));
        var detailEvent = TerminalEvent.Mouse(
            new MouseGesture(80, 20, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, filtersEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, timelineEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, detailEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var filtersRecord = JsonDocument.Parse(lines[0]);
        using var timelineRecord = JsonDocument.Parse(lines[1]);
        using var detailRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("action_timeline_filter_cycle", filtersRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("action_timeline:filters", filtersRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(22_000, filtersRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("action_timeline_scroll_down", timelineRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("action_timeline:timeline", timelineRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(22_100, timelineRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("action_timeline_detail_toggle", detailRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("action_timeline:detail", detailRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(22_200, detailRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseActionTimelineMouseMutatesFilterSelectionAndDetail()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 22,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 6, timestamp);
        Assert.Equal(1, state.ActionTimelineFilterIndex);
        Assert.Equal(0, state.ActionTimelineFocusIndex);
        Assert.False(state.ActionTimelineContextArmed);

        state = ApplyMouse(
            state,
            3,
            20,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.ActionTimelineSelectedIndex);
        Assert.Equal(1, state.ActionTimelineFocusIndex);

        state = ApplyMouse(state, 80, 20, timestamp + TimeSpan.FromMilliseconds(20));
        Assert.True(state.ActionTimelineDetailExpanded);
        Assert.Equal(2, state.ActionTimelineFocusIndex);

        state = ApplyMouse(
            state,
            80,
            20,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.Right,
            TerminalMouseKind.Down);
        Assert.Equal(2, state.ActionTimelineFocusIndex);
        Assert.True(state.ActionTimelineContextArmed);
    }

    [Fact]
    public void ShowcaseActionTimelineRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 22,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            ActionTimelineFilterIndex = 2,
            ActionTimelineSelectedIndex = 3,
            ActionTimelineDetailExpanded = true,
            ActionTimelineFocusIndex = 2,
            ActionTimelineContextArmed = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Active filter: component:runtime", screen);
        Assert.Contains("Focus: 2 context", screen);
        Assert.Contains("Event Timeline [selected 3]", screen);
        Assert.Contains("Event Detail [context]", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesTerminalCapabilitiesPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 12,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var matrix = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var evidence = ShowcaseFrameHitRegistry.HitTest(state, 45, 6);
        var simulation = ShowcaseFrameHitRegistry.HitTest(state, 90, 6);

        Assert.Equal("terminal_capabilities:matrix", matrix.LocalHitId);
        Assert.Equal((uint)12_000, matrix.UpstreamHitId);
        Assert.Equal("terminal_capabilities:evidence", evidence.LocalHitId);
        Assert.Equal((uint)12_100, evidence.UpstreamHitId);
        Assert.Equal("terminal_capabilities:simulation", simulation.LocalHitId);
        Assert.Equal((uint)12_200, simulation.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsTerminalCapabilitiesMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-terminal-capabilities-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=12", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 12,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var matrixEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var evidenceEvent = TerminalEvent.Mouse(
            new MouseGesture(45, 6, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));
        var simulationEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 6, TerminalMouseButton.Right, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, matrixEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, evidenceEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, simulationEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var matrixRecord = JsonDocument.Parse(lines[0]);
        using var evidenceRecord = JsonDocument.Parse(lines[1]);
        using var simulationRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("terminal_capabilities_inspect", matrixRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("terminal_capabilities:matrix", matrixRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(12_000, matrixRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("terminal_capabilities_panel_scroll_down", evidenceRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("terminal_capabilities:evidence", evidenceRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(12_100, evidenceRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("terminal_capabilities_profile_reset", simulationRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("terminal_capabilities:simulation", simulationRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(12_200, simulationRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseTerminalCapabilitiesMouseMutatesSelectionAndProfile()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 12,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 7, timestamp);
        Assert.Equal(2, state.TerminalCapabilitiesSelectedRow);

        state = ApplyMouse(
            state,
            3,
            7,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelUp,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.TerminalCapabilitiesSelectedRow);

        state = ApplyMouse(
            state,
            90,
            6,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.TerminalCapabilitiesProfileIndex);
    }

    [Fact]
    public void ShowcaseTerminalCapabilitiesRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 12,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            TerminalCapabilitiesSelectedRow = 2,
            TerminalCapabilitiesProfileIndex = 2
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Selected: hyperlinks", screen);
        Assert.Contains("active=xterm-256color", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesPerformancePanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 14,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var list = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var selected = ShowcaseFrameHitRegistry.HitTest(state, 3, 15);
        var stats = ShowcaseFrameHitRegistry.HitTest(state, 92, 6);
        var footer = ShowcaseFrameHitRegistry.HitTest(state, 3, 29);

        Assert.Equal("performance:list", list.LocalHitId);
        Assert.Equal((uint)14_000, list.UpstreamHitId);
        Assert.Equal("performance:list:selected", selected.LocalHitId);
        Assert.Equal((uint)14_010, selected.UpstreamHitId);
        Assert.Equal("performance:stats", stats.LocalHitId);
        Assert.Equal((uint)14_100, stats.UpstreamHitId);
        Assert.Equal("performance:footer", footer.LocalHitId);
        Assert.Equal((uint)14_200, footer.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsPerformanceMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-performance-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=14", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 14,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var selectedEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 15, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var statsEvent = TerminalEvent.Mouse(
            new MouseGesture(92, 6, TerminalMouseButton.Right, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(10));
        var listEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, selectedEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, statsEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, listEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var selectedRecord = JsonDocument.Parse(lines[0]);
        using var statsRecord = JsonDocument.Parse(lines[1]);
        using var listRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("performance_row_select", selectedRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("performance:list:selected", selectedRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(14_010, selectedRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("performance_stats_context", statsRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("performance:stats", statsRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(14_100, statsRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("performance_list_scroll_down", listRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("performance:list", listRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(14_000, listRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcasePerformanceMouseMutatesSelectedItem()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 14,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(
            state,
            3,
            6,
            timestamp,
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.PerformanceSelectedIndex);
        Assert.Equal(0, state.PerformanceFocusIndex);
        Assert.False(state.PerformanceContextArmed);

        state = ApplyMouse(state, 3, 15, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.Equal(2, state.PerformanceSelectedIndex);
        Assert.Equal(1, state.PerformanceFocusIndex);

        state = ApplyMouse(
            state,
            100,
            8,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.Right,
            TerminalMouseKind.Down);
        Assert.Equal(2, state.PerformanceFocusIndex);
        Assert.True(state.PerformanceContextArmed);
    }

    [Fact]
    public void ShowcasePerformanceRendersMouseSelectedItem()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 14,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            PerformanceSelectedIndex = 42,
            PerformanceFocusIndex = 2,
            PerformanceContextArmed = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Performance Stats [context]", screen);
        Assert.Contains("Selected:     43 / 10000", screen);
        Assert.Contains("Focus:        2 context", screen);
        Assert.Contains("Event #00042", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesMarkdownPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 15,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var renderer = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var stream = ShowcaseFrameHitRegistry.HitTest(state, 45, 6);
        var detection = ShowcaseFrameHitRegistry.HitTest(state, 45, 26);
        var style = ShowcaseFrameHitRegistry.HitTest(state, 90, 6);
        var unicode = ShowcaseFrameHitRegistry.HitTest(state, 90, 12);
        var wrap = ShowcaseFrameHitRegistry.HitTest(state, 90, 24);

        Assert.Equal("markdown:renderer", renderer.LocalHitId);
        Assert.Equal((uint)15_000, renderer.UpstreamHitId);
        Assert.Equal("markdown:stream", stream.LocalHitId);
        Assert.Equal((uint)15_100, stream.UpstreamHitId);
        Assert.Equal("markdown:detection", detection.LocalHitId);
        Assert.Equal((uint)15_110, detection.UpstreamHitId);
        Assert.Equal("markdown:style", style.LocalHitId);
        Assert.Equal((uint)15_200, style.UpstreamHitId);
        Assert.Equal("markdown:unicode", unicode.LocalHitId);
        Assert.Equal((uint)15_210, unicode.UpstreamHitId);
        Assert.Equal("markdown:wrap", wrap.LocalHitId);
        Assert.Equal((uint)15_220, wrap.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsMarkdownMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-markdown-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=15", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 15,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var rendererEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp);
        var detectionEvent = TerminalEvent.Mouse(
            new MouseGesture(45, 26, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(10));
        var wrapEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 24, TerminalMouseButton.Right, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, rendererEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, detectionEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, wrapEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var rendererRecord = JsonDocument.Parse(lines[0]);
        using var detectionRecord = JsonDocument.Parse(lines[1]);
        using var wrapRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("markdown_renderer_scroll_down", rendererRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("markdown:renderer", rendererRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(15_000, rendererRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("markdown_detection_focus", detectionRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("markdown:detection", detectionRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(15_110, detectionRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("markdown_wrap_context", wrapRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("markdown:wrap", wrapRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(15_220, wrapRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseMarkdownMouseMutatesPaneScrollAndWrapMode()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 15,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(
            state,
            3,
            6,
            timestamp,
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(0, state.MarkdownActivePaneIndex);
        Assert.Equal(1, state.MarkdownRendererScroll);
        Assert.False(state.MarkdownContextArmed);

        state = ApplyMouse(state, 90, 24, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.Equal(5, state.MarkdownActivePaneIndex);
        Assert.Equal(1, state.MarkdownWrapModeIndex);

        state = ApplyMouse(
            state,
            90,
            24,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.Right,
            TerminalMouseKind.Down);
        Assert.Equal(5, state.MarkdownActivePaneIndex);
        Assert.True(state.MarkdownContextArmed);
    }

    [Fact]
    public void ShowcaseMarkdownRendersMouseSelectedPaneState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 15,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            MarkdownActivePaneIndex = 5,
            MarkdownRendererScroll = 2,
            MarkdownStreamScroll = 3,
            MarkdownWrapModeIndex = 1,
            MarkdownContextArmed = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Wrap [Character context]", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesMermaidPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 16,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var header = ShowcaseFrameHitRegistry.HitTest(state, 3, 2);
        var library = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var viewport = ShowcaseFrameHitRegistry.HitTest(state, 40, 6);
        var controls = ShowcaseFrameHitRegistry.HitTest(state, 90, 6);
        var metrics = ShowcaseFrameHitRegistry.HitTest(state, 90, 18);
        var status = ShowcaseFrameHitRegistry.HitTest(state, 90, 26);

        Assert.Equal("mermaid:header", header.LocalHitId);
        Assert.Equal((uint)16_000, header.UpstreamHitId);
        Assert.Equal("mermaid:library", library.LocalHitId);
        Assert.Equal((uint)16_100, library.UpstreamHitId);
        Assert.Equal("mermaid:viewport", viewport.LocalHitId);
        Assert.Equal((uint)16_200, viewport.UpstreamHitId);
        Assert.Equal("mermaid:controls", controls.LocalHitId);
        Assert.Equal((uint)16_300, controls.UpstreamHitId);
        Assert.Equal("mermaid:metrics", metrics.LocalHitId);
        Assert.Equal((uint)16_400, metrics.UpstreamHitId);
        Assert.Equal("mermaid:status", status.LocalHitId);
        Assert.Equal((uint)16_500, status.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsMermaidMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-mermaid-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=16", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 16,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var libraryEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var viewportEvent = TerminalEvent.Mouse(
            new MouseGesture(40, 6, TerminalMouseButton.Right, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(10));
        var statusEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 26, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, libraryEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, viewportEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, statusEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var libraryRecord = JsonDocument.Parse(lines[0]);
        using var viewportRecord = JsonDocument.Parse(lines[1]);
        using var statusRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("mermaid_sample_select", libraryRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("mermaid:library", libraryRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(16_100, libraryRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("mermaid_viewport_reset", viewportRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("mermaid:viewport", viewportRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(16_200, viewportRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("mermaid_status_scroll_down", statusRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("mermaid:status", statusRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(16_500, statusRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseMermaidMouseMutatesSampleViewportStatusAndContext()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 16,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 6, timestamp);
        Assert.Equal(1, state.MermaidFocusIndex);
        Assert.Equal(1, state.MermaidSampleIndex);

        state = ApplyMouse(
            state,
            40,
            6,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(2, state.MermaidFocusIndex);
        Assert.Equal(1, state.MermaidZoomStep);

        state = ApplyMouse(
            state,
            90,
            26,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(5, state.MermaidFocusIndex);
        Assert.Equal(1, state.MermaidStatusScroll);

        state = ApplyMouse(
            state,
            40,
            6,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.Right);
        Assert.Equal(2, state.MermaidFocusIndex);
        Assert.Equal(0, state.MermaidZoomStep);
        Assert.True(state.MermaidContextArmed);
    }

    [Fact]
    public void ShowcaseMermaidRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(140, 36),
            screenNumber: 16,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            MermaidFocusIndex = 5,
            MermaidSampleIndex = 2,
            MermaidZoomStep = 3,
            MermaidPanelScroll = 4,
            MermaidStatusScroll = 5,
            MermaidContextArmed = true
        };
        var buffer = new RenderBuffer(140, 36);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(140, 36), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("mouse focus=5 sample_idx=2 zoom_step=3 panel_scroll=4 status_scroll=5 context=armed", screen);
        Assert.Contains("Mermaid Showcase", screen);
        Assert.Contains("Library", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesMermaidMegaPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 17,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var shared = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var library = ShowcaseFrameHitRegistry.HitTest(state, 90, 6);
        var controls = ShowcaseFrameHitRegistry.HitTest(state, 90, 16);
        var detail = ShowcaseFrameHitRegistry.HitTest(state, 90, 26);

        Assert.Equal("mermaid_mega:shared_showcase", shared.LocalHitId);
        Assert.Equal((uint)17_000, shared.UpstreamHitId);
        Assert.Equal("mermaid_mega:library", library.LocalHitId);
        Assert.Equal((uint)17_100, library.UpstreamHitId);
        Assert.Equal("mermaid_mega:controls", controls.LocalHitId);
        Assert.Equal((uint)17_200, controls.UpstreamHitId);
        Assert.Equal("mermaid_mega:node_detail", detail.LocalHitId);
        Assert.Equal((uint)17_300, detail.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsMermaidMegaMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-mermaid-mega-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=17", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 17,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var libraryEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var sharedEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.Right, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(10));
        var detailEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 26, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, libraryEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, sharedEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, detailEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var libraryRecord = JsonDocument.Parse(lines[0]);
        using var sharedRecord = JsonDocument.Parse(lines[1]);
        using var detailRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("mermaid_mega_sample_select", libraryRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("mermaid_mega:library", libraryRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(17_100, libraryRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("mermaid_mega_viewport_reset", sharedRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("mermaid_mega:shared_showcase", sharedRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(17_000, sharedRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("mermaid_mega_detail_scroll_down", detailRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("mermaid_mega:node_detail", detailRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(17_300, detailRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseMermaidMegaMouseMutatesSampleViewportDetailAndContext()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 17,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 90, 6, timestamp);
        Assert.Equal(1, state.MermaidMegaFocusIndex);
        Assert.Equal(1, state.MermaidMegaSampleIndex);

        state = ApplyMouse(
            state,
            3,
            6,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(0, state.MermaidMegaFocusIndex);
        Assert.Equal(1, state.MermaidMegaZoomStep);

        state = ApplyMouse(
            state,
            90,
            26,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(3, state.MermaidMegaFocusIndex);
        Assert.Equal(1, state.MermaidMegaDetailScroll);

        state = ApplyMouse(
            state,
            3,
            6,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.Right);
        Assert.Equal(0, state.MermaidMegaFocusIndex);
        Assert.Equal(0, state.MermaidMegaZoomStep);
        Assert.True(state.MermaidMegaContextArmed);
    }

    [Fact]
    public void ShowcaseMermaidMegaRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(150, 38),
            screenNumber: 17,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            MermaidMegaFocusIndex = 3,
            MermaidMegaSampleIndex = 2,
            MermaidMegaZoomStep = 2,
            MermaidMegaDetailScroll = 4,
            MermaidMegaPanelScroll = 5,
            MermaidMegaContextArmed = true
        };
        var buffer = new RenderBuffer(150, 38);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(150, 38), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("mega mouse focus=3 sample_idx=2 zoom_step=2 detail_scroll=4 panel_scroll=5 context=armed", screen);
        Assert.Contains("Mega Sample Library", screen);
        Assert.Contains("detail_scroll=4", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesAdvancedPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 10,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var patterns = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var composite = ShowcaseFrameHitRegistry.HitTest(state, 90, 6);

        Assert.Equal("advanced:patterns", patterns.LocalHitId);
        Assert.Equal((uint)10_000, patterns.UpstreamHitId);
        Assert.Equal("advanced:composite", composite.LocalHitId);
        Assert.Equal((uint)10_100, composite.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsAdvancedMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-advanced-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=10", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 10,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var patternsEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp);
        var compositeEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(10));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, patternsEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, compositeEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var patternsRecord = JsonDocument.Parse(lines[0]);
        using var compositeRecord = JsonDocument.Parse(lines[1]);
        Assert.Equal("advanced_patterns_scroll_down", patternsRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("advanced:patterns", patternsRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(10_000, patternsRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("advanced_composite_focus", compositeRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("advanced:composite", compositeRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(10_100, compositeRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseAdvancedMouseMutatesPatternAndCompositeSelection()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 10,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(
            state,
            3,
            6,
            timestamp,
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.AdvancedPatternIndex);
        Assert.Equal(0, state.AdvancedFocusIndex);

        state = ApplyMouse(state, 90, 6, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.Equal(1, state.AdvancedCompositeModeIndex);
        Assert.Equal(1, state.AdvancedFocusIndex);

        state = ApplyMouse(
            state,
            90,
            6,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.Right,
            TerminalMouseKind.Down);
        Assert.True(state.AdvancedContextArmed);
        Assert.Equal(1, state.AdvancedFocusIndex);
    }

    [Fact]
    public void ShowcaseAdvancedRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 10,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            AdvancedPatternIndex = 3,
            AdvancedCompositeModeIndex = 2,
            AdvancedFocusIndex = 1,
            AdvancedContextArmed = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Patterns [selected 3]", screen);
        Assert.Contains("Composite [focus evidence]", screen);
        Assert.Contains("advanced mouse focus=1 context=armed", screen);
        Assert.Contains("Selected pattern: 3", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesTableThemeGalleryPresets()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 11,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var presetA = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var presetB = ShowcaseFrameHitRegistry.HitTest(state, 45, 6);
        var presetC = ShowcaseFrameHitRegistry.HitTest(state, 90, 6);

        Assert.Equal("table_theme:preset:0", presetA.LocalHitId);
        Assert.Equal((uint)11_000, presetA.UpstreamHitId);
        Assert.Equal("table_theme:preset:1", presetB.LocalHitId);
        Assert.Equal((uint)11_001, presetB.UpstreamHitId);
        Assert.Equal("table_theme:preset:2", presetC.LocalHitId);
        Assert.Equal((uint)11_002, presetC.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsTableThemeGalleryMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-table-theme-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=11", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 11,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var selectEvent = TerminalEvent.Mouse(
            new MouseGesture(45, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var scrollEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 6, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));
        var contextEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.Right, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, selectEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, scrollEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, contextEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var selectRecord = JsonDocument.Parse(lines[0]);
        using var scrollRecord = JsonDocument.Parse(lines[1]);
        using var contextRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("table_theme_preset_select", selectRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("table_theme:preset:1", selectRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(11_001, selectRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("table_theme_preset_next", scrollRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("table_theme:preset:2", scrollRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(11_002, scrollRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("table_theme_preset_context", contextRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("table_theme:preset:0", contextRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(11_000, contextRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseTableThemeGalleryMouseSelectsActivePreset()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 11,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 45, 6, timestamp);
        Assert.Equal(1, state.TableThemePresetIndex);

        state = ApplyMouse(
            state,
            90,
            6,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(2, state.TableThemePresetIndex);

        state = ApplyMouse(
            state,
            3,
            6,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelUp,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.TableThemePresetIndex);
    }

    [Fact]
    public void ShowcaseTableThemeGalleryRendersActivePresetTitle()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 11,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            TableThemePresetIndex = 2
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Preset C [active]", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesDataVizPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 8,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var progress = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var metrics = ShowcaseFrameHitRegistry.HitTest(state, 3, 14);
        var narrative = ShowcaseFrameHitRegistry.HitTest(state, 90, 6);

        Assert.Equal("data_viz:progress", progress.LocalHitId);
        Assert.Equal((uint)8_000, progress.UpstreamHitId);
        Assert.Equal("data_viz:metrics_table", metrics.LocalHitId);
        Assert.Equal((uint)8_100, metrics.UpstreamHitId);
        Assert.Equal("data_viz:narrative", narrative.LocalHitId);
        Assert.Equal((uint)8_200, narrative.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsDataVizMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-data-viz-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=8", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 8,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var progressEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var metricsEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 14, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));
        var narrativeEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 6, TerminalMouseButton.Right, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, progressEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, metricsEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, narrativeEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var progressRecord = JsonDocument.Parse(lines[0]);
        using var metricsRecord = JsonDocument.Parse(lines[1]);
        using var narrativeRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("data_viz_progress_focus", progressRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("data_viz:progress", progressRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(8_000, progressRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("data_viz_metrics_scroll_down", metricsRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("data_viz:metrics_table", metricsRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(8_100, metricsRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("data_viz_narrative_context", narrativeRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("data_viz:narrative", narrativeRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(8_200, narrativeRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseDataVizMouseMutatesMetricAndNarrativeSelection()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 8,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(
            state,
            3,
            14,
            timestamp,
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.DataVizActivePanelIndex);
        Assert.Equal(1, state.DataVizMetricRowIndex);
        Assert.False(state.DataVizContextArmed);

        state = ApplyMouse(state, 90, 6, timestamp + TimeSpan.FromMilliseconds(10), TerminalMouseButton.Right);
        Assert.Equal(2, state.DataVizActivePanelIndex);
        Assert.Equal(1, state.DataVizNarrativeDetailIndex);
        Assert.True(state.DataVizContextArmed);
    }

    [Fact]
    public void ShowcaseDataVizRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 8,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            DataVizActivePanelIndex = 2,
            DataVizMetricRowIndex = 2,
            DataVizNarrativeDetailIndex = 1,
            DataVizContextArmed = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Narrative [context]", screen);
        Assert.Contains("Active panel: Narrative", screen);
        Assert.Contains("Selected metric row: 2", screen);
        Assert.Contains("Context action: armed", screen);
        Assert.Contains("Detail: metrics table selected", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesCodeExplorerPanes()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 4,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var tree = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var editor = ShowcaseFrameHitRegistry.HitTest(state, 90, 6);

        Assert.Equal("code_explorer:tree", tree.LocalHitId);
        Assert.Equal((uint)4_000, tree.UpstreamHitId);
        Assert.Equal("code_explorer:editor", editor.LocalHitId);
        Assert.Equal((uint)4_100, editor.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsCodeExplorerMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-code-explorer-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=4", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 4,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var treeEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var editorScrollEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 6, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));
        var editorContextEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 6, TerminalMouseButton.Right, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, treeEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, editorScrollEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, editorContextEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var treeRecord = JsonDocument.Parse(lines[0]);
        using var editorScrollRecord = JsonDocument.Parse(lines[1]);
        using var editorContextRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("code_explorer_tree_select", treeRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("code_explorer:tree", treeRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(4_000, treeRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("code_explorer_editor_scroll_down", editorScrollRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("code_explorer:editor", editorScrollRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(4_100, editorScrollRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("code_explorer_editor_context", editorContextRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("code_explorer:editor", editorContextRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(4_100, editorContextRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseCodeExplorerMouseMutatesTreeEditorAndContext()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 4,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 6, timestamp);
        Assert.Equal(0, state.CodeExplorerFocusIndex);
        Assert.Equal(1, state.CodeExplorerSelectedNodeIndex);

        state = ApplyMouse(
            state,
            3,
            6,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(0, state.CodeExplorerFocusIndex);
        Assert.Equal(2, state.CodeExplorerSelectedNodeIndex);

        state = ApplyMouse(
            state,
            90,
            6,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.CodeExplorerFocusIndex);
        Assert.Equal(1, state.CodeExplorerEditorScroll);

        state = ApplyMouse(
            state,
            90,
            6,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.Right);
        Assert.Equal(1, state.CodeExplorerFocusIndex);
        Assert.True(state.CodeExplorerContextArmed);
    }

    [Fact]
    public void ShowcaseCodeExplorerRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 4,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            CodeExplorerFocusIndex = 1,
            CodeExplorerSelectedNodeIndex = 4,
            CodeExplorerEditorScroll = 3,
            CodeExplorerContextArmed = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("code mouse focus=1 selected_node=4 editor_scroll=3 context=armed", screen);
        Assert.Contains("src/screens/theme_studio.rs", screen);
        Assert.Contains("scroll=3", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesShakespearePanes()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 3,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var search = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var notes = ShowcaseFrameHitRegistry.HitTest(state, 90, 6);

        Assert.Equal("shakespeare:search", search.LocalHitId);
        Assert.Equal((uint)3_000, search.UpstreamHitId);
        Assert.Equal("shakespeare:notes", notes.LocalHitId);
        Assert.Equal((uint)3_100, notes.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsShakespeareMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-shakespeare-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=3", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 3,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var searchEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var searchScrollEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));
        var notesContextEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 6, TerminalMouseButton.Right, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, searchEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, searchScrollEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, notesContextEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var searchRecord = JsonDocument.Parse(lines[0]);
        using var searchScrollRecord = JsonDocument.Parse(lines[1]);
        using var notesContextRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("shakespeare_search_focus", searchRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("shakespeare:search", searchRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(3_000, searchRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("shakespeare_search_scroll_down", searchScrollRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("shakespeare:search", searchScrollRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(3_000, searchScrollRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("shakespeare_notes_context", notesContextRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("shakespeare:notes", notesContextRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(3_100, notesContextRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseShakespeareMouseMutatesFocusQueryScrollAndContext()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 3,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 6, timestamp);
        Assert.Equal(0, state.ShakespeareFocusIndex);
        Assert.Equal(1, state.ShakespeareQueryIndex);

        state = ApplyMouse(
            state,
            3,
            6,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(0, state.ShakespeareFocusIndex);
        Assert.Equal(1, state.ShakespeareSearchScroll);

        state = ApplyMouse(
            state,
            90,
            6,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.ShakespeareFocusIndex);
        Assert.Equal(1, state.ShakespeareNotesScroll);

        state = ApplyMouse(
            state,
            90,
            6,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.Right);
        Assert.Equal(1, state.ShakespeareFocusIndex);
        Assert.True(state.ShakespeareContextArmed);
    }

    [Fact]
    public void ShowcaseShakespeareRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 3,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            ShakespeareFocusIndex = 1,
            ShakespeareQueryIndex = 2,
            ShakespeareSearchScroll = 3,
            ShakespeareNotesScroll = 4,
            ShakespeareContextArmed = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("shakespeare mouse focus=1 query_idx=2 search_scroll=3 notes_scroll=4 context=armed", screen);
        Assert.Contains("query=king", screen);
        Assert.Contains("Notes [focus]", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesDashboardPanelsWithoutReplacingLinks()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var overview = ShowcaseFrameHitRegistry.HitTest(state, 3, 4);
        var highlight = ShowcaseFrameHitRegistry.HitTest(state, 90, 18);
        var link = ShowcaseFrameHitRegistry.HitTest(state, 90, 5);

        Assert.Equal("dashboard:overview", overview.LocalHitId);
        Assert.Equal((uint)2_000, overview.UpstreamHitId);
        Assert.Equal("dashboard:highlights", highlight.LocalHitId);
        Assert.Equal((uint)2_100, highlight.UpstreamHitId);
        Assert.Equal(ShowcaseHitLayer.Pane, link.Layer);
        Assert.Equal("pane:18", link.LocalHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsDashboardPanelMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-dashboard-panels-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=2", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var overviewEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 4, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var highlightsScrollEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 18, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));
        var highlightsContextEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 18, TerminalMouseButton.Right, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, overviewEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, highlightsScrollEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, highlightsContextEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var overviewRecord = JsonDocument.Parse(lines[0]);
        using var highlightsScrollRecord = JsonDocument.Parse(lines[1]);
        using var highlightsContextRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("dashboard_overview_focus", overviewRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("dashboard:overview", overviewRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(2_000, overviewRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("dashboard_highlights_next", highlightsScrollRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("dashboard:highlights", highlightsScrollRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(2_100, highlightsScrollRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("dashboard_highlights_context", highlightsContextRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("dashboard:highlights", highlightsContextRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(2_100, highlightsContextRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseDashboardMouseMutatesOverviewHighlightsAndContext()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 4, timestamp);
        Assert.Equal(0, state.DashboardFocusIndex);
        Assert.Equal(1, state.DashboardOverviewScroll);
        Assert.False(state.DashboardContextArmed);

        state = ApplyMouse(
            state,
            90,
            18,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.DashboardFocusIndex);
        Assert.Equal(1, state.DashboardHighlightIndex);

        state = ApplyMouse(
            state,
            90,
            18,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.Right);
        Assert.Equal(1, state.DashboardFocusIndex);
        Assert.True(state.DashboardContextArmed);
    }

    [Fact]
    public void ShowcaseDashboardRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 2,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            DashboardFocusIndex = 1,
            DashboardOverviewScroll = 3,
            DashboardHighlightIndex = 5,
            DashboardContextArmed = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Overview", screen);
        Assert.Contains("scroll=3", screen);
        Assert.Contains("context=armed", screen);
        Assert.Contains("Highlights [focus]", screen);
        Assert.Contains("> Drag & Drop lab", screen);
        Assert.Contains("selected=5 focus=1", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesQuakePanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 45,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var canvas = ShowcaseFrameHitRegistry.HitTest(state, 3, 8);
        var player = ShowcaseFrameHitRegistry.HitTest(state, 90, 8);
        var renderer = ShowcaseFrameHitRegistry.HitTest(state, 90, 15);
        var controls = ShowcaseFrameHitRegistry.HitTest(state, 90, 24);

        Assert.Equal("quake:canvas", canvas.LocalHitId);
        Assert.Equal((uint)45_000, canvas.UpstreamHitId);
        Assert.Equal("quake:player", player.LocalHitId);
        Assert.Equal((uint)45_100, player.UpstreamHitId);
        Assert.Equal("quake:renderer", renderer.LocalHitId);
        Assert.Equal((uint)45_200, renderer.UpstreamHitId);
        Assert.Equal("quake:controls", controls.LocalHitId);
        Assert.Equal((uint)45_300, controls.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsQuakeMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-quake-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=45", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 45,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var canvasEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 8, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var playerScrollEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 8, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));
        var controlsContextEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 24, TerminalMouseButton.Right, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, canvasEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, playerScrollEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, controlsContextEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var canvasRecord = JsonDocument.Parse(lines[0]);
        using var playerScrollRecord = JsonDocument.Parse(lines[1]);
        using var controlsContextRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("quake_canvas_focus", canvasRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("quake:canvas", canvasRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(45_000, canvasRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("quake_player_yaw_right", playerScrollRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("quake:player", playerScrollRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(45_100, playerScrollRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("quake_panel_context", controlsContextRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("quake:controls", controlsContextRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(45_300, controlsContextRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseQuakeMouseMutatesFocusQualityYawPitchAndReset()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 45,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(
            state,
            3,
            8,
            timestamp,
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(0, state.QuakeFocusIndex);
        Assert.Equal(1, state.QuakeQualityIndex);

        state = ApplyMouse(
            state,
            90,
            8,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.QuakeFocusIndex);
        Assert.Equal(1, state.QuakeYawStep);

        state = ApplyMouse(state, 90, 8, timestamp + TimeSpan.FromMilliseconds(20));
        Assert.Equal(1, state.QuakeFocusIndex);
        Assert.Equal(1, state.QuakePitchStep);

        state = ApplyMouse(state, 3, 8, timestamp + TimeSpan.FromMilliseconds(30));
        Assert.Equal(0, state.QuakeFocusIndex);
        Assert.Equal(1, state.QuakeFireFlash);

        state = ApplyMouse(
            state,
            90,
            24,
            timestamp + TimeSpan.FromMilliseconds(40),
            TerminalMouseButton.Right);
        Assert.Equal(3, state.QuakeFocusIndex);
        Assert.Equal(1, state.QuakeResetCount);
        Assert.Equal(0, state.QuakeYawStep);
        Assert.Equal(0, state.QuakePitchStep);
        Assert.Equal(0, state.QuakeFireFlash);
    }

    [Fact]
    public void ShowcaseQuakeRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 45,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            QuakeFocusIndex = 1,
            QuakeQualityIndex = 2,
            QuakeYawStep = 3,
            QuakePitchStep = -2,
            QuakePanelScroll = 4,
            QuakeFireFlash = 5,
            QuakeResetCount = 2
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("V quality [Minimal]", screen);
        Assert.Contains("Player + Physics [focus]", screen);
        Assert.Contains("pitch=-0.16", screen);
        Assert.Contains("quality=Minimal", screen);
        Assert.Contains("flash=5 reset=2", screen);
        Assert.Contains("scroll=4", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesWidgetGalleryPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 5,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var progress = ShowcaseFrameHitRegistry.HitTest(state, 3, 4);
        var list = ShowcaseFrameHitRegistry.HitTest(state, 3, 12);
        var tabs = ShowcaseFrameHitRegistry.HitTest(state, 90, 4);
        var table = ShowcaseFrameHitRegistry.HitTest(state, 90, 12);

        Assert.Equal("widget_gallery:progress", progress.LocalHitId);
        Assert.Equal((uint)5_000, progress.UpstreamHitId);
        Assert.Equal("widget_gallery:list", list.LocalHitId);
        Assert.Equal((uint)5_100, list.UpstreamHitId);
        Assert.Equal("widget_gallery:tabs", tabs.LocalHitId);
        Assert.Equal((uint)5_200, tabs.UpstreamHitId);
        Assert.Equal("widget_gallery:table", table.LocalHitId);
        Assert.Equal((uint)5_300, table.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsWidgetGalleryMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-widget-gallery-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=5", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 5,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var listEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 12, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var tableEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 12, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));
        var tabsEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 4, TerminalMouseButton.Right, TerminalMouseKind.Down),
            timestamp + TimeSpan.FromMilliseconds(20));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, listEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, tableEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 3, frame: 3, tabsEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, lines.Length);
        using var listRecord = JsonDocument.Parse(lines[0]);
        using var tableRecord = JsonDocument.Parse(lines[1]);
        using var tabsRecord = JsonDocument.Parse(lines[2]);
        Assert.Equal("widget_gallery_item_select", listRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("widget_gallery:list", listRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(5_100, listRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("widget_gallery_table_scroll_down", tableRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("widget_gallery:table", tableRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(5_300, tableRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("widget_gallery_context_action", tabsRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("widget_gallery:tabs", tabsRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(5_200, tabsRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseWidgetGalleryMouseMutatesSelections()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 5,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(
            state,
            3,
            12,
            timestamp,
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(5, state.WidgetGalleryListIndex);
        Assert.Equal(1, state.WidgetGalleryFocusIndex);
        Assert.False(state.WidgetGalleryContextArmed);

        state = ApplyMouse(state, 90, 4, timestamp + TimeSpan.FromMilliseconds(10));
        Assert.Equal(3, state.WidgetGalleryTabIndex);
        Assert.Equal(2, state.WidgetGalleryFocusIndex);

        state = ApplyMouse(
            state,
            90,
            12,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelUp,
            TerminalMouseKind.Scroll);
        Assert.Equal(0, state.WidgetGalleryTableRow);
        Assert.Equal(3, state.WidgetGalleryFocusIndex);

        state = ApplyMouse(
            state,
            90,
            4,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.Right,
            TerminalMouseKind.Down);
        Assert.Equal(2, state.WidgetGalleryFocusIndex);
        Assert.True(state.WidgetGalleryContextArmed);
    }

    [Fact]
    public void ShowcaseWidgetGalleryRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 5,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            WidgetGalleryTabIndex = 3,
            WidgetGalleryTableRow = 3,
            WidgetGalleryFocusIndex = 2,
            WidgetGalleryContextArmed = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Tabs [context 3]", screen);
        Assert.Contains("Table [3]", screen);
        Assert.Contains("[Extras]", screen);
        Assert.Contains("Mermaid", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesLayoutLabPanels()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 6,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);

        var workspace = ShowcaseFrameHitRegistry.HitTest(state, 3, 6);
        var metrics = ShowcaseFrameHitRegistry.HitTest(state, 90, 6);

        Assert.Equal("layout_lab:workspace", workspace.LocalHitId);
        Assert.Equal((uint)6_000, workspace.UpstreamHitId);
        Assert.Equal("layout_lab:metrics", metrics.LocalHitId);
        Assert.Equal((uint)6_100, metrics.UpstreamHitId);
    }

    [Fact]
    public void ShowcaseEvidenceJsonlWriterEmitsLayoutLabMouseActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ftui-showcase-layout-lab-mouse-{Guid.NewGuid():N}.jsonl");
        var options = ShowcaseCliOptions.Parse(
            ["--screen=6", "--evidence-jsonl", path],
            _ => null);
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 6,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");
        var workspaceEvent = TerminalEvent.Mouse(
            new MouseGesture(3, 6, TerminalMouseButton.Left, TerminalMouseKind.Down),
            timestamp);
        var metricsEvent = TerminalEvent.Mouse(
            new MouseGesture(90, 6, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll),
            timestamp + TimeSpan.FromMilliseconds(10));

        using (var writer = ShowcaseEvidenceJsonlWriter.Create(options.EvidenceJsonlPath))
        {
            Assert.NotNull(writer);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 1, frame: 1, workspaceEvent, state, state);
            writer.WriteMouseEvent("input", options, RuntimeFrameStats.Empty, stepIndex: 2, frame: 2, metricsEvent, state, state);
        }

        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        using var workspaceRecord = JsonDocument.Parse(lines[0]);
        using var metricsRecord = JsonDocument.Parse(lines[1]);
        Assert.Equal("layout_lab_workspace_focus", workspaceRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("layout_lab:workspace", workspaceRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(6_000, workspaceRecord.RootElement.GetProperty("target_id").GetInt32());
        Assert.Equal("layout_lab_metrics_scroll_down", metricsRecord.RootElement.GetProperty("mouse_action").GetString());
        Assert.Equal("layout_lab:metrics", metricsRecord.RootElement.GetProperty("hit_id").GetString());
        Assert.Equal(6_100, metricsRecord.RootElement.GetProperty("target_id").GetInt32());
    }

    [Fact]
    public void ShowcaseLayoutLabMouseMutatesWorkspaceAndMetricsState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 6,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight);
        var timestamp = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        state = ApplyMouse(state, 3, 6, timestamp);
        Assert.Equal(0, state.LayoutLabFocusIndex);
        Assert.Equal(1, state.LayoutLabSelectedPaneIndex);

        state = ApplyMouse(
            state,
            3,
            6,
            timestamp + TimeSpan.FromMilliseconds(10),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(0, state.LayoutLabFocusIndex);
        Assert.Equal(1, state.LayoutLabWorkspaceZoom);

        state = ApplyMouse(
            state,
            90,
            6,
            timestamp + TimeSpan.FromMilliseconds(20),
            TerminalMouseButton.WheelDown,
            TerminalMouseKind.Scroll);
        Assert.Equal(1, state.LayoutLabFocusIndex);
        Assert.Equal(1, state.LayoutLabMetricsScroll);

        state = ApplyMouse(
            state,
            90,
            6,
            timestamp + TimeSpan.FromMilliseconds(30),
            TerminalMouseButton.Right);
        Assert.Equal(1, state.LayoutLabFocusIndex);
        Assert.True(state.LayoutLabContextArmed);
    }

    [Fact]
    public void ShowcaseLayoutLabRendersMouseSelectedState()
    {
        var state = ShowcaseDemoState.Create(
            inlineMode: false,
            viewport: new Size(120, 32),
            screenNumber: 6,
            language: "en",
            flowDirection: WidgetFlowDirection.LeftToRight) with
        {
            LayoutLabFocusIndex = 1,
            LayoutLabWorkspaceZoom = 2,
            LayoutLabMetricsScroll = 3,
            LayoutLabSelectedPaneIndex = 2,
            LayoutLabContextArmed = true
        };
        var buffer = new RenderBuffer(120, 32);

        ShowcaseSurface.Create(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(120, 32), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Pane Workspace [zoom=2]", screen);
        Assert.Contains("selected_pane_index=2", screen);
        Assert.Contains("Workspace Metrics [focus scroll=3]", screen);
        Assert.Contains("local_index=2", screen);
        Assert.Contains("Context: armed", screen);
    }

    [Fact]
    public void ShowcaseFrameHitRegistryExposesSpecificRegionsForEveryCatalogScreen()
    {
        var missingScreens = new List<string>();
        foreach (var screen in ShowcaseCatalog.Screens)
        {
            var state = ShowcaseDemoState.Create(
                inlineMode: false,
                viewport: new Size(120, 32),
                screenNumber: screen.Number,
                language: "en",
                flowDirection: WidgetFlowDirection.LeftToRight);
            var genericPaneId = $"pane:{screen.Number}";
            var hasSpecificRegion = ShowcaseFrameHitRegistry.BuildRegions(state).Any(region =>
                region.Result.Layer is ShowcaseHitLayer.Content or ShowcaseHitLayer.Link or ShowcaseHitLayer.Overlay &&
                region.Result.LocalHitId != genericPaneId &&
                region.Result.LocalHitId != "none");
            if (!hasSpecificRegion)
            {
                missingScreens.Add($"{screen.Number}:{screen.Slug}");
            }
        }

        Assert.Empty(missingScreens);
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
