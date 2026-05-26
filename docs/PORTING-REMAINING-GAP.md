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

## ftui-runtime — Comprehensive Audit (62 upstream modules)

### Ported (functional equivalents)

| Upstream | Lines | .NET Equivalent | Status |
|---|---|---|---|
| `program.rs` | 14244 | `AppRuntime.cs` + `AppSession.cs` | ✅ Core loop |
| `degradation_cascade.rs` | 732 | `DegradationCascade.cs` | ✅ |
| `effect_system.rs` | 825 | `EffectSystem.cs` | ✅ |
| `resize_coalescer.rs` | 4353 | `ResizeCoalescer.cs` + `LoadGovernor.cs` | ✅ Partial |
| `queueing_scheduler.rs` | 2891 | `RuntimeQueueingScheduler.cs` | ✅ |
| `conformal_predictor.rs` | 1823 | `LoadGovernor.cs` | ⚠️ Partial |
| `conformal_frame_guard.rs` | 786 | `LoadGovernor.cs` | ⚠️ Partial |
| `conformal_alert.rs` | 1821 | `LoadGovernor.cs` | ⚠️ Partial |
| `conformal_stages.rs` | 532 | Not ported | ❌ |
| `simulator.rs` | 1748 | `AppSimulator.cs` | ✅ |
| `terminal_writer.rs` | 6493 | Console backend (in `FrankenTui.Tty`) | ✅ |
| `render_thread.rs` | 771 | `RuntimeInputController.cs` | ⚠️ Partial |
| `render_trace.rs` | 1507 | `RuntimeInputEngine.cs` | ⚠️ Partial |
| `policy_config.rs` | 1417 | `RuntimePolicyConfig.cs` | ⚠️ Partial |
| `state_persistence.rs` | 1443 | Pane workspace in showcase | ⚠️ Partial |
| `wasm_runner.rs` | 779 | `ShowcaseRunnerCore.cs` | ✅ |
| `input_macro.rs` | 1914 | `MacroRecorder.cs` | ⚠️ Partial |
| `evidence_sink.rs` | 469 | `ShowcaseEvidenceJsonlWriter.cs` | ⚠️ Partial |
| `evidence_bridges.rs` | 520 | Not ported | ❌ |
| `evidence_telemetry.rs` | 502 | Not ported | ❌ |
| `unified_evidence.rs` | 1109 | Not ported | ❌ |
| `diff_evidence.rs` | 796 | Not ported | ❌ |

### Not Ported

| # | Module | Lines | Type | Notes |
|---|---|---|---|---|
| 42 | `allocation_budget.rs` | 1405 | Performance | Allocation budgeting |
| 43 | `alpha_investing.rs` | 561 | Statistics | Sequential testing |
| 44 | `asciicast.rs` | 453 | Recording | Asciicast format |
| 45 | `bocpd.rs` | 1956 | Statistics | Bayesian change point detection |
| 46 | `cancellation.rs` | 251 | Runtime | Task cancellation tokens |
| 47 | `cost_model.rs` | 1788 | Scheduling | Cost model for priority |
| 48 | `countmin_sketch.rs` | 1022 | DataStructure | Count-min sketch |
| 49 | `debug_trace.rs` | 93 | Debug | Debug trace utilities |
| 50 | `decision_core.rs` | 569 | Decision | Decision core engine |
| 51 | `eprocess_throttle.rs` | 1658 | Performance | E-process throttling |
| 52 | `event_trace.rs` | 2254 | Telemetry | Event tracing infrastructure |
| 53 | `flake_detector.rs` | 1027 | Testing | Flaky test detection |
| 54 | `flat_combine.rs` | 589 | DataStructure | Flat combine structure |
| 55 | `input_fairness.rs` | 1214 | Scheduling | Fair input scheduling |
| 56 | `ivm.rs` | 1549 | Data | Incremental view maintenance |
| 57 | `lens.rs` | 542 | Combinator | Lens/combinator patterns |
| 58 | `locale.rs` | 323 | i18n | Locale support |
| 59 | `log_sink.rs` | 275 | Logging | Log sink |
| 60 | `metrics_registry.rs` | 714 | Telemetry | Metrics registry |
| 61 | `policy_registry.rs` | 452 | Config | Policy registry |
| 62 | `process_subscription.rs` | 969 | Runtime | Process subscription |
| 63 | `retry.rs` | 697 | Utility | Retry logic with backoff |
| 64 | `reversible.rs` | 826 | Data | Reversible computation |
| 65 | `rough_path.rs` | 469 | Math | Rough path theory |
| 66 | `schedule_trace.rs` | 1541 | Telemetry | Schedule tracing |
| 67 | `schema_compat.rs` | 672 | Data | Schema compatibility |
| 68 | `slo.rs` | 830 | Quality | SLO management |
| 69 | `sos_barrier.rs` | 257 | Math | SOS barrier method |
| 70 | `sos_barrier_coeffs.rs` | 45 | Math | Barrier coefficients |
| 71 | `stdio_capture.rs` | 549 | System | Stdio capture |
| 72 | `string_model.rs` | 507 | Data | String model |
| 73 | `subscription.rs` | 2037 | Runtime | Subscription system |
| 74 | `telemetry.rs` | 2108 | Telemetry | Core telemetry |
| 75 | `telemetry_schema.rs` | 282 | Telemetry | Telemetry schema |
| 76 | `timeline_aggregator.rs` | 990 | Data | Timeline aggregation |
| 77 | `transparency.rs` | 474 | Quality | Transparency reporting |
| 78 | `validation_pipeline.rs` | 1960 | Quality | Validation pipeline |
| 79 | `voi_sampling.rs` | 2299 | Decision | Value-of-information sampling |
| 80 | `voi_telemetry.rs` | 214 | Decision | VOI telemetry |

### Summary

- **17 modules** have .NET equivalents (5 complete, 12 partial)
- **39 modules** are not ported — primarily advanced performance/telemetry/scheduling/statistics infrastructure
- The .NET runtime covers the core program loop (init → update → render → repeat) but lacks the upstream's deep performance governance (conformal prediction, degradation cascade tiers, e-process throttling), telemetry system (event traces, metrics, evidence sinks), and advanced scheduling (cost models, fairness, process subscriptions)

## Already Ported (confirmed complete)

**ftui-widgets (38 .NET files):** Block, Panel, Paragraph, List, Table, Tabs, Progress, TextArea, Tree, Scrollbar, Stack, Padding, Sparkline, Toast, Spinner, Group, DecisionCard, Badge, Emoji, StatusLine, Stopwatch, Paginator, Pretty, Timer, JsonView, HistoryPanel, ValidationError, ConstraintOverlay, LayoutDebugger, Rule, Popover, NotificationQueue, LogRingBuffer, LogViewer, TextInput, VirtualizedList, FilePicker, DebugOverlay, KeyboardDrag, VoiDebugOverlay, DriftVisualization, ErrorBoundary, Columns, Align, Cached, Choreography, HeightPredictor, DrawingPrimitives (BorderChars)

**ftui-extras (27 .NET files):** VisualFx (PlasmaFx, MetaballsFx), GlowingText, Image, ThemeSystem (15 themes, ThemePalette), TextEffects (9 effects), CanvasPrimitives (CanvasMode, Painter, CanvasWidget), ChartWidgets (BarChart, LineChart), SyntaxHighlighter, TracebackWidget, FormWidgets, MarkdownDocument, MermaidEngine, HintRanker, TimingWidgets, HelpWidgets, DashboardSurface, ConsoleText, CommandPalette, BufferExport, ExtrasShowcaseFactory, HostedParity*, LogSearch, MacroRecorder, PaneWorkspaceWidget, PerformanceHud, Validation

**ftui-render (core):** Buffer, Cell, Frame (via RuntimeRenderContext), PackedRgba, presenter (via Console backend), diff (BufferDiff), grapheme_pool (via CellContent)

**ftui-runtime (core loop):** AppRuntime, AppSession, AppSimulator, DegradationCascade, EffectSystem, ResizeCoalescer, RuntimeQueueingScheduler, LoadGovernor, RuntimeInputController, RuntimeInputEngine, GestureRecognizer, KeybindingResolver, RuntimeFrameStats, RuntimeRenderContext, ReplayTape (17 modules covering the essential program lifecycle)
