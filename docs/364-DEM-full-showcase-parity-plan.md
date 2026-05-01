# 364-DEM Full Showcase Parity Plan

## Purpose

This document records the follow-on plan required to move FrankenTui.Net from
the current `362-DEM` baseline to full practical parity with upstream
`ftui-demo-showcase`.

The current local state is:

- terminal app shell parity exists at a coarse level: upstream-shaped screen
  catalog, guided-tour entrypoint, scripted screen selection, and interactive
  navigation
- shared hosted-parity and extras surfaces remain the underlying reusable local
  rendering/test substrate
- many individual showcase screens still use local approximations, reduced
  state models, or placeholder composites rather than direct screen-for-screen
  ports
- `tools/FrankenTui.ShowcaseCompare` now provides the automated app-snapshot
  evidence lane documented in
  [`365-DEM-showcase-comparison-harness.md`](./365-DEM-showcase-comparison-harness.md)

This plan closes that gap explicitly instead of allowing the current shell-level
baseline to be mistaken for full demo parity.

Use this with:

- [`200-PRT-port-work-breakdown.md`](./200-PRT-port-work-breakdown.md)
- [`210-STS-port-status.md`](./210-STS-port-status.md)
- [`242-MAP-upstream-sync-workflow.md`](./242-MAP-upstream-sync-workflow.md)
- [`304-RTM-determinism-and-evidence.md`](./304-RTM-determinism-and-evidence.md)
- [`335-HST-host-divergence-ledger.md`](./335-HST-host-divergence-ledger.md)
- [`357-VRF-shared-sample-comparison-scaffold.md`](./357-VRF-shared-sample-comparison-scaffold.md)

## Exit Condition

`364-DEM` is complete only when all of the following are true:

- the .NET terminal demo uses an upstream-shaped app model rather than a local
  synthetic shell over placeholder panels
- all in-scope upstream showcase screens exist locally with materially matching
  visible structure, state, and interactive behavior
- app-level chrome and overlay behavior matches upstream closely enough that
  parity evidence can be collected screen-for-screen
- the terminal and wasm/web showcase runners exercise the same screen registry,
  navigation model, and scripted scenarios
- verification includes direct showcase screen comparison against the managed
  upstream workspace instead of only shared micro-samples
- Windows Terminal evidence exists for the same representative demo contract,
  not just reduced console-host checks

## Definition Of Full Parity

For this workstream, “full parity” does not mean “bit-identical every frame on
every host.” It means:

1. the same practical user-facing demo surface exists
2. the same screen catalog and entrypoints exist
3. the same important demo-specific state machines exist
4. the same scripted and interactive behaviors are exercised by verification
5. any remaining divergence is narrow, named, and documented

The burden of proof stays on FrankenTui.Net to show why any remaining mismatch
must exist due to runtime or host constraints.

## Current Gap Summary

Relative to upstream `ftui-demo-showcase`, the current local baseline is still
partial in these ways:

- CLI/env coverage is narrower than upstream
- app chrome and overlays are simplified
- many screens are placeholders or merged composites instead of direct ports
- several upstream screen families still depend on missing lower-layer widget,
  interaction, diagnostics, or visualization depth
- host-size bootstrap now follows the upstream contract for normal showcase
  runs: the .NET host resolves the initial viewport from the backend terminal
  size unless explicit CLI dimensions or VFX/Mermaid harness sizing force a
  viewport; broader resize/chrome evidence remains part of the control-plane
  parity work
- shared sample comparison does not yet run a real showcase-to-showcase lane
- web/wasm alignment still uses shared primitives rather than one shared
  showcase program model

## 364-DEM-A Basis Inventory

Basis:

- upstream workspace commit:
  `40c98246f27f9d174b3923c8df841ba325247dd4`
- upstream screen registry:
  `.external/frankentui/crates/ftui-demo-showcase/src/screens/mod.rs`
- upstream app model/chrome/control-plane sources:
  `.external/frankentui/crates/ftui-demo-showcase/src/app.rs`,
  `.external/frankentui/crates/ftui-demo-showcase/src/chrome.rs`,
  `.external/frankentui/crates/ftui-demo-showcase/src/tour.rs`,
  `.external/frankentui/crates/ftui-demo-showcase/src/cli.rs`
- current local registry:
  `apps/FrankenTui.Demo.Showcase/ShowcaseCatalog.cs`
- current local shell/model:
  `apps/FrankenTui.Demo.Showcase/Program.cs`,
  `apps/FrankenTui.Demo.Showcase/ShowcaseInteractiveProgram.cs`,
  `apps/FrankenTui.Demo.Showcase/ShowcaseSurface.cs`,
  `apps/FrankenTui.Demo.Showcase/ShowcaseViewFactory.cs`

The current local catalog carries the same 45 screen ids, order, slugs, titles,
short labels, categories, and blurbs as the upstream registry. This is registry
parity only. Most screen bodies remain reduced local compositions in
`ShowcaseSurface.cs`, not full ports of the corresponding upstream screen
state machines.

### Screen Parity Ledger

| # | Upstream screen | Slug | Upstream source | Category | Current local owner | Current local status |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Guided Tour | `guided_tour` | `screens/mod.rs`, `tour.rs`, `app.rs` | Tour | `ShowcaseSurface.BuildGuidedTour` | partial: landing/tour shell only |
| 2 | Dashboard | `dashboard` | `screens/dashboard.rs` | Tour | `ShowcaseSurface.BuildDashboard` | partial: overview panels over local session |
| 3 | Shakespeare | `shakespeare` | `screens/shakespeare.rs` | Text | `ShowcaseSurface.BuildShakespeare` | partial: text/search approximation |
| 4 | Code Explorer | `code_explorer` | `screens/code_explorer.rs` | Text | `ShowcaseSurface.BuildCodeExplorer` | partial: static code/pane approximation |
| 5 | Widget Gallery | `widget_gallery` | `screens/widget_gallery.rs` | Core | `ShowcaseSurface.BuildWidgetGallery` | partial: local widget inventory gallery |
| 6 | Layout Lab | `layout_lab` | `screens/layout_lab.rs`, `pane_interaction.rs` | Interaction | `ShowcaseSurface.BuildLayoutLab` | partial: local pane workspace, no full persistence/control parity |
| 7 | Forms & Input | `forms_input` | `screens/forms_input.rs` | Interaction | `ShowcaseSurface.BuildFormsInput` | partial: local forms sample |
| 8 | Data Viz | `data_viz` | `screens/data_viz.rs` | Visuals | `ShowcaseSurface.BuildDataViz` | partial: local chart/composite sample |
| 9 | File Browser | `file_browser` | `screens/file_browser.rs` | Interaction | `ShowcaseSurface.BuildFileBrowser` | partial: static tree/preview approximation |
| 10 | Advanced | `advanced_features` | `screens/advanced_features.rs` | Core | `ShowcaseSurface.BuildAdvancedFeatures` | partial: local advanced-pattern panel |
| 11 | Table Theme Gallery | `table_theme_gallery` | `screens/table_theme_gallery.rs` | Visuals | `ShowcaseSurface.BuildTableThemeGallery` | partial: local theme/table sample |
| 12 | Terminal Capabilities | `terminal_capabilities` | `screens/terminal_capabilities.rs` | Systems | `ShowcaseSurface.BuildTerminalCapabilities` | partial: local matrix/evidence/simulation tri-panel with deterministic capability rows, evidence-source labels, diagnostic event names, profile controls, and environment summary; exact probing, input-driven mode switching, and report export remain open |
| 13 | Macro Recorder | `macro_recorder` | `screens/macro_recorder.rs` | Interaction | `ShowcaseSurface.BuildMacroRecorder` | partial: local controls/timeline/event-detail/scenario-runner layout with deterministic preset macro rows and upstream-shaped key labels; live recorder focus routing, mouse focus, playback queue, and exact tracing remain open |
| 14 | Performance | `performance` | `screens/performance.rs` | Systems | `ShowcaseSurface.BuildPerformance` | partial: local virtualized 10k-item log list, performance stats panel, visible-row counts, and navigation status; live keyboard/mouse list mutation and exact upstream styling remain open |
| 15 | Markdown | `markdown_rich_text` | `screens/markdown_rich_text.rs` | Text | `ShowcaseSurface.BuildMarkdown` | partial: local three-column markdown renderer, LLM streaming/detection panel, style sampler, Unicode table, and wrap/alignment demo; animated backdrop, live scroll/focus controls, exact markdown detection/cache behavior, and full syntax/theme depth remain open |
| 16 | Mermaid Showcase | `mermaid_showcase` | `screens/mermaid_showcase.rs` | Visuals | `ShowcaseSurface.BuildMermaid` | partial: local Mermaid library/viewport/controls/metrics/status-log surface now includes render mode, palette, guard, zoom/pan, viewport override, crossings/symmetry/compactness, and `mermaid_render` status evidence; full upstream IR renderer, search/inspect modes, debug overlays, node navigation, minimap, palette rendering, exact JSONL schema, and full mouse/key control parity remain open |
| 17 | Mermaid Mega Showcase | `mermaid_mega_showcase` | `screens/mermaid_mega_showcase.rs` | Visuals | `ShowcaseSurface.BuildMermaidMega` | partial: local Mega surface now combines the shared Mermaid viewport with sample-library metadata, filter/category summary, controls/keymap panel, node-detail summary, and `mermaid_mega_recompute` metrics evidence; full upstream generated sample corpus, interactive node navigation, edge highlighting, search/inspect modes, minimap, debounced recompute cache, palette rendering, and exact JSONL/keymap parity remain open |
| 18 | Visual Effects | `visual_effects` | `screens/visual_effects.rs` | Visuals | `ShowcaseSurface.BuildVisualEffects` | partial: now renders a deterministic frame-driven Braille canvas through local canvas primitives, switches local patterns from every current upstream harness effect key, and surfaces normalized effect label/description/renderer metadata; rich upstream effect implementations, real Quake rasterization, and golden output remain open |
| 19 | Responsive Layout | `responsive_demo` | `screens/responsive_demo.rs` | Core | `ShowcaseSurface.BuildResponsive` | partial: local breakpoint indicator, default/custom-threshold notes, sidebar/content/aside adaptive layout, visibility rules, responsive values, and simulated-width controls are visible; live resize/mouse/key mutation, actual `ResponsiveLayout` primitive parity, breakpoint coloring, and exact custom-breakpoint state remain open |
| 20 | Log Search | `log_search` | `screens/log_search.rs` | Text | `ShowcaseSurface.BuildLogSearch` | partial: local log-search viewport now sits beside upstream-shaped live-stream limits, search/filter key controls, deterministic env flags, and diagnostic event/field table; real streaming append/follow mode, inline search/filter state machine, case/context toggles, and exact diagnostic JSONL/hook plumbing remain open |
| 21 | Notifications | `notifications` | `screens/notifications.rs` | Interaction | `ShowcaseSurface.BuildNotifications` | partial: local notification surface now shows upstream-shaped trigger instructions, max-visible/max-queued queue config, TopRight stack, toast priorities/styles/actions, lifecycle states, mouse affordances, and tick expiry/promotion notes; real `NotificationQueue` widget model, auto-dismiss timers, action invocation state, mouse hit regions, and exact toast rendering remain open |
| 22 | Action Timeline | `action_timeline` | `screens/action_timeline.rs` | Systems | `ShowcaseSurface.BuildActionTimeline` | partial: local event-stream surface now shows filters/follow controls, deterministic event timeline rows, selected event details, field/evidence payloads, max-event/burst constraints, diagnostic span names, and navigation/mouse affordances; real bounded ring-buffer state, input recording hooks, filter cycling, follow-drop decisions, mouse hit regions, and tracing integration remain open |
| 23 | Intrinsic Sizing | `intrinsic_sizing` | `screens/intrinsic_sizing.rs` | Core | `ShowcaseSurface.BuildIntrinsicSizing` | partial: local intrinsic-sizing surface now shows the four upstream scenarios, effective-width breakpoints, adaptive-sidebar/card/table/form rules, width override controls, mouse cycle affordances, and embedded pane-studio notes; real content-aware measurement primitives, live scenario switching, width override state, embedded pane workspace interaction, and exact responsive rendering remain open |
| 24 | Layout Inspector | `layout_inspector` | `screens/layout_inspector.rs` | Core | `ShowcaseSurface.BuildLayoutInspector` | partial: local inspector now shows upstream-shaped scenarios, solver steps, overlay/tree toggles, constraint-vs-rect records, overflow/underflow statuses, mouse/key affordances, and pane-studio notes; real `ConstraintOverlay`/`LayoutDebugger` widget parity, live scenario/step state, embedded pane interaction, hit-region routing, and exact styled rect visualization remain open |
| 25 | Advanced Text Editor | `advanced_text_editor` | `screens/advanced_text_editor.rs` | Text | `ShowcaseSurface.BuildAdvancedTextEditor` | partial: local editor surface now shows sample multiline editor content, line/cursor/match status, search/replace controls, undo/redo history panel, focus-cycle controls, text-editor diagnostic env flags, JSONL field labels, and diagnostic event names; real `AdvancedTextEditor` state machine, line-number/selection rendering, search navigation, replace/undo/redo mutation, focus hit regions, and exact diagnostics plumbing remain open |
| 26 | Mouse Playground | `mouse_playground` | `screens/mouse_playground.rs` | Interaction | `ShowcaseSurface.BuildMousePlayground` | partial: local mouse surface now shows upstream-shaped 4x3 hit-target grid, hit IDs, hover/click state, event log kinds, stats/overlay state, keyboard controls, diagnostic env flags, JSONL field labels, and telemetry hook names; real SGR mouse decoding, frame hit registry integration, hover stabilizer state, target click mutation, overlay rendering, jitter stats, and exact diagnostic hook plumbing remain open |
| 27 | Form Validation | `form_validation` | `screens/form_validation.rs` | Interaction | `ShowcaseSurface.BuildFormValidation` | partial: local form-validation surface now shows upstream-shaped nine-field registration form, real-time/on-submit mode/status, touched/dirty state, error summary, validator inventory, toast queue config, mouse affordances, and diagnostic event names; real `Form`/`FormState` mutation, cross-field password matching, submit-mode error clearing, notification queue state, mouse hit regions, and exact form rendering remain open |
| 28 | Virtualized Search | `virtualized_search` | `screens/virtualized_search.rs` | Systems | `ShowcaseSurface.BuildVirtualizedSearch` | partial: local virtualized-search surface now follows the upstream search-bar plus 70/30 results/stats layout with 10k-item dataset metadata, deterministic filtered results, match positions/scores, keybinding inventory, diagnostic env flags, JSONL field labels, and telemetry hook names; real `VirtualizedSearch` state, fzy-style incremental filtering/highlighting, keyboard/mouse navigation, viewport synchronization, diagnostic log storage, and exact render styling remain open |
| 29 | Async Tasks | `async_tasks` | `screens/async_tasks.rs` | Systems | `ShowcaseSurface.BuildAsyncTasks` | partial: local async-task surface now follows the upstream header/task-queue/details/activity/help layout with task lifecycle states, scheduler policy cycle, aging/fairness formula, hazard cancellation fields, metrics/invariant names, mouse/key affordances, and diagnostic JSONL event names; real `AsyncTaskManager` state mutation, queueing-policy scheduler, progress bars, hazard auto-cancel decisions, diagnostic log storage, mouse hit regions, and exact upstream styling remain open |
| 30 | Theme Studio | `theme_studio` | `screens/theme_studio.rs` | Visuals | `ShowcaseSurface.BuildThemeStudio` | partial: local theme-studio surface now follows the upstream presets/token-inspector/status layout with preset names, token categories, hex values, contrast/WCAG ratings, JSON and Ghostty export fields, focus/navigation/mouse affordances, diagnostic env flags, JSONL field labels, and telemetry hook names; real global theme switching, token swatch rendering, contrast computation from live palette, export payload generation, mouse hit regions, diagnostic log storage, and exact upstream styling remain open |
| 31 | Time-Travel Studio | `snapshot_player` | `screens/snapshot_player.rs` | Visuals | `ShowcaseSurface.BuildSnapshotPlayer` | partial: local time-travel surface now follows the upstream timeline/preview/frame-info layout with frame metadata, playback/recording controls, marker/timeline mouse affordances, A/B compare rows, heatmap state, checksum/chain fields, JSONL report fields, diagnostic event names, and replay invariants; real `SnapshotPlayer` frame buffer storage, delta rendering, timeline scrubbing state, marker mutation, compare diff cache, heatmap overlay rendering, JSONL file export, diagnostic log storage, and exact upstream styling remain open |
| 32 | Performance Challenge | `performance_hud` | `screens/performance_hud.rs` | Systems | `ShowcaseSurface.BuildPerformanceChallenge` | partial: local performance challenge surface now follows the upstream title/metrics/sparkline/render-budget/status layout with latency percentiles, tick-sample metadata, stress harness state, degradation tier thresholds, forced-tier controls, budget/sparkline mouse affordances, JSONL tier-change fields, deterministic env notes, and keybinding labels; real `PerformanceHud` ring-buffer metrics, sparkline rendering, stress ramp/hold/cooldown state, tier-change logger integration, forced-tier mutation, budget adjustment, mouse hit regions, and exact upstream styling remain open |
| 33 | Explainability Cockpit | `explainability_cockpit` | `screens/explainability_cockpit.rs` | Systems | `ShowcaseSurface.BuildExplainability` | partial: local explainability cockpit surface now follows the upstream header/source, diff strategy, resize BOCPD, budget decision, timeline, and controls layout with deterministic diff/resize/budget evidence rows, JSONL event and field labels, refresh/pause/clear/focus/scroll keybindings, source env hints, and mouse/timeline affordance notes; real evidence-file loading, JSONL parsing, auto-refresh, pause/clear state mutation, panel focus, timeline scroll, mouse hit regions, overlay mode, and exact upstream styling remain open |
| 34 | i18n Stress Lab | `i18n_demo` | `screens/i18n_demo.rs` | Text | `ShowcaseSurface.BuildI18n` | partial: local i18n surface now follows the upstream locale bar plus String Lookup, Pluralization Rules, RTL Layout Mirroring, and Stress Lab panels with locale coverage/fallback labels, plural-category examples, RTL/LTR mirroring samples, grapheme/display-width/truncation evidence, stress-report JSONL fields, and locale/panel/sample/export keybinding labels; real `StringCatalog`, plural-rule engine, grapheme cursor state, sample-set mutation, report file export, mouse locale/panel routing, RTL flow reversal, and exact upstream styling remain open |
| 35 | VOI Overlay | `voi_overlay` | `screens/voi_overlay.rs` | Systems | `ShowcaseSurface.BuildVoiOverlay` | partial: local VOI overlay surface now follows the upstream centered sampler-debug overlay shape with decision, posterior, observation, ledger, and controls sections, fallback/inline-auto source labels, VOI posterior/gain/cost/e-value fields, ledger decision/observation entries, focus/detail/ledger keybindings, and mouse hit-region notes; real `VoiSampler`, `inline_auto_voi_snapshot`, overlay widget rendering, focus/expanded state mutation, ledger selection, mouse hit testing, runtime observation updates, and exact upstream styling remain open |
| 36 | Inline Mode | `inline_mode_story` | `screens/inline_mode_story.rs` | Tour | `ShowcaseSurface.BuildInlineModeStory` | partial: local inline-mode surface now follows the upstream header plus inline/alt-screen comparison story with scrollback-preservation labels, anchor/UI-height/log-rate state, deterministic log rows, compare/alt-screen notes, stress limits, keybindings, and mouse hit-region labels; real live `InlineModeStory` log ring buffer, tick-driven stream growth, compare/mode/anchor/height/rate mutation, stress burst generation, layout hit testing, inline writer behavior, and exact upstream styling remain open |
| 37 | Accessibility | `accessibility_panel` | `screens/accessibility_panel.rs` | Systems | `ShowcaseSurface.BuildAccessibility` | partial: local accessibility surface now follows the upstream overview, toggles, WCAG contrast, live preview, and telemetry layout with high-contrast/reduced-motion/large-text state labels, WCAG threshold rows, preview styling labels, app-level toggle action names, telemetry event fields, keybindings, and mouse hit-row notes; real app-level `A11ySettings` sync, contrast computation from live palette, theme large-text style application, telemetry event ring buffer, mouse toggle dispatch, overlay integration, and exact upstream styling remain open |
| 38 | Widget Builder | `widget_builder` | `screens/widget_builder.rs` | Core | `ShowcaseSurface.BuildWidgetBuilder` | partial: local widget-builder surface now follows the upstream sandbox header plus presets, widget tree, live preview, props, export, and mouse-hint layout with preset names, widget-kind ids, editable prop labels, JSONL export fields, snapshot schema labels, keyboard shortcuts, and mouse routing notes; real `WidgetBuilder` preset state, widget config mutation, live widget rendering, save/export file IO, props hash generation, list selection state, mouse hit testing, and exact upstream styling remain open |
| 39 | Command Palette Evidence Lab | `command_palette_lab` | `screens/command_palette_lab.rs` | Interaction | `ShowcaseSurface.BuildCommandPaletteLab` | materially aligned: local screen now uses the upstream 12-item `cmd:*` sample action set, two-line header, 55/45 palette/evidence columns, selected-result summary, compact evidence ledger, split bench/hint footer, screen-local `0`-`5`/`m` match-filter controls, `b`-toggled three-tick deterministic sample-query bench loop, palette-area wheel/click handling, local HintRanker-style EU/net/VOI evidence, and frame/mouse evidence; exact styling remains local |
| 40 | Determinism Lab | `determinism_lab` | `screens/determinism_lab.rs`, `determinism.rs` | Systems | `ShowcaseSurface.BuildDeterminismLab` | partial: local determinism-lab surface now follows the upstream header plus equivalence, scene preview, checks, and report/env layout with Full/DirtyRows/FullRedraw strategy rows, checksum timeline, mismatch fields, scenario/run labels, JSONL export fields, deterministic env keys, hash-key format, FNV checksum notes, keybindings, and mouse hit-region labels; real buffer generation, diff application, checksum comparison, scenario simulation, run history/details scrolling, JSONL file export, hit-region registration, and exact upstream styling remain open |
| 41 | Hyperlink Playground | `hyperlink_playground` | `screens/hyperlink_playground.rs` | Interaction | `ShowcaseSurface.BuildHyperlinkPlayground` | partial: local hyperlink surface now follows the upstream header plus OSC-8 links, details/registry, and controls/JSONL layout with upstream link labels/URLs, LinkRegistry and HitRegion labels, OSC-8 open/close evidence, hover/action state labels, keybindings, mouse action labels, logging env vars, JSONL fields, action names, and hit-id base; real `LinkRegistry` rendering, hit-region registration, hover/click/focus state, keyboard activation/copy mutation, JSONL file output, and exact upstream styling remain open |
| 42 | Kanban Board | `kanban_board` | `screens/kanban_board.rs` | Interaction | `ShowcaseSurface.BuildKanbanBoard` | partial: local Kanban now renders the upstream-shaped app chrome and deterministic 80x24 board frame exactly under `tools/FrankenTui.ShowcaseCompare --screens 42`, including Todo/In Progress/Done seed cards, focused heavy border, compact footer, and app status line; keyboard focus movement, card moves, undo, redo, history, redo stack, and rendered card positions mutate through `ShowcaseKanbanState`; mouse drag/drop routing, card dimming/drop preview styling, cached hit-region registration, and wider viewport/interactive snapshot evidence remain open |
| 43 | Live Markdown Editor | `markdown_live_editor` | `screens/markdown_live_editor.rs` | Text | `ShowcaseSurface.BuildMarkdownLiveEditor` | partial: local live-markdown surface now follows the upstream search/editor/preview layout with upstream sample markdown, search query/match labels, focus modes, TextArea line-number/soft-wrap status, MarkdownRenderer preview, raw-vs-rendered width diff, preview scroll state, MarkdownTheme/SyntaxHighlighter notes, keyboard and mouse controls, cached layout-rect notes, and JSONL action fields; real mutable `MarkdownLiveEditor` state, rope editing, search selection, focus transitions, preview scrolling, diff recomputation, mouse pane routing, syntax-highlighted Markdown rendering, and exact upstream styling remain open |
| 44 | Drag & Drop Lab | `drag_drop` | `screens/drag_drop.rs` | Interaction | `ShowcaseSurface.BuildDragDrop` | partial: local drag/drop surface now follows the upstream three-mode layout with Sortable List, Cross-Container, and Keyboard Drag tabs, deterministic Item/File lists, selected/focused list state, keyboard drag manager status, drop target/payload labels, announcements, sortable reorder and transfer controls, mouse click/scroll/right-click routing notes, small-terminal fallback text, cached layout-rect notes, and JSONL action fields; real `DragDropDemo` state mutation, `KeyboardDragManager` integration, `DropTargetInfo` navigation, list reorder/transfer behavior, announcements queue, mouse hit testing, and exact upstream styling remain open |
| 45 | Quake E1M1 (Easter Egg) | `quake_easter_egg` | `screens/quake.rs`, `screens/3d_data.rs` | Visuals | `ShowcaseSurface.BuildQuake` | partial: local Quake surface now renders the deterministic FPS Braille canvas plus upstream-shaped player/physics, controls, mesh-raster, quality-tier, palette, depth-buffer, small-terminal fallback, harness, JSONL, and divergence labels tied to `QUAKE_E1M1_VERTS`/`QUAKE_E1M1_TRIS`; real `QuakeE1M1State` physics, collision, mesh rasterization, clipping/depth pipeline, quality-tier output, fire flash, asset-derived geometry rendering, and exact upstream styling remain open |

### Control-Plane Contract Ledger

| Contract area | Upstream source | Upstream behavior at `40c98246` | Local status |
| --- | --- | --- | --- |
| screen mode | `cli.rs`, `main.rs` | `--screen-mode=alt|inline|inline-auto`; env override `FTUI_DEMO_SCREEN_MODE` | partial: CLI/env parser supports `alt`, `inline`, and `inline-auto`; full upstream runtime sizing behavior still pending |
| inline sizing | `cli.rs`, runtime setup | `--ui-height`, `--ui-min-height`, `--ui-max-height`; env overrides | partial: CLI/env parser supports height/min/max and clamps `inline-auto`; terminal layout integration still remains shallow |
| screen selection | `cli.rs`, `screens/mod.rs` | `--screen=N`; env override `FTUI_DEMO_SCREEN`; registry-derived numbering | partial: `--screen`, `--screen=N`, env override, and legacy `--scenario` map into the local 45-screen registry |
| guided tour | `cli.rs`, `tour.rs`, `app.rs` | `--tour`, `--tour-speed`, `--tour-start-step`; env overrides; pause/resume/step behavior | partial: launch/tick advancement, env/CLI seed controls, landing start/speed keyboard controls, mouse wheel/click landing controls, active-tour pause/resume, Left/Right or n/p step-index progression, speed controls, keyboard exit, active-tour mouse-overlay exit, and a local upstream-shaped storyboard/callout/highlight evidence baseline with local resolved highlight rectangles exist; exact upstream frame geometry, full hit-region registration, and exact overlay event stream remain pending |
| mouse policy | `cli.rs`, `app.rs`, `chrome.rs` | `--mouse=on|off|auto`, `--no-mouse`, status-bar toggle, chrome/screen hit routing | partial: CLI/env mouse policy now drives `TerminalSessionOptions`; F6/`m` and a local bottom status-row mouse zone toggle the demo mouse-capture state, evidence field, and backend SGR mouse-capture feature; local category-tab, visible screen-tab, tab-wheel chrome routing, and dashboard highlight pane-link routing are headless-covered; upstream frame hit registry, generalized pane hit routing, and full chrome/screen routing remain pending |
| pane workspace persistence | `cli.rs`, `app.rs`, `screens/layout_lab.rs` | `--pane-workspace=PATH`, `FTUI_DEMO_PANE_WORKSPACE`, save/ack/recovery/corpus behavior | partial: CLI/env parser carries the workspace path; showcase shell loads/saves a versioned local pane workspace envelope through canonical workspace JSON validation, migrates raw-v1 local snapshots, preserves invalid snapshots, records load/recovery/migration state in generic launch/frame evidence, emits local `pane_workspace_save` acknowledgment records with schema version, and surfaces restored/default/recovered status in Layout Lab; local corpus coverage now checks deterministic byte-stable workspace re-export, but broader upstream lifecycle/corpus behavior remains absent |
| deterministic auto-exit | `cli.rs`, `app.rs` | `FTUI_DEMO_EXIT_AFTER_MS`, `FTUI_DEMO_EXIT_AFTER_TICKS`, deterministic seed/tick envs | partial: parser supports `FTUI_DEMO_DETERMINISTIC`, `FTUI_DEMO_SEED`, `FTUI_DEMO_TICK_MS`, `FTUI_DEMO_EXIT_AFTER_MS`, `FTUI_DEMO_EXIT_AFTER_TICKS`, plus CLI overrides; generic interactive loop honors tick cadence and auto-exit, but screen-level deterministic fixture seeding remains pending |
| VFX harness | `cli.rs`, `screens/visual_effects.rs`, `tests/visual_effects_pty.rs` | `--vfx-*` flags and `FTUI_DEMO_VFX_*` envs for effect, size, seed, JSONL, perf | partial: CLI/env parser carries harness, effect, tick, frames, size, seed, JSONL, run id, perf, exit-after, local golden path, and golden-update controls, and centralized CLI help now advertises those controls; VFX effect parsing now recognizes the current upstream 19-key effect catalog plus aliases and canonicalizes known keys; enabled harnesses force the local Visual Effects screen, size, tick cadence, scripted frame count, and write deterministic local harness JSONL records with upstream-style `vfx_harness_start`, input/rendered-buffer checksums, upstream-style `vfx_frame` aliases, Doom/Quake `vfx_input` records for the upstream FPS input script, shared local `hash_key` fields, normalized effect label/description/renderer/canvas-mode/local-quality/FPS-effect metadata, and local `vfx_perf_frame`/`vfx_perf_summary` timing records; a local VFX golden registry can save/load/verify/update hash vectors and extract JSONL frame hashes, and scripted runs can enforce or update those local vectors; upstream VFX renderer/golden parity remains absent |
| Mermaid harness | `cli.rs`, Mermaid screens/tests | `--mermaid-*` flags and `FTUI_DEMO_MERMAID_*` envs for deterministic harness runs | partial: CLI/env parser carries harness, tick, size, seed, JSONL, and run id controls, and centralized CLI help now advertises those controls; enabled harnesses force the local Mermaid screen, size, tick cadence, and mouse-off policy, and write deterministic local harness JSONL records with upstream-style `mermaid_harness_start`, `mermaid_frame`, and `mermaid_harness_done` event names plus local hash/sample/timing/config/link aliases, launch `env`, and done `run_id` fields derived from `MermaidShowcaseSurface.BuildState`; upstream Mermaid recompute/render telemetry parity remains absent |
| diagnostics/evidence logs | `app.rs`, `test_logging.rs`, `main.rs` | screen-init, mouse, a11y, perf HUD, VFX, Mermaid, and evidence JSONL env-gated logs | partial: `FTUI_DEMO_EVIDENCE_JSONL` / `FTUI_HARNESS_EVIDENCE_JSONL` now write generic launch/screen-init/frame evidence JSONL locally, including pane-workspace load/recovery/migration/schema fields, startup `screen_init` records with `demo_screen_init` stream identity, screen id/category, init milliseconds, effect count, and memory-estimate fields, interactive tour/overlay/pane/debug state fields, tour callout/highlight fields, common upstream-style context aliases (`upstream_schema_version`, `seq`, `run_id`, `seed`, `screen_mode`), local `tour_event` records for tour state changes, local `palette_event` records for palette state changes, local `a11y_event` records with upstream-shaped `panel_toggle`, `high_contrast_toggle`, `reduced_motion_toggle`, and `large_text_toggle` names plus tick/screen/flag fields, local `perf_hud_event` records with upstream-shaped `hud_toggle` state/tick/screen fields plus visible-HUD `tick_stats` records every 60 ticks using deterministic local frame timing and visible-HUD `tick_stall` since-ms/tick/screen/reduced-motion fields, local `mouse_event` records with upstream-compatible `kind`/`x`/`y`/`hit_id`/`action`/`target_screen`/`current_screen` aliases plus richer local fields, and local `mouse_capture_toggle` records with upstream-compatible `state`/`mode`/`source`/`current_screen` fields; exact upstream lazy screen-init lifecycle, exact upstream A11y/perf HUD stream gating, upstream rolling perf sample window, exact upstream stall timing semantics, remaining upstream-specific mouse/VFX/Mermaid streams, and exact per-stream tour/palette/mouse JSONL schema/hit-id shapes are not matched |
| global shortcuts | `cli.rs`, `app.rs` | screen hotkeys, Tab/Shift+Tab, Shift+H/Shift+L, Ctrl+K/F/Shift+F, Ctrl+0..6 palette filters, `?`, Esc, A, Ctrl+T/P, Ctrl+I, F6, F12, undo/redo, quit | partial: local navigation/palette/help/tour subset; advertised upstream Ctrl+K command-palette toggle, local screen-ID command entries/favorites/filtering/execution, Ctrl+Shift+F favorites-only filtering, local command-category Ctrl+0/Ctrl+1..N palette filtering, Shift+H/Shift+L screen navigation, Ctrl+I evidence ledger toggle, Ctrl+P perf HUD toggle, F6/`m` mouse-capture toggle, F12 debug overlay toggle, Shift+A A11y panel toggle, palette/evidence/perf/debug/help/a11y Escape precedence, and matching local help text are covered; full overlay/shortcut precedence remains pending |
| overlay precedence | `app.rs`, `chrome.rs` | command palette, evidence ledger, perf HUD, debug, help, a11y, status, chrome, then screen | partial: local palette/evidence/perf/debug/help/a11y/status shell now dismisses command palette before lower overlays, then evidence, perf, debug, help, and A11y on Escape; command palette also suppresses local chrome and pane mouse routing; richer upstream evidence/perf/a11y/debug surfaces, exact hit registry, generalized pane routing, and full screen-routing precedence remain unported |
| wasm runner | `crates/ftui-showcase-wasm/src/runner_core.rs`, `wasm_runner.rs` | shared `AppModel` driven through WASM runner inputs/interruption events | partial: local `ShowcaseRunnerCore` now steps the shared showcase web rendering path, tracks frame/size/scenario state, accepts encoded scenario/control input, clamps resize dimensions, and emits pane interruption records for native-touch yield, context loss, and render stalls; full wasm-bindgen/AppModel runner parity and terminal/web unification remain pending |

### Current Upstream Drift Since Prior Review

The previous selective waves reviewed upstream through
`2d25a03dd453c4384287df2271dc8fdcf3247c06`. The refreshed basis
`40c98246f27f9d174b3923c8df841ba325247dd4` adds new in-scope demo/runtime
pressure that is not closed by the current local code:

| Upstream area | Representative commits | Local classification | Follow-on owner |
| --- | --- | --- | --- |
| runtime hardening and adaptive load control | `54373939` `feat(runtime): introduce LoadGovernorConfig for adaptive render degradation`, `0f844251` load-governor telemetry, timing/evidence hardening commits | partial: local runtime now has the upstream-shaped PID/e-process load-governor core plus richer degradation telemetry, generic showcase frame evidence, reusable operator HUD surfacing, doctor text/dashboard summaries, local conformal predictor/frame-guard/degradation-cascade baselines, upstream-shaped policy-config conversion feeding runtime defaults, render-frame conformal/cascade telemetry, degradation context propagation, `SKIP_FRAME` pre-render bypass, and lower-tier responses in core block/paragraph/progress/list/status/tabs/textarea/table/tree/scrollbar/inspector widgets; staged conformal variants and remaining full-catalog lower-tier widget degradation remain open | `300-RTM`, `304-RTM`, `364-DEM-E4` |
| pane workspace persistence and migration | `c38349a9`, `ba20e312`, `e2927070`, recovery/hardening commits | partial: local pane workspace has deterministic replay/JSON, demo-shell versioned envelope load/save, raw-v1 migration, invalid snapshot preservation, launch/frame evidence for recovery/migration/schema state, save acknowledgment evidence, and Layout Lab recovery status, but broader upstream corpus coverage remains absent | `280-LYT`, `315-WGT`, `364-DEM-B`, `364-DEM-E1` |
| visual effects canvas and VFX harness | `f886a800`, `4883171e`, VFX harness/perf/golden commits | partial/absent: local VFX screen now renders an effect-aware deterministic Braille canvas instead of static placeholder art; local deterministic VFX JSONL carries input/rendered-buffer checksums, upstream-style `vfx_frame` aliases, Doom/Quake `vfx_input` script records, normalized effect metadata, and local VFX perf records, local extras now carry `CanvasPixelRect`/Braille `RenderExcluding`, local effect parsing covers the current upstream 19-key catalog and aliases, the Quake screen now renders a deterministic FPS Braille canvas, and a local golden hash registry can extract, update, and compare frame hashes during scripted runs; rich upstream effect implementations, real Quake rasterization, upstream-equivalent renderer output, and enforced upstream golden fixtures remain absent | `370-EXT`, `350-VRF`, `364-DEM-E5` |
| markdown/render hot paths | `30e13822`, `e1e1ea4e`, `46757055`, `88066ff2`, `b29c3b3b` | partial: local markdown now has bounded parsed-document caching for demo redraw reuse, bounded cached LaTeX-to-Unicode conversion for inline/display math, GFM table-row inline span preservation for links/emphasis, viewport-aware styled text layout for visible visual-line windows, and a dashboard markdown summary routed through the same cache path; upstream's richer animated dashboard render-cache behavior is still broader than the local dashboard | `290-TXT`, `370-EXT`, `364-DEM-E2` |
| command palette prefix filtering | `7f467ba0` | partial: local command palette has deterministic ranking plus an upstream-shaped match-kind order (exact, prefix, word-start, substring, fuzzy), Bayesian-style score factors, tag boosts, compact ranking evidence entries, preview/JSONL top-result diagnostics, showcase screen-ID entries (`screen:NN`), favorites/filtering, screen-category metadata, and Enter execution to screens; the full upstream evidence-ledger UI/type depth for ranking is not ported | `315-WGT`, `364-DEM-E3` |
| wasm/pane interruption and touch handling | `a2e4c5dd`, `686d4560`, `627ac8f5`, related tests | partial: local `ShowcaseRunnerCore` now covers step/resize/scenario state plus native-touch capture release and context-loss/render-stall interruption evidence, but it remains a .NET runner-core analogue rather than the upstream wasm-bindgen `AppModel` runner | `340-WEB`, `364-DEM-H` |
| Windows native backend fallback guidance | `c00adfb5`, `40c98246` | locally covered: demo `--help`, the code-level host matrix, and `335-HST` now map upstream's Unix-only native-backend / Windows crossterm-compat guidance to the .NET managed Windows console backend and explicitly warn against Unix-native backend assumptions; broader real-host Windows evidence remains tracked under `364-DEM-I` | `333-HST`, `335-HST`, `364-DEM-I` |

## Work Breakdown

### 364-DEM-A Inventory And Basis Lock

Goal:
- establish an explicit, maintained screen-by-screen parity ledger against the
  current upstream basis

Required outputs:
- a registry table listing every upstream `ftui-demo-showcase` screen with:
  screen number, slug, source file, category, current local status, and owning
  .NET surface
- an app-contract table for:
  CLI flags, environment variables, overlays, tour behavior, palette behavior,
  debug/perf/a11y surfaces, status bar toggles, and mouse behavior
- provenance links from each local screen implementation back to its upstream
  basis file(s)

Acceptance:
- there is no remaining ambiguity about what local demo parity still excludes

### 364-DEM-B App Model And Chrome Parity

Goal:
- replace the current large synthetic shell with a real upstream-shaped app
  model and chrome layer

Required outputs:
- .NET equivalents for upstream app state, current-screen routing, guided-tour
  state, overlay toggles, palette state, status/debug/perf/help control state,
  and screen-local message routing
- a dedicated chrome layer for:
  tab bar, category tabs if retained locally, status bar, guided-tour landing,
  help overlay, command palette overlay, evidence/debug/perf surfaces, and
  mouse hit-target routing
- direct screen selection and navigation rules that align with upstream rather
  than current local convenience behavior

Dependencies:
- `302-RTM`
- `315-WGT`
- `333-HST`

Acceptance:
- the showcase shell no longer relies on placeholder “screen body only”
  rendering to simulate app-level parity

### 364-DEM-C Control-Plane Parity

Goal:
- align launch/test automation behavior with upstream demo contracts

Required outputs:
- CLI support matching the practical upstream surface for:
  `--screen-mode`, `--screen`, `--tour`, `--tour-speed`,
  `--tour-start-step`, mouse policy, auto-exit/test hooks, and help/version
- env-var handling for the demo surface where upstream behavior materially
  affects verification, diagnostics, or deterministic runs
- stable deterministic launch paths for scripted screen capture

Acceptance:
- terminal demo automation can target named screens and control modes using the
  same conceptual inputs as upstream

### 364-DEM-D Tour And Overlay Behavior

Goal:
- port the non-screen-specific behavior that makes the showcase feel like the
  upstream demo rather than a screen switcher

Required outputs:
- guided-tour landing screen behavior
- autoplay tour progression, pause/resume, step navigation, speed adjustment,
  and exit behavior
- overlay composition and precedence rules
- palette/help/debug/perf overlay dismissal and key handling rules
- mouse interaction routing for chrome, overlays, and tab changes

Acceptance:
- app interaction matches upstream closely enough that screen-level parity is
  not masked by shell-level differences

### 364-DEM-E Screen Porting Waves

Screen work should proceed by grouped dependencies rather than by arbitrary
chronology.

#### 364-DEM-E1 Tour And Core Wave

Targets:
- Guided Tour
- Dashboard
- Widget Gallery
- Layout Lab
- Responsive Layout
- Intrinsic Sizing
- Layout Inspector
- Widget Builder
- Inline Mode Story

Likely lower-layer needs:
- stronger layout-inspection views
- richer widget-catalog arrangements
- tour-specific callout and overlay composition

Acceptance:
- the primary first-impression screens no longer rely on text placeholders

#### 364-DEM-E2 Text And Editor Wave

Targets:
- Shakespeare
- Code Explorer
- Markdown
- Advanced Text Editor
- Live Markdown Editor
- i18n Stress Lab

Likely lower-layer needs:
- richer search/highlighting behavior
- text editing depth
- mixed-script width/truncation fidelity
- markdown live-preview workflow state

Acceptance:
- text-heavy demo surfaces behave like real screen ports rather than static
  showcases of adjacent primitives

#### 364-DEM-E3 Interaction And Operator Wave

Targets:
- Forms & Input
- File Browser
- Macro Recorder
- Notifications
- Mouse Playground
- Form Validation
- Command Palette Evidence Lab
- Hyperlink Playground
- Kanban Board
- Drag & Drop Lab

Likely lower-layer needs:
- richer file-tree state and preview routing
- drag/drop interaction model
- hyperlink hit-region behavior
- stronger form/editor interaction parity

Acceptance:
- operator/interaction screens reflect upstream demo workflows, not just local
  widget availability

#### 364-DEM-E4 Systems, Diagnostics, And Evidence Wave

Targets:
- Terminal Capabilities
- Performance
- Action Timeline
- Virtualized Search
- Async Tasks
- Performance Challenge
- Explainability Cockpit
- VOI Overlay
- Accessibility
- Snapshot/Time-Travel Studio

Likely lower-layer needs:
- stronger evidence surfaces
- runtime/event accounting
- more complete diagnostic overlays
- accessibility-control-panel depth

Acceptance:
- the showcase can demonstrate runtime and evidence features with upstream-like
  visibility and control

#### 364-DEM-E5 Visuals And Novelty Wave

Targets:
- Data Viz
- Mermaid Showcase
- Mermaid Mega Showcase
- Theme Studio
- Visual Effects
- Quake Easter Egg

Likely lower-layer needs:
- chart/canvas-style widget depth
- deeper Mermaid renderer/showcase parity
- visual-effects harnesses
- theme-editing and palette-inspection state

Acceptance:
- visual and novelty screens stop being acknowledged placeholders and become
  real first-class showcase surfaces

### 364-DEM-F Lower-Layer Closure From Demo Pressure

Goal:
- explicitly route demo blockers to their true layer instead of hiding them in
  demo-only shims

Required outputs:
- whenever a demo screen reveals a missing capability, add or update the
  corresponding lower-layer tracking item under runtime/widgets/extras/host/web
- avoid demo-specific forks of behavior that would make future upstream sync
  harder

Acceptance:
- demo depth grows by strengthening shared layers, not by accumulating bespoke
  screen-only hacks

### 364-DEM-G Verification Expansion

Goal:
- move parity claims from qualitative inspection to direct screen comparison

Required outputs:
- headless per-screen smoke tests for the full local registry
- scripted PTY tests for navigation, overlays, guided tour, inline mode, and
  cleanup behavior
- a direct showcase comparison lane extending `357-VRF` with named showcase
  screens, fixed sizes, and scripted input tapes
- per-screen artifacts under the local evidence layout
- mismatch reporting that identifies screen id, host mode, and basis pair

Acceptance:
- demo parity can be described from artifacts and tests, not memory

### 364-DEM-H Web/Wasm Alignment

Goal:
- finish `363-DEM` at the program-model level, not only at the primitive level

Required outputs:
- web/wasm runner using the same screen registry and showcase model as the
  terminal runner
- screen selection and scripted capture parity between terminal and web
- shared scenario naming across host surfaces

Acceptance:
- terminal and web showcase runners consume one showcase program model with
  host adapters, not separate local approximations

### 364-DEM-I Windows Host Closure

Goal:
- ensure the parity claim survives the real Windows terminal host

Required outputs:
- scripted Windows Terminal evidence runs for representative screens from each
  screen family
- interactive Windows evidence for guided tour, palette, inline mode, resize,
  and cleanup
- host notes kept current in [`335-HST-host-divergence-ledger.md`](./335-HST-host-divergence-ledger.md)

Acceptance:
- Windows demo parity is evidenced on the real host, not inferred indirectly

## Priority Order

The recommended dependency order is:

1. `364-DEM-A` Inventory And Basis Lock
2. `364-DEM-B` App Model And Chrome Parity
3. `364-DEM-C` Control-Plane Parity
4. `364-DEM-D` Tour And Overlay Behavior
5. `364-DEM-E1` Tour And Core Wave
6. `364-DEM-G` Verification Expansion baseline
7. `364-DEM-E2` Text And Editor Wave
8. `364-DEM-E3` Interaction And Operator Wave
9. `364-DEM-E4` Systems/Diagnostics Wave
10. `364-DEM-E5` Visuals And Novelty Wave
11. `364-DEM-H` Web/Wasm Alignment closure
12. `364-DEM-I` Windows Host Closure

`364-DEM-F` is continuous and should be applied throughout.

## Verification Gates

The plan is only credible if each wave lands with explicit proof.

Minimum gates:

- headless render coverage for each newly-ported screen
- PTY behavior checks for any wave that changes runtime interaction or cleanup
- comparison-lane additions whenever a screen reaches “materially ported”
- web runner checks for any screen claimed as terminal/web aligned
- Windows Terminal evidence refresh for any wave that changes host-facing demo
  behavior

## Documentation And Tracking Rules

As this work proceeds:

- keep [`210-STS-port-status.md`](./210-STS-port-status.md) explicit about the
  difference between shell-level parity and full screen-level parity
- update [`246-MAP-upstream-contract-gap-register.md`](./246-MAP-upstream-contract-gap-register.md)
  if the deeper audit reveals showcase contracts that are still materially
  absent
- keep [`391-DOC-dotnet-implementation-notes.md`](./391-DOC-dotnet-implementation-notes.md)
  honest about the current shape of the terminal app
- update [`357-VRF-shared-sample-comparison-scaffold.md`](./357-VRF-shared-sample-comparison-scaffold.md)
  once the showcase comparison lane grows beyond micro-samples

## Immediate Next Step

The next concrete action is `364-DEM-D`: continue closing tour and overlay
behavior by adding the remaining upstream frame hit registry, generalized pane
hit routing, exact frame-geometry reconciliation, richer overlay event stream,
and deeper chrome contracts on top of the maintained `364-DEM-A` inventory.
