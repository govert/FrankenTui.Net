# 210-STS Port Status

## Purpose

This document is the canonical execution-status ledger for the work hierarchy in
`200-PRT-port-work-breakdown.md`.

The plan document defines intended work. This status document records what has
actually landed so far.

## Status Legend

- `completed`: the current planned baseline for this code is in the repo and
  has matching verification for its current layer
- `in progress`: meaningful baseline work exists, but the intended depth of the
  item is not yet fully landed
- `not started`: no meaningful implementation has landed yet
- `blocked`: currently blocked from progressing from this workspace

Cross-cutting blockers that do not fully stop adjacent work are recorded
separately in [2026-03-09-big-batch-blockers.md](./2026-03-09-big-batch-blockers.md)
and [2026-03-09-hosted-parity-blockers.md](./2026-03-09-hosted-parity-blockers.md).

## Current Basis

- Current status basis commit:
  working tree after first `f612df2b` upstream-sync wave
- Current upstream workspace basis:
  `40c98246f27f9d174b3923c8df841ba325247dd4`
- Last full verification pass at status update time:
  `dotnet test FrankenTui.Net.sln --no-restore`

## Story So Far

- `8d23563` `Define port planning framework`
  Planning doctrine, upstream roadmap, and the initial hierarchical work list
  landed.
- `6e30caf` `Create .NET setup slice`
  The .NET 10 solution, project graph, mapping ledger, and artifact layout were
  established.
- `9156af3` `Port initial core and render primitives`
  Geometry, cursor, text-width, cells, buffers, diffs, and first headless tests
  landed.
- `584d345` `Port presenter and terminal model baseline`
  Capability profiling, ANSI helpers, presenter logic, terminal model, and
  deeper headless verification landed.
- `3812dca` `Complete base port baseline through PTY verification`
  The first end-to-end base-port band from kernel through PTY verification
  landed, including style, layout, text, runtime, widgets, web baseline, demo,
  doctor, harness, and PTY tests.
- hosted-parity working tree after `352c835`
  Shared hosted session state, richer widget interaction support, styled web
  output, evidence writing, doctor dashboards, web/showcase alignment, and CI
  wiring have landed.
- fidelity-and-evidence working tree after `352c835`
  Upstream basis refresh rules, divergence ledgers, runtime replay/evidence
  artifacts, DOM-level hosted verification, benchmark gates, doctor evidence
  output, and CI cloning of the upstream reference corpus have landed. Current
  local verification is `54` headless tests, `4` web tests, and `4` PTY tests
  via `dotnet test FrankenTui.Net.sln --configuration Release`.
- runtime-text-interactivity working tree after `9797fbe`
  Queued runtime sessions, resize-aware app flow, interactive showcase mode,
  layout cache/trace metadata, normalized mixed-script text helpers, styled text
  rendering in widgets, shaping/hyphenation policy recording, and extras
  classification have landed. Current local verification is `58` headless tests,
  `4` web tests, and `5` PTY tests via `dotnet test FrankenTui.Net.sln
  --configuration Release`.
- material-extras working tree after `7335c55`
  Material `FrankenTui.Extras` surfaces now include markdown rendering, export
  helpers, ANSI-aware console cleanup, forms/validation, help/timing widgets,
  traceback rendering, and an extras showcase slice that is exercised in
  terminal, web, and PTY verification. Current local verification is `62`
  headless tests, `5` web tests, and `6` PTY tests via `dotnet test
  FrankenTui.Net.sln --configuration Release`.
- simd-optimization working tree after `cb31a4a`
  `FrankenTui.Simd` now provides optional safe acceleration hooks for row diff
  and word wrap, benchmark/doctors/demo surfaces opt into it explicitly, and
  equivalence coverage keeps the optimized path behaviorally aligned with the
  baseline path. Current local verification is `65` headless tests, `5` web
  tests, and `6` PTY tests via `dotnet test FrankenTui.Net.sln --configuration
  Release`.
- bugs-polish-and-sample-comparison working tree after `628492c`
  Added regression coverage around wide-glyph overwrite cleanup in the
  presenter path, a reusable command runner for verification helpers, and a
  generated Rust-backed shared sample comparison scaffold that runs against the
  managed upstream workspace under `.external/frankentui`. Current local
  verification is `67` headless tests, `5` web tests, and `6` PTY tests via
  `dotnet test FrankenTui.Net.sln --no-restore`.
- inline-mode-divergence investigation working tree after `628492c`
  Added an explicit divergence-triage policy plus a maintained inline-mode
  host ledger after confirming that the current .NET inline path does not yet
  implement the upstream cursor-save/restore and region-clearing contract. The
  observed messy redraw path is currently classified as a local port gap rather
  than an upstream bug candidate.
- upstream-contract-gap audit working tree after `628492c`
  Added a maintained register of in-scope upstream contracts that are still
  partial or absent locally, so follow-on parity work can be driven by explicit
  contract closure rather than by ad hoc memory.
- upstream refresh and showcase inventory after `40c98246`
  Refreshed the managed upstream workspace to
  `40c98246f27f9d174b3923c8df841ba325247dd4`, basis-locked the full
  `ftui-demo-showcase` 45-screen registry and control-plane contract in
  `364-DEM-full-showcase-parity-plan.md`, and reopened active gap rows in
  `246-MAP-upstream-contract-gap-register.md` for the fresh
  `2d25a03d..40c98246` upstream range. This is an inventory and gap-tracking
  landing, not a full implementation closure.
- load-governor baseline after upstream refresh
  Added a local `LoadGovernorConfig` and runtime controller baseline
  corresponding to the upstream adaptive load-governor seam: frame-time target,
  PID gains, anytime-valid e-process settings, hysteresis thresholds, cooldown,
  degradation floor, frame-stat decision fields, and
  `ftui.decision.degradation` telemetry with PID/e-process evidence fields.
  Showcase generic frame evidence now also carries the same controller evidence
  fields for demo runs, the reusable performance HUD renders those controller
  fields for operator surfaces, and the doctor artifact/text path now captures a
  runtime performance snapshot and reports the load-governor level, action,
  reason, PID/e-process values, margins, warmup state, and transition id.
  `RuntimeConformalPredictor` and `RuntimeDegradationCascade` now also port a
  local conformal frame-risk baseline with upstream-shaped bucket keys, exact /
  mode+diff / mode / global / default residual fallback hierarchy, n+1
  conformal quantiles, bounded per-bucket windows, reset counters,
  EMA/nonconformity p99 prediction, fallback guard state, recovery streaks,
  degradation floor, upstream-shaped JSONL schema names, and essential-widget
  filtering. `RuntimePolicyConfig` and its conformal, frame-guard, cascade,
  PID, e-process budget, and budget policy records now mirror the upstream
  policy-config conversion seam and feed `RuntimeExecutionPolicy` defaults for
  load-governor and cascade configuration while preserving explicit
  `LoadGovernor` overrides. `AppRuntime` now observes the policy-backed
  cascade each rendered frame and records conformal bucket, guard state,
  p99/budget/calibration fields, and cascade before/after decisions in
  `RuntimeFrameStats`, `ftui.decision.degradation`, and generic showcase frame
  JSONL evidence. When an explicit `PolicyConfig` is supplied, the runtime render
  path now applies the cascade as a pre-render gate: lower tiers are passed
  through `RuntimeRenderContext.DegradationLevel` for widget adoption, and
  `SKIP_FRAME` bypasses view rendering and presentation while preserving frame
  evidence. Default runtime rendering remains deterministic and evidence-only
  for the wall-clock cascade path. Core local widgets now consume that context
  for the first lower-tier degradation slices: `BlockWidget` uses ASCII borders
  at `SimpleBorders` and clears decorative chrome at `EssentialOnly`,
  `ParagraphWidget`, `ListWidget`, `TabsWidget`, `TextAreaWidget`, and
  `TableWidget` strip styling at `NoStyling` and clear at `Skeleton`,
  `TreeWidget` switches to ASCII branches at `SimpleBorders`, preserves only
  labels at `EssentialOnly`, and clears at `Skeleton`, `ScrollbarWidget`
  switches to ASCII rails at `SimpleBorders` and clears at `EssentialOnly`,
  `BufferInspectorWidget` and `LayoutInspectorWidget` strip styling at
  `NoStyling` and clear at `Skeleton`, `StatusWidget` preserves essential text
  at `Skeleton`, and `ProgressWidget` uses plain `#` bars at `NoStyling`,
  percentage-only output at `EssentialOnly`, and clear-at-`Skeleton` behavior.
  Focused verification is
  `8` focused load-governor/showcase-evidence tests via
  `dotnet test tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore --filter "FullyQualifiedName~LoadGovernorTests|FullyQualifiedName~ShowcaseShellTests.ShowcaseEvidenceJsonlWriterEmitsStableLaunchAndFrameRecords"`,
  `8` focused conformal predictor/cascade tests via
  `dotnet test tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore --filter FullyQualifiedName~DegradationCascadeTests`,
  `5` focused policy-config conversion tests via
  `dotnet test tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore --filter FullyQualifiedName~RuntimePolicyConfigTests`,
  `27` focused runtime/widget degradation tests via
  `dotnet test tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore --filter "FullyQualifiedName~WidgetClearContractTests|FullyQualifiedName~LoadGovernorTests"`,
  plus `dotnet build tools/FrankenTui.Doctor/FrankenTui.Doctor.csproj --no-restore`
  and a successful `dotnet run --project tools/FrankenTui.Doctor/FrankenTui.Doctor.csproj --no-restore -- --format text --width 48 --height 12 --write-artifacts --run-id load-governor-doctor-smoke`.
- markdown hot-path slice after upstream `30e13822` / `b29c3b3b`
  `MarkdownDocumentBuilder` now has a bounded parsed-document cache for
  width-stable/demo redraw reuse, a bounded cached LaTeX-to-Unicode conversion
  path for inline/display math, and GFM table-row parsing that preserves inline
  link and emphasis spans instead of flattening the whole row to muted text.
  Showcase Markdown and extras Markdown surfaces now use the cached parse path.
  `TextRenderer.LayoutViewport` and the `TextRenderOptions` visual-window
  fields now let paragraph/markdown callers render only the requested visual
  line slice while preserving styled runs, covering the local analogue of
  upstream visible-line wrapping. The local dashboard surface now includes a
  cached markdown summary on the same parse/cache path, giving the .NET port a
  dashboard-specific cache parity foothold without claiming the richer upstream
  animated dashboard renderer is fully mirrored. The public `Ui.Markdown`
  helper also routes through the cached parse path, so app-facing Markdown
  construction shares the same cache behavior as showcase/extras surfaces.
  Focused verification is `6` extras tests via
  `dotnet test tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore --filter FullyQualifiedName~ExtrasFeatureTests`
  and `12` combined markdown/text-depth tests via
  `dotnet test tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore --filter "FullyQualifiedName~RuntimeLayoutTextDepthTests|FullyQualifiedName~ExtrasFeatureTests"`.
  The dashboard/cache slice is covered in `17` focused text/extras tests via
  `dotnet test tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore --filter "FullyQualifiedName~StyleLayoutTextRuntimeTests|FullyQualifiedName~RuntimeLayoutTextDepthTests|FullyQualifiedName~ExtrasFeatureTests"`.
- Windows backend fallback guidance after upstream `c00adfb5` / `40c98246`
  Upstream now makes the native backend Unix-only and points Windows users at
  the compatibility path. The .NET demo help, code-level host matrix, and
  `335-HST` ledger now state the local analogue explicitly: Windows showcase
  runs use the managed Windows console backend and must not be routed through
  Unix-native backend assumptions. Focused verification is `3` host/help tests
  via
  `dotnet test tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore --filter "FullyQualifiedName~HostDivergenceTests|FullyQualifiedName~ShowcaseShellTests.ShowcaseCliHelpMentionsHarnessGoldenAndRunIdControls"`.
- SIMD/grapheme diff stabilization after load-governor baseline
  Fixed an order-dependent headless failure where the SIMD row accelerator
  bypassed grapheme-aware comparison and treated equal buffer-local grapheme
  registry ids as changed. Headless verification is now
  `184` tests via
  `dotnet test tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore`.
- demo control-plane launch baseline after showcase inventory
  Added a testable showcase CLI/env parser for the upstream-shaped practical
  launch contract: screen mode including `inline-auto`, inline height/min/max,
  screen selection, guided-tour seed controls, mouse policy, deterministic
  mode/seed parsing, tick cadence, generic auto-exit controls, and pane-workspace
  path selection. Guided-tour landing keyboard controls now adjust start screen
  and speed before launching from the selected state, and active tours support
  upstream-shaped pause/resume, step, speed, and exit keys. Guided-tour landing
  mouse wheel/click controls and active-tour mouse-overlay exit now mirror the
  upstream overlay-level behavior while the full hit-region registry remains
  open. Local chrome mouse routing now covers category-tab clicks, visible
  screen-tab clicks, tab-wheel screen cycling, tour stop on chrome navigation,
  and command-palette priority over chrome routing. Local dashboard highlight
  pane-link routing now switches to linked screens while preserving non-link and
  non-left forwarding behavior; generalized pane hit routing and the full
  upstream frame hit registry remain open. Generic launch/screen-init/frame
  evidence JSONL is now emitted when
  `FTUI_DEMO_EVIDENCE_JSONL` or `FTUI_HARNESS_EVIDENCE_JSONL` is set, and
  interactive frame evidence carries current tour, tour callout/highlight,
  locally resolved highlight rectangle, overlay, palette, screen, and pane
  snapshot state. Local `tour_event` records are emitted when interactive
  input/tick processing changes tour state, and local `palette_event` records
  are emitted when palette state changes. Local `mouse_event` records are now
  emitted for interactive mouse input with upstream-compatible `kind`, `x`,
  `y`, `hit_id`, `action`, `target_screen`, and `current_screen` aliases plus
  local detail fields for palette, chrome, status, tour, and pane-link routing
  evidence. Local `mouse_capture_toggle` records are emitted when capture state
  changes with upstream-compatible state/mode/source/current-screen fields.
  All generic local evidence records now include upstream-style context aliases
  for `seq`, `run_id`, `seed`, `screen_mode`, and `upstream_schema_version`.
  Local startup `screen_init` records now carry `demo_screen_init` stream
  identity, screen id/category, init milliseconds, effect count, and memory
  estimate fields in the shared evidence stream. Local `a11y_event` records
  are emitted when panel, high-contrast, reduced-motion, or large-text state
  changes, with upstream-shaped `panel_toggle`, `high_contrast_toggle`,
  `reduced_motion_toggle`, and `large_text_toggle` names plus tick/screen/flag
  fields. Local `perf_hud_event` records are emitted when the performance HUD
  is toggled, carrying upstream-shaped `hud_toggle` state/tick/screen fields.
  While the perf HUD is visible, local `tick_stats` records are emitted every
  60 ticks with deterministic frame-derived fps/tps, avg/p95/p99/min/max,
  sample count, present/diff timing, changed-cell, and dirty-row fields.
  Local `tick_stall` records are emitted when the interactive loop observes a
  visible-HUD tick gap beyond the local stall threshold, carrying since-ms,
  tick, screen, and reduced-motion fields.
  The exact upstream lazy screen-init lifecycle, separate upstream A11y/perf HUD
  JSONL stream gating, upstream rolling perf sample window, exact upstream
  stall timing semantics, plus exact upstream mouse JSONL schema and hit ids
  remain open.
  Escape now dismisses the local
  command palette before the help overlay, matching the first upstream overlay
  precedence rule, and Ctrl+K now opens the local command palette as advertised
  by upstream and the local help surface. Ctrl+F and Ctrl+Shift+F now provide a
  local command-entry favorite toggle and favorites-only palette filter.
  Ctrl+0 and Ctrl+1..N now clear/set local command-category palette filters.
  The showcase command palette now uses screen-ID entries (`screen:NN`) for the
  45-screen catalog, so favorites, category filtering, preview metadata, and
  Enter execution operate on showcase screens and route to the selected screen.
  Palette ranking now carries an upstream-shaped match-kind order with
  exact/prefix/word-start/substring/fuzzy classes, Bayesian-style score
  factors, tag boosts, compact ranking evidence entries, preview diagnostics,
  and palette JSONL top-result evidence; the full upstream ranking evidence
  ledger UI/type depth remains open.
  Screen 39 now has a local Command Palette Evidence Lab surface with palette
  results, selected-result score/match diagnostics, a compact evidence ledger,
  and upstream-shaped bench/hint footer panels; upstream live match-mode
  controls are partially covered by screen-local `0`-`5` and `m` match-filter
  controls plus frame evidence for the active filter. The deterministic bench
  loop is now `b`-toggled, uses the upstream sample-query cycle, advances on a
  three-tick cadence, and records active bench state/frame/query in frame
  evidence. Screen-local palette mouse handling now
  covers palette-area wheel navigation and left-click selected-command execution
  with `palette_lab` mouse evidence. Screen 39 also now renders a local
  HintRanker-style evidence ledger with expected utility, net value, and VOI
  scores, and frame evidence records the top hint plus a compact hint ledger.
  The lab now uses the upstream 12-item `cmd:*` sample action set instead of
  the global showcase screen catalog. The local render geometry now follows the
  upstream two-line header, 55/45 palette/evidence columns, and split
  bench/hint footer structure; exact styling remains local.
  Ctrl+I now toggles a local evidence ledger overlay, F12 toggles a local debug
  overlay, Escape dismisses evidence before perf/debug/help while preserving
  command-palette-first precedence, Ctrl+P toggles a local performance HUD
  overlay, and Shift+A toggles a local A11y panel with Shift+H/M/L flags for
  contrast, motion, and large text. F6 or `m` toggles local mouse-capture state,
  and the local bottom status row exposes mouse toggle zones for help, palette,
  A11y, perf, debug, evidence, and mouse capture when no overlay is covering it.
  Interactive mouse-capture changes now reconfigure the terminal session SGR
  mouse feature through the backend. Frame evidence records
  evidence/perf/debug/A11y overlay state, A11y flags, and mouse-capture state.
  Shift+H and Shift+L now navigate previous/next screens and stop active tours
  like upstream when the A11y panel is not consuming them, and the local help
  overlay lists those implemented control shortcuts. VFX and
  Mermaid harness launch options are parsed, test-covered, and applied to local
  screen selection, size, and tick cadence. Scripted VFX harness runs now write
  deterministic local JSONL records with upstream-style `vfx_harness_start`
  launch records, input and rendered-buffer frame checksums plus upstream-style
  `vfx_frame`/`frame_idx`/`hash` aliases, and Doom/Quake VFX harness runs now
  emit the upstream FPS input script as `vfx_input` records with shared
  `hash_key` correlation fields. `--vfx-perf`
  emits local `vfx_perf_frame` and `vfx_perf_summary` timing records. A local
  `ShowcaseVfxGoldenRegistry` can now derive stable scenario
  names, save/load hash vectors, compare actual frames against expected hashes,
  extract numeric frame hashes from VFX JSONL, and enforce or update local
  `--vfx-golden` / `FTUI_DEMO_VFX_GOLDEN` hash vectors during scripted VFX
  runs. Local extras now also carry
  `CanvasMode`,
  `CanvasPixelRect.FromCellIntersection`, and Braille
  `CanvasPainter.RenderExcluding` primitives for overlay-aware canvas drawing.
  `ShowcaseVfxEffects` now mirrors the current upstream 19-key effect catalog
  and aliases (`rd`, `attractor`, `flow_field`, `wave`, `model-3d`, `e1m1`,
  etc.) so CLI parsing, display labels, harness JSONL, golden scenario names,
  and deterministic local canvas rendering use the same canonical effect keys.
  The local Visual Effects screen now uses those primitives for a deterministic
  frame-driven Braille canvas instead of static placeholder art, and scripted
  rendering threads parsed `--vfx-effect` names into distinct local canvas
  patterns for every upstream effect key. VFX harness launch/frame records now
  carry normalized effect labels, descriptions, renderer name, canvas mode,
  local quality, and FPS-effect classification fields, and the Quake E1M1
  screen now renders through the same deterministic FPS Braille canvas rather
  than a static text placeholder, with upstream-shaped player/physics,
  controls, mesh-raster, quality-tier, palette, depth-buffer, small-terminal
  fallback, harness, JSONL, and divergence labels tied to
  `QUAKE_E1M1_VERTS`/`QUAKE_E1M1_TRIS`; real `QuakeE1M1State` physics,
  collision, mesh rasterization, clipping/depth pipeline, quality-tier output,
  fire flash, asset-derived geometry rendering, and exact upstream styling
  remain tracked under `364-DEM-E5`. `ShowcaseCliHelp` now centralizes the
  operator-facing `--help` text and advertises the parsed VFX run-id, perf,
  exit-after, seed/size, local golden/update, and Mermaid run-id/seed/size
  harness controls.
  Mermaid harness runs now emit upstream-style `mermaid_harness_start`,
  `mermaid_frame`, and `mermaid_harness_done` records with local `hash_key`,
  `cols`/`rows`, numeric `hash`, `sample_idx`, sample identity, tier/glyph,
  cache, config-hash, link, and parse/layout/route/render timing fields, plus
  upstream-compatible launch `env` and done-record `run_id` fields;
  real Quake asset/raster parity, upstream-equivalent VFX canvas output,
  enforced upstream golden fixtures, and Mermaid recompute/render harness
  behavior remain open. Pane workspace paths now
  load/save a versioned local showcase workspace envelope, migrate raw-v1 local
  workspace snapshots, preserve invalid snapshots, report load/recovery and
  migration state in launch/frame evidence, emit local `pane_workspace_save`
  acknowledgment records with schema version, and surface restored/default/
  recovered status in the Layout Lab screen. `PaneWorkspaceState` now also
  exposes a canonical JSON import/export path with validation for selected
  panes, timeline cursors, checkpoints, ratios, and deterministic byte-stable
  corpus re-export; the showcase envelope path uses that importer for v2 and
  raw-v1 snapshots. Broader upstream lifecycle/corpus coverage remains open.
  Focused verification includes `69` showcase-shell tests via
  `dotnet test tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore --filter FullyQualifiedName~ShowcaseShellTests`
  and `84` operator-surface/showcase-shell tests via
  `dotnet test tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore --filter "FullyQualifiedName~OperatorSurfaceTests|FullyQualifiedName~ShowcaseShellTests"`.
  The Terminal Capabilities screen now has a local upstream-shaped
  matrix/evidence/simulation tri-panel with deterministic capability rows,
  evidence-source labels, diagnostic event names, simulated profile controls,
  and environment summary; exact probing, input-driven mode switching, and
  report export remain tracked under `364-DEM`. The Macro Recorder screen now
  has a local controls/timeline/event-detail/scenario-runner layout with
  deterministic preset macro rows and upstream-shaped key labels; live recorder
  focus routing, mouse focus, playback queue, and exact tracing remain tracked
  under `364-DEM`. The Performance screen now has a local upstream-shaped
  virtualized 10k-item log list, performance stats panel, visible-row counts,
  and navigation status; live keyboard/mouse list mutation and exact upstream
  styling remain tracked under `364-DEM`. The Markdown/Rich Text screen now has
  a local three-column markdown renderer, LLM streaming/detection panel, style
  sampler, Unicode table, and wrap/alignment demo; animated backdrop, live
  scroll/focus controls, exact markdown detection/cache behavior, and full
  syntax/theme depth remain tracked under `364-DEM`. The Mermaid Showcase
  surface now exposes upstream-shaped render mode, palette, guard, zoom/pan,
  viewport override, crossings/symmetry/compactness, and `mermaid_render`
  status evidence in the local library/viewport/controls/metrics/status-log
  layout; full upstream IR renderer, search/inspect modes, debug overlays,
  node navigation, minimap, palette rendering, exact JSONL schema, and full
  mouse/key control parity remain tracked under `364-DEM`. The Mermaid Mega
  Showcase now combines the shared Mermaid viewport with sample-library
  metadata, filter/category summary, controls/keymap panel, node-detail summary,
  and `mermaid_mega_recompute` metrics evidence; full upstream generated sample
  corpus, interactive node navigation, edge highlighting, search/inspect modes,
  minimap, debounced recompute cache, palette rendering, and exact JSONL/keymap
  parity remain tracked under `364-DEM`. The Responsive Layout screen now has
  a local breakpoint indicator, default/custom-threshold notes,
  sidebar/content/aside adaptive layout, visibility rules, responsive values,
  and simulated-width controls; live resize/mouse/key mutation, actual
  `ResponsiveLayout` primitive parity, breakpoint coloring, and exact
  custom-breakpoint state remain tracked under `364-DEM`. The Log Search screen
  now places the local search viewport beside upstream-shaped live-stream
  limits, search/filter key controls, deterministic env flags, diagnostic event
  names, and JSONL field labels; real streaming append/follow mode, inline
  search/filter state machine, case/context toggles, and exact diagnostic
  hook/schema plumbing remain tracked under `364-DEM`. The Notifications screen
  now exposes upstream-shaped trigger instructions, max-visible/max-queued queue
  config, TopRight stack, toast priorities/styles/actions, lifecycle states,
  mouse affordances, and tick expiry/promotion notes; real `NotificationQueue`
  widget state, auto-dismiss timers, action invocation state, mouse hit regions,
  and exact toast rendering remain tracked under `364-DEM`. The Action Timeline
  screen now shows upstream-shaped filters/follow controls, deterministic event
  timeline rows, selected event details, field/evidence payloads,
  max-event/burst constraints, diagnostic span names, and navigation/mouse
  affordances; real bounded ring-buffer state, input recording hooks, filter
  cycling, follow-drop decisions, mouse hit regions, and tracing integration
  remain tracked under `364-DEM`. The Intrinsic Sizing screen now shows the four
  upstream scenarios, effective-width breakpoints, adaptive-sidebar/card/table/
  form rules, width override controls, mouse cycle affordances, and embedded
  pane-studio notes; real content-aware measurement primitives, live scenario
  switching, width override state, embedded pane workspace interaction, and
  exact responsive rendering remain tracked under `364-DEM`. The Layout
  Inspector screen now shows upstream-shaped scenarios, solver steps,
  overlay/tree toggles, constraint-vs-rect records, overflow/underflow statuses,
  mouse/key affordances, and pane-studio notes; real `ConstraintOverlay` /
  `LayoutDebugger` widget parity, live scenario/step state, embedded pane
  interaction, hit-region routing, and exact styled rect visualization remain
  tracked under `364-DEM`. The Advanced Text Editor screen now shows sample
  multiline editor content, line/cursor/match status, search/replace controls,
  undo/redo history, focus-cycle controls, text-editor diagnostic env flags,
  JSONL field labels, and diagnostic event names; real `AdvancedTextEditor`
  state, line-number/selection rendering, search navigation, replace/undo/redo
  mutation, focus hit regions, and exact diagnostics plumbing remain tracked
  under `364-DEM`. The Mouse Playground screen now shows an upstream-shaped
  4x3 hit-target grid, hit IDs, hover/click state, event log kinds,
  stats/overlay state, keyboard controls, diagnostic env flags, JSONL field
  labels, and telemetry hook names; real SGR mouse decoding, frame hit registry
  integration, hover stabilizer state, target click mutation, overlay rendering,
  jitter stats, and exact diagnostic hook plumbing remain tracked under
  `364-DEM`. The Form Validation screen now shows an upstream-shaped nine-field
  registration form, real-time/on-submit mode/status, touched/dirty state, error
  summary, validator inventory, toast queue config, mouse affordances, and
  diagnostic event names; real `Form` / `FormState` mutation, cross-field
  password matching, submit-mode error clearing, notification queue state, mouse
  hit regions, and exact form rendering remain tracked under `364-DEM`. The
  Virtualized Search screen now follows the upstream search-bar plus 70/30
  results/stats layout with 10k-item dataset metadata, deterministic filtered
  results, match positions/scores, keybinding inventory, diagnostic env flags,
  JSONL field labels, and telemetry hook names; real `VirtualizedSearch` state,
  fzy-style incremental filtering/highlighting, keyboard/mouse navigation,
  viewport synchronization, diagnostic log storage, and exact render styling
  remain tracked under `364-DEM`. The Async Tasks screen now follows the
  upstream header/task-queue/details/activity/help layout with task lifecycle
  states, scheduler policy cycle, aging/fairness formula, hazard cancellation
  fields, metrics/invariant names, mouse/key affordances, and diagnostic JSONL
  event names; real `AsyncTaskManager` state mutation, queueing-policy
  scheduler, progress bars, hazard auto-cancel decisions, diagnostic log
  storage, mouse hit regions, and exact upstream styling remain tracked under
  `364-DEM`. The Theme Studio screen now follows the upstream
  presets/token-inspector/status layout with preset names, token categories, hex
  values, contrast/WCAG ratings, JSON and Ghostty export fields,
  focus/navigation/mouse affordances, diagnostic env flags, JSONL field labels,
  and telemetry hook names; real global theme switching, token swatch rendering,
  contrast computation from live palette, export payload generation, mouse hit
  regions, diagnostic log storage, and exact upstream styling remain tracked
  under `364-DEM`. The Time-Travel Studio screen now follows the upstream
  timeline/preview/frame-info layout with frame metadata, playback/recording
  controls, marker/timeline mouse affordances, A/B compare rows, heatmap state,
  checksum/chain fields, JSONL report fields, diagnostic event names, and replay
  invariants; real `SnapshotPlayer` frame buffer storage, delta rendering,
  timeline scrubbing state, marker mutation, compare diff cache, heatmap overlay
  rendering, JSONL file export, diagnostic log storage, and exact upstream
  styling remain tracked under `364-DEM`. The Performance Challenge screen now
  follows the upstream title/metrics/sparkline/render-budget/status layout with
  latency percentiles, tick-sample metadata, stress harness state, degradation
  tier thresholds, forced-tier controls, budget/sparkline mouse affordances,
  JSONL tier-change fields, deterministic env notes, and keybinding labels; real
  `PerformanceHud` ring-buffer metrics, sparkline rendering, stress
  ramp/hold/cooldown state, tier-change logger integration, forced-tier
  mutation, budget adjustment, mouse hit regions, and exact upstream styling
  remain tracked under `364-DEM`. The Explainability Cockpit screen now follows
  the upstream header/source plus diff-strategy, resize-BOCPD, budget-decision,
  decision-timeline, and control layout with deterministic evidence rows, JSONL
  event and field labels, refresh/pause/clear/focus/scroll keybindings, source
  env hints, and mouse/timeline affordance notes; real evidence-file loading,
  JSONL parsing, auto-refresh, pause/clear state mutation, panel focus,
  timeline scroll, mouse hit regions, overlay mode, and exact upstream styling
  remain tracked under `364-DEM`. The i18n Stress Lab screen now follows the
  upstream locale-bar plus String Lookup, Pluralization Rules, RTL Layout
  Mirroring, and Stress Lab panel layout with locale coverage/fallback labels,
  plural-category examples, RTL/LTR mirroring samples, grapheme/display-width
  truncation evidence, stress-report JSONL fields, and locale/panel/sample/export
  keybinding labels; real `StringCatalog`, plural-rule engine, grapheme cursor
  state, sample-set mutation, report file export, mouse locale/panel routing,
  RTL flow reversal, and exact upstream styling remain tracked under `364-DEM`.
  The VOI Overlay screen now follows the upstream centered sampler-debug overlay
  shape with decision, posterior, observation, ledger, and controls sections,
  fallback/inline-auto source labels, VOI posterior/gain/cost/e-value fields,
  ledger decision/observation entries, focus/detail/ledger keybindings, and
  mouse hit-region notes; real `VoiSampler`, `inline_auto_voi_snapshot`, overlay
  widget rendering, focus/expanded state mutation, ledger selection, mouse hit
  testing, runtime observation updates, and exact upstream styling remain
  tracked under `364-DEM`.
  The Inline Mode screen now follows the upstream header plus inline/alt-screen
  comparison story with scrollback-preservation labels, anchor/UI-height/log-rate
  state, deterministic log rows, compare/alt-screen notes, stress limits,
  keybindings, and mouse hit-region labels; real `InlineModeStory` log ring
  buffer, tick-driven stream growth, compare/mode/anchor/height/rate mutation,
  stress burst generation, layout hit testing, inline writer behavior, and exact
  upstream styling remain tracked under `364-DEM`.
  The Accessibility screen now follows the upstream overview, toggles, WCAG
  contrast, live preview, and telemetry layout with high-contrast,
  reduced-motion, and large-text state labels, WCAG threshold rows, preview
  styling labels, app-level toggle action names, telemetry event fields,
  keybindings, and mouse hit-row notes; real app-level `A11ySettings` sync,
  contrast computation from live palette, theme large-text style application,
  telemetry event ring buffer, mouse toggle dispatch, overlay integration, and
  exact upstream styling remain tracked under `364-DEM`.
  The Widget Builder screen now follows the upstream sandbox header plus
  presets, widget tree, live preview, props, export, and mouse-hint layout with
  preset names, widget-kind ids, editable prop labels, JSONL export fields,
  snapshot schema labels, keyboard shortcuts, and mouse routing notes; real
  `WidgetBuilder` preset state, widget config mutation, live widget rendering,
  save/export file IO, props hash generation, list selection state, mouse hit
  testing, and exact upstream styling remain tracked under `364-DEM`.
  The Determinism Lab screen now follows the upstream header plus equivalence,
  scene preview, checks, and report/env layout with Full/DirtyRows/FullRedraw
  strategy rows, checksum timeline, mismatch fields, scenario/run labels, JSONL
  export fields, deterministic env keys, hash-key format, FNV checksum notes,
  keybindings, and mouse hit-region labels; real buffer generation, diff
  application, checksum comparison, scenario simulation, run history/details
  scrolling, JSONL file export, hit-region registration, and exact upstream
  styling remain tracked under `364-DEM`.
  The Hyperlink Playground screen now follows the upstream header plus OSC-8
  links, details/registry, and controls/JSONL layout with upstream link
  labels/URLs, `LinkRegistry` and `HitRegion` labels, OSC-8 open/close evidence,
  hover/action state labels, keybindings, mouse action labels, logging env vars,
  JSONL fields, action names, and hit-id base; real `LinkRegistry` rendering,
  hit-region registration, hover/click/focus state, keyboard activation/copy
  mutation, JSONL file output, and exact upstream styling remain tracked under
  `364-DEM`.
  The Kanban Board screen now uses the upstream app chrome and compact board
  frame for the default 80x24 app snapshot. `tools/FrankenTui.ShowcaseCompare
  --screens 42` reports an exact match against upstream
  `app_kanbanboard_80x24.snap` with 24 equal rows, 0 differing rows, and a
  1.000 local/upstream nonblank-character ratio. Keyboard focus movement, card
  moves, undo, redo, history, redo stack, and rendered card positions still
  mutate through a screen-local `ShowcaseKanbanState`; mouse drag/drop routing,
  card dimming/drop preview styling, cached hit-region registration, and wider
  viewport/interactive evidence remain tracked under `364-DEM`.
  The Live Markdown Editor screen now follows the upstream search/editor/preview
  layout with upstream sample markdown, search query/match labels, focus modes,
  TextArea line-number/soft-wrap status, MarkdownRenderer preview,
  raw-vs-rendered width diff, preview scroll state, MarkdownTheme and
  SyntaxHighlighter notes, keyboard and mouse controls, cached layout-rect
  notes, and JSONL action fields; real mutable `MarkdownLiveEditor` state, rope
  editing, search selection, focus transitions, preview scrolling, diff
  recomputation, mouse pane routing, syntax-highlighted Markdown rendering, and
  exact upstream styling remain tracked under `364-DEM`.
  The Drag & Drop Lab screen now follows the upstream three-mode layout with
  Sortable List, Cross-Container, and Keyboard Drag tabs, deterministic
  Item/File lists, selected/focused list state, keyboard drag manager status,
  drop target/payload labels, announcements, sortable reorder and transfer
  controls, mouse click/scroll/right-click routing notes, small-terminal
  fallback text, cached layout-rect notes, and JSONL action fields; real
  `DragDropDemo` state mutation, `KeyboardDragManager` integration,
  `DropTargetInfo` navigation, list reorder/transfer behavior, announcements
  queue, mouse hit testing, and exact upstream styling remain tracked under
  `364-DEM`.
  The `365-DEM` showcase comparison harness now exists as
  `tools/FrankenTui.ShowcaseCompare`; it renders all 45 local showcase screens
  headlessly at 80x24, compares them with the managed upstream
  `ftui-demo-showcase` app snapshots, and writes row-level evidence under
  `artifacts/showcase-compare`. The first all-screen run found 45/45 app-level
  snapshot differences with no missing upstream basis files, making the
  remaining `364-DEM` simplification gap directly visible; Screen 42 now has a
  focused exact-match lane after the common chrome and Kanban board alignment.
  The showcase host-size bootstrap now follows upstream's normal-app contract:
  `ShowcaseViewportResolver` seeds normal alt-screen runs from the backend
  terminal size, inline runs from host width plus UI height, and preserves
  explicit CLI dimensions plus VFX/Mermaid harness forced sizes. Focused
  verification covers alt-screen, inline, explicit-size, and harness cases.
  Current
  full-solution verification is `336` headless tests,
  `7` PTY tests,
  and `9` web tests via
  `dotnet test FrankenTui.Net.sln --no-restore`.
- web/wasm runner interruption slice after upstream pane-web commits
  Added a local `ShowcaseRunnerCore` in `apps/FrankenTui.Showcase.Wasm` as the
  first platform-independent runner analogue over the shared showcase web
  rendering path. It owns frame/size/scenario state, exposes a step/resize/input
  API, and records upstream-shaped pane interruption lines for native touch
  gesture yield, context loss, and render-stall lifecycle events. Multi-touch
  pane input now releases the active local capture in the runner core and emits
  `phase=native_touch_gesture` / `command=release` evidence; context loss and
  render stall paths similarly clear active pane capture and report
  `context_lost` / `render_stalled` records. This is still not a full
  wasm-bindgen `AppModel` runner. Focused verification is `9` web tests via
  `dotnet test tests/FrankenTui.Tests.Web/FrankenTui.Tests.Web.csproj --no-restore`.
- terminal-contract parity batch after upstream-contract-gap audit
  Added the first explicit terminal-facing contract closure batch: inline mode
  now uses a dedicated writer with DEC save/restore and shrink cleanup, backend
  polling and `write_log` routing are first-class, sanitize-by-default is
  enforced at the writer boundary, and the showcase interactive path now uses
  backend-driven events instead of app-local console polling. Current local
  verification is `74` headless tests, `5` web tests, and `6` PTY tests via
  `dotnet test FrankenTui.Net.sln`.
- diff-evidence-and-routing contract batch after terminal-contract parity batch
  Added the first runtime-facing contract closure batch after inline-mode
  parity: `AppRuntime` now drives an explicit diff strategy selector and
  decision ledger, hosted-parity capture now records replay/trace/diff/runtime
  artifacts through one shared harness, doctor now emits runtime-driven replay
  and manifest artifacts instead of a synthetic one-entry tape, subprocess
  output can now be routed through the one-writer log path, and the upstream
  sample comparison lane now covers event-driven counter flow plus inline-mode
  overlay rows. Current local verification is `81` headless tests, `5` web
  tests, and `6` PTY tests via `dotnet test FrankenTui.Net.sln
  --configuration Release`, plus a successful doctor run with
  `--write-artifacts --write-manifest --run-benchmarks`.
- input-contract closure batch after diff-evidence-and-routing contract batch
  Added `SemanticEvent`, `GestureRecognizer`, `KeybindingResolver`, and
  `ResizeCoalescer` baselines, then routed the hosted-parity demo and runtime
  harness through one shared input engine so policy/gesture/resize behavior is
  exercised in both the interactive sample and the evidence path. Current local
  verification is `86` headless tests, `5` web tests, and `6` PTY tests via
  `dotnet test FrankenTui.Net.sln --no-restore`.
- operator-surface closure batch after input-contract closure batch
  Added the first explicit shared pane workspace, command palette, log search,
  macro recorder, and performance HUD baselines, then integrated them into the
  hosted-parity extras surface so terminal/web/PTY verification exercises them
  as visible demo features rather than only as internal helpers. Current local
  verification is `91` headless tests, `5` web tests, and `6` PTY tests via
  `dotnet test FrankenTui.Net.sln --no-restore`.
- telemetry-mermaid-opentui contract wave after operator-surface closure batch
  Added upstream-shaped telemetry env-var parsing, runtime telemetry event and
  redaction baselines, Mermaid config/showcase contract scaffolding, OpenTUI
  semantic/policy contract loading and validation, doctor/runtime contract
  artifact export, and new headless coverage around all three surfaces. Current
  local verification is `97` headless tests, `5` web tests, and `6` PTY tests
  via `dotnet test FrankenTui.Net.sln --no-restore`.
- telemetry-install-and-opentui-gate depth batch after `e9f96a9`
  Added deterministic telemetry layer/install APIs, PTY env-backed telemetry
  verification, OpenTUI confidence/licensing contract loading, and an
  evidence-driven OpenTUI contract gate that writes clause-level doctor
  artifacts from the managed upstream contract set. Current local verification
  is `100` headless tests, `5` web tests, and `7` PTY tests via `dotnet test
  FrankenTui.Net.sln --no-restore`, plus a successful doctor run with
  `--write-artifacts --write-manifest --run-benchmarks`.
- runtime-input-and-proof wave after `e8cb8c6`
  Added a shared runtime input envelope/controller, rewired the showcase and
  hosted runtime harness through that reusable path, widened telemetry event
  coverage, added concurrent routed-log and sanitizer-fuzz proof, widened the
  shared sample comparison suite with command-palette and log-search samples,
  and added OpenTUI planner/certification projections that now feed doctor and
  the local contract gate. Current local verification is `103` headless tests,
  `5` web tests, and `7` PTY tests via `dotnet test FrankenTui.Net.sln
  --no-restore`, plus a successful doctor run with
  `--write-artifacts --write-manifest --run-benchmarks`.
- operator-runtime depth closure batch after `c41eef2`
  Hosted extras/operator surfaces now carry a real command-palette execution
  loop, live log-search tiering/highlighting, explicit macro
  record/ready/play/loop state with timer-driven playback, and runtime-fed HUD
  snapshots. The interactive showcase now feeds runtime frame stats back into
  the extras surface rather than reconstructing those views from unrelated
  session flags. Current local verification is `108` headless tests, `5` web
  tests, and `7` PTY tests via `dotnet test FrankenTui.Net.sln --no-restore`.
- final local contract-closure wave after `c41eef2`
  The backend boundary is now explicitly split into lifecycle/output/event
  facets, pane state and Mermaid state are persistent interactive hosted/demo
  surfaces, Mermaid has a deterministic parse/render/diagnostic baseline,
  telemetry has a tested OTLP bridge/export path and stronger redaction/macro
  coverage, the shared comparison lane now includes pane/macro/Mermaid samples,
  and Windows CI now refreshes doctor artifacts in addition to Linux. Current
  local verification is `114` headless tests, `5` web tests, and `7` PTY tests
  via `dotnet test FrankenTui.Net.sln --no-restore`, with the remaining
  non-local blocker tracked in
  `2026-03-12-windows-conpty-evidence-blocker.md`.
- CI evidence stabilization and Windows host-artifact review after `90e71de`
  Cross-platform subprocess contract tests now use a portable shell helper,
  hosted CI benchmark overruns are advisory by default rather than hard-failing
  doctor/evidence refreshes, the PTY doctor artifact test now matches the clean
  runner artifact layout, and Windows CI now captures usable doctor/inline
  evidence. That evidence narrows the last gap to interactive ConPTY proof
  specifically, because the current runner is still a redirected Windows
  console rather than a captured Windows Terminal/SSH session.
- external Windows evidence closure after `d39b32e`
  A real Windows host run now provides successful build/doctor output plus
  tooling/extras inline transcripts and an interactive showcase transcript.
  Windows is therefore no longer only `validated-ci`; it is now treated as
  `validated-external`, and the former ConPTY blocker is retained only as a
  historical closure record.
- first upstream-sync wave after `f612df2b`
  Refreshed the managed upstream workspace to `f612df2b9346e3001a854c89ef017e91edd9cf5d`,
  ported render-side certified diff hints into `BufferDiff` and `AppRuntime`,
  added deterministic pane-workspace replay checkpoints/diagnostics, and
  refreshed headless coverage around both surfaces. Current local verification
  is `120` headless tests, `5` web tests, and `7` PTY tests via `dotnet test
  FrankenTui.Net.sln --no-restore`. Remaining upstream drift from the same
  range is now tracked explicitly in `246-MAP-upstream-contract-gap-register.md`.
- render-gauntlet catch-up wave after first `f612df2b` sync batch
  Added a local `FrankenTui.Testing.Harness` render gauntlet, presenter
  equivalence comparer, and layout-reuse contract helper so the repo now has an
  explicit harness baseline for the upstream render-equivalence slice rather
  than only ad hoc render tests and benchmarks. Current local verification is
  `124` headless tests via `dotnet test tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore`.
- runtime-effect catch-up wave after render-gauntlet catch-up wave
  Added a local runtime effect-system baseline with command/subscription
  counters, queue telemetry, reconcile accounting, optional effect labels on
  `AppCommand` and `Subscription`, and runtime/session wiring that records the
  effect and queue signals without changing the public update/program model.
  Current local verification is `127` headless tests via `dotnet test
  tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore`.
- doctor-operator contract catch-up wave after runtime-effect catch-up wave
  Added explicit artifact-manifest taxonomy/validation and failure-signature
  contract helpers, then wired the doctor `--write-manifest` path to emit those
  summaries alongside the existing replay/benchmark/contract artifacts. Current
  local verification is `132` headless tests via `dotnet test
  tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore`.
- fixture-suite catch-up wave after doctor-operator contract catch-up wave
  Added a local fixture registry, deterministic baseline-capture model,
  bounded fixture runner, and rollout scorecard baseline so the harness now
  carries explicit fixture-suite / baseline-capture / rollout-evidence depth
  rather than only the earlier render gauntlet and benchmark gate slices.
  Current local verification is `136` headless tests via `dotnet test
  tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore`.
- doctor-cost-profile catch-up wave after fixture-suite catch-up wave
  Added a local doctor workflow cost-profile baseline plus explicit workflow
  summary artifacts/report fields so the doctor path now emits structured
  cost, stage, and orchestration evidence instead of only the aggregate
  environment report plus replay/benchmark artifacts. Current local
  verification is `138` headless tests plus the doctor PTY artifact path via
  `dotnet test tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore`
  and `dotnet test tests/FrankenTui.Tests.Pty/FrankenTui.Tests.Pty.csproj --no-restore --filter DoctorCanWriteArtifactsAndTextSummary`.
- doctor-bootstrap-and-suite catch-up wave after doctor-cost-profile catch-up wave
  Added explicit bootstrap-summary and suite-report artifacts/report fields so
  the current local doctor flow now records basis/bootstrap stages and a
  machine-readable single-run suite aggregate instead of leaving that
  orchestration depth implicit. Current local verification is `139` headless
  tests plus the doctor PTY artifact path via `dotnet test
  tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore`
  and `dotnet test tests/FrankenTui.Tests.Pty/FrankenTui.Tests.Pty.csproj --no-restore --filter DoctorCanWriteArtifactsAndTextSummary`.
- doctor-runmeta catch-up wave after doctor-bootstrap-and-suite catch-up wave
  Added explicit local `run_meta` and `suite_manifest` artifacts/report fields
  so the current doctor flow now records a concrete run-level contract and a
  machine-readable suite-manifest aggregate rather than only higher-level
  summaries. Current local verification is `140` headless tests plus the doctor
  PTY artifact path via `dotnet test
  tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore`
  and `dotnet test tests/FrankenTui.Tests.Pty/FrankenTui.Tests.Pty.csproj --no-restore --filter DoctorCanWriteArtifactsAndTextSummary`.
- doctor-seed-plan catch-up wave after doctor-runmeta catch-up wave
  Added an explicit local seed-plan artifact/report field with upstream-shaped
  endpoint, retry, timeout, and stage defaults so the doctor bootstrap policy
  is recorded as a concrete contract rather than by convention. Current local
  verification is `141` headless tests plus the doctor PTY artifact path via
  `dotnet test tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore`
  and `dotnet test tests/FrankenTui.Tests.Pty/FrankenTui.Tests.Pty.csproj --no-restore --filter DoctorCanWriteArtifactsAndTextSummary`.
- doctor-seed-execution catch-up wave after doctor-seed-plan catch-up wave
  Added a deterministic seed-execution artifact plus a reusable suite-aggregate
  summary over local `run_meta` entries, so the current doctor flow now records
  bootstrap execution results and aggregate suite state rather than only static
  seed policy plus single-run summaries. Current local verification is `142`
  headless tests plus the doctor PTY artifact path via `dotnet test
  tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore`
  and `dotnet test tests/FrankenTui.Tests.Pty/FrankenTui.Tests.Pty.csproj --no-restore --filter DoctorCanWriteArtifactsAndTextSummary`.
- doctor-suite-report catch-up wave after doctor-seed-execution catch-up wave
  Added a stable doctor-suite workspace under `artifacts/replay/doctor-suite`,
  persisted each local `run_meta` into that suite directory, and rebuilt the
  suite manifest, suite aggregate, JSON suite report, and HTML suite index from
  the collected suite-run corpus rather than only from the current run. Current
  local verification is `143` headless tests, `5` web tests, and `7` PTY tests
  via `dotnet test FrankenTui.Net.sln --no-restore`.
- doctor-actual-seed catch-up wave after doctor-suite-report catch-up wave
  Added an optional real JSON-RPC doctor seed runner behind `--seed-mode actual`
  with retry, poll, and stage logging against the configured MCP endpoint, while
  keeping simulation as the default local doctor path. Current local
  verification is `145` headless tests, `5` web tests, and `7` PTY tests via
  `dotnet test FrankenTui.Net.sln --no-restore`.
- asupersync-evidence catch-up wave after doctor-actual-seed catch-up wave
  Added an explicit orchestration-only Asupersync evidence artifact with lane,
  fallback, and correlation fields so the local runtime/doctor stack now emits
  the newer lane-selection contract instead of only implicit legacy behavior.
  Current local verification is `146` headless tests, `5` web tests, and `7`
  PTY tests via `dotnet test FrankenTui.Net.sln --no-restore`.
- runtime-fault-and-lifecycle catch-up wave after asupersync-evidence catch-up wave
  Extended the local runtime effect-system baseline with command/subscription
  cancellation and failure accounting plus declared subscription start/stop
  lifecycle telemetry, so the remaining upstream `effect_system` /
  `subscription` row is now treated as an execution-model divergence rather
  than an open missing contract. Current local verification is `148` headless
  tests, `5` web tests, and `7` PTY tests via `dotnet test FrankenTui.Net.sln
  --no-restore`.

## Status

### 220-PTH Pathfinder Baseline

| Code | Status | Note |
| --- | --- | --- |
| `221-PTH` | `completed` | Upstream crate-to-project map is recorded in `240-MAP`. |
| `222-PTH` | `completed` | Initial .NET decomposition is captured in `230-ARC`. |
| `223-PTH` | `completed` | Initial parity corpus was captured and has now started to turn into local tests. |
| `224-PTH` | `completed` | Supported terminal matrix and inherited host assumptions are recorded and reflected in the host matrix code. |
| `225-PTH` | `completed` | Verification doctrine and mode split are in place. |
| `226-PTH` | `completed` | Demo, web/wasm, and doctor surfaces are explicitly in scope and now have baseline implementations. |

### 230-ARC Repository And Build Skeleton

| Code | Status | Note |
| --- | --- | --- |
| `231-ARC` | `completed` | Solution and project layout are established. |
| `232-ARC` | `completed` | Package boundaries are live in the solution graph. |
| `233-ARC` | `completed` | Provenance and divergence recording conventions exist at repo-doc level. |
| `234-ARC` | `completed` | Current baseline stays NativeAOT-conscious and dependency-light. |
| `235-ARC` | `completed` | Artifact layout exists under `artifacts/`, now including the comparison lane. |

### 240-MAP Provenance And Syncability

| Code | Status | Note |
| --- | --- | --- |
| `241-MAP` | `completed` | Module mapping ledger exists. |
| `242-MAP` | `completed` | Upstream basis recording, refresh rules, and bug/artifact triage expectations are explicit in `242-MAP-upstream-sync-workflow.md`. |
| `243-MAP` | `completed` | `.external` refresh and reconciliation workflow is explicit and is exercised in CI by cloning the upstream reference corpus. |
| `244-MAP` | `completed` | Divergence recording now has explicit ledgers, an index, and a triage policy rather than only batch notes. |

### 250-KRN Terminal Kernel And Core Primitives

| Code | Status | Note |
| --- | --- | --- |
| `251-KRN` | `completed` | Geometry, cursor, capabilities, events, and key/mouse primitives are present. |
| `252-KRN` | `completed` | Session ownership and cleanup semantics are present; OS-native raw-mode fidelity gap is tracked in the blocker note. |
| `253-KRN` | `completed` | Inline-mode behavior now uses a dedicated writer with DEC save/restore, row clearing, routed inline logs, and backend-driven polling rather than newline-based repainting. |
| `254-KRN` | `completed` | Backend abstractions now include session configuration, feature toggles, bounded polling, routed log output, and subprocess-forwardable log routing across memory and console backends. |
| `255-KRN` | `completed` | Capability probing, overrides, semantic hover stabilization, and policy helpers are implemented. |

### 260-RND Render Kernel

| Code | Status | Note |
| --- | --- | --- |
| `261-RND` | `completed` | Cells, buffers, grapheme handling, and frame helpers are implemented. |
| `262-RND` | `completed` | Diff computation and run grouping are implemented. |
| `263-RND` | `completed` | ANSI emission, presenter logic, hyperlinks, sanitization, and output budgeting are implemented. |
| `264-RND` | `completed` | Headless render helpers and terminal-model verification hooks are implemented. |

### 270-STY Style And Theme System

| Code | Status | Note |
| --- | --- | --- |
| `271-STY` | `completed` | Base color/style primitives and `UiStyle` are implemented. |
| `272-STY` | `completed` | Theme and stylesheet composition are present. |
| `273-STY` | `completed` | Interactive and table/theme behaviors needed by current widgets are present at baseline level. |

### 280-LYT Layout System

| Code | Status | Note |
| --- | --- | --- |
| `281-LYT` | `completed` | Base layout primitives and directionality are implemented. |
| `282-LYT` | `completed` | Constraint-based split solver is implemented. |
| `283-LYT` | `completed` | Layout cache, richer trace metadata, and improved inspector output now provide cache/debug/repro support. |
| `284-LYT` | `completed` | Responsive breakpoint baseline is present. |

### 290-TXT Text System

| Code | Status | Note |
| --- | --- | --- |
| `291-TXT` | `completed` | Spans, lines, documents, wrapping, cursoring, and basic markup/view logic are present. |
| `292-TXT` | `completed` | Width calculation and wrapping policy are implemented. |
| `293-TXT` | `completed` | Normalized search and mixed-script direction/script segmentation are now implemented and tested. |
| `294-TXT` | `completed` | Shaping and hyphenation policy decisions are now explicit in code and in `294-TXT-shaping-and-hyphenation-evaluation.md`. |
| `295-TXT` | `completed` | Styled text rendering and markup/fallback integration are now used directly by paragraph and textarea widgets. |

### 300-RTM Runtime And Execution Loop

| Code | Status | Note |
| --- | --- | --- |
| `301-RTM` | `completed` | Runtime skeleton, view rendering, and backend presentation path are implemented. |
| `302-RTM` | `completed` | Queued app sessions, resize-aware flow, cancellation-aware loops, and interactive showcase use of the runtime are now implemented. |
| `303-RTM` | `completed` | Deterministic runtime trace, replay tape, JSON round-trip, hosted-parity runtime capture, and evidence-manifest integration are implemented and tested. |
| `304-RTM` | `completed` | Runtime execution policy switches, diff-decision evidence, doctor runtime capture, and evidence/baseline decisions are now explicit in code and documented in `304-RTM-determinism-and-evidence.md`. |
| `305-RTM` | `completed` | App simulator and headless runtime helpers are implemented. |

### 310-WGT Widget Surface

| Code | Status | Note |
| --- | --- | --- |
| `311-WGT` | `completed` | Foundational widgets and composition primitives have landed at baseline level. |
| `312-WGT` | `completed` | Shared focus order, pointer state, live-region messaging, and scripted hosted-parity session state are implemented. |
| `313-WGT` | `completed` | Inspector and debug-oriented widgets are present. |
| `314-WGT` | `completed` | Hosted surfaces now carry language, direction, and accessibility snapshot data through shared view/state helpers. |
| `315-WGT` | `completed` | Hosted parity and doctor dashboard surfaces now exercise operator-facing widget compositions directly. |

### 320-API Public Facade And Package Surface

| Code | Status | Note |
| --- | --- | --- |
| `321-API` | `completed` | A first coherent public facade exists in `src/FrankenTui/Ui.cs`. |
| `322-API` | `completed` | `Ui` now exposes hosted-parity and web rendering entry points used by showcase and tests. |

### 330-HST Terminal Host Backends And Platforms

| Code | Status | Note |
| --- | --- | --- |
| `331-HST` | `completed` | Supported terminal matrix and host policy baseline are recorded and implemented. |
| `332-HST` | `completed` | Unix/macOS/Linux host behavior has a working baseline through the console backend and PTY tests. |
| `333-HST` | `completed` | Windows host contract baseline exists, Windows CI now refreshes doctor artifacts, and the only remaining non-local evidence blocker is tracked in `2026-03-12-windows-conpty-evidence-blocker.md`. |
| `334-HST` | `completed` | PTY test-host support is implemented. |
| `335-HST` | `completed` | Host validation status, remoting classification, known divergences, evidence sources, and capability override policy are maintained in code and in `335-HST-host-divergence-ledger.md`. |

### 340-WEB Web And WASM Host Surfaces

| Code | Status | Note |
| --- | --- | --- |
| `341-WEB` | `completed` | Web execution boundary is defined in code through the shared render/runtime path and web host. |
| `342-WEB` | `completed` | Deterministic web host baseline exists. |
| `343-WEB` | `completed` | Showcase wasm rendering now rides the shared hosted-parity session and web document path. |
| `344-WEB` | `completed` | Boundary is now recorded explicitly in `344-WEB-web-boundary.md`. |

### 350-VRF Verification Stack

| Code | Status | Note |
| --- | --- | --- |
| `351-VRF` | `completed` | Baseline test and artifact infrastructure is in place. |
| `352-VRF` | `completed` | Kernel/render headless tests are active and passing. |
| `353-VRF` | `completed` | Widget/runtime headless tests and simulator checks are active and passing at baseline level. |
| `354-VRF` | `completed` | Invariant and corpus-backed regression now cover runtime replay, diff-decision ledgers, manifest contracts, benchmark fixtures, and host metadata. |
| `355-VRF` | `completed` | PTY-backed integration tests are active and passing on the current Unix workspace, now including explicit inline save/restore evidence. Cross-platform evidence gaps are documented in the blocker note. |
| `356-VRF` | `completed` | Hosted web parity tests now include deterministic DOM-level parsing through `WebDomRunner` in addition to render-equivalence checks. |
| `357-VRF` | `completed` | Evidence manifests, replay artifacts, contract-shape checks, and the shared sample comparison lane now compare event-driven counter, unicode, wide-overwrite, and inline-overlay samples against the managed upstream corpus under `.external/frankentui`. |
| `358-VRF` | `completed` | Benchmark runner, tracked budget fixture, doctor gate, and artifact writing are now active. |
| `359-VRF` | `completed` | GitHub Actions now restores, builds, tests, clones the upstream reference corpus, and refreshes doctor evidence artifacts across Linux and Windows. |

### 360-DEM Demo And Showcase Surface

| Code | Status | Note |
| --- | --- | --- |
| `361-DEM` | `completed` | The hosted-parity showcase slice remains formalized as a shared session and reusable surface for web/tests. |
| `362-DEM` | `completed` | Terminal showcase now exposes an upstream-shaped screen catalog with guided-tour launch, screen-based scripted rendering, and interactive navigation, exercised by PTY/headless verification. |
| `363-DEM` | `completed` | Terminal and web showcase alignment remains explicit through shared rendering primitives, while the terminal app now layers an upstream-shaped shell over those shared surfaces. |
| `364-DEM` | `in progress` | Full screen-level and control-plane parity work beyond the current shell-level baseline is tracked in `364-DEM-full-showcase-parity-plan.md`; `364-DEM-A` inventory, `364-DEM-C` launch-control baseline, and `365-DEM` app-snapshot comparison harness are landed. |

### 370-EXT Extras And Optional Optimization Surface

| Code | Status | Note |
| --- | --- | --- |
| `371-EXT` | `completed` | Extras classification is now recorded in `371-EXT-extras-classification.md`. |
| `372-EXT` | `completed` | The in-sequence material extras slice is now landed in `FrankenTui.Extras` and recorded in `372-EXT-material-extras-slice.md`. |
| `373-EXT` | `completed` | The optional safe optimization surface is now landed in `FrankenTui.Simd` and recorded in `373-EXT-simd-optimization-surface.md`. |

### 380-TOL Doctor And Operational Tooling

| Code | Status | Note |
| --- | --- | --- |
| `381-TOL` | `completed` | Doctor now reports environment state, host validation status, divergence notes, and recommendations. |
| `382-TOL` | `completed` | Harness and doctor flows now write hosted-parity JSON, text, HTML, replay, benchmark, manifest, and comparison artifacts. |
| `383-TOL` | `completed` | Maintainer-facing doctor output now exists in JSON and readable text form with evidence-oriented status details. |
| `384-TOL` | `completed` | CI now refreshes doctor artifacts, benchmark artifacts, and replay evidence on Linux. |

### 390-DOC FrankenTui.Net Implementation Docs

| Code | Status | Note |
| --- | --- | --- |
| `391-DOC` | `completed` | .NET-specific implementation notes are recorded in `391-DOC-dotnet-implementation-notes.md`. |
| `392-DOC` | `completed` | Divergence, sync, host, and verification docs now exist as maintained ledgers and workflows rather than only blockers. |
| `393-DOC` | `completed` | README and AGENTS guidance now point at the status ledger and the new sync/evidence/host docs. |
