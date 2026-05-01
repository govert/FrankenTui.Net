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
  `40c98246f27f9d174b3923c8df841ba325247dd4`
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
git -C .external/frankentui checkout 40c98246f27f9d174b3923c8df841ba325247dd4
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

## 2026-04-14 Selective Sync Wave

Batch basis:

- managed upstream workspace inspected at `be5f67289e862fd823af1548811d431cccea4ffb`
  on branch `fix/windows-demo-crossterm-fallback`
- fetched upstream head reviewed at `origin/main`
  `2d25a03dd453c4384287df2271dc8fdcf3247c06`
- dominant upstream commits reviewed:
  - `a48d33ee` `fix(parser): flatten colon-separated SGR sub-parameters instead of discarding them`
  - `59d7709e` `refactor(render): introduce PreparedContent enum and optimize presenter hot paths`
  - `10c341f5` `perf(render): quad-cell diff skip, same-row CUP elimination, ASCII PreparedContent fast path, and cached hyperlink policy`

Local landings in this selective wave:

- `src/FrankenTui.Render/TerminalModel.cs`
  now flattens colon-separated CSI/SGR subparameters so headless ANSI model
  parsing preserves ITU T.416-style truecolor forms like
  `ESC[38:2:255:128:0m`, matching the upstream parser fix at the local
  verification boundary.
- `src/FrankenTui.Render/BufferDiff.cs`
  now skips unchanged four-cell blocks before checking individual cells, which
  ports the safe local subset of the upstream row-scan optimization without
  changing the existing accelerator seam.
- `src/FrankenTui.Render/Presenter.cs` and
  `src/FrankenTui.Render/AnsiBuilder.cs`
  now carry the directly applicable presenter subset from the reviewed
  upstream render commits: same-row cursor planning now avoids `CUP` when
  `CHA` or relative moves dominate, hyperlink-disable policy is cached per
  presenter instance, grapheme fallback output now preserves the encoded cell
  width, and standalone zero-width content is replaced with a width-1 visible
  placeholder to keep terminal cursor state aligned.
- `tests/FrankenTui.Tests.Headless/TerminalModelTests.cs` and
  `tests/FrankenTui.Tests.Headless/DiffAndHeadlessTests.cs`,
  `tests/FrankenTui.Tests.Headless/PresenterTests.cs`, and
  `tests/FrankenTui.Tests.Headless/AnsiSequencesTests.cs`
  now preserve the colon-parameter truecolor contract, the late-change
  quad-skip regression, and the width-safe presenter fallback behavior
  locally.

Selective adoption note:

- The upstream `PreparedContent` / presenter hot-path commits were reviewed but
  only partially applicable. The Rust implementation now relies on a grapheme
  pool-backed content preparation path that the current .NET port does not yet
  expose. The local presenter already covered the same-row cursor-move and
  hyperlink-disabled behavioral cases, so this wave ports the directly
  applicable parser and diff deltas while leaving the larger presenter
  refactor as an explicit future adoption candidate rather than an implicit
  drift.

## 2026-04-15 Widget Clearing Sync Wave

Batch basis:

- fetched upstream head reviewed at `origin/main`
  `2d25a03dd453c4384287df2271dc8fdcf3247c06`
- dominant upstream commits reviewed:
  - `2b59ac89` `feat(widgets): add clear_text_row helper + apply it to paginator/stopwatch/timer NoStyling paths`
  - `13c71d4b` `fix(widgets/validation_error,decision_card,drift): clear separator cells + tidy last NoStyling paths`
  - `c00c740e` `fix(widgets): generalize clear_text_row → clear_text_area and apply across the entire widget set`

Local landings in this widget-clearing wave:

- `src/FrankenTui.Widgets/WidgetRenderHelpers.cs`
  now provides shared `ClearTextArea` and `ClearTextRow` helpers so widgets can
  wipe owned text regions before rendering shorter follow-on frames.
- `src/FrankenTui.Widgets/ParagraphWidget.cs`,
  `src/FrankenTui.Widgets/ListWidget.cs`,
  `src/FrankenTui.Widgets/StatusWidget.cs`,
  `src/FrankenTui.Widgets/TabsWidget.cs`,
  `src/FrankenTui.Widgets/TextAreaWidget.cs`,
  `src/FrankenTui.Widgets/TreeWidget.cs`,
  `src/FrankenTui.Widgets/ProgressWidget.cs`, and
  `src/FrankenTui.Widgets/InspectorWidgets.cs`
  now clear their owned text area or row before drawing, closing the stale
  suffix / stale row leakage path when a later render is shorter than the
  previous frame.
- `src/FrankenTui.Extras/HelpWidgets.cs` and
  `src/FrankenTui.Extras/TimingWidgets.cs`
  now carry the same owned-area clearing contract on the local extras surface.
- `tests/FrankenTui.Tests.Headless/WidgetClearContractTests.cs`
  preserves the contract explicitly by rendering long-then-short widget frames
  into the same buffer and asserting that no old glyphs survive.

Selective adoption note:

- The upstream widget-clearing wave spans dozens of widgets and degradation
  modes that do not all exist one-to-one in FrankenTui.Net. This batch ports
  the same clearing contract to the current local widget inventory instead of
  copying crate-local implementation details that have no .NET counterpart.

## 2026-04-15 Grapheme Registry Sync Wave

Batch basis:

- fetched upstream head reviewed at `origin/main`
  `2d25a03dd453c4384287df2271dc8fdcf3247c06`
- dominant upstream commit reviewed:
  - `59d7709e` `refactor(render): introduce PreparedContent enum and optimize presenter hot paths`

Local landings in this grapheme-registry wave:

- `src/FrankenTui.Core/TerminalTextWidth.cs`
  now measures display width by Unicode text element instead of by scalar rune,
  so combining clusters and ZWJ emoji sequences follow the same cell-budget
  contract expected by the upstream render path.
- `src/FrankenTui.Render/GraphemeRegistry.cs` and
  `src/FrankenTui.Render/Buffer.cs`
  now provide a buffer-owned grapheme registry plus `SetText` / `CreateTextCell`
  helpers, allowing multi-codepoint grapheme clusters to survive as real cell
  content instead of immediately collapsing to placeholder output.
- `src/FrankenTui.Render/BufferPainter.cs`,
  `src/FrankenTui.Text/TextRenderer.cs`,
  `src/FrankenTui.Text/TextWrapper.cs`,
  `src/FrankenTui.Backend/InlineTerminalWriter.cs`, and
  `src/FrankenTui.Testing.Harness/SharedSampleComparison.cs`
  now iterate text elements for width and placement, aligning local write and
  wrap behavior with the upstream grapheme-aware preparation intent.
- `src/FrankenTui.Render/BufferDiff.cs`
  now compares resolved grapheme text rather than raw grapheme IDs, so two
  visually identical frames do not churn diff output just because their local
  buffer registries interned graphemes in different slot orders.
- `src/FrankenTui.Render/Presenter.cs`,
  `src/FrankenTui.Render/HeadlessBufferView.cs`, and
  `src/FrankenTui.Web/WebHost.cs`
  now resolve registered grapheme text on output surfaces instead of always
  degrading to box or question-mark placeholders.
- `tests/FrankenTui.Tests.Headless/CorePrimitivesTests.cs`,
  `tests/FrankenTui.Tests.Headless/RenderPrimitivesTests.cs`,
  `tests/FrankenTui.Tests.Headless/PresenterTests.cs`, and
  `tests/FrankenTui.Tests.Web/WebHostTests.cs`
  now preserve the local contract for combining-sequence width, ZWJ width,
  resolved grapheme output, and diff stability across buffer-local grapheme IDs.

Selective adoption note:

- This is intentionally not a line-for-line copy of the Rust `GraphemePool` /
  `PreparedContent` implementation. The local shape stays buffer-owned and .NET
  idiomatic, but it now closes the behavioral gap that previously forced the
  .NET presenter, headless, and web surfaces to degrade registered grapheme
  cells to placeholders.

## 2026-04-15 Focus State Sync Wave

Batch basis:

- fetched upstream head reviewed at `origin/main`
  `2d25a03dd453c4384287df2271dc8fdcf3247c06`
- dominant upstream commits reviewed:
  - `f61cd2a9` `feat(focus): add host_focused state tracking for correct blur/restore behavior when terminal loses focus`
  - `741c4446` `feat(focus): expand tab navigation, host-focus guard paths, and focus recovery`
  - `6e629137` `feat(focus): unify deferred focus targeting when host is blurred`
  - `a50d1aee` `feat(focus,modal): collapsed focus trap specs, empty-trap recovery, and remove_group_without_repair`
  - `d240f48f` `feat(modal): harden focus trap lifecycle, mid-stack removal, and return-focus retargeting`
  - `e1d846dd` `feat(focus,modal): extend focus manager and modal integration from concurrent development`

Local landings in this focus-state wave:

- `src/FrankenTui.Widgets/WidgetInputState.cs`
  now carries host-focus state, deferred focus targeting, a local focus-trap
  stack, active-focus-order resolution, and push/pop trap helpers, so blur,
  restore, and modal-constrained traversal are now explicit local state
  contracts instead of being implicit side effects of a single `FocusedId`.
- `src/FrankenTui.Extras/HostedParityScenario.cs`
  now routes the hosted-parity modal toggle and modal-dismiss actions through
  the new focus-trap helpers, making the local extras surface exercise real
  modal focus confinement and restoration behavior.
- `tests/FrankenTui.Tests.Headless/HostedParityStateTests.cs`
  now preserves the local contract for deferred focus across host blur/restore,
  focus-trap cycling and restore-on-pop, and hosted modal trap survival across
  blur plus dismissal back to the base focus target.
- `src/FrankenTui.Widgets/WidgetInputState.cs`
  now also preserves per-trap selected focus and supports inactive trap
  removal with upper restore-chain retargeting, which closes the local
  stale-restore class where a nested top trap could pop back to an old lower
  target instead of the latest surviving lower selection.
- `src/FrankenTui.Widgets/WidgetInputState.cs`
  also now supports trap-order updates that collapse empty traps and repair
  upper restore targets when a lower trap's allowed focus set changes, which is
  the local analogue of the upstream focus-graph-driven empty-trap repair path.
- `src/FrankenTui.Widgets/WidgetFocusGraph.cs`,
  `src/FrankenTui.Widgets/WidgetFocusManager.cs`, and
  `src/FrankenTui.Widgets/WidgetModalStack.cs`
  now provide a first-class local focus graph, graph-backed focus manager, and
  focus-aware modal stack. This closes the deeper remaining parity area from
  the upstream `FocusManager` / `FocusAwareModalStack` wave by carrying
  focusability through a shared graph, preserving modal return targets through
  non-top modal removal, and repairing trap return paths when modal focusable
  sets mutate while nested modals are open.
- `tests/FrankenTui.Tests.Headless/FocusCoordinatorTests.cs`
  now preserves the graph-backed focus contract explicitly, including tab-chain
  behavior, host blur/restore with an active modal trap, non-LIFO modal
  removal, and return-focus repair after live modal focus-set changes.

Selective adoption note:

- This wave is still not a line-for-line translation of the Rust widgets crate.
  FrankenTui.Net keeps the earlier lightweight `WidgetInputState` surface for
  hosted parity and simple widget-facing state, but the reusable local
  graph/manager/modal abstractions now cover the upstream focus-manager and
  modal-focus coordination contract that had remained open after the earlier
  deferred-focus and trap-stack batches.

## 2026-05-01 Upstream Refresh And Showcase Inventory

Batch basis:

- managed upstream workspace refreshed to `origin/main`
  `40c98246f27f9d174b3923c8df841ba325247dd4`
- previous reviewed upstream head was
  `2d25a03dd453c4384287df2271dc8fdcf3247c06`
- local external inventory updated in [`EXTERNALS.md`](./EXTERNALS.md)
- dominant upstream surfaces reviewed:
  - `crates/ftui-demo-showcase/src/screens/mod.rs`
  - `crates/ftui-demo-showcase/src/cli.rs`
  - `crates/ftui-demo-showcase/src/app.rs`
  - `crates/ftui-demo-showcase/src/chrome.rs`
  - `crates/ftui-demo-showcase/src/tour.rs`
  - `crates/ftui-showcase-wasm/src/runner_core.rs`

Local landing in this refresh:

- [`364-DEM-full-showcase-parity-plan.md`](./364-DEM-full-showcase-parity-plan.md)
  now carries the `364-DEM-A` basis inventory for the current upstream
  showcase: all 45 upstream screen ids, slugs, source files, current local
  owners, and current local parity status, plus the app/control-plane contract
  ledger.
- [`246-MAP-upstream-contract-gap-register.md`](./246-MAP-upstream-contract-gap-register.md)
  now reopens explicit active gaps for the fresh `2d25a03d..40c98246`
  upstream range: full showcase screen parity, demo control-plane parity,
  adaptive load governance, pane workspace persistence, visual-effects canvas
  harness parity, and wasm showcase runner alignment.
- `src/FrankenTui.Runtime/LoadGovernor.cs`,
  `src/FrankenTui.Runtime/RuntimeExecutionPolicy.cs`,
  `src/FrankenTui.Runtime/RuntimeFrameStats.cs`, and
  `src/FrankenTui.Runtime/AppRuntime.cs`
  now provide a local `LoadGovernorConfig` baseline with target frame time,
  upstream-shaped PID gains, anytime-valid e-process settings, hysteresis
  thresholds, cooldown, degradation floor, frame-stat decision fields, and
  `ftui.decision.degradation` telemetry including PID term, e-process, gate
  threshold/margin, warmup, and transition-correlation fields.
- `tests/FrankenTui.Tests.Headless/LoadGovernorTests.cs`
  preserves the local default, disabled, e-process warmup, degradation-floor,
  transition, and telemetry behavior.
- `apps/FrankenTui.Demo.Showcase/ShowcaseEvidenceJsonlWriter.cs` and
  `tests/FrankenTui.Tests.Headless/ShowcaseShellTests.cs` now thread those
  load-governor PID/e-process fields through generic showcase frame evidence.
- `src/FrankenTui.Extras/PerformanceHud.cs` now promotes the same
  load-governor action/reason, PID/e-process, gate-margin, warmup, and
  transition-correlation fields into the reusable operator HUD snapshot and
  widget.
- `src/FrankenTui.Testing.Harness/HostedParityRuntimeHarness.cs` now keeps the
  final `RuntimeFrameStats` in runtime captures, and
  `tools/FrankenTui.Doctor` uses that to include a runtime performance snapshot
  plus a text/dashboard load-governor summary in doctor runs.
- `src/FrankenTui.Runtime/DegradationCascade.cs` now carries local
  `RuntimeConformalPredictor` and `RuntimeDegradationCascade` baselines
  corresponding to upstream `conformal_predictor.rs`, `conformal_frame_guard.rs`,
  and `degradation_cascade.rs`: upstream-shaped mode/diff/size bucket keys,
  exact / mode+diff / mode / global / default residual fallback hierarchy, n+1
  conformal quantiles, bounded per-bucket windows, reset counters, EMA and
  rolling nonconformity tracking, warmup/calibrated/at-risk guard state, p99
  budget prediction, recovery streaks, degradation-floor clamping,
  upstream-shaped `conformal-v1`, `conformal-frame-guard-v1`, and
  `degradation-cascade-v1` JSONL field names, and essential-widget filtering.
- `src/FrankenTui.Runtime/RuntimePolicyConfig.cs` now carries the upstream-shaped
  policy-config conversion seam for conformal, frame guard, cascade, PID,
  e-process budget, and budget-controller policy records. `RuntimeExecutionPolicy`
  consumes that aggregate for effective load-governor and cascade defaults while
  preserving explicit `LoadGovernor` overrides.
- `tests/FrankenTui.Tests.Headless/DegradationCascadeTests.cs` covers the local
  predictor n+1 quantile, fallback hierarchy, window/reset behavior, cascade
  initial state, p99-driven degradation, recovery, JSONL schema fields, and
  essential-widget filtering.
- `src/FrankenTui.Render/BufferDiff.cs` now bypasses raw SIMD row comparison
  for rows containing registered grapheme cells, preserving semantic equality
  across buffer-local grapheme registry ids.
- `tests/FrankenTui.Tests.Headless/RenderPrimitivesTests.cs` now exercises the
  equal-grapheme diff contract with SIMD enabled so this remains covered
  regardless of test ordering.
- `apps/FrankenTui.Demo.Showcase/ShowcaseCliOptions.cs` now provides a
  testable local parser for the upstream-shaped showcase launch baseline:
  `--screen-mode=alt|inline|inline-auto`, inline height/min/max controls,
  screen and guided-tour env overrides, `--mouse` / `--no-mouse` policy,
  deterministic seed/mode parsing, tick cadence, generic auto-exit controls,
  pane-workspace path selection, and generic evidence JSONL path selection.
- `ShowcaseCliOptions` now also carries the upstream-shaped VFX and Mermaid
  harness launch options: harness toggles, tick cadence, forced size, seeds,
  JSONL paths, run ids, VFX frame count, VFX perf, and VFX exit-after-ms.
  Enabled harnesses now also apply local launch defaults: VFX forces the Visual
  Effects screen, forced size, tick cadence, and scripted frame count when
  provided; Mermaid forces the Mermaid screen, forced size, tick cadence, and
  mouse-off policy.
- `apps/FrankenTui.Demo.Showcase/ShowcaseHarnessJsonlWriter.cs` now writes
  local harness-specific JSONL records for scripted VFX and Mermaid harness
  runs, including stable per-frame input checksums and rendered-buffer checksums
  when the scripted render path supplies a frame buffer. VFX launch records now
  use the upstream `vfx_harness_start` event name and expose `hash_key`, `cols`,
  `rows`, and `perf` aliases. VFX frame records also expose upstream-style
  `vfx_frame`, `frame_idx`, numeric `hash`, `cols`, `rows`, and `time` fields
  for the local harness extractor lane. When `--vfx-perf` is enabled, local VFX
  runs now emit upstream-shaped `vfx_perf_frame` records and a closing
  `vfx_perf_summary` record with phase percentile fields. Doom/Quake scripted
  VFX runs now also emit the upstream FPS input script as `vfx_input` records
  (`w_down`, `d_down`, mouse look/fire, releases, etc.) with shared `hash_key`
  correlation fields across launch, input, frame, and perf records.
- Mermaid harness records now use upstream-style `mermaid_harness_start`,
  `mermaid_frame`, and `mermaid_harness_done` event names locally, with
  `hash_key`, `cols`, `rows`, numeric frame `hash`, `sample_idx`, and local
  Mermaid sample identity aliases. Mermaid frame records also derive local
  tier/glyph/cache/config-hash/link and parse/layout/route/render timing fields
  from `MermaidShowcaseSurface.BuildState`; launch records expose an
  upstream-compatible `env` object and done records carry `run_id`. Full upstream
  Mermaid recompute and renderer telemetry remains broader than the local
  harness shape.
- `apps/FrankenTui.Demo.Showcase/ShowcaseVfxGoldenRegistry.cs` now provides a
  local VFX hash-vector helper for deterministic harness runs: scenario naming
  from parsed harness options, hash-vector save/load, actual-versus-expected
  comparison results, optional update behavior, and numeric `vfx_frame.hash`
  extraction from JSONL. `ShowcaseCliOptions` and `Program.cs` now thread
  `--vfx-golden` / `FTUI_DEMO_VFX_GOLDEN` and
  `--vfx-update-golden` / `FTUI_DEMO_VFX_UPDATE_GOLDEN` so scripted VFX runs can
  fail on missing/mismatched local golden hash vectors or refresh them
  intentionally.
- `apps/FrankenTui.Demo.Showcase/ShowcaseVfxEffects.cs` now centralizes the
  current upstream `EffectType::ALL` key set and aliases, keeping local CLI
  parsing, screen labels, harness JSONL, golden scenario names, and deterministic
  canvas pattern selection on the same canonical effect keys.
- `apps/FrankenTui.Demo.Showcase/ShowcaseCliHelp.cs` now centralizes the local
  showcase CLI `--help` surface and explicitly lists the parsed VFX run-id,
  perf, exit-after, seed/size, local golden/update, and Mermaid run-id/seed/size
  harness controls so operator guidance tracks the current parser/enforcement
  surface.
- `src/FrankenTui.Extras/CanvasPrimitives.cs` now carries the first local
  canvas primitive parity layer for VFX: `CanvasMode`,
  `CanvasPixelRect.FromCellIntersection`, and Braille
  `CanvasPainter.RenderExcluding`, including the upstream overlay-exclusion
  coordinate conversion semantics.
- `apps/FrankenTui.Demo.Showcase/ShowcaseSurface.cs` now renders screen 18
  through a deterministic frame-driven Braille canvas widget instead of the
  previous static text-art placeholder; scripted rendering threads parsed
  `--vfx-effect` names into distinct local canvas patterns for every current
  upstream effect key so rendered-buffer checksums vary by effect.
- `apps/FrankenTui.Demo.Showcase/ShowcasePaneWorkspacePersistence.cs` now
  provides the local showcase persistence lifecycle baseline for
  `--pane-workspace` / `FTUI_DEMO_PANE_WORKSPACE`: load/save a versioned local
  pane workspace envelope, migrate raw-v1 local workspace snapshots, preserve
  invalid snapshots, and report schema/migration state through generic evidence.
- `apps/FrankenTui.Demo.Showcase/Program.cs` now drives terminal inline mode,
  mouse tracking, interactive tick cadence, and generic auto-exit from that
  parser instead of hard-coded local defaults, and emits generic launch/frame
  evidence when `FTUI_DEMO_EVIDENCE_JSONL` or `FTUI_HARNESS_EVIDENCE_JSONL`
  is set.
- `apps/FrankenTui.Demo.Showcase/ShowcaseInteractiveProgram.cs` now carries
  upstream-shaped guided-tour landing keyboard controls: Up/Down or h/j/k/l
  translation can change the starting screen, `+`/`-` tunes speed, `r` resets,
  and Enter/Space starts from the selected landing state. Active tours also
  support Space pause/resume, Left/Right or n/p step changes, `+`/`-` speed
  changes, and Escape exit. Its local overlay dismissal path now preserves
  command-palette-before-evidence/debug/help, evidence-before-perf, and
  debug-before-help Escape precedence. Ctrl+I toggles a local evidence ledger
  overlay that summarizes screen, frame, pane, tour, palette, and overlay state.
  F12 toggles a local debug overlay carrying screen, viewport, tour, palette,
  pane, and runtime-frame counters. Ctrl+P toggles a local performance HUD
  overlay, and Shift+A toggles a local A11y panel with Shift+H/M/L controls for
  high contrast, reduced motion, and large text flags. F6 or `m` toggles the
  local mouse-capture state, and the local bottom status row exposes mouse
  toggle zones for help, palette, A11y, perf HUD, debug, evidence, and mouse
  capture when no overlay is covering it. Interactive mouse-capture changes now
  call the terminal session feature transition so the backend emits SGR mouse
  enable/disable controls instead of only changing local state. Shift+H and
  Shift+L now route to previous/next screen and stop active tours like upstream
  when the A11y panel is not consuming those keys. The help overlay lists the
  implemented local control-plane shortcuts.
- `apps/FrankenTui.Demo.Showcase/ShowcaseTourStoryboard.cs` adds a local
  upstream-shaped tour storyboard baseline with callout title/body/hint and
  highlight percentages for the current upstream cinematic tour stops. Active
  tour rendering now shows a callout panel, active tour navigation advances
  through storyboard step indexes rather than plain screen numbers, and frame
  evidence carries the step index/count plus callout id/title/body/hint/highlight
  fields and a locally resolved highlight rectangle against the current
  showcase body area.
- `src/FrankenTui.Extras/HostedParityScenario.cs` now accepts the upstream
  advertised Ctrl+K command-palette shortcut while retaining the earlier local
  Ctrl+P alias.
- `src/FrankenTui.Extras/CommandPalette.cs` now carries a local command-entry
  favorite list and favorites-only filter, wired to the upstream-shaped
  Ctrl+F/Ctrl+Shift+F palette shortcuts through the shared hosted input path.
  It also carries a local command-category filter wired to Ctrl+0/Ctrl+1..N.
- `apps/FrankenTui.Demo.Showcase/ShowcaseEvidenceJsonlWriter.cs` provides the
  local JSONL writer used by scripted and interactive showcase runs, including
  pane-workspace load/recovery/migration/schema fields in launch records when a
  workspace path is configured and interactive tour/overlay/current-screen/pane
  snapshot fields in frame records, including the local evidence/perf/debug/A11y overlay
  visibility bits, A11y flag state, and mouse-capture state. It also emits local
  `tour_event` records when tour activity, pause state, speed, start screen, or
  current screen changes during interactive input/tick processing, and local
  `palette_event` records when palette open, query, selection, favorite, filter,
  preview, or execution state changes.
- `tests/FrankenTui.Tests.Headless/ShowcaseShellTests.cs` now verifies the
  upstream-style env defaults and command-line override behavior for the launch
  and deterministic automation controls, VFX/Mermaid harness option parsing,
  pane persistence, pane recovery evidence, guided-tour landing and active-tour
  keyboard controls, interactive tour-state evidence, local tour-event evidence,
  local palette-event evidence, Ctrl+K command-palette launch,
  Ctrl+F/Ctrl+Shift+F palette favorite controls, Ctrl+0/Ctrl+1..N palette
  category controls, Ctrl+I evidence ledger toggling, Ctrl+P perf HUD toggling,
  F6/`m` mouse-capture toggling, local status-row mouse toggle zones, F12 debug
  overlay toggling, Shift+A A11y panel toggling and panel-local Shift+H/M/L
  flags, Shift+H/Shift+L navigation, guided-tour landing mouse wheel/click
  controls, active-tour mouse-overlay exit, category-tab and visible
  screen-tab mouse routing, tab-wheel screen cycling, palette priority over
  chrome routing, local dashboard highlight pane-link routing,
  screen-ID command-palette entries/favorites/category filtering/execution plus
  upstream-shaped command-palette match-kind scoring and compact ranking
  evidence, a local screen-39 Command Palette Evidence Lab surface with
  the upstream `cmd:*` sample action set, upstream-shaped two-column render
  geometry, screen-local match-filter controls, `b`-toggled deterministic
  bench-query evidence, and palette-area wheel/click mouse handling plus
  HintRanker-style EU/net/VOI evidence,
  palette/evidence/perf/debug/help/A11y Escape precedence, help-overlay
  shortcut coverage, local `mouse_event` evidence records with upstream-compatible
  field aliases, local `mouse_capture_toggle` records, and the generic evidence
  JSONL shape with upstream-style `seq`, `run_id`, `seed`, `screen_mode`, and
  `upstream_schema_version` context aliases. It also covers the local VFX golden
  hash registry save/load/verify/update path, JSONL hash extraction, VFX effect-key
  catalog/alias normalization, Doom/Quake scripted VFX input JSONL records, and
  upstream-style Mermaid harness start/frame/done event names plus hash/sample
  and local telemetry aliases.

Selective adoption note:

- This refresh intentionally does not claim implementation closure. The
  current local catalog matches upstream ordering and metadata, but most
  screen bodies are still reduced local compositions. The work is now basis
  locked so the next implementation waves can close gaps screen-by-screen and
  feature-by-feature with traceable evidence.
- The load-governor landing ports the upstream control seam but not the full
  Rust PID/e-process implementation. That remaining depth stays open under
  `GAP-304-LOAD-GOVERNOR` until either the deeper controller is ported or the
  threshold controller is accepted as an explicit local divergence.
- The showcase control-plane landing is still a practical launch baseline, not
  full app parity. Upstream-equivalent VFX canvas output, full Mermaid
  recompute/render harness behavior, screen-level deterministic fixture seeding, broader
  pane-workspace upstream corpus coverage, upstream-specific diagnostics JSONL streams, and
  the remaining upstream overlay stack, full upstream frame hit registry,
  generalized pane hit routing, richer evidence/debug/perf/a11y surfaces, and
  full chrome/screen routing precedence remain open under
  `GAP-364-DEMO-CONTROL-PLANE`.
