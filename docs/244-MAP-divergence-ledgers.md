# 244-MAP Divergence Ledgers

## Purpose

This document is the index of maintained divergence ledgers for FrankenTui.Net.
It records where we intentionally preserve a .NET-specific implementation
boundary, a local testing substitute, or a host-specific limit rather than
pretending parity is already exact.

This covers `244-MAP` from
[`200-PRT-port-work-breakdown.md`](./200-PRT-port-work-breakdown.md).

## Related Policy

- [245-MAP-divergence-triage-policy.md](./245-MAP-divergence-triage-policy.md):
  classification and escalation rules for newly discovered behavioral
  differences
- [246-MAP-upstream-contract-gap-register.md](./246-MAP-upstream-contract-gap-register.md):
  audit register of in-scope upstream contracts that are still partial or
  otherwise not yet fully carried locally

## Active Ledgers

| Surface | Primary Record | Current Divergence Baseline |
| --- | --- | --- |
| Terminal hosts | [335-HST-host-divergence-ledger.md](./335-HST-host-divergence-ledger.md) | Linux PTY evidence is local; macOS and Windows remain contract-backed from this workspace. |
| Runtime replay and evidence | [304-RTM-determinism-and-evidence.md](./304-RTM-determinism-and-evidence.md) | Runtime-driven replay, trace, diff-evidence, and manifest artifacts are first-class; the remaining deliberate limit is that doctor pairs a tooling-scenario runtime capture with a separate static dashboard artifact set. |
| Web verification | [304-RTM-determinism-and-evidence.md](./304-RTM-determinism-and-evidence.md) | Deterministic DOM parsing uses AngleSharp rather than a full browser engine in the repo-local gate. |
| Benchmark regression gate | [304-RTM-determinism-and-evidence.md](./304-RTM-determinism-and-evidence.md) | Budgets are tracked in a local fixture aligned to upstream keys where possible, with .NET-specific additions for runtime and web document cases. |
| Repo/runtime notes | [391-DOC-dotnet-implementation-notes.md](./391-DOC-dotnet-implementation-notes.md) | The repo is a coherent .NET port, not an asset-level fork of the upstream workspace. |

## Resolved Historical Ledgers

| Surface | Primary Record | Closure Baseline |
| --- | --- | --- |
| Inline terminal mode | [336-HST-inline-mode-divergence-ledger.md](./336-HST-inline-mode-divergence-ledger.md) | The dedicated inline writer contract has landed locally with DEC save/restore, region clearing, routed inline logs, backend-driven polling, and headless/PTY verification. |

## Divergence Recording Rules

Record a divergence when one of these is true:

- the .NET implementation uses a different runtime or host boundary than
  upstream
- the local verification stack uses an equivalent substitute rather than the
  exact upstream execution environment
- the repo carries an intentionally narrower or broader public surface than the
  upstream crate layout implies
- the workspace cannot produce a class of evidence locally and must rely on CI
  or contract-backed documentation

Do not record a divergence for ordinary language-level translation differences
that preserve the same functional contract.

When a difference is first discovered, classify it under
[245-MAP-divergence-triage-policy.md](./245-MAP-divergence-triage-policy.md)
before deciding whether it belongs in these ledgers or in an upstream issue.

## Closure Rule

A divergence can be removed from the active ledger only when:

1. the replacement implementation has landed,
2. verification covering the new behavior exists in the repo, and
3. the supporting doc or ledger entry has been updated in the same batch.
