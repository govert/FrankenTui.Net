# 391-DOC .NET Implementation Notes

## Purpose

This document records .NET-specific implementation choices that are worth
documenting locally instead of sending readers back upstream.

It covers the current baseline for `391-DOC` through `393-DOC` in
[`200-PRT-port-work-breakdown.md`](./200-PRT-port-work-breakdown.md).

## Repo Shape Choice

FrankenTui.Net is not a forked copy of the upstream FrankenTUI repository. It
is a coherent .NET repository that ports the same implementation surface and
verification intent while preserving explicit provenance.

That means:

- the local project graph is organized as .NET libraries, apps, tools, and test
  projects
- upstream docs are referenced when they remain the authoritative explanation
- only .NET-specific structure, divergence, or workflow decisions are restated
  locally

## Managed Runtime Choice

Terminal lifecycle, replay capture, evidence emission, and hosted rendering are
implemented as managed .NET surfaces first. That keeps the repo usable as a
library and toolchain repo without introducing a hidden native shell script or
sidecar orchestration layer.

## Hosted Verification Choice

The local repo uses deterministic HTML generation plus DOM parsing as the
tracked hosted verification baseline. This is intentionally lighter than a full
browser-engine stack, but it still exercises the semantic document structure
that the shared hosted-parity surfaces produce.

## Upstream Reference Choice

The `.external/frankentui` workspace is a managed local reference corpus, not a
vendored dependency. Refresh rules are in
[`242-MAP-upstream-sync-workflow.md`](./242-MAP-upstream-sync-workflow.md), and
active divergence tracking is indexed from
[`244-MAP-divergence-ledgers.md`](./244-MAP-divergence-ledgers.md).

## Maintainer Orientation

For the current state of execution, start with:

- [`210-STS-port-status.md`](./210-STS-port-status.md)
- [`242-MAP-upstream-sync-workflow.md`](./242-MAP-upstream-sync-workflow.md)
- [`304-RTM-determinism-and-evidence.md`](./304-RTM-determinism-and-evidence.md)
- [`335-HST-host-divergence-ledger.md`](./335-HST-host-divergence-ledger.md)

Root-level orientation in `README.md` and `AGENTS.md` should stay aligned with
those documents.
