# 242-MAP Upstream Sync Workflow

## Purpose

This document defines the repeatable workflow for recording the upstream basis
 used by each FrankenTui.Net porting wave and for refreshing
`.external/frankentui` without losing traceability.

This covers `242-MAP` and `243-MAP` from
[`200-PRT-port-work-breakdown.md`](./200-PRT-port-work-breakdown.md).

## Current Basis

- Managed upstream workspace: `.external/frankentui`
- Current basis commit:
  `7a91089366bd4644e086d5a422cb76b052e3de17`
- Primary upstream reference assets currently used by local verification:
  - `tests/baseline.json`
  - `docs/spec/opentui-evidence-manifest.md`
  - `crates/doctor_frankentui/contracts/opentui_evidence_manifest_v1.json`
  - `crates/ftui-runtime/tests/deterministic_replay.rs`
  - `crates/ftui-web/tests/wasm_step_program.rs`

## Recording Rules Per Porting Batch

For every implementation batch that consults upstream behavior, record:

1. The exact upstream basis commit used during the batch.
2. The exact upstream files consulted for the batch.
3. The local tests, fixtures, or docs added to preserve that basis locally.
4. Any deliberate divergence in
   [`244-MAP-divergence-ledgers.md`](./244-MAP-divergence-ledgers.md).
5. Any host-specific effects in
   [`335-HST-host-divergence-ledger.md`](./335-HST-host-divergence-ledger.md).

Commit messages and batch reports should mention the dominant planning codes for
the work, but the authoritative basis and refresh rules live here.

## Refresh Workflow

Rebuild `.external/frankentui` using the inventory rules in
[`EXTERNALS.md`](./EXTERNALS.md), then refresh the tracked basis using this
sequence:

```bash
mkdir -p .external
git clone https://github.com/Dicklesworthstone/frankentui.git .external/frankentui
git -C .external/frankentui checkout 7a91089366bd4644e086d5a422cb76b052e3de17
```

To move to a newer basis:

```bash
git -C .external/frankentui fetch origin
git -C .external/frankentui checkout <new-upstream-commit>
```

Then reconcile the local repo:

1. Update this document and [`EXTERNALS.md`](./EXTERNALS.md) with the new basis.
2. Update any affected mappings in
   [`240-MAP-module-mapping-ledger.md`](./240-MAP-module-mapping-ledger.md).
3. Refresh or re-evaluate local fixtures that intentionally mirror upstream
   contracts, especially:
   - `tests/fixtures/358-vrf-performance-baseline.json`
   - evidence-manifest contract assertions
   - replay and web-reference tests
4. Re-run the local verification and artifact workflow:

```bash
dotnet test FrankenTui.Net.sln
dotnet run --project tools/FrankenTui.Doctor/FrankenTui.Doctor.csproj -- --format text --write-artifacts --write-manifest --run-benchmarks
```

## Reconciliation Standard

Refreshing upstream does not mean copying upstream files into this repo. The
required outcome is:

- local code remains coherent as a .NET library and tooling repo
- local tests still point at the intended upstream contract surface
- any newly discovered gap is recorded as an explicit divergence instead of
  being left implicit

If a local surface no longer matches the upstream reference basis closely
enough, record that first and only then choose whether to port, defer, or
reclassify the surface.
