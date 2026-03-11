# 2026-03-12 Windows ConPTY Evidence Blocker

## Context

FrankenTui.Net now carries a Windows terminal backend, a Windows host profile,
and Windows CI execution for restore/build/test. Doctor artifact generation is
also wired into the Windows CI lane.

## Remaining Blocker

The primary development workspace for this batch is Linux over SSH, so it still
cannot produce native interactive ConPTY execution evidence directly from the
authoring machine.

That leaves one honest remaining gap:

- no locally-collected Windows ConPTY transcript/evidence run for the current
  closure batch

## Current Mitigation

- Windows-specific code paths are exercised in the CI matrix.
- Doctor artifacts are now generated on Windows in CI.
- Windows CI now captures host evidence plus inline showcase transcripts under
  the `windows-doctor-artifacts` bundle.
- The host matrix and divergence ledgers continue to treat Windows as
  `validated-ci`, not `validated-local`.

## Current Evidence State

The current Windows CI lane proves that:

- the Windows build/test lane is green
- the Windows doctor/tooling lane runs and writes replay/contract artifacts
- inline showcase samples execute on a Windows host and produce terminal
  transcripts

It also proves what is still missing:

- `ci-windows-host-evidence.txt` currently shows blank `WT_SESSION`, `ConPty`,
  and `TERM` values
- the runner evidence therefore reflects a redirected Windows CI console, not a
  captured interactive Windows Terminal or SSH/ConPTY session

So the remaining blocker is no longer "any Windows evidence"; it is specifically
"interactive ConPTY evidence outside a redirected CI console".

## Exit Criteria

This blocker can be closed when a native Windows host run captures at least:

- a doctor artifact refresh
- an interactive demo/showcase transcript
- a host evidence update confirming the current interactive ConPTY path
