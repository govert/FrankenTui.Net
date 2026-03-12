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
  working tree after external Windows evidence closure
- Current upstream workspace basis:
  `7a91089366bd4644e086d5a422cb76b052e3de17`
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
| `361-DEM` | `completed` | The hosted-parity showcase slice is now formalized as a shared session and surface. |
| `362-DEM` | `completed` | Terminal showcase now supports scenario/frame-driven scripted rendering, a small interactive mode, and an extras scenario, exercised by PTY tests. |
| `363-DEM` | `completed` | Terminal and web showcase alignment is now explicit through shared `Ui`/showcase rendering and web tests. |

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
