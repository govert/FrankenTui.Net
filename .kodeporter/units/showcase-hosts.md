---
id: showcase-hosts
name: Showcase and host surfaces
status: in-progress
sourceAnchors:
  - symbolPath: ftui-demo-showcase
    basisLabel: upstream-15cc6543-local-helper
    contentHash: 3eb4851049aa4ee1419655e5bd03404e3291796a00d8cf97ccb8548e545b9d9c
  - symbolPath: ftui-web
    basisLabel: upstream-15cc6543-local-helper
    contentHash: 789d7122427f53e607523d43a8eee8a23aaff150de1dc978e2374279e1bebe85
  - symbolPath: ftui-showcase-wasm
    basisLabel: upstream-15cc6543-local-helper
    contentHash: 1b11b5b40062af90657acc3473d53efa41f7f5f94398516a1da5b8d5829c9c7a
  - symbolPath: ftui
    basisLabel: upstream-15cc6543-local-helper
    contentHash: af02308626b51cafdd7605a55658aff92e46a376083d9ed3da8712d5f5eb6a60
targetAnchors:
  - symbolPath: FrankenTui.Demo.Showcase
    basisLabel: target-current-worktree
    contentHash: 04f88399b2e6f64ca3e4aca1b6ffe7fdb76750cce0d1051a91ad8b451bd84b1a
  - symbolPath: FrankenTui.Web
    basisLabel: target-current-worktree
    contentHash: 50777baf83da2b7a81676c2727a62ecb505fb9d04520eb622501e4d28063d18a
  - symbolPath: FrankenTui.Showcase.Wasm
    basisLabel: target-current-worktree
    contentHash: 9859043d03035283862c746749ad8491966c691a8b06917d49221d740029ac9e
claims: []
depth: dossiered
---

## Purpose

Port the full 45-screen showcase, app/chrome/control plane, terminal and web/WASM runners, deterministic harnesses, and supported host behavior.

## Contract

Use `showcase-frame-parity@1` and `terminal-host-parity@1`. All screens must have materially matching visible structure, state, interaction, evidence, and shared-model host behavior. Exact static frame lanes are closed only by exact normalized equality; similarity is diagnostic. Remaining differences must be narrow, named, and host/runtime-necessary.

## Questions

Why do all 45 current static frames differ from the current upstream snapshot corpus? Which mismatches come from shared chrome, stale golden data, missing screen state, or lower-layer behavior? Do terminal and web use one program model on the latest upstream registry?

## Evidence

The closed 45-entry frame-0 inventory in `fixtures/kodeporter/showcase-static-frames.jsonl`, plan-driven source/target adapter observations, scripted interactive tapes, PTY/web/Windows host evidence, and per-screen diff artifacts.

