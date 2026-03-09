# 2026-03-09 Big-Batch Blockers

This note records blockers encountered during the base-port autorun that spans
the big-batch band from `252-KRN` through `355-VRF`.

## Active Blockers

- Cross-platform PTY execution cannot be validated from this Linux workspace.
  The .NET port now carries host matrix contracts and a Unix PTY runner, but
  actual Windows and macOS PTY lifecycle evidence requires external CI or
  native host validation.
- Raw-mode lifecycle semantics are represented in the .NET session model and
  cleanup plan, but this batch does not yet include direct `termios`/ConPTY
  manipulation. The current evidence therefore covers ANSI/session cleanup and
  PTY transcript behavior, not full OS-native raw-mode fidelity.
- One-writer behavior is enforced by the local writer gate and exercised through
  sequential PTY frame runs, but forced concurrent writer contention against one
  live PTY is not meaningfully reproducible in this workspace without a more
  invasive stress harness.

## Work Continued Around These Blockers

- Unix-host PTY tests were implemented and used as the current executable
  baseline for `355-VRF`.
- Windows and macOS expectations were encoded in the terminal host matrix and
  doctor output so the unsupported evidence gap is explicit rather than hidden.
- Session cleanup, inline-mode behavior, render/runtime/widget baselines, and
  deterministic headless/web verification continued without waiting for the
  blocked host-evidence pieces.
