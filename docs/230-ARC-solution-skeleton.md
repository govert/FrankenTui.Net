# 230-ARC Solution Skeleton

## Purpose

This document records the initial .NET workspace shape created for the port. It
answers the early `230-ARC` questions without locking later implementation
details prematurely.

## Solution Baseline

- Solution file: `FrankenTui.Net.sln`
- SDK pin: `global.json` -> `.NET SDK 10.0.103`
- Solution format choice: classic `.sln`, not `.slnx`, to maximize current
  compatibility across CLI, editor, and agent tooling

## Repository Layout

- `src/`: ported libraries and reusable support libraries
- `tests/`: runnable test projects organized by verification mode
- `apps/`: demo and wasm showcase app surfaces
- `tools/`: operational entry points such as `FrankenTui.Doctor`
- `artifacts/`: local evidence and replay layout, ignored by default except for
  tracked placeholders and docs

## Initial Project Decomposition

The starting decomposition is recognizability-first. It broadly mirrors major
upstream crates so syncability stays obvious while the port is still young.

### Core Library Projects

- `src/FrankenTui`
- `src/FrankenTui.Core`
- `src/FrankenTui.Backend`
- `src/FrankenTui.Tty`
- `src/FrankenTui.Render`
- `src/FrankenTui.Style`
- `src/FrankenTui.Layout`
- `src/FrankenTui.Text`
- `src/FrankenTui.Runtime`
- `src/FrankenTui.Widgets`
- `src/FrankenTui.Extras`
- `src/FrankenTui.A11y`
- `src/FrankenTui.I18n`
- `src/FrankenTui.Simd`
- `src/FrankenTui.Web`

### Reusable Verification Libraries

- `src/FrankenTui.Testing.Harness`
- `src/FrankenTui.Testing.Pty`

These live under `src/` rather than `tests/` because they are reusable support
libraries, not just test buckets.

### App And Tool Surfaces

- `apps/FrankenTui.Demo.Showcase`
- `apps/FrankenTui.Showcase.Wasm`
- `tools/FrankenTui.Doctor`

### Runnable Test Projects

- `tests/FrankenTui.Tests.Headless`
- `tests/FrankenTui.Tests.Pty`
- `tests/FrankenTui.Tests.Web`

## Dependency Direction Notes

The first project graph intentionally preserves a few important upstream
directions:

- `FrankenTui.Render` depends on `FrankenTui.Core`
- `FrankenTui.Style` depends on `FrankenTui.Render`, not the reverse
- `FrankenTui.Runtime` depends on the kernel, backend, layout, style, text, and
  i18n layers
- `FrankenTui.Tty` and `FrankenTui.Web` are host-facing consumers over shared
  core behavior
- `FrankenTui` is the public facade, not the architectural center
- `FrankenTui.Doctor` remains a separate tool surface rather than being folded
  into the main facade

This graph is intentionally incomplete for optional features. It is a stable
starting shape, not a claim that every future dependency edge is already known.

## Artifact Layout

Generated evidence belongs under `artifacts/` using these tracked buckets:

- `artifacts/headless/`
- `artifacts/pty/`
- `artifacts/web/`
- `artifacts/replay/`
- `artifacts/benchmarks/`
- `artifacts/doctor/`
- `artifacts/comparison/`

Generated contents stay ignored by default. Small curated baselines may be
promoted intentionally later when they become stable review assets.
