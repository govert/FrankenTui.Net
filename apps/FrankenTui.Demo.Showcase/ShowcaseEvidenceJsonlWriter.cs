using System.Globalization;
using System.Text.Json;
using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Runtime;

namespace FrankenTui.Demo.Showcase;

public sealed class ShowcaseEvidenceJsonlWriter : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly StreamWriter _writer;
    private long _sequence;
    private string? _hoverHitKey;
    private TerminalMouseButton? _suppressNextUpButton;

    private ShowcaseEvidenceJsonlWriter(string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read));
    }

    public static ShowcaseEvidenceJsonlWriter? Create(string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : new ShowcaseEvidenceJsonlWriter(path);

    public void WriteLaunch(ShowcaseCliOptions options, ShowcasePaneWorkspaceLoadResult? paneWorkspaceLoad = null)
    {
        Write(
            "launch",
            options,
            RuntimeFrameStats.Empty,
            stepIndex: 0,
            frame: 0,
            extra: new Dictionary<string, object?>
            {
                ["launch_screen_mode"] = options.ScreenMode.ToString(),
                ["mouse_mode"] = options.MouseMode.ToString(),
                ["tick_interval_ms"] = options.TickIntervalMilliseconds,
                ["exit_after_ms"] = options.ExitAfterMilliseconds,
                ["exit_after_ticks"] = options.ExitAfterTicks,
                ["deterministic"] = options.Deterministic,
                ["deterministic_seed"] = options.DeterministicSeed?.ToString(CultureInfo.InvariantCulture),
                ["pane_workspace"] = options.PaneWorkspacePath,
                ["pane_workspace_loaded"] = paneWorkspaceLoad?.Loaded,
                ["pane_workspace_error"] = paneWorkspaceLoad?.Error,
                ["pane_workspace_invalid_snapshot"] = paneWorkspaceLoad?.InvalidSnapshotPath,
                ["pane_workspace_snapshot_hash"] = paneWorkspaceLoad?.Workspace.SnapshotHash(),
                ["pane_workspace_schema_version"] = paneWorkspaceLoad?.SchemaVersion,
                ["pane_workspace_migration_applied"] = paneWorkspaceLoad?.MigrationApplied,
                ["pane_workspace_migration_from_version"] = paneWorkspaceLoad?.MigrationFromVersion,
                ["vfx_harness"] = options.VfxHarness.Enabled,
                ["vfx_effect"] = options.VfxHarness.Effect,
                ["vfx_frames"] = options.VfxHarness.Frames,
                ["vfx_jsonl"] = options.VfxHarness.JsonlPath,
                ["vfx_run_id"] = options.VfxHarness.RunId,
                ["mermaid_harness"] = options.MermaidHarness.Enabled,
                ["mermaid_jsonl"] = options.MermaidHarness.JsonlPath,
                ["mermaid_run_id"] = options.MermaidHarness.RunId
            });
    }

    public void WriteScreenInit(
        ShowcaseCliOptions options,
        ulong initMilliseconds = 0,
        int? effectCount = null,
        ulong? memoryEstimateBytes = null)
    {
        var screen = ShowcaseCatalog.Get(options.ScreenNumber);
        Write(
            "screen_init",
            options,
            RuntimeFrameStats.Empty,
            stepIndex: 0,
            frame: 0,
            extra: new Dictionary<string, object?>
            {
                ["diagnostics_stream"] = "demo_screen_init",
                ["source"] = "generic_evidence",
                ["screen_id"] = screen.Id,
                ["screen_category"] = ScreenCategoryLabel(screen.Category),
                ["init_ms"] = initMilliseconds,
                ["effect_count"] = effectCount ?? ResolveScreenInitEffectCount(screen),
                ["memory_estimate_bytes"] = memoryEstimateBytes?.ToString(CultureInfo.InvariantCulture) ?? "unknown"
            });
    }

    public void WriteFrame(
        string eventName,
        ShowcaseCliOptions options,
        RuntimeFrameStats stats,
        int stepIndex,
        int frame)
    {
        Write(eventName, options, stats, stepIndex, frame, extra: null);
    }

    public void WritePaneWorkspaceSaveEvent(
        ShowcaseCliOptions options,
        RuntimeFrameStats stats,
        int stepIndex,
        int frame,
        ShowcasePaneWorkspaceSaveResult saveResult)
    {
        ArgumentNullException.ThrowIfNull(saveResult);

        Write(
            "pane_workspace_save",
            options,
            stats,
            stepIndex,
            frame,
            new Dictionary<string, object?>
            {
                ["pane_workspace"] = saveResult.Path,
                ["pane_workspace_saved"] = saveResult.Saved,
                ["pane_workspace_save_error"] = saveResult.Error,
                ["pane_workspace_snapshot_hash"] = saveResult.SnapshotHash,
                ["pane_workspace_schema_version"] = saveResult.SchemaVersion
            });
    }

    internal void WriteFrame(
        string eventName,
        ShowcaseCliOptions options,
        RuntimeFrameStats stats,
        int stepIndex,
        int frame,
        ShowcaseDemoState state)
    {
        Write(eventName, options, stats, stepIndex, frame, BuildStateFields(state));
    }

    internal void WriteTourEvent(
        string trigger,
        ShowcaseCliOptions options,
        RuntimeFrameStats stats,
        int stepIndex,
        int frame,
        ShowcaseDemoState before,
        ShowcaseDemoState after)
    {
        if (!TourChanged(before, after))
        {
            return;
        }

        var fields = BuildStateFields(after) as Dictionary<string, object?> ?? [];
        fields["tour_trigger"] = trigger;
        fields["tour_action"] = ClassifyTourAction(before, after);
        fields["tour_from_screen_number"] = before.CurrentScreenNumber;
        fields["tour_from_screen_slug"] = before.CurrentScreen.Slug;
        fields["tour_to_screen_number"] = after.CurrentScreenNumber;
        fields["tour_to_screen_slug"] = after.CurrentScreen.Slug;
        fields["tour_was_active"] = before.TourActive;
        fields["tour_was_paused"] = before.TourPaused;
        fields["tour_previous_speed"] = before.TourSpeed.ToString("0.##", CultureInfo.InvariantCulture);

        Write("tour_event", options, stats, stepIndex, frame, fields);
    }

    internal void WritePaletteEvent(
        string trigger,
        ShowcaseCliOptions options,
        RuntimeFrameStats stats,
        int stepIndex,
        int frame,
        ShowcaseDemoState before,
        ShowcaseDemoState after)
    {
        var beforePalette = before.Session.CommandPalette;
        var afterPalette = after.Session.CommandPalette;
        if (!PaletteChanged(beforePalette, afterPalette))
        {
            return;
        }

        var fields = BuildStateFields(after) as Dictionary<string, object?> ?? [];
        fields["palette_trigger"] = trigger;
        fields["palette_action"] = ClassifyPaletteAction(beforePalette, afterPalette);
        fields["palette_was_open"] = beforePalette.IsOpen;
        fields["palette_query_before"] = beforePalette.Query;
        fields["palette_query_after"] = afterPalette.Query;
        fields["palette_selected_index_before"] = beforePalette.SelectedIndex;
        fields["palette_selected_index_after"] = afterPalette.SelectedIndex;
        fields["palette_favorites_only"] = afterPalette.FavoritesOnly;
        fields["palette_favorite_count"] = afterPalette.FavoriteEntryIds?.Count ?? 0;
        fields["palette_favorite_ids"] = afterPalette.FavoriteEntryIds is null
            ? null
            : string.Join(",", afterPalette.FavoriteEntryIds);
        fields["palette_category_filter"] = afterPalette.CategoryFilter?.ToString();
        fields["palette_last_executed_command"] = afterPalette.LastExecutedCommandId;
        if (afterPalette.IsOpen && !string.IsNullOrWhiteSpace(afterPalette.Query))
        {
            var results = CommandPaletteController.Results(afterPalette, ShowcaseCommandPalette.Entries());
            var selectedIndex = results.Count == 0
                ? -1
                : Math.Clamp(afterPalette.SelectedIndex, 0, results.Count - 1);
            if (selectedIndex >= 0)
            {
                var selected = results[selectedIndex];
                fields["palette_top_command_id"] = selected.Entry.Id;
                fields["palette_top_score"] = selected.Score.ToString("0.000", CultureInfo.InvariantCulture);
                fields["palette_top_match_kind"] = selected.MatchKind.ToString();
                fields["palette_top_match_positions"] = string.Join(",", selected.MatchPositions);
                fields["palette_top_evidence"] = string.Join(
                    "|",
                    selected.Evidence.Select(static entry =>
                        $"{entry.Kind}:{entry.Factor.ToString("0.###", CultureInfo.InvariantCulture)}:{entry.Description}"));
            }
        }

        Write("palette_event", options, stats, stepIndex, frame, fields);
    }

    internal void WriteMouseEvent(
        string trigger,
        ShowcaseCliOptions options,
        RuntimeFrameStats stats,
        int stepIndex,
        int frame,
        TerminalEvent terminalEvent,
        ShowcaseDemoState before,
        ShowcaseDemoState after)
    {
        if (terminalEvent is not MouseTerminalEvent mouseEvent)
        {
            return;
        }

        var gesture = mouseEvent.Gesture;
        var hitBefore = ShowcaseFrameHitRegistry.HitTest(before, gesture.Column, gesture.Row);
        var hoverChanged = false;
        if (gesture.Kind is TerminalMouseKind.Move or TerminalMouseKind.Drag)
        {
            var nextHoverHitKey = HoverHitKey(hitBefore);
            if (_hoverHitKey != nextHoverHitKey)
            {
                hoverChanged = true;
                _hoverHitKey = nextHoverHitKey;
                WriteMouseEventRecord(
                    "hover_change",
                    trigger,
                    options,
                    stats,
                    stepIndex,
                    frame,
                    gesture,
                    before,
                    after,
                    hitBefore);
            }
        }

        if (gesture.Kind == TerminalMouseKind.Move && hoverChanged && IsChromeTarget(hitBefore))
        {
            return;
        }

        var suppressUp = !before.Session.CommandPalette.IsOpen &&
            gesture.Kind == TerminalMouseKind.Up &&
            _suppressNextUpButton == gesture.Button;
        if (suppressUp)
        {
            _suppressNextUpButton = null;
        }

        if (!before.Session.CommandPalette.IsOpen &&
            gesture.Kind == TerminalMouseKind.Down &&
            gesture.Button == TerminalMouseButton.Left &&
            IsChromeOrDashboardLinkTarget(before, hitBefore))
        {
            _suppressNextUpButton = gesture.Button;
            WriteMouseEventRecord(
                "down_click_fallback",
                trigger,
                options,
                stats,
                stepIndex,
                frame,
                gesture,
                before,
                after,
                ShowcaseFrameHitRegistry.Resolve(gesture, before, before));
        }

        var action = suppressUp
            ? "up_suppressed_after_down_click"
            : ClassifyMouseAction(gesture, before, after);
        var hit = ShowcaseFrameHitRegistry.Resolve(gesture, before, after);
        WriteMouseEventRecord(action, trigger, options, stats, stepIndex, frame, gesture, before, after, hit);
    }

    private void WriteMouseEventRecord(
        string action,
        string trigger,
        ShowcaseCliOptions options,
        RuntimeFrameStats stats,
        int stepIndex,
        int frame,
        MouseGesture gesture,
        ShowcaseDemoState before,
        ShowcaseDemoState after,
        ShowcaseHitTestResult hit)
    {
        var targetChanged = before.CurrentScreenNumber != after.CurrentScreenNumber;
        var fields = BuildStateFields(after) as Dictionary<string, object?> ?? [];
        fields["mouse_trigger"] = trigger;
        fields["mouse_kind"] = MouseKindLabel(gesture);
        fields["mouse_button"] = gesture.Button.ToString();
        fields["mouse_column"] = gesture.Column;
        fields["mouse_row"] = gesture.Row;
        fields["mouse_action"] = action;
        fields["mouse_current_screen_number"] = before.CurrentScreenNumber;
        fields["mouse_current_screen_slug"] = before.CurrentScreen.Slug;
        fields["mouse_target_screen_number"] = targetChanged
            ? after.CurrentScreenNumber
            : null;
        fields["mouse_target_screen_slug"] = targetChanged
            ? after.CurrentScreen.Slug
            : null;
        fields["kind"] = MouseKindLabel(gesture);
        fields["x"] = gesture.Column;
        fields["y"] = gesture.Row;
        fields["hit_id"] = hit.LocalHitId;
        fields["hit_raw_id"] = hit.UpstreamHitId;
        fields["hit_layer"] = hit.Layer.ToString().ToLowerInvariant();
        fields["hit_target_screen_number"] = hit.TargetScreenNumber;
        fields["hit_target_category"] = hit.TargetCategory?.ToString();
        fields["target_id"] = hit.Layer == ShowcaseHitLayer.Content ? hit.UpstreamHitId : null;
        fields["link_id"] = hit.Layer == ShowcaseHitLayer.Link && hit.UpstreamHitId is { } linkRawId
            ? linkRawId - ShowcaseFrameHitRegistry.LinkHitBase + 1
            : null;
        fields["action"] = action;
        fields["target_screen"] = targetChanged ? after.CurrentScreen.Title : "none";
        fields["current_screen"] = before.CurrentScreen.Title;

        Write("mouse_event", options, stats, stepIndex, frame, fields);
    }

    internal void WriteMouseCaptureToggleEvent(
        string trigger,
        ShowcaseCliOptions options,
        RuntimeFrameStats stats,
        int stepIndex,
        int frame,
        TerminalEvent? terminalEvent,
        ShowcaseDemoState before,
        ShowcaseDemoState after)
    {
        if (before.MouseCaptureEnabled == after.MouseCaptureEnabled)
        {
            return;
        }

        var fields = BuildStateFields(after) as Dictionary<string, object?> ?? [];
        fields["mouse_capture_trigger"] = trigger;
        fields["state"] = after.MouseCaptureEnabled ? "on" : "off";
        fields["mode"] = after.InlineMode ? "inline" : "alt";
        fields["source"] = MouseCaptureSourceLabel(trigger, terminalEvent);
        fields["current_screen"] = after.CurrentScreen.Title;
        fields["mouse_capture_previous_state"] = before.MouseCaptureEnabled ? "on" : "off";

        Write("mouse_capture_toggle", options, stats, stepIndex, frame, fields);
    }

    internal void WriteA11yEvent(
        string trigger,
        ShowcaseCliOptions options,
        RuntimeFrameStats stats,
        int stepIndex,
        int frame,
        ShowcaseDemoState before,
        ShowcaseDemoState after)
    {
        foreach (var action in ClassifyA11yActions(before, after))
        {
            var fields = BuildStateFields(after) as Dictionary<string, object?> ?? [];
            fields["diagnostics_stream"] = "a11y";
            fields["a11y_trigger"] = trigger;
            fields["a11y_event"] = action;
            fields["tick"] = frame;
            fields["screen"] = after.CurrentScreen.Title;
            fields["panel_visible"] = BoolString(after.A11yPanelVisible);
            fields["high_contrast"] = BoolString(after.A11yHighContrast);
            fields["reduced_motion"] = BoolString(after.A11yReducedMotion);
            fields["large_text"] = BoolString(after.A11yLargeText);
            fields["panel_visible_previous"] = BoolString(before.A11yPanelVisible);
            fields["high_contrast_previous"] = BoolString(before.A11yHighContrast);
            fields["reduced_motion_previous"] = BoolString(before.A11yReducedMotion);
            fields["large_text_previous"] = BoolString(before.A11yLargeText);

            Write("a11y_event", options, stats, stepIndex, frame, fields);
        }
    }

    internal void WritePerfHudEvent(
        string trigger,
        ShowcaseCliOptions options,
        RuntimeFrameStats stats,
        int stepIndex,
        int frame,
        ShowcaseDemoState before,
        ShowcaseDemoState after)
    {
        if (before.PerfHudVisible == after.PerfHudVisible)
        {
            return;
        }

        var fields = BuildStateFields(after) as Dictionary<string, object?> ?? [];
        fields["diagnostics_stream"] = "perf_hud";
        fields["perf_hud_trigger"] = trigger;
        fields["perf_hud_event"] = "hud_toggle";
        fields["state"] = after.PerfHudVisible ? "on" : "off";
        fields["tick"] = frame;
        fields["screen"] = after.CurrentScreen.Title;
        fields["previous_state"] = before.PerfHudVisible ? "on" : "off";

        Write("perf_hud_event", options, stats, stepIndex, frame, fields);
    }

    internal void WritePerfHudStatsEvent(
        ShowcaseCliOptions options,
        RuntimeFrameStats stats,
        int stepIndex,
        int frame,
        ShowcaseDemoState state)
    {
        if (!state.PerfHudVisible || frame <= 0 || frame % 60 != 0)
        {
            return;
        }

        var frameMs = Math.Max(stats.FrameDurationMs, 0);
        var estimatedFps = frameMs <= double.Epsilon ? 0 : 1000d / frameMs;
        var fields = BuildStateFields(state) as Dictionary<string, object?> ?? [];
        fields["diagnostics_stream"] = "perf_hud";
        fields["perf_hud_event"] = "tick_stats";
        fields["tick"] = frame;
        fields["screen"] = state.CurrentScreen.Title;
        fields["fps"] = estimatedFps.ToString("0.###", CultureInfo.InvariantCulture);
        fields["tps"] = estimatedFps.ToString("0.###", CultureInfo.InvariantCulture);
        fields["avg_ms"] = frameMs.ToString("0.###", CultureInfo.InvariantCulture);
        fields["p95_ms"] = frameMs.ToString("0.###", CultureInfo.InvariantCulture);
        fields["p99_ms"] = frameMs.ToString("0.###", CultureInfo.InvariantCulture);
        fields["min_ms"] = frameMs.ToString("0.###", CultureInfo.InvariantCulture);
        fields["max_ms"] = frameMs.ToString("0.###", CultureInfo.InvariantCulture);
        fields["samples"] = Math.Max(stats.LoadGovernorFramesObserved, 1u);
        fields["present_ms"] = stats.PresentDurationMs.ToString("0.###", CultureInfo.InvariantCulture);
        fields["diff_ms"] = stats.DiffDurationMs.ToString("0.###", CultureInfo.InvariantCulture);
        fields["changed_cells"] = stats.ChangedCells;
        fields["dirty_rows"] = stats.DirtyRows;

        Write("perf_hud_event", options, stats, stepIndex, frame, fields);
    }

    internal void WritePerfHudStallEvent(
        ShowcaseCliOptions options,
        RuntimeFrameStats stats,
        int stepIndex,
        int frame,
        ShowcaseDemoState state,
        long sinceMilliseconds)
    {
        if (!state.PerfHudVisible || sinceMilliseconds < 0)
        {
            return;
        }

        var fields = BuildStateFields(state) as Dictionary<string, object?> ?? [];
        fields["diagnostics_stream"] = "perf_hud";
        fields["perf_hud_event"] = "tick_stall";
        fields["since_ms"] = sinceMilliseconds;
        fields["tick"] = frame;
        fields["screen"] = state.CurrentScreen.Title;
        fields["reduced_motion"] = BoolString(state.A11yReducedMotion);

        Write("perf_hud_event", options, stats, stepIndex, frame, fields);
    }

    private static IReadOnlyDictionary<string, object?>? BuildStateFields(ShowcaseDemoState? state)
    {
        if (state is null)
        {
            return null;
        }

        var callout = state.TourCallout;
        var highlightRect = callout is null
            ? null
            : ShowcaseTourStoryboard.FormatRect(ShowcaseTourStoryboard.ResolveHighlight(callout, state.Viewport));
        var hintLedger = ShowcaseSurface.BuildPaletteLabHintLedger();
        var topHint = hintLedger.Count == 0 ? null : hintLedger[0];
        return new Dictionary<string, object?>
        {
            ["state_screen_number"] = state.CurrentScreenNumber,
            ["state_screen_slug"] = state.CurrentScreen.Slug,
            ["tour_active"] = state.TourActive,
            ["tour_paused"] = state.TourPaused,
            ["tour_speed"] = state.TourSpeed.ToString("0.##", CultureInfo.InvariantCulture),
            ["tour_start_screen"] = state.TourStartScreen,
            ["tour_step_index"] = state.TourStepIndex,
            ["tour_step_count"] = ShowcaseTourStoryboard.Count,
            ["tour_callout_id"] = callout?.StepId,
            ["tour_callout_title"] = callout?.Title,
            ["tour_callout_body"] = callout?.Body,
            ["tour_callout_hint"] = callout?.Hint,
            ["tour_highlight"] = callout?.Highlight,
            ["tour_highlight_rect"] = highlightRect,
            ["evidence_ledger_visible"] = state.EvidenceLedgerVisible,
            ["perf_hud_visible"] = state.PerfHudVisible,
            ["debug_visible"] = state.DebugVisible,
            ["help_visible"] = state.HelpVisible,
            ["a11y_panel_visible"] = state.A11yPanelVisible,
            ["a11y_high_contrast"] = state.A11yHighContrast,
            ["a11y_reduced_motion"] = state.A11yReducedMotion,
            ["a11y_large_text"] = state.A11yLargeText,
            ["mouse_capture_enabled"] = state.MouseCaptureEnabled,
            ["palette_lab_match_filter"] = state.PaletteLabMatchFilter.ToString(),
            ["palette_lab_bench_enabled"] = state.PaletteLabBenchEnabled,
            ["palette_lab_bench_frame"] = state.PaletteLabBenchFrame,
            ["palette_lab_bench_processed"] = state.PaletteLabBenchProcessed,
            ["palette_lab_bench_step_ticks"] = ShowcaseSurface.PaletteLabBenchStepTicks,
            ["palette_lab_bench_query"] = ShowcaseSurface.ResolvePaletteLabQuery(state, state.Session.CommandPalette),
            ["palette_lab_hint_top"] = topHint?.Label,
            ["palette_lab_hint_top_expected_utility"] = topHint?.ExpectedUtility.ToString("0.000", CultureInfo.InvariantCulture),
            ["palette_lab_hint_top_net_value"] = topHint?.NetValue.ToString("0.000", CultureInfo.InvariantCulture),
            ["palette_lab_hint_top_voi"] = topHint?.ValueOfInformation.ToString("0.000", CultureInfo.InvariantCulture),
            ["palette_lab_hint_ledger"] = string.Join(
                "|",
                hintLedger.Take(3).Select(static entry =>
                    $"{entry.Rank}:{entry.Label}:{entry.ExpectedUtility.ToString("0.###", CultureInfo.InvariantCulture)}:{entry.NetValue.ToString("0.###", CultureInfo.InvariantCulture)}:{entry.ValueOfInformation.ToString("0.###", CultureInfo.InvariantCulture)}")),
            ["palette_open"] = state.Session.CommandPalette.IsOpen,
            ["pane_workspace_loaded"] = state.PaneWorkspaceLoaded,
            ["pane_workspace_recovery_error"] = state.PaneWorkspaceRecoveryError,
            ["pane_workspace_invalid_snapshot"] = state.PaneWorkspaceInvalidSnapshotPath,
            ["pane_workspace_snapshot_hash"] = state.Session.PaneWorkspace.SnapshotHash()
        };
    }

    private static bool TourChanged(ShowcaseDemoState before, ShowcaseDemoState after) =>
        before.TourActive != after.TourActive ||
        before.TourPaused != after.TourPaused ||
        before.CurrentScreenNumber != after.CurrentScreenNumber ||
        before.TourStepIndex != after.TourStepIndex ||
        Math.Abs(before.TourSpeed - after.TourSpeed) > double.Epsilon ||
        before.TourStartScreen != after.TourStartScreen;

    private static string ClassifyTourAction(ShowcaseDemoState before, ShowcaseDemoState after)
    {
        if (!before.TourActive && after.TourActive)
        {
            return "start";
        }

        if (before.TourActive && !after.TourActive)
        {
            return "exit";
        }

        if (before.TourPaused != after.TourPaused)
        {
            return after.TourPaused ? "pause" : "resume";
        }

        if (Math.Abs(before.TourSpeed - after.TourSpeed) > double.Epsilon)
        {
            return after.TourSpeed > before.TourSpeed ? "speed_up" : "speed_down";
        }

        if (after.TourStepIndex > before.TourStepIndex)
        {
            return "next";
        }

        if (after.TourStepIndex < before.TourStepIndex)
        {
            return "previous";
        }

        if (before.TourStartScreen != after.TourStartScreen)
        {
            return "landing_adjust";
        }

        return "changed";
    }

    private static bool PaletteChanged(CommandPaletteState before, CommandPaletteState after) =>
        before.IsOpen != after.IsOpen ||
        before.Query != after.Query ||
        before.SelectedIndex != after.SelectedIndex ||
        before.PreviewFocused != after.PreviewFocused ||
        before.Status != after.Status ||
        before.LastExecutedCommandId != after.LastExecutedCommandId ||
        before.FavoritesOnly != after.FavoritesOnly ||
        before.CategoryFilter != after.CategoryFilter ||
        !FavoriteIdsEqual(before, after);

    private static string ClassifyPaletteAction(CommandPaletteState before, CommandPaletteState after)
    {
        if (!before.IsOpen && after.IsOpen)
        {
            return "open";
        }

        if (before.IsOpen && !after.IsOpen)
        {
            return after.LastExecutedCommandId is null ? "close" : "execute";
        }

        if (before.FavoritesOnly != after.FavoritesOnly)
        {
            return after.FavoritesOnly ? "favorites_only_on" : "favorites_only_off";
        }

        if (!FavoriteIdsEqual(before, after))
        {
            return (after.FavoriteEntryIds?.Count ?? 0) > (before.FavoriteEntryIds?.Count ?? 0)
                ? "favorite_add"
                : "favorite_remove";
        }

        if (before.CategoryFilter != after.CategoryFilter)
        {
            return after.CategoryFilter is null ? "category_filter_clear" : "category_filter_set";
        }

        if (before.Query != after.Query)
        {
            return string.IsNullOrEmpty(after.Query) ? "query_clear" : "query_change";
        }

        if (before.SelectedIndex != after.SelectedIndex)
        {
            return "selection_change";
        }

        if (before.PreviewFocused != after.PreviewFocused)
        {
            return "preview_focus";
        }

        return "changed";
    }

    private static string ClassifyMouseAction(
        MouseGesture gesture,
        ShowcaseDemoState before,
        ShowcaseDemoState after)
    {
        if (before.Session.CommandPalette.IsOpen)
        {
            return gesture.Kind == TerminalMouseKind.Scroll ? "palette_scroll" : "palette_mouse";
        }

        if (before.CurrentScreenNumber == 39 &&
            TryResolvePaletteLabHit(gesture, before))
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return gesture.Button == TerminalMouseButton.WheelUp
                    ? "palette_lab_scroll_up"
                    : "palette_lab_scroll_down";
            }

            if (before.Session.CommandPalette.LastExecutedCommandId != after.Session.CommandPalette.LastExecutedCommandId)
            {
                return "palette_lab_execute";
            }

            return "palette_lab_mouse";
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(before, gesture.Column, gesture.Row);
        if (IsNonLeftChromeOrPaneClickTarget(gesture, before, hit))
        {
            return gesture.Kind == TerminalMouseKind.Up
                ? "click_non_left"
                : "down_non_left_click_target";
        }

        if (gesture.Kind == TerminalMouseKind.Drag)
        {
            return "drag_forward";
        }

        if (hit.Layer == ShowcaseHitLayer.Overlay)
        {
            if (before.CurrentScreenNumber == 1 &&
                !before.TourActive &&
                before.TourStartScreen != after.TourStartScreen &&
                hit.UpstreamHitId == ShowcaseFrameHitRegistry.OverlayTour)
            {
                return after.TourStartScreen > before.TourStartScreen
                    ? "tour_landing_step_next"
                    : "tour_landing_step_prev";
            }

            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return "scroll_forward";
            }

            return hit.UpstreamHitId switch
            {
                ShowcaseFrameHitRegistry.OverlayHelpClose or ShowcaseFrameHitRegistry.OverlayHelpContent
                    when before.HelpVisible != after.HelpVisible => "overlay_help_close",
                ShowcaseFrameHitRegistry.OverlayTour when !before.TourActive && after.TourActive => "overlay_tour_start",
                ShowcaseFrameHitRegistry.OverlayTour when before.TourActive && !after.TourActive => "overlay_tour_click",
                ShowcaseFrameHitRegistry.OverlayTour => "overlay_tour_click",
                ShowcaseFrameHitRegistry.OverlayA11y when before.A11yPanelVisible != after.A11yPanelVisible => "overlay_a11y_close",
                ShowcaseFrameHitRegistry.OverlayPerfHud when before.PerfHudVisible != after.PerfHudVisible => "overlay_perf_close",
                ShowcaseFrameHitRegistry.OverlayEvidence when before.EvidenceLedgerVisible != after.EvidenceLedgerVisible => "overlay_evidence_close",
                ShowcaseFrameHitRegistry.OverlayDebug when before.DebugVisible != after.DebugVisible => "overlay_debug_close",
                _ => "overlay_unknown"
            };
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 43)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll && hit.LocalHitId == "live_markdown:preview")
            {
                return gesture.Button == TerminalMouseButton.WheelUp
                    ? "live_markdown_preview_scroll_up"
                    : "live_markdown_preview_scroll_down";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "live_markdown:search" => "live_markdown_focus_search",
                    "live_markdown:editor" => "live_markdown_focus_editor",
                    "live_markdown:preview" => "live_markdown_focus_preview",
                    _ => "live_markdown_hit_test"
                };
            }

            return "live_markdown_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 44)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return gesture.Button == TerminalMouseButton.WheelUp
                    ? "drag_drop_scroll_up"
                    : "drag_drop_scroll_down";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Right)
            {
                return "drag_drop_context_action";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId.StartsWith("drag_drop:tab:", StringComparison.Ordinal)
                    ? "drag_drop_tab_select"
                    : "drag_drop_item_select";
            }

            return "drag_drop_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 12)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId switch
                {
                    "terminal_capabilities:matrix" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "terminal_capabilities_selection_prev"
                        : "terminal_capabilities_selection_next",
                    "terminal_capabilities:simulation" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "terminal_capabilities_profile_prev"
                        : "terminal_capabilities_profile_next",
                    _ => gesture.Button == TerminalMouseButton.WheelUp
                        ? "terminal_capabilities_panel_scroll_up"
                        : "terminal_capabilities_panel_scroll_down"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Right)
            {
                return hit.LocalHitId == "terminal_capabilities:simulation"
                    ? "terminal_capabilities_profile_reset"
                    : "terminal_capabilities_context_action";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "terminal_capabilities:matrix" => "terminal_capabilities_inspect",
                    "terminal_capabilities:evidence" => "terminal_capabilities_evidence_focus",
                    "terminal_capabilities:simulation" => "terminal_capabilities_profile_cycle",
                    _ => "terminal_capabilities_hit_test"
                };
            }

            return "terminal_capabilities_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 10)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId == "advanced:patterns"
                    ? gesture.Button == TerminalMouseButton.WheelUp
                        ? "advanced_patterns_scroll_up"
                        : "advanced_patterns_scroll_down"
                    : gesture.Button == TerminalMouseButton.WheelUp
                        ? "advanced_composite_scroll_up"
                        : "advanced_composite_scroll_down";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Right)
            {
                return hit.LocalHitId == "advanced:composite"
                    ? "advanced_composite_context"
                    : "advanced_patterns_context";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "advanced:patterns" => "advanced_patterns_focus",
                    "advanced:composite" => "advanced_composite_focus",
                    _ => "advanced_hit_test"
                };
            }

            return "advanced_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 8)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId == "data_viz:narrative"
                    ? gesture.Button == TerminalMouseButton.WheelUp
                        ? "data_viz_narrative_scroll_up"
                        : "data_viz_narrative_scroll_down"
                    : gesture.Button == TerminalMouseButton.WheelUp
                        ? "data_viz_metrics_scroll_up"
                        : "data_viz_metrics_scroll_down";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Right)
            {
                return hit.LocalHitId == "data_viz:narrative"
                    ? "data_viz_narrative_context"
                    : "data_viz_metrics_context";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "data_viz:progress" => "data_viz_progress_focus",
                    "data_viz:metrics_table" => "data_viz_metrics_focus",
                    "data_viz:narrative" => "data_viz_narrative_focus",
                    _ => "data_viz_hit_test"
                };
            }

            return "data_viz_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 11)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return gesture.Button == TerminalMouseButton.WheelUp
                    ? "table_theme_preset_prev"
                    : "table_theme_preset_next";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Right)
            {
                return "table_theme_preset_context";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId.StartsWith("table_theme:preset:", StringComparison.Ordinal)
                    ? "table_theme_preset_select"
                    : "table_theme_hit_test";
            }

            return "table_theme_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 14)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId switch
                {
                    "performance:list" or "performance:list:selected" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "performance_list_scroll_up"
                        : "performance_list_scroll_down",
                    "performance:stats" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "performance_stats_scroll_up"
                        : "performance_stats_scroll_down",
                    _ => gesture.Button == TerminalMouseButton.WheelUp
                        ? "performance_panel_scroll_up"
                        : "performance_panel_scroll_down"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Right)
            {
                return hit.LocalHitId == "performance:stats"
                    ? "performance_stats_context"
                    : "performance_row_context";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "performance:list" => "performance_list_focus",
                    "performance:list:selected" => "performance_row_select",
                    "performance:stats" => "performance_stats_focus",
                    "performance:footer" => "performance_controls_focus",
                    _ => "performance_hit_test"
                };
            }

            return "performance_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 15)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId switch
                {
                    "markdown:renderer" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "markdown_renderer_scroll_up"
                        : "markdown_renderer_scroll_down",
                    "markdown:stream" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "markdown_stream_scroll_up"
                        : "markdown_stream_scroll_down",
                    "markdown:unicode" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "markdown_unicode_scroll_up"
                        : "markdown_unicode_scroll_down",
                    _ => gesture.Button == TerminalMouseButton.WheelUp
                        ? "markdown_panel_scroll_up"
                        : "markdown_panel_scroll_down"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Right)
            {
                return hit.LocalHitId == "markdown:wrap"
                    ? "markdown_wrap_context"
                    : "markdown_panel_context";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "markdown:renderer" => "markdown_renderer_focus",
                    "markdown:stream" => "markdown_stream_focus",
                    "markdown:detection" => "markdown_detection_focus",
                    "markdown:style" => "markdown_style_focus",
                    "markdown:unicode" => "markdown_unicode_focus",
                    "markdown:wrap" => "markdown_wrap_mode_cycle",
                    _ => "markdown_hit_test"
                };
            }

            return "markdown_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 16)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId switch
                {
                    "mermaid:library" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "mermaid_sample_prev"
                        : "mermaid_sample_next",
                    "mermaid:viewport" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "mermaid_viewport_zoom_in"
                        : "mermaid_viewport_zoom_out",
                    "mermaid:status" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "mermaid_status_scroll_up"
                        : "mermaid_status_scroll_down",
                    _ => gesture.Button == TerminalMouseButton.WheelUp
                        ? "mermaid_panel_scroll_up"
                        : "mermaid_panel_scroll_down"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Right)
            {
                return hit.LocalHitId == "mermaid:viewport"
                    ? "mermaid_viewport_reset"
                    : "mermaid_context_action";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "mermaid:header" => "mermaid_header_focus",
                    "mermaid:library" => "mermaid_sample_select",
                    "mermaid:viewport" => "mermaid_viewport_focus",
                    "mermaid:controls" => "mermaid_controls_focus",
                    "mermaid:metrics" => "mermaid_metrics_focus",
                    "mermaid:status" => "mermaid_status_focus",
                    _ => "mermaid_hit_test"
                };
            }

            return "mermaid_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 17)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId switch
                {
                    "mermaid_mega:library" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "mermaid_mega_sample_prev"
                        : "mermaid_mega_sample_next",
                    "mermaid_mega:shared_showcase" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "mermaid_mega_viewport_zoom_in"
                        : "mermaid_mega_viewport_zoom_out",
                    "mermaid_mega:node_detail" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "mermaid_mega_detail_scroll_up"
                        : "mermaid_mega_detail_scroll_down",
                    _ => gesture.Button == TerminalMouseButton.WheelUp
                        ? "mermaid_mega_panel_scroll_up"
                        : "mermaid_mega_panel_scroll_down"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Right)
            {
                return hit.LocalHitId == "mermaid_mega:shared_showcase"
                    ? "mermaid_mega_viewport_reset"
                    : "mermaid_mega_context_action";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "mermaid_mega:shared_showcase" => "mermaid_mega_viewport_focus",
                    "mermaid_mega:library" => "mermaid_mega_sample_select",
                    "mermaid_mega:controls" => "mermaid_mega_controls_focus",
                    "mermaid_mega:node_detail" => "mermaid_mega_node_detail_focus",
                    _ => "mermaid_mega_hit_test"
                };
            }

            return "mermaid_mega_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 9)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId == "file_browser:preview"
                    ? gesture.Button == TerminalMouseButton.WheelUp
                        ? "file_browser_preview_scroll_up"
                        : "file_browser_preview_scroll_down"
                    : gesture.Button == TerminalMouseButton.WheelUp
                        ? "file_browser_tree_scroll_up"
                        : "file_browser_tree_scroll_down";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId == "file_browser:preview"
                    ? "file_browser_preview_focus"
                    : "file_browser_tree_select";
            }

            return "file_browser_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 21)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId == "notifications:lifecycle"
                    ? gesture.Button == TerminalMouseButton.WheelUp
                        ? "notifications_lifecycle_scroll_up"
                        : "notifications_lifecycle_scroll_down"
                    : gesture.Button == TerminalMouseButton.WheelUp
                        ? "notifications_push_success"
                        : "notifications_push_info";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                if (hit.LocalHitId.StartsWith("notifications:trigger:", StringComparison.Ordinal))
                {
                    return hit.LocalHitId["notifications:trigger:".Length..] switch
                    {
                        "success" => "notifications_trigger_success",
                        "error" => "notifications_trigger_error",
                        "warning" => "notifications_trigger_warning",
                        "info" => "notifications_trigger_info",
                        "urgent" => "notifications_trigger_urgent",
                        "dismiss_all" => "notifications_dismiss_all",
                        _ => "notifications_trigger"
                    };
                }

                return hit.LocalHitId.StartsWith("notifications:toast:", StringComparison.Ordinal)
                    ? "notifications_toast_click"
                    : "notifications_lifecycle_focus";
            }

            return "notifications_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 18)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId == "visual_effects:canvas"
                    ? gesture.Button == TerminalMouseButton.WheelUp
                        ? "visual_effects_effect_prev"
                        : "visual_effects_effect_next"
                    : gesture.Button == TerminalMouseButton.WheelUp
                        ? "visual_effects_harness_scroll_up"
                        : "visual_effects_harness_scroll_down";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId == "visual_effects:canvas"
                    ? "visual_effects_canvas_focus"
                    : "visual_effects_harness_focus";
            }

            return "visual_effects_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 19)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return gesture.Button == TerminalMouseButton.WheelUp
                    ? "responsive_width_decrement"
                    : "responsive_width_increment";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Right)
            {
                return "responsive_breakpoints_reset";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "responsive:indicator" => "responsive_breakpoints_toggle",
                    "responsive:layout_info" => "responsive_layout_info_focus",
                    "responsive:values" => "responsive_values_focus",
                    "responsive:sidebar" => "responsive_sidebar_focus",
                    "responsive:content" => "responsive_content_focus",
                    "responsive:aside" => "responsive_aside_toggle",
                    _ => "responsive_hit_test"
                };
            }

            return "responsive_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 22)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId == "action_timeline:timeline"
                    ? gesture.Button == TerminalMouseButton.WheelUp
                        ? "action_timeline_scroll_up"
                        : "action_timeline_scroll_down"
                    : gesture.Button == TerminalMouseButton.WheelUp
                        ? "action_timeline_panel_scroll_up"
                        : "action_timeline_panel_scroll_down";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Right)
            {
                return hit.LocalHitId == "action_timeline:filters"
                    ? "action_timeline_clear"
                    : "action_timeline_context_action";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "action_timeline:filters" => "action_timeline_filter_cycle",
                    "action_timeline:timeline" => "action_timeline_event_select",
                    "action_timeline:detail" => "action_timeline_detail_toggle",
                    _ => "action_timeline_hit_test"
                };
            }

            return "action_timeline_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 23)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId switch
                {
                    "intrinsic_sizing:scenarios" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "intrinsic_sizing_scenario_prev"
                        : "intrinsic_sizing_scenario_next",
                    "intrinsic_sizing:detail" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "intrinsic_sizing_width_decrement"
                        : "intrinsic_sizing_width_increment",
                    _ => gesture.Button == TerminalMouseButton.WheelUp
                        ? "intrinsic_sizing_panel_scroll_up"
                        : "intrinsic_sizing_panel_scroll_down"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Right)
            {
                return "intrinsic_sizing_pane_mode";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "intrinsic_sizing:header" => "intrinsic_sizing_header_focus",
                    "intrinsic_sizing:scenarios" => "intrinsic_sizing_scenario_select",
                    "intrinsic_sizing:detail" => "intrinsic_sizing_detail_focus",
                    "intrinsic_sizing:controls" => "intrinsic_sizing_controls_focus",
                    _ => "intrinsic_sizing_hit_test"
                };
            }

            return "intrinsic_sizing_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 24)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId switch
                {
                    "layout_inspector:info" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "layout_inspector_scenario_prev"
                        : "layout_inspector_scenario_next",
                    "layout_inspector:overlay" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "layout_inspector_step_prev"
                        : "layout_inspector_step_next",
                    "layout_inspector:tree" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "layout_inspector_tree_scroll_up"
                        : "layout_inspector_tree_scroll_down",
                    _ => gesture.Button == TerminalMouseButton.WheelUp
                        ? "layout_inspector_panel_scroll_up"
                        : "layout_inspector_panel_scroll_down"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Right)
            {
                return hit.LocalHitId == "layout_inspector:pane_studio"
                    ? "layout_inspector_pane_mode"
                    : "layout_inspector_overlay_toggle";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "layout_inspector:info" => "layout_inspector_scenario_select",
                    "layout_inspector:overlay" => "layout_inspector_step_select",
                    "layout_inspector:tree" => "layout_inspector_tree_focus",
                    "layout_inspector:pane_studio" => "layout_inspector_pane_focus",
                    _ => "layout_inspector_hit_test"
                };
            }

            return "layout_inspector_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 13)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId.StartsWith("macro_recorder:timeline:", StringComparison.Ordinal)
                    ? gesture.Button == TerminalMouseButton.WheelUp
                        ? "macro_timeline_scroll_up"
                        : "macro_timeline_scroll_down"
                    : gesture.Button == TerminalMouseButton.WheelUp
                        ? "macro_panel_scroll_up"
                        : "macro_panel_scroll_down";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                if (hit.LocalHitId.StartsWith("macro_recorder:timeline:", StringComparison.Ordinal))
                {
                    return "macro_timeline_select";
                }

                return hit.LocalHitId switch
                {
                    "macro_recorder:controls" => "macro_controls_focus",
                    "macro_recorder:event_detail" => "macro_event_detail_focus",
                    "macro_recorder:scenario_runner" => "macro_scenario_select",
                    _ => "macro_hit_test"
                };
            }

            return "macro_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 20)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                if (hit.LocalHitId.StartsWith("log_search:result:", StringComparison.Ordinal))
                {
                    return gesture.Button == TerminalMouseButton.WheelUp
                        ? "log_search_results_scroll_up"
                        : "log_search_results_scroll_down";
                }

                if (hit.LocalHitId.StartsWith("log_search:diagnostic:", StringComparison.Ordinal))
                {
                    return gesture.Button == TerminalMouseButton.WheelUp
                        ? "log_search_diagnostics_scroll_up"
                        : "log_search_diagnostics_scroll_down";
                }

                return gesture.Button == TerminalMouseButton.WheelUp
                    ? "log_search_panel_scroll_up"
                    : "log_search_panel_scroll_down";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                if (hit.LocalHitId.StartsWith("log_search:result:", StringComparison.Ordinal))
                {
                    return "log_search_result_select";
                }

                if (hit.LocalHitId.StartsWith("log_search:diagnostic:", StringComparison.Ordinal))
                {
                    return "log_search_diagnostic_select";
                }

                return hit.LocalHitId switch
                {
                    "log_search:live_stream" => "log_search_live_stream_focus",
                    "log_search:controls" => "log_search_controls_focus",
                    _ => "log_search_hit_test"
                };
            }

            return "log_search_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 25)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                if (hit.LocalHitId.StartsWith("advanced_text_editor:line:", StringComparison.Ordinal))
                {
                    return gesture.Button == TerminalMouseButton.WheelUp
                        ? "advanced_text_editor_scroll_up"
                        : "advanced_text_editor_scroll_down";
                }

                if (hit.LocalHitId.StartsWith("advanced_text_editor:history:", StringComparison.Ordinal))
                {
                    return gesture.Button == TerminalMouseButton.WheelUp
                        ? "advanced_text_editor_history_scroll_up"
                        : "advanced_text_editor_history_scroll_down";
                }

                if (hit.LocalHitId.StartsWith("advanced_text_editor:diagnostic:", StringComparison.Ordinal))
                {
                    return gesture.Button == TerminalMouseButton.WheelUp
                        ? "advanced_text_editor_diagnostics_scroll_up"
                        : "advanced_text_editor_diagnostics_scroll_down";
                }

                return gesture.Button == TerminalMouseButton.WheelUp
                    ? "advanced_text_editor_panel_scroll_up"
                    : "advanced_text_editor_panel_scroll_down";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                if (hit.LocalHitId.StartsWith("advanced_text_editor:line:", StringComparison.Ordinal))
                {
                    return "advanced_text_editor_line_select";
                }

                if (hit.LocalHitId.StartsWith("advanced_text_editor:history:", StringComparison.Ordinal))
                {
                    return "advanced_text_editor_history_select";
                }

                if (hit.LocalHitId.StartsWith("advanced_text_editor:diagnostic:", StringComparison.Ordinal))
                {
                    return "advanced_text_editor_diagnostic_select";
                }

                return hit.LocalHitId switch
                {
                    "advanced_text_editor:search" => "advanced_text_editor_search_focus",
                    _ => "advanced_text_editor_hit_test"
                };
            }

            return "advanced_text_editor_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 28)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                if (hit.LocalHitId.StartsWith("virtualized_search:result:", StringComparison.Ordinal))
                {
                    return gesture.Button == TerminalMouseButton.WheelUp
                        ? "virtualized_search_results_scroll_up"
                        : "virtualized_search_results_scroll_down";
                }

                if (hit.LocalHitId.StartsWith("virtualized_search:diagnostic:", StringComparison.Ordinal))
                {
                    return gesture.Button == TerminalMouseButton.WheelUp
                        ? "virtualized_search_diagnostics_scroll_up"
                        : "virtualized_search_diagnostics_scroll_down";
                }

                return gesture.Button == TerminalMouseButton.WheelUp
                    ? "virtualized_search_panel_scroll_up"
                    : "virtualized_search_panel_scroll_down";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                if (hit.LocalHitId.StartsWith("virtualized_search:result:", StringComparison.Ordinal))
                {
                    return "virtualized_search_result_select";
                }

                if (hit.LocalHitId.StartsWith("virtualized_search:diagnostic:", StringComparison.Ordinal))
                {
                    return "virtualized_search_diagnostic_select";
                }

                return hit.LocalHitId switch
                {
                    "virtualized_search:search_bar" => "virtualized_search_focus_search",
                    "virtualized_search:stats" => "virtualized_search_stats_focus",
                    _ => "virtualized_search_hit_test"
                };
            }

            return "virtualized_search_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 29)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                if (hit.LocalHitId.StartsWith("async_tasks:task:", StringComparison.Ordinal))
                {
                    return gesture.Button == TerminalMouseButton.WheelUp
                        ? "async_tasks_queue_scroll_up"
                        : "async_tasks_queue_scroll_down";
                }

                return hit.LocalHitId == "async_tasks:hazard"
                    ? gesture.Button == TerminalMouseButton.WheelUp
                        ? "async_tasks_hazard_scroll_up"
                        : "async_tasks_hazard_scroll_down"
                    : gesture.Button == TerminalMouseButton.WheelUp
                        ? "async_tasks_panel_scroll_up"
                        : "async_tasks_panel_scroll_down";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                if (hit.LocalHitId.StartsWith("async_tasks:task:", StringComparison.Ordinal))
                {
                    return "async_tasks_task_select";
                }

                return hit.LocalHitId switch
                {
                    "async_tasks:scheduler" => "async_tasks_scheduler_focus",
                    "async_tasks:details" => "async_tasks_details_focus",
                    "async_tasks:activity" => "async_tasks_activity_focus",
                    "async_tasks:evidence" => "async_tasks_evidence_focus",
                    "async_tasks:hazard" => "async_tasks_hazard_focus",
                    "async_tasks:footer" => "async_tasks_controls_focus",
                    _ => "async_tasks_hit_test"
                };
            }

            return "async_tasks_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 30)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                if (hit.LocalHitId.StartsWith("theme_studio:token:", StringComparison.Ordinal))
                {
                    return gesture.Button == TerminalMouseButton.WheelUp
                        ? "theme_studio_tokens_scroll_up"
                        : "theme_studio_tokens_scroll_down";
                }

                return hit.LocalHitId == "theme_studio:diagnostics"
                    ? gesture.Button == TerminalMouseButton.WheelUp
                        ? "theme_studio_diagnostics_scroll_up"
                        : "theme_studio_diagnostics_scroll_down"
                    : gesture.Button == TerminalMouseButton.WheelUp
                        ? "theme_studio_panel_scroll_up"
                        : "theme_studio_panel_scroll_down";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                if (hit.LocalHitId.StartsWith("theme_studio:preset:", StringComparison.Ordinal))
                {
                    return "theme_studio_preset_select";
                }

                if (hit.LocalHitId.StartsWith("theme_studio:token:", StringComparison.Ordinal))
                {
                    return "theme_studio_token_select";
                }

                return hit.LocalHitId switch
                {
                    "theme_studio:export" => "theme_studio_export_focus",
                    "theme_studio:diagnostics" => "theme_studio_diagnostics_focus",
                    "theme_studio:footer" => "theme_studio_controls_focus",
                    _ => "theme_studio_hit_test"
                };
            }

            return "theme_studio_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 31)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId switch
                {
                    "snapshot_player:timeline" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "snapshot_player_timeline_scroll_up"
                        : "snapshot_player_timeline_scroll_down",
                    "snapshot_player:diagnostics" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "snapshot_player_diagnostics_scroll_up"
                        : "snapshot_player_diagnostics_scroll_down",
                    _ => gesture.Button == TerminalMouseButton.WheelUp
                        ? "snapshot_player_panel_scroll_up"
                        : "snapshot_player_panel_scroll_down"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Right)
            {
                return hit.LocalHitId switch
                {
                    "snapshot_player:timeline" => "snapshot_player_marker_toggle",
                    "snapshot_player:preview" => "snapshot_player_heatmap_toggle",
                    _ => "snapshot_player_context_action"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Drag && hit.LocalHitId == "snapshot_player:timeline")
            {
                return "snapshot_player_timeline_scrub";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "snapshot_player:timeline" => "snapshot_player_timeline_select",
                    "snapshot_player:preview" => "snapshot_player_preview_focus",
                    "snapshot_player:compare" => "snapshot_player_compare_focus",
                    "snapshot_player:frame_info" => "snapshot_player_frame_info_focus",
                    "snapshot_player:controls" => "snapshot_player_controls_focus",
                    "snapshot_player:diagnostics" => "snapshot_player_diagnostics_focus",
                    _ => "snapshot_player_hit_test"
                };
            }

            return "snapshot_player_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 32)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId switch
                {
                    "performance_challenge:sparkline" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "performance_challenge_sparkline_scroll_up"
                        : "performance_challenge_sparkline_scroll_down",
                    "performance_challenge:budget" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "performance_challenge_budget_scroll_up"
                        : "performance_challenge_budget_scroll_down",
                    _ => gesture.Button == TerminalMouseButton.WheelUp
                        ? "performance_challenge_panel_scroll_up"
                        : "performance_challenge_panel_scroll_down"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                if (hit.LocalHitId.StartsWith("performance_challenge:tier:", StringComparison.Ordinal))
                {
                    return "performance_challenge_tier_select";
                }

                return hit.LocalHitId switch
                {
                    "performance_challenge:header" => "performance_challenge_header_focus",
                    "performance_challenge:metrics" => "performance_challenge_metrics_focus",
                    "performance_challenge:sparkline" => "performance_challenge_sparkline_focus",
                    "performance_challenge:evidence" => "performance_challenge_evidence_focus",
                    "performance_challenge:budget" => "performance_challenge_budget_focus",
                    "performance_challenge:stress" => "performance_challenge_stress_toggle",
                    "performance_challenge:footer" => "performance_challenge_controls_focus",
                    _ => "performance_challenge_hit_test"
                };
            }

            return "performance_challenge_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 33)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId switch
                {
                    "explainability:timeline" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "explainability_timeline_scroll_up"
                        : "explainability_timeline_scroll_down",
                    "explainability:source_controls" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "explainability_source_scroll_up"
                        : "explainability_source_scroll_down",
                    _ => gesture.Button == TerminalMouseButton.WheelUp
                        ? "explainability_panel_scroll_up"
                        : "explainability_panel_scroll_down"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "explainability:header" => "explainability_header_focus",
                    "explainability:diff_strategy" => "explainability_diff_focus",
                    "explainability:resize_regime" => "explainability_resize_focus",
                    "explainability:budget_decisions" => "explainability_budget_focus",
                    "explainability:timeline" => "explainability_timeline_focus",
                    "explainability:source_controls" => "explainability_source_controls_focus",
                    _ => "explainability_hit_test"
                };
            }

            return "explainability_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 34)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId switch
                {
                    "i18n:plural_rules" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "i18n_plural_count_decrement"
                        : "i18n_plural_count_increment",
                    "i18n:stress_lab" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "i18n_stress_sample_prev"
                        : "i18n_stress_sample_next",
                    _ => gesture.Button == TerminalMouseButton.WheelUp
                        ? "i18n_panel_scroll_up"
                        : "i18n_panel_scroll_down"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "i18n:locale_bar" => "i18n_locale_select",
                    "i18n:string_lookup" => "i18n_string_lookup_focus",
                    "i18n:plural_rules" => "i18n_plural_rules_focus",
                    "i18n:rtl_layout" => "i18n_rtl_layout_focus",
                    "i18n:stress_lab" => "i18n_stress_lab_focus",
                    "i18n:footer" => "i18n_controls_focus",
                    _ => "i18n_hit_test"
                };
            }

            return "i18n_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 35)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId switch
                {
                    "voi_overlay:ledger" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "voi_overlay_ledger_scroll_up"
                        : "voi_overlay_ledger_scroll_down",
                    "voi_overlay:controls" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "voi_overlay_controls_scroll_up"
                        : "voi_overlay_controls_scroll_down",
                    _ => gesture.Button == TerminalMouseButton.WheelUp
                        ? "voi_overlay_panel_scroll_up"
                        : "voi_overlay_panel_scroll_down"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "voi_overlay:header" => "voi_overlay_header_focus",
                    "voi_overlay:decision" => "voi_overlay_decision_focus",
                    "voi_overlay:posterior" => "voi_overlay_posterior_focus",
                    "voi_overlay:observation" => "voi_overlay_observation_focus",
                    "voi_overlay:ledger" => "voi_overlay_ledger_focus",
                    "voi_overlay:controls" => "voi_overlay_controls_focus",
                    "voi_overlay:footer" => "voi_overlay_footer_focus",
                    _ => "voi_overlay_hit_test"
                };
            }

            return "voi_overlay_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 36)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId switch
                {
                    "inline_mode:inline_story" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "inline_mode_log_rate_decrement"
                        : "inline_mode_log_rate_increment",
                    "inline_mode:state_limits" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "inline_mode_state_scroll_up"
                        : "inline_mode_state_scroll_down",
                    _ => gesture.Button == TerminalMouseButton.WheelUp
                        ? "inline_mode_panel_scroll_up"
                        : "inline_mode_panel_scroll_down"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "inline_mode:header" => "inline_mode_compare_toggle",
                    "inline_mode:inline_story" => "inline_mode_stream_pause_toggle",
                    "inline_mode:alt_story" => "inline_mode_alt_focus",
                    "inline_mode:controls" => "inline_mode_controls_focus",
                    "inline_mode:state_limits" => "inline_mode_state_focus",
                    "inline_mode:footer" => "inline_mode_footer_focus",
                    _ => "inline_mode_hit_test"
                };
            }

            return "inline_mode_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 37)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId switch
                {
                    "accessibility:telemetry" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "accessibility_telemetry_scroll_up"
                        : "accessibility_telemetry_scroll_down",
                    "accessibility:preview" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "accessibility_preview_scroll_up"
                        : "accessibility_preview_scroll_down",
                    _ => gesture.Button == TerminalMouseButton.WheelUp
                        ? "accessibility_panel_scroll_up"
                        : "accessibility_panel_scroll_down"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "accessibility:overview" => "accessibility_overview_focus",
                    "accessibility:toggles" => "accessibility_toggle_select",
                    "accessibility:preview" => "accessibility_preview_focus",
                    "accessibility:wcag" => "accessibility_wcag_focus",
                    "accessibility:telemetry" => "accessibility_telemetry_focus",
                    "accessibility:footer" => "accessibility_controls_focus",
                    _ => "accessibility_hit_test"
                };
            }

            return "accessibility_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 38)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId switch
                {
                    "widget_builder:presets" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "widget_builder_preset_prev"
                        : "widget_builder_preset_next",
                    "widget_builder:tree" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "widget_builder_tree_scroll_up"
                        : "widget_builder_tree_scroll_down",
                    "widget_builder:props" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "widget_builder_value_decrement"
                        : "widget_builder_value_increment",
                    _ => gesture.Button == TerminalMouseButton.WheelUp
                        ? "widget_builder_panel_scroll_up"
                        : "widget_builder_panel_scroll_down"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Right)
            {
                return hit.LocalHitId switch
                {
                    "widget_builder:presets" => "widget_builder_preset_save",
                    "widget_builder:tree" => "widget_builder_border_toggle",
                    _ => "widget_builder_context_action"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "widget_builder:header" => "widget_builder_header_focus",
                    "widget_builder:presets" => "widget_builder_preset_select",
                    "widget_builder:tree" => "widget_builder_tree_select",
                    "widget_builder:preview" => "widget_builder_preview_toggle",
                    "widget_builder:props" => "widget_builder_props_focus",
                    "widget_builder:export" => "widget_builder_export_focus",
                    "widget_builder:footer" => "widget_builder_controls_focus",
                    _ => "widget_builder_hit_test"
                };
            }

            return "widget_builder_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 40)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId switch
                {
                    "determinism:equivalence" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "determinism_seed_decrement"
                        : "determinism_seed_increment",
                    "determinism:checks" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "determinism_checks_scroll_up"
                        : "determinism_checks_scroll_down",
                    "determinism:report" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "determinism_report_scroll_up"
                        : "determinism_report_scroll_down",
                    _ => gesture.Button == TerminalMouseButton.WheelUp
                        ? "determinism_panel_scroll_up"
                        : "determinism_panel_scroll_down"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId switch
                {
                    "determinism:header" => "determinism_header_focus",
                    "determinism:equivalence" => "determinism_strategy_select",
                    "determinism:report" => "determinism_export_focus",
                    "determinism:preview" => "determinism_preview_focus",
                    "determinism:checks" => "determinism_scenario_run",
                    "determinism:footer" => "determinism_controls_focus",
                    _ => "determinism_hit_test"
                };
            }

            return "determinism_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 7)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId == "forms_input:text_area"
                    ? gesture.Button == TerminalMouseButton.WheelUp
                        ? "forms_input_text_scroll_up"
                        : "forms_input_text_scroll_down"
                    : gesture.Button == TerminalMouseButton.WheelUp
                        ? "forms_input_field_scroll_up"
                        : "forms_input_field_scroll_down";
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                return hit.LocalHitId == "forms_input:text_area"
                    ? "forms_input_text_focus"
                    : "forms_input_field_focus";
            }

            return "forms_input_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 27)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll)
            {
                return hit.LocalHitId switch
                {
                    "form_validation:rules" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "form_validation_rules_scroll_up"
                        : "form_validation_rules_scroll_down",
                    "form_validation:diagnostics" => gesture.Button == TerminalMouseButton.WheelUp
                        ? "form_validation_diagnostics_scroll_up"
                        : "form_validation_diagnostics_scroll_down",
                    _ => gesture.Button == TerminalMouseButton.WheelUp
                        ? "form_validation_scroll_up"
                        : "form_validation_scroll_down"
                };
            }

            if (gesture.Kind == TerminalMouseKind.Down && gesture.Button == TerminalMouseButton.Left)
            {
                if (hit.LocalHitId.StartsWith("form_validation:field:", StringComparison.Ordinal))
                {
                    return "form_validation_field_focus";
                }

                if (hit.LocalHitId.StartsWith("form_validation:error:", StringComparison.Ordinal))
                {
                    return "form_validation_error_select";
                }

                return hit.LocalHitId switch
                {
                    "form_validation:mode" => "form_validation_mode_toggle",
                    "form_validation:touched_dirty" => "form_validation_state_focus",
                    "form_validation:rules" => "form_validation_rules_focus",
                    "form_validation:controls" => "form_validation_controls_focus",
                    "form_validation:notifications" => "form_validation_notifications_focus",
                    "form_validation:diagnostics" => "form_validation_diagnostics_focus",
                    _ => "form_validation_hit_test"
                };
            }

            return "form_validation_hit_test";
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 42)
        {
            return gesture.Kind switch
            {
                TerminalMouseKind.Down when gesture.Button == TerminalMouseButton.Left => "kanban_drag_start",
                TerminalMouseKind.Drag when gesture.Button == TerminalMouseButton.Left => "kanban_drag_move",
                TerminalMouseKind.Up when gesture.Button == TerminalMouseButton.Left => "kanban_drop",
                _ => "kanban_hit_test"
            };
        }

        if (hit.Layer == ShowcaseHitLayer.Content && before.CurrentScreenNumber == 26)
        {
            return gesture.Kind switch
            {
                TerminalMouseKind.Down when gesture.Button == TerminalMouseButton.Left => "target_click",
                TerminalMouseKind.Move => "mouse_move",
                TerminalMouseKind.Drag => "mouse_drag",
                TerminalMouseKind.Scroll => "mouse_scroll",
                _ => "hit_test"
            };
        }

        if (hit.Layer == ShowcaseHitLayer.Link)
        {
            return gesture.Kind switch
            {
                TerminalMouseKind.Down when gesture.Button == TerminalMouseButton.Left => "mouse_select",
                TerminalMouseKind.Up when gesture.Button == TerminalMouseButton.Left => "mouse_activate",
                TerminalMouseKind.Move or TerminalMouseKind.Drag => "link_hover",
                _ => "link_forward"
            };
        }

        if (hit.Layer == ShowcaseHitLayer.StatusToggle && hit.UpstreamHitId is { } statusRawId)
        {
            return statusRawId switch
            {
                ShowcaseFrameHitRegistry.StatusHelpToggle when before.HelpVisible != after.HelpVisible => "status_toggle_help",
                ShowcaseFrameHitRegistry.StatusPaletteToggle when before.Session.CommandPalette.IsOpen != after.Session.CommandPalette.IsOpen => "status_toggle_palette",
                ShowcaseFrameHitRegistry.StatusA11yToggle when before.A11yPanelVisible != after.A11yPanelVisible => "status_toggle_a11y",
                ShowcaseFrameHitRegistry.StatusPerfToggle when before.PerfHudVisible != after.PerfHudVisible => "status_toggle_perf",
                ShowcaseFrameHitRegistry.StatusDebugToggle when before.DebugVisible != after.DebugVisible => "status_toggle_debug",
                ShowcaseFrameHitRegistry.StatusMouseToggle when before.MouseCaptureEnabled != after.MouseCaptureEnabled => "status_toggle_mouse",
                _ => "status_unknown"
            };
        }

        if (before.MouseCaptureEnabled != after.MouseCaptureEnabled)
        {
            return "status_toggle_mouse";
        }

        if (before.HelpVisible != after.HelpVisible)
        {
            return "status_toggle_help";
        }

        if (before.A11yPanelVisible != after.A11yPanelVisible)
        {
            return "status_toggle_a11y";
        }

        if (before.PerfHudVisible != after.PerfHudVisible)
        {
            return "status_toggle_perf";
        }

        if (before.DebugVisible != after.DebugVisible)
        {
            return "status_toggle_debug";
        }

        if (before.EvidenceLedgerVisible != after.EvidenceLedgerVisible)
        {
            return "status_toggle_evidence";
        }

        if (before.Session.CommandPalette.IsOpen != after.Session.CommandPalette.IsOpen)
        {
            return "status_toggle_palette";
        }

        if (!before.TourActive && after.TourActive)
        {
            return "overlay_tour_start";
        }

        if (before.TourActive && !after.TourActive)
        {
            return "overlay_tour_click";
        }

        if (before.CurrentScreenNumber != after.CurrentScreenNumber)
        {
            var navigationHit = ShowcaseFrameHitRegistry.HitTest(before, gesture.Column, gesture.Row);
            if (gesture.Kind == TerminalMouseKind.Scroll && navigationHit.Layer == ShowcaseHitLayer.Tab)
            {
                return gesture.Button == TerminalMouseButton.WheelUp
                    ? "scroll_prev_tab"
                    : "scroll_next_tab";
            }

            return "switch_screen";
        }

        if (gesture.Button == TerminalMouseButton.Left &&
            gesture.Kind is TerminalMouseKind.Down or TerminalMouseKind.Up &&
            hit.Layer is ShowcaseHitLayer.Tab or ShowcaseHitLayer.Category &&
            hit.TargetScreenNumber == before.CurrentScreenNumber)
        {
            return "tab_no_change";
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            return "scroll_forward";
        }

        return gesture.Kind switch
        {
            TerminalMouseKind.Down => "down_forward",
            TerminalMouseKind.Up => "up_forward",
            TerminalMouseKind.Drag => "drag_forward",
            TerminalMouseKind.Move => "move",
            _ => "mouse_forward"
        };
    }

    private static string? HoverHitKey(ShowcaseHitTestResult hit) =>
        hit.LocalHitId == "none" && hit.UpstreamHitId is null
            ? null
            : hit.UpstreamHitId?.ToString(CultureInfo.InvariantCulture) ?? hit.LocalHitId;

    private static bool IsNonLeftChromeOrPaneClickTarget(
        MouseGesture gesture,
        ShowcaseDemoState before,
        ShowcaseHitTestResult hit)
    {
        if (gesture.Button == TerminalMouseButton.Left ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Up))
        {
            return false;
        }

        return IsChromeOrDashboardLinkTarget(before, hit);
    }

    private static bool IsChromeOrDashboardLinkTarget(ShowcaseDemoState before, ShowcaseHitTestResult hit) =>
        IsChromeTarget(hit) ||
        hit is { Layer: ShowcaseHitLayer.Pane, TargetScreenNumber: { } target } &&
        before.CurrentScreenNumber == 2 &&
        target != before.CurrentScreenNumber;

    private static bool IsChromeTarget(ShowcaseHitTestResult hit) =>
        hit.Layer is ShowcaseHitLayer.Overlay or ShowcaseHitLayer.StatusToggle or ShowcaseHitLayer.Tab or ShowcaseHitLayer.Category;

    private static string MouseKindLabel(MouseGesture gesture) =>
        gesture.Kind switch
        {
            TerminalMouseKind.Down => $"down_{ButtonLabel(gesture.Button)}",
            TerminalMouseKind.Up => $"up_{ButtonLabel(gesture.Button)}",
            TerminalMouseKind.Drag => $"drag_{ButtonLabel(gesture.Button)}",
            TerminalMouseKind.Move => "moved",
            TerminalMouseKind.Scroll when gesture.Button == TerminalMouseButton.WheelUp => "scroll_up",
            TerminalMouseKind.Scroll when gesture.Button == TerminalMouseButton.WheelDown => "scroll_down",
            TerminalMouseKind.Scroll => "scroll",
            _ => gesture.Kind.ToString().ToLowerInvariant()
        };

    private static string MouseCaptureSourceLabel(string trigger, TerminalEvent? terminalEvent)
    {
        if (terminalEvent is null)
        {
            return trigger == "tick" ? "playback" : "user";
        }

        return "user";
    }

    private static string ResolveLocalMouseHitId(
        MouseGesture gesture,
        ShowcaseDemoState before,
        ShowcaseDemoState after)
    {
        if (before.Session.CommandPalette.IsOpen)
        {
            return "palette";
        }

        if (before.CurrentScreenNumber == 39 &&
            TryResolvePaletteLabHit(gesture, before))
        {
            return "palette_lab";
        }

        if (gesture.Row == 0)
        {
            return before.CurrentScreenNumber == after.CurrentScreenNumber
                ? "category"
                : $"category:{after.CurrentScreen.Category}";
        }

        if (gesture.Row == 1)
        {
            return before.CurrentScreenNumber == after.CurrentScreenNumber
                ? "tab"
                : $"tab:{after.CurrentScreenNumber}";
        }

        if (before.CurrentScreenNumber == 1)
        {
            return "overlay:tour";
        }

        if (before.CurrentScreenNumber == 2 && before.CurrentScreenNumber != after.CurrentScreenNumber)
        {
            return $"pane:{after.CurrentScreenNumber}";
        }

        if (before.Viewport.Height > 0 && gesture.Row == before.Viewport.Height - 1)
        {
            return "status";
        }

        return "none";
    }

    private static bool TryResolvePaletteLabHit(MouseGesture gesture, ShowcaseDemoState before) =>
        ShowcaseDemoState.TryResolvePaletteLabPaletteArea(before.Viewport, out var area) &&
        gesture.Column >= area.X &&
        gesture.Column < area.X + area.Width &&
        gesture.Row >= area.Y &&
        gesture.Row < area.Y + area.Height;

    private static string ButtonLabel(TerminalMouseButton button) =>
        button switch
        {
            TerminalMouseButton.Left => "left",
            TerminalMouseButton.Middle => "middle",
            TerminalMouseButton.Right => "right",
            TerminalMouseButton.WheelUp => "wheel_up",
            TerminalMouseButton.WheelDown => "wheel_down",
            _ => "none"
        };

    private static bool FavoriteIdsEqual(CommandPaletteState before, CommandPaletteState after)
    {
        var beforeIds = before.FavoriteEntryIds ?? [];
        var afterIds = after.FavoriteEntryIds ?? [];
        return beforeIds.Count == afterIds.Count &&
            beforeIds.Order(StringComparer.Ordinal).SequenceEqual(afterIds.Order(StringComparer.Ordinal));
    }

    private static IEnumerable<string> ClassifyA11yActions(ShowcaseDemoState before, ShowcaseDemoState after)
    {
        if (before.A11yPanelVisible != after.A11yPanelVisible)
        {
            yield return "panel_toggle";
        }

        if (before.A11yHighContrast != after.A11yHighContrast)
        {
            yield return "high_contrast_toggle";
        }

        if (before.A11yReducedMotion != after.A11yReducedMotion)
        {
            yield return "reduced_motion_toggle";
        }

        if (before.A11yLargeText != after.A11yLargeText)
        {
            yield return "large_text_toggle";
        }
    }

    private static string BoolString(bool value) => value ? "true" : "false";

    private void Write(
        string eventName,
        ShowcaseCliOptions options,
        RuntimeFrameStats stats,
        int stepIndex,
        int frame,
        IReadOnlyDictionary<string, object?>? extra)
    {
        var screen = ShowcaseCatalog.Get(options.ScreenNumber);
        var sequence = _sequence++;
        var fields = new Dictionary<string, object?>
        {
            ["schema_version"] = "1.0.0",
            ["upstream_schema_version"] = "test-jsonl-v1",
            ["sequence"] = sequence,
            ["seq"] = sequence,
            ["run_id"] = ResolveRunId(options),
            ["seed"] = options.DeterministicSeed ?? 0UL,
            ["screen_mode"] = ScreenModeLabel(options.ScreenMode),
            ["event"] = eventName,
            ["timestamp_utc"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["step_index"] = stepIndex,
            ["frame"] = frame,
            ["screen_number"] = screen.Number,
            ["screen_slug"] = screen.Slug,
            ["screen_title"] = screen.Title,
            ["width"] = options.Width,
            ["height"] = options.Height,
            ["changed_cells"] = stats.ChangedCells,
            ["dirty_rows"] = stats.DirtyRows,
            ["bytes_emitted"] = stats.BytesEmitted,
            ["frame_duration_ms"] = stats.FrameDurationMs,
            ["present_duration_ms"] = stats.PresentDurationMs,
            ["diff_duration_ms"] = stats.DiffDurationMs,
            ["degradation_level"] = stats.DegradationLevel,
            ["load_governor_action"] = stats.LoadGovernorAction,
            ["load_governor_reason"] = stats.LoadGovernorReason,
            ["load_governor_pid_output"] = stats.LoadGovernorPidOutput,
            ["load_governor_pid_p"] = stats.LoadGovernorPidP,
            ["load_governor_pid_i"] = stats.LoadGovernorPidI,
            ["load_governor_pid_d"] = stats.LoadGovernorPidD,
            ["load_governor_e_value"] = stats.LoadGovernorEProcessValue,
            ["load_governor_eprocess_sigma_ms"] = stats.LoadGovernorEProcessSigmaMs,
            ["load_governor_frames_observed"] = stats.LoadGovernorFramesObserved,
            ["load_governor_frames_since_change"] = stats.LoadGovernorFramesSinceChange,
            ["load_governor_pid_gate_threshold"] = stats.LoadGovernorPidGateThreshold,
            ["load_governor_pid_gate_margin"] = stats.LoadGovernorPidGateMargin,
            ["load_governor_evidence_threshold"] = stats.LoadGovernorEvidenceThreshold,
            ["load_governor_evidence_margin"] = stats.LoadGovernorEvidenceMargin,
            ["load_governor_in_warmup"] = stats.LoadGovernorEProcessInWarmup,
            ["load_governor_transition_seq"] = stats.LoadGovernorTransitionSeq,
            ["load_governor_transition_correlation_id"] = stats.LoadGovernorTransitionCorrelationId,
            ["runtime_mode"] = stats.RuntimeMode,
            ["runtime_mode_before"] = stats.RuntimeModeBefore,
            ["pressure_class"] = stats.RuntimePressureClass,
            ["work_disposition"] = stats.RuntimeWorkDisposition,
            ["governor_reason"] = stats.RuntimeGovernorReason,
            ["governor_transition"] = stats.RuntimeGovernorTransition,
            ["strict_semantics_preserved"] = stats.RuntimeStrictSemanticsPreserved,
            ["queue_in_flight"] = stats.RuntimeQueueInFlight,
            ["queue_max_depth"] = stats.RuntimeQueueMaxDepth,
            ["queue_dropped_delta"] = stats.RuntimeQueueDroppedDelta,
            ["resize_coalescing_active"] = stats.RuntimeResizeCoalescingActive,
            ["recovery_intervals_observed"] = stats.RuntimeRecoveryIntervalsObserved,
            ["recovery_intervals_required"] = stats.RuntimeRecoveryIntervalsRequired,
            ["deferred_work_total"] = stats.RuntimeDeferredWorkTotal,
            ["coalesced_work_total"] = stats.RuntimeCoalescedWorkTotal,
            ["dropped_work_total"] = stats.RuntimeDroppedWorkTotal,
            ["cascade_decision"] = stats.CascadeDecision,
            ["cascade_level_before"] = stats.CascadeLevelBefore,
            ["cascade_level_after"] = stats.CascadeLevelAfter,
            ["cascade_guard_state"] = stats.CascadeGuardState,
            ["conformal_bucket"] = stats.ConformalBucketKey,
            ["conformal_upper_us"] = stats.ConformalUpperMicroseconds,
            ["conformal_budget_us"] = stats.ConformalBudgetMicroseconds,
            ["conformal_calibration_size"] = stats.ConformalCalibrationSize,
            ["conformal_fallback_level"] = stats.ConformalFallbackLevel,
            ["conformal_interval_width_us"] = stats.ConformalIntervalWidthMicroseconds,
            ["cascade_recovery_streak"] = stats.CascadeRecoveryStreak,
            ["cascade_recovery_threshold"] = stats.CascadeRecoveryThreshold
        };

        if (extra is not null)
        {
            foreach (var item in extra)
            {
                fields[item.Key] = item.Value;
            }
        }

        _writer.WriteLine(JsonSerializer.Serialize(fields, JsonOptions));
        _writer.Flush();
    }

    public void Dispose() => _writer.Dispose();

    private static string ResolveRunId(ShowcaseCliOptions options)
    {
        if (options.VfxHarness.Enabled && !string.IsNullOrWhiteSpace(options.VfxHarness.RunId))
        {
            return options.VfxHarness.RunId;
        }

        if (options.MermaidHarness.Enabled && !string.IsNullOrWhiteSpace(options.MermaidHarness.RunId))
        {
            return options.MermaidHarness.RunId;
        }

        return "frankentui-net-showcase";
    }

    private static string ScreenModeLabel(ShowcaseScreenMode mode) =>
        mode switch
        {
            ShowcaseScreenMode.Inline => "inline",
            ShowcaseScreenMode.InlineAuto => "inline-auto",
            _ => "alt"
        };

    private static string ScreenCategoryLabel(ShowcaseScreenCategory category) =>
        category.ToString().ToLowerInvariant();

    private static int ResolveScreenInitEffectCount(ShowcaseScreen screen) =>
        screen.Number switch
        {
            18 => ShowcaseVfxEffects.AllCanonicalKeys.Length,
            45 => 1,
            _ => 0
        };
}
