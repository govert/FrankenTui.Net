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
- `blocked`: remaining work is real, but cannot be fully closed from the
  current workspace alone
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

## Active Remaining Gaps

| Contract Family | Upstream Basis | Local Status | Why It Is Not Fully Ported Yet | Closure Direction |
| --- | --- | --- | --- | --- |
| Windows ConPTY host contract | `docs/adr/ADR-004-windows-v1-scope.md`, `docs/WINDOWS.md` | `blocked` | The Windows backend, host matrix, CI matrix, and Windows doctor-artifact lane are in place, but the primary Linux workspace still cannot produce native interactive ConPTY execution evidence directly. | Close via a native Windows host run and update the blocker note `2026-03-12-windows-conpty-evidence-blocker.md`. |

## In-Scope Contract Surfaces Still Fully Absent Locally

At the current audit level, no in-scope contract surface remains completely
absent.

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
- `docs/spec/keybinding-policy.md`
  A reusable runtime input envelope/controller now routes the current
  interactive surfaces through one shared keybinding-aware path rather than a
  hosted-only loop.
- `docs/spec/semantic-events.md`
  The shared runtime input path now carries both source events and semantic
  events into app-facing messages for the current interactive surfaces.
- `docs/spec/resize-scheduler.md` and `docs/spec/resize-migration.md`
  Resize coalescing is now exposed through the same reusable runtime input path
  with resize decision telemetry and ready-size application.
- `docs/spec/opentui-evidence-manifest.md`
  Doctor and harness artifacts now use a runtime-driven replay/trace/diff
  capture path rather than a synthetic one-entry replay record, while
  preserving the upstream manifest shape.
- `docs/spec/opentui-semantic-equivalence-contract.md` and
  `docs/spec/opentui-transformation-policy-matrix.md`
  Local tooling now validates the upstream semantic/policy bundle, materializes
  planner/certification projections, and feeds planner findings into the local
  contract gate and doctor artifact set.
- `docs/spec/command-palette.md`
  The hosted extras/runtime surface now carries a real open/query/select/execute
  palette loop with deterministic ranking, preview state, command execution,
  and PTY/headless coverage.
- `docs/spec/log-search.md`
  The hosted extras/runtime surface now carries live-stream integration,
  full-vs-lite tiering, context merging, all-match highlighting, and interactive
  toggle/edit behavior with headless verification.
- `docs/spec/macro-recorder.md`
  The hosted extras/runtime surface now carries explicit record/ready/play/loop
  state, normalized replay timing, timer-driven playback, and PTY/headless
  coverage rather than only deriving a macro from evidence logs after the fact.
- `docs/spec/performance-hud.md`
  The HUD can now consume runtime frame statistics directly, the interactive
  showcase feeds those stats back into the extras surface, and headless/PTY
  coverage verifies the runtime-fed path.
- `docs/adr/ADR-003-terminal-backend.md`,
  `docs/adr/ADR-005-one-writer-rule.md`, and
  `docs/adr/ADR-006-untrusted-output-policy.md`
  The backend boundary is now explicitly split into lifecycle, output-sink, and
  event-source facets; routed subprocess output stays behind the writer gate;
  and sanitize-by-default proof includes fuzz and PTY coverage.
- `docs/spec/pane-parity-contract-and-program.md`
  Pane state now persists through the hosted/demo path, supports timeline
  undo/redo, and is exercised directly in headless/web/PTY coverage instead of
  being rebuilt as a view-only snapshot.
- `docs/spec/telemetry.md`
  The env-var/install surface now has a tested OTLP bridge/export path rather
  than only a configuration plan.
- `docs/spec/telemetry-events.md`
  Macro playback evidence and stronger redaction proof are now carried in the
  telemetry surface and tests.
- `docs/spec/mermaid-config.md` and `docs/spec/mermaid-showcase.md`
  The Mermaid surface now has a deterministic parse/render/diagnostic baseline
  plus interactive showcase preferences, rather than only a static preview
  scaffold.
- `crates/ftui-runtime/tests/deterministic_replay.rs` and
  `crates/ftui-web/tests/wasm_step_program.rs`
  The shared comparison scaffold now includes pane, macro, and Mermaid cases in
  addition to the earlier render/runtime sample set, so representative hosted
  surfaces are covered by the cross-implementation lane.
- `docs/spec/cache-and-layout.md`
  The local kernel already uses 16-byte cell-friendly value types and row-major
  buffers; no distinct open contract gap is currently recorded here.
- `docs/spec/state-machines.md`
  The local runtime and render pipeline have clear state boundaries and tests,
  even though individual subcontracts above remain open.

If later evidence shows one of these is still materially incomplete, promote it
to an active row in this register.

## Next Step

The only remaining explicit blocker in this register is native Windows ConPTY
evidence capture from a real Windows host. All other previously active local
contract-gap rows have been closed into reviewed surfaces and should only be
re-opened if fresh divergence evidence appears.
