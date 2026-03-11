# 248-MAP Runtime Input And Proof Wave

## Purpose

This note records the contract-closure wave that moved several runtime-facing
surfaces from hosted-parity-only glue toward reusable runtime infrastructure,
while also widening the proof story around routing, sanitization, shared sample
parity, and OpenTUI certification.

Use it with [`246-MAP-upstream-contract-gap-register.md`](./246-MAP-upstream-contract-gap-register.md)
and [`210-STS-port-status.md`](./210-STS-port-status.md) to understand what
closed in this wave and what remains deliberately partial.

## Landed Surface

- `RuntimeInputEnvelope`, `RuntimeInputEngine`, and `RuntimeInputController`
  now define a reusable runtime-facing input path that carries:
  - source event
  - effective translated event
  - semantic events
  - keybinding decisions
  - resize decisions and ready sizes
  - quit/idle labels for loop control
- The interactive showcase and hosted runtime harness now use that shared
  runtime path instead of a bespoke hosted-parity-only loop.
- `HostedParityInputEngine` is now a thin hosted wrapper over the shared
  runtime input engine rather than a separate policy/gesture implementation.

## Verification And Evidence Depth

- Input telemetry now emits:
  - `ftui.input.event`
  - `ftui.decision.resize`
  - `ftui.reflow.apply`
  - `ftui.reflow.placeholder`
- Runtime telemetry coverage also now includes:
  - `ftui.program.init`
  - `ftui.program.view`
  - `ftui.program.subscriptions`
  - `ftui.render.flush`
- Routed-output proof now includes a concurrent inline-log stress test against
  the writer gate.
- Untrusted-output proof now includes a deterministic sanitizer fuzz lane that
  verifies forbidden controls do not survive sanitization.
- The shared sample comparison suite now covers:
  - command palette ranking summary
  - log search highlighting/context summary
  in addition to the earlier counter, unicode, wide-overwrite, and inline
  overlay samples.

## OpenTUI Tooling Depth

- `OpenTuiPlanner` now materializes both planner and certification projections
  from the upstream transformation policy.
- Doctor artifact capture now writes:
  - `opentui_planner_report`
  - `opentui_planner_findings`
- `OpenTuiContractGate` now accepts planner findings and records them as a
  first-class gate stage rather than relying only on manifest coverage.

## Still Partial After This Wave

- Terminal backend lifecycle remains partial because native raw-mode/event-source
  proof is still limited from this Linux workspace.
- Windows ConPTY remains partial because native Windows evidence is still CI-/
  host-dependent from this machine.
- Mermaid still remains a contract scaffold rather than a full local
  parse/layout/render engine.
- Telemetry install remains partial because there is still no real OTLP exporter
  bridge behind the install plan.
- Operator surfaces such as pane parity, command dispatch, live log search,
  macro playback, and runtime-fed HUD metrics still need deeper app-level
  integration.
