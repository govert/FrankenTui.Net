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
        var action = ClassifyMouseAction(gesture, before, after);
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
        fields["hit_id"] = ResolveLocalMouseHitId(gesture, before, after);
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
            return "palette_mouse";
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

        if (before.CurrentScreenNumber == 1 &&
            !before.TourActive &&
            before.TourStartScreen != after.TourStartScreen)
        {
            return after.TourStartScreen > before.TourStartScreen
                ? "tour_landing_step_next"
                : "tour_landing_step_prev";
        }

        if (!before.TourActive && after.TourActive)
        {
            return "overlay_tour_start";
        }

        if (before.TourActive && !after.TourActive)
        {
            return "overlay_tour_click";
        }

        if (before.CurrentScreenNumber == 2 && after.CurrentScreenNumber != before.CurrentScreenNumber)
        {
            return "pane_link_switch_screen";
        }

        if (before.CurrentScreenNumber != after.CurrentScreenNumber)
        {
            if (gesture.Kind == TerminalMouseKind.Scroll && gesture.Row == 1)
            {
                return gesture.Button == TerminalMouseButton.WheelUp
                    ? "scroll_prev_tab"
                    : "scroll_next_tab";
            }

            return gesture.Row == 0 ? "category_switch_screen" : "switch_screen";
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
