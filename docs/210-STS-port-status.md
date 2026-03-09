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
separately in [2026-03-09-big-batch-blockers.md](./2026-03-09-big-batch-blockers.md).

## Current Basis

- Current status basis commit: `3812dca`
- Current upstream workspace basis:
  `7a91089366bd4644e086d5a422cb76b052e3de17`
- Last full verification pass at status update time:
  `dotnet test FrankenTui.Net.sln`

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
| `235-ARC` | `completed` | Artifact layout exists under `artifacts/`. |

### 240-MAP Provenance And Syncability

| Code | Status | Note |
| --- | --- | --- |
| `241-MAP` | `completed` | Module mapping ledger exists. |
| `242-MAP` | `in progress` | Batch commits and notes carry basis/divergence context, but the workflow is still informal. |
| `243-MAP` | `in progress` | `.external` rebuild instructions exist, but refresh/reconciliation workflow is not yet fully explicit. |
| `244-MAP` | `in progress` | Divergence tracking has started through blocker notes and host-policy docs, but not yet as separate ledgers per surface. |

### 250-KRN Terminal Kernel And Core Primitives

| Code | Status | Note |
| --- | --- | --- |
| `251-KRN` | `completed` | Geometry, cursor, capabilities, events, and key/mouse primitives are present. |
| `252-KRN` | `completed` | Session ownership and cleanup semantics are present; OS-native raw-mode fidelity gap is tracked in the blocker note. |
| `253-KRN` | `completed` | Inline-mode behavior, event parsing, paste, focus, mouse parsing, and coalescing are implemented at baseline level. |
| `254-KRN` | `completed` | Backend abstractions and memory/console backends are in place. |
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
| `283-LYT` | `in progress` | Trace and inspector support exist; incremental/cache-oriented layout tooling is still thin. |
| `284-LYT` | `completed` | Responsive breakpoint baseline is present. |

### 290-TXT Text System

| Code | Status | Note |
| --- | --- | --- |
| `291-TXT` | `completed` | Spans, lines, documents, wrapping, cursoring, and basic markup/view logic are present. |
| `292-TXT` | `completed` | Width calculation and wrapping policy are implemented. |
| `293-TXT` | `in progress` | Search and normalization landed; rope, bidi, and deeper script segmentation are not yet present. |
| `294-TXT` | `not started` | Shaping and hyphenation evaluation has not yet been recorded. |
| `295-TXT` | `in progress` | Markup and fallback text paths exist; shaped-render integration does not. |

### 300-RTM Runtime And Execution Loop

| Code | Status | Note |
| --- | --- | --- |
| `301-RTM` | `completed` | Runtime skeleton, view rendering, and backend presentation path are implemented. |
| `302-RTM` | `in progress` | Commands and subscriptions exist; deeper queueing, cancellation, and resize semantics are still light. |
| `303-RTM` | `in progress` | Trace and replay scaffolding exist; richer determinism tooling remains to be built. |
| `304-RTM` | `not started` | Later runtime policy/telemetry/state subsystems are not yet evaluated. |
| `305-RTM` | `completed` | App simulator and headless runtime helpers are implemented. |

### 310-WGT Widget Surface

| Code | Status | Note |
| --- | --- | --- |
| `311-WGT` | `completed` | Foundational widgets and composition primitives have landed at baseline level. |
| `312-WGT` | `in progress` | Widget input/focus/state support is started but still shallow. |
| `313-WGT` | `completed` | Inspector and debug-oriented widgets are present. |
| `314-WGT` | `in progress` | A11y and i18n integration points exist, but widget-layer integration is still partial. |
| `315-WGT` | `in progress` | Operator-facing/dashboard-style surfaces have started, but deeper advanced widgets remain. |

### 320-API Public Facade And Package Surface

| Code | Status | Note |
| --- | --- | --- |
| `321-API` | `completed` | A first coherent public facade exists in `src/FrankenTui/Ui.cs`. |
| `322-API` | `in progress` | Consumer-facing composition is working, but not yet stabilized as a mature package surface. |

### 330-HST Terminal Host Backends And Platforms

| Code | Status | Note |
| --- | --- | --- |
| `331-HST` | `completed` | Supported terminal matrix and host policy baseline are recorded and implemented. |
| `332-HST` | `completed` | Unix/macOS/Linux host behavior has a working baseline through the console backend and PTY tests. |
| `333-HST` | `completed` | Windows host contract baseline exists; native Windows evidence remains a documented gap from this workspace. |
| `334-HST` | `completed` | PTY test-host support is implemented. |
| `335-HST` | `in progress` | Divergence recording exists, but not yet as a fuller maintained host ledger. |

### 340-WEB Web And WASM Host Surfaces

| Code | Status | Note |
| --- | --- | --- |
| `341-WEB` | `completed` | Web execution boundary is defined in code through the shared render/runtime path and web host. |
| `342-WEB` | `completed` | Deterministic web host baseline exists. |
| `343-WEB` | `in progress` | Showcase wasm runner baseline exists, but it is still shallow. |
| `344-WEB` | `in progress` | Boundary discipline is understood, but not yet fully documented as a dedicated divergence ledger. |

### 350-VRF Verification Stack

| Code | Status | Note |
| --- | --- | --- |
| `351-VRF` | `completed` | Baseline test and artifact infrastructure is in place. |
| `352-VRF` | `completed` | Kernel/render headless tests are active and passing. |
| `353-VRF` | `completed` | Widget/runtime headless tests and simulator checks are active and passing at baseline level. |
| `354-VRF` | `in progress` | Invariant-style tests exist; fuzz/corpus depth is still limited. |
| `355-VRF` | `completed` | PTY-backed integration tests are active and passing on the current Unix workspace. Cross-platform evidence gaps are documented in the blocker note. |
| `356-VRF` | `in progress` | Baseline web tests exist, but browser/node parity runners are not yet present. |
| `357-VRF` | `not started` | Replay/evidence comparison workflows against upstream are not yet built. |
| `358-VRF` | `not started` | Benchmarks and performance gates are not yet built. |
| `359-VRF` | `not started` | CI/regression runner wiring has not yet been added. |

### 360-DEM Demo And Showcase Surface

| Code | Status | Note |
| --- | --- | --- |
| `361-DEM` | `in progress` | The minimum showcase slice has effectively started to emerge, but is not yet formally captured. |
| `362-DEM` | `in progress` | Terminal showcase baseline exists and is exercised by PTY tests. |
| `363-DEM` | `not started` | Terminal and web showcase alignment is not yet treated as its own parity workflow. |

### 370-EXT Extras And Optional Optimization Surface

| Code | Status | Note |
| --- | --- | --- |
| `371-EXT` | `not started` | Extras classification is not yet recorded. |
| `372-EXT` | `not started` | Feature-gated extras beyond the current dashboard baseline are not yet ported. |
| `373-EXT` | `not started` | SIMD/optimization work has not started. |

### 380-TOL Doctor And Operational Tooling

| Code | Status | Note |
| --- | --- | --- |
| `381-TOL` | `in progress` | Doctor scope mapping has started through the current environment report and blocker notes. |
| `382-TOL` | `in progress` | Capture/reporting support has started through harness and PTY tooling, but not full replay triage flows. |
| `383-TOL` | `in progress` | Maintainer-facing reporting exists at baseline JSON-report level only. |
| `384-TOL` | `not started` | CI/demo/parity alignment for doctor tooling is not yet built. |

### 390-DOC FrankenTui.Net Implementation Docs

| Code | Status | Note |
| --- | --- | --- |
| `391-DOC` | `not started` | Deeper .NET-specific implementation docs are not yet written. |
| `392-DOC` | `in progress` | Divergence and verification docs now exist, including blocker and status tracking notes. |
| `393-DOC` | `in progress` | README and agent guidance are being kept aligned, but this remains an ongoing maintenance item. |
