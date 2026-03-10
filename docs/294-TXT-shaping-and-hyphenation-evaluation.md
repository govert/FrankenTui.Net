# 294-TXT Shaping And Hyphenation Evaluation

## Purpose

This document records the current FrankenTui.Net decision for text shaping and
hyphenation under .NET 10 and NativeAOT-conscious constraints.

This closes `294-TXT` from
[`200-PRT-port-work-breakdown.md`](./200-PRT-port-work-breakdown.md).

## Current Decision

The active baseline is:

- `TextShapingMode.NativeAotSafe`
- `TextHyphenationMode.Disabled`

These choices are reflected in the text-rendering surface rather than being
left as implicit omissions.

## Why This Is The Baseline

- the charter favors a small dependency surface and explicit tradeoffs
- the current port already has visible and verifiable progress with unshaped,
  width-aware terminal text rendering
- adding a shaping engine now would widen dependency, packaging, and AOT risk
  before a concrete parity gap has been isolated

## Deferred Evaluation Paths

The code-level policy surface records two later paths without turning them on:

- `TextShapingMode.EvaluateExternalShaper`
- `TextHyphenationMode.EvaluateSoftHyphenOnly`

Those are markers for a future, evidence-driven revisit. They are not promises
that the current repo already performs external shaping or hyphenation.

## Revisit Trigger

Revisit this decision only when at least one of these is true:

1. a parity-critical upstream case is demonstrably wrong without shaping
2. a supported script shows unacceptable terminal or web-host degradation
3. benchmark and packaging evidence show that a shaping dependency can be added
   without violating current repo constraints
