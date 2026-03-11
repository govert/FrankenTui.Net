# 250-MAP Final Contract Closure Wave

## Purpose

This note records the batch that closed the remaining locally-fixable upstream
contract gaps after `249-MAP`.

## Scope

The wave covered:

- terminal backend boundary split proof and CI-host evidence wiring
- wider shared-sample parity coverage for pane, macro, and Mermaid surfaces
- stateful pane and Mermaid interaction in the hosted/demo path
- telemetry exporter bridge proof, macro event coverage, and stronger redaction
  tests
- Mermaid parse/layout/render/diagnostic baseline instead of static preview-only
  scaffolding

## Main Landings

- `ITerminalBackend` is now explicitly decomposed into lifecycle, output-sink,
  and event-source facets.
- `SharedSampleComparison` now carries pane workspace, macro replay, and
  Mermaid preview cases in addition to the earlier render/runtime samples.
- `PaneWorkspaceState` now supports undo/redo timeline restoration.
- `HostedParitySession` now persists pane and Mermaid state, and the extras
  scenario drives both through interactive controls instead of rebuilding them
  from incidental session flags.
- `TelemetryConfig` now has a tested OTLP bridge/export path, and telemetry
  tests cover exporter payload shape, macro event recording, and redaction
  behavior.
- `MermaidShowcaseSurface` now uses a deterministic parse/render pipeline with
  diagnostics and interactive preferences instead of only selecting a canned
  preview string.
- GitHub Actions now runs doctor artifact generation on Windows as well as
  Linux so host evidence is not Linux-only in CI.

## Verification

The closure wave was verified with:

- `dotnet test tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore`
- `dotnet test tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore --filter SampleComparisonTests`
- `dotnet test FrankenTui.Net.sln --no-restore`

Verification result at close:

- `114` headless tests
- `5` web tests
- `7` PTY tests

## Remaining Non-Local Item

The only explicit remaining contract blocker after this wave is native Windows
ConPTY execution evidence from outside the primary Linux workspace. That is
tracked separately in `2026-03-12-windows-conpty-evidence-blocker.md`.
