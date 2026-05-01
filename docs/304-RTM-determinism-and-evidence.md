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
- `RuntimePolicyConfig`
- `RuntimeTrace<TMessage>`
- `ReplayTape<TMessage>`
- `DiffStrategySelector`
- `DiffEvidenceLedger`
- `LoadGovernorConfig`
- `AppRuntime<TModel, TMessage>`
- `AppSimulator<TModel, TMessage>`

The runtime now records step index, incoming message, emitted messages,
headless screen text, presenter output, diff-strategy decisions, and a stable
fingerprint per step.

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
- `LoadGovernor`
- `PolicyConfig`

Trace and replay capture are active by default. The local load-governor seam is
also enabled by default and observes measured frame time to publish a
degradation decision in `RuntimeFrameStats`. Telemetry and state persistence
remain explicit policy knobs so later work can widen them without hiding the
decision in ad hoc flags.

## Runtime Load Governor

The current .NET load governor is the local baseline for the upstream
`LoadGovernorConfig` surface refreshed at upstream
`40c98246f27f9d174b3923c8df841ba325247dd4`.

Local implementation:

- `LoadGovernorConfig`
- `BudgetControllerConfig`
- `PidGains`
- `EProcessConfig`
- `RuntimeDegradationLevel`
- `RuntimeLoadGovernor`
- `RuntimePolicyConfig`
- `RuntimeConformalPolicyConfig`
- `RuntimeFrameGuardPolicyConfig`
- `RuntimeCascadePolicyConfig`
- `RuntimePidPolicyConfig`
- `RuntimeEProcessBudgetPolicyConfig`
- `RuntimeBudgetPolicyConfig`
- `RuntimeFrameStats.DegradationLevel`
- `RuntimeFrameStats.LoadGovernorAction`
- `RuntimeFrameStats.LoadGovernorReason`
- `RuntimeDegradationCascade`
- `RuntimeConformalPredictor`
- `RuntimeConformalConfig`
- `RuntimeConformalBucketKey`
- `ConformalFrameGuardConfig`
- `DegradationCascadeConfig`
- `RuntimeP99Prediction`
- `RuntimeCascadeEvidence`

The local controller now carries the upstream-shaped adaptive budget core:
target frame time, PID gains with anti-windup, anytime-valid e-process settings
and state, hysteresis thresholds, cooldown frames, degradation floor, and
transition sequencing. Decisions use the upstream-style reason codes for
cooldown, overload/underload evidence pass or insufficiency, full-quality, max
degradation, floor, and threshold-band cases. `RuntimeFrameStats` records the
decision plus PID terms, e-process value/sigma, observed-frame count,
gate thresholds and margins, warmup state, and transition correlation.

The reusable performance HUD now consumes those fields from
`PerformanceHudSnapshot.FromRuntime`, and hosted runtime captures retain final
`RuntimeFrameStats` so doctor runs can report a load-governor summary in text
and dashboard artifacts. The `load-governor-doctor-smoke` run verified the
actual doctor CLI path emits level, action, reason, PID/e-process values,
margins, warmup state, and transition ids.

The runtime also now carries a local conformal predictor and
degradation-cascade baseline: upstream-shaped mode/diff/size bucket keys, exact
/ mode+diff / mode / global / default residual fallback hierarchy, n+1
conformal quantiles, bounded per-bucket windows, reset counters, EMA frame
timing, bounded time-series and nonconformity windows, warmup / calibrated /
at-risk guard state, p99 upper-bound prediction, recovery streaks,
degradation-floor clamping, upstream-shaped JSONL schema names, and
essential-widget filtering.

`RuntimePolicyConfig` mirrors the upstream policy-config conversion seam for
conformal, frame guard, cascade, PID, e-process budget, and budget-controller
records. `RuntimeExecutionPolicy.EffectiveLoadGovernor` now derives its default
load-governor config from that aggregate unless an explicit `LoadGovernor`
override is supplied, and `EffectiveDegradationCascade` exposes the matching
policy-backed cascade config.

`AppRuntime` now evaluates that policy-backed cascade around rendering each
frame. It records conformal bucket, upper-bound, budget, calibration, fallback,
guard-state, recovery, and cascade before/after decision fields in
`RuntimeFrameStats` and in degradation telemetry. To preserve deterministic
default fixture output, the wall-clock cascade changes rendering only when an
explicit `PolicyConfig` is supplied. Under that explicit policy, lower tiers are
exposed to widgets through `RuntimeRenderContext.DegradationLevel`; `SKIP_FRAME`
is an authoritative render gate that bypasses view rendering and presentation
while leaving the previous screen intact. The initial lower-tier widget slices
cover `BlockWidget`, `ParagraphWidget`, `ProgressWidget`, `ListWidget`,
`StatusWidget`, `TabsWidget`, `TextAreaWidget`, `TableWidget`, `TreeWidget`,
`ScrollbarWidget`, `BufferInspectorWidget`, and `LayoutInspectorWidget`: simple
ASCII borders/branches/rails, style stripping, decorative-chrome clearing,
essential status/tree text, percentage-only progress output, and skeleton
clearing are now headless-covered.

Remaining load-governor depth is the broader upstream render-program layer: the
current local cascade does not yet port staged conformal variants or broad
widget-level lower-tier degradation behavior across the full widget catalog. That
remaining depth is tracked as
`GAP-304-LOAD-GOVERNOR` in
[`246-MAP-upstream-contract-gap-register.md`](./246-MAP-upstream-contract-gap-register.md).

When runtime telemetry is enabled, `AppRuntime` emits
`ftui.decision.degradation` with the decision action, reason, before/after
levels, frame duration, target duration, normalized error, PID term fields,
e-process value/sigma, gate thresholds and margins, warmup state, and transition
correlation fields, plus conformal/cascade evidence fields. Generic showcase
frame evidence carries the same load-governor and conformal/cascade evidence
fields for demo runs.

## Evidence Contract

The evidence-manifest writer in `FrankenTui.Testing.Harness` is intentionally
shaped against upstream reference materials under `.external/frankentui`:

- `docs/spec/opentui-evidence-manifest.md`
- `crates/doctor_frankentui/contracts/opentui_evidence_manifest_v1.json`

The local manifest preserves the upstream top-level contract shape in
snake_case JSON while filling it with FrankenTui.Net-specific stage claims:

- `303-RTM` runtime replay
- `354-VRF` diff-decision evidence
- `356-VRF` web snapshot
- `357-VRF` terminal and replay evidence
- `358-VRF` benchmark evidence

Doctor no longer writes a synthetic one-entry replay tape. The current
artifact-generating path uses `HostedParityRuntimeHarness` to capture a real
runtime session with:

- replay tape
- runtime trace
- diff-decision ledger
- event script
- terminal transcript
- final hosted terminal/web evidence snapshot

The current doctor output deliberately keeps the static dashboard artifacts
alongside that runtime capture rather than pretending the dashboard itself is
already a separate interactive program.

## Web And Hosted Verification

Hosted web verification now has two layers:

1. Render-equivalence tests over `WebHost` and the shared showcase/session
   model.
2. DOM-level parsing through `WebDomRunner` using AngleSharp.

The DOM runner is the current local substitute for a full browser engine. This
is a deliberate baseline choice, not an unrecorded gap.

## Shared Sample Comparison Scaffold

The verification harness now includes a small cross-implementation sample suite
that runs against both:

- local .NET render primitives and headless projection
- the managed upstream Rust workspace under `.external/frankentui`

This comparison is intentionally deterministic. It currently covers:

- event-driven counter-flow frames, including resize progression
- unicode-cell frames
- wide-character overwrite cleanup
- inline overlay final-screen rows

The upstream side is executed through a generated local Rust helper crate under
`artifacts/comparison/`, built against the upstream path dependencies. That
helper now exercises both buffer-oriented samples and upstream inline-mode
logic. It is still a scaffold rather than a full showcase-to-showcase parity
oracle.

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

On GitHub-hosted CI, benchmark overruns are treated as advisory by default so
shared-runner noise does not block doctor/evidence refreshes. Strict benchmark
failure can still be forced with `FTUI_STRICT_BENCHMARKS=1`.

## Maintainer Commands

Run the full local baseline:

```bash
dotnet test FrankenTui.Net.sln
dotnet run --project tools/FrankenTui.Doctor/FrankenTui.Doctor.csproj -- --format text --write-artifacts --write-manifest --run-benchmarks
```

Key artifact outputs:

- `artifacts/replay/`
- `artifacts/web/`
- `artifacts/benchmarks/`
- `artifacts/contracts/`
- `artifacts/comparison/`

## Current Deliberate Limits

- Doctor runtime evidence is currently driven by the hosted-parity tooling
  scenario while the static dashboard artifact set is preserved separately.
- Deterministic DOM verification still uses AngleSharp rather than a full
  browser engine in the local repo gate.
- Benchmark budgets are intentionally conservative initial gates and should be
  tightened only after refreshes are measured across CI hosts.
- The load-governor baseline exposes the upstream-shaped control seam,
  PID/e-process telemetry, conformal/cascade evidence, and the skip-frame
  render gate, but staged conformal variants and broad full-catalog lower-tier widget
  degradation remain open for deeper parity.
