# 110-UPR FrankenTUI Repo Roadmap

## 111-UPR Purpose

This document maps the upstream FrankenTUI repository into work areas relevant
to FrankenTui.Net. It is a roadmap of the upstream repo surface, not the .NET
implementation plan itself.

## 120-UPR Workspace Map

The upstream workspace currently contains these major crate groups:

- `ftui`: public facade crate
- `ftui-core`: terminal lifecycle, capabilities, input, inline mode
- `ftui-render`: cells, buffers, diffs, presenter, headless rendering
- `ftui-style`: colors, styles, themes
- `ftui-layout`: flex/grid/layout solvers
- `ftui-text`: wrapping, segmentation, shaping, editing, width logic
- `ftui-runtime`: runtime loop, subscriptions, terminal writer, replay, policy
- `ftui-widgets`: base widget library
- `ftui-extras`: feature-gated higher-level widgets and effects
- `ftui-a11y`, `ftui-i18n`, `ftui-backend`, `ftui-tty`: support layers and
  host abstractions
- `ftui-harness`, `ftui-pty`, `ftui-demo-showcase`: verification and demo
  assets
- `ftui-web`, `ftui-showcase-wasm`: web/wasm host surfaces
- `doctor_frankentui`: separate diagnostics/migration tooling
- `fuzz/`: fuzz targets for parser/layout/text/render invariants

## 130-UPR Direct Core Library Targets

These areas most directly align with the FrankenTui.Net charter and should be
treated as the primary port surface:

- `ftui-core`
- `ftui-render`
- `ftui-style`
- `ftui-layout`
- `ftui-text`
- `ftui-runtime`
- `ftui-widgets`
- `ftui-backend`
- `ftui-tty`
- `ftui-a11y`
- `ftui-i18n`
- `ftui`
- `ftui-extras`
- `ftui-simd`

These crates define the kernel, renderer, layout/text engines, runtime, widget
surface, optional library extensions, optimization hooks, and top-level package
facade.

## 140-UPR Direct Verification And Tooling Targets

These upstream areas are first-class port targets because they define how
FrankenTUI is tested, inspected, and operationalized in practice:

- `ftui-harness`
- `ftui-pty`
- `doctor_frankentui`
- `fuzz/`

They are not optional polish. They carry parity evidence, regression coverage,
capture/replay workflows, and maintainer tooling that the .NET port should
cover in equivalent form.

## 150-UPR Direct Host And Showcase Targets

These upstream areas are also directly in scope because they exercise or expose
the same core through additional hosts or showcase surfaces:

- `ftui-web`
- `ftui-showcase-wasm`
- `ftui-demo-showcase`

## 160-UPR Verification Characteristics

The upstream repo has a substantial verification surface that should inform the
.NET port, even where exact tooling differs:

- crate unit and integration tests across nearly every core crate
- headless rendering and simulator-style verification
- PTY-backed tests via `ftui-pty`
- snapshot/golden infrastructure via `ftui-harness`
- demo/showcase integration tests
- criterion benchmarks across render, layout, text, runtime, widgets, and demo
- fuzz targets for parser, layout, text shaping, width, widget render, and VT
  parsing
- shell and Python E2E scripts for showcase, terminal compatibility, perf, and
  determinism checks

For planning purposes, this means FrankenTui.Net must include its own
verification stack rather than treating tests as a thin afterthought.

## 170-UPR Deferred But In-Scope Targets

Some surfaces are still fully in scope but should follow stable kernel,
runtime, and verification foundations:

- broader `ftui-extras` coverage beyond parity-critical slices
- `ftui-simd`-equivalent optional optimizations
- deeper demo/showcase breadth beyond the minimum parity-driving screens
- doctor/web features whose utility depends on stable evidence and replay layers

## 180-UPR Reference-Only Targets For Now

These upstream areas should be treated as reference material unless FrankenTui
.Net later decides to adopt equivalent scope explicitly:

- docs or assets centered on `frankenterm`, remote protocols, or FFI contracts
  that are not required by the FrankenTui.Net implementation
- external website embedding assets and content repos outside the FrankenTUI
  repository itself
- illustrative media, screenshots, and other nonessential upstream assets that
  do not need to be copied into this repository

They may still influence architecture or verification ideas, but they are not
required to establish the initial .NET port.

## 190-UPR Upstream Docs To Reference

These upstream documents are especially relevant as reference material while the
.NET port is being built:

- `README.md`
- `docs/getting-started.md`
- `docs/adr/ADR-001-inline-mode.md`
- `docs/adr/ADR-002-presenter-emission.md`
- `docs/adr/ADR-003-terminal-backend.md`
- `docs/adr/ADR-005-one-writer-rule.md`
- `docs/spec/diff-strategy-contract.md`
- `docs/spec/terminal-engine-correctness.md`
- `docs/spec/wasm-showcase-runner-contract.md`
- `docs/spec/opentui-evidence-manifest.md`
- `docs/spec/sdk-test-strategy.md`
- `docs/testing/no-mock-policy.md`
- `docs/testing/coverage-matrix.md`
- `docs/testing/e2e-coverage-matrix.md`

FrankenTui.Net should reference these where useful, and only replace them with
local docs when .NET-specific implementation constraints require it.

## 200-UPR Repo Interpretation

The upstream repo should be read as a layered system:

1. kernel and terminal ownership
2. render and diff machinery
3. style, layout, and text systems
4. runtime, widget composition, and public facade
5. terminal hosts, PTY support, and web/wasm hosts
6. harnesses, demos, fuzzing, and verification assets
7. doctor tooling and evidence/reporting workflows
8. optional extras and optimization surfaces

That layered interpretation drives the .NET work breakdown.
