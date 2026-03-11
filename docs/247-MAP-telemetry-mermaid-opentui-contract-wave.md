# 247-MAP Telemetry, Mermaid, And OpenTUI Contract Wave

## Purpose

This note records the contract-closure wave that moved three previously
`absent` upstream surfaces into explicit local baselines:

- telemetry env-var contract and event/redaction schema
- Mermaid config and showcase contract surface
- OpenTUI migration semantic/policy contract handling

Use it with [`246-MAP-upstream-contract-gap-register.md`](./246-MAP-upstream-contract-gap-register.md)
to understand what now exists locally and what still remains partial.

## Telemetry

Landed local surface:

- `TelemetryConfig` parses the upstream OTEL / FTUI env-var contract, including
  endpoint precedence, protocol choice, span processor, explicit parent trace
  context, and warnings for invalid inputs.
- `TelemetryConfig.BuildLayer()` and `TelemetryConfig.Install()` now provide a
  deterministic, no-clobber install surface with explicit subscriber collision
  handling.
- `TelemetrySessionLog`, `TelemetryEvent`, and `TelemetryRedactor` provide a
  deterministic local event schema and conservative redaction baseline.
- `AppRuntime`, `AppSession`, and `RuntimeInputController` now emit
  runtime/update/view/subscription/input/reflow/render/flush/fallback telemetry
  events when telemetry is enabled via policy.
- doctor and runtime artifact capture now persist telemetry JSON alongside
  replay, trace, diff, and manifest artifacts.
- PTY verification now covers env-driven telemetry enablement and explicit
  parent-trace context parsing through the doctor text surface.

Still partial:

- no OTLP exporter/subscriber install path exists yet
- no PTY env-matrix evidence lane exists yet for telemetry enablement
- event coverage does not yet match every upstream span family

## Mermaid

Landed local surface:

- `MermaidConfig` parses and validates the upstream `FTUI_MERMAID_*` contract.
- `MermaidShowcaseSurface` provides a deterministic local showcase screen,
  sample catalog, summary metrics, and status-log schema.
- the extras/demo/web surfaces now expose Mermaid as a visible tracked module.
- doctor artifact capture persists Mermaid config snapshots.

Still partial:

- this is a contract scaffold and showcase baseline, not the full upstream
  parse/layout/render engine
- metrics are deterministic local summary values, not full engine timings
- JSONL diagnostics, capability-profile behavior, and richer interactive
  showcase flow still need deeper parity work

## OpenTUI Migration

Landed local surface:

- local types load and validate the upstream semantic-equivalence and
  transformation-policy contract JSON artifacts
- local tooling now also loads and persists the upstream confidence-model and
  licensing/provenance contracts
- validation checks clause traceability, projection completeness, stable sort,
  and construct coverage
- doctor artifact capture now persists normalized local copies plus a summary
  report, planner/certification findings, and an OpenTUI contract-gate report
  for the managed upstream basis

Still partial:

- no real source translator emits construct-specific findings yet
- no local extension workflow exists yet beyond the upstream-tracked bundle
- validator outputs are currently summary-oriented rather than full gate reports

## Artifact Impact

This wave introduces a stable local `artifacts/contracts/` bucket for:

- telemetry config snapshots
- Mermaid config snapshots
- OpenTUI semantic contract, transformation policy, and summary artifacts

That bucket is local-only and remains rebuildable from repo state plus the
managed upstream workspace under `.external/frankentui`.
