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

The terminal showcase likewise now has two local modes:

- scripted frame output for deterministic evidence and PTY coverage
- a small interactive mode that stays inside the same core runtime/widget path

The hosted showcase also now carries a dedicated extras scenario, so higher
level optional surfaces remain visible in the same testable terminal/web path
instead of drifting into unverified helper code.

## Extras Packaging Choice

Upstream `ftui-extras` is feature-gated at Cargo level. The current .NET port
does not mirror that one-feature-per-flag layout mechanically. Instead, the
material extras slice lives in `FrankenTui.Extras` as a modular assembly with
separate types for markdown, export, validation/forms, help, timing, traceback,
and console helpers.

That choice keeps the first extras wave:

- easy to exercise in tests and demos
- honest about what is already ported
- open to later package decomposition if NuGet/trimming concerns justify it

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
