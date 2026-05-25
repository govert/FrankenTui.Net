using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Layout;
using FrankenTui.Runtime;
using FrankenTui.Widgets;
using System.Globalization;

namespace FrankenTui.Demo.Showcase;

internal sealed class ShowcaseInteractiveProgram : IAppProgram<ShowcaseDemoState, ShowcaseDemoMessage>
{
    private readonly bool _inlineMode;
    private readonly int _screenNumber;
    private readonly bool _tour;
    private readonly double _tourSpeed;
    private readonly int _tourStartStep;
    private readonly string _language;
    private readonly WidgetFlowDirection _flowDirection;
    private readonly Size _initialSize;
    private readonly PaneWorkspaceState? _initialPaneWorkspace;
    private readonly ShowcasePaneWorkspaceLoadResult? _initialPaneWorkspaceLoad;
    private readonly bool _initialMouseCaptureEnabled;

    public ShowcaseInteractiveProgram(
        bool inlineMode,
        int screenNumber,
        bool tour,
        double tourSpeed,
        int tourStartStep,
        string language,
        WidgetFlowDirection flowDirection,
        Size initialSize,
        PaneWorkspaceState? initialPaneWorkspace = null,
        ShowcasePaneWorkspaceLoadResult? initialPaneWorkspaceLoad = null,
        bool initialMouseCaptureEnabled = false)
    {
        _inlineMode = inlineMode;
        _screenNumber = screenNumber;
        _tour = tour;
        _tourSpeed = tourSpeed;
        _tourStartStep = tourStartStep;
        _language = language;
        _flowDirection = flowDirection;
        _initialSize = initialSize;
        _initialPaneWorkspace = initialPaneWorkspace;
        _initialPaneWorkspaceLoad = initialPaneWorkspaceLoad;
        _initialMouseCaptureEnabled = initialMouseCaptureEnabled;
    }

    public ShowcaseDemoState Initialize() =>
        ShowcaseDemoState.Create(
            _inlineMode,
            _initialSize,
            _screenNumber,
            _language,
            _flowDirection,
            _tour,
            _tourSpeed,
            _tourStartStep,
            _initialPaneWorkspace,
            _initialPaneWorkspaceLoad,
            _initialMouseCaptureEnabled);

    public UpdateResult<ShowcaseDemoState, ShowcaseDemoMessage> Update(ShowcaseDemoState model, ShowcaseDemoMessage message) =>
        message switch
        {
            ShowcaseInputMessage input => UpdateResult<ShowcaseDemoState, ShowcaseDemoMessage>.FromModel(
                model.ApplyInput(input.Input, input.RuntimeStats)),
            ShowcaseTimerMessage timer => UpdateResult<ShowcaseDemoState, ShowcaseDemoMessage>.FromModel(
                model.ApplyTick(timer.Now, timer.RuntimeStats)),
            _ => UpdateResult<ShowcaseDemoState, ShowcaseDemoMessage>.FromModel(model)
        };

    public IRuntimeView BuildView(ShowcaseDemoState model) => ShowcaseSurface.Create(model);
}

internal sealed record ShowcaseDemoState(
    bool InlineMode,
    Size Viewport,
    int CurrentScreenNumber,
    HostedParitySession Session,
    string Language,
    WidgetFlowDirection FlowDirection,
    bool TourActive = false,
    bool TourPaused = false,
    double TourSpeed = 1.0,
    int TourStartScreen = 2,
    int TourStepIndex = 0,
    DateTimeOffset? LastTourAdvance = null,
    int ScriptFrame = 0,
    string? VfxEffect = null,
    bool EvidenceLedgerVisible = false,
    bool PerfHudVisible = false,
    bool DebugVisible = false,
    bool HelpVisible = false,
    bool A11yPanelVisible = false,
    bool A11yHighContrast = false,
    bool A11yReducedMotion = false,
    bool A11yLargeText = false,
    bool MouseCaptureEnabled = false,
    ShowcasePaletteLabMatchFilter PaletteLabMatchFilter = ShowcasePaletteLabMatchFilter.All,
    int DashboardFocusIndex = 0,
    int DashboardOverviewScroll = 0,
    int DashboardHighlightIndex = 0,
    bool DashboardContextArmed = false,
    int ShakespeareFocusIndex = 0,
    int ShakespeareQueryIndex = 0,
    int ShakespeareSearchScroll = 0,
    int ShakespeareNotesScroll = 0,
    bool ShakespeareContextArmed = false,
    int CodeExplorerFocusIndex = 0,
    int CodeExplorerSelectedNodeIndex = 0,
    int CodeExplorerEditorScroll = 0,
    bool CodeExplorerContextArmed = false,
    int LayoutLabFocusIndex = 0,
    int LayoutLabWorkspaceZoom = 0,
    int LayoutLabMetricsScroll = 0,
    int LayoutLabSelectedPaneIndex = 0,
    bool LayoutLabContextArmed = false,
    int WidgetGalleryFocusIndex = 0,
    bool WidgetGalleryContextArmed = false,
    int WidgetGalleryListIndex = 4,
    int WidgetGalleryTabIndex = 2,
    int WidgetGalleryTableRow = 1,
    int FormsInputSelectedFieldIndex = 1,
    int FormsInputFocusIndex = 0,
    int FormsInputTextScroll = 0,
    int TableThemePresetIndex = 0,
    int TerminalCapabilitiesSelectedRow = 1,
    int TerminalCapabilitiesProfileIndex = 0,
    int MacroRecorderFocusIndex = 0,
    int MacroRecorderTimelineIndex = 0,
    int MacroRecorderScenarioIndex = 0,
    bool MacroRecorderContextArmed = false,
    int PerformanceFocusIndex = 0,
    int PerformanceSelectedIndex = 0,
    bool PerformanceContextArmed = false,
    int MarkdownActivePaneIndex = 0,
    int MarkdownRendererScroll = 0,
    int MarkdownStreamScroll = 0,
    int MarkdownWrapModeIndex = 0,
    bool MarkdownContextArmed = false,
    int MermaidFocusIndex = 0,
    int MermaidSampleIndex = 0,
    int MermaidZoomStep = 0,
    int MermaidPanelScroll = 0,
    int MermaidStatusScroll = 0,
    bool MermaidContextArmed = false,
    int MermaidMegaFocusIndex = 0,
    int MermaidMegaSampleIndex = 0,
    int MermaidMegaZoomStep = 0,
    int MermaidMegaDetailScroll = 0,
    int MermaidMegaPanelScroll = 0,
    bool MermaidMegaContextArmed = false,
    int VisualEffectsFocusIndex = 0,
    int VisualEffectsEffectIndex = -1,
    int VisualEffectsHarnessScroll = 0,
    int ResponsiveFocusIndex = 0,
    int ResponsiveWidthOffset = 0,
    bool ResponsiveCustomBreakpoints = false,
    bool ResponsiveAsideForcedVisible = false,
    int LogSearchFocusIndex = 0,
    int LogSearchSelectedResultIndex = 0,
    int LogSearchSelectedDiagnosticIndex = 0,
    int LogSearchResultScroll = 0,
    int LogSearchDiagnosticsScroll = 0,
    bool LogSearchPaused = false,
    int DataVizActivePanelIndex = 0,
    int DataVizMetricRowIndex = 0,
    int DataVizNarrativeDetailIndex = 0,
    bool DataVizContextArmed = false,
    int FileBrowserSelectedRowIndex = 0,
    int FileBrowserFocusIndex = 0,
    int FileBrowserTreeScroll = 0,
    int FileBrowserPreviewScroll = 0,
    int AdvancedPatternIndex = 0,
    int AdvancedCompositeModeIndex = 0,
    int AdvancedFocusIndex = 0,
    bool AdvancedContextArmed = false,
    int NotificationsFocusIndex = 0,
    int NotificationsTriggerIndex = 0,
    int NotificationsToastIndex = 0,
    int NotificationsLifecycleScroll = 0,
    bool NotificationsContextArmed = false,
    int ActionTimelineFocusIndex = 0,
    int ActionTimelineFilterIndex = 0,
    int ActionTimelineSelectedIndex = 0,
    bool ActionTimelineDetailExpanded = false,
    bool ActionTimelineContextArmed = false,
    int IntrinsicSizingFocusIndex = 0,
    int IntrinsicSizingScenarioIndex = 0,
    int IntrinsicSizingWidthPresetIndex = 2,
    int IntrinsicSizingDetailScroll = 0,
    bool IntrinsicSizingContextArmed = false,
    int LayoutInspectorScenarioIndex = 0,
    int LayoutInspectorStepIndex = 0,
    int LayoutInspectorFocusIndex = 0,
    bool LayoutInspectorOverlayVisible = true,
    bool LayoutInspectorTreeVisible = true,
    bool LayoutInspectorContextArmed = false,
    int AdvancedTextEditorCursorLine = 8,
    int AdvancedTextEditorFocusIndex = 0,
    int AdvancedTextEditorHistoryIndex = 0,
    int AdvancedTextEditorDiagnosticsIndex = 0,
    int MousePlaygroundFocusIndex = 0,
    int MousePlaygroundSelectedTargetIndex = 0,
    int MousePlaygroundSelectedTargetClicks = 0,
    int MousePlaygroundEventIndex = 0,
    bool MousePlaygroundOverlayVisible = false,
    bool MousePlaygroundJitterStatsVisible = false,
    bool MousePlaygroundContextArmed = false,
    int FormValidationFocusIndex = 0,
    int FormValidationSelectedFieldIndex = 0,
    int FormValidationSelectedErrorIndex = 0,
    int FormValidationRulesScroll = 0,
    int FormValidationDiagnosticsScroll = 0,
    bool FormValidationOnSubmitMode = false,
    bool FormValidationSubmitted = false,
    int VirtualizedSearchSelectedIndex = 0,
    int VirtualizedSearchDiagnosticIndex = 0,
    int VirtualizedSearchDiagnosticsScroll = 0,
    bool VirtualizedSearchFocusSearch = false,
    bool VirtualizedSearchStatsFocused = false,
    int AsyncTasksSelectedIndex = 0,
    int AsyncTasksFocusedPanelIndex = 0,
    int AsyncTasksHazardScroll = 0,
    int AsyncTasksPolicyIndex = 2,
    bool AsyncTasksAgingEnabled = true,
    int ThemeStudioPresetIndex = 0,
    int ThemeStudioTokenIndex = 0,
    int ThemeStudioFocusIndex = 0,
    int ThemeStudioDiagnosticsScroll = 0,
    bool ThemeStudioExportArmed = false,
    int SnapshotPlayerFrameIndex = -1,
    int SnapshotPlayerFocusIndex = 0,
    int SnapshotPlayerDiagnosticsScroll = 0,
    int SnapshotPlayerCompareIndex = 0,
    bool SnapshotPlayerMarkerEnabled = false,
    bool SnapshotPlayerHeatmapEnabled = true,
    bool SnapshotPlayerPlaying = false,
    bool SnapshotPlayerTimelineScrubbing = false,
    int PerformanceChallengeFocusIndex = 0,
    int PerformanceChallengeForcedTierIndex = -1,
    int PerformanceChallengeStressModeIndex = 1,
    int PerformanceChallengeStressLoad = 0,
    int PerformanceChallengeBudgetMs = 17,
    int PerformanceChallengeSparklineModeIndex = 0,
    int PerformanceChallengeEvidenceScroll = 0,
    bool PerformanceChallengePaused = false,
    int ExplainabilityFocusIndex = 0,
    int ExplainabilityTimelineScroll = 0,
    int ExplainabilitySourceScroll = 0,
    bool ExplainabilityPaused = false,
    bool ExplainabilityOverlayMode = false,
    bool ExplainabilityAutoRefresh = true,
    int I18nLocaleIndex = 0,
    int I18nFocusIndex = 0,
    int I18nPluralCount = 1,
    int I18nStressSampleIndex = 0,
    bool I18nRtlEnabled = false,
    bool I18nExportArmed = false,
    int VoiOverlayFocusIndex = 0,
    int VoiOverlayLedgerIndex = 0,
    int VoiOverlayControlsScroll = 0,
    int VoiOverlayResetCount = 0,
    bool VoiOverlayDetailExpanded = false,
    bool VoiOverlayVisible = true,
    int InlineModeFocusIndex = 0,
    int InlineModeLogRateIndex = -1,
    int InlineModeUiHeightIndex = -1,
    int InlineModeStateScroll = 0,
    bool InlineModeCompareEnabled = false,
    bool InlineModePaused = false,
    bool InlineModeAnchorBottom = true,
    int AccessibilityFocusIndex = 0,
    int AccessibilitySelectedToggleIndex = 0,
    int AccessibilityPreviewScroll = 0,
    int AccessibilityTelemetryScroll = 0,
    IReadOnlyList<ShowcaseA11yTelemetryEntry>? AccessibilityTelemetryEvents = null,
    int WidgetBuilderPresetIndex = -1,
    int WidgetBuilderSelectedIndex = -1,
    int WidgetBuilderFocusIndex = 0,
    int WidgetBuilderTreeScroll = 0,
    int WidgetBuilderPropsScroll = 0,
    int WidgetBuilderValue = -1,
    bool WidgetBuilderPreviewEnabled = true,
    bool WidgetBuilderBorderEnabled = true,
    bool WidgetBuilderTitleEnabled = true,
    int WidgetBuilderAccentIndex = 0,
    bool WidgetBuilderPresetSaved = false,
    bool WidgetBuilderExportArmed = false,
    int DeterminismFocusIndex = 0,
    int DeterminismStrategyIndex = 1,
    int DeterminismScenarioIndex = 0,
    int DeterminismSeedOffset = 0,
    int DeterminismReportScroll = 0,
    int DeterminismChecksScroll = 0,
    int DeterminismRunCount = 0,
    bool DeterminismFaultEnabled = false,
    bool DeterminismExportArmed = false,
    bool DeterminismPaused = false,
    bool DeterminismChecksumLogged = false,
    int HyperlinkFocusIndex = 0,
    int HyperlinkHoverIndex = -1,
    int HyperlinkLastActionIndex = 0,
    int HyperlinkActivationCount = 0,
    bool HyperlinkCopied = false,
    int MarkdownLiveFocusIndex = 0,
    int MarkdownLivePreviewScroll = 0,
    int MarkdownLiveSearchMatchIndex = 0,
    int MarkdownLiveCursorLine = 6,
    bool MarkdownLiveDiffMode = false,
    int DragDropModeIndex = 0,
    int DragDropSelectedIndex = 0,
    int DragDropFocusedList = 0,
    int DragDropMoveCount = 0,
    bool DragDropKeyboardActive = false,
    bool DragDropContextAction = false,
    int QuakeFocusIndex = 0,
    int QuakeQualityIndex = 0,
    int QuakeYawStep = 0,
    int QuakePitchStep = 0,
    int QuakePanelScroll = 0,
    int QuakeFireFlash = 0,
    int QuakeResetCount = 0,
    bool PaletteLabBenchEnabled = false,
    int PaletteLabBenchFrame = 0,
    int PaletteLabBenchProcessed = 0,
    bool PaneWorkspaceLoaded = false,
    string? PaneWorkspaceRecoveryError = null,
    string? PaneWorkspaceInvalidSnapshotPath = null,
    ShowcaseKanbanState? KanbanBoard = null,
    RuntimeFrameStats? RuntimeStats = null,
    bool QuitRequested = false)
{
    public ShowcaseScreen CurrentScreen => ShowcaseCatalog.Get(CurrentScreenNumber);

    public ShowcaseTourCallout? TourCallout =>
        TourActive ? ShowcaseTourStoryboard.At(TourStepIndex) : null;

    public static ShowcaseDemoState Create(
        bool inlineMode,
        Size viewport,
        int screenNumber,
        string language,
        WidgetFlowDirection flowDirection,
        bool tour = false,
        double tourSpeed = 1.0,
        int tourStartStep = 1,
        PaneWorkspaceState? paneWorkspace = null,
        ShowcasePaneWorkspaceLoadResult? paneWorkspaceLoad = null,
        bool mouseCaptureEnabled = false,
        string? vfxEffect = null)
    {
        var session = HostedParitySession.Create(inlineMode, HostedParityScenarioId.Extras, language, flowDirection) with
        {
            PaneWorkspace = paneWorkspace ?? PaneWorkspaceState.CreateDemo()
        };
        var current = ShowcaseCatalog.ClampScreenNumber(screenNumber);
        var startScreen = Math.Clamp(tourStartStep <= 1 ? 2 : tourStartStep, 2, ShowcaseCatalog.Screens.Count);
        var model = new ShowcaseDemoState(
            inlineMode,
            viewport,
            current,
            session,
            language,
            flowDirection,
            TourSpeed: Math.Clamp(tourSpeed, 0.25, 4.0),
            TourStartScreen: startScreen,
            PaneWorkspaceLoaded: paneWorkspaceLoad?.Loaded ?? false,
            PaneWorkspaceRecoveryError: paneWorkspaceLoad?.Error,
            PaneWorkspaceInvalidSnapshotPath: paneWorkspaceLoad?.InvalidSnapshotPath,
            KanbanBoard: ShowcaseKanbanState.CreateDefault(),
            MouseCaptureEnabled: mouseCaptureEnabled,
            VfxEffect: NormalizeVfxEffect(vfxEffect));
        return tour ? model.StartTour(DateTimeOffset.UtcNow) : model;
    }

    private static string? NormalizeVfxEffect(string? effect)
    {
        return ShowcaseVfxEffects.NormalizeHarnessInput(effect);
    }

    public KeybindingState CreateKeybindingState() =>
        new(
            InputNonEmpty: !string.IsNullOrWhiteSpace(Session.InputBuffer) || Session.CommandPalette.IsOpen,
            TaskRunning: false,
            ModalOpen: Session.CommandPalette.IsOpen,
            ViewOverlay: EvidenceLedgerVisible || PerfHudVisible || DebugVisible || HelpVisible || A11yPanelVisible || TourActive);

    public ShowcaseDemoState RecordA11yTelemetry(string kind)
    {
        var tick = RuntimeStats?.StepIndex ?? ScriptFrame;
        var events = AccessibilityTelemetryEvents ?? [];
        var nextEvents = events
            .Append(new ShowcaseA11yTelemetryEntry(kind, tick, A11yHighContrast, A11yReducedMotion, A11yLargeText))
            .TakeLast(6)
            .ToArray();
        return this with { AccessibilityTelemetryEvents = nextEvents };
    }

    public ShowcaseDemoState AdvanceScript(int frames)
    {
        var next = this;
        var now = DateTimeOffset.UtcNow;
        for (var index = 0; index < Math.Max(frames, 0); index++)
        {
            now += TimeSpan.FromMilliseconds(350);
            next = next.ApplyTick(now, RuntimeFrameStats.Empty) with { ScriptFrame = next.ScriptFrame + 1 };
        }

        return next;
    }

    public ShowcaseDemoState ApplyInput(RuntimeInputEnvelope input, RuntimeFrameStats runtimeStats)
    {
        ArgumentNullException.ThrowIfNull(input);

        var next = this with
        {
            Viewport = input.ResizeToApply ?? Viewport,
            RuntimeStats = runtimeStats,
            QuitRequested = QuitRequested || input.QuitRequested
        };

        if (input.EffectiveEvent is null)
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is ResizeTerminalEvent resize)
        {
            next = next with { Viewport = resize.Size };
        }

        if (input.EffectiveEvent is MouseTerminalEvent tourMouseEvent &&
            HandleTourMouse(tourMouseEvent, input.Timestamp, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent overlayMouseEvent &&
            HandleOverlayMouse(overlayMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent statusMouseEvent &&
            HandleStatusMouse(statusMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent mouseEvent &&
            HandleChromeMouse(mouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent dashboardMouseEvent &&
            HandleDashboardMouse(dashboardMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent shakespeareMouseEvent &&
            HandleShakespeareMouse(shakespeareMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent codeExplorerMouseEvent &&
            HandleCodeExplorerMouse(codeExplorerMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent layoutLabMouseEvent &&
            HandleLayoutLabMouse(layoutLabMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent paletteLabMouseEvent &&
            HandlePaletteLabMouse(paletteLabMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent tableThemeMouseEvent &&
            HandleTableThemeGalleryMouse(tableThemeMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent dataVizMouseEvent &&
            HandleDataVizMouse(dataVizMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent fileBrowserMouseEvent &&
            HandleFileBrowserMouse(fileBrowserMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent advancedMouseEvent &&
            HandleAdvancedMouse(advancedMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent notificationsMouseEvent &&
            HandleNotificationsMouse(notificationsMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent actionTimelineMouseEvent &&
            HandleActionTimelineMouse(actionTimelineMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent intrinsicSizingMouseEvent &&
            HandleIntrinsicSizingMouse(intrinsicSizingMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent layoutInspectorMouseEvent &&
            HandleLayoutInspectorMouse(layoutInspectorMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent advancedTextEditorMouseEvent &&
            HandleAdvancedTextEditorMouse(advancedTextEditorMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent mousePlaygroundMouseEvent &&
            HandleMousePlaygroundMouse(mousePlaygroundMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent formValidationMouseEvent &&
            HandleFormValidationMouse(formValidationMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent virtualizedSearchMouseEvent &&
            HandleVirtualizedSearchMouse(virtualizedSearchMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent asyncTasksMouseEvent &&
            HandleAsyncTasksMouse(asyncTasksMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent themeStudioMouseEvent &&
            HandleThemeStudioMouse(themeStudioMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent snapshotPlayerMouseEvent &&
            HandleSnapshotPlayerMouse(snapshotPlayerMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent performanceChallengeMouseEvent &&
            HandlePerformanceChallengeMouse(performanceChallengeMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent explainabilityMouseEvent &&
            HandleExplainabilityMouse(explainabilityMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent i18nMouseEvent &&
            HandleI18nMouse(i18nMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent voiOverlayMouseEvent &&
            HandleVoiOverlayMouse(voiOverlayMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent inlineModeMouseEvent &&
            HandleInlineModeMouse(inlineModeMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent accessibilityMouseEvent &&
            HandleAccessibilityMouse(accessibilityMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent widgetBuilderMouseEvent &&
            HandleWidgetBuilderMouse(widgetBuilderMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent determinismMouseEvent &&
            HandleDeterminismMouse(determinismMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent hyperlinkMouseEvent &&
            HandleHyperlinkMouse(hyperlinkMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent terminalCapabilitiesMouseEvent &&
            HandleTerminalCapabilitiesMouse(terminalCapabilitiesMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent widgetGalleryMouseEvent &&
            HandleWidgetGalleryMouse(widgetGalleryMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent formsInputMouseEvent &&
            HandleFormsInputMouse(formsInputMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent macroRecorderMouseEvent &&
            HandleMacroRecorderMouse(macroRecorderMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent performanceMouseEvent &&
            HandlePerformanceMouse(performanceMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent markdownMouseEvent &&
            HandleMarkdownMouse(markdownMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent mermaidMouseEvent &&
            HandleMermaidMouse(mermaidMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent mermaidMegaMouseEvent &&
            HandleMermaidMegaMouse(mermaidMegaMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent visualEffectsMouseEvent &&
            HandleVisualEffectsMouse(visualEffectsMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent responsiveMouseEvent &&
            HandleResponsiveMouse(responsiveMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent logSearchMouseEvent &&
            HandleLogSearchMouse(logSearchMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent markdownLiveMouseEvent &&
            HandleMarkdownLiveMouse(markdownLiveMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent dragDropMouseEvent &&
            HandleDragDropMouse(dragDropMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent quakeMouseEvent &&
            HandleQuakeMouse(quakeMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent kanbanMouseEvent &&
            HandleKanbanBoardMouse(kanbanMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent paneMouseEvent &&
            HandlePaneMouse(paneMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is not KeyTerminalEvent keyEvent)
        {
            return SyncSession(next, next.Session.Advance(input).WithRuntimeStats(runtimeStats));
        }

        if (HandleShowcasePaletteKey(keyEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (HandleKanbanBoardKey(keyEvent.Gesture, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (DispatchScreenKey(keyEvent.Gesture, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (HandleGlobalKey(keyEvent, input.Timestamp, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        return SyncSession(next, next.Session.Advance(input).WithRuntimeStats(runtimeStats));
    }

    public ShowcaseDemoState ApplyTick(DateTimeOffset now, RuntimeFrameStats runtimeStats)
    {
        var nextSession = Session.AdvanceTime(now).WithRuntimeStats(runtimeStats);
        var next = SyncSession(this with { RuntimeStats = runtimeStats }, nextSession);
        next = AdvancePaletteLabBench(next);
        if (!next.TourActive || next.TourPaused)
        {
            return next;
        }

        var interval = TimeSpan.FromMilliseconds(Math.Max(220, 1000 / Math.Max(next.TourSpeed, 0.25)));
        if (next.LastTourAdvance is { } last && now - last < interval)
        {
            return next;
        }

        if (next.TourStepIndex + 1 >= ShowcaseTourStoryboard.Count)
        {
            return next with { TourActive = false, TourPaused = false, LastTourAdvance = now };
        }

        return next.MoveTourStep(1, now);
    }

    private static ShowcaseDemoState SyncSession(ShowcaseDemoState state, HostedParitySession session) =>
        state with { Session = session };

    private static ShowcaseDemoState AdvancePaletteLabBench(ShowcaseDemoState state)
    {
        if (state.CurrentScreenNumber != 39 ||
            state.Session.CommandPalette.IsOpen ||
            state.TourActive ||
            !state.PaletteLabBenchEnabled)
        {
            return state;
        }

        var nextFrame = state.PaletteLabBenchFrame + 1;
        var nextProcessed = nextFrame > 0 && nextFrame % ShowcaseSurface.PaletteLabBenchStepTicks == 0
            ? state.PaletteLabBenchProcessed + 1
            : state.PaletteLabBenchProcessed;
        return state with
        {
            PaletteLabBenchFrame = nextFrame,
            PaletteLabBenchProcessed = nextProcessed
        };
    }

    private static bool HandleShowcasePaletteKey(KeyTerminalEvent keyEvent, ref ShowcaseDemoState next)
    {
        var gesture = keyEvent.Gesture;
        if (IsControlCharacter(gesture, 'k'))
        {
            var palette = CommandPaletteController.Toggle(next.Session.CommandPalette);
            next = SyncSession(next, next.Session with
            {
                CommandPalette = palette,
                InputState = next.Session.InputState.Announce(palette.IsOpen ? "Command palette opened" : "Command palette closed")
            });
            return true;
        }

        if (!next.Session.CommandPalette.IsOpen)
        {
            return false;
        }

        var applied = CommandPaletteController.Apply(
            next.Session.CommandPalette,
            keyEvent,
            ShowcaseCommandPalette.Entries());
        var session = next.Session with
        {
            CommandPalette = applied.State
        };

        if (applied.Execution is { } execution &&
            ShowcaseCommandPalette.TryResolveScreen(execution, out var screenNumber))
        {
            var screen = ShowcaseCatalog.Get(screenNumber);
            session = session with
            {
                InputState = session.InputState.Announce($"Screen {screen.Number}: {screen.Title} opened")
            };
            next = next with
            {
                CurrentScreenNumber = screen.Number,
                TourActive = false,
                TourPaused = false,
                Session = session
            };
            return true;
        }

        next = SyncSession(next, session);
        return true;
    }

    private static readonly Dictionary<int, (char Key, Func<ShowcaseDemoState, ShowcaseDemoState> Mutation)[]> ScreenKeyMap = new()
    {
        [1] = [ ( 'j', s => s with { TourStartScreen = Math.Clamp(s.TourStartScreen + 1, 2, 45) } ), ( 'k', s => s with { TourStartScreen = Math.Clamp(s.TourStartScreen - 1, 2, 45) } ), ],
        [2] = [ ( 'j', s => s with { DashboardHighlightIndex = (s.DashboardHighlightIndex + 1) % 8, DashboardFocusIndex = 1 } ), ( 'k', s => s with { DashboardHighlightIndex = (s.DashboardHighlightIndex + 7) % 8, DashboardFocusIndex = 1 } ), ],
        [3] = [ ( 'j', s => s with { ShakespeareQueryIndex = (s.ShakespeareQueryIndex + 1) % 4, ShakespeareFocusIndex = 0 } ), ( 'k', s => s with { ShakespeareQueryIndex = (s.ShakespeareQueryIndex + 3) % 4, ShakespeareFocusIndex = 0 } ), ],
        [4] = [ ( 'j', s => s with { CodeExplorerSelectedNodeIndex = Math.Clamp(s.CodeExplorerSelectedNodeIndex + 1, 0, 6), CodeExplorerFocusIndex = 0 } ), ( 'k', s => s with { CodeExplorerSelectedNodeIndex = Math.Clamp(s.CodeExplorerSelectedNodeIndex - 1, 0, 6), CodeExplorerFocusIndex = 0 } ), ],
        [5] = [ ( 'j', s => s with { WidgetGalleryListIndex = Math.Clamp(s.WidgetGalleryListIndex + 1, 0, 6), WidgetGalleryFocusIndex = 1 } ), ( 'k', s => s with { WidgetGalleryListIndex = Math.Clamp(s.WidgetGalleryListIndex - 1, 0, 6), WidgetGalleryFocusIndex = 1 } ), ],
        [6] = [ ( 'j', s => s with { LayoutLabSelectedPaneIndex = (s.LayoutLabSelectedPaneIndex + 1) % 4, LayoutLabFocusIndex = 0 } ), ( 'k', s => s with { LayoutLabSelectedPaneIndex = (s.LayoutLabSelectedPaneIndex + 3) % 4, LayoutLabFocusIndex = 0 } ), ( 'z', s => s with { LayoutLabWorkspaceZoom = Math.Clamp(s.LayoutLabWorkspaceZoom + 1, -3, 3), LayoutLabFocusIndex = 0 } ), ],
        [7] = [ ( 'j', s => s with { FormsInputSelectedFieldIndex = (s.FormsInputSelectedFieldIndex + 1) % 3, FormsInputFocusIndex = 0 } ), ( 'k', s => s with { FormsInputSelectedFieldIndex = (s.FormsInputSelectedFieldIndex + 2) % 3, FormsInputFocusIndex = 0 } ), ],
        [8] = [ ( 'j', s => s with { DataVizMetricRowIndex = Math.Clamp(s.DataVizMetricRowIndex + 1, 0, 3), DataVizActivePanelIndex = 1 } ), ( 'k', s => s with { DataVizMetricRowIndex = Math.Clamp(s.DataVizMetricRowIndex - 1, 0, 3), DataVizActivePanelIndex = 1 } ), ],
        [9] = [ ( 'h', s => s with { FileBrowserFocusIndex = 0 } ), ( 'l', s => s with { FileBrowserFocusIndex = 1 } ), ( 'j', s => s with { FileBrowserSelectedRowIndex = Math.Clamp(s.FileBrowserSelectedRowIndex + 1, 0, 5), FileBrowserFocusIndex = 0 } ), ( 'k', s => s with { FileBrowserSelectedRowIndex = Math.Clamp(s.FileBrowserSelectedRowIndex - 1, 0, 5), FileBrowserFocusIndex = 0 } ), ( 'g', s => s with { FileBrowserSelectedRowIndex = 0, FileBrowserFocusIndex = 0 } ), ],
        [10] = [ ( 'j', s => s with { AdvancedPatternIndex = Math.Clamp(s.AdvancedPatternIndex + 1, 0, 4), AdvancedFocusIndex = 0 } ), ( 'k', s => s with { AdvancedPatternIndex = Math.Clamp(s.AdvancedPatternIndex - 1, 0, 4), AdvancedFocusIndex = 0 } ), ],
        [11] = [ ( 'j', s => s with { TableThemePresetIndex = (s.TableThemePresetIndex + 1) % 3 } ), ( 'k', s => s with { TableThemePresetIndex = (s.TableThemePresetIndex + 2) % 3 } ), ],
        [13] = [ ( 'r', s => s with { MacroRecorderFocusIndex = 1, MacroRecorderTimelineIndex = 0, MacroRecorderScenarioIndex = 0 } ), ( ' ', s => s with { MacroRecorderScenarioIndex = (s.MacroRecorderScenarioIndex + 1) % 3, MacroRecorderFocusIndex = 3 } ), ( 'j', s => s with { MacroRecorderTimelineIndex = Math.Clamp(s.MacroRecorderTimelineIndex + 1, 0, 4), MacroRecorderFocusIndex = 1 } ), ( 'k', s => s with { MacroRecorderTimelineIndex = Math.Clamp(s.MacroRecorderTimelineIndex - 1, 0, 4), MacroRecorderFocusIndex = 1 } ), ],
        [14] = [ ( 'j', s => s with { PerformanceSelectedIndex = Math.Clamp(s.PerformanceSelectedIndex + 1, 0, 9999), PerformanceFocusIndex = 1 } ), ( 'k', s => s with { PerformanceSelectedIndex = Math.Clamp(s.PerformanceSelectedIndex - 1, 0, 9999), PerformanceFocusIndex = 1 } ), ( 'g', s => s with { PerformanceSelectedIndex = 0, PerformanceFocusIndex = 1 } ), ],
        [15] = [ ( 'j', s => s with { MarkdownRendererScroll = Math.Clamp(s.MarkdownRendererScroll + 1, 0, 12), MarkdownActivePaneIndex = 0 } ), ( 'k', s => s with { MarkdownRendererScroll = Math.Clamp(s.MarkdownRendererScroll - 1, 0, 12), MarkdownActivePaneIndex = 0 } ), ( 'w', s => s with { MarkdownWrapModeIndex = (s.MarkdownWrapModeIndex + 1) % 3, MarkdownActivePaneIndex = 5 } ), ],
        [16] = [ ( 'j', s => s with { MermaidSampleIndex = Math.Clamp(s.MermaidSampleIndex + 1, 0, MermaidShowcaseSurface.Catalog().Count - 1), MermaidFocusIndex = 0 } ), ( 'k', s => s with { MermaidSampleIndex = Math.Clamp(s.MermaidSampleIndex - 1, 0, MermaidShowcaseSurface.Catalog().Count - 1), MermaidFocusIndex = 0 } ), ],
        [17] = [ ( 'j', s => s with { MermaidMegaSampleIndex = Math.Clamp(s.MermaidMegaSampleIndex + 1, 0, MermaidShowcaseSurface.Catalog().Count - 1), MermaidMegaFocusIndex = 0 } ), ( 'k', s => s with { MermaidMegaSampleIndex = Math.Clamp(s.MermaidMegaSampleIndex - 1, 0, MermaidShowcaseSurface.Catalog().Count - 1), MermaidMegaFocusIndex = 0 } ), ],
        [18] = [ ( 'j', s => s with { VisualEffectsEffectIndex = (ResolveVisualEffectsEffectIndex(s) + 1) % ShowcaseVfxEffects.AllCanonicalKeys.Length, VisualEffectsFocusIndex = 0 } ), ( 'k', s => s with { VisualEffectsEffectIndex = (ResolveVisualEffectsEffectIndex(s) + ShowcaseVfxEffects.AllCanonicalKeys.Length - 1) % ShowcaseVfxEffects.AllCanonicalKeys.Length, VisualEffectsFocusIndex = 0 } ), ],
        [19] = [ ( 'b', s => s with { ResponsiveCustomBreakpoints = !s.ResponsiveCustomBreakpoints, ResponsiveFocusIndex = 0 } ), ( 'a', s => s with { ResponsiveAsideForcedVisible = !s.ResponsiveAsideForcedVisible, ResponsiveFocusIndex = 0 } ), ],
        [20] = [ ( 'j', s => s with { LogSearchSelectedResultIndex = Math.Clamp(s.LogSearchSelectedResultIndex + 1, 0, 99), LogSearchFocusIndex = 0 } ), ( 'k', s => s with { LogSearchSelectedResultIndex = Math.Clamp(s.LogSearchSelectedResultIndex - 1, 0, 99), LogSearchFocusIndex = 0 } ), ( 'p', s => s with { LogSearchPaused = !s.LogSearchPaused, LogSearchFocusIndex = 0 } ), ],
        [21] = [ ( 'j', s => s with { NotificationsTriggerIndex = Math.Clamp(s.NotificationsTriggerIndex + 1, 0, 5), NotificationsFocusIndex = 0 } ), ( 'k', s => s with { NotificationsTriggerIndex = Math.Clamp(s.NotificationsTriggerIndex - 1, 0, 5), NotificationsFocusIndex = 0 } ), ],
        [22] = [ ( 'j', s => s with { ActionTimelineSelectedIndex = Math.Clamp(s.ActionTimelineSelectedIndex + 1, 0, 7), ActionTimelineFocusIndex = 1 } ), ( 'k', s => s with { ActionTimelineSelectedIndex = Math.Clamp(s.ActionTimelineSelectedIndex - 1, 0, 7), ActionTimelineFocusIndex = 1 } ), ( 'f', s => s with { ActionTimelineFilterIndex = (s.ActionTimelineFilterIndex + 1) % 4, ActionTimelineFocusIndex = 0 } ), ],
        [23] = [ ( 'j', s => s with { IntrinsicSizingScenarioIndex = (s.IntrinsicSizingScenarioIndex + 1) % 4, IntrinsicSizingFocusIndex = 1 } ), ( 'k', s => s with { IntrinsicSizingScenarioIndex = (s.IntrinsicSizingScenarioIndex + 3) % 4, IntrinsicSizingFocusIndex = 1 } ), ( 'w', s => s with { IntrinsicSizingWidthPresetIndex = (s.IntrinsicSizingWidthPresetIndex + 1) % 4, IntrinsicSizingFocusIndex = 3 } ), ],
        [24] = [ ( 'j', s => s with { LayoutInspectorStepIndex = (s.LayoutInspectorStepIndex + 1) % 3, LayoutInspectorFocusIndex = 0 } ), ( 'k', s => s with { LayoutInspectorStepIndex = (s.LayoutInspectorStepIndex + 2) % 3, LayoutInspectorFocusIndex = 0 } ), ( 's', s => s with { LayoutInspectorScenarioIndex = (s.LayoutInspectorScenarioIndex + 1) % 4, LayoutInspectorFocusIndex = 0 } ), ],
        [25] = [ ( 'j', s => s with { AdvancedTextEditorHistoryIndex = Math.Clamp(s.AdvancedTextEditorHistoryIndex + 1, 0, 5), AdvancedTextEditorFocusIndex = 2 } ), ( 'k', s => s with { AdvancedTextEditorHistoryIndex = Math.Clamp(s.AdvancedTextEditorHistoryIndex - 1, 0, 5), AdvancedTextEditorFocusIndex = 2 } ), ( 'u', s => s with { AdvancedTextEditorFocusIndex = s.AdvancedTextEditorFocusIndex == 2 ? 0 : 2 } ), ],
        [26] = [ ( 'j', s => s with { MousePlaygroundSelectedTargetIndex = Math.Clamp(s.MousePlaygroundSelectedTargetIndex + 1, 0, 11), MousePlaygroundFocusIndex = 2 } ), ( 'k', s => s with { MousePlaygroundSelectedTargetIndex = Math.Clamp(s.MousePlaygroundSelectedTargetIndex - 1, 0, 11), MousePlaygroundFocusIndex = 2 } ), ( 'o', s => s with { MousePlaygroundOverlayVisible = !s.MousePlaygroundOverlayVisible, MousePlaygroundFocusIndex = 2 } ), ],
        [27] = [ ( 'j', s => s with { FormValidationSelectedFieldIndex = Math.Clamp(s.FormValidationSelectedFieldIndex + 1, 0, 8), FormValidationFocusIndex = 1 } ), ( 'k', s => s with { FormValidationSelectedFieldIndex = Math.Clamp(s.FormValidationSelectedFieldIndex - 1, 0, 8), FormValidationFocusIndex = 1 } ), ( 'm', s => s with { FormValidationOnSubmitMode = !s.FormValidationOnSubmitMode, FormValidationFocusIndex = 0 } ), ],
        [29] = [ ( 'n', s => s with { AsyncTasksPolicyIndex = (s.AsyncTasksPolicyIndex + 1) % 6, AsyncTasksFocusedPanelIndex = 0 } ), ( 'c', s => s with { AsyncTasksPolicyIndex = (s.AsyncTasksPolicyIndex + 5) % 6, AsyncTasksFocusedPanelIndex = 0 } ), ( 's', s => s with { AsyncTasksPolicyIndex = (s.AsyncTasksPolicyIndex + 1) % 6, AsyncTasksFocusedPanelIndex = 0 } ), ( 'a', s => s with { AsyncTasksAgingEnabled = !s.AsyncTasksAgingEnabled, AsyncTasksFocusedPanelIndex = 6 } ), ( 'r', s => s with { AsyncTasksPolicyIndex = 2, AsyncTasksAgingEnabled = true, AsyncTasksSelectedIndex = 0, AsyncTasksHazardScroll = 0, AsyncTasksFocusedPanelIndex = 1 } ), ( 'j', s => s with { AsyncTasksSelectedIndex = Math.Clamp(s.AsyncTasksSelectedIndex + 1, 0, 7), AsyncTasksFocusedPanelIndex = 1 } ), ( 'k', s => s with { AsyncTasksSelectedIndex = Math.Clamp(s.AsyncTasksSelectedIndex - 1, 0, 7), AsyncTasksFocusedPanelIndex = 1 } ), ],
        [30] = [ ( 'e', s => s with { ThemeStudioExportArmed = !s.ThemeStudioExportArmed, ThemeStudioFocusIndex = 2 } ), ],
        [31] = [ ( 'j', s => s with { SnapshotPlayerFrameIndex = Math.Clamp(s.SnapshotPlayerFrameIndex + 1, 0, Math.Max(s.ScriptFrame, 0)), SnapshotPlayerFocusIndex = 0 } ), ( 'k', s => s with { SnapshotPlayerFrameIndex = Math.Clamp(s.SnapshotPlayerFrameIndex - 1, 0, Math.Max(s.ScriptFrame, 0)), SnapshotPlayerFocusIndex = 0 } ), ( 'g', s => s with { SnapshotPlayerFrameIndex = 0, SnapshotPlayerFocusIndex = 0 } ), ],
        [32] = [ ( 'j', s => s with { PerformanceChallengeForcedTierIndex = Math.Clamp(s.PerformanceChallengeForcedTierIndex + 1, -1, 3), PerformanceChallengeFocusIndex = 6 } ), ( 'k', s => s with { PerformanceChallengeForcedTierIndex = Math.Clamp(s.PerformanceChallengeForcedTierIndex - 1, -1, 3), PerformanceChallengeFocusIndex = 6 } ), ( 's', s => s with { PerformanceChallengeStressModeIndex = (s.PerformanceChallengeStressModeIndex + 1) % 4, PerformanceChallengeFocusIndex = 5 } ), ],
        [33] = [ ( 'j', s => s with { ExplainabilityTimelineScroll = Math.Clamp(s.ExplainabilityTimelineScroll + 1, 0, 8), ExplainabilityFocusIndex = 4 } ), ( 'k', s => s with { ExplainabilityTimelineScroll = Math.Clamp(s.ExplainabilityTimelineScroll - 1, 0, 8), ExplainabilityFocusIndex = 4 } ), ( 'p', s => s with { ExplainabilityPaused = !s.ExplainabilityPaused, ExplainabilityAutoRefresh = s.ExplainabilityPaused, ExplainabilityFocusIndex = 0 } ), ],
        [34] = [ ( 'j', s => s with { I18nStressSampleIndex = (s.I18nStressSampleIndex + 1) % 4, I18nFocusIndex = 4 } ), ( 'k', s => s with { I18nStressSampleIndex = (s.I18nStressSampleIndex + 3) % 4, I18nFocusIndex = 4 } ), ( 'l', s => s with { I18nLocaleIndex = (s.I18nLocaleIndex + 1) % 5, I18nRtlEnabled = (s.I18nLocaleIndex + 1) % 5 == 4, I18nFocusIndex = 0 } ), ],
        [35] = [ ( 'j', s => s with { VoiOverlayLedgerIndex = (s.VoiOverlayLedgerIndex + 1) % 3, VoiOverlayFocusIndex = 4 } ), ( 'k', s => s with { VoiOverlayLedgerIndex = (s.VoiOverlayLedgerIndex + 2) % 3, VoiOverlayFocusIndex = 4 } ), ( 'r', s => s with { VoiOverlayResetCount = s.VoiOverlayResetCount + 1, VoiOverlayLedgerIndex = 0, VoiOverlayFocusIndex = 4 } ), ],
        [36] = [ ( 'j', s => s with { InlineModeLogRateIndex = Math.Clamp(ResolveInlineModeLogRateIndex(s) + 1, 0, 3), InlineModeFocusIndex = 3 } ), ( 'k', s => s with { InlineModeLogRateIndex = Math.Clamp(ResolveInlineModeLogRateIndex(s) - 1, 0, 3), InlineModeFocusIndex = 3 } ), ( 'p', s => s with { InlineModePaused = !s.InlineModePaused, InlineModeFocusIndex = 3 } ), ],
        [37] = [ ( 'j', s => s with { AccessibilityPreviewScroll = Math.Clamp(s.AccessibilityPreviewScroll + 1, 0, 8), AccessibilityFocusIndex = 2 } ), ( 'k', s => s with { AccessibilityPreviewScroll = Math.Clamp(s.AccessibilityPreviewScroll - 1, 0, 8), AccessibilityFocusIndex = 2 } ), ],
    };

    private static bool DispatchScreenKey(KeyGesture gesture, ref ShowcaseDemoState next)
    {
        if (next.Session.CommandPalette.IsOpen || next.EvidenceLedgerVisible ||
            next.PerfHudVisible || next.DebugVisible || next.HelpVisible ||
            next.A11yPanelVisible || next.TourActive)
        {
            return false;
        }

        if (!ScreenKeyMap.TryGetValue(next.CurrentScreenNumber, out var mappings))
        {
            return false;
        }

        if (!gesture.IsCharacter || gesture.Character is not { } rune)
        {
            return false;
        }

        // Try unmodified first, then Shift-modified for uppercase shortcuts like Shift+G/E
        var mods = gesture.Modifiers;
        if (mods != TerminalModifiers.None && mods != TerminalModifiers.Shift)
        {
            return false;
        }

        var key = char.ToLowerInvariant((char)rune.Value);
        foreach (var (k, mutation) in mappings)
        {
            if (k == key)
            {
                next = mutation(next);
                return true;
            }
        }

        return false;
    }

    private bool HandleGlobalKey(KeyTerminalEvent keyEvent, DateTimeOffset now, ref ShowcaseDemoState next)
    {
        var gesture = keyEvent.Gesture;
        if (next.EvidenceLedgerVisible &&
            !next.Session.CommandPalette.IsOpen &&
            gesture.Key == TerminalKey.Escape &&
            gesture.Modifiers == TerminalModifiers.None)
        {
            next = next with { EvidenceLedgerVisible = false };
            return true;
        }

        if (next.PerfHudVisible &&
            !next.Session.CommandPalette.IsOpen &&
            gesture.Key == TerminalKey.Escape &&
            gesture.Modifiers == TerminalModifiers.None)
        {
            next = next with { PerfHudVisible = false };
            return true;
        }

        if (next.DebugVisible &&
            !next.Session.CommandPalette.IsOpen &&
            gesture.Key == TerminalKey.Escape &&
            gesture.Modifiers == TerminalModifiers.None)
        {
            next = next with { DebugVisible = false };
            return true;
        }

        if (next.HelpVisible &&
            !next.PerfHudVisible &&
            !next.DebugVisible &&
            !next.Session.CommandPalette.IsOpen &&
            gesture.Key == TerminalKey.Escape &&
            gesture.Modifiers == TerminalModifiers.None)
        {
            next = next with { HelpVisible = false };
            return true;
        }

        if (next.A11yPanelVisible &&
            !next.PerfHudVisible &&
            !next.DebugVisible &&
            !next.HelpVisible &&
            !next.Session.CommandPalette.IsOpen &&
            gesture.Key == TerminalKey.Escape &&
            gesture.Modifiers == TerminalModifiers.None)
        {
            next = next with { A11yPanelVisible = false };
            return true;
        }

        if (next.A11yPanelVisible)
        {
            if (IsShiftCharacter(gesture, 'h'))
            {
                next = (next with { A11yHighContrast = !next.A11yHighContrast }).RecordA11yTelemetry("HighContrast");
                return true;
            }

            if (IsShiftCharacter(gesture, 'm'))
            {
                next = (next with { A11yReducedMotion = !next.A11yReducedMotion }).RecordA11yTelemetry("ReducedMotion");
                return true;
            }

            if (IsShiftCharacter(gesture, 'l'))
            {
                next = (next with { A11yLargeText = !next.A11yLargeText }).RecordA11yTelemetry("LargeText");
                return true;
            }
        }

        if (IsControlCharacter(gesture, 'i'))
        {
            next = next with { EvidenceLedgerVisible = !next.EvidenceLedgerVisible };
            return true;
        }

        if (IsControlCharacter(gesture, 'p'))
        {
            next = next with { PerfHudVisible = !next.PerfHudVisible };
            return true;
        }

        if (next.CurrentScreenNumber == 37)
        {
            if (IsCharacter(gesture, 'h'))
            {
                next = (next with { A11yHighContrast = !next.A11yHighContrast }).RecordA11yTelemetry("HighContrast");
                return true;
            }

            if (IsCharacter(gesture, 'm'))
            {
                next = (next with { A11yReducedMotion = !next.A11yReducedMotion }).RecordA11yTelemetry("ReducedMotion");
                return true;
            }

            if (IsCharacter(gesture, 'l'))
            {
                next = (next with { A11yLargeText = !next.A11yLargeText }).RecordA11yTelemetry("LargeText");
                return true;
            }
        }

        if (HandleWidgetBuilderKey(gesture, ref next))
        {
            return true;
        }

        if (HandleDeterminismKey(gesture, ref next))
        {
            return true;
        }

        if (HandleHyperlinkKey(gesture, ref next))
        {
            return true;
        }

        if (HandlePaletteLabKey(gesture, ref next))
        {
            return true;
        }

        if (gesture.Key == TerminalKey.F6 || IsCharacter(gesture, 'm'))
        {
            next = next with { MouseCaptureEnabled = !next.MouseCaptureEnabled };
            return true;
        }

        if (IsShiftCharacter(gesture, 'a'))
        {
            next = next with { A11yPanelVisible = !next.A11yPanelVisible };
            return true;
        }

        if (next.TourActive)
        {
            if (gesture.Key == TerminalKey.Escape)
            {
                next = next with { TourActive = false, TourPaused = false, CurrentScreenNumber = 1 };
                return true;
            }

            if (IsCharacter(gesture, ' '))
            {
                next = next with { TourPaused = !next.TourPaused };
                return true;
            }

            if (gesture.Modifiers == TerminalModifiers.None &&
                (gesture.Key == TerminalKey.Right || IsCharacter(gesture, 'n')))
            {
                next = next.MoveTourStep(1, now);
                return true;
            }

            if (gesture.Modifiers == TerminalModifiers.None &&
                (gesture.Key == TerminalKey.Left || IsCharacter(gesture, 'p')))
            {
                next = next.MoveTourStep(-1, now);
                return true;
            }

            if (IsCharacter(gesture, '+') || IsCharacter(gesture, '='))
            {
                next = AdjustTourSpeed(next, 1.25);
                return true;
            }

            if (IsCharacter(gesture, '-'))
            {
                next = AdjustTourSpeed(next, 1.0 / 1.25);
                return true;
            }
        }

        if (gesture.Key == TerminalKey.F12)
        {
            next = next with { DebugVisible = !next.DebugVisible };
            return true;
        }

        if (IsCharacter(gesture, '?'))
        {
            next = next with { HelpVisible = !next.HelpVisible };
            return true;
        }

        if (IsCharacter(gesture, 'q'))
        {
            next = next with { QuitRequested = true };
            return true;
        }

        if (next.CurrentScreenNumber == 1 && !next.TourActive)
        {
            if (gesture.Key == TerminalKey.Escape && gesture.Modifiers == TerminalModifiers.None)
            {
                next = next with { CurrentScreenNumber = 2 };
                return true;
            }

            if (gesture.Key == TerminalKey.Enter || IsCharacter(gesture, ' '))
            {
                next = next.StartTour(now);
                return true;
            }

            if (gesture.Modifiers == TerminalModifiers.None &&
                (gesture.Key == TerminalKey.Down || IsCharacter(gesture, 'j') || IsCharacter(gesture, 'n')))
            {
                next = AdjustTourLandingStart(next, 1);
                return true;
            }

            if (gesture.Modifiers == TerminalModifiers.None &&
                (gesture.Key == TerminalKey.Up || IsCharacter(gesture, 'k') || IsCharacter(gesture, 'p')))
            {
                next = AdjustTourLandingStart(next, -1);
                return true;
            }

            if (IsCharacter(gesture, '+') || IsCharacter(gesture, '='))
            {
                next = AdjustTourSpeed(next, 1.25);
                return true;
            }

            if (IsCharacter(gesture, '-'))
            {
                next = AdjustTourSpeed(next, 1.0 / 1.25);
                return true;
            }

            if (IsCharacter(gesture, 'r'))
            {
                next = next with { TourStartScreen = 2, TourSpeed = 1.0 };
                return true;
            }
        }

        if (gesture.Key == TerminalKey.Tab)
        {
            var delta = gesture.Modifiers.HasFlag(TerminalModifiers.Shift) ? -1 : 1;
            next = next with { CurrentScreenNumber = ShowcaseCatalog.Move(next.CurrentScreenNumber, delta) };
            return true;
        }

        if (IsShiftCharacter(gesture, 'h') || IsShiftCharacter(gesture, 'l'))
        {
            next = next with
            {
                TourActive = false,
                TourPaused = false,
                CurrentScreenNumber = ShowcaseCatalog.Move(
                    next.CurrentScreenNumber,
                    IsShiftCharacter(gesture, 'h') ? -1 : 1)
            };
            return true;
        }

        if (gesture.Modifiers == TerminalModifiers.None &&
            (gesture.Key == TerminalKey.Left || gesture.Key == TerminalKey.Right))
        {
            next = next with
            {
                CurrentScreenNumber = ShowcaseCatalog.Move(
                    next.CurrentScreenNumber,
                    gesture.Key == TerminalKey.Left ? -1 : 1)
            };
            return true;
        }

        if (gesture.IsCharacter && gesture.Character is { } rune && gesture.Modifiers == TerminalModifiers.None)
        {
            var value = (char)rune.Value;
            if (value == '0')
            {
                next = next with { CurrentScreenNumber = 10 };
                return true;
            }

            if (value is >= '1' and <= '9')
            {
                next = next with { CurrentScreenNumber = value - '0' };
                return true;
            }
        }

        return false;
    }

    private static bool HandlePaletteLabKey(KeyGesture gesture, ref ShowcaseDemoState next)
    {
        if (next.CurrentScreenNumber != 39 ||
            next.Session.CommandPalette.IsOpen ||
            next.EvidenceLedgerVisible ||
            next.PerfHudVisible ||
            next.DebugVisible ||
            next.HelpVisible ||
            next.A11yPanelVisible ||
            next.TourActive)
        {
            return false;
        }

        if (IsCharacter(gesture, 'b'))
        {
            next = next with
            {
                PaletteLabBenchEnabled = !next.PaletteLabBenchEnabled,
                PaletteLabBenchFrame = 0,
                PaletteLabBenchProcessed = 0
            };
            return true;
        }

        if (IsCharacter(gesture, 'm'))
        {
            next = next with { PaletteLabMatchFilter = next.PaletteLabMatchFilter.Next() };
            return true;
        }

        if (!gesture.IsCharacter || gesture.Character is not { } rune || gesture.Modifiers != TerminalModifiers.None)
        {
            return false;
        }

        var filter = rune.Value switch
        {
            '0' => ShowcasePaletteLabMatchFilter.All,
            '1' => ShowcasePaletteLabMatchFilter.Exact,
            '2' => ShowcasePaletteLabMatchFilter.Prefix,
            '3' => ShowcasePaletteLabMatchFilter.WordStart,
            '4' => ShowcasePaletteLabMatchFilter.Substring,
            '5' => ShowcasePaletteLabMatchFilter.Fuzzy,
            _ => (ShowcasePaletteLabMatchFilter?)null
        };
        if (filter is null)
        {
            return false;
        }

        next = next with { PaletteLabMatchFilter = filter.Value };
        return true;
    }

    private static bool HandleKanbanBoardKey(KeyGesture gesture, ref ShowcaseDemoState next)
    {
        if (next.CurrentScreenNumber != 42 ||
            next.Session.CommandPalette.IsOpen ||
            next.EvidenceLedgerVisible ||
            next.PerfHudVisible ||
            next.DebugVisible ||
            next.HelpVisible ||
            next.A11yPanelVisible ||
            next.TourActive)
        {
            return false;
        }

        var board = next.KanbanBoard ?? ShowcaseKanbanState.CreateDefault();
        ShowcaseKanbanState? updated = null;

        if (gesture.Modifiers == TerminalModifiers.None)
        {
            updated = gesture.Key switch
            {
                TerminalKey.Left => board.FocusLeft(),
                TerminalKey.Right => board.FocusRight(),
                TerminalKey.Up => board.FocusUp(),
                TerminalKey.Down => board.FocusDown(),
                _ => updated
            };
        }

        if (updated is null && gesture.IsCharacter && gesture.Character is { } rune)
        {
            updated = (char)rune.Value switch
            {
                'h' => board.FocusLeft(),
                'l' => board.FocusRight(),
                'j' => board.FocusDown(),
                'k' => board.FocusUp(),
                'H' => board.MoveCardLeft(),
                'L' => board.MoveCardRight(),
                'u' => board.Undo(),
                'r' => board.Redo(),
                _ => null
            };
        }

        if (updated is null || ReferenceEquals(updated, board) || updated == board)
        {
            return updated is not null;
        }

        next = next with { KanbanBoard = updated };
        return true;
    }

    private static bool HandleChromeMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.Session.CommandPalette.IsOpen ||
            next.Viewport.Height == 0 ||
            gesture.Row >= next.Viewport.Height - 1)
        {
            return false;
        }

        if (gesture.Kind is TerminalMouseKind.Scroll)
        {
            var scrollHit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
            if (scrollHit.Layer == ShowcaseHitLayer.Tab)
            {
                if (gesture.Button is TerminalMouseButton.WheelDown)
                {
                    next = StopTourAndMove(next, 1);
                    return true;
                }

                if (gesture.Button is TerminalMouseButton.WheelUp)
                {
                    next = StopTourAndMove(next, -1);
                    return true;
                }
            }
        }

        if (gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Up) ||
            gesture.Button is not TerminalMouseButton.Left)
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if ((hit.Layer is ShowcaseHitLayer.Category or ShowcaseHitLayer.Tab) &&
            hit.TargetScreenNumber is { } targetScreenNumber)
        {
            next = StopTourAndSelect(next, targetScreenNumber);
            return true;
        }

        return false;
    }

    private static ShowcaseDemoState StopTourAndSelect(ShowcaseDemoState state, int screenNumber) =>
        state with
        {
            TourActive = false,
            TourPaused = false,
            CurrentScreenNumber = ShowcaseCatalog.ClampScreenNumber(screenNumber)
        };

    private static ShowcaseDemoState StopTourAndMove(ShowcaseDemoState state, int delta) =>
        state with
        {
            TourActive = false,
            TourPaused = false,
            CurrentScreenNumber = ShowcaseCatalog.Move(state.CurrentScreenNumber, delta)
        };

    private static bool HandleDashboardMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 2 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            hit.LocalHitId is not ("dashboard:overview" or "dashboard:highlights"))
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "dashboard:overview" => next with
                {
                    DashboardFocusIndex = 0,
                    DashboardOverviewScroll = Math.Clamp(next.DashboardOverviewScroll + delta, 0, 6),
                    DashboardContextArmed = false
                },
                "dashboard:highlights" => next with
                {
                    DashboardFocusIndex = 1,
                    DashboardHighlightIndex = Math.Clamp(next.DashboardHighlightIndex + delta, 0, 7),
                    DashboardContextArmed = false
                },
                _ => next
            };
            return true;
        }

        if (gesture.Button == TerminalMouseButton.Right)
        {
            next = next with
            {
                DashboardFocusIndex = hit.LocalHitId == "dashboard:overview" ? 0 : 1,
                DashboardContextArmed = true
            };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "dashboard:overview" => next with
            {
                DashboardFocusIndex = 0,
                DashboardOverviewScroll = Math.Clamp(next.DashboardOverviewScroll + 1, 0, 6),
                DashboardContextArmed = false
            },
            "dashboard:highlights" => next with
            {
                DashboardFocusIndex = 1,
                DashboardHighlightIndex = (next.DashboardHighlightIndex + 1) % 8,
                DashboardContextArmed = false
            },
            _ => next
        };
        return true;
    }

    private static bool HandleShakespeareMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 3 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            hit.LocalHitId is not ("shakespeare:search" or "shakespeare:notes"))
        {
            return false;
        }

        var focus = hit.LocalHitId == "shakespeare:search" ? 0 : 1;
        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId == "shakespeare:search"
                ? next with
                {
                    ShakespeareFocusIndex = focus,
                    ShakespeareSearchScroll = Math.Clamp(next.ShakespeareSearchScroll + delta, 0, 12),
                    ShakespeareContextArmed = false
                }
                : next with
                {
                    ShakespeareFocusIndex = focus,
                    ShakespeareNotesScroll = Math.Clamp(next.ShakespeareNotesScroll + delta, 0, 12),
                    ShakespeareContextArmed = false
                };
            return true;
        }

        if (gesture.Button == TerminalMouseButton.Right)
        {
            next = next with
            {
                ShakespeareFocusIndex = focus,
                ShakespeareContextArmed = true
            };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId == "shakespeare:search"
            ? next with
            {
                ShakespeareFocusIndex = focus,
                ShakespeareQueryIndex = (next.ShakespeareQueryIndex + 1) % 4,
                ShakespeareContextArmed = false
            }
            : next with
            {
                ShakespeareFocusIndex = focus,
                ShakespeareContextArmed = false
            };
        return true;
    }

    private static bool HandleCodeExplorerMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 4 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            hit.LocalHitId is not ("code_explorer:tree" or "code_explorer:editor"))
        {
            return false;
        }

        var focus = hit.LocalHitId == "code_explorer:tree" ? 0 : 1;
        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId == "code_explorer:tree"
                ? next with
                {
                    CodeExplorerFocusIndex = focus,
                    CodeExplorerSelectedNodeIndex = Math.Clamp(next.CodeExplorerSelectedNodeIndex + delta, 0, 6),
                    CodeExplorerContextArmed = false
                }
                : next with
                {
                    CodeExplorerFocusIndex = focus,
                    CodeExplorerEditorScroll = Math.Clamp(next.CodeExplorerEditorScroll + delta, 0, 12),
                    CodeExplorerContextArmed = false
                };
            return true;
        }

        if (gesture.Button == TerminalMouseButton.Right)
        {
            next = next with
            {
                CodeExplorerFocusIndex = focus,
                CodeExplorerContextArmed = true
            };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId == "code_explorer:tree"
            ? next with
            {
                CodeExplorerFocusIndex = focus,
                CodeExplorerSelectedNodeIndex = (next.CodeExplorerSelectedNodeIndex + 1) % 7,
                CodeExplorerContextArmed = false
            }
            : next with
            {
                CodeExplorerFocusIndex = focus,
                CodeExplorerContextArmed = false
            };
        return true;
    }

    private static bool HandleLayoutLabMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 6 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            hit.LocalHitId is not ("layout_lab:workspace" or "layout_lab:metrics"))
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "layout_lab:workspace" => next with
                {
                    LayoutLabFocusIndex = 0,
                    LayoutLabWorkspaceZoom = Math.Clamp(next.LayoutLabWorkspaceZoom + delta, -3, 3),
                    LayoutLabContextArmed = false
                },
                "layout_lab:metrics" => next with
                {
                    LayoutLabFocusIndex = 1,
                    LayoutLabMetricsScroll = Math.Clamp(next.LayoutLabMetricsScroll + delta, 0, 8),
                    LayoutLabContextArmed = false
                },
                _ => next
            };
            return true;
        }

        if (gesture.Button == TerminalMouseButton.Right)
        {
            next = next with
            {
                LayoutLabFocusIndex = hit.LocalHitId == "layout_lab:workspace" ? 0 : 1,
                LayoutLabContextArmed = true
            };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "layout_lab:workspace" => next with
            {
                LayoutLabFocusIndex = 0,
                LayoutLabSelectedPaneIndex = (next.LayoutLabSelectedPaneIndex + 1) % 4,
                LayoutLabContextArmed = false
            },
            "layout_lab:metrics" => next with
            {
                LayoutLabFocusIndex = 1,
                LayoutLabMetricsScroll = Math.Clamp(next.LayoutLabMetricsScroll + 1, 0, 8),
                LayoutLabContextArmed = false
            },
            _ => next
        };
        return true;
    }

    private static bool HandleKanbanBoardMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 42 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Up or TerminalMouseKind.Drag))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            !TryParseKanbanHit(hit.LocalHitId, out var col, out var row, out _))
        {
            return false;
        }

        var board = next.KanbanBoard ?? ShowcaseKanbanState.CreateDefault();
        var updated = gesture.Kind switch
        {
            TerminalMouseKind.Down when gesture.Button == TerminalMouseButton.Left => board.FocusAt(col, row).StartDrag(col, row),
            TerminalMouseKind.Up when gesture.Button == TerminalMouseButton.Left => board.DropOnColumn(col),
            TerminalMouseKind.Drag when gesture.Button == TerminalMouseButton.Left => board.DragOverColumn(col),
            _ => board
        };

        if (updated == board)
        {
            return true;
        }

        next = next with { KanbanBoard = updated };
        return true;
    }

    private static bool TryParseKanbanHit(string localHitId, out int col, out int row, out int cardId)
    {
        col = 0;
        row = 0;
        cardId = 0;
        var parts = localHitId.Split(':');
        return parts.Length == 4 &&
            parts[0] == "kanban" &&
            int.TryParse(parts[1], out col) &&
            int.TryParse(parts[2], out row) &&
            int.TryParse(parts[3], out cardId);
    }

    private static bool HandlePaneMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            next.Viewport.Height <= 4 ||
            gesture.Row >= next.Viewport.Height - 1 ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Up) ||
            gesture.Button is not TerminalMouseButton.Left)
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit is not { Layer: ShowcaseHitLayer.Pane, TargetScreenNumber: { } targetScreenNumber })
        {
            return false;
        }

        if (next.CurrentScreenNumber != 2 || targetScreenNumber == next.CurrentScreenNumber)
        {
            return false;
        }

        next = next with
        {
            CurrentScreenNumber = targetScreenNumber,
            TourActive = false,
            TourPaused = false
        };
        return true;
    }

    private static bool HandlePaletteLabMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 39 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            next.EvidenceLedgerVisible ||
            next.PerfHudVisible ||
            next.DebugVisible ||
            next.HelpVisible ||
            next.A11yPanelVisible ||
            !TryResolvePaletteLabPaletteArea(next.Viewport, out var paletteArea) ||
            !Contains(paletteArea, gesture.Column, gesture.Row))
        {
            return false;
        }

        var palette = next.Session.CommandPalette;
        var labPalette = palette with
        {
            IsOpen = true,
            Query = ShowcaseSurface.ResolvePaletteLabQuery(next, palette)
        };
        var results = ShowcaseSurface.FilterPaletteLabResultsForDemo(
            CommandPaletteController.Results(labPalette, ShowcaseCommandPalette.EvidenceLabEntries()),
            next.PaletteLabMatchFilter);
        var selectedIndex = results.Count == 0
            ? -1
            : Math.Clamp(palette.SelectedIndex, 0, results.Count - 1);

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -3 : 3;
            next = next with
            {
                Session = next.Session with
                {
                    CommandPalette = palette with { SelectedIndex = MovePaletteLabSelection(selectedIndex, results.Count, delta) }
                }
            };
            return true;
        }

        if (gesture.Kind != TerminalMouseKind.Down ||
            gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        if (selectedIndex < 0 || selectedIndex >= results.Count)
        {
            next = next with
            {
                Session = next.Session with
                {
                    CommandPalette = palette with { Status = "No command selected." }
                }
            };
            return true;
        }

        var selected = results[selectedIndex].Entry;
        next = next with
        {
            Session = next.Session with
            {
                CommandPalette = palette with
                {
                    LastExecutedCommandId = selected.Id,
                    Status = $"Executed {selected.Title}."
                },
                InputState = next.Session.InputState.Announce($"Palette lab executed {selected.Title}")
            }
        };
        return true;
    }

    internal static bool TryResolvePaletteLabPaletteArea(Size viewport, out Rect area)
    {
        area = default;
        if (viewport.Width < 12 || viewport.Height < 6)
        {
            return false;
        }

        var width = Math.Max(1, viewport.Width * 34 / 100);
        var height = Math.Max(1, viewport.Height - 4);
        area = new Rect(0, 3, (ushort)Math.Min(width, ushort.MaxValue), (ushort)Math.Min(height, ushort.MaxValue));
        return true;
    }

    private static bool Contains(Rect rect, ushort column, ushort row) =>
        column >= rect.X &&
        column < rect.X + rect.Width &&
        row >= rect.Y &&
        row < rect.Y + rect.Height;

    private static int MovePaletteLabSelection(int selectedIndex, int count, int delta)
    {
        if (count <= 0)
        {
            return 0;
        }

        return Math.Clamp((selectedIndex < 0 ? 0 : selectedIndex) + delta, 0, count - 1);
    }

    private static bool HandleTableThemeGalleryMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 11 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            !hit.LocalHitId.StartsWith("table_theme:preset:", StringComparison.Ordinal))
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = next with { TableThemePresetIndex = Math.Clamp(next.TableThemePresetIndex + delta, 0, 2) };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        var presetText = hit.LocalHitId["table_theme:preset:".Length..];
        if (!int.TryParse(presetText, CultureInfo.InvariantCulture, out var preset))
        {
            return false;
        }

        next = next with { TableThemePresetIndex = Math.Clamp(preset, 0, 2) };
        return true;
    }

    private static bool HandleDataVizMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 8 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "data_viz:metrics_table" => next with
                {
                    DataVizActivePanelIndex = 1,
                    DataVizContextArmed = false,
                    DataVizMetricRowIndex = Math.Clamp(next.DataVizMetricRowIndex + delta, 0, 3)
                },
                "data_viz:narrative" => next with
                {
                    DataVizActivePanelIndex = 2,
                    DataVizContextArmed = false,
                    DataVizNarrativeDetailIndex = Math.Clamp(next.DataVizNarrativeDetailIndex + delta, 0, 2)
                },
                _ => next
            };
            return hit.LocalHitId is "data_viz:metrics_table" or "data_viz:narrative";
        }

        if (gesture.Button != TerminalMouseButton.Left && gesture.Button != TerminalMouseButton.Right)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "data_viz:progress" => next with
            {
                DataVizActivePanelIndex = 0,
                DataVizContextArmed = false
            },
            "data_viz:metrics_table" => next with
            {
                DataVizActivePanelIndex = 1,
                DataVizContextArmed = gesture.Button == TerminalMouseButton.Right,
                DataVizMetricRowIndex = Math.Clamp(next.DataVizMetricRowIndex + 1, 0, 3)
            },
            "data_viz:narrative" => next with
            {
                DataVizActivePanelIndex = 2,
                DataVizContextArmed = gesture.Button == TerminalMouseButton.Right,
                DataVizNarrativeDetailIndex = Math.Clamp(next.DataVizNarrativeDetailIndex + 1, 0, 2)
            },
            _ => next
        };
        return hit.LocalHitId is "data_viz:progress" or "data_viz:metrics_table" or "data_viz:narrative";
    }

    private static bool HandleFileBrowserMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 9 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                { } value when value.StartsWith("file_browser:tree:", StringComparison.Ordinal) => next with
                {
                    FileBrowserFocusIndex = 0,
                    FileBrowserTreeScroll = Math.Clamp(next.FileBrowserTreeScroll + delta, 0, 8)
                },
                "file_browser:preview" => next with
                {
                    FileBrowserFocusIndex = 1,
                    FileBrowserPreviewScroll = Math.Clamp(next.FileBrowserPreviewScroll + delta, 0, 8)
                },
                _ => next
            };
            return hit.LocalHitId.StartsWith("file_browser:tree:", StringComparison.Ordinal) ||
                hit.LocalHitId == "file_browser:preview";
        }

        if (gesture.Button != TerminalMouseButton.Left ||
            !hit.LocalHitId.StartsWith("file_browser:tree:", StringComparison.Ordinal))
        {
            if (hit.LocalHitId == "file_browser:preview")
            {
                next = next with { FileBrowserFocusIndex = 1 };
                return true;
            }

            return false;
        }

        var rowText = hit.LocalHitId["file_browser:tree:".Length..];
        if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
        {
            return false;
        }

        next = next with
        {
            FileBrowserFocusIndex = 0,
            FileBrowserSelectedRowIndex = Math.Clamp(row + next.FileBrowserTreeScroll, 0, 5)
        };
        return true;
    }

    private static bool HandleAdvancedMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 10 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "advanced:patterns" => next with
                {
                    AdvancedFocusIndex = 0,
                    AdvancedPatternIndex = Math.Clamp(next.AdvancedPatternIndex + delta, 0, 4)
                },
                "advanced:composite" => next with
                {
                    AdvancedFocusIndex = 1,
                    AdvancedCompositeModeIndex = Math.Clamp(next.AdvancedCompositeModeIndex + delta, 0, 2)
                },
                _ => next
            };
            return hit.LocalHitId is "advanced:patterns" or "advanced:composite";
        }

        if (gesture.Button == TerminalMouseButton.Right && hit.LocalHitId is "advanced:patterns" or "advanced:composite")
        {
            next = next with
            {
                AdvancedFocusIndex = hit.LocalHitId == "advanced:patterns" ? 0 : 1,
                AdvancedContextArmed = true
            };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "advanced:patterns" => next with
            {
                AdvancedFocusIndex = 0,
                AdvancedContextArmed = false,
                AdvancedPatternIndex = Math.Clamp(next.AdvancedPatternIndex + 1, 0, 4)
            },
            "advanced:composite" => next with
            {
                AdvancedFocusIndex = 1,
                AdvancedContextArmed = false,
                AdvancedCompositeModeIndex = Math.Clamp(next.AdvancedCompositeModeIndex + 1, 0, 2)
            },
            _ => next
        };
        return hit.LocalHitId is "advanced:patterns" or "advanced:composite";
    }

    private static bool HandleNotificationsMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 21 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                { } value when value.StartsWith("notifications:toast:", StringComparison.Ordinal) => next with
                {
                    NotificationsFocusIndex = 1,
                    NotificationsContextArmed = false,
                    NotificationsToastIndex = Math.Clamp(next.NotificationsToastIndex + delta, 0, 4)
                },
                "notifications:lifecycle" => next with
                {
                    NotificationsFocusIndex = 2,
                    NotificationsContextArmed = false,
                    NotificationsLifecycleScroll = Math.Clamp(next.NotificationsLifecycleScroll + delta, 0, 6)
                },
                _ => next
            };
            return hit.LocalHitId.StartsWith("notifications:toast:", StringComparison.Ordinal) ||
                hit.LocalHitId == "notifications:lifecycle";
        }

        if (gesture.Button == TerminalMouseButton.Right &&
            (hit.LocalHitId.StartsWith("notifications:trigger:", StringComparison.Ordinal) ||
                hit.LocalHitId.StartsWith("notifications:toast:", StringComparison.Ordinal) ||
                hit.LocalHitId == "notifications:lifecycle"))
        {
            next = next with
            {
                NotificationsFocusIndex = hit.LocalHitId switch
                {
                    { } value when value.StartsWith("notifications:toast:", StringComparison.Ordinal) => 1,
                    "notifications:lifecycle" => 2,
                    _ => 0
                },
                NotificationsContextArmed = true
            };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        if (hit.LocalHitId.StartsWith("notifications:trigger:", StringComparison.Ordinal))
        {
            var trigger = hit.LocalHitId["notifications:trigger:".Length..];
            var triggerIndex = trigger switch
            {
                "error" => 1,
                "warning" => 2,
                "info" => 3,
                "urgent" => 4,
                "dismiss_all" => 5,
                _ => 0
            };
            next = next with
            {
                NotificationsFocusIndex = 0,
                NotificationsContextArmed = false,
                NotificationsTriggerIndex = triggerIndex
            };
            return true;
        }

        if (hit.LocalHitId.StartsWith("notifications:toast:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["notifications:toast:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with
            {
                NotificationsFocusIndex = 1,
                NotificationsContextArmed = false,
                NotificationsToastIndex = Math.Clamp(row, 0, 4)
            };
            return true;
        }

        if (hit.LocalHitId == "notifications:lifecycle")
        {
            next = next with
            {
                NotificationsFocusIndex = 2,
                NotificationsContextArmed = false,
                NotificationsLifecycleScroll = Math.Clamp(next.NotificationsLifecycleScroll + 1, 0, 6)
            };
            return true;
        }

        return false;
    }

    private static bool HandleActionTimelineMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 22 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "action_timeline:timeline" => next with
                {
                    ActionTimelineFocusIndex = 1,
                    ActionTimelineContextArmed = false,
                    ActionTimelineSelectedIndex = Math.Clamp(next.ActionTimelineSelectedIndex + delta, 0, 7)
                },
                "action_timeline:filters" => next with
                {
                    ActionTimelineFocusIndex = 0,
                    ActionTimelineContextArmed = false,
                    ActionTimelineFilterIndex = Math.Clamp(next.ActionTimelineFilterIndex + delta, 0, 3)
                },
                _ => next
            };
            return hit.LocalHitId is "action_timeline:timeline" or "action_timeline:filters";
        }

        if (gesture.Button == TerminalMouseButton.Right &&
            hit.LocalHitId is "action_timeline:filters" or "action_timeline:timeline" or "action_timeline:detail")
        {
            next = next with
            {
                ActionTimelineFocusIndex = hit.LocalHitId switch
                {
                    "action_timeline:timeline" => 1,
                    "action_timeline:detail" => 2,
                    _ => 0
                },
                ActionTimelineContextArmed = true
            };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "action_timeline:filters" => next with
            {
                ActionTimelineFocusIndex = 0,
                ActionTimelineContextArmed = false,
                ActionTimelineFilterIndex = (next.ActionTimelineFilterIndex + 1) % 4
            },
            "action_timeline:timeline" => next with
            {
                ActionTimelineFocusIndex = 1,
                ActionTimelineContextArmed = false,
                ActionTimelineSelectedIndex = Math.Clamp(next.ActionTimelineSelectedIndex + 1, 0, 7)
            },
            "action_timeline:detail" => next with
            {
                ActionTimelineFocusIndex = 2,
                ActionTimelineContextArmed = false,
                ActionTimelineDetailExpanded = !next.ActionTimelineDetailExpanded
            },
            _ => next
        };
        return hit.LocalHitId is "action_timeline:filters" or "action_timeline:timeline" or "action_timeline:detail";
    }

    private static bool HandleIntrinsicSizingMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 23 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "intrinsic_sizing:scenarios" => next with
                {
                    IntrinsicSizingFocusIndex = 1,
                    IntrinsicSizingContextArmed = false,
                    IntrinsicSizingScenarioIndex = Math.Clamp(next.IntrinsicSizingScenarioIndex + delta, 0, 3)
                },
                "intrinsic_sizing:detail" => next with
                {
                    IntrinsicSizingFocusIndex = 2,
                    IntrinsicSizingContextArmed = false,
                    IntrinsicSizingDetailScroll = Math.Clamp(next.IntrinsicSizingDetailScroll + delta, 0, 6)
                },
                "intrinsic_sizing:controls" => next with
                {
                    IntrinsicSizingFocusIndex = 3,
                    IntrinsicSizingContextArmed = false,
                    IntrinsicSizingWidthPresetIndex = Math.Clamp(next.IntrinsicSizingWidthPresetIndex + delta, 0, 3)
                },
                _ => next
            };
            return hit.LocalHitId is "intrinsic_sizing:scenarios" or "intrinsic_sizing:detail" or "intrinsic_sizing:controls";
        }

        if (gesture.Button == TerminalMouseButton.Right &&
            hit.LocalHitId is "intrinsic_sizing:header" or "intrinsic_sizing:scenarios" or "intrinsic_sizing:detail" or "intrinsic_sizing:controls")
        {
            next = next with
            {
                IntrinsicSizingFocusIndex = hit.LocalHitId switch
                {
                    "intrinsic_sizing:scenarios" => 1,
                    "intrinsic_sizing:detail" => 2,
                    "intrinsic_sizing:controls" => 3,
                    _ => 0
                },
                IntrinsicSizingContextArmed = true
            };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "intrinsic_sizing:header" => next with
            {
                IntrinsicSizingFocusIndex = 0,
                IntrinsicSizingContextArmed = false
            },
            "intrinsic_sizing:scenarios" => next with
            {
                IntrinsicSizingFocusIndex = 1,
                IntrinsicSizingContextArmed = false,
                IntrinsicSizingScenarioIndex = (next.IntrinsicSizingScenarioIndex + 1) % 4
            },
            "intrinsic_sizing:detail" => next with
            {
                IntrinsicSizingFocusIndex = 2,
                IntrinsicSizingContextArmed = false,
                IntrinsicSizingDetailScroll = Math.Clamp(next.IntrinsicSizingDetailScroll + 1, 0, 6)
            },
            "intrinsic_sizing:controls" => next with
            {
                IntrinsicSizingFocusIndex = 3,
                IntrinsicSizingContextArmed = false,
                IntrinsicSizingWidthPresetIndex = (next.IntrinsicSizingWidthPresetIndex + 1) % 4
            },
            _ => next
        };
        return hit.LocalHitId is "intrinsic_sizing:header" or "intrinsic_sizing:scenarios" or "intrinsic_sizing:detail" or "intrinsic_sizing:controls";
    }

    private static bool HandleLayoutInspectorMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 24 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "layout_inspector:info" or "layout_inspector:pane_studio" => next with
                {
                    LayoutInspectorFocusIndex = hit.LocalHitId == "layout_inspector:pane_studio" ? 3 : 0,
                    LayoutInspectorContextArmed = false,
                    LayoutInspectorScenarioIndex = (next.LayoutInspectorScenarioIndex + delta + 3) % 3
                },
                "layout_inspector:overlay" => next with
                {
                    LayoutInspectorFocusIndex = 1,
                    LayoutInspectorContextArmed = false,
                    LayoutInspectorStepIndex = (next.LayoutInspectorStepIndex + delta + 3) % 3
                },
                "layout_inspector:tree" => next with
                {
                    LayoutInspectorFocusIndex = 2,
                    LayoutInspectorContextArmed = false,
                    LayoutInspectorTreeVisible = !next.LayoutInspectorTreeVisible
                },
                _ => next
            };
            return hit.LocalHitId is "layout_inspector:info" or "layout_inspector:overlay" or "layout_inspector:tree" or "layout_inspector:pane_studio";
        }

        next = (hit.LocalHitId, gesture.Button) switch
        {
            ("layout_inspector:info", TerminalMouseButton.Left) => next with
            {
                LayoutInspectorFocusIndex = 0,
                LayoutInspectorContextArmed = false,
                LayoutInspectorScenarioIndex = (next.LayoutInspectorScenarioIndex + 1) % 3
            },
            ("layout_inspector:overlay", TerminalMouseButton.Left) => next with
            {
                LayoutInspectorFocusIndex = 1,
                LayoutInspectorContextArmed = false,
                LayoutInspectorStepIndex = (next.LayoutInspectorStepIndex + 1) % 3
            },
            ("layout_inspector:overlay", TerminalMouseButton.Right) => next with
            {
                LayoutInspectorFocusIndex = 1,
                LayoutInspectorContextArmed = true,
                LayoutInspectorOverlayVisible = !next.LayoutInspectorOverlayVisible
            },
            ("layout_inspector:tree", TerminalMouseButton.Left) => next with
            {
                LayoutInspectorFocusIndex = 2,
                LayoutInspectorContextArmed = false,
                LayoutInspectorTreeVisible = !next.LayoutInspectorTreeVisible
            },
            ("layout_inspector:pane_studio", TerminalMouseButton.Right) => next with
            {
                LayoutInspectorFocusIndex = 3,
                LayoutInspectorContextArmed = true,
                LayoutInspectorOverlayVisible = !next.LayoutInspectorOverlayVisible
            },
            ("layout_inspector:pane_studio", TerminalMouseButton.Left) => next with
            {
                LayoutInspectorFocusIndex = 3,
                LayoutInspectorContextArmed = false,
                LayoutInspectorStepIndex = (next.LayoutInspectorStepIndex + 1) % 3
            },
            _ => next
        };
        return hit.LocalHitId is "layout_inspector:info" or "layout_inspector:overlay" or "layout_inspector:tree" or "layout_inspector:pane_studio";
    }

    private static bool HandleAdvancedTextEditorMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 25 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                { } value when value.StartsWith("advanced_text_editor:line:", StringComparison.Ordinal) => next with
                {
                    AdvancedTextEditorCursorLine = Math.Clamp(next.AdvancedTextEditorCursorLine + delta, 0, 12),
                    AdvancedTextEditorFocusIndex = 0
                },
                { } value when value.StartsWith("advanced_text_editor:history:", StringComparison.Ordinal) => next with
                {
                    AdvancedTextEditorHistoryIndex = Math.Clamp(next.AdvancedTextEditorHistoryIndex + delta, 0, 5),
                    AdvancedTextEditorFocusIndex = 2
                },
                { } value when value.StartsWith("advanced_text_editor:diagnostic:", StringComparison.Ordinal) => next with
                {
                    AdvancedTextEditorDiagnosticsIndex = Math.Clamp(next.AdvancedTextEditorDiagnosticsIndex + delta, 0, 9),
                    AdvancedTextEditorFocusIndex = 3
                },
                "advanced_text_editor:search" => next with
                {
                    AdvancedTextEditorFocusIndex = 1
                },
                _ => next
            };
            return hit.LocalHitId == "advanced_text_editor:search" ||
                hit.LocalHitId.StartsWith("advanced_text_editor:line:", StringComparison.Ordinal) ||
                hit.LocalHitId.StartsWith("advanced_text_editor:history:", StringComparison.Ordinal) ||
                hit.LocalHitId.StartsWith("advanced_text_editor:diagnostic:", StringComparison.Ordinal);
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        if (hit.LocalHitId.StartsWith("advanced_text_editor:line:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["advanced_text_editor:line:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with
            {
                AdvancedTextEditorCursorLine = Math.Clamp(row, 0, 12),
                AdvancedTextEditorFocusIndex = 0
            };
            return true;
        }

        if (hit.LocalHitId.StartsWith("advanced_text_editor:history:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["advanced_text_editor:history:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with
            {
                AdvancedTextEditorHistoryIndex = Math.Clamp(row, 0, 5),
                AdvancedTextEditorFocusIndex = 2
            };
            return true;
        }

        if (hit.LocalHitId.StartsWith("advanced_text_editor:diagnostic:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["advanced_text_editor:diagnostic:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with
            {
                AdvancedTextEditorDiagnosticsIndex = Math.Clamp(row, 0, 9),
                AdvancedTextEditorFocusIndex = 3
            };
            return true;
        }

        if (hit.LocalHitId == "advanced_text_editor:search")
        {
            next = next with { AdvancedTextEditorFocusIndex = 1 };
            return true;
        }

        return false;
    }

    private static bool HandleMousePlaygroundMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 26 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            !hit.LocalHitId.StartsWith("target:", StringComparison.Ordinal))
        {
            return false;
        }

        var targetText = hit.LocalHitId["target:".Length..];
        if (!int.TryParse(targetText, CultureInfo.InvariantCulture, out var targetId))
        {
            return false;
        }

        var targetIndex = Math.Clamp(targetId - 1, 0, 11);
        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = next with
            {
                MousePlaygroundFocusIndex = 2,
                MousePlaygroundSelectedTargetIndex = targetIndex,
                MousePlaygroundEventIndex = Math.Clamp(next.MousePlaygroundEventIndex + delta, 0, 9),
                MousePlaygroundJitterStatsVisible = true,
                MousePlaygroundContextArmed = false
            };
            return true;
        }

        next = gesture.Button switch
        {
            TerminalMouseButton.Left => next with
            {
                MousePlaygroundFocusIndex = 0,
                MousePlaygroundSelectedTargetIndex = targetIndex,
                MousePlaygroundSelectedTargetClicks = next.MousePlaygroundSelectedTargetIndex == targetIndex
                    ? Math.Clamp(next.MousePlaygroundSelectedTargetClicks + 1, 0, 99)
                    : 1,
                MousePlaygroundEventIndex = 7,
                MousePlaygroundContextArmed = false
            },
            TerminalMouseButton.Right => next with
            {
                MousePlaygroundFocusIndex = 1,
                MousePlaygroundSelectedTargetIndex = targetIndex,
                MousePlaygroundOverlayVisible = !next.MousePlaygroundOverlayVisible,
                MousePlaygroundEventIndex = 8,
                MousePlaygroundContextArmed = true
            },
            TerminalMouseButton.Middle => next with
            {
                MousePlaygroundFocusIndex = 2,
                MousePlaygroundSelectedTargetIndex = targetIndex,
                MousePlaygroundJitterStatsVisible = !next.MousePlaygroundJitterStatsVisible,
                MousePlaygroundEventIndex = 9,
                MousePlaygroundContextArmed = false
            },
            _ => next
        };
        return gesture.Button is TerminalMouseButton.Left or TerminalMouseButton.Right or TerminalMouseButton.Middle;
    }

    private static bool HandleFormValidationMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 27 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                { } value when value.StartsWith("form_validation:field:", StringComparison.Ordinal) => next with
                {
                    FormValidationFocusIndex = 1,
                    FormValidationSelectedFieldIndex = Math.Clamp(next.FormValidationSelectedFieldIndex + delta, 0, 8)
                },
                "form_validation:rules" => next with
                {
                    FormValidationFocusIndex = 4,
                    FormValidationRulesScroll = Math.Clamp(next.FormValidationRulesScroll + delta, 0, 6)
                },
                "form_validation:diagnostics" => next with
                {
                    FormValidationFocusIndex = 7,
                    FormValidationDiagnosticsScroll = Math.Clamp(next.FormValidationDiagnosticsScroll + delta, 0, 8)
                },
                _ => next
            };
            return hit.LocalHitId == "form_validation:rules" ||
                hit.LocalHitId == "form_validation:diagnostics" ||
                hit.LocalHitId.StartsWith("form_validation:field:", StringComparison.Ordinal);
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        if (hit.LocalHitId.StartsWith("form_validation:field:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["form_validation:field:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with
            {
                FormValidationFocusIndex = 1,
                FormValidationSelectedFieldIndex = Math.Clamp(row, 0, 8)
            };
            return true;
        }

        if (hit.LocalHitId.StartsWith("form_validation:error:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["form_validation:error:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with
            {
                FormValidationFocusIndex = 3,
                FormValidationSelectedErrorIndex = Math.Clamp(row, 0, 8),
                FormValidationOnSubmitMode = !next.FormValidationOnSubmitMode
            };
            return true;
        }

        next = hit.LocalHitId switch
        {
            "form_validation:mode" => next with
            {
                FormValidationFocusIndex = 0,
                FormValidationOnSubmitMode = !next.FormValidationOnSubmitMode
            },
            "form_validation:touched_dirty" => next with
            {
                FormValidationFocusIndex = 2,
                FormValidationSubmitted = !next.FormValidationSubmitted
            },
            "form_validation:controls" => next with
            {
                FormValidationFocusIndex = 5,
                FormValidationSubmitted = true
            },
            "form_validation:notifications" => next with
            {
                FormValidationFocusIndex = 6,
                FormValidationSubmitted = !next.FormValidationSubmitted
            },
            "form_validation:rules" => next with
            {
                FormValidationFocusIndex = 4,
                FormValidationRulesScroll = Math.Clamp(next.FormValidationRulesScroll + 1, 0, 6)
            },
            "form_validation:diagnostics" => next with
            {
                FormValidationFocusIndex = 7,
                FormValidationDiagnosticsScroll = Math.Clamp(next.FormValidationDiagnosticsScroll + 1, 0, 8)
            },
            _ => next
        };
        return hit.LocalHitId is "form_validation:mode" or "form_validation:touched_dirty" or
            "form_validation:controls" or "form_validation:notifications" or
            "form_validation:rules" or "form_validation:diagnostics";
    }

    private static bool HandleVirtualizedSearchMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 28 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                { } value when value.StartsWith("virtualized_search:result:", StringComparison.Ordinal) => next with
                {
                    VirtualizedSearchSelectedIndex = Math.Clamp(next.VirtualizedSearchSelectedIndex + delta * 3, 0, 11),
                    VirtualizedSearchFocusSearch = false,
                    VirtualizedSearchStatsFocused = false
                },
                { } value when value.StartsWith("virtualized_search:diagnostic:", StringComparison.Ordinal) => next with
                {
                    VirtualizedSearchDiagnosticsScroll = Math.Clamp(next.VirtualizedSearchDiagnosticsScroll + delta, 0, 8),
                    VirtualizedSearchDiagnosticIndex = Math.Clamp(next.VirtualizedSearchDiagnosticIndex + delta, 0, 8),
                    VirtualizedSearchStatsFocused = true,
                    VirtualizedSearchFocusSearch = false
                },
                "virtualized_search:stats" => next with
                {
                    VirtualizedSearchStatsFocused = true,
                    VirtualizedSearchFocusSearch = false
                },
                _ => next
            };
            return hit.LocalHitId == "virtualized_search:stats" ||
                hit.LocalHitId.StartsWith("virtualized_search:result:", StringComparison.Ordinal) ||
                hit.LocalHitId.StartsWith("virtualized_search:diagnostic:", StringComparison.Ordinal);
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        if (hit.LocalHitId == "virtualized_search:search_bar")
        {
            next = next with
            {
                VirtualizedSearchFocusSearch = true,
                VirtualizedSearchStatsFocused = false
            };
            return true;
        }

        if (hit.LocalHitId == "virtualized_search:stats")
        {
            next = next with
            {
                VirtualizedSearchStatsFocused = true,
                VirtualizedSearchFocusSearch = false
            };
            return true;
        }

        if (hit.LocalHitId.StartsWith("virtualized_search:result:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["virtualized_search:result:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with
            {
                VirtualizedSearchSelectedIndex = Math.Clamp(row, 0, 11),
                VirtualizedSearchFocusSearch = false,
                VirtualizedSearchStatsFocused = false
            };
            return true;
        }

        if (hit.LocalHitId.StartsWith("virtualized_search:diagnostic:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["virtualized_search:diagnostic:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with
            {
                VirtualizedSearchDiagnosticsScroll = Math.Clamp(row, 0, 8),
                VirtualizedSearchDiagnosticIndex = Math.Clamp(row, 0, 8),
                VirtualizedSearchStatsFocused = true,
                VirtualizedSearchFocusSearch = false
            };
            return true;
        }

        return false;
    }

    private static bool HandleAsyncTasksMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 29 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                { } value when value.StartsWith("async_tasks:task:", StringComparison.Ordinal) => next with
                {
                    AsyncTasksSelectedIndex = Math.Clamp(next.AsyncTasksSelectedIndex + delta, 0, 7),
                    AsyncTasksFocusedPanelIndex = 1
                },
                "async_tasks:hazard" => next with
                {
                    AsyncTasksHazardScroll = Math.Clamp(next.AsyncTasksHazardScroll + delta, 0, 6),
                    AsyncTasksFocusedPanelIndex = 4
                },
                _ => next
            };
            return hit.LocalHitId == "async_tasks:hazard" ||
                hit.LocalHitId.StartsWith("async_tasks:task:", StringComparison.Ordinal);
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        if (hit.LocalHitId.StartsWith("async_tasks:task:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["async_tasks:task:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with
            {
                AsyncTasksSelectedIndex = Math.Clamp(row, 0, 7),
                AsyncTasksFocusedPanelIndex = 1
            };
            return true;
        }

        next = hit.LocalHitId switch
        {
            "async_tasks:scheduler" => next with
            {
                AsyncTasksPolicyIndex = (next.AsyncTasksPolicyIndex + 1) % 6,
                AsyncTasksFocusedPanelIndex = 0
            },
            "async_tasks:details" => next with { AsyncTasksFocusedPanelIndex = 2 },
            "async_tasks:activity" => next with { AsyncTasksFocusedPanelIndex = 3 },
            "async_tasks:evidence" => next with { AsyncTasksFocusedPanelIndex = 5 },
            "async_tasks:hazard" => next with { AsyncTasksFocusedPanelIndex = 4 },
            "async_tasks:footer" => next with
            {
                AsyncTasksAgingEnabled = !next.AsyncTasksAgingEnabled,
                AsyncTasksFocusedPanelIndex = 6
            },
            _ => next
        };
        return hit.LocalHitId is "async_tasks:scheduler" or "async_tasks:details" or
            "async_tasks:activity" or "async_tasks:evidence" or "async_tasks:hazard" or "async_tasks:footer";
    }

    private static bool HandleThemeStudioMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 30 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                { } value when value.StartsWith("theme_studio:preset:", StringComparison.Ordinal) => next with
                {
                    ThemeStudioPresetIndex = Math.Clamp(next.ThemeStudioPresetIndex + delta, 0, 4),
                    ThemeStudioFocusIndex = 0
                },
                { } value when value.StartsWith("theme_studio:token:", StringComparison.Ordinal) => next with
                {
                    ThemeStudioTokenIndex = Math.Clamp(next.ThemeStudioTokenIndex + delta, 0, 24),
                    ThemeStudioFocusIndex = 1
                },
                "theme_studio:diagnostics" => next with
                {
                    ThemeStudioDiagnosticsScroll = Math.Clamp(next.ThemeStudioDiagnosticsScroll + delta, 0, 8),
                    ThemeStudioFocusIndex = 3
                },
                _ => next
            };
            return hit.LocalHitId == "theme_studio:diagnostics" ||
                hit.LocalHitId.StartsWith("theme_studio:preset:", StringComparison.Ordinal) ||
                hit.LocalHitId.StartsWith("theme_studio:token:", StringComparison.Ordinal);
        }

        if (hit.LocalHitId.StartsWith("theme_studio:preset:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["theme_studio:preset:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with
            {
                ThemeStudioPresetIndex = Math.Clamp(row, 0, 4),
                ThemeStudioFocusIndex = 0,
                ThemeStudioExportArmed = gesture.Button == TerminalMouseButton.Right
            };
            return gesture.Button is TerminalMouseButton.Left or TerminalMouseButton.Right;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        if (hit.LocalHitId.StartsWith("theme_studio:token:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["theme_studio:token:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with
            {
                ThemeStudioTokenIndex = Math.Clamp(row, 0, 24),
                ThemeStudioFocusIndex = 1
            };
            return true;
        }

        next = hit.LocalHitId switch
        {
            "theme_studio:export" => next with
            {
                ThemeStudioExportArmed = true,
                ThemeStudioFocusIndex = 2
            },
            "theme_studio:diagnostics" => next with
            {
                ThemeStudioFocusIndex = 3,
                ThemeStudioDiagnosticsScroll = Math.Clamp(next.ThemeStudioDiagnosticsScroll + 1, 0, 8)
            },
            "theme_studio:footer" => next with
            {
                ThemeStudioFocusIndex = 4,
                ThemeStudioExportArmed = !next.ThemeStudioExportArmed
            },
            _ => next
        };
        return hit.LocalHitId is "theme_studio:export" or "theme_studio:diagnostics" or "theme_studio:footer";
    }

    private static bool HandleSnapshotPlayerMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        const int frameCount = 50;
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 31 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Up or TerminalMouseKind.Scroll or TerminalMouseKind.Drag))
        {
            return false;
        }

        static int FrameFromTimelineColumn(ShowcaseDemoState state, int column, int frameCount)
        {
            var innerWidth = Math.Max(1, state.Viewport.Width - 2);
            var timelineWidth = Math.Max(1, innerWidth * 60 / 100);
            var localColumn = Math.Clamp(column - 1, 0, timelineWidth - 1);
            return Math.Clamp(localColumn * frameCount / timelineWidth, 0, frameCount - 1);
        }

        if (next.SnapshotPlayerTimelineScrubbing &&
            gesture.Button == TerminalMouseButton.Left &&
            gesture.Kind is TerminalMouseKind.Drag or TerminalMouseKind.Up)
        {
            next = next with
            {
                SnapshotPlayerFrameIndex = FrameFromTimelineColumn(next, gesture.Column, frameCount),
                SnapshotPlayerFocusIndex = 0,
                SnapshotPlayerTimelineScrubbing = gesture.Kind != TerminalMouseKind.Up
            };
            return true;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        var currentFrame = next.SnapshotPlayerFrameIndex >= 0
            ? next.SnapshotPlayerFrameIndex
            : next.ScriptFrame % frameCount;
        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "snapshot_player:timeline" => next with
                {
                    SnapshotPlayerFrameIndex = Math.Clamp(currentFrame + delta, 0, frameCount - 1),
                    SnapshotPlayerFocusIndex = 0
                },
                "snapshot_player:diagnostics" => next with
                {
                    SnapshotPlayerDiagnosticsScroll = Math.Clamp(next.SnapshotPlayerDiagnosticsScroll + delta, 0, 8),
                    SnapshotPlayerFocusIndex = 5
                },
                _ => next
            };
            return hit.LocalHitId is "snapshot_player:timeline" or "snapshot_player:diagnostics";
        }

        if (hit.LocalHitId == "snapshot_player:timeline")
        {
            var selectedFrame = FrameFromTimelineColumn(next, gesture.Column, frameCount);
            next = next with
            {
                SnapshotPlayerFrameIndex = selectedFrame,
                SnapshotPlayerFocusIndex = 0,
                SnapshotPlayerTimelineScrubbing = gesture.Button == TerminalMouseButton.Left &&
                    gesture.Kind is TerminalMouseKind.Down or TerminalMouseKind.Drag,
                SnapshotPlayerMarkerEnabled = gesture.Button == TerminalMouseButton.Right
                    ? !next.SnapshotPlayerMarkerEnabled
                    : next.SnapshotPlayerMarkerEnabled
            };
            return gesture.Button is TerminalMouseButton.Left or TerminalMouseButton.Right;
        }

        if (hit.LocalHitId == "snapshot_player:preview")
        {
            next = next with
            {
                SnapshotPlayerFocusIndex = 1,
                SnapshotPlayerHeatmapEnabled = gesture.Button == TerminalMouseButton.Right
                    ? !next.SnapshotPlayerHeatmapEnabled
                    : next.SnapshotPlayerHeatmapEnabled
            };
            return gesture.Button is TerminalMouseButton.Left or TerminalMouseButton.Right;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "snapshot_player:compare" => next with
            {
                SnapshotPlayerFocusIndex = 2,
                SnapshotPlayerCompareIndex = (next.SnapshotPlayerCompareIndex + 1) % 2
            },
            "snapshot_player:frame_info" => next with
            {
                SnapshotPlayerFocusIndex = 3
            },
            "snapshot_player:controls" => next with
            {
                SnapshotPlayerFocusIndex = 4,
                SnapshotPlayerPlaying = !next.SnapshotPlayerPlaying
            },
            "snapshot_player:diagnostics" => next with
            {
                SnapshotPlayerFocusIndex = 5,
                SnapshotPlayerDiagnosticsScroll = Math.Clamp(next.SnapshotPlayerDiagnosticsScroll + 1, 0, 8)
            },
            _ => next
        };
        return hit.LocalHitId is "snapshot_player:compare" or "snapshot_player:frame_info" or "snapshot_player:controls" or "snapshot_player:diagnostics";
    }

    private static bool HandlePerformanceChallengeMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 32 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "performance_challenge:sparkline" => next with
                {
                    PerformanceChallengeSparklineModeIndex = Math.Clamp(next.PerformanceChallengeSparklineModeIndex + delta, 0, 1),
                    PerformanceChallengeFocusIndex = 2
                },
                "performance_challenge:evidence" => next with
                {
                    PerformanceChallengeEvidenceScroll = Math.Clamp(next.PerformanceChallengeEvidenceScroll + delta, 0, 8),
                    PerformanceChallengeFocusIndex = 3
                },
                "performance_challenge:budget" => next with
                {
                    PerformanceChallengeBudgetMs = Math.Clamp(next.PerformanceChallengeBudgetMs - delta, 1, 100),
                    PerformanceChallengeFocusIndex = 4
                },
                _ => next
            };
            return hit.LocalHitId is "performance_challenge:sparkline" or "performance_challenge:evidence" or "performance_challenge:budget";
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        if (hit.LocalHitId.StartsWith("performance_challenge:tier:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["performance_challenge:tier:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with
            {
                PerformanceChallengeForcedTierIndex = Math.Clamp(row, 0, 3),
                PerformanceChallengeFocusIndex = 6
            };
            return true;
        }

        next = hit.LocalHitId switch
        {
            "performance_challenge:header" => next with
            {
                PerformanceChallengePaused = !next.PerformanceChallengePaused,
                PerformanceChallengeFocusIndex = 0
            },
            "performance_challenge:metrics" => next with
            {
                PerformanceChallengeFocusIndex = 1
            },
            "performance_challenge:sparkline" => next with
            {
                PerformanceChallengeSparklineModeIndex = (next.PerformanceChallengeSparklineModeIndex + 1) % 2,
                PerformanceChallengeFocusIndex = 2
            },
            "performance_challenge:evidence" => next with
            {
                PerformanceChallengeEvidenceScroll = Math.Clamp(next.PerformanceChallengeEvidenceScroll + 1, 0, 8),
                PerformanceChallengeFocusIndex = 3
            },
            "performance_challenge:budget" => next with
            {
                PerformanceChallengeBudgetMs = Math.Clamp(next.PerformanceChallengeBudgetMs + 1, 1, 100),
                PerformanceChallengeFocusIndex = 4
            },
            "performance_challenge:stress" => next with
            {
                PerformanceChallengeStressModeIndex = (next.PerformanceChallengeStressModeIndex + 1) % 4,
                PerformanceChallengeStressLoad = Math.Clamp(next.PerformanceChallengeStressLoad + 25, 0, 100),
                PerformanceChallengeFocusIndex = 5
            },
            "performance_challenge:footer" => next with
            {
                PerformanceChallengeForcedTierIndex = -1,
                PerformanceChallengeStressLoad = 0,
                PerformanceChallengeFocusIndex = 7
            },
            _ => next
        };
        return hit.LocalHitId is
            "performance_challenge:header" or
            "performance_challenge:metrics" or
            "performance_challenge:sparkline" or
            "performance_challenge:evidence" or
            "performance_challenge:budget" or
            "performance_challenge:stress" or
            "performance_challenge:footer";
    }

    private static bool HandleExplainabilityMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 33 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "explainability:timeline" => next with
                {
                    ExplainabilityTimelineScroll = Math.Clamp(next.ExplainabilityTimelineScroll - delta, 0, 8),
                    ExplainabilityFocusIndex = 4
                },
                "explainability:source_controls" => next with
                {
                    ExplainabilitySourceScroll = Math.Clamp(next.ExplainabilitySourceScroll + delta, 0, 8),
                    ExplainabilityFocusIndex = 5
                },
                _ => next
            };
            return hit.LocalHitId is "explainability:timeline" or "explainability:source_controls";
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "explainability:header" => next with
            {
                ExplainabilityPaused = !next.ExplainabilityPaused,
                ExplainabilityAutoRefresh = next.ExplainabilityPaused,
                ExplainabilityFocusIndex = 0
            },
            "explainability:diff_strategy" => next with
            {
                ExplainabilityFocusIndex = 1
            },
            "explainability:resize_regime" => next with
            {
                ExplainabilityFocusIndex = 2
            },
            "explainability:budget_decisions" => next with
            {
                ExplainabilityFocusIndex = 3
            },
            "explainability:timeline" => next with
            {
                ExplainabilityTimelineScroll = Math.Clamp(next.ExplainabilityTimelineScroll + 1, 0, 8),
                ExplainabilityFocusIndex = 4
            },
            "explainability:source_controls" => next with
            {
                ExplainabilityOverlayMode = !next.ExplainabilityOverlayMode,
                ExplainabilityFocusIndex = 5
            },
            _ => next
        };
        return hit.LocalHitId is
            "explainability:header" or
            "explainability:diff_strategy" or
            "explainability:resize_regime" or
            "explainability:budget_decisions" or
            "explainability:timeline" or
            "explainability:source_controls";
    }

    private static bool HandleI18nMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 34 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "i18n:plural_rules" => next with
                {
                    I18nPluralCount = Math.Clamp(next.I18nPluralCount - delta, 0, 21),
                    I18nFocusIndex = 2
                },
                "i18n:stress_lab" => next with
                {
                    I18nStressSampleIndex = Math.Clamp(next.I18nStressSampleIndex + delta, 0, 3),
                    I18nFocusIndex = 4
                },
                _ => next
            };
            return hit.LocalHitId is "i18n:plural_rules" or "i18n:stress_lab";
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        if (hit.LocalHitId == "i18n:locale_bar")
        {
            var innerWidth = Math.Max(1, next.Viewport.Width - 2);
            var localeIndex = Math.Clamp((gesture.Column - 1) * 6 / innerWidth, 0, 5);
            next = next with
            {
                I18nLocaleIndex = localeIndex,
                I18nRtlEnabled = localeIndex == 4,
                I18nFocusIndex = 0
            };
            return true;
        }

        next = hit.LocalHitId switch
        {
            "i18n:string_lookup" => next with
            {
                I18nFocusIndex = 1
            },
            "i18n:plural_rules" => next with
            {
                I18nPluralCount = Math.Clamp(next.I18nPluralCount + 1, 0, 21),
                I18nFocusIndex = 2
            },
            "i18n:rtl_layout" => next with
            {
                I18nRtlEnabled = !next.I18nRtlEnabled,
                I18nFocusIndex = 3
            },
            "i18n:stress_lab" => next with
            {
                I18nStressSampleIndex = (next.I18nStressSampleIndex + 1) % 4,
                I18nFocusIndex = 4
            },
            "i18n:footer" => next with
            {
                I18nExportArmed = !next.I18nExportArmed,
                I18nFocusIndex = 5
            },
            _ => next
        };
        return hit.LocalHitId is "i18n:string_lookup" or "i18n:plural_rules" or "i18n:rtl_layout" or "i18n:stress_lab" or "i18n:footer";
    }

    private static bool HandleVoiOverlayMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 35 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "voi_overlay:ledger" => next with
                {
                    VoiOverlayLedgerIndex = Math.Clamp(next.VoiOverlayLedgerIndex + delta, 0, 2),
                    VoiOverlayFocusIndex = 4
                },
                "voi_overlay:controls" => next with
                {
                    VoiOverlayControlsScroll = Math.Clamp(next.VoiOverlayControlsScroll + delta, 0, 8),
                    VoiOverlayFocusIndex = 5
                },
                _ => next
            };
            return hit.LocalHitId is "voi_overlay:ledger" or "voi_overlay:controls";
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "voi_overlay:header" => next with
            {
                VoiOverlayVisible = !next.VoiOverlayVisible,
                VoiOverlayFocusIndex = 0
            },
            "voi_overlay:decision" => next with
            {
                VoiOverlayFocusIndex = 1
            },
            "voi_overlay:posterior" => next with
            {
                VoiOverlayFocusIndex = 2
            },
            "voi_overlay:observation" => next with
            {
                VoiOverlayFocusIndex = 3
            },
            "voi_overlay:ledger" => next with
            {
                VoiOverlayLedgerIndex = (next.VoiOverlayLedgerIndex + 1) % 3,
                VoiOverlayFocusIndex = 4
            },
            "voi_overlay:controls" => next with
            {
                VoiOverlayDetailExpanded = !next.VoiOverlayDetailExpanded,
                VoiOverlayFocusIndex = 5
            },
            "voi_overlay:footer" => next with
            {
                VoiOverlayResetCount = next.VoiOverlayResetCount + 1,
                VoiOverlayLedgerIndex = 0
            },
            _ => next
        };
        return hit.LocalHitId is
            "voi_overlay:header" or
            "voi_overlay:decision" or
            "voi_overlay:posterior" or
            "voi_overlay:observation" or
            "voi_overlay:ledger" or
            "voi_overlay:controls" or
            "voi_overlay:footer";
    }

    private static bool HandleInlineModeMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 36 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "inline_mode:inline_story" => next with
                {
                    InlineModeLogRateIndex = Math.Clamp(ResolveInlineModeLogRateIndex(next) - delta, 0, 3),
                    InlineModeFocusIndex = 1
                },
                "inline_mode:state_limits" => next with
                {
                    InlineModeStateScroll = Math.Clamp(next.InlineModeStateScroll + delta, 0, 8),
                    InlineModeFocusIndex = 4
                },
                _ => next
            };
            return hit.LocalHitId is "inline_mode:inline_story" or "inline_mode:state_limits";
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "inline_mode:header" => next with
            {
                InlineModeCompareEnabled = !next.InlineModeCompareEnabled,
                InlineModeFocusIndex = 0
            },
            "inline_mode:inline_story" => next with
            {
                InlineModePaused = !next.InlineModePaused,
                InlineModeFocusIndex = 1
            },
            "inline_mode:alt_story" => next with
            {
                InlineModeAnchorBottom = !next.InlineModeAnchorBottom,
                InlineModeFocusIndex = 2
            },
            "inline_mode:controls" => next with
            {
                InlineModeUiHeightIndex = Math.Clamp(ResolveInlineModeUiHeightIndex(next) + 1, 0, 3),
                InlineModeFocusIndex = 3
            },
            "inline_mode:state_limits" => next with
            {
                InlineModeStateScroll = Math.Clamp(next.InlineModeStateScroll + 1, 0, 8),
                InlineModeFocusIndex = 4
            },
            "inline_mode:footer" => next with
            {
                InlineModeCompareEnabled = false,
                InlineModePaused = false,
                InlineModeStateScroll = 0,
                InlineModeFocusIndex = 5
            },
            _ => next
        };
        return hit.LocalHitId is "inline_mode:header" or "inline_mode:inline_story" or "inline_mode:alt_story" or "inline_mode:controls" or "inline_mode:state_limits" or "inline_mode:footer";
    }

    private static int ResolveInlineModeLogRateIndex(ShowcaseDemoState state) =>
        state.InlineModeLogRateIndex >= 0 ? state.InlineModeLogRateIndex : Math.Abs(state.ScriptFrame % 4);

    private static int ResolveInlineModeUiHeightIndex(ShowcaseDemoState state) =>
        state.InlineModeUiHeightIndex >= 0 ? state.InlineModeUiHeightIndex : Math.Abs(state.ScriptFrame % 4);

    private static bool HandleAccessibilityMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 37 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "accessibility:preview" => next with
                {
                    AccessibilityPreviewScroll = Math.Clamp(next.AccessibilityPreviewScroll + delta, 0, 8),
                    AccessibilityFocusIndex = 2
                },
                "accessibility:telemetry" => next with
                {
                    AccessibilityTelemetryScroll = Math.Clamp(next.AccessibilityTelemetryScroll + delta, 0, 8),
                    AccessibilityFocusIndex = 4
                },
                _ => next
            };
            return hit.LocalHitId is "accessibility:preview" or "accessibility:telemetry";
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        if (hit.LocalHitId == "accessibility:toggles")
        {
            var selected = Math.Clamp((gesture.Row - 9) / 2, 0, 2);
            next = selected switch
            {
                0 => (next with
                {
                    A11yHighContrast = !next.A11yHighContrast,
                    AccessibilitySelectedToggleIndex = 0,
                    AccessibilityFocusIndex = 1
                }).RecordA11yTelemetry("HighContrast"),
                1 => (next with
                {
                    A11yReducedMotion = !next.A11yReducedMotion,
                    AccessibilitySelectedToggleIndex = 1,
                    AccessibilityFocusIndex = 1
                }).RecordA11yTelemetry("ReducedMotion"),
                _ => (next with
                {
                    A11yLargeText = !next.A11yLargeText,
                    AccessibilitySelectedToggleIndex = 2,
                    AccessibilityFocusIndex = 1
                }).RecordA11yTelemetry("LargeText")
            };
            return true;
        }

        next = hit.LocalHitId switch
        {
            "accessibility:overview" => next with
            {
                AccessibilityFocusIndex = 0
            },
            "accessibility:preview" => next with
            {
                AccessibilityPreviewScroll = Math.Clamp(next.AccessibilityPreviewScroll + 1, 0, 8),
                AccessibilityFocusIndex = 2
            },
            "accessibility:wcag" => next with
            {
                AccessibilityFocusIndex = 3
            },
            "accessibility:telemetry" => next with
            {
                AccessibilityTelemetryScroll = Math.Clamp(next.AccessibilityTelemetryScroll + 1, 0, 8),
                AccessibilityFocusIndex = 4
            },
            "accessibility:footer" => (next with
            {
                A11yHighContrast = false,
                A11yReducedMotion = false,
                A11yLargeText = false,
                AccessibilityPreviewScroll = 0,
                AccessibilityTelemetryScroll = 0,
                AccessibilityFocusIndex = 5
            }).RecordA11yTelemetry("Panel"),
            _ => next
        };
        return hit.LocalHitId is "accessibility:overview" or "accessibility:preview" or "accessibility:wcag" or "accessibility:telemetry" or "accessibility:footer";
    }

    private static bool HandleWidgetBuilderMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 38 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "widget_builder:presets" => next with
                {
                    WidgetBuilderPresetIndex = Math.Clamp(ResolveWidgetBuilderPresetIndex(next) + delta, 0, 2),
                    WidgetBuilderFocusIndex = 1
                },
                "widget_builder:tree" => next with
                {
                    WidgetBuilderSelectedIndex = Math.Clamp(ResolveWidgetBuilderSelectedIndex(next) + delta, 0, 4),
                    WidgetBuilderTreeScroll = Math.Clamp(next.WidgetBuilderTreeScroll + delta, 0, 8),
                    WidgetBuilderFocusIndex = 2
                },
                "widget_builder:props" => next with
                {
                    WidgetBuilderPropsScroll = Math.Clamp(next.WidgetBuilderPropsScroll + delta, 0, 8),
                    WidgetBuilderValue = Math.Clamp(ResolveWidgetBuilderValue(next) + delta * 5, 0, 100),
                    WidgetBuilderFocusIndex = 4
                },
                _ => next
            };
            return hit.LocalHitId is "widget_builder:presets" or "widget_builder:tree" or "widget_builder:props";
        }

        if (hit.LocalHitId == "widget_builder:presets" && gesture.Button == TerminalMouseButton.Right)
        {
            next = next with
            {
                WidgetBuilderPresetSaved = true,
                WidgetBuilderFocusIndex = 1
            };
            return true;
        }

        if (hit.LocalHitId == "widget_builder:tree" && gesture.Button == TerminalMouseButton.Right)
        {
            next = next with
            {
                WidgetBuilderBorderEnabled = !next.WidgetBuilderBorderEnabled,
                WidgetBuilderFocusIndex = 2
            };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "widget_builder:header" => next with
            {
                WidgetBuilderFocusIndex = 0
            },
            "widget_builder:presets" => next with
            {
                WidgetBuilderPresetIndex = Math.Clamp((gesture.Row - 4), 0, 2),
                WidgetBuilderFocusIndex = 1
            },
            "widget_builder:tree" => next with
            {
                WidgetBuilderSelectedIndex = Math.Clamp((gesture.Row - 15), 0, 4),
                WidgetBuilderFocusIndex = 2
            },
            "widget_builder:preview" => next with
            {
                WidgetBuilderPreviewEnabled = !next.WidgetBuilderPreviewEnabled,
                WidgetBuilderFocusIndex = 3
            },
            "widget_builder:props" => next with
            {
                WidgetBuilderPropsScroll = Math.Clamp(next.WidgetBuilderPropsScroll + 1, 0, 8),
                WidgetBuilderFocusIndex = 4
            },
            "widget_builder:export" => next with
            {
                WidgetBuilderExportArmed = true,
                WidgetBuilderFocusIndex = 5
            },
            "widget_builder:footer" => next with
            {
                WidgetBuilderPresetSaved = false,
                WidgetBuilderExportArmed = false,
                WidgetBuilderFocusIndex = 6
            },
            _ => next
        };
        return hit.LocalHitId is "widget_builder:header" or "widget_builder:presets" or "widget_builder:tree" or "widget_builder:preview" or "widget_builder:props" or "widget_builder:export" or "widget_builder:footer";
    }

    private static bool HandleWidgetBuilderKey(KeyGesture gesture, ref ShowcaseDemoState next)
    {
        if (next.CurrentScreenNumber != 38 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Modifiers.HasFlag(TerminalModifiers.Control) ||
            gesture.Modifiers.HasFlag(TerminalModifiers.Alt))
        {
            return false;
        }

        var selected = ResolveWidgetBuilderSelectedIndex(next);
        if (gesture.Key == TerminalKey.Down || IsCharacter(gesture, 'j'))
        {
            next = next with
            {
                WidgetBuilderSelectedIndex = (selected + 1) % 5,
                WidgetBuilderFocusIndex = 2
            };
            return true;
        }

        if (gesture.Key == TerminalKey.Up || IsCharacter(gesture, 'k'))
        {
            next = next with
            {
                WidgetBuilderSelectedIndex = selected == 0 ? 4 : selected - 1,
                WidgetBuilderFocusIndex = 2
            };
            return true;
        }

        if (IsCharacter(gesture, 'p'))
        {
            next = next with
            {
                WidgetBuilderPresetIndex = (ResolveWidgetBuilderPresetIndex(next) + 1) % 3,
                WidgetBuilderSelectedIndex = 0,
                WidgetBuilderFocusIndex = 1
            };
            return true;
        }

        if (IsShiftCharacter(gesture, 'p'))
        {
            var preset = ResolveWidgetBuilderPresetIndex(next);
            next = next with
            {
                WidgetBuilderPresetIndex = preset == 0 ? 2 : preset - 1,
                WidgetBuilderSelectedIndex = 0,
                WidgetBuilderFocusIndex = 1
            };
            return true;
        }

        if (IsCharacter(gesture, 'r') || IsShiftCharacter(gesture, 'r'))
        {
            next = next with
            {
                WidgetBuilderSelectedIndex = 0,
                WidgetBuilderPreviewEnabled = true,
                WidgetBuilderBorderEnabled = true,
                WidgetBuilderTitleEnabled = true,
                WidgetBuilderAccentIndex = 0,
                WidgetBuilderValue = -1,
                WidgetBuilderFocusIndex = 6
            };
            return true;
        }

        if (IsCharacter(gesture, 's') || IsShiftCharacter(gesture, 's'))
        {
            next = next with
            {
                WidgetBuilderPresetSaved = true,
                WidgetBuilderFocusIndex = 1
            };
            return true;
        }

        if (IsCharacter(gesture, 'x') || IsShiftCharacter(gesture, 'x'))
        {
            next = next with
            {
                WidgetBuilderExportArmed = true,
                WidgetBuilderFocusIndex = 5
            };
            return true;
        }

        if (IsCharacter(gesture, 'e') || IsShiftCharacter(gesture, 'e'))
        {
            next = next with
            {
                WidgetBuilderPreviewEnabled = !next.WidgetBuilderPreviewEnabled,
                WidgetBuilderFocusIndex = 4
            };
            return true;
        }

        if (IsCharacter(gesture, 'b') || IsShiftCharacter(gesture, 'b'))
        {
            next = next with
            {
                WidgetBuilderBorderEnabled = !next.WidgetBuilderBorderEnabled,
                WidgetBuilderFocusIndex = 4
            };
            return true;
        }

        if (IsCharacter(gesture, 't') || IsShiftCharacter(gesture, 't'))
        {
            next = next with
            {
                WidgetBuilderTitleEnabled = !next.WidgetBuilderTitleEnabled,
                WidgetBuilderFocusIndex = 4
            };
            return true;
        }

        if (IsCharacter(gesture, 'c') || IsShiftCharacter(gesture, 'c'))
        {
            next = next with
            {
                WidgetBuilderAccentIndex = (Math.Clamp(next.WidgetBuilderAccentIndex, 0, 5) + 1) % 6,
                WidgetBuilderFocusIndex = 4
            };
            return true;
        }

        if (IsCharacter(gesture, '['))
        {
            next = next with
            {
                WidgetBuilderValue = Math.Clamp(ResolveWidgetBuilderValue(next) - 5, 0, 100),
                WidgetBuilderFocusIndex = 4
            };
            return true;
        }

        if (IsCharacter(gesture, ']'))
        {
            next = next with
            {
                WidgetBuilderValue = Math.Clamp(ResolveWidgetBuilderValue(next) + 5, 0, 100),
                WidgetBuilderFocusIndex = 4
            };
            return true;
        }

        return false;
    }

    private static int ResolveWidgetBuilderPresetIndex(ShowcaseDemoState state) =>
        state.WidgetBuilderPresetIndex >= 0 ? state.WidgetBuilderPresetIndex : Math.Abs(state.ScriptFrame) % 3;

    private static int ResolveWidgetBuilderSelectedIndex(ShowcaseDemoState state) =>
        state.WidgetBuilderSelectedIndex >= 0 ? state.WidgetBuilderSelectedIndex : Math.Abs(state.ScriptFrame) % 4;

    private static int ResolveWidgetBuilderValue(ShowcaseDemoState state) =>
        state.WidgetBuilderValue >= 0 ? state.WidgetBuilderValue : 40 + (Math.Abs(state.ScriptFrame) % 12) * 5;

    private static bool HandleDeterminismMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 40 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "determinism:equivalence" => next with
                {
                    DeterminismStrategyIndex = Math.Clamp(next.DeterminismStrategyIndex + delta, 0, 2),
                    DeterminismFocusIndex = 1
                },
                "determinism:report" => next with
                {
                    DeterminismReportScroll = Math.Clamp(next.DeterminismReportScroll + delta, 0, 8),
                    DeterminismFocusIndex = 2
                },
                "determinism:checks" => next with
                {
                    DeterminismChecksScroll = Math.Clamp(next.DeterminismChecksScroll + delta, 0, 8),
                    DeterminismFocusIndex = 4
                },
                _ => next
            };
            return hit.LocalHitId is "determinism:equivalence" or "determinism:report" or "determinism:checks";
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "determinism:header" => next with
            {
                DeterminismFaultEnabled = !next.DeterminismFaultEnabled,
                DeterminismFocusIndex = 0
            },
            "determinism:equivalence" => next with
            {
                DeterminismStrategyIndex = (next.DeterminismStrategyIndex + 1) % 3,
                DeterminismFocusIndex = 1
            },
            "determinism:report" => next with
            {
                DeterminismExportArmed = true,
                DeterminismFocusIndex = 2
            },
            "determinism:preview" => next with
            {
                DeterminismSeedOffset = Math.Clamp(next.DeterminismSeedOffset + 1, 0, 10),
                DeterminismFocusIndex = 3
            },
            "determinism:checks" => next with
            {
                DeterminismScenarioIndex = (next.DeterminismScenarioIndex + 1) % 3,
                DeterminismRunCount = next.DeterminismRunCount + 1,
                DeterminismFocusIndex = 4
            },
            "determinism:footer" => next with
            {
                DeterminismStrategyIndex = 1,
                DeterminismScenarioIndex = 0,
                DeterminismSeedOffset = 0,
                DeterminismFaultEnabled = false,
                DeterminismExportArmed = false,
                DeterminismFocusIndex = 5
            },
            _ => next
        };
        return hit.LocalHitId is "determinism:header" or "determinism:equivalence" or "determinism:report" or "determinism:preview" or "determinism:checks" or "determinism:footer";
    }

    private static bool HandleDeterminismKey(KeyGesture gesture, ref ShowcaseDemoState next)
    {
        if (next.CurrentScreenNumber != 40 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Modifiers.HasFlag(TerminalModifiers.Control) ||
            gesture.Modifiers.HasFlag(TerminalModifiers.Alt))
        {
            return false;
        }

        if (IsCharacter(gesture, '1') || IsCharacter(gesture, '2') || IsCharacter(gesture, '3'))
        {
            var strategy = gesture.Character!.Value.Value - '1';
            next = next with
            {
                DeterminismStrategyIndex = Math.Clamp(strategy, 0, 2),
                DeterminismFocusIndex = 1
            };
            return true;
        }

        if (IsCharacter(gesture, '['))
        {
            next = next with
            {
                DeterminismSeedOffset = Math.Clamp(next.DeterminismSeedOffset - 1, 0, 10),
                DeterminismFocusIndex = 3
            };
            return true;
        }

        if (IsCharacter(gesture, ']'))
        {
            next = next with
            {
                DeterminismSeedOffset = Math.Clamp(next.DeterminismSeedOffset + 1, 0, 10),
                DeterminismFocusIndex = 3
            };
            return true;
        }

        if (IsCharacter(gesture, ' '))
        {
            next = next with
            {
                DeterminismPaused = !next.DeterminismPaused,
                DeterminismFocusIndex = 0
            };
            return true;
        }

        if (IsCharacter(gesture, 'f') || IsShiftCharacter(gesture, 'f'))
        {
            next = next with
            {
                DeterminismFaultEnabled = !next.DeterminismFaultEnabled,
                DeterminismFocusIndex = 0
            };
            return true;
        }

        if (IsCharacter(gesture, 'e') || IsShiftCharacter(gesture, 'e'))
        {
            next = next with
            {
                DeterminismExportArmed = true,
                DeterminismFocusIndex = 2
            };
            return true;
        }

        if (gesture.Key == TerminalKey.Enter || IsCharacter(gesture, 'r') || IsShiftCharacter(gesture, 'r'))
        {
            next = next with
            {
                DeterminismRunCount = next.DeterminismRunCount + 1,
                DeterminismFocusIndex = 4
            };
            return true;
        }

        if (IsCharacter(gesture, 'a') || IsShiftCharacter(gesture, 'a'))
        {
            next = next with
            {
                DeterminismRunCount = next.DeterminismRunCount + 3,
                DeterminismFocusIndex = 4
            };
            return true;
        }

        if (IsCharacter(gesture, 'c') || IsShiftCharacter(gesture, 'c'))
        {
            next = next with
            {
                DeterminismChecksumLogged = true,
                DeterminismFocusIndex = 2
            };
            return true;
        }

        if (IsCharacter(gesture, 'x') || IsShiftCharacter(gesture, 'x'))
        {
            next = next with
            {
                DeterminismStrategyIndex = 1,
                DeterminismScenarioIndex = 0,
                DeterminismSeedOffset = 0,
                DeterminismReportScroll = 0,
                DeterminismChecksScroll = 0,
                DeterminismRunCount = 0,
                DeterminismFaultEnabled = false,
                DeterminismExportArmed = false,
                DeterminismPaused = false,
                DeterminismChecksumLogged = false,
                DeterminismFocusIndex = 5
            };
            return true;
        }

        return false;
    }

    private static bool HandleHyperlinkMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 41 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Move or TerminalMouseKind.Down or TerminalMouseKind.Up or TerminalMouseKind.Drag or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            if (hit.Layer != ShowcaseHitLayer.Link)
            {
                return false;
            }

            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = next with
            {
                HyperlinkFocusIndex = Math.Clamp(next.HyperlinkFocusIndex + delta, 0, 4),
                HyperlinkLastActionIndex = 1
            };
            return true;
        }

        if (hit.Layer != ShowcaseHitLayer.Link || hit.UpstreamHitId is null)
        {
            return false;
        }

        var linkIndex = Math.Clamp((int)(hit.UpstreamHitId.Value - ShowcaseFrameHitRegistry.LinkHitBase), 0, 4);
        if (gesture.Kind is TerminalMouseKind.Move or TerminalMouseKind.Drag)
        {
            next = next with
            {
                HyperlinkHoverIndex = linkIndex,
                HyperlinkLastActionIndex = 1
            };
            return true;
        }

        if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Right)
        {
            next = next with
            {
                HyperlinkFocusIndex = linkIndex,
                HyperlinkHoverIndex = linkIndex,
                HyperlinkLastActionIndex = 3,
                HyperlinkCopied = true
            };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Down)
        {
            next = next with
            {
                HyperlinkFocusIndex = linkIndex,
                HyperlinkHoverIndex = linkIndex,
                HyperlinkLastActionIndex = 2
            };
            return true;
        }

        if (gesture.Kind == TerminalMouseKind.Up)
        {
            next = next with
            {
                HyperlinkFocusIndex = linkIndex,
                HyperlinkHoverIndex = linkIndex,
                HyperlinkLastActionIndex = 4,
                HyperlinkActivationCount = next.HyperlinkActivationCount + 1
            };
            return true;
        }

        return false;
    }

    private static bool HandleHyperlinkKey(KeyGesture gesture, ref ShowcaseDemoState next)
    {
        if (next.CurrentScreenNumber != 41 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Modifiers.HasFlag(TerminalModifiers.Control) ||
            gesture.Modifiers.HasFlag(TerminalModifiers.Alt))
        {
            return false;
        }

        var active = next.HyperlinkHoverIndex >= 0
            ? Math.Clamp(next.HyperlinkHoverIndex, 0, 4)
            : Math.Clamp(next.HyperlinkFocusIndex, 0, 4);

        if (gesture.Key == TerminalKey.Up || (gesture.Key == TerminalKey.Tab && gesture.Modifiers.HasFlag(TerminalModifiers.Shift)))
        {
            next = next with
            {
                HyperlinkFocusIndex = active == 0 ? 4 : active - 1,
                HyperlinkHoverIndex = -1,
                HyperlinkLastActionIndex = 1
            };
            return true;
        }

        if (gesture.Key == TerminalKey.Down || gesture.Key == TerminalKey.Tab)
        {
            next = next with
            {
                HyperlinkFocusIndex = (active + 1) % 5,
                HyperlinkHoverIndex = -1,
                HyperlinkLastActionIndex = 1
            };
            return true;
        }

        if (gesture.Key == TerminalKey.Enter || IsCharacter(gesture, ' '))
        {
            next = next with
            {
                HyperlinkFocusIndex = active,
                HyperlinkHoverIndex = -1,
                HyperlinkLastActionIndex = 4,
                HyperlinkActivationCount = next.HyperlinkActivationCount + 1
            };
            return true;
        }

        if (IsCharacter(gesture, 'c'))
        {
            next = next with
            {
                HyperlinkFocusIndex = active,
                HyperlinkHoverIndex = -1,
                HyperlinkLastActionIndex = 3,
                HyperlinkCopied = true
            };
            return true;
        }

        return false;
    }

    private static bool HandleTerminalCapabilitiesMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 12 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "terminal_capabilities:matrix" => next with
                {
                    TerminalCapabilitiesSelectedRow = Math.Clamp(next.TerminalCapabilitiesSelectedRow + delta, 0, 5)
                },
                "terminal_capabilities:simulation" => next with
                {
                    TerminalCapabilitiesProfileIndex = Math.Clamp(next.TerminalCapabilitiesProfileIndex + delta, 0, 5)
                },
                _ => next
            };
            return hit.LocalHitId is "terminal_capabilities:matrix" or "terminal_capabilities:simulation";
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "terminal_capabilities:matrix" => next with
            {
                TerminalCapabilitiesSelectedRow = Math.Clamp(gesture.Row - 5, 0, 5)
            },
            "terminal_capabilities:simulation" => next with
            {
                TerminalCapabilitiesProfileIndex = Math.Clamp(next.TerminalCapabilitiesProfileIndex + 1, 0, 5)
            },
            _ => next
        };
        return hit.LocalHitId is "terminal_capabilities:matrix" or "terminal_capabilities:simulation";
    }

    private static bool HandleWidgetGalleryMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 5 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            next = hit.LocalHitId switch
            {
                "widget_gallery:list" => next with
                {
                    WidgetGalleryFocusIndex = 1,
                    WidgetGalleryContextArmed = false,
                    WidgetGalleryListIndex = Math.Clamp(next.WidgetGalleryListIndex + delta, 0, 6)
                },
                "widget_gallery:tabs" => next with
                {
                    WidgetGalleryFocusIndex = 2,
                    WidgetGalleryContextArmed = false,
                    WidgetGalleryTabIndex = Math.Clamp(next.WidgetGalleryTabIndex + delta, 0, 3)
                },
                "widget_gallery:table" => next with
                {
                    WidgetGalleryFocusIndex = 3,
                    WidgetGalleryContextArmed = false,
                    WidgetGalleryTableRow = Math.Clamp(next.WidgetGalleryTableRow + delta, 0, 3)
                },
                _ => next
            };
            return hit.LocalHitId is "widget_gallery:list" or "widget_gallery:tabs" or "widget_gallery:table";
        }

        if (gesture.Button == TerminalMouseButton.Right &&
            hit.LocalHitId is "widget_gallery:progress" or "widget_gallery:list" or "widget_gallery:tabs" or "widget_gallery:table")
        {
            next = next with
            {
                WidgetGalleryFocusIndex = hit.LocalHitId switch
                {
                    "widget_gallery:list" => 1,
                    "widget_gallery:tabs" => 2,
                    "widget_gallery:table" => 3,
                    _ => 0
                },
                WidgetGalleryContextArmed = true
            };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "widget_gallery:progress" => next with
            {
                WidgetGalleryFocusIndex = 0,
                WidgetGalleryContextArmed = false
            },
            "widget_gallery:list" => next with
            {
                WidgetGalleryFocusIndex = 1,
                WidgetGalleryContextArmed = false,
                WidgetGalleryListIndex = Math.Clamp(next.WidgetGalleryListIndex + 1, 0, 6)
            },
            "widget_gallery:tabs" => next with
            {
                WidgetGalleryFocusIndex = 2,
                WidgetGalleryContextArmed = false,
                WidgetGalleryTabIndex = Math.Clamp(next.WidgetGalleryTabIndex + 1, 0, 3)
            },
            "widget_gallery:table" => next with
            {
                WidgetGalleryFocusIndex = 3,
                WidgetGalleryContextArmed = false,
                WidgetGalleryTableRow = Math.Clamp(next.WidgetGalleryTableRow + 1, 0, 3)
            },
            _ => next
        };
        return hit.LocalHitId is "widget_gallery:progress" or "widget_gallery:list" or "widget_gallery:tabs" or "widget_gallery:table";
    }

    private static bool HandleFormsInputMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 7 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll && hit.LocalHitId == "forms_input:text_area")
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = next with
            {
                FormsInputFocusIndex = 1,
                FormsInputTextScroll = Math.Clamp(next.FormsInputTextScroll + delta, 0, 12)
            };
            return true;
        }

        if (gesture.Kind != TerminalMouseKind.Down ||
            gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        if (hit.LocalHitId == "forms_input:text_area")
        {
            next = next with { FormsInputFocusIndex = 1 };
            return true;
        }

        if (!hit.LocalHitId.StartsWith("forms_input:field:", StringComparison.Ordinal))
        {
            return false;
        }

        var fieldText = hit.LocalHitId["forms_input:field:".Length..];
        if (!int.TryParse(fieldText, CultureInfo.InvariantCulture, out var fieldIndex))
        {
            return false;
        }

        next = next with
        {
            FormsInputFocusIndex = 0,
            FormsInputSelectedFieldIndex = Math.Clamp(fieldIndex, 0, 2)
        };
        return true;
    }

    private static bool HandleMacroRecorderMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 13 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                { } value when value.StartsWith("macro_recorder:timeline:", StringComparison.Ordinal) => next with
                {
                    MacroRecorderFocusIndex = 1,
                    MacroRecorderContextArmed = false,
                    MacroRecorderTimelineIndex = Math.Clamp(next.MacroRecorderTimelineIndex + delta, 0, 4)
                },
                "macro_recorder:scenario_runner" => next with
                {
                    MacroRecorderFocusIndex = 3,
                    MacroRecorderContextArmed = false,
                    MacroRecorderScenarioIndex = Math.Clamp(next.MacroRecorderScenarioIndex + delta, 0, 2)
                },
                _ => next
            };
            return hit.LocalHitId.StartsWith("macro_recorder:timeline:", StringComparison.Ordinal) ||
                hit.LocalHitId == "macro_recorder:scenario_runner";
        }

        if (gesture.Button == TerminalMouseButton.Right &&
            (hit.LocalHitId == "macro_recorder:controls" ||
                hit.LocalHitId == "macro_recorder:event_detail" ||
                hit.LocalHitId == "macro_recorder:scenario_runner" ||
                hit.LocalHitId.StartsWith("macro_recorder:timeline:", StringComparison.Ordinal)))
        {
            next = next with
            {
                MacroRecorderFocusIndex = hit.LocalHitId switch
                {
                    { } value when value.StartsWith("macro_recorder:timeline:", StringComparison.Ordinal) => 1,
                    "macro_recorder:event_detail" => 2,
                    "macro_recorder:scenario_runner" => 3,
                    _ => 0
                },
                MacroRecorderContextArmed = true
            };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        if (hit.LocalHitId == "macro_recorder:controls")
        {
            next = next with
            {
                MacroRecorderFocusIndex = 0,
                MacroRecorderContextArmed = false
            };
            return true;
        }

        if (hit.LocalHitId.StartsWith("macro_recorder:timeline:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["macro_recorder:timeline:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with
            {
                MacroRecorderFocusIndex = 1,
                MacroRecorderContextArmed = false,
                MacroRecorderTimelineIndex = Math.Clamp(row, 0, 4)
            };
            return true;
        }

        if (hit.LocalHitId == "macro_recorder:event_detail")
        {
            next = next with
            {
                MacroRecorderFocusIndex = 2,
                MacroRecorderContextArmed = false
            };
            return true;
        }

        if (hit.LocalHitId == "macro_recorder:scenario_runner")
        {
            next = next with
            {
                MacroRecorderFocusIndex = 3,
                MacroRecorderContextArmed = false,
                MacroRecorderScenarioIndex = Math.Clamp(next.MacroRecorderScenarioIndex + 1, 0, 2)
            };
            return true;
        }

        return false;
    }

    private static bool HandlePerformanceMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 14 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "performance:list" or "performance:list:selected" => next with
                {
                    PerformanceFocusIndex = hit.LocalHitId == "performance:list:selected" ? 1 : 0,
                    PerformanceContextArmed = false,
                    PerformanceSelectedIndex = Math.Clamp(next.PerformanceSelectedIndex + delta, 0, 9_999)
                },
                "performance:stats" => next with
                {
                    PerformanceFocusIndex = 2,
                    PerformanceContextArmed = false
                },
                _ => next
            };
            return hit.LocalHitId is "performance:list" or "performance:list:selected" or "performance:stats";
        }

        if (gesture.Button == TerminalMouseButton.Right &&
            hit.LocalHitId is "performance:list" or "performance:list:selected" or "performance:stats" or "performance:footer")
        {
            next = next with
            {
                PerformanceFocusIndex = hit.LocalHitId switch
                {
                    "performance:list:selected" => 1,
                    "performance:stats" => 2,
                    "performance:footer" => 3,
                    _ => 0
                },
                PerformanceContextArmed = true
            };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "performance:list" or "performance:list:selected" => next with
            {
                PerformanceFocusIndex = hit.LocalHitId == "performance:list:selected" ? 1 : 0,
                PerformanceContextArmed = false,
                PerformanceSelectedIndex = Math.Clamp(next.PerformanceSelectedIndex + 1, 0, 9_999)
            },
            "performance:stats" => next with
            {
                PerformanceFocusIndex = 2,
                PerformanceContextArmed = false
            },
            "performance:footer" => next with
            {
                PerformanceFocusIndex = 3,
                PerformanceContextArmed = false
            },
            _ => next
        };
        return hit.LocalHitId is "performance:list" or "performance:list:selected" or "performance:stats" or "performance:footer";
    }

    private static bool HandleMarkdownMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 15 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            !hit.LocalHitId.StartsWith("markdown:", StringComparison.Ordinal))
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "markdown:renderer" => next with
                {
                    MarkdownActivePaneIndex = 0,
                    MarkdownContextArmed = false,
                    MarkdownRendererScroll = Math.Clamp(next.MarkdownRendererScroll + delta, 0, 12)
                },
                "markdown:stream" => next with
                {
                    MarkdownActivePaneIndex = 1,
                    MarkdownContextArmed = false,
                    MarkdownStreamScroll = Math.Clamp(next.MarkdownStreamScroll + delta, 0, 12)
                },
                "markdown:unicode" => next with
                {
                    MarkdownActivePaneIndex = 4,
                    MarkdownContextArmed = false
                },
                _ => next
            };
            return hit.LocalHitId is "markdown:renderer" or "markdown:stream" or "markdown:unicode";
        }

        if (gesture.Button == TerminalMouseButton.Right)
        {
            next = hit.LocalHitId switch
            {
                "markdown:renderer" => next with { MarkdownActivePaneIndex = 0, MarkdownContextArmed = true },
                "markdown:stream" => next with { MarkdownActivePaneIndex = 1, MarkdownContextArmed = true },
                "markdown:detection" => next with { MarkdownActivePaneIndex = 2, MarkdownContextArmed = true },
                "markdown:style" => next with { MarkdownActivePaneIndex = 3, MarkdownContextArmed = true },
                "markdown:unicode" => next with { MarkdownActivePaneIndex = 4, MarkdownContextArmed = true },
                "markdown:wrap" => next with { MarkdownActivePaneIndex = 5, MarkdownContextArmed = true },
                _ => next
            };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "markdown:renderer" => next with { MarkdownActivePaneIndex = 0, MarkdownContextArmed = false },
            "markdown:stream" => next with { MarkdownActivePaneIndex = 1, MarkdownContextArmed = false },
            "markdown:detection" => next with { MarkdownActivePaneIndex = 2, MarkdownContextArmed = false },
            "markdown:style" => next with { MarkdownActivePaneIndex = 3, MarkdownContextArmed = false },
            "markdown:unicode" => next with { MarkdownActivePaneIndex = 4, MarkdownContextArmed = false },
            "markdown:wrap" => next with
            {
                MarkdownActivePaneIndex = 5,
                MarkdownContextArmed = false,
                MarkdownWrapModeIndex = (next.MarkdownWrapModeIndex + 1) % 3
            },
            _ => next
        };
        return true;
    }

    private static bool HandleMermaidMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 16 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            !hit.LocalHitId.StartsWith("mermaid:", StringComparison.Ordinal))
        {
            return false;
        }

        var focus = hit.LocalHitId switch
        {
            "mermaid:header" => 0,
            "mermaid:library" => 1,
            "mermaid:viewport" => 2,
            "mermaid:controls" => 3,
            "mermaid:metrics" => 4,
            "mermaid:status" => 5,
            _ => next.MermaidFocusIndex
        };

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "mermaid:library" => next with
                {
                    MermaidFocusIndex = focus,
                    MermaidSampleIndex = Math.Clamp(next.MermaidSampleIndex + delta, 0, MermaidShowcaseSurface.Catalog().Count - 1),
                    MermaidContextArmed = false
                },
                "mermaid:viewport" => next with
                {
                    MermaidFocusIndex = focus,
                    MermaidZoomStep = Math.Clamp(next.MermaidZoomStep + delta, -5, 5),
                    MermaidContextArmed = false
                },
                "mermaid:status" => next with
                {
                    MermaidFocusIndex = focus,
                    MermaidStatusScroll = Math.Clamp(next.MermaidStatusScroll + delta, 0, 12),
                    MermaidContextArmed = false
                },
                _ => next with
                {
                    MermaidFocusIndex = focus,
                    MermaidPanelScroll = Math.Clamp(next.MermaidPanelScroll + delta, 0, 12),
                    MermaidContextArmed = false
                }
            };
            return true;
        }

        if (gesture.Button == TerminalMouseButton.Right)
        {
            next = hit.LocalHitId == "mermaid:viewport"
                ? next with
                {
                    MermaidFocusIndex = focus,
                    MermaidZoomStep = 0,
                    MermaidContextArmed = true
                }
                : next with
                {
                    MermaidFocusIndex = focus,
                    MermaidContextArmed = true
                };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId == "mermaid:library"
            ? next with
            {
                MermaidFocusIndex = focus,
                MermaidSampleIndex = (next.MermaidSampleIndex + 1) % MermaidShowcaseSurface.Catalog().Count,
                MermaidContextArmed = false
            }
            : next with
            {
                MermaidFocusIndex = focus,
                MermaidContextArmed = false
            };
        return true;
    }

    private static bool HandleMermaidMegaMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 17 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            !hit.LocalHitId.StartsWith("mermaid_mega:", StringComparison.Ordinal))
        {
            return false;
        }

        var focus = hit.LocalHitId switch
        {
            "mermaid_mega:shared_showcase" => 0,
            "mermaid_mega:library" => 1,
            "mermaid_mega:controls" => 2,
            "mermaid_mega:node_detail" => 3,
            _ => next.MermaidMegaFocusIndex
        };

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "mermaid_mega:library" => next with
                {
                    MermaidMegaFocusIndex = focus,
                    MermaidMegaSampleIndex = Math.Clamp(next.MermaidMegaSampleIndex + delta, 0, MermaidShowcaseSurface.Catalog().Count - 1),
                    MermaidMegaContextArmed = false
                },
                "mermaid_mega:shared_showcase" => next with
                {
                    MermaidMegaFocusIndex = focus,
                    MermaidMegaZoomStep = Math.Clamp(next.MermaidMegaZoomStep + delta, -5, 5),
                    MermaidMegaContextArmed = false
                },
                "mermaid_mega:node_detail" => next with
                {
                    MermaidMegaFocusIndex = focus,
                    MermaidMegaDetailScroll = Math.Clamp(next.MermaidMegaDetailScroll + delta, 0, 12),
                    MermaidMegaContextArmed = false
                },
                _ => next with
                {
                    MermaidMegaFocusIndex = focus,
                    MermaidMegaPanelScroll = Math.Clamp(next.MermaidMegaPanelScroll + delta, 0, 12),
                    MermaidMegaContextArmed = false
                }
            };
            return true;
        }

        if (gesture.Button == TerminalMouseButton.Right)
        {
            next = hit.LocalHitId == "mermaid_mega:shared_showcase"
                ? next with
                {
                    MermaidMegaFocusIndex = focus,
                    MermaidMegaZoomStep = 0,
                    MermaidMegaContextArmed = true
                }
                : next with
                {
                    MermaidMegaFocusIndex = focus,
                    MermaidMegaContextArmed = true
                };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId == "mermaid_mega:library"
            ? next with
            {
                MermaidMegaFocusIndex = focus,
                MermaidMegaSampleIndex = (next.MermaidMegaSampleIndex + 1) % MermaidShowcaseSurface.Catalog().Count,
                MermaidMegaContextArmed = false
            }
            : next with
            {
                MermaidMegaFocusIndex = focus,
                MermaidMegaContextArmed = false
            };
        return true;
    }

    private static bool HandleVisualEffectsMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 18 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            !hit.LocalHitId.StartsWith("visual_effects:", StringComparison.Ordinal))
        {
            return false;
        }

        var focus = hit.LocalHitId == "visual_effects:canvas" ? 0 : 1;
        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            if (hit.LocalHitId == "visual_effects:canvas")
            {
                var current = ResolveVisualEffectsEffectIndex(next);
                var count = ShowcaseVfxEffects.AllCanonicalKeys.Length;
                next = next with
                {
                    VisualEffectsFocusIndex = focus,
                    VisualEffectsEffectIndex = (current + delta + count) % count
                };
                return true;
            }

            next = next with
            {
                VisualEffectsFocusIndex = focus,
                VisualEffectsHarnessScroll = Math.Clamp(next.VisualEffectsHarnessScroll + delta, 0, 12)
            };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = next with { VisualEffectsFocusIndex = focus };
        return true;
    }

    private static int ResolveVisualEffectsEffectIndex(ShowcaseDemoState state)
    {
        if (state.VisualEffectsEffectIndex >= 0)
        {
            return Math.Clamp(state.VisualEffectsEffectIndex, 0, ShowcaseVfxEffects.AllCanonicalKeys.Length - 1);
        }

        var effect = ShowcaseVfxEffects.NormalizeOrDefault(state.VfxEffect);
        var index = Array.IndexOf(ShowcaseVfxEffects.AllCanonicalKeys, effect);
        return index < 0 ? 0 : index;
    }

    private static bool HandleResponsiveMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 19 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            !hit.LocalHitId.StartsWith("responsive:", StringComparison.Ordinal))
        {
            return false;
        }

        var focus = hit.LocalHitId switch
        {
            "responsive:indicator" => 0,
            "responsive:layout_info" => 1,
            "responsive:values" => 2,
            "responsive:sidebar" => 3,
            "responsive:content" => 4,
            "responsive:aside" => 5,
            _ => next.ResponsiveFocusIndex
        };

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -10 : 10;
            next = next with
            {
                ResponsiveFocusIndex = focus,
                ResponsiveWidthOffset = Math.Clamp(next.ResponsiveWidthOffset + delta, -80, 120)
            };
            return true;
        }

        if (gesture.Button == TerminalMouseButton.Right)
        {
            next = next with
            {
                ResponsiveFocusIndex = focus,
                ResponsiveWidthOffset = 0,
                ResponsiveCustomBreakpoints = false,
                ResponsiveAsideForcedVisible = false
            };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "responsive:indicator" => next with
            {
                ResponsiveFocusIndex = focus,
                ResponsiveCustomBreakpoints = !next.ResponsiveCustomBreakpoints
            },
            "responsive:aside" => next with
            {
                ResponsiveFocusIndex = focus,
                ResponsiveAsideForcedVisible = !next.ResponsiveAsideForcedVisible
            },
            _ => next with { ResponsiveFocusIndex = focus }
        };
        return true;
    }

    private static bool HandleLogSearchMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 20 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            !hit.LocalHitId.StartsWith("log_search:", StringComparison.Ordinal))
        {
            return false;
        }

        var isResult = TryParseLogSearchRow(hit.LocalHitId, "log_search:result:", out var resultRow);
        var isDiagnostic = TryParseLogSearchRow(hit.LocalHitId, "log_search:diagnostic:", out var diagnosticRow);
        var focus = isResult
            ? 0
            : hit.LocalHitId switch
            {
                "log_search:live_stream" => 1,
                "log_search:controls" => 2,
                _ when isDiagnostic => 3,
                _ => next.LogSearchFocusIndex
            };

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = isResult
                ? next with
                {
                    LogSearchFocusIndex = focus,
                    LogSearchResultScroll = Math.Clamp(next.LogSearchResultScroll + delta, 0, 40)
                }
                : isDiagnostic
                    ? next with
                    {
                        LogSearchFocusIndex = focus,
                        LogSearchDiagnosticsScroll = Math.Clamp(next.LogSearchDiagnosticsScroll + delta, 0, 12)
                    }
                    : next with { LogSearchFocusIndex = focus };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            _ when isResult => next with
            {
                LogSearchFocusIndex = focus,
                LogSearchSelectedResultIndex = Math.Clamp(resultRow + next.LogSearchResultScroll, 0, 99)
            },
            _ when isDiagnostic => next with
            {
                LogSearchFocusIndex = focus,
                LogSearchSelectedDiagnosticIndex = Math.Clamp(diagnosticRow + next.LogSearchDiagnosticsScroll, 0, 99)
            },
            "log_search:live_stream" => next with
            {
                LogSearchFocusIndex = focus,
                LogSearchPaused = !next.LogSearchPaused
            },
            _ => next with { LogSearchFocusIndex = focus }
        };
        return true;
    }

    private static bool TryParseLogSearchRow(string localHitId, string prefix, out int row)
    {
        row = 0;
        return localHitId.StartsWith(prefix, StringComparison.Ordinal) &&
            int.TryParse(localHitId[prefix.Length..], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out row);
    }

    private static bool HandleMarkdownLiveMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 43 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            !hit.LocalHitId.StartsWith("live_markdown:", StringComparison.Ordinal))
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "live_markdown:search" => next with
                {
                    MarkdownLiveFocusIndex = 1,
                    MarkdownLiveSearchMatchIndex = Math.Clamp(next.MarkdownLiveSearchMatchIndex + delta, 0, 3)
                },
                "live_markdown:editor" => next with
                {
                    MarkdownLiveFocusIndex = 0,
                    MarkdownLiveCursorLine = Math.Clamp(next.MarkdownLiveCursorLine + delta, 0, 20)
                },
                "live_markdown:preview" => next with
                {
                    MarkdownLiveFocusIndex = 2,
                    MarkdownLivePreviewScroll = Math.Clamp(next.MarkdownLivePreviewScroll + delta, 0, 12)
                },
                _ => next
            };
            return hit.LocalHitId is "live_markdown:search" or "live_markdown:editor" or "live_markdown:preview";
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "live_markdown:search" => next with
            {
                MarkdownLiveFocusIndex = 1,
                MarkdownLiveSearchMatchIndex = (next.MarkdownLiveSearchMatchIndex + 1) % 4
            },
            "live_markdown:editor" => next with
            {
                MarkdownLiveFocusIndex = 0,
                MarkdownLiveCursorLine = Math.Clamp(next.MarkdownLiveCursorLine + 1, 0, 20)
            },
            "live_markdown:preview" => next with
            {
                MarkdownLiveFocusIndex = 2,
                MarkdownLiveDiffMode = !next.MarkdownLiveDiffMode
            },
            _ => next
        };
        return hit.LocalHitId is "live_markdown:search" or "live_markdown:editor" or "live_markdown:preview";
    }

    private static bool HandleDragDropMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 44 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            !hit.LocalHitId.StartsWith("drag_drop:", StringComparison.Ordinal))
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            if (!TryParseDragDropItem(hit.LocalHitId, out var list, out _))
            {
                return false;
            }

            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = next with
            {
                DragDropFocusedList = list,
                DragDropSelectedIndex = Math.Clamp(next.DragDropSelectedIndex + delta, 0, 7),
                DragDropContextAction = false
            };
            return true;
        }

        if (hit.LocalHitId.StartsWith("drag_drop:tab:", StringComparison.Ordinal))
        {
            var tabText = hit.LocalHitId["drag_drop:tab:".Length..];
            if (!int.TryParse(tabText, CultureInfo.InvariantCulture, out var tab))
            {
                return false;
            }

            next = next with
            {
                DragDropModeIndex = Math.Clamp(tab, 0, 2),
                DragDropKeyboardActive = tab == 2 && !next.DragDropKeyboardActive,
                DragDropContextAction = false
            };
            return true;
        }

        if (!TryParseDragDropItem(hit.LocalHitId, out var focusedList, out var row))
        {
            return false;
        }

        if (gesture.Button == TerminalMouseButton.Right)
        {
            next = next with
            {
                DragDropFocusedList = focusedList,
                DragDropSelectedIndex = Math.Clamp(row, 0, 7),
                DragDropMoveCount = next.DragDropMoveCount + 1,
                DragDropContextAction = true
            };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = next with
        {
            DragDropFocusedList = focusedList,
            DragDropSelectedIndex = Math.Clamp(row, 0, 7),
            DragDropContextAction = false
        };
        return true;
    }

    private static bool TryParseDragDropItem(string localHitId, out int list, out int row)
    {
        list = 0;
        row = 0;
        const string prefix = "drag_drop:item:";
        if (!localHitId.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var parts = localHitId[prefix.Length..].Split(':', 2);
        return parts.Length == 2 &&
            int.TryParse(parts[0], CultureInfo.InvariantCulture, out list) &&
            int.TryParse(parts[1], CultureInfo.InvariantCulture, out row);
    }

    private static bool HandleQuakeMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 45 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            !hit.LocalHitId.StartsWith("quake:", StringComparison.Ordinal))
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "quake:canvas" => next with
                {
                    QuakeFocusIndex = 0,
                    QuakeQualityIndex = Math.Clamp(next.QuakeQualityIndex + delta, 0, 3)
                },
                "quake:player" => next with
                {
                    QuakeFocusIndex = 1,
                    QuakeYawStep = Math.Clamp(next.QuakeYawStep + delta, -12, 12)
                },
                "quake:renderer" => next with
                {
                    QuakeFocusIndex = 2,
                    QuakePanelScroll = Math.Clamp(next.QuakePanelScroll + delta, 0, 8)
                },
                "quake:controls" => next with
                {
                    QuakeFocusIndex = 3,
                    QuakePanelScroll = Math.Clamp(next.QuakePanelScroll + delta, 0, 8)
                },
                _ => next
            };
            return hit.LocalHitId is "quake:canvas" or "quake:player" or "quake:renderer" or "quake:controls";
        }

        if (gesture.Button == TerminalMouseButton.Right)
        {
            next = next with
            {
                QuakeFocusIndex = QuakeFocusIndexForHit(hit.LocalHitId),
                QuakeResetCount = next.QuakeResetCount + 1,
                QuakeYawStep = 0,
                QuakePitchStep = 0,
                QuakeFireFlash = 0
            };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "quake:canvas" => next with
            {
                QuakeFocusIndex = 0,
                QuakeFireFlash = Math.Clamp(next.QuakeFireFlash + 1, 0, 5)
            },
            "quake:player" => next with
            {
                QuakeFocusIndex = 1,
                QuakePitchStep = Math.Clamp(next.QuakePitchStep + 1, -6, 6)
            },
            "quake:renderer" => next with { QuakeFocusIndex = 2 },
            "quake:controls" => next with
            {
                QuakeFocusIndex = 3,
                QuakeQualityIndex = (next.QuakeQualityIndex + 1) % 4
            },
            _ => next
        };
        return hit.LocalHitId is "quake:canvas" or "quake:player" or "quake:renderer" or "quake:controls";
    }

    private static int QuakeFocusIndexForHit(string localHitId) => localHitId switch
    {
        "quake:canvas" => 0,
        "quake:player" => 1,
        "quake:renderer" => 2,
        "quake:controls" => 3,
        _ => 0
    };

    private static bool HandleTourMouse(MouseTerminalEvent mouseEvent, DateTimeOffset now, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.Session.CommandPalette.IsOpen ||
            next.EvidenceLedgerVisible ||
            next.PerfHudVisible ||
            next.DebugVisible ||
            next.HelpVisible ||
            next.A11yPanelVisible ||
            next.Viewport.Height == 0 ||
            gesture.Row == next.Viewport.Height - 1)
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (next.CurrentScreenNumber == 1 &&
            !next.TourActive &&
            gesture.Kind is TerminalMouseKind.Scroll &&
            hit is { Layer: ShowcaseHitLayer.Overlay, UpstreamHitId: ShowcaseFrameHitRegistry.OverlayTour })
        {
            if (gesture.Button is TerminalMouseButton.WheelDown)
            {
                next = AdjustTourLandingStart(next, 1);
                return true;
            }

            if (gesture.Button is TerminalMouseButton.WheelUp)
            {
                next = AdjustTourLandingStart(next, -1);
                return true;
            }
        }

        if (gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Up) ||
            gesture.Button is not TerminalMouseButton.Left ||
            hit is not { Layer: ShowcaseHitLayer.Overlay, UpstreamHitId: ShowcaseFrameHitRegistry.OverlayTour })
        {
            return false;
        }

        if (next.TourActive)
        {
            next = next with { TourActive = false, TourPaused = false, CurrentScreenNumber = 1, LastTourAdvance = now };
            return true;
        }

        if (next.CurrentScreenNumber == 1)
        {
            next = next.StartTour(now);
            return true;
        }

        return false;
    }

    private static bool HandleOverlayMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (gesture.Kind is not TerminalMouseKind.Down ||
            gesture.Button is not TerminalMouseButton.Left ||
            next.Session.CommandPalette.IsOpen)
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Overlay || hit.UpstreamHitId is not { } rawId)
        {
            return false;
        }

        switch (rawId)
        {
            case ShowcaseFrameHitRegistry.OverlayEvidence:
                next = next with { EvidenceLedgerVisible = false };
                return true;
            case ShowcaseFrameHitRegistry.OverlayPerfHud:
                next = next with { PerfHudVisible = false };
                return true;
            case ShowcaseFrameHitRegistry.OverlayDebug:
                next = next with { DebugVisible = false };
                return true;
            case ShowcaseFrameHitRegistry.OverlayHelpClose:
            case ShowcaseFrameHitRegistry.OverlayHelpContent:
                next = next with { HelpVisible = false };
                return true;
            case ShowcaseFrameHitRegistry.OverlayA11y:
                next = next with { A11yPanelVisible = false };
                return true;
            default:
                return false;
        }
    }

    private static bool HandleStatusMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (gesture.Kind is not TerminalMouseKind.Down ||
            gesture.Button is not TerminalMouseButton.Left ||
            next.Session.CommandPalette.IsOpen ||
            next.EvidenceLedgerVisible ||
            next.PerfHudVisible ||
            next.DebugVisible ||
            next.HelpVisible ||
            next.A11yPanelVisible ||
            next.Viewport.Height == 0 ||
            gesture.Row != next.Viewport.Height - 1)
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.StatusToggle || hit.UpstreamHitId is not { } rawId)
        {
            return false;
        }

        switch (rawId)
        {
            case ShowcaseFrameHitRegistry.StatusHelpToggle:
                next = next with { HelpVisible = !next.HelpVisible };
                return true;
            case ShowcaseFrameHitRegistry.StatusPaletteToggle:
                next = SyncSession(next, next.Session with
                {
                    CommandPalette = CommandPaletteController.Toggle(next.Session.CommandPalette)
                });
                return true;
            case ShowcaseFrameHitRegistry.StatusA11yToggle:
                next = next with { A11yPanelVisible = !next.A11yPanelVisible };
                return true;
            case ShowcaseFrameHitRegistry.StatusPerfToggle:
                next = next with { PerfHudVisible = !next.PerfHudVisible };
                return true;
            case ShowcaseFrameHitRegistry.StatusDebugToggle:
                next = next with { DebugVisible = !next.DebugVisible };
                return true;
            case ShowcaseFrameHitRegistry.StatusMouseToggle:
                next = next with { MouseCaptureEnabled = !next.MouseCaptureEnabled };
                return true;
            default:
                return false;
        }
    }

    private ShowcaseDemoState StartTour(DateTimeOffset now)
    {
        var startIndex = ShowcaseTourStoryboard.FirstIndexForScreen(TourStartScreen);
        var callout = ShowcaseTourStoryboard.At(startIndex);
        return this with
        {
            TourActive = true,
            TourPaused = false,
            TourStepIndex = startIndex,
            CurrentScreenNumber = callout.ScreenNumber,
            LastTourAdvance = now
        };
    }

    private ShowcaseDemoState MoveTourStep(int delta, DateTimeOffset now)
    {
        var nextIndex = Math.Clamp(TourStepIndex + delta, 0, ShowcaseTourStoryboard.Count - 1);
        var callout = ShowcaseTourStoryboard.At(nextIndex);
        return this with
        {
            TourStepIndex = nextIndex,
            CurrentScreenNumber = callout.ScreenNumber,
            LastTourAdvance = now
        };
    }

    private static ShowcaseDemoState AdjustTourLandingStart(ShowcaseDemoState state, int delta) =>
        state with
        {
            TourStartScreen = Math.Clamp(
                state.TourStartScreen + delta,
                2,
                ShowcaseCatalog.Screens.Count)
        };

    private static ShowcaseDemoState AdjustTourSpeed(ShowcaseDemoState state, double multiplier) =>
        state with { TourSpeed = Math.Clamp(state.TourSpeed * multiplier, 0.25, 4.0) };

    private static bool IsCharacter(KeyGesture gesture, char expected)
    {
        if (!gesture.IsCharacter || gesture.Character is not { } rune || gesture.Modifiers != TerminalModifiers.None)
        {
            return false;
        }

        return char.ToLowerInvariant((char)rune.Value) == char.ToLowerInvariant(expected);
    }

    private static bool IsShiftCharacter(KeyGesture gesture, char expected)
    {
        if (!gesture.IsCharacter || gesture.Character is not { } rune || gesture.Modifiers != TerminalModifiers.Shift)
        {
            return false;
        }

        return char.ToLowerInvariant((char)rune.Value) == char.ToLowerInvariant(expected);
    }

    private static bool IsControlCharacter(KeyGesture gesture, char expected)
    {
        if (!gesture.IsCharacter || gesture.Character is not { } rune)
        {
            return false;
        }

        if (!gesture.Modifiers.HasFlag(TerminalModifiers.Control))
        {
            return false;
        }

        return char.ToLowerInvariant((char)rune.Value) == char.ToLowerInvariant(expected);
    }
}

internal abstract record ShowcaseDemoMessage;

internal sealed record ShowcaseInputMessage(RuntimeInputEnvelope Input, RuntimeFrameStats RuntimeStats) : ShowcaseDemoMessage;

internal sealed record ShowcaseTimerMessage(DateTimeOffset Now, RuntimeFrameStats RuntimeStats) : ShowcaseDemoMessage;

internal sealed record ShowcaseA11yTelemetryEntry(
    string Kind,
    int Tick,
    bool HighContrast,
    bool ReducedMotion,
    bool LargeText);

internal sealed record ShowcaseKanbanCard(int Id, string Title, string Tag);

internal sealed record ShowcaseKanbanMove(int CardId, int FromCol, int ToCol, int FromRow, int ToRow);

internal sealed record ShowcaseKanbanState(
    IReadOnlyList<ShowcaseKanbanCard> Todo,
    IReadOnlyList<ShowcaseKanbanCard> InProgress,
    IReadOnlyList<ShowcaseKanbanCard> Done,
    int FocusCol,
    int FocusRow,
    IReadOnlyList<ShowcaseKanbanMove> History,
    IReadOnlyList<ShowcaseKanbanMove> RedoStack,
    int DragSourceCol = -1,
    int DragSourceRow = -1,
    int DragHoverCol = -1)
{
    public static ShowcaseKanbanState CreateDefault() =>
        new(
            [
                new(1, "Design login page", "UI"),
                new(2, "Add input validation", "Logic"),
                new(3, "Write unit tests", "QA"),
                new(4, "Set up CI pipeline", "Ops")
            ],
            [
                new(5, "Build nav component", "UI"),
                new(6, "Implement auth flow", "Logic")
            ],
            [new(7, "Project scaffolding", "Ops")],
            FocusCol: 0,
            FocusRow: 0,
            History: [],
            RedoStack: []);

    public IReadOnlyList<ShowcaseKanbanCard> Column(int col) =>
        col switch
        {
            0 => Todo,
            1 => InProgress,
            _ => Done
        };

    public bool CanUndo => History.Count > 0;

    public bool CanRedo => RedoStack.Count > 0;

    public bool IsDragging => DragSourceCol >= 0 && DragSourceRow >= 0;

    public ShowcaseKanbanState FocusLeft() =>
        FocusCol == 0 ? this : WithFocus(FocusCol - 1, FocusRow);

    public ShowcaseKanbanState FocusRight() =>
        FocusCol >= 2 ? this : WithFocus(FocusCol + 1, FocusRow);

    public ShowcaseKanbanState FocusUp() =>
        FocusRow == 0 ? this : this with { FocusRow = FocusRow - 1 };

    public ShowcaseKanbanState FocusDown()
    {
        var len = Column(FocusCol).Count;
        return len == 0 || FocusRow >= len - 1 ? this : this with { FocusRow = FocusRow + 1 };
    }

    public ShowcaseKanbanState MoveCardLeft() =>
        FocusCol == 0 ? this : MoveCard(FocusCol, FocusRow, FocusCol - 1);

    public ShowcaseKanbanState MoveCardRight() =>
        FocusCol >= 2 ? this : MoveCard(FocusCol, FocusRow, FocusCol + 1);

    public ShowcaseKanbanState Undo()
    {
        if (History.Count == 0)
        {
            return this;
        }

        var move = History[^1];
        var columns = CloneColumns();
        var currentRow = columns[move.ToCol].FindIndex(card => card.Id == move.CardId);
        if (currentRow < 0)
        {
            return this;
        }

        var card = columns[move.ToCol][currentRow];
        columns[move.ToCol].RemoveAt(currentRow);
        var insertAt = Math.Min(move.FromRow, columns[move.FromCol].Count);
        columns[move.FromCol].Insert(insertAt, card);

        var history = History.Take(History.Count - 1).ToArray();
        var redo = RedoStack.Concat([move with { FromRow = insertAt, ToRow = currentRow }]).ToArray();
        return FromColumns(columns, move.FromCol, insertAt, history, redo);
    }

    public ShowcaseKanbanState Redo()
    {
        if (RedoStack.Count == 0)
        {
            return this;
        }

        var move = RedoStack[^1];
        var columns = CloneColumns();
        var currentRow = columns[move.FromCol].FindIndex(card => card.Id == move.CardId);
        if (currentRow < 0)
        {
            return this;
        }

        var card = columns[move.FromCol][currentRow];
        columns[move.FromCol].RemoveAt(currentRow);
        var insertAt = columns[move.ToCol].Count;
        columns[move.ToCol].Add(card);

        var history = History.Concat([move with { FromRow = currentRow, ToRow = insertAt }]).ToArray();
        var redo = RedoStack.Take(RedoStack.Count - 1).ToArray();
        return FromColumns(columns, move.ToCol, insertAt, history, redo);
    }

    public ShowcaseKanbanState FocusAt(int col, int row) =>
        col is < 0 or > 2 ? this : WithFocus(col, row).ClearDrag();

    public ShowcaseKanbanState FocusColumn(int col) =>
        col is < 0 or > 2 ? this : WithFocus(col, FocusRow).ClearDrag();

    public ShowcaseKanbanState MoveFocusedToColumn(int toCol) =>
        toCol is < 0 or > 2 ? this : MoveCard(FocusCol, FocusRow, toCol);

    public ShowcaseKanbanState StartDrag(int col, int row) =>
        col is < 0 or > 2 || row < 0 || row >= Column(col).Count
            ? ClearDrag()
            : WithFocus(col, row) with
            {
                DragSourceCol = col,
                DragSourceRow = row,
                DragHoverCol = col
            };

    public ShowcaseKanbanState DragOverColumn(int col)
    {
        if (!IsDragging || col is < 0 or > 2)
        {
            return this;
        }

        return WithFocus(col, FocusRow) with { DragHoverCol = col };
    }

    public ShowcaseKanbanState DropOnColumn(int col)
    {
        if (!IsDragging)
        {
            return MoveFocusedToColumn(col).ClearDrag();
        }

        var moved = col is < 0 or > 2 ? this : MoveCard(DragSourceCol, DragSourceRow, col);
        return moved.ClearDrag();
    }

    public ShowcaseKanbanState ClearDrag() =>
        IsDragging || DragHoverCol >= 0
            ? this with { DragSourceCol = -1, DragSourceRow = -1, DragHoverCol = -1 }
            : this;

    private ShowcaseKanbanState MoveCard(int fromCol, int fromRow, int toCol)
    {
        var columns = CloneColumns();
        if (fromCol == toCol || fromCol is < 0 or > 2 || toCol is < 0 or > 2 || fromRow < 0 || fromRow >= columns[fromCol].Count)
        {
            return this;
        }

        var card = columns[fromCol][fromRow];
        columns[fromCol].RemoveAt(fromRow);
        var toRow = columns[toCol].Count;
        columns[toCol].Add(card);

        var history = History.Concat([new ShowcaseKanbanMove(card.Id, fromCol, toCol, fromRow, toRow)]).ToArray();
        return FromColumns(columns, toCol, toRow, history, []);
    }

    private ShowcaseKanbanState WithFocus(int col, int row)
    {
        var len = Column(col).Count;
        var clamped = len == 0 ? 0 : Math.Clamp(row, 0, len - 1);
        return this with { FocusCol = col, FocusRow = clamped };
    }

    private List<ShowcaseKanbanCard>[] CloneColumns() =>
        [Todo.ToList(), InProgress.ToList(), Done.ToList()];

    private static ShowcaseKanbanState FromColumns(
        List<ShowcaseKanbanCard>[] columns,
        int focusCol,
        int focusRow,
        IReadOnlyList<ShowcaseKanbanMove> history,
        IReadOnlyList<ShowcaseKanbanMove> redo)
    {
        var len = columns[focusCol].Count;
        var clampedRow = len == 0 ? 0 : Math.Clamp(focusRow, 0, len - 1);
        return new ShowcaseKanbanState(
            columns[0].ToArray(),
            columns[1].ToArray(),
            columns[2].ToArray(),
            focusCol,
            clampedRow,
            history,
            redo);
    }
}

internal enum ShowcasePaletteLabMatchFilter
{
    All,
    Exact,
    Prefix,
    WordStart,
    Substring,
    Fuzzy
}

internal static class ShowcasePaletteLabMatchFilterExtensions
{
    public static ShowcasePaletteLabMatchFilter Next(this ShowcasePaletteLabMatchFilter filter) =>
        filter switch
        {
            ShowcasePaletteLabMatchFilter.All => ShowcasePaletteLabMatchFilter.Exact,
            ShowcasePaletteLabMatchFilter.Exact => ShowcasePaletteLabMatchFilter.Prefix,
            ShowcasePaletteLabMatchFilter.Prefix => ShowcasePaletteLabMatchFilter.WordStart,
            ShowcasePaletteLabMatchFilter.WordStart => ShowcasePaletteLabMatchFilter.Substring,
            ShowcasePaletteLabMatchFilter.Substring => ShowcasePaletteLabMatchFilter.Fuzzy,
            _ => ShowcasePaletteLabMatchFilter.All
        };
}
