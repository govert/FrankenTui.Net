# 365-DEM Showcase Comparison Harness

## Purpose

`365-DEM` records the automated comparison lane for the .NET showcase against
the managed upstream `ftui-demo-showcase` snapshots.

This harness exists because shell-level availability is not enough evidence for
showcase parity. It renders each .NET showcase screen headlessly, compares that
80x24 terminal frame with upstream's checked-in app snapshot, and writes a
screen-by-screen report that makes simplification, missing structure, and row
drift visible.

Use this with:

- [`210-STS-port-status.md`](./210-STS-port-status.md)
- [`242-MAP-upstream-sync-workflow.md`](./242-MAP-upstream-sync-workflow.md)
- [`246-MAP-upstream-contract-gap-register.md`](./246-MAP-upstream-contract-gap-register.md)
- [`304-RTM-determinism-and-evidence.md`](./304-RTM-determinism-and-evidence.md)
- [`364-DEM-full-showcase-parity-plan.md`](./364-DEM-full-showcase-parity-plan.md)

## Tool

The comparison runner lives at:

`tools/FrankenTui.ShowcaseCompare/FrankenTui.ShowcaseCompare.csproj`

Default command:

```powershell
dotnet run --project C:\Work\FrankenTui.Net\tools\FrankenTui.ShowcaseCompare\FrankenTui.ShowcaseCompare.csproj -- --out C:\Work\FrankenTui.Net\artifacts\showcase-compare
```

Useful options:

```powershell
dotnet run --project C:\Work\FrankenTui.Net\tools\FrankenTui.ShowcaseCompare\FrankenTui.ShowcaseCompare.csproj -- --screens 42,43,44 --out C:\Work\FrankenTui.Net\artifacts\showcase-compare
dotnet run --project C:\Work\FrankenTui.Net\tools\FrankenTui.ShowcaseCompare\FrankenTui.ShowcaseCompare.csproj -- --fail-on-diff
```

`--fail-on-diff` is intended for future gates after a screen is declared
snapshot-aligned. The default mode is evidence collection and therefore returns
success when snapshots are present even if all compared screens differ.

## Outputs

The default output directory is `artifacts/showcase-compare/`.

Generated files:

- `index.md`: one-row summary per screen with exact-match status, equal row
  count, differing row count, nonblank character counts, and local/upstream
  ratio
- `local/*.local.snap`: .NET-rendered terminal frames
- `upstream/*.upstream.snap`: copied upstream terminal frames used as the basis
- `diff/*.diff.txt`: row-by-row upstream/local text differences

## Current Baseline

The first harness run compared all 45 app-level upstream snapshots at 80x24 and
found no exact screen matches. That is expected for the current partial showcase
port and should be treated as actionable evidence for `364-DEM`, not as a
regression in itself.

After the first chrome/Kanban pass, Screen 42 (`app_kanbanboard_80x24.snap`) has
a focused exact-match lane: 24 equal rows, 0 differing rows, and a 1.000
local/upstream nonblank-character ratio under `--screens 42`.

When a screen is ported more deeply, rerun the harness and use its diff file as
the immediate evidence loop before updating `docs/210-STS-port-status.md` or
closing a `docs/246-MAP-upstream-contract-gap-register.md` item.
