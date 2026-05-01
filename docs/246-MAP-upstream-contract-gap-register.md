# 246-MAP Upstream Contract Gap Register

## Purpose

This document is the maintained register of upstream FrankenTUI contracts that
FrankenTui.Net has not yet fully ported.

Use it as the concrete follow-on checklist after the current hierarchical port
plan, so parity work is driven by explicit upstream contracts instead of by
memory or ad hoc bug reports.

This document extends:

- [`242-MAP-upstream-sync-workflow.md`](./242-MAP-upstream-sync-workflow.md)
- [`244-MAP-divergence-ledgers.md`](./244-MAP-divergence-ledgers.md)
- [`245-MAP-divergence-triage-policy.md`](./245-MAP-divergence-triage-policy.md)

## Audit Basis

This register is based on a conservative audit of:

- accepted and proposed upstream ADRs under `.external/frankentui/docs/adr/`
- in-scope practical spec documents under `.external/frankentui/docs/spec/`
- current maintained local ledgers, blocker notes, and implementation docs
- local code search for matching types, surfaces, or explicit substitutes

This register is intentionally narrower than "every upstream document". It
tracks contracts that are either:

- already adopted locally but still partial, or
- clearly in practical repo scope but still not yet fully carried locally

## Status Legend

- `partial`: a local baseline exists, but the upstream contract is not fully
  carried yet
- `blocked`: remaining work is real, but cannot be fully closed from the
  current workspace alone
- `reference-only`: excluded from this register by current scope rules

## Scope Exclusions

Per [`110-UPR-frankentui-roadmap.md`](./110-UPR-frankentui-roadmap.md), this
register excludes current reference-only upstream areas unless FrankenTui.Net
adopts them explicitly later:

- `frankenterm` browser API, websocket, remote-threat-model, and related remote
  transport contracts
- FFI and embedded-core crate-layout contracts
- SDK-specific contracts that are not part of the current FrankenTui.Net
  implementation surface

## Active Remaining Gaps

The managed upstream workspace is currently refreshed to
`40c98246f27f9d174b3923c8df841ba325247dd4`. The earlier post-`f612df2b`
contract rows remain closed, but the `2d25a03d..40c98246` upstream range
introduces fresh in-scope gaps that are now tracked explicitly instead of being
hidden behind the existing shell-level showcase parity.

| Code | Status | Upstream basis | Local state | Required closure |
| --- | --- | --- | --- | --- |
| `GAP-364-DEMO-SCREEN-PARITY` | `partial` | `crates/ftui-demo-showcase/src/screens/mod.rs` plus per-screen modules at `40c98246` | Local catalog has all 45 upstream screens, but most bodies in `ShowcaseSurface.cs` are reduced approximations. Screen 12 now has an upstream-shaped local Terminal Capabilities matrix/evidence/simulation tri-panel with deterministic capability rows, evidence-source labels, diagnostic event names, simulated profile controls, and environment summary; exact probing, input-driven mode switching, and report export remain open. Screen 13 now has an upstream-shaped local Macro Recorder controls/timeline/event-detail/scenario-runner layout with deterministic preset macro rows and upstream-shaped key labels; live recorder focus routing, mouse focus, playback queue, and exact tracing remain open. Screen 14 now has an upstream-shaped virtualized 10k-item log list, performance stats panel, visible-row counts, and navigation status; live keyboard/mouse list mutation and exact upstream styling remain open. Screen 15 now has an upstream-shaped local three-column Markdown/Rich Text surface with markdown renderer, LLM streaming/detection panel, style sampler, Unicode table, and wrap/alignment demo; animated backdrop, live scroll/focus controls, exact markdown detection/cache behavior, and full syntax/theme depth remain open. Screen 16 now has a deeper local Mermaid library/viewport/controls/metrics/status-log surface including render mode, palette, guard, zoom/pan, viewport override, crossings/symmetry/compactness, and `mermaid_render` status evidence; full upstream IR renderer, search/inspect modes, debug overlays, node navigation, minimap, palette rendering, exact JSONL schema, and full mouse/key control parity remain open. Screen 17 now combines the shared Mermaid viewport with Mega-specific sample-library metadata, filter/category summary, controls/keymap panel, node-detail summary, and `mermaid_mega_recompute` metrics evidence; full upstream generated sample corpus, interactive node navigation, edge highlighting, search/inspect modes, minimap, debounced recompute cache, palette rendering, and exact JSONL/keymap parity remain open. Screen 19 now has an upstream-shaped local Responsive Layout surface with breakpoint indicator, default/custom-threshold notes, sidebar/content/aside adaptive layout, visibility rules, responsive values, and simulated-width controls; live resize/mouse/key mutation, actual `ResponsiveLayout` primitive parity, breakpoint coloring, and exact custom-breakpoint state remain open. Screen 20 now has an upstream-shaped local Log Search surface with the search viewport, live-stream limits, search/filter key controls, deterministic env flags, diagnostic event names, and JSONL field labels; real streaming append/follow mode, inline search/filter state machine, case/context toggles, and exact diagnostic hook/schema plumbing remain open. Screen 21 now has an upstream-shaped local Notifications surface with trigger instructions, max-visible/max-queued queue config, TopRight stack, toast priorities/styles/actions, lifecycle states, mouse affordances, and tick expiry/promotion notes; real NotificationQueue widget state, auto-dismiss timers, action invocation state, mouse hit regions, and exact toast rendering remain open. Screen 22 now has an upstream-shaped local Action Timeline surface with filters/follow controls, deterministic event timeline rows, selected event details, field/evidence payloads, max-event/burst constraints, diagnostic span names, and navigation/mouse affordances; real bounded ring-buffer state, input recording hooks, filter cycling, follow-drop decisions, mouse hit regions, and tracing integration remain open. Screen 23 now has an upstream-shaped local Intrinsic Sizing surface with the four upstream scenarios, effective-width breakpoints, adaptive-sidebar/card/table/form rules, width override controls, mouse cycle affordances, and embedded pane-studio notes; real content-aware measurement primitives, live scenario switching, width override state, embedded pane workspace interaction, and exact responsive rendering remain open. Screen 24 now has an upstream-shaped local Layout Inspector surface with scenarios, solver steps, overlay/tree toggles, constraint-vs-rect records, overflow/underflow statuses, mouse/key affordances, and pane-studio notes; real ConstraintOverlay/LayoutDebugger widget parity, live scenario/step state, embedded pane interaction, hit-region routing, and exact styled rect visualization remain open. Screen 25 now has an upstream-shaped local Advanced Text Editor surface with sample multiline editor content, line/cursor/match status, search/replace controls, undo/redo history panel, focus-cycle controls, text-editor diagnostic env flags, JSONL field labels, and diagnostic event names; real AdvancedTextEditor state machine, line-number/selection rendering, search navigation, replace/undo/redo mutation, focus hit regions, and exact diagnostics plumbing remain open. Screen 26 now has an upstream-shaped local Mouse Playground surface with a 4x3 hit-target grid, hit IDs, hover/click state, event log kinds, stats/overlay state, keyboard controls, diagnostic env flags, JSONL field labels, and telemetry hook names; real SGR mouse decoding, frame hit registry integration, hover stabilizer state, target click mutation, overlay rendering, jitter stats, and exact diagnostic hook plumbing remain open. Screen 27 now has an upstream-shaped local Form Validation surface with the nine-field registration form, real-time/on-submit mode/status, touched/dirty state, error summary, validator inventory, toast queue config, mouse affordances, and diagnostic event names; real Form/FormState mutation, cross-field password matching, submit-mode error clearing, notification queue state, mouse hit regions, and exact form rendering remain open. Screen 28 now has an upstream-shaped local Virtualized Search surface with the search-bar plus 70/30 results/stats layout, 10k-item dataset metadata, deterministic filtered results, match positions/scores, keybinding inventory, diagnostic env flags, JSONL field labels, and telemetry hook names; real VirtualizedSearch state, fzy-style incremental filtering/highlighting, keyboard/mouse navigation, viewport synchronization, diagnostic log storage, and exact render styling remain open. Screen 29 now has an upstream-shaped local Async Tasks surface with the header/task-queue/details/activity/help layout, task lifecycle states, scheduler policy cycle, aging/fairness formula, hazard cancellation fields, metrics/invariant names, mouse/key affordances, and diagnostic JSONL event names; real AsyncTaskManager state mutation, queueing-policy scheduler, progress bars, hazard auto-cancel decisions, diagnostic log storage, mouse hit regions, and exact upstream styling remain open. Screen 30 now has an upstream-shaped local Theme Studio surface with the presets/token-inspector/status layout, preset names, token categories, hex values, contrast/WCAG ratings, JSON and Ghostty export fields, focus/navigation/mouse affordances, diagnostic env flags, JSONL field labels, and telemetry hook names; real global theme switching, token swatch rendering, contrast computation from live palette, export payload generation, mouse hit regions, diagnostic log storage, and exact upstream styling remain open. Screen 31 now has an upstream-shaped local Time-Travel Studio surface with the timeline/preview/frame-info layout, frame metadata, playback/recording controls, marker/timeline mouse affordances, A/B compare rows, heatmap state, checksum/chain fields, JSONL report fields, diagnostic event names, and replay invariants; real SnapshotPlayer frame buffer storage, delta rendering, timeline scrubbing state, marker mutation, compare diff cache, heatmap overlay rendering, JSONL file export, diagnostic log storage, and exact upstream styling remain open. Screen 32 now has an upstream-shaped local Performance Challenge surface with the title/metrics/sparkline/render-budget/status layout, latency percentiles, tick-sample metadata, stress harness state, degradation tier thresholds, forced-tier controls, budget/sparkline mouse affordances, JSONL tier-change fields, deterministic env notes, and keybinding labels; real PerformanceHud ring-buffer metrics, sparkline rendering, stress ramp/hold/cooldown state, tier-change logger integration, forced-tier mutation, budget adjustment, mouse hit regions, and exact upstream styling remain open. Screen 33 now has an upstream-shaped local Explainability Cockpit surface with the header/source plus diff-strategy, resize-BOCPD, budget-decision, decision-timeline, and control layout, deterministic evidence rows, JSONL event and field labels, refresh/pause/clear/focus/scroll keybindings, source env hints, and mouse/timeline affordance notes; real evidence-file loading, JSONL parsing, auto-refresh, pause/clear state mutation, panel focus, timeline scroll, mouse hit regions, overlay mode, and exact upstream styling remain open. Screen 34 now has an upstream-shaped local i18n Stress Lab surface with the locale-bar plus String Lookup, Pluralization Rules, RTL Layout Mirroring, and Stress Lab panel layout, locale coverage/fallback labels, plural-category examples, RTL/LTR mirroring samples, grapheme/display-width/truncation evidence, stress-report JSONL fields, and locale/panel/sample/export keybinding labels; real StringCatalog, plural-rule engine, grapheme cursor state, sample-set mutation, report file export, mouse locale/panel routing, RTL flow reversal, and exact upstream styling remain open. Screen 35 now has an upstream-shaped local VOI Overlay surface with the centered sampler-debug overlay shape, decision/posterior/observation/ledger/control sections, fallback and inline-auto source labels, VOI posterior/gain/cost/e-value fields, ledger decision/observation entries, focus/detail/ledger keybindings, and mouse hit-region notes; real VoiSampler, inline_auto_voi_snapshot, overlay widget rendering, focus/expanded state mutation, ledger selection, mouse hit testing, runtime observation updates, and exact upstream styling remain open. Screen 36 now has an upstream-shaped local Inline Mode surface with the header plus inline/alt-screen comparison story, scrollback-preservation labels, anchor/UI-height/log-rate state, deterministic log rows, compare/alt-screen notes, stress limits, keybindings, and mouse hit-region labels; real InlineModeStory log ring buffer, tick-driven stream growth, compare/mode/anchor/height/rate mutation, stress burst generation, layout hit testing, inline writer behavior, and exact upstream styling remain open. Screen 37 now has an upstream-shaped local Accessibility surface with the overview, toggles, WCAG contrast, live preview, and telemetry layout, high-contrast/reduced-motion/large-text state labels, WCAG threshold rows, preview styling labels, app-level toggle action names, telemetry event fields, keybindings, and mouse hit-row notes; real app-level A11ySettings sync, contrast computation from live palette, theme large-text style application, telemetry event ring buffer, mouse toggle dispatch, overlay integration, and exact upstream styling remain open. Screen 38 now has an upstream-shaped local Widget Builder surface with the sandbox header plus presets, widget tree, live preview, props, export, and mouse-hint layout, preset names, widget-kind ids, editable prop labels, JSONL export fields, snapshot schema labels, keyboard shortcuts, and mouse routing notes; real WidgetBuilder preset state, widget config mutation, live widget rendering, save/export file IO, props hash generation, list selection state, mouse hit testing, and exact upstream styling remain open. Screen 39 now has a materially aligned local Command Palette Evidence Lab with upstream 12-item `cmd:*` sample action set, two-line header, 55/45 palette/evidence columns, selected-result diagnostics, compact evidence ledger, split bench/hint footer, screen-local `0`-`5`/`m` match-filter controls, a `b`-toggled three-tick deterministic sample-query bench loop, palette-area wheel/click handling, local HintRanker-style expected-utility/net-value/VOI evidence, and frame/mouse evidence for the active filter/bench/mouse/hint state; exact styling remains local. Screen 40 now has an upstream-shaped local Determinism Lab surface with the header plus equivalence, scene preview, checks, and report/env layout, Full/DirtyRows/FullRedraw strategy rows, checksum timeline, mismatch fields, scenario/run labels, JSONL export fields, deterministic env keys, hash-key format, FNV checksum notes, keybindings, and mouse hit-region labels; real buffer generation, diff application, checksum comparison, scenario simulation, run history/details scrolling, JSONL file export, hit-region registration, and exact upstream styling remain open. Screen 41 now has an upstream-shaped local Hyperlink Playground surface with the header plus OSC-8 links, details/registry, and controls/JSONL layout, upstream link labels/URLs, LinkRegistry and HitRegion labels, OSC-8 open/close evidence, hover/action state labels, keybindings, mouse action labels, logging env vars, JSONL fields, action names, and hit-id base; real LinkRegistry rendering, hit-region registration, hover/click/focus state, keyboard activation/copy mutation, JSONL file output, and exact upstream styling remain open. Screen 42 now has an upstream-shaped local Kanban Board surface whose default 80x24 app snapshot exactly matches upstream app_kanbanboard_80x24.snap under tools/FrankenTui.ShowcaseCompare --screens 42, with deterministic Todo/In Progress/Done seed cards, focused heavy border, compact footer, and app status line; keyboard focus movement, card moves, undo, redo, history, redo stack, and rendered card positions mutate through a screen-local ShowcaseKanbanState, while mouse drag/drop routing, card dimming/drop preview styling, cached hit-region registration, and wider viewport/interactive snapshot evidence remain open. Screen 43 now has an upstream-shaped local Live Markdown Editor surface with the search/editor/preview layout, upstream sample markdown, search query/match labels, focus modes, TextArea line-number/soft-wrap status, MarkdownRenderer preview, raw-vs-rendered width diff, preview scroll state, MarkdownTheme and SyntaxHighlighter notes, keyboard and mouse controls, cached layout-rect notes, and JSONL action fields; real MarkdownLiveEditor state mutation, rope editing, search selection, focus transitions, preview scrolling, diff recomputation, mouse pane routing, syntax-highlighted Markdown rendering, and exact upstream styling remain open. Screen 44 now has an upstream-shaped local Drag & Drop Lab surface with Sortable List, Cross-Container, and Keyboard Drag tabs, deterministic Item/File lists, selected/focused list state, KeyboardDragManager status, DropTargetInfo and DragPayload labels, announcements, sortable reorder and transfer controls, mouse click/scroll/right-click routing notes, small-terminal fallback text, cached layout-rect notes, and JSONL action fields; real DragDropDemo state mutation, KeyboardDragManager integration, DropTargetInfo navigation, list reorder/transfer behavior, announcements queue, mouse hit testing, and exact upstream styling remain open. | Execute the `364-DEM` waves in `364-DEM-full-showcase-parity-plan.md` until each screen has materially matching state, structure, interactions, and evidence. |
| `GAP-364-DEMO-CONTROL-PLANE` | `partial` | `crates/ftui-demo-showcase/src/cli.rs`, `app.rs`, `chrome.rs`, `tour.rs` at `40c98246` | Local CLI/env parsing now covers the practical upstream launch baseline for screen mode including `inline-auto`, inline height/min/max, screen selection, guided-tour seed controls, mouse policy, deterministic mode/seed parsing, tick cadence, generic auto-exit by elapsed milliseconds or ticks, pane-workspace path selection, upstream-shaped host-size bootstrap for normal runs via backend terminal size, explicit CLI dimensions and VFX/Mermaid harness forced-size preservation, generic evidence JSONL launch/screen-init/frame logging, VFX/Mermaid harness option parsing plus local screen/size/timing/JSONL application, VFX upstream-style launch/frame/input/perf event names and Mermaid upstream-style start/frame/done event names with local hash/sample/timing/config/link aliases, and pane-workspace JSON load/save with a versioned local envelope, raw-v1 migration, invalid snapshot preservation, launch/frame evidence for recovery/migration state, local `pane_workspace_save` acknowledgment records, and Layout Lab restored/default/recovered status. Guided-tour landing start/speed keyboard controls, mouse wheel/click landing controls, active pause/resume, Left/Right or n/p storyboard step-index progression, speed up/down, keyboard exit, and active-tour mouse-overlay exit are implemented and headless-covered. Local chrome mouse routing now covers category-tab clicks, visible screen-tab clicks, tab-wheel screen cycling, tour stop on chrome navigation, and command-palette priority over chrome routing. Local dashboard highlight pane-link routing now switches to linked screens and preserves non-link/non-left forwarding behavior. Interactive generic frame evidence now carries tour active/paused/speed state, tour step index/count, tour callout/highlight state, local resolved highlight rectangles, overlay visibility, evidence/perf/debug/A11y visibility, A11y flags, mouse-capture state, palette state, current screen, and pane snapshot hash, and local `tour_event`, `palette_event`, `mouse_event`, `mouse_capture_toggle`, `a11y_event`, and `perf_hud_event` records are emitted when tour, palette, mouse input, mouse capture, A11y, or perf-HUD visibility changes relevant showcase state. Generic local evidence records now include upstream-style `upstream_schema_version`, `seq`, `run_id`, `seed`, and `screen_mode` context aliases, local startup `screen_init` records now carry `demo_screen_init` stream identity, screen id/category, init milliseconds, effect count, and memory-estimate fields, local A11y records carry upstream-shaped `panel_toggle`, `high_contrast_toggle`, `reduced_motion_toggle`, and `large_text_toggle` names plus tick/screen/flag fields, local perf-HUD toggle records carry upstream-shaped `hud_toggle` state/tick/screen fields, and local visible-HUD periodic records carry upstream-shaped `tick_stats` fields every 60 ticks using deterministic frame-derived fps/tps, avg/p95/p99/min/max, sample count, present/diff timing, changed-cell, and dirty-row values, and local visible-HUD stall records carry upstream-shaped `tick_stall` since-ms/tick/screen/reduced-motion fields. Local mouse events now include upstream-compatible `kind`, `x`, `y`, `hit_id`, `action`, `target_screen`, and `current_screen` aliases in addition to local detail fields, and local mouse-capture toggle events include upstream-compatible `state`, `mode`, `source`, and `current_screen` fields. Advertised Ctrl+K command-palette launch works locally, showcase palette entries use `screen:NN` ids with screen metadata, command-entry Ctrl+F favorites operate on screen ids, Ctrl+Shift+F favorites-only filtering, Ctrl+0/Ctrl+1..N command-category palette filtering, Enter execution routes to selected showcase screens, and local palette ranking now follows the upstream match-kind order (exact, prefix, word-start, substring, fuzzy) with Bayesian-style score factors, tag boosts, compact ranking evidence entries, preview diagnostics, and palette JSONL top-result evidence. Ctrl+I evidence ledger toggling, Ctrl+P perf HUD toggling, F6/`m` mouse-capture toggling, F12 debug overlay toggling, and Shift+A A11y panel toggling are implemented and listed in the local help overlay. A local bottom status-row mouse zone toggles help, palette, A11y, perf HUD, debug, evidence ledger, and mouse-capture state when no overlay is covering it, and interactive mouse-capture changes now reconfigure the terminal session SGR mouse feature. Shift+H/Shift+L screen navigation stops active tours like upstream, and Escape precedence now dismisses command palette before lower overlays, then evidence ledger, perf HUD, debug, help, and A11y. Remaining upstream-specific diagnostics JSONL streams, exact upstream lazy screen-init lifecycle, exact upstream A11y/perf HUD JSONL stream gating, upstream rolling perf sample window, exact upstream stall timing semantics, VFX/Mermaid renderer-equivalent harness execution and full telemetry payloads, broader pane-workspace upstream corpus coverage, screen-level deterministic fixture seeding, upstream frame hit registry, generalized pane hit routing, full upstream ranking evidence-ledger UI/type depth, exact tour frame geometry/hit regions, exact upstream tour/palette/mouse JSONL schema/hit-id shapes, full upstream overlay/chrome depth, and full chrome/screen routing are not fully carried. | Port the remaining app model, chrome, diagnostics, deterministic harness, tour, overlay, and persistence contracts under `364-DEM-B` through `364-DEM-D`, then expand verification under `364-DEM-G`. |
| `GAP-304-LOAD-GOVERNOR` | `partial` | `54373939`, `0f844251`, `conformal_predictor.rs`, `degradation_cascade.rs`, `conformal_frame_guard.rs`, and related runtime hardening commits in `2d25a03d..40c98246` | Local runtime now has `LoadGovernorConfig`, `BudgetControllerConfig`, `PidGains`, `EProcessConfig`, degradation levels, PID/e-process-gated frame-stat decisions, transition sequencing/correlation, `ftui.decision.degradation` telemetry with PID/e-process/gate evidence fields, focused tests for default/disabled/warmup/degradation-floor/telemetry behavior, generic showcase frame evidence carrying the same controller fields, reusable operator HUD rendering of those fields, hosted runtime captures retaining final `RuntimeFrameStats`, and doctor text/dashboard summaries for load-governor level/action/reason/PID/e-process/margins/warmup/transition state. `RuntimeConformalPredictor` and `RuntimeDegradationCascade` now add local conformal frame-risk and degradation-cascade baselines with upstream-shaped bucket keys, exact/mode+diff/mode/global/default fallback hierarchy, n+1 conformal quantiles, bounded per-bucket windows, reset counters, EMA/nonconformity p99 prediction, fallback/calibrated/at-risk guard state, recovery streaks, degradation-floor clamping, upstream-shaped JSONL schema fields, essential-widget filtering, and focused predictor/cascade tests. `RuntimePolicyConfig` now carries upstream-shaped conformal/frame-guard/cascade/PID/e-process budget/budget-controller policy conversion and feeds `RuntimeExecutionPolicy` effective defaults while preserving explicit load-governor overrides. `AppRuntime` now records policy-backed conformal/cascade frame evidence in `RuntimeFrameStats`, degradation telemetry, and generic showcase JSONL; with explicit `PolicyConfig`, it passes lower-tier cascade levels through `RuntimeRenderContext` and applies `SKIP_FRAME` as a pre-render bypass. `BlockWidget`, `ParagraphWidget`, `ProgressWidget`, `ListWidget`, `StatusWidget`, `TabsWidget`, `TextAreaWidget`, `TableWidget`, `TreeWidget`, `ScrollbarWidget`, `BufferInspectorWidget`, and `LayoutInspectorWidget` now carry lower-tier widget responses for simple borders/branches/rails, no styling, essential-only, and skeleton states. Staged conformal variants and remaining full-catalog widget-level lower-tier degradation are not yet fully ported. | Port the remaining upstream render-program integration depth or document a narrow staged deferral once the current runtime-hardening boundary is finalized. |
| `GAP-281-PANE-PERSISTENCE` | `partial` | `c38349a9`, `ba20e312`, `e2927070`, and pane recovery/corpus commits in `2d25a03d..40c98246` | Local `PaneWorkspaceState` supports JSON round-trip, deterministic replay, checkpoints, undo/redo, demo rendering, canonical JSON import/export with validation, current-schema migration audit fields, and byte-stable corpus re-export coverage. The showcase shell now loads/saves a versioned local `--pane-workspace` envelope through that canonical importer, migrates raw-v1 local workspace snapshots, preserves invalid snapshots, emits generic launch/frame evidence for load/recovery/migration state, emits local `pane_workspace_save` acknowledgment evidence with schema version, and surfaces restored/default/recovered status in Layout Lab. Broader upstream lifecycle/corpus coverage remains open. | Extend the pane workspace model and demo shell to cover the remaining upstream corpus lifecycle, then add PTY/comparison coverage. |
| `GAP-364-VFX-CANVAS-HARNESS` | `partial` | `f886a800`, `4883171e`, visual-effects harness/golden commits, and Quake perf commits in `2d25a03d..40c98246` | Local Visual Effects and Quake screens are cataloged, CLI/env harness options select the local VFX screen and forced harness size/timing, and scripted VFX runs can emit deterministic local harness JSONL records with upstream-style `vfx_harness_start` launch records, input checksums, rendered-buffer checksums when a frame buffer is available, upstream-style `vfx_frame`/`frame_idx`/numeric `hash` aliases, Doom/Quake `vfx_input` records for the upstream FPS input script, shared local `hash_key` correlation fields, normalized effect labels/descriptions, renderer/canvas-mode/local-quality/FPS-effect fields, and local `vfx_perf_frame`/`vfx_perf_summary` timing records when `--vfx-perf` is enabled. A local `ShowcaseVfxGoldenRegistry` now derives deterministic scenario names, saves/loads hash vectors, compares actual frame hashes against expected hashes, extracts numeric frame hashes from VFX JSONL, and is wired to scripted runs through local `--vfx-golden` / `--vfx-update-golden` controls. `ShowcaseVfxEffects` now mirrors the current upstream 19-effect key catalog and aliases so local CLI parsing, labels, descriptions, JSONL effect fields, golden scenario names, and deterministic canvas pattern selection share canonical keys. `FrankenTui.Extras` now carries local `CanvasMode`, `CanvasPixelRect.FromCellIntersection`, and Braille `CanvasPainter.RenderExcluding` primitives matching the upstream exclusion semantics, screen 18 renders a deterministic frame-driven Braille canvas that responds to every current upstream harness effect key, and screen 45 now renders a deterministic FPS Braille canvas for Quake E1M1 with upstream-shaped player/physics, controls, mesh-raster, quality-tier, palette, depth-buffer, small-terminal fallback, harness, JSONL, and divergence labels tied to QUAKE_E1M1_VERTS/QUAKE_E1M1_TRIS. Rich upstream effect implementations, real QuakeE1M1State physics/collision, real Quake mesh rasterization, upstream-equivalent VFX renderer output, and enforced upstream golden fixtures remain open. | Port the remaining VFX renderer/effect primitives needed by the current upstream visual screens or document a narrow divergence, then add upstream-equivalent VFX golden evidence. |
| `GAP-344-WASM-SHOWCASE-RUNNER` | `partial` | `crates/ftui-showcase-wasm/src/runner_core.rs`, `wasm.rs`, and pane interruption/touch commits in `2d25a03d..40c98246` | Local web/wasm alignment now has a `ShowcaseRunnerCore` analogue that owns frame/size/scenario state over the shared showcase web rendering path, steps the shared model, accepts encoded input for scenario/runner control, clamps resize dimensions, and emits upstream-shaped pane interruption records for native-touch yield, context loss, and render stalls. Multi-touch pane input releases active local capture with `phase=native_touch_gesture` / `command=release` evidence, and context-loss/render-stall paths clear capture and log lifecycle records. It is still not a full wasm-bindgen `AppModel` runner, and terminal/web hosts are not yet unified on one app model. | Complete `364-DEM-H` by adapting terminal and web hosts to the same showcase model and adding broader screen selection/interruption evidence. |

## In-Scope Contract Surfaces Still Fully Absent Locally

At the current audit level, no newly reviewed in-scope surface from the
`f612df2b...40c98246` range is classified as completely absent. The largest
new gaps are partial because local analogues already exist, but they do not yet
carry the current upstream contract depth.

## Contracts Reviewed But Not Registered As Current Gaps

These upstream contracts were reviewed and are not currently carried as open
items in this register:

- `docs/adr/ADR-002-presenter-emission.md`
  Current local presenter behavior follows the reset-and-apply baseline closely
  enough to treat it as ported for now.
- `docs/adr/ADR-001-inline-mode.md`
  The newline-based inline rendering gap is now closed locally via a dedicated
  inline writer, headless contract tests, and PTY verification. The historical
  closure record is retained in `336-HST-inline-mode-divergence-ledger.md`.
- `docs/spec/diff-strategy-contract.md`
  The local runtime now carries an explicit regime-based diff strategy selector,
  degraded-terminal fallback, and `DiffEvidenceLedger`, so this is no longer a
  fixed-scan-only gap.
- `docs/spec/keybinding-policy.md`
  A reusable runtime input envelope/controller now routes the current
  interactive surfaces through one shared keybinding-aware path rather than a
  hosted-only loop.
- `docs/spec/semantic-events.md`
  The shared runtime input path now carries both source events and semantic
  events into app-facing messages for the current interactive surfaces.
- `docs/spec/resize-scheduler.md` and `docs/spec/resize-migration.md`
  Resize coalescing is now exposed through the same reusable runtime input path
  with resize decision telemetry and ready-size application.
- `docs/spec/opentui-evidence-manifest.md`
  Doctor and harness artifacts now use a runtime-driven replay/trace/diff
  capture path rather than a synthetic one-entry replay record, while
  preserving the upstream manifest shape.
- `docs/spec/opentui-semantic-equivalence-contract.md` and
  `docs/spec/opentui-transformation-policy-matrix.md`
  Local tooling now validates the upstream semantic/policy bundle, materializes
  planner/certification projections, and feeds planner findings into the local
  contract gate and doctor artifact set.
- `docs/spec/command-palette.md`
  The hosted extras/runtime surface now carries a real open/query/select/execute
  palette loop with deterministic ranking, preview state, command execution,
  and PTY/headless coverage.
- `docs/spec/log-search.md`
  The hosted extras/runtime surface now carries live-stream integration,
  full-vs-lite tiering, context merging, all-match highlighting, and interactive
  toggle/edit behavior with headless verification.
- `docs/spec/macro-recorder.md`
  The hosted extras/runtime surface now carries explicit record/ready/play/loop
  state, normalized replay timing, timer-driven playback, and PTY/headless
  coverage rather than only deriving a macro from evidence logs after the fact.
- `docs/spec/performance-hud.md`
  The HUD can now consume runtime frame statistics directly, the interactive
  showcase feeds those stats back into the extras surface, and headless/PTY
  coverage verifies the runtime-fed path.
- `crates/ftui-harness/src/render_gauntlet.rs`,
  `crates/ftui-harness/src/presenter_equivalence.rs`,
  `crates/ftui-harness/src/layout_reuse.rs`, `src/fixture_suite.rs`,
  `src/baseline_capture.rs`, and `src/rollout_scorecard.rs`
  The local harness now carries render gauntlet, presenter equivalence,
  layout-reuse, fixture-suite, rollout-scorecard, doctor cost-profile, and
  suite-report/index depth, so this slice is no longer tracked as an active
  post-`f612df2b` gap.
- `crates/doctor_frankentui/src/capture.rs`, `src/doctor.rs`,
  `src/report.rs`, `src/seed.rs`, and the new harness cost-profile modules
  The local doctor surface now carries artifact-manifest/failure-signature
  validation, workflow/bootstrap summaries, `run_meta` / `suite_manifest`,
  seed-plan and seed-execution artifacts, a stable `doctor-suite` workspace,
  and an optional actual JSON-RPC seed/bootstrap path behind `--seed-mode
  actual`, so this slice is no longer tracked as an active post-`f612df2b`
  gap.
- `docs/adr/ADR-010-asupersync-targeted-adoption.md`,
  `docs/spec/asupersync-frankentui-seam-inventory.md`, and
  `docs/spec/asupersync-frankentui-invariants-metrics-evidence.md`
  The local runtime/doctor stack now emits an explicit orchestration-only
  Asupersync evidence artifact with lane, fallback, divergence, and
  correlation fields, so this row is no longer tracked as an active evidence
  contract gap even though the deeper runtime executor/orchestration work
  remains open separately.
- `crates/ftui-runtime/src/effect_system.rs` and the expanded
  `crates/ftui-runtime/src/subscription.rs` surface from `bd377ca4`
  The local runtime now carries effect counters, queue/reconcile snapshots,
  optional effect labels, cancellation/failure accounting for command and
  subscription execution, and declared subscription lifecycle start/stop
  telemetry. FrankenTui.Net still does not mirror the upstream background
  subscription-thread and bounded join implementation one-to-one, but that is
  now treated as an execution-model divergence rather than an untracked missing
  operator contract because the local synchronous runtime has explicit
  lifecycle/fault evidence and no hidden background thread manager.
- `docs/adr/ADR-003-terminal-backend.md`,
  `docs/adr/ADR-005-one-writer-rule.md`, and
  `docs/adr/ADR-006-untrusted-output-policy.md`
  The backend boundary is now explicitly split into lifecycle, output-sink, and
  event-source facets; routed subprocess output stays behind the writer gate;
  and sanitize-by-default proof includes fuzz and PTY coverage.
- `docs/spec/pane-parity-contract-and-program.md`
  Pane state now persists through the hosted/demo path, supports timeline
  undo/redo, and is exercised directly in headless/web/PTY coverage instead of
  being rebuilt as a view-only snapshot.
- `docs/spec/telemetry.md`
  The env-var/install surface now has a tested OTLP bridge/export path rather
  than only a configuration plan.
- `docs/spec/telemetry-events.md`
  Macro playback evidence and stronger redaction proof are now carried in the
  telemetry surface and tests.
- `docs/spec/mermaid-config.md` and `docs/spec/mermaid-showcase.md`
  The Mermaid surface now has a deterministic parse/render/diagnostic baseline
  plus interactive showcase preferences, rather than only a static preview
  scaffold.
- `crates/ftui-runtime/tests/deterministic_replay.rs` and
  `crates/ftui-web/tests/wasm_step_program.rs`
  The shared comparison scaffold now includes pane, macro, and Mermaid cases in
  addition to the earlier render/runtime sample set, so representative hosted
  surfaces are covered by the cross-implementation lane.
- `docs/spec/cache-and-layout.md`
  The local kernel already uses 16-byte cell-friendly value types and row-major
  buffers; no distinct open contract gap is currently recorded here.
- `docs/spec/state-machines.md`
  The local runtime and render pipeline have clear state boundaries and tests,
  even though individual subcontracts above remain open.
- `docs/adr/ADR-004-windows-v1-scope.md` and `docs/WINDOWS.md`
  Windows is no longer an active contract-gap row: CI doctor/showcase evidence
  is supplemented by an external local Windows interactive transcript, so the
  host surface is now treated as `validated-external`. The retained closure
  record is `2026-03-12-windows-conpty-evidence-blocker.md`.
- `crates/ftui-widgets/src/focus/manager.rs`,
  `crates/ftui-widgets/src/modal/focus_integration.rs`, and
  `crates/ftui-widgets/src/modal/stack.rs`
  FrankenTui.Net now carries an explicit local analogue for this wave via
  `WidgetFocusGraph`, `WidgetFocusManager`, and `WidgetFocusAwareModalStack`,
  alongside the earlier `WidgetInputState` hosted-parity state surface. The
  local tests now cover graph-backed focusability, nested modal trapping,
  mid-stack modal removal, live modal focus-set repair, and host blur/restore,
  so this slice is no longer tracked as an active gap row.

If later evidence shows one of these is still materially incomplete, promote it
to an active row in this register.

## Next Step

This register is now in maintenance mode. Re-open a gap row only if fresh
divergence evidence appears from parity testing, host validation, or upstream
contract drift.
