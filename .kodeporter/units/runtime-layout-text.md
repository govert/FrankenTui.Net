---
id: runtime-layout-text
name: Runtime, layout, and text systems
status: in-progress
sourceAnchors:
  - symbolPath: ftui-runtime
    basisLabel: upstream-15cc6543-local-helper
    contentHash: 69cd859e560e7a5f15c6eb955f9067466b7e67bf800d1339ca961aab2288a799
  - symbolPath: ftui-layout
    basisLabel: upstream-15cc6543-local-helper
    contentHash: 93f36344cba9c96317a56392b76c2280d4e33f44d241482113b6682f84007497
  - symbolPath: ftui-text
    basisLabel: upstream-15cc6543-local-helper
    contentHash: f73ecf1216fb870c53b29294af63f60fadf0fd4874507543b21f07731a24ba6a
  - symbolPath: ftui-style
    basisLabel: upstream-15cc6543-local-helper
    contentHash: 13fca5efc29c7f4bd58c51c20262e73fdad58f681b1bc9e93b7782c1d5b5bfda
  - symbolPath: ftui-i18n
    basisLabel: upstream-15cc6543-local-helper
    contentHash: e2263da13d74465c4922d054379eee4429283a13e32127e02a2acbc6229175d9
targetAnchors:
  - symbolPath: FrankenTui.Runtime
    basisLabel: target-current-worktree
    contentHash: e781abf4087a39d837a8b96c1f273b8068d14ed6f9e0481194456865cf247160
  - symbolPath: FrankenTui.Layout
    basisLabel: target-current-worktree
    contentHash: afbc2d63a2308652fb4b0e3fe7fa1d191f3a62d2f7bd63ee150bfa0f5b022625
  - symbolPath: FrankenTui.Text
    basisLabel: target-current-worktree
    contentHash: b39fffaebcb248e2ca2b979c01bc3c742ab0336796e8948a35eb4396b11f3a2a
  - symbolPath: FrankenTui.Style
    basisLabel: target-current-worktree
    contentHash: e773b38847645e237d9242ad1f455b516b6249c97bb962014247f5aba30d4c27
  - symbolPath: FrankenTui.I18n
    basisLabel: target-current-worktree
    contentHash: 53545be6bcfbfa87d52c5863c00a8fd03aef658ed594e56483030b5c64090a73
claims: []
depth: dossiered
---

## Purpose

Port the Elm-style runtime, commands/subscriptions, scheduling and load governance, replay/evidence, layout and pane systems, and Unicode/text/markdown behavior.

## Contract

Preserve state transitions, effect ordering, cancellation and lifecycle completion, deterministic replay, policy decisions, layout results, text width/wrapping/editing, and declared degradation behavior. Runtime implementation shape may differ only when its observable consequences are evidenced and the divergence is explicit.

## Questions

Does the local runtime implement upstream lifecycle completion on every exit and the post-`f958e59e` scheduler, cancellation, telemetry, text, and pane contracts? Which staged-degradation and corpus-lifecycle gaps remain real on the latest basis?

## Evidence

Pinned source anchors, deterministic input tapes, runtime traces, policy/evidence JSONL, layout corpora, Unicode property cases, differential samples, and operational observations.

