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
  `f612df2b9346e3001a854c89ef017e91edd9cf5d`
- Primary upstream reference assets currently used by local verification:
  - `tests/baseline.json`
  - `docs/spec/diff-strategy-contract.md`
  - `docs/spec/opentui-evidence-manifest.md`
  - `docs/adr/ADR-005-one-writer-rule.md`
  - `docs/adr/ADR-006-untrusted-output-policy.md`
  - `docs/adr/ADR-010-asupersync-targeted-adoption.md`
  - `docs/spec/asupersync-frankentui-seam-inventory.md`
  - `docs/spec/asupersync-frankentui-invariants-metrics-evidence.md`
  - `crates/doctor_frankentui/contracts/opentui_evidence_manifest_v1.json`
  - `crates/ftui-core/src/inline_mode.rs`
  - `crates/ftui-render/src/diff.rs`
  - `crates/ftui-layout/src/pane.rs`
  - `crates/ftui-runtime/tests/deterministic_replay.rs`
  - `crates/ftui-web/tests/wasm_step_program.rs`

## Recording Rules Per Porting Batch

For every implementation batch that consults upstream behavior, record:

1. The exact upstream basis commit used during the batch.
2. The exact upstream files consulted for the batch.
3. The local tests, fixtures, or docs added to preserve that basis locally.
4. Any triage decision for newly discovered behavior differences under
   [`245-MAP-divergence-triage-policy.md`](./245-MAP-divergence-triage-policy.md).
5. Any deliberate or open divergence in
   [`244-MAP-divergence-ledgers.md`](./244-MAP-divergence-ledgers.md).
6. Any host-specific effects in
   [`335-HST-host-divergence-ledger.md`](./335-HST-host-divergence-ledger.md).

Commit messages and batch reports should mention the dominant planning codes for
the work, but the authoritative basis and refresh rules live here.

## Bug And Artifact Triage Rule

When a behavior bug is discovered after the port lands:

1. capture the local repro command or artifact,
2. capture the executed host path,
3. compare against the managed upstream basis,
4. classify the result using
   [`245-MAP-divergence-triage-policy.md`](./245-MAP-divergence-triage-policy.md),
5. record the outcome in the relevant ledger before deciding whether to port
   locally or file upstream.

This prevents "silent fork drift" where an unported upstream subsystem gets
misread as an upstream defect.

## Refresh Workflow

Rebuild `.external/frankentui` using the inventory rules in
[`EXTERNALS.md`](./EXTERNALS.md), then refresh the tracked basis using this
sequence:

```bash
mkdir -p .external
git clone https://github.com/Dicklesworthstone/frankentui.git .external/frankentui
git -C .external/frankentui checkout f612df2b9346e3001a854c89ef017e91edd9cf5d
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
   - diff-strategy selector expectations and diff-evidence artifacts
   - evidence-manifest contract assertions
   - replay, routed-output, and web-reference tests
4. Re-run the local verification and artifact workflow:

```bash
dotnet test FrankenTui.Net.sln
dotnet run --project tools/FrankenTui.Doctor/FrankenTui.Doctor.csproj -- --format text --write-artifacts --write-manifest --run-benchmarks
```

## 2026-03-20 Sync Wave

Batch basis:

- upstream commit: `f612df2b9346e3001a854c89ef017e91edd9cf5d`
- dominant upstream commits reviewed:
  - `e99c4448` `feat(render): add DiffSkipHint for certificate-based diff elision`
  - `c5332777` `feat(harness): add render gauntlet framework and equivalence modules`
  - `bd377ca4` `feat: major README refresh, expand effect system and subscription engine (+1035 lines)`

Local landings in this wave:

- `src/FrankenTui.Render/BufferDiff.cs`
  now carries explicit `DiffSkipHint` support plus certified diff entry points.
- `src/FrankenTui.Runtime/AppRuntime.cs`
  now translates runtime diff-strategy selections into certified skip hints so
  stable frames can skip or narrow diff work explicitly.
- `src/FrankenTui.Layout/PaneWorkspace.cs`
  now carries deterministic replay checkpoints, replay diagnostics, and
  checkpoint-spacing decisions for the pane workspace timeline.
- `src/FrankenTui.Testing.Harness/RenderGauntlet.cs`,
  `src/FrankenTui.Testing.Harness/PresenterEquivalence.cs`, and
  `src/FrankenTui.Testing.Harness/LayoutReuseContract.cs`
  now provide a local render-gauntlet / presenter-equivalence / layout-reuse
  baseline corresponding to the new upstream harness slice.
- `src/FrankenTui.Runtime/EffectSystem.cs`,
  `src/FrankenTui.Runtime/AppCommand.cs`,
  `src/FrankenTui.Runtime/Subscription.cs`,
  `src/FrankenTui.Runtime/AppRuntime.cs`, and
  `src/FrankenTui.Runtime/AppSession.cs`
  now provide a local effect-system observability baseline with command and
  subscription counters, queue telemetry, reconcile accounting, optional
  effect labels, cancellation/failure accounting, and declared subscription
  lifecycle start/stop telemetry while preserving the existing public
  runtime/update shape.
- `src/FrankenTui.Testing.Harness/ArtifactManifestContract.cs`,
  `src/FrankenTui.Testing.Harness/FailureSignatures.cs`,
  `tools/FrankenTui.Doctor/Program.cs`, and
  `tools/FrankenTui.Doctor/DoctorReport.cs`
  now provide explicit artifact-taxonomy validation and failure-signature
  summaries in the local doctor evidence path.
- `src/FrankenTui.Testing.Harness/BaselineCapture.cs`,
  `src/FrankenTui.Testing.Harness/FixtureSuite.cs`,
  `src/FrankenTui.Testing.Harness/FixtureRunner.cs`, and
  `src/FrankenTui.Testing.Harness/RolloutScorecard.cs`
  now provide a local deterministic fixture-suite, baseline-capture, bounded
  fixture-runner, and rollout scorecard baseline corresponding to the upstream
  harness operator-depth slice.
- `src/FrankenTui.Testing.Harness/DoctorCostProfile.cs`,
  `src/FrankenTui.Testing.Harness/DoctorWorkflowSummary.cs`,
  `tools/FrankenTui.Doctor/Program.cs`,
  `tools/FrankenTui.Doctor/DoctorReport.cs`, and
  `tools/FrankenTui.Doctor/DoctorDashboardViewFactory.cs`
  now provide a local doctor cost-profile baseline plus explicit workflow
  summary artifacts/report fields corresponding to the upstream operator-depth
  cost-profile / capture-report orchestration slice.
- `src/FrankenTui.Testing.Harness/DoctorBootstrapSummary.cs`,
  `src/FrankenTui.Testing.Harness/DoctorSuiteReport.cs`,
  `tools/FrankenTui.Doctor/Program.cs`,
  `tools/FrankenTui.Doctor/DoctorReport.cs`, and
  `tools/FrankenTui.Doctor/DoctorDashboardViewFactory.cs`
  now provide explicit bootstrap-summary and suite-report artifacts/report
  fields corresponding to the upstream doctor seed/bootstrap and report
  aggregation orchestration slice for the current single-run local doctor flow.
- `src/FrankenTui.Testing.Harness/DoctorRunMeta.cs`,
  `src/FrankenTui.Testing.Harness/DoctorSuiteManifest.cs`,
  `src/FrankenTui.Testing.Harness/ArtifactManifestContract.cs`,
  `tools/FrankenTui.Doctor/Program.cs`,
  `tools/FrankenTui.Doctor/DoctorReport.cs`, and
  `tools/FrankenTui.Doctor/DoctorDashboardViewFactory.cs`
  now provide explicit local `run_meta` and `suite_manifest` artifacts/report
  fields corresponding to the upstream run-level metadata and suite-manifest
  slice for the current local doctor flow.
- `src/FrankenTui.Testing.Harness/DoctorSeedPlan.cs`,
  `tools/FrankenTui.Doctor/Program.cs`,
  and `tools/FrankenTui.Doctor/DoctorDashboardViewFactory.cs`
  now provide an explicit local seed-plan artifact/report field corresponding
  to the upstream seed/bootstrap policy slice, including endpoint, retry,
  timeout, and stage defaults.
- `src/FrankenTui.Testing.Harness/DoctorSeedExecution.cs`,
  `src/FrankenTui.Testing.Harness/DoctorSuiteAggregator.cs`,
  `tools/FrankenTui.Doctor/Program.cs`,
  `tools/FrankenTui.Doctor/DoctorReport.cs`, and
  `tools/FrankenTui.Doctor/DoctorDashboardViewFactory.cs`
  now provide a deterministic local seed-execution artifact plus a reusable
  suite aggregate over `run_meta` entries corresponding to the upstream seed
  execution and multi-run suite observability slice.
- `src/FrankenTui.Testing.Harness/DoctorSuiteManifest.cs`,
  `src/FrankenTui.Testing.Harness/DoctorSuiteReport.cs`,
  and `tools/FrankenTui.Doctor/Program.cs`
  now persist each run into a stable local `artifacts/replay/doctor-suite/`
  workspace and rebuild the suite manifest, suite aggregate, JSON suite report,
  and HTML suite index from the collected `run_meta` set, corresponding to the
  upstream multi-run capture/report aggregation slice rather than only the
  single-run local doctor path.
- `src/FrankenTui.Testing.Harness/DoctorSeedExecution.cs`,
  `tools/FrankenTui.Doctor/Program.cs`, and
  `tests/FrankenTui.Tests.Headless/DoctorWorkflowTests.cs`
  now provide an optional actual JSON-RPC seed/bootstrap path behind
  `--seed-mode actual`, with retry/poll behavior, stage-level status capture,
  and seed-log output corresponding to the upstream real remote seed/bootstrap
  execution slice rather than simulation only.
- `src/FrankenTui.Testing.Harness/AsupersyncEvidence.cs`,
  `tools/FrankenTui.Doctor/Program.cs`, and
  `tests/FrankenTui.Tests.Headless/DoctorWorkflowTests.cs`
  now provide an explicit orchestration-only Asupersync evidence artifact with
  lane, fallback, divergence, and correlation fields corresponding to the
  upstream lane/shadow/fallback evidence contract, without claiming that the
  local runtime already has a real shadow lane.
- `tests/FrankenTui.Tests.Headless/DiffAndHeadlessTests.cs`
  `tests/FrankenTui.Tests.Headless/OperatorSurfaceTests.cs`, and
  `tests/FrankenTui.Tests.Headless/RenderGauntletTests.cs`
  preserve the new render-hint, pane-checkpoint, and harness behavior locally.
  `tests/FrankenTui.Tests.Headless/EffectSystemTests.cs`
  preserves the effect-system and queue-accounting baseline locally.
  `tests/FrankenTui.Tests.Headless/ArtifactManifestContractTests.cs`,
  `tests/FrankenTui.Tests.Headless/FailureSignaturesTests.cs`, and
  `tests/FrankenTui.Tests.Pty/PtyIntegrationTests.cs`
  preserve the new operator evidence contracts and doctor artifact outputs.
  `tests/FrankenTui.Tests.Headless/FixtureSuiteTests.cs`
  preserves the new fixture registry, baseline-capture, runner, and rollout
  scorecard behavior locally.
  `tests/FrankenTui.Tests.Headless/DoctorWorkflowTests.cs`
  preserves the new doctor cost-profile, bootstrap-summary, suite-report, and
  workflow summary behavior locally, plus the local `run_meta` and
  `suite_manifest` round-trip behavior, seed-plan defaults, and seed
  execution/suite aggregate behavior, plus suite-directory report rebuilding
  over collected `run_meta` entries, plus the optional actual JSON-RPC seed
  execution path and the explicit Asupersync lane/fallback evidence artifact,
  while `tests/FrankenTui.Tests.Pty/PtyIntegrationTests.cs`
  now also preserves the doctor cost-profile, bootstrap-summary, suite-report,
  workflow-summary, seed-plan, seed-execution, suite-aggregate, `run_meta`,
  `suite_manifest`, and suite-index artifact outputs under PTY execution.

This wave now closes the currently tracked post-`f612df2b` contract rows in
[`246-MAP-upstream-contract-gap-register.md`](./246-MAP-upstream-contract-gap-register.md).
The remaining non-isomorphic runtime difference is the upstream background
subscription-thread / bounded-join implementation shape, which is treated
locally as an explicit execution-model divergence rather than as an untracked
missing contract.

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
