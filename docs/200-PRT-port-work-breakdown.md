# 200-PRT FrankenTui.Net Port Work Breakdown

## 210-PRT Planning Intent

This is the initial dependency-based hierarchy for porting FrankenTUI into a
coherent .NET implementation. It covers core implementation work, verification
work, tooling, host surfaces, and supporting documentation/provenance work.

The hierarchy is intentionally broad enough to cover the full port surface, but
it is ordered by enabling dependencies rather than by dates or release phases.

## 220-PTH Pathfinder Baseline

- `221-PTH` Capture the upstream crate-to-module map that FrankenTui.Net will
  track.
  Depends on: `110-UPR`
- `222-PTH` Decide the initial .NET solution/package decomposition that best
  preserves upstream recognizability without forcing one-crate-per-project
  cargo mirroring.
  Depends on: `221-PTH`
- `223-PTH` Establish the first upstream parity corpus to reuse in the port:
  sample frames, ANSI traces, PTY cases, web/wasm cases, doctor evidence cases,
  and selected golden tests.
  Depends on: `221-PTH`
- `224-PTH` Capture the supported terminal matrix, capability-profile
  assumptions, and host-behavior contracts that FrankenTui.Net should inherit
  from upstream.
  Depends on: `110-UPR`
- `225-PTH` Define the .NET verification doctrine: no-mock policy, headless vs
  capture vs PTY vs web-host testing boundaries, and artifact conventions.
  Depends on: `110-UPR`
- `226-PTH` Map the intended .NET scope for `doctor_frankentui`, `ftui-web`,
  `ftui-showcase-wasm`, and `ftui-demo-showcase`.
  Depends on: `110-UPR`

## 230-ARC Repository And Build Skeleton

- `231-ARC` Create the .NET solution, project layout, and naming conventions for
  the core port.
  Depends on: `222-PTH`
- `232-ARC` Define package boundaries for public facade, core kernel, renderer,
  layout/text/style subsystems, runtime, widgets, terminal hosts, web/wasm
  hosts, tooling, and test utilities.
  Depends on: `231-ARC`
- `233-ARC` Set repository conventions for upstream references, divergence notes,
  and source-level provenance markers.
  Depends on: `231-ARC`
- `234-ARC` Set NativeAOT-conscious build constraints and dependency rules for
  the core.
  Depends on: `231-ARC`
- `235-ARC` Define repository artifact layout for snapshots, traces, replay
  logs, benchmark baselines, and other parity evidence.
  Depends on: `231-ARC`, `225-PTH`

## 240-MAP Provenance And Syncability

- `241-MAP` Create a module mapping ledger from upstream crates/modules to .NET
  namespaces, assemblies, and test projects.
  Depends on: `233-ARC`
- `242-MAP` Define how each porting wave records its upstream commit basis and
  deliberate divergences.
  Depends on: `241-MAP`
- `243-MAP` Define the refresh workflow for updating `.external/frankentui` and
  reconciling local implementation against upstream changes.
  Depends on: `241-MAP`
- `244-MAP` Maintain divergence ledgers for terminal hosts, web/wasm surfaces,
  and tooling surfaces where direct transliteration is not possible.
  Depends on: `242-MAP`

## 250-KRN Terminal Kernel And Core Primitives

- `251-KRN` Port geometry, cursor, event, key sequence, and terminal capability
  primitives.
  Depends on: `232-ARC`
- `252-KRN` Port terminal session ownership, raw mode/lifecycle setup, and
  cleanup semantics.
  Depends on: `251-KRN`, `234-ARC`
- `253-KRN` Port inline-mode behavior, event parsing, coalescing, and
  capability-driven behavior.
  Depends on: `252-KRN`
- `254-KRN` Port backend abstractions needed by runtime and host backends.
  Depends on: `251-KRN`
- `255-KRN` Port capability probing, override behavior, semantic-event layers,
  and input/hover stabilization behavior that affect visible semantics.
  Depends on: `252-KRN`, `253-KRN`

## 260-RND Render Kernel

- `261-RND` Port cells, buffers, grapheme handling, and frame primitives.
  Depends on: `251-KRN`
- `262-RND` Port diff computation and diff strategy contracts.
  Depends on: `261-RND`
- `263-RND` Port ANSI/presenter emission, link handling, sanitization, and
  output budgeting.
  Depends on: `262-RND`, `252-KRN`
- `264-RND` Port headless render helpers and terminal-model verification hooks.
  Depends on: `261-RND`, `262-RND`

## 270-STY Style And Theme System

- `271-STY` Port color, style, and attribute primitives.
  Depends on: `261-RND`
- `272-STY` Port theme and stylesheet composition.
  Depends on: `271-STY`
- `273-STY` Port table/theme and interactive style behaviors needed by widgets
  and showcase screens.
  Depends on: `272-STY`

## 280-LYT Layout System

- `281-LYT` Port base layout primitives, directionality, and pane/workspace
  models.
  Depends on: `251-KRN`
- `282-LYT` Port flex/grid solvers and constraint behavior.
  Depends on: `281-LYT`
- `283-LYT` Port incremental/cache/debug/repro tooling that stabilizes layout
  correctness.
  Depends on: `282-LYT`
- `284-LYT` Port responsive/breakpoint behavior only after base solver parity is
  established.
  Depends on: `282-LYT`

## 290-TXT Text System

- `291-TXT` Port spans/segments, wrapping, cursoring, editing, and view logic.
  Depends on: `271-STY`, `282-LYT`
- `292-TXT` Port width calculation, cache policy, and Unicode/grapheme rules.
  Depends on: `291-TXT`
- `293-TXT` Port search, rope, normalization, bidi, and script segmentation.
  Depends on: `291-TXT`
- `294-TXT` Evaluate shaping and hyphenation support in .NET with fidelity and
  NativeAOT constraints kept explicit.
  Depends on: `293-TXT`, `234-ARC`
- `295-TXT` Port markup, shaped-render integration, and fallback text paths
  used by widgets, demos, tools, and web hosts.
  Depends on: `291-TXT`, `294-TXT`

## 300-RTM Runtime And Execution Loop

- `301-RTM` Port the app/model/update/view runtime skeleton and terminal writer
  ownership model.
  Depends on: `254-KRN`, `263-RND`, `282-LYT`, `291-TXT`
- `302-RTM` Port subscriptions, commands, cancellation, queueing, and resize
  handling.
  Depends on: `301-RTM`
- `303-RTM` Port replay, trace, and determinism facilities needed for parity
  verification.
  Depends on: `301-RTM`
- `304-RTM` Evaluate which upstream policy/telemetry/state-persistence features
  are later in-sequence but still in-scope runtime subsystems, including policy,
  telemetry, event trace, and state persistence.
  Depends on: `302-RTM`, `303-RTM`
- `305-RTM` Port simulator and headless execution helpers used by verification,
  demos, and tooling.
  Depends on: `301-RTM`, `303-RTM`

## 310-WGT Widget Surface

- `311-WGT` Port foundational widgets and composition primitives: block,
  paragraph, padding, panel, list, tabs, table, textarea, tree, scrollbar, and
  status/progress widgets.
  Depends on: `271-STY`, `282-LYT`, `291-TXT`, `301-RTM`
- `312-WGT` Port input/mouse/focus/state helpers required by interactive widgets.
  Depends on: `311-WGT`
- `313-WGT` Port diagnostic/debug/inspector widgets that are useful for parity
  work and implementation debugging.
  Depends on: `312-WGT`
- `314-WGT` Port accessibility and i18n integration points used by the widget
  layer.
  Depends on: `311-WGT`
- `315-WGT` Port advanced widgets and operator-facing surfaces needed by demo,
  doctor, and verification workflows.
  Depends on: `312-WGT`, `313-WGT`

## 320-API Public Facade And Package Surface

- `321-API` Define the public facade and package export surface corresponding to
  upstream `ftui` without forcing strict Rust API compatibility.
  Depends on: `232-ARC`, `301-RTM`, `311-WGT`
- `322-API` Stabilize consumer-facing package composition so demos, tools,
  terminal hosts, and web hosts can share the same core-facing entry points.
  Depends on: `321-API`

## 330-HST Terminal Host Backends And Platforms

- `331-HST` Define the supported terminal matrix, capability-profile policy, and
  per-platform behavior contracts for Windows, macOS, and Linux terminal
  targets.
  Depends on: `224-PTH`, `252-KRN`, `255-KRN`
- `332-HST` Implement Unix/macOS/Linux terminal backend behavior and capability
  handling.
  Depends on: `331-HST`, `254-KRN`, `301-RTM`
- `333-HST` Implement Windows terminal backend behavior and platform-specific
  divergence handling.
  Depends on: `331-HST`, `254-KRN`, `301-RTM`
- `334-HST` Implement PTY and other test-host support needed for integration,
  replay, and evidence capture.
  Depends on: `332-HST`, `333-HST`
- `335-HST` Maintain per-platform divergence records, capability overrides, and
  testable support assumptions as first-class artifacts.
  Depends on: `244-MAP`, `331-HST`, `332-HST`, `333-HST`

## 340-WEB Web And WASM Host Surfaces

- `341-WEB` Define the web/wasm execution model, input/session contracts, and
  core-sharing boundary for the .NET port.
  Depends on: `226-PTH`, `301-RTM`
- `342-WEB` Port the `ftui-web`-equivalent deterministic web host surface.
  Depends on: `341-WEB`, `263-RND`, `301-RTM`
- `343-WEB` Port the showcase wasm runner and related host integration
  corresponding to `ftui-showcase-wasm`.
  Depends on: `342-WEB`
- `344-WEB` Define and preserve the boundary between in-scope web/wasm code and
  reference-only external website or remote-protocol assets.
  Depends on: `242-MAP`, `341-WEB`

## 350-VRF Verification Stack

- `351-VRF` Implement the baseline .NET test and artifact infrastructure from
  the planning doctrine: no-mock policy, layer-to-test mapping, artifact
  formats, and automation scaffolding.
  Depends on: `225-PTH`, `235-ARC`
- `352-VRF` Build kernel and render headless golden tests for buffers, diffs,
  ANSI emission, and terminal-model results.
  Depends on: `264-RND`, `351-VRF`
- `353-VRF` Build widget and runtime headless golden tests, simulator checks,
  and other non-PTY behavior verification.
  Depends on: `305-RTM`, `311-WGT`, `351-VRF`
- `354-VRF` Build property, invariant, and fuzz/corpus regression tests for
  layout, text, input parsing, runtime determinism, and selected tooling logic.
  Depends on: `253-KRN`, `283-LYT`, `293-TXT`, `303-RTM`, `351-VRF`
- `355-VRF` Build PTY-backed integration tests for terminal lifecycle, inline
  mode, scrollback, one-writer behavior, and cross-platform host expectations.
  Depends on: `332-HST`, `333-HST`, `334-HST`, `351-VRF`
- `356-VRF` Build web/wasm parity tests and browser/node runners for the web
  host surface.
  Depends on: `342-WEB`, `343-WEB`, `351-VRF`
- `357-VRF` Build replay, trace, and evidence workflows that compare .NET
  behavior with upstream reference behavior across terminal, web, demo, and tool
  surfaces.
  Depends on: `223-PTH`, `303-RTM`, `355-VRF`, `356-VRF`
- `358-VRF` Build benchmarks, performance baselines, and regression gates for
  render, layout, text, runtime, widgets, and selected tool/web hot paths.
  Depends on: `262-RND`, `282-LYT`, `292-TXT`, `301-RTM`, `342-WEB`, `351-VRF`
- `359-VRF` Wire CI and regression runners for unit, property, PTY, web/wasm,
  replay, tooling, and performance gates as the relevant layers come online.
  Depends on: `351-VRF`

## 360-DEM Demo And Showcase Surface

- `361-DEM` Identify the minimum upstream demo/showcase surface needed to
  exercise the core port credibly.
  Depends on: `226-PTH`, `315-WGT`
- `362-DEM` Port the terminal demo/showcase surface corresponding to
  `ftui-demo-showcase`.
  Depends on: `361-DEM`, `355-VRF`
- `363-DEM` Align terminal and web showcase runners so they exercise the same
  core semantics and parity corpus.
  Depends on: `343-WEB`, `362-DEM`, `357-VRF`
- `364-DEM` Deepen the local showcase from shell-level parity to full practical
  screen, chrome, control-plane, verification, and host parity with upstream
  `ftui-demo-showcase`.
  Depends on: `302-RTM`, `315-WGT`, `333-HST`, `343-WEB`, `357-VRF`, `362-DEM`,
  `363-DEM`

## 370-EXT Extras And Optional Optimization Surface

- `371-EXT` Classify upstream extras into parity-critical, later in-sequence,
  and optional slices without declaring them out of scope.
  Depends on: `315-WGT`, `304-RTM`
- `372-EXT` Port feature-gated extras that are materially important to the
  FrankenTui.Net library surface.
  Depends on: `371-EXT`
- `373-EXT` Evaluate and port `ftui-simd`-equivalent optional optimizations only
  after benchmark baselines exist.
  Depends on: `358-VRF`

## 380-TOL Doctor And Operational Tooling

- `381-TOL` Define the scope mapping from upstream `doctor_frankentui` to
  FrankenTui.Net tooling responsibilities, including where direct ports end and
  .NET-native adaptations begin.
  Depends on: `226-PTH`, `242-MAP`
- `382-TOL` Port capture, seeding, suite reporting, replay triage, and
  diagnostics flows that materially support porting and verification.
  Depends on: `381-TOL`, `357-VRF`
- `383-TOL` Port contract, evidence, and maintainer-facing reporting surfaces
  needed for agent and maintainer workflows.
  Depends on: `382-TOL`
- `384-TOL` Align doctor tooling with CI, parity evidence, demo/showcase
  runners, and divergence reporting.
  Depends on: `359-VRF`, `362-DEM`, `383-TOL`

## 390-DOC FrankenTui.Net Implementation Docs

- `391-DOC` Write .NET-specific implementation docs only where upstream docs are
  no longer sufficient because of runtime, platform, tool, or API differences.
  Depends on: `233-ARC`, `242-MAP`
- `392-DOC` Maintain local docs for divergence records, verification approach,
  host/tool surface boundaries, and port status.
  Depends on: `244-MAP`, `351-VRF`
- `393-DOC` Keep README and agent guidance aligned with the actual port surface
  and current execution doctrine.
  Depends on: `391-DOC`
