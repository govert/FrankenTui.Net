# 2026-03-12 Windows ConPTY Evidence Closure Record

## Context

FrankenTui.Net now carries a Windows terminal backend, a Windows host profile,
Windows CI execution for restore/build/test, and Windows doctor/showcase
artifact capture.

## Closure Outcome

The original blocker is now closed.

The closure evidence set consists of:

- a successful external local Windows build and doctor run
- local Windows inline showcase transcripts for the tooling and extras scenarios
- a local Windows interactive showcase transcript captured from PowerShell
- the pre-existing green Windows CI doctor/evidence lane

This moves Windows from `validated-ci` to `validated-external` in the maintained
host matrix. The primary Linux workspace still cannot execute native Windows
terminals directly, but the contract gap is no longer open because interactive
Windows host evidence now exists outside that workspace.

## Evidence Notes

- The Windows CI lane remains useful, but its host evidence is still a
  redirected runner console with blank `WT_SESSION`, `ConPty`, and `TERM`
  values.
- The external local Windows run is the evidence that closes the interactive
  ConPTY gap, because it includes an actual user-hosted interactive transcript
  rather than only redirected CI output.
- This record is retained so the closure basis stays explicit rather than being
  buried in commit history.
