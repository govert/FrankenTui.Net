# 335-HST Host Divergence Ledger

## Purpose

This is the maintained host-validation and divergence ledger for terminal
platforms currently modeled by FrankenTui.Net.

This covers `335-HST` from
[`200-PRT-port-work-breakdown.md`](./200-PRT-port-work-breakdown.md).

## Host Matrix

| Platform | Host | Validation Status | Evidence Sources | Known Divergences | Capability Override Policy |
| --- | --- | --- | --- | --- | --- |
| `linux` | `unix-tty` | `validated-local` | `headless`, `pty`, `doctor`, `ci-linux` | raw-mode ownership stays at the managed backend boundary; transcript evidence is stronger than raw TTY state capture; inline-mode parity gap is closed locally and retained as a historical record in [336-HST-inline-mode-divergence-ledger.md](./336-HST-inline-mode-divergence-ledger.md) | prefer modern ANSI features and degrade cleanly when terminal metadata is sparse |
| `macos` | `unix-tty` | `validated-external` | `headless`, `design-contract`, `pending-ci-macos` | no native macOS PTY evidence is produced from the primary Linux workspace; capability tuning currently follows the shared Unix policy | assume modern ANSI support and re-check mux behavior before widening host-specific upgrades |
| `windows` | `conpty` | `validated-external` | `headless`, `ci-windows`, `ci-windows-doctor`, `ci-windows-inline-transcripts`, `windows-local-doctor`, `windows-local-inline`, `windows-local-interactive` | the primary Linux workspace still cannot execute native Windows terminals directly; Windows CI evidence is still a redirected runner console with blank `WT_SESSION`, `ConPty`, and `TERM` values; in-repo PTY assertions remain Unix-only even though external Windows transcript evidence now exists; upstream `native-backend` entry points are Unix-only and point Windows users to the compatibility backend, which maps locally to the managed Windows console backend; the closure record is retained in [2026-03-12-windows-conpty-evidence-blocker.md](./2026-03-12-windows-conpty-evidence-blocker.md) | prefer the Windows console capability set and avoid Unix-only toggles unless explicitly supported; do not route Windows showcase runs through Unix-native backend assumptions |

## Remoting And Mux Classification

- Classify a remote session by the host that actually executes the TUI code.
- `Windows Terminal -> SSH -> Linux` is recorded under the Linux host row, not
  under the Windows `conpty` row.
- If `tmux`, `screen`, or another mux is active, record that in the relevant
  divergence note because upstream inline-mode strategy and synchronized output
  policy are mux-sensitive.

## Usage Rule

When host-specific work changes terminal assumptions, update both:

1. the code-level matrix in `src/FrankenTui.Tty/TerminalHostMatrix.cs`
2. this ledger entry

Do not rely on commit messages alone for host-surface changes.
