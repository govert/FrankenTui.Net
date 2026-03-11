# 245-MAP Divergence Triage Policy

## Purpose

This document defines how FrankenTui.Net triages behavioral differences found
after the port has landed, so the repo can keep tracking upstream FrankenTUI
without letting local gaps turn into silent fork drift.

This document extends the `242-MAP` and `244-MAP` maintenance workflow from
[`242-MAP-upstream-sync-workflow.md`](./242-MAP-upstream-sync-workflow.md) and
[`244-MAP-divergence-ledgers.md`](./244-MAP-divergence-ledgers.md).

## Classification Set

Use exactly one of these classifications for a newly discovered difference:

- `parity-preserving translation`
  The .NET code is structurally different but preserves the same observable
  contract. Do not open a divergence ledger just for this.
- `local gap`
  Upstream already has the relevant behavior or subsystem, but the .NET port
  does not yet implement it fully.
- `intentional local divergence`
  The repo deliberately keeps a .NET-specific boundary, substitute, or narrower
  execution model.
- `suspected upstream defect`
  The same behavior appears questionable in the upstream basis, but the team
  does not yet have a minimized upstream repro or code-level proof.
- `confirmed upstream defect`
  The same behavior is reproduced against the managed upstream basis or is
  directly evidenced by upstream code and tests; prepare an issue or PR
  upstream and record it locally until the basis refresh absorbs the change.

## Required Evidence

Before classifying a difference, collect:

1. The exact local command, test, or artifact that shows the behavior.
2. The executed host path, including remoting and mux layers when relevant.
3. The exact upstream basis commit consulted.
4. The exact upstream files consulted: docs, tests, and implementation files.
5. The affected local files or modules.
6. The current decision and closure condition.

If the host path is remote, classify by the host that actually executes the
terminal code. For example, `Windows Terminal -> SSH -> Linux -> tmux` is a
`linux/unix-tty` execution path with mux involvement, not a local
`windows/conpty` execution path.

## Decision Order

Follow this order whenever a parity question appears:

1. Reproduce locally and capture the host path.
2. Compare the behavior to the current upstream basis docs, tests, and source.
3. Classify the difference using the set above.
4. Record the result in the relevant divergence ledger before changing scope or
   filing upstream issues.
5. Only then choose whether to:
   - port missing local behavior,
   - preserve an intentional local difference,
   - or raise an upstream issue/PR.

## Upstream Issue Threshold

Do not file an upstream issue when the local repo simply has not ported the
relevant upstream contract yet.

Open an upstream issue or PR only when at least one of these is true:

- the same fault is reproduced against the managed upstream basis
- the upstream docs/tests promise one behavior while the upstream code clearly
  does another
- the local port faithfully mirrors the upstream behavior and the bug still
  occurs

If the current local code is missing a dedicated upstream subsystem, classify it
as a `local gap` first even if the user-visible symptom looks like an upstream
bug.

## Recording Rule

Every `local gap`, `intentional local divergence`, `suspected upstream defect`,
or `confirmed upstream defect` must be recorded in a maintained ledger. The
minimum entry must include:

- status
- classification
- upstream basis
- host path
- evidence summary
- affected local files
- affected upstream files
- next action
- closure rule

## Closure Rule

A recorded difference can leave the active ledgers only when:

1. the local implementation or upstream refresh has actually closed the gap,
2. verification for the corrected behavior exists in the repo, and
3. the relevant ledger and status docs are updated in the same batch.
