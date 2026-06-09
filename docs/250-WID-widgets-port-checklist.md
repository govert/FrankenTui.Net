# Widgets Porting Checklist — FINAL

Generated 2026-05-30. Updated 2026-06-01.
Upstream: `.external/frankentui/crates/ftui-widgets/src/`

## Summary

| Category | Count | Status |
|----------|:---:|--------|
| Top-level .rs files | 55 | **55/55 ✓** |
| Top-level correspond to Runtime data structs | 4 | Referenced from Runtime |
| C# source files in `FrankenTui.Widgets/` | 33 | — |
| Total C# lines | 4,144 | — |
| Subdirectories (command_palette, focus, modal) | 3 | **Not yet ported** (21,286L) |

All 3 key projects build with 0 errors:
- `FrankenTui.Render` — 0 errors
- `FrankenTui.Runtime` — 0 errors
- `FrankenTui.Widgets` — 0 errors

## Core Traits & Helpers

- [x] `lib.rs` → `WidgetCore.cs` — IWidget, IStatefulWidget, Budgeted, WidgetDrawing, WidgetStyle, Borders flags
- [x] `stateful.rs` → `Stateful.cs` — StateKey, IStateful<T>, VersionedState<T>
- [x] `measurable.rs` → `Measurable.cs` — IMeasurableWidget, SizeConstraints
- [x] `borders.rs` → `Borders.cs` — BorderSet, BorderType
- [x] `mouse.rs` → `Mouse.cs` — MouseResult enum
- [x] `state.rs` → `Stateful.cs` — (re-export, merged)
- [x] `cached.rs` → `Cached.cs` — CachedWidget wrapper
- [x] `undo_support.rs` → `InfraWidgets.cs` — UndoSupport<T>, TextEditOperation
- [x] `measure_cache.rs` → `InfraWidgets.cs` — MeasureCache, WidgetId
- [x] `choreography.rs` → `InfraWidgets.cs` — Choreography, Action
- [x] `layout_debugger.rs` → `InfraWidgets.cs` — LayoutDebugger

## Layout Widgets

- [x] `align.rs` → `Align.cs` — Alignment container with h/v positioning
- [x] `columns.rs` → `Columns.cs` — Horizontal column layout with Flex
- [x] `constraint_overlay.rs` → `AdvancedWidgets.cs` — ConstraintOverlay
- [x] `layout.rs` → `AdvancedWidgets.cs` — LayoutWidget grid container
- [x] `padding.rs` → `LayoutWidgets.cs` — Padding wrapper (merged)
- [x] `group.rs` → `LayoutWidgets.cs` — Group with title (merged)

## Display Widgets

- [x] `block.rs` → `Block.cs` — Block with borders, title, padding
- [x] `paragraph.rs` → `Paragraph.cs` — Multi-line styled text
- [x] `sparkline.rs` → `Sparkline.cs` — 8-level trend chart with gradient
- [x] `progress.rs` → `Progress.cs` — Progress bar with label
- [x] `spinner.rs` → `Spinner.cs` — Loading spinner with frames
- [x] `rule.rs` → `Rule.cs` — Horizontal/vertical separator
- [x] `badge.rs` → `LayoutWidgets.cs` — Small badge label (merged)
- [x] `emoji.rs` → `DisplayWidgets.cs` — Emoji renderer (merged)
- [x] `pretty.rs` → `DisplayWidgets.cs` — Pretty-print wrapper (merged)
- [x] `debug_overlay.rs` → `AdvancedWidgets.cs` — DebugOverlay (merged)
- [x] `drift_visualization.rs` → `AdvancedWidgets.cs` — DriftVisualization (merged)
- [x] `voi_debug_overlay.rs` → `AdvancedWidgets.cs` — VoiDebugOverlay (merged)
- [x] `height_predictor.rs` → `AdvancedWidgets.cs` — HeightPredictor (merged)

## Status & Timer Widgets

- [x] `status_line.rs` → `StatusWidget.cs` — StatusLine with left/center/right (merged)
- [x] `stopwatch.rs` → `StatusWidget.cs` — Stopwatch with formats (merged)
- [x] `timer.rs` → `StatusWidget.cs` — Countdown timer (merged)
- [x] `toast.rs` → `Toast.cs` — Toast notification popup

## List & Scroll

- [x] `list.rs` → `List.cs` — Selectable list with filter, state
- [x] `scrollbar.rs` → `Scrollbar.cs` — Scrollbar with thumb/track
- [x] `table.rs` → `Table.cs` — Data table with rows, columns
- [x] `log_ring.rs` → `InfraWidgets.cs` — LogRing<T> ring buffer (merged)
- [x] `log_viewer.rs` → `LogViewer.cs` — Scrolling log viewer with search/filter
- [x] `virtualized.rs` → `Virtualized.cs` — Virtualized<T> content container

## Tree & Input

- [x] `tree.rs` → `Tree.cs` — Expandable tree with guides, search, keyboard/mouse
- [x] `input.rs` → `Input.cs` — Single-line text input, grapheme-aware
- [x] `textarea.rs` → `TextArea.cs` — Multi-line editor with scroll
- [x] `tabs.rs` → `Tabs.cs` — Tab bar navigation
- [x] `drag.rs` → `AdvancedWidgets.cs` — DragWidget (merged)
- [x] `keyboard_drag.rs` → `AdvancedWidgets.cs` — KeyboardDragWidget (merged)
- [x] `error_boundary.rs` → `InfraWidgets.cs` — ErrorBoundary<W> (merged)

## Container Widgets

- [x] `panel.rs` → `ContainerWidgets.cs` — Panel bordered container (merged)
- [x] `popover.rs` → `ContainerWidgets.cs` — Popover positioning overlay (merged)
- [x] `paginator.rs` → `ContainerWidgets.cs` — Paginator paging control (merged)
- [x] `notification_queue.rs` → `InfraWidgets.cs` — NotificationQueue (merged)

## Data Display

- [x] `json_view.rs` → `InfraWidgets.cs` — JsonView (merged)
- [x] `diagnostics.rs` → `DisplayWidgets.cs` — DiagnosticLog<T> (merged)
- [x] `decision_card.rs` → `DisplayWidgets.cs` — DecisionCard (merged)
- [x] `validation_error.rs` → `DisplayWidgets.cs` — ValidationErrorDisplay (merged)
- [x] `history_panel.rs` → `AdvancedWidgets.cs` — HistoryPanel (merged)
- [x] `file_picker.rs` → `AdvancedWidgets.cs` — FilePicker (merged)

## Help & Hint System

- [x] `help.rs` → `Help.cs` — Help widget, Short/Full modes, search
- [x] `help_index.rs` → `AdvancedWidgets.cs` — HelpIndex (merged)
- [x] `help_registry.rs` → `AdvancedWidgets.cs` — HelpRegistry (merged)
- [x] `hint_ranker.rs` → `InfraWidgets.cs` — HintRanker (merged)
- [x] `inspector.rs` → `Inspector.cs` — InspectorOverlay, bounds/hits/detail

## Misc

- [x] `list_helper.rs` — (2L re-export, merged)
- [x] `adaptive_radix.rs` — (already in FrankenTui.Runtime)
- [x] `elias_fano.rs` — (already in FrankenTui.Runtime)
- [x] `fenwick.rs` — (already in FrankenTui.Runtime)
- [x] `louds.rs` — (already in FrankenTui.Runtime)

## Subdirectories (NOT YET PORTED)

### command_palette/ (7,197L)
- [ ] `command_palette/mod.rs` (3271L)
- [ ] `command_palette/scorer.rs` (3709L)
- [ ] `command_palette/property_tests.rs` (213L)
- [ ] `command_palette/palette.rs` (2L)
- [ ] `command_palette/rank_confidence.rs` (2L)

### focus/ (3,558L)
- [ ] `focus/manager.rs` (1969L)
- [ ] `focus/graph.rs` (861L)
- [ ] `focus/spatial.rs` (494L)
- [ ] `focus/indicator.rs` (221L)
- [ ] `focus/mod.rs` (13L)

### modal/ (10,531L)
- [ ] `modal/focus_integration.rs` (3262L)
- [ ] `modal/stack.rs` (2410L)
- [ ] `modal/dialog.rs` (2146L)
- [ ] `modal/animation.rs` (1648L)
- [ ] `modal/container.rs` (995L)
- [ ] `modal/mod.rs` (70L)
