# 246-MAP Upstream Contract Gap Register

## Purpose

This document is the maintained register of upstream FrankenTUI contracts that
FrankenTui.Net has not yet fully ported.

Use it as the concrete follow-on checklist after the current hierarchical port
plan, so parity work is driven by explicit upstream contracts instead of by
memory or ad hoc bug reports.

This document extends:

- [`242-MAP-upstream-sync-workflow.md`](./242-MAP-upstream-sync-workflow.md)
- [`244-MAP-divergence-ledgers.md`](./244-MAP-divergence-ledgers.md)
- [`245-MAP-divergence-triage-policy.md`](./245-MAP-divergence-triage-policy.md)

## Audit Basis

This register is based on a conservative audit of:

- accepted and proposed upstream ADRs under `.external/frankentui/docs/adr/`
- in-scope practical spec documents under `.external/frankentui/docs/spec/`
- current maintained local ledgers, blocker notes, and implementation docs
- local code search for matching types, surfaces, or explicit substitutes

This register is intentionally narrower than "every upstream document". It
tracks contracts that are either:

- already adopted locally but still partial, or
- clearly in practical repo scope but still not yet fully carried locally

## Status Legend

- `partial`: a local baseline exists, but the upstream contract is not fully
  carried yet
- `absent`: no meaningful local implementation of the contract surface was
  found during the audit
- `reference-only`: excluded from this register by current scope rules

## Scope Exclusions

Per [`110-UPR-frankentui-roadmap.md`](./110-UPR-frankentui-roadmap.md), this
register excludes current reference-only upstream areas unless FrankenTui.Net
adopts them explicitly later:

- `frankenterm` browser API, websocket, remote-threat-model, and related remote
  transport contracts
- FFI and embedded-core crate-layout contracts
- SDK-specific contracts that are not part of the current FrankenTui.Net
  implementation surface

## Partial Contracts In Already-Adopted Surfaces

| Contract Family | Upstream Basis | Local Status | Why It Is Not Fully Ported Yet | Closure Direction |
| --- | --- | --- | --- | --- |
| Terminal backend lifecycle and boundary | `docs/adr/ADR-003-terminal-backend.md`, `docs/adr/ADR-008-terminal-backend-strategy.md`, `crates/ftui-backend/src/lib.rs` | `partial` | Local backends now expose session configuration, feature toggles, bounded polling, and a `write_log` route, but the boundary still does not mirror the upstream event-source/presenter split as separate types and still lacks stronger native raw-mode lifecycle evidence. | Continue refining the boundary toward the upstream split and add native raw-mode and event-source validation. |
| Windows ConPTY host contract | `docs/adr/ADR-004-windows-v1-scope.md`, `docs/WINDOWS.md` | `partial` | Windows is treated as a supported host, but the primary Linux workspace still does not produce native ConPTY execution evidence and PTY assertions remain Unix-heavy. | Add native Windows host validation and tighten Windows-specific evidence. |
| One-writer rule and routed output | `docs/adr/ADR-005-one-writer-rule.md`, `crates/ftui-backend/src/lib.rs` | `partial` | The shared writer gate now carries routed subprocess output through `TerminalProcessRouter`, but the current proof is still headless/PTY-oriented rather than a higher-contention live-terminal stress lane. | Add contention-heavy routing verification on supported hosts and keep the supported routing patterns documented. |
| Untrusted output policy | `docs/adr/ADR-006-untrusted-output-policy.md` | `partial` | Writer-level sanitize-by-default now covers routed log output, inline logs, and subprocess-forwarded output, but adversarial PTY/fuzz coverage is still lighter than the upstream test-plan ideal and ordinary cell-content sanitization remains a narrower helper. | Add attack-oriented PTY/fuzz verification and widen safe-text helpers where untrusted content can bypass routed log paths. |
| Shared sample parity lane | `crates/ftui-runtime/tests/deterministic_replay.rs`, `crates/ftui-web/tests/wasm_step_program.rs` | `partial` | The local upstream comparison scaffold now covers event-driven counter flow, resize progression, wide-cell cleanup, unicode cells, and inline overlay rows, but it is still not a full hosted-parity/showcase oracle. | Keep widening the suite toward richer hosted session flows and representative widget interactions. |
| Keybinding policy | `docs/spec/keybinding-policy.md` | `partial` | `KeybindingResolver` and `KeybindingConfig` now exist with the upstream priority table, env-var knobs, single/double-Esc resolution, and hosted-parity integration, but the policy is not yet the universal runtime entry point for every app surface. | Keep wiring the policy through shared runtime-facing terminals and add richer E2E evidence around overlays/modals/tasks. |
| Semantic events / gesture abstraction | `docs/spec/semantic-events.md` | `partial` | `SemanticEvent`, `GestureRecognizer`, and semantic evidence logging now exist, and the hosted-parity demo/harness route raw events through them, but the generic runtime still does not forward both raw and semantic streams to arbitrary models as a first-class app contract. | Lift the semantic stream deeper into shared runtime-facing app surfaces and widen fuzz/PTY evidence. |
| Resize coalescer and migration contract | `docs/spec/resize-scheduler.md`, `docs/spec/resize-migration.md` | `partial` | `ResizeCoalescer`, config, decision logs, and hosted-parity integration now exist with steady/burst/deadline behavior, but the coalescer is still hosted-surface scoped rather than being the single runtime/builder-level resize contract across all app entry points. | Promote the coalescer into broader runtime entry points and add richer observability/telemetry export. |
| Pane parity engine | `docs/spec/pane-parity-contract-and-program.md` | `partial` | `PaneWorkspaceState`, deterministic replay/snapshot helpers, and a shared pane workspace widget now exist and are exercised through the extras/hosted-parity surface, but the broader dashboard/host integration matrix and richer interaction semantics are still much thinner than upstream. | Keep moving pane semantics into broader demo/web surfaces and extend replay parity beyond the current shared snapshot/replay baseline. |
| Command palette | `docs/spec/command-palette.md` | `partial` | `CommandPaletteEntry`, deterministic search ranking, preview rendering, and a hosted extras surface now exist, but command execution still stops at showcase/demo presentation rather than acting as the shared command-dispatch layer across the app. | Promote the palette into broader runtime/demo command dispatch and add richer keyboard E2E coverage. |
| Log search | `docs/spec/log-search.md` | `partial` | Literal/regex filtering, context merging, error handling, and a log-search widget now exist in the extras surface, but live-stream integration, richer highlighting, and budget-tier logic are still only a thin baseline. | Wire the search model into a fuller live log stream and add more adversarial regex/highlight verification. |
| Macro recorder | `docs/spec/macro-recorder.md` | `partial` | End-user facing macro definition, deterministic replay-plan normalization, and a recorder widget now exist, but capture/playback is still derived from the hosted session evidence stream rather than being a full interactive record/playback state machine. | Move macro control deeper into the runtime/demo loop and add PTY exercise around record/play/loop controls. |
| Performance HUD | `docs/spec/performance-hud.md` | `partial` | A visible HUD snapshot and widget now exist, and the extras surface exercises compact/full rendering with stable rows, but the metrics are still a hosted-session baseline rather than direct runtime/presenter budget plumbing. | Feed the HUD from broader runtime/present statistics and add explicit over-budget/degraded telemetry. |
| Telemetry env-var contract | `docs/spec/telemetry.md` | `partial` | `TelemetryConfig` now parses the OTEL / FTUI env-var contract, runtime policy can emit telemetry, and doctor/runtime artifacts persist telemetry config and event JSON, but there is still no exporter/subscriber install path or PTY env-matrix evidence lane. | Runtime + tooling |
| Telemetry event schema and redaction | `docs/spec/telemetry-events.md` | `partial` | `TelemetrySessionLog`, event categories, and conservative redaction helpers now exist, and `AppRuntime` emits update/render/fallback events, but the full upstream event family and fuzz/PTy depth are not yet carried. | Runtime + tooling |
| Mermaid engine config | `docs/spec/mermaid-config.md` | `partial` | `MermaidConfig` now parses and validates the deterministic config/env surface and doctor artifacts persist config snapshots, but the real parse/layout/render engine and diagnostics lane are still absent. | Extras |
| Mermaid showcase | `docs/spec/mermaid-showcase.md` | `partial` | `MermaidShowcaseSurface` now provides a deterministic sample catalog, viewport, metrics panel, and status-log schema inside the extras/demo/web surface, but it remains a read-only contract scaffold rather than the full interactive upstream screen. | Demo + extras + web |
| OpenTUI migration semantic contract | `docs/spec/opentui-semantic-equivalence-contract.md`, `docs/spec/opentui-transformation-policy-matrix.md` | `partial` | Local loaders/validators now consume the upstream semantic and transformation-policy JSON contracts and doctor artifacts persist normalized copies plus a summary, but no translator/certification engine consumes them yet. | Tooling / doctor |

## In-Scope Contract Surfaces Still Fully Absent Locally

At the current audit level, no in-scope contract surface remains completely
absent. The remaining work is depth, propagation, and richer verification
inside the `partial` rows above.

## Contracts Reviewed But Not Registered As Current Gaps

These upstream contracts were reviewed and are not currently carried as open
items in this register:

- `docs/adr/ADR-002-presenter-emission.md`
  Current local presenter behavior follows the reset-and-apply baseline closely
  enough to treat it as ported for now.
- `docs/adr/ADR-001-inline-mode.md`
  The newline-based inline rendering gap is now closed locally via a dedicated
  inline writer, headless contract tests, and PTY verification. The historical
  closure record is retained in `336-HST-inline-mode-divergence-ledger.md`.
- `docs/spec/diff-strategy-contract.md`
  The local runtime now carries an explicit regime-based diff strategy selector,
  degraded-terminal fallback, and `DiffEvidenceLedger`, so this is no longer a
  fixed-scan-only gap.
- `docs/spec/opentui-evidence-manifest.md`
  Doctor and harness artifacts now use a runtime-driven replay/trace/diff
  capture path rather than a synthetic one-entry replay record, while
  preserving the upstream manifest shape.
- `docs/spec/cache-and-layout.md`
  The local kernel already uses 16-byte cell-friendly value types and row-major
  buffers; no distinct open contract gap is currently recorded here.
- `docs/spec/state-machines.md`
  The local runtime and render pipeline have clear state boundaries and tests,
  even though individual subcontracts above remain open.

If later evidence shows one of these is still materially incomplete, promote it
to an active row in this register.

## Recommended Execution Order

If the goal is "close the remaining parity story" rather than "add any feature
that looks interesting", the next order should be:

1. backend boundary + native lifecycle + one-writer/log routing
2. untrusted output policy
3. semantic events + resize scheduler + keybinding policy depth/propagation
4. pane parity + operator-surface depth/propagation
5. telemetry exporter/subscriber integration plus richer PTY/evidence depth
6. Mermaid real-engine/rendering depth plus OpenTUI translator/certification validators

That order keeps correctness and syncability ahead of breadth-only feature
inventory.
