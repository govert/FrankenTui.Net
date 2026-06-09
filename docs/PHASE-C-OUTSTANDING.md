# Phase C — Final Status

All Phase C work complete. Updated 2026-05-30.

## Test Counts

| Module | Rust | C# | Status |
|--------|:---:|:---:|:---:|
| C1 Clipboard | 89 | 87 | ✅ Complete — class hierarchy, set_with_fallback |
| C2 Console | 58 | 77 | ✅ Complete — exceeds upstream, 77 tests |
| C3 Logging | 49 | 29 | ✅ Complete — TracingConsoleLayer ported, 31 tracing Layer tests N/A |
| C4 Export | 85 | 77 | ✅ Complete — HtmlExporter, SvgExporter, TextExporter |
| C5 Live | 46 | 40 | ✅ Complete — sanitize fix, 40 tests |
| C6a PtyCapture | 32 | 28 | ✅ Complete — Resize() added |
| C6b StdioCapture | 19 | 16 | ✅ Complete |
| **Total** | **378** | **354** | **Phase C complete** |

### Progress: 181 → 354 tests (+173, +96%)

## What Was Done (Full Session)

### C1: Clipboard — Rewritten + Class Hierarchy + set_with_fallback
- **Rewritten from scratch** (was 16KB null bytes)
- `ClipboardError` changed from enum to sealed abstract record class hierarchy with detail strings: `NotAvailable`, `InvalidInput(string Detail)`, `WriteError(string Detail)`, `ReadError(string Detail)`, `Timeout`
- Added `SetWithFallback()` method
- 87 tests updated to `Assert.IsType<T>()` patterns

### C2: Console — P1 Alignment + Full Test Coverage
- Replaced `ConsoleStyle(string?,string?)` with `FrankenTui.Style.UiStyle`
- Added `SegmentControl` enum (Newline, CarriageReturn)
- Grapheme-aware width via `TerminalTextWidth.DisplayWidth()`
- Ported `SplitNextWord()`, `SplitAtWidth()` helpers
- `LineCount` property, style stack merge matching upstream
- `ConsoleSink` sanitize test, `IntoCaptured` uses `\n`
- 77 tests (exceeds upstream 58)

### C3: Logging — TracingConsoleLayer Ported
- Ported full `TracingConsoleLayer` with builder pattern, console access guard, timestamp/level/target/message/fields/source formatting
- 11 layer tests + 6 style tests
- 29 tests total

### C4: Export — Full Exporter Coverage
- `HtmlExporter` (inline + CSS-class), `SvgExporter`, `TextExporter` (plain + ANSI)
- `ExportHelpers` (CellDisplayText, HtmlEscapeInto, SvgEscapeInto)
- 77 tests covering HTML, SVG, Text ANSI, escape helpers

### C5: LiveStream — Sanitize Fix + Edge Cases
- `Sanitize()`: regex → character-level control-char filtering
- 40 tests covering overflow, cursor, sanitize

### C6a: PtyCapture — Resize Added
- `Resize(ushort, ushort)` on `IPtySession`, UnixPtySession (TIOCSWINSZ), WindowsPtySession (ResizePseudoConsole), PtyCapture

## Remaining Gaps (All Deferred or N/A)

| Gap | Reason |
|------|--------|
| Clipboard `ClipboardError` std::error traits (2 tests) | No `Error` trait in .NET |
| Logging tracing `Layer` integration (31 tests) | No `tracing` crate in .NET |
| Live Send+Sync/Clone/Debug/Eq (8 tests) | .NET GC handles threading; records have equality |
| Export Debug/Clone derives (3 tests) | C# records have built-in equality and `with` |
| StdioCapture poisoned lock (3 tests) | .NET has no mutex poisoning |
| PtyCapture drop/threading edge cases (4 tests) | Platform-specific threading |
