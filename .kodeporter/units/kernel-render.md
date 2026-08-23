---
id: kernel-render
name: Terminal kernel and render pipeline
status: in-progress
sourceAnchors:
  - symbolPath: ftui-core
    basisLabel: upstream-15cc6543-local-helper
    contentHash: bcfa075d4492221a7eed1bfbe8361397ae83cb1103e789fd3ac8174731a80708
  - symbolPath: ftui-render
    basisLabel: upstream-15cc6543-local-helper
    contentHash: 68c07e523f565818c3b9d07e767d493e0fd3997ec3aeedeb7246d26bf6f0ad2d
  - symbolPath: ftui-backend
    basisLabel: upstream-15cc6543-local-helper
    contentHash: 4a73015c3d72858915c4f30bd45b1a6911b57c6fb3634c79abed43d317515a8f
  - symbolPath: ftui-tty
    basisLabel: upstream-15cc6543-local-helper
    contentHash: 97cc2e05b394214d2c075d97fd65b1033a39c31d4635287cdcd9617b6ec77e0d
targetAnchors:
  - symbolPath: FrankenTui.Core
    basisLabel: target-current-worktree
    contentHash: dd9718d7a2a17c2469c32454e122d23be63cf023ab714c594bf209b7b11787d4
  - symbolPath: FrankenTui.Render
    basisLabel: target-current-worktree
    contentHash: 3123b4cc0e2fd25240c559d7bebdd9f5be8d0860c7b1a3fbfa0fc78c1f8f7a30
  - symbolPath: FrankenTui.Backend
    basisLabel: target-current-worktree
    contentHash: cbe8dd9d4cf3a03f0472a32677d172a5ec8887f7bf71be06ecdbb5da5f190ebd
  - symbolPath: FrankenTui.Tty
    basisLabel: target-current-worktree
    contentHash: 363992d273624da9ae76ff50dda59fc240ec5d59d58ffcf9ac86e0dfa43b7aad
claims: []
depth: dossiered
---

## Purpose

Port terminal ownership, events, capabilities, buffers, graphemes, diffs, presentation, sanitization, inline/alt-screen behavior, and backend contracts.

## Contract

Terminal result and effect fidelity is exact where observable: cells, normalized text, ANSI sequencing, cursor/mode/clear behavior, one-writer routing, cancellation, cleanup, and teardown. Host-dependent routes must stay explicit under `terminal-host-parity@1`.

## Questions

What changed in upstream core, backend, tty, render, SIMD, and input contracts after `f958e59e`? Does every current optimization preserve strict behavior under wide graphemes, partial writes, resize, sanitization, and failure paths?

## Evidence

Structural mappings, render gauntlet, terminal model, presenter comparisons, PTY transcripts, property/adversarial tests, and real-host evidence. A passing build alone is not preservation evidence.

