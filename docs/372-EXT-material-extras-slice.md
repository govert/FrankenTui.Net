# 372-EXT Material Extras Slice

## Purpose

This document records the materially important extras slice that FrankenTui.Net
ports in-sequence for `372-EXT`.

It is intentionally narrower than “port all of `ftui-extras` immediately” and
broader than “leave extras for the end.” The goal is to land the optional
surfaces that materially affect the local library, demo, web, and tooling
story.

## Landed Extras

The current `FrankenTui.Extras` assembly now includes:

- markdown document rendering with headings, links, block quotes, bullets, code
  fences, and simple C# syntax highlighting
- buffer export helpers for plain-text and deterministic HTML capture
- ANSI-aware console text cleanup helpers
- form state, validation rules, and validation-summary rendering
- help/spotlight widgets for operator guidance
- timer and stopwatch display widgets
- traceback formatting and rendering helpers
- an extras showcase slice wired into the hosted-parity terminal and web
  surfaces

These are the extras that most directly improve:

- library completeness for app-facing higher-level helpers
- demo credibility, because the showcase can now exercise more than the kernel
  and dashboard baseline
- verification, because the extras scenario is covered in headless, web, and
  PTY tests

## Packaging Choice

Upstream uses Cargo feature flags. The current .NET port keeps the material
extras together in `FrankenTui.Extras` rather than trying to force a one-feature
per-assembly split immediately.

That means the current baseline is:

- modular by namespace and type surface
- testable and demo-visible now
- still open to later package decomposition if NuGet packaging or trimming
  pressure makes that worthwhile

## Deliberately Deferred

The following extras remain later-in-sequence or optional even after this
batch:

- file picker and clipboard integrations
- diagram, mermaid, and richer export surfaces
- charts, canvas, and visual-FX layers
- image protocols and advanced terminal emulation helpers
- Doom/Quake novelty surfaces
- optimization work covered separately by `373-EXT`

## Relationship To The Demo

The hosted-parity showcase now includes an `Extras` scenario. That keeps the
extras batch honest: the newly ported helpers are not just library inventory,
they are visible in terminal, web, and PTY-backed demo runs.
