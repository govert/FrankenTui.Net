# FrankenTui.Net — Remaining Upstream Gap

Generated 2026-05-26. All ftui-widgets rendering widgets and ftui-extras rendering effects are ported.
Remaining gaps are data structures, system utilities, debug tools, and specialized subsystems.

## ftui-widgets — Remaining (data structures / utilities only)

| # | Module | Lines | Type | Notes |
|---|---|---|---|---|
| 1 | `adaptive_radix.rs` | 1000 | Data structure | Adaptive radix tree |
| 2 | `borders.rs` | 414 | Utility | Border type system (partial in DrawingPrimitives.cs) |
| 3 | `diagnostics.rs` | 495 | Utility | Diagnostic utilities |
| 4 | `drag.rs` | 1549 | Interaction | Full drag support (partial in showcase screen 44) |
| 5 | `elias_fano.rs` | 903 | Data structure | Elias-Fano encoding |
| 6 | `fenwick.rs` | 851 | Data structure | Fenwick tree for prefix sums |
| 7 | `help.rs` | 3436 | Utility | Help system |
| 8 | `help_index.rs` | 901 | Utility | Help index |
| 9 | `help_registry.rs` | 598 | Utility | Help registry |
| 10 | `inspector.rs` | 3280 | Debug tool | Widget inspector |
| 11 | `list_helper.rs` | 2 | Utility | Trivial helper |
| 12 | `louds.rs` | 835 | Data structure | LOUDS encoding |
| 13 | `measurable.rs` | 676 | Utility | Measurement trait |
| 14 | `measure_cache.rs` | 1007 | Utility | Measurement LRU cache |
| 15 | `mouse.rs` | 16 | Utility | Mouse support stub |
| 16 | `state.rs` | 5 | Utility | Widget state trait |
| 17 | `stateful.rs` | 1702 | Utility | Stateful widget base |
| 18 | `undo_support.rs` | 820 | Utility | Undo/redo infrastructure |

## ftui-extras — Remaining

| # | Module | Lines | Type | Notes |
|---|---|---|---|---|
| 19 | `clipboard.rs` | 1861 | System | System clipboard integration |
| 20 | `console.rs` | 1406 | System | Console utilities |
| 21 | `diagram.rs` | 1047 | Rendering | Diagram types (partial in MermaidEngine.cs) |
| 22 | `diagram_layout.rs` | 3085 | Rendering | Diagram layout engine |
| 23 | `dot_parser.rs` | 1592 | Rendering | DOT format parser |
| 24 | `export.rs` | 1691 | System | Buffer/JSON/file export |
| 25 | `filesize.rs` | 635 | Utility | File size formatting |
| 26 | `forms.rs` | 3268 | Rendering | Form widgets (partial in FormWidgets.cs) |
| 27 | `live.rs` | 1475 | System | Live streaming support |
| 28 | `logging.rs` | 1436 | System | Logging infrastructure |
| 29 | `markdown.rs` | 3896 | Rendering | Full Markdown (partial in MarkdownDocumentBuilder.cs) |
| 30 | `mermaid.rs` | 14691 | Rendering | Mermaid core (partial in MermaidEngine.cs) |
| 31 | `mermaid_diff.rs` | 1830 | Rendering | Mermaid diff |
| 32 | `mermaid_layout.rs` | 10695 | Rendering | Mermaid layout |
| 33 | `mermaid_minimap.rs` | 1474 | Rendering | Mermaid minimap |
| 34 | `mermaid_render.rs` | 9349 | Rendering | Mermaid renderer |
| 35 | `pty_capture.rs` | 720 | System | PTY capture |
| 36 | `sinkhorn_morph.rs` | 1076 | Math | Sinkhorn optimal transport |

## ftui-render — Remaining

| # | Module | Lines | Type | Notes |
|---|---|---|---|---|
| 37 | `drawing.rs` | 1261 | Rendering | Border drawing (partial in DrawingPrimitives.cs) |
| 38 | `headless.rs` | 844 | Rendering | Headless rendering mode |
| 39 | `link_registry.rs` | 709 | Rendering | OSC-8 hyperlink registry |
| 40 | `sanitize.rs` | 1564 | Rendering | Output sanitization |
| 41 | `spatial_hit_index.rs` | 1979 | Rendering | Spatial hit testing index |

## ftui-runtime — Not comprehensively audited

The runtime crate has 40+ modules. Key ones with .NET equivalents:
- `program.rs` → AppRuntime.cs
- `render_thread.rs` → partial
- `terminal_writer.rs` → Console backend
- `evidence_*.rs` → ShowcaseEvidenceJsonlWriter.cs (partial)
- `degradation_cascade.rs` → partial
- `conformal_*.rs` → partial
- `bocpd.rs` → not ported
- `resize_coalescer.rs` → not ported
- `queueing_scheduler.rs` → not ported
- `input_fairness.rs` → not ported
- Many others → not ported

## Already Ported (confirmed complete)

**ftui-widgets (38 .NET files):** Block, Panel, Paragraph, List, Table, Tabs, Progress, TextArea, Tree, Scrollbar, Stack, Padding, Sparkline, Toast, Spinner, Group, DecisionCard, Badge, Emoji, StatusLine, Stopwatch, Paginator, Pretty, Timer, JsonView, HistoryPanel, ValidationError, ConstraintOverlay, LayoutDebugger, Rule, Popover, NotificationQueue, LogRingBuffer, LogViewer, TextInput, VirtualizedList, FilePicker, DebugOverlay, KeyboardDrag, VoiDebugOverlay, DriftVisualization, ErrorBoundary, Columns, Align, Cached, Choreography, HeightPredictor, DrawingPrimitives (BorderChars)

**ftui-extras (27 .NET files):** VisualFx (PlasmaFx, MetaballsFx), GlowingText, Image, ThemeSystem (15 themes, ThemePalette), TextEffects (9 effects), CanvasPrimitives (CanvasMode, Painter, CanvasWidget), ChartWidgets (BarChart, LineChart), SyntaxHighlighter, TracebackWidget, FormWidgets, MarkdownDocument, MermaidEngine, HintRanker, TimingWidgets, HelpWidgets, DashboardSurface, ConsoleText, CommandPalette, BufferExport, ExtrasShowcaseFactory, HostedParity*, LogSearch, MacroRecorder, PaneWorkspaceWidget, PerformanceHud, Validation

**ftui-render (core):** Buffer, Cell, Frame (via RuntimeRenderContext), PackedRgba, presenter (via Console backend), diff (BufferDiff), grapheme_pool (via CellContent)
