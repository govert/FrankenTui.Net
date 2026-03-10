# 371-EXT Extras Classification

## Purpose

This document classifies the current extras surface into what is already
parity-critical, what should follow later in sequence, and what remains
optional.

This closes `371-EXT` from
[`200-PRT-port-work-breakdown.md`](./200-PRT-port-work-breakdown.md).

## Current Classification

### Parity-Critical Now

- hosted-parity surfaces that exercise demo, web, doctor, and evidence flows
- dashboard/operator compositions used directly by the local showcase and tool
  surfaces
- extras that make verification or demo parity materially clearer

These are already represented in `src/FrankenTui.Extras`.

### Later In Sequence

- broader higher-level widgets and effect layers that matter to the library
  surface but are not currently required to prove parity
- richer showcase-only compositions once the core interactive/session model is
  stable
- extras that depend on a stronger text/layout/runtime baseline than the
  current repo had before this batch

This is the likely follow-on area for `372-EXT`.

### Optional

- decorative or highly specialized extras that do not materially affect the
  FrankenTui.Net contract
- extras whose value is mostly aesthetic rather than parity- or
  toolchain-driving

## Batch Interpretation

For the current project state, `ftui-extras` is not “all later” and not “all
optional.” The rule is narrower:

- keep demo, doctor, web, and verification-driving extras in-sequence
- defer breadth-only extras until they improve the actual port surface rather
  than just increase inventory

## Relationship To 372 And 373

- `372-EXT` is where materially important feature-gated extras should actually
  be ported next
- `373-EXT` remains separate because optimization work should follow feature and
  demo depth, not drive it
