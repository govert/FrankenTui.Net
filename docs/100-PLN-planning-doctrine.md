# 100-PLN Planning Doctrine

## Purpose

This document defines how planning work is represented in FrankenTui.Net.

## 110-PLN Planning Rules

- FrankenTui.Net planning is organized as a hierarchical work/task list rather
  than as calendar-based roadmaps.
- Sequence work by dependency, not by dates, milestones, releases, or phases.
- Keep the number of planning documents low at the start. Add child documents
  only when a parent area becomes too dense to manage cleanly.
- Planning must cover implementation work, tests, verification tooling, and
  provenance/sync work.
- Planning must also cover supporting tools and non-terminal host surfaces when
  they are part of FrankenTui.Net scope.
- The repository is not a fork of FrankenTUI. It is a coherent .NET ported
  implementation with its own documentation set.
- We do not need to duplicate upstream documentation unless FrankenTui.Net needs
  implementation-specific guidance. Upstream docs should be referenced when they
  remain authoritative.
- Verification should be attached to the earliest layer that can be tested; do
  not defer parity work until upper layers exist.

## 120-PLN Code Scheme

- Planning and work-list documents use a sortable prefix: `NNN-AAA`.
- `NNN` is a short numeric sequence for ordering.
- `AAA` is a short area code.
- Document filenames begin with the relevant code.
- Headings for work/task list sections also begin with the relevant code.
- Work items inside hierarchical plans also use `NNN-AAA` codes.

Examples:

- `100-PLN-planning-doctrine.md`
- `110-UPR-frankentui-roadmap.md`
- `200-PRT-port-work-breakdown.md`

## 130-PLN Area Code Conventions

These area codes are currently reserved for planning:

- `PLN`: planning doctrine and planning process
- `UPR`: upstream repo roadmap/reference map
- `PRT`: master port work breakdown
- `PTH`: pathfinder and exploratory work
- `ARC`: repository and architecture skeleton
- `MAP`: provenance, mapping, and syncability work
- `KRN`: terminal/kernel primitives and lifecycle
- `RND`: render kernel
- `STY`: style and theme
- `LYT`: layout
- `TXT`: text system
- `RTM`: runtime and execution loop
- `WGT`: widgets
- `API`: public facade and package surface
- `EXT`: extras and optional feature surfaces
- `DEM`: demo and showcase surfaces
- `HST`: host backends and platform adapters
- `WEB`: web and wasm host surfaces
- `TOL`: diagnostic and operational tooling
- `VRF`: verification, harnesses, and benchmarks
- `DOC`: FrankenTui.Net-specific implementation docs

Additional area codes can be introduced when justified by the work.

## 140-PLN Hierarchy Rule

- Parent items describe a coherent work area.
- Child items describe concrete sub-areas or enabling steps within that area.
- A child item may depend on siblings or on earlier parents, but dependency
  chains should be made explicit.
- Do not manufacture pseudo-phases. If an item is exploratory, mark it as
  pathfinder work inside the hierarchy instead.

## 150-PLN Status and Revision Rule

- The planning hierarchy is a living dependency map.
- Items may be added, split, or re-sequenced as upstream understanding improves.
- Revisions should preserve code stability where practical so references do not
  churn unnecessarily.
