# 357-VRF Shared Sample Comparison Scaffold

## Purpose

This document records the first local scaffold for direct sample comparison
between FrankenTui.Net and the managed upstream FrankenTUI workspace under
`.external/frankentui`.

It extends the earlier `357-VRF` contract/evidence work with a real, runnable
cross-implementation sample lane.

## Scope

The current scaffold is intentionally small but no longer render-kernel only.
It compares a shared sample suite made of:

- `counter_flow`: four frames of an event-driven counter flow with a resize
  transition
- `unicode_cells`: a mixed ASCII/emoji frame
- `wide_overwrite`: a wide-character frame followed by a narrow overwrite frame
- `inline_overlay`: final inline-mode rows after a UI present plus routed log
  write

The goal is not to prove full demo parity yet. The goal is to establish a
repeatable mechanism for:

- generating the same named samples in both implementations
- normalizing them into the same headless row/text shape
- including both runtime-step and inline-mode contracts in the same lane
- recording artifacts and diffs under the local evidence layout

## How It Works

The .NET side runs through `FrankenTui.Testing.Harness.SharedSampleComparison`.

The upstream side is executed by a generated local Rust helper crate written
under `artifacts/comparison/upstream-shared-sample-runner/`. That helper:

- depends on the managed upstream crates via local path references
- renders the shared sample suite, including upstream inline-mode behavior
- prints a stable JSON capture consumed by the .NET harness

This keeps the comparison honest without turning this repository into a fork of
the upstream workspace.

## Current Command Surface

The scaffold is exercised today through the headless test lane:

```bash
dotnet test tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --filter SampleComparisonTests
```

Artifacts are written under:

- `artifacts/comparison/vrf357-shared-samples.dotnet.json`
- `artifacts/comparison/vrf357-shared-samples.upstream.json`
- `artifacts/comparison/vrf357-shared-samples.report.json`

## Deliberate Limits

- This is not yet a full showcase-to-showcase parity runner.
- The current suite covers representative runtime and inline contracts, but it
  does not yet exercise full hosted-parity session flows or richer widget
  interactions.
- The upstream helper crate is generated locally and is not a tracked upstream
  asset.

That limitation is deliberate. The scaffold is meant to make richer
cross-implementation sample comparison straightforward later, not to pretend
that the current demos are already screen-for-screen equivalent.
