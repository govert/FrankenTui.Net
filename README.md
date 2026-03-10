# FrankenTui.Net

FrankenTui.Net is a traceable, updateable .NET 10 port of
[FrankenTUI](https://github.com/Dicklesworthstone/frankentui). The project is
intended to preserve upstream behavior, rendering discipline, and syncability
rather than reinterpret FrankenTUI as a new .NET UI framework.

This repository is currently at bootstrap stage. The governing project
definition is in [CHARTER.md](./CHARTER.md), which is prescriptive rather than
aspirational.

The initial .NET workspace baseline is now in place under
[FrankenTui.Net.sln](./FrankenTui.Net.sln) with the SDK pinned in
[global.json](./global.json).

The current sample surface is the hosted-parity showcase under
`apps/FrankenTui.Demo.Showcase`, which now supports both scripted frame output
and a small interactive terminal mode.

## Start Here

- [CHARTER.md](./CHARTER.md): mission, scope, non-goals, and execution doctrine
- [docs/README.md](./docs/README.md): tracked documentation index
- [docs/210-STS-port-status.md](./docs/210-STS-port-status.md): canonical
  execution-status ledger for the hierarchical port plan
- [docs/EXTERNALS.md](./docs/EXTERNALS.md): `.external/` inventory and rebuild
  instructions
- [docs/220-PTH-pathfinder-baseline.md](./docs/220-PTH-pathfinder-baseline.md):
  upstream baseline, parity corpus, and early verification doctrine
- [docs/230-ARC-solution-skeleton.md](./docs/230-ARC-solution-skeleton.md):
  initial .NET solution shape and artifact layout
- [docs/240-MAP-module-mapping-ledger.md](./docs/240-MAP-module-mapping-ledger.md):
  upstream crate-to-project mapping ledger
- [docs/242-MAP-upstream-sync-workflow.md](./docs/242-MAP-upstream-sync-workflow.md):
  upstream basis recording and `.external` refresh workflow
- [docs/304-RTM-determinism-and-evidence.md](./docs/304-RTM-determinism-and-evidence.md):
  runtime replay, evidence manifest, and benchmark gate baseline
- [PROVENANCE.md](./PROVENANCE.md): upstream basis, port framing, and rights
  allocation
- [LICENSE](./LICENSE): repository licensing overview
- [AGENTS.md](./AGENTS.md): current agent guidance for work in this repository

## Repo Shape

- `src/`: ported library surfaces plus reusable verification support libraries
- `tests/`: runnable .NET test projects, initially split by verification mode
- `apps/`: showcase and wasm host application surfaces
- `tools/`: operator and diagnostics entry points such as `FrankenTui.Doctor`
- `artifacts/`: tracked artifact layout; generated evidence remains ignored by
  default unless promoted intentionally

## Licensing Summary

FrankenTui.Net includes both original repository materials and work derived from
FrankenTUI. Original FrankenTui.Net-only materials are MIT licensed. Material
ported, translated, or otherwise derived from FrankenTUI remains subject to the
upstream FrankenTUI license, including its OpenAI/Anthropic rider. See
[LICENSE](./LICENSE) and [PROVENANCE.md](./PROVENANCE.md) for the exact framing.
