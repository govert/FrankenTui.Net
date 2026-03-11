# 336-HST Inline Mode Divergence Ledger

## Status

- `status`: `closed`
- `classification`: `resolved local gap`
- `first recorded`: `2026-03-11`
- `resolved`: `2026-03-11`

## Summary

This gap is now closed. FrankenTui.Net inline mode no longer behaves as
"alternate-screen disabled plus newline after present". The local path now uses
a dedicated inline writer that saves/restores the cursor with DEC `ESC 7` /
`ESC 8`, clears stale inline rows when the region shrinks, routes inline logs
through the same writer gate, and is verified in headless and PTY tests.

The original visible symptom was messy inline repaint/clearing, especially in
mux and remote-terminal paths.

## Executed Host Path

- user-reported viewing path: `Windows Terminal -> SSH -> Linux`
- local observed execution path from the remote shell:
  `linux/unix-tty -> tmux`
- captured environment while investigating:
  - `TERM=tmux-256color`
  - `TERM_PROGRAM=tmux`
  - `SSH_CONNECTION` set
  - `TMUX` set

This should be classified as a Linux host path with mux involvement, not as a
local `windows/conpty` execution.

## Local Evidence

### Reproduction Command

```bash
script -qefc "dotnet run --project apps/FrankenTui.Demo.Showcase/FrankenTui.Demo.Showcase.csproj --no-build -- --inline --width 40 --height 8 --frames 2 --scenario tooling" /dev/null
```

### Closed Local Behavior

- the emitted control stream now includes DEC cursor save/restore
- the inline UI region is cleared explicitly, including shrink cases
- inline mode is no longer coupled to extra `Environment.NewLine` writes after
  each present
- demo interaction now polls the backend rather than bypassing it with ad hoc
  console input logic

## Affected Local Files

- [`../apps/FrankenTui.Demo.Showcase/Program.cs`](../apps/FrankenTui.Demo.Showcase/Program.cs)
- [`../src/FrankenTui.Backend/InlineTerminalWriter.cs`](../src/FrankenTui.Backend/InlineTerminalWriter.cs)
- [`../src/FrankenTui.Backend/TerminalOutputSanitizer.cs`](../src/FrankenTui.Backend/TerminalOutputSanitizer.cs)
- [`../src/FrankenTui.Backend/TerminalFeatureControl.cs`](../src/FrankenTui.Backend/TerminalFeatureControl.cs)
- [`../src/FrankenTui.Backend/ITerminalBackend.cs`](../src/FrankenTui.Backend/ITerminalBackend.cs)
- [`../src/FrankenTui.Backend/MemoryTerminalBackend.cs`](../src/FrankenTui.Backend/MemoryTerminalBackend.cs)
- [`../src/FrankenTui.Tty/TerminalSessionOptions.cs`](../src/FrankenTui.Tty/TerminalSessionOptions.cs)
- [`../src/FrankenTui.Tty/TerminalSession.cs`](../src/FrankenTui.Tty/TerminalSession.cs)
- [`../src/FrankenTui.Tty/ConsoleTerminalBackend.cs`](../src/FrankenTui.Tty/ConsoleTerminalBackend.cs)
- [`../tests/FrankenTui.Tests.Headless/TerminalBackendContractTests.cs`](../tests/FrankenTui.Tests.Headless/TerminalBackendContractTests.cs)
- [`../tests/FrankenTui.Tests.Pty/PtyIntegrationTests.cs`](../tests/FrankenTui.Tests.Pty/PtyIntegrationTests.cs)

## Upstream Basis Consulted

- basis commit:
  `7a91089366bd4644e086d5a422cb76b052e3de17`
- docs:
  - `.external/frankentui/docs/adr/ADR-001-inline-mode.md`
  - `.external/frankentui/docs/concepts/screen-modes.md`
- implementation:
  - `.external/frankentui/crates/ftui-core/src/inline_mode.rs`
  - `.external/frankentui/crates/ftui-runtime/src/terminal_writer.rs`
- upstream tests consulted:
  - `present_ui_saves_and_restores_cursor`
  - `present_ui_clears_ui_lines`
  - inline-region cleanup and resize handling in `terminal_writer.rs`

## Resolution Basis

The upstream basis already documented and tested the behavior needed to avoid
this class of artifact:

- inline mode preserves scrollback by saving and restoring the cursor
- inline UI lines are cleared explicitly before redraw
- runtime writer state tracks the last inline region so shrink and resize cases
  can clear stale rows

The local port now carries an equivalent inline writer subsystem and has local
verification for the corrected behavior. This did not require an upstream bug
report because the upstream basis was already correct; the work was a local port
closure.

## Verification

- headless:
  - `TerminalBackendContractTests.InlineModeUsesDecSaveRestoreAndClearsShrunkenRegion`
  - `TerminalBackendContractTests.InlineLogWritesAreSanitizedAndCursorProtected`
- PTY:
  - `PtyIntegrationTests.ShowcaseInlineModeAvoidsAlternateScreenAndKeepsVisibleTranscript`
  - `PtyIntegrationTests.ShowcaseInteractiveInlineModeConsumesInputAndExitsOnQuit`

## Follow-On Note

This file is retained as a historical divergence record. Any future inline-mode
differences should open a new active ledger only if they represent a fresh
behavioral mismatch, not merely a different internal optimization strategy.
