# 304-RTM Determinism And Evidence

## Purpose

This document records the current .NET baseline for runtime determinism,
evidence artifacts, DOM-level hosted verification, and benchmark regression
gates.

It closes the current baseline for `302-RTM`, `303-RTM`, `304-RTM`, and the
fidelity band from `354-VRF` through `358-VRF` in
[`200-PRT-port-work-breakdown.md`](./200-PRT-port-work-breakdown.md).

## Runtime Determinism Surface

The current deterministic runtime surface is:

- `RuntimeExecutionPolicy`
- `RuntimeTrace<TMessage>`
- `ReplayTape<TMessage>`
- `AppRuntime<TModel, TMessage>`
- `AppSimulator<TModel, TMessage>`

The runtime now records step index, incoming message, emitted messages,
headless screen text, presenter output, and a stable fingerprint per step.

Queued application flow now has a first-class local baseline through
`AppSession<TModel, TMessage>`, which provides:

- pending-message queueing
- drain semantics for command/subscription follow-on messages
- resize-aware rerender flow
- cancellation-aware loops suitable for terminal demos and tests

`RuntimeExecutionPolicy` is the local switchboard for evidence-related behavior:

- `CaptureTrace`
- `CaptureReplayTape`
- `EmitTelemetry`
- `PersistStateSnapshots`

Only trace and replay capture are active in the current baseline. Telemetry and
state persistence remain explicit policy knobs so later work can widen them
without hiding the decision in ad hoc flags.

## Evidence Contract

The evidence-manifest writer in `FrankenTui.Testing.Harness` is intentionally
shaped against upstream reference materials under `.external/frankentui`:

- `docs/spec/opentui-evidence-manifest.md`
- `crates/doctor_frankentui/contracts/opentui_evidence_manifest_v1.json`

The local manifest preserves the upstream top-level contract shape in
snake_case JSON while filling it with FrankenTui.Net-specific stage claims:

- `303-RTM` runtime replay
- `356-VRF` web snapshot
- `357-VRF` terminal and replay evidence
- `358-VRF` benchmark evidence

## Web And Hosted Verification

Hosted web verification now has two layers:

1. Render-equivalence tests over `WebHost` and the shared showcase/session
   model.
2. DOM-level parsing through `WebDomRunner` using AngleSharp.

The DOM runner is the current local substitute for a full browser engine. This
is a deliberate baseline choice, not an unrecorded gap.

## Benchmark Gate

The benchmark regression gate is driven by:

- fixture:
  `tests/fixtures/358-vrf-performance-baseline.json`
- runner:
  `PerformanceBenchmarkRunner`
- CI entry point:
  `FrankenTui.Doctor --write-artifacts --write-manifest --run-benchmarks`

The tracked fixture aligns with upstream benchmark keys where direct local
equivalents exist and adds .NET-specific cases for runtime dispatch and web
document rendering.

## Maintainer Commands

Run the full local baseline:

```bash
dotnet test FrankenTui.Net.sln
dotnet run --project tools/FrankenTui.Doctor/FrankenTui.Doctor.csproj -- --format text --write-artifacts --write-manifest --run-benchmarks
```

Key artifact outputs:

- `artifacts/doctor/`
- `artifacts/replay/`
- `artifacts/web/`
- `artifacts/benchmarks/`

## Current Deliberate Limits

- Doctor emits a deterministic replay entry for the dashboard artifact run, but
  it is not yet a full live-event recording session.
- Benchmark budgets are intentionally conservative initial gates and should be
  tightened only after refreshes are measured across CI hosts.
