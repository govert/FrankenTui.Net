# 249-MAP Operator Runtime Depth Wave

## Purpose

This note records the closure wave that moved the hosted extras/operator
surfaces from static showcase inventory toward real runtime-driven behavior.

It follows:

- [`246-MAP-upstream-contract-gap-register.md`](./246-MAP-upstream-contract-gap-register.md)
- [`248-MAP-runtime-input-and-proof-wave.md`](./248-MAP-runtime-input-and-proof-wave.md)

## What Landed

- Command palette state now has a real open/query/select/execute loop with
  deterministic execution results instead of stopping at search-only preview.
- Log search now carries a live merged stream, full-vs-lite tiering, all-match
  highlighting, and interactive toggle/edit control paths.
- Macro recording now has explicit `idle -> recording -> ready -> playing`
  state transitions, normalized tick-driven playback, loop/speed controls, and
  runtime timer advancement in the interactive showcase.
- Performance HUD snapshots can now be populated from runtime frame stats
  instead of only from hosted-session heuristics, and the interactive showcase
  feeds those stats back into the extras surface.
- The hosted extras surface now preserves operator state across frames rather
  than reconstructing command/search/macro/HUD views from unrelated session
  flags.

## Verification

- `dotnet test tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --no-restore`
- `dotnet test FrankenTui.Net.sln --no-restore`

Verification at this wave:

- `108` headless tests
- `5` web tests
- `7` PTY tests

## Remaining Open Depth

This wave closes the active contract-gap rows for command palette, log search,
macro recorder, and performance HUD.

The remaining explicit depth work stays in:

- terminal backend lifecycle/native host proof
- Windows ConPTY evidence
- routed-output/untrusted-output stress depth
- wider shared sample parity oracles
- pane parity breadth
- telemetry exporter + macro/redaction depth
- Mermaid real engine/render depth
