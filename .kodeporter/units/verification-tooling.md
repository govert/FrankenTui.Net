---
id: verification-tooling
name: Verification and operational tooling
status: in-progress
sourceAnchors:
  - symbolPath: doctor_frankentui
    basisLabel: upstream-15cc6543-local-helper
    contentHash: 846ae7dc74052584152108a3a02c96543401d67b55d56ade8169912674508c95
  - symbolPath: ftui-harness
    basisLabel: upstream-15cc6543-local-helper
    contentHash: 5298dbd0dce444d41902938dd08271936da2757cf873da8164db8fff1d9b1daa
  - symbolPath: ftui-pty
    basisLabel: upstream-15cc6543-local-helper
    contentHash: a7773673acdab619f957064f9f5355089e2ab02cb4fe08e1bbde320291e5228d
targetAnchors:
  - symbolPath: FrankenTui.Doctor
    basisLabel: target-current-worktree
    contentHash: 637167138e2db109f5f77939e169fe682603e4be16789105e621b3213c65dbb2
  - symbolPath: FrankenTui.Testing.Harness
    basisLabel: target-current-worktree
    contentHash: 6418e4b3e251f0da51df9f0a06f33303a25185b5c452871fc845d490270426de
  - symbolPath: FrankenTui.Testing.Pty
    basisLabel: target-current-worktree
    contentHash: be9d88d77f546153f48308a4c5218939c42b2493893e19e3c83c8907221fa078
claims: []
depth: dossiered
---

## Purpose

Port and maintain the doctor, harness, fixtures, benchmarks, replay, comparison, evidence-manifest, and maintainer-facing diagnostic surfaces.

## Contract

Verification output is observation testimony, not automatic preservation truth. Every artifact identifies basis, method, context, denominator, and replay coordinates. Cached snapshots and hard-coded basis labels cannot support a current conclusion. Generated status, health, gates, and reports remain reproducible views.

## Questions

Which upstream doctor/harness capabilities changed after `f958e59e`? Why does current doctor output still report bootstrap basis `7a910893`? Which generated snapshot corpus is stale, and which negative or completion conclusions relied on it?

## Evidence

Plan/run receipts, manifest validation, deterministic replay, benchmark observations, coverage declarations, test inventory, independent adapters, and view diffs distinguishing data, policy, context, and method changes.

