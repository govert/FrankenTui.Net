# FrankenTui.Net — Porting Execution Plan

Generated 2026-05-26. Targets: 80 remaining items across ftui-widgets, ftui-extras, ftui-render, ftui-runtime.

## Phase A: Low-Effort Utilities (1 session, ~5 items)

Quick wins — self-contained, no dependencies, each < 1000 lines.

| # | Module | Target File | Effort |
|---|---|---|---|
| A1 | `filesize.rs` (635L) | `FrankenTui.Extras/FileSize.cs` | ✅ |
| A2 | `borders.rs` (414L) | `FrankenTui.Widgets/BorderPresets.cs` | ✅ |
| A3 | `mouse.rs` (16L) | `FrankenTui.Widgets/MouseSupport.cs` | ✅ |
| A4 | `list_helper.rs` (2L) | Merged into `ListWidget.cs` | ✅ |
| A5 | `diagnostics.rs` (495L) | `FrankenTui.Widgets/Diagnostics.cs` | ✅ |

## Phase B: Widget Utilities (1-2 sessions, ~8 items)

Widget infrastructure — completes the widget layer.

| # | Module | Target File | Effort |
|---|---|---|---|
| B1 | `undo_support.rs` (820L) | `FrankenTui.Widgets/WidgetInfrastructure.cs` | ✅ |
| B2 | `state.rs` + `stateful.rs` (1707L) | `FrankenTui.Widgets/WidgetInfrastructure.cs` | ✅ |
| B3 | `measurable.rs` + `measure_cache.rs` (1683L) | `FrankenTui.Widgets/WidgetInfrastructure.cs` | ✅ |
| B4 | `inspector.rs` (3280L) | `FrankenTui.Widgets/InspectorAndDrag.cs` | ✅ |
| B5 | `drag.rs` (1549L) | `FrankenTui.Widgets/InspectorAndDrag.cs` | ✅ |

## Phase C: System Utilities (1 session, ~6 items)

Self-contained modules — clipboard, console, logging, export, etc.

| # | Module | Target File | Effort |
|---|---|---|---|
| C1 | `clipboard.rs` (1861L) | `FrankenTui.Extras/SystemUtilities.cs` | ✅ |
| C2 | `console.rs` (1406L) | `FrankenTui.Extras/SystemUtilities.cs` | ✅ |
| C3 | `logging.rs` (1436L) | `FrankenTui.Extras/SystemUtilities.cs` | ✅ |
| C4 | `export.rs` (1691L) | `FrankenTui.Extras/SystemUtilities.cs` | ✅ |
| C5 | `live.rs` (1475L) | `FrankenTui.Extras/SystemUtilities.cs` | ✅ |
| C6 | `pty_capture.rs` + `stdio_capture.rs` (1269L) | `FrankenTui.Extras/SystemUtilities.cs` | ✅ |

## Phase D: Data Structures (1 session, ~5 items)

Algorithm/data structure ports — self-contained, testable.

| # | Module | Target File | Effort |
|---|---|---|---|
| D1 | `adaptive_radix.rs` (1000L) | `FrankenTui.Runtime/AdaptiveRadix.cs` | 30 min |
| D2 | `elias_fano.rs` (903L) | `FrankenTui.Runtime/EliasFano.cs` | 30 min |
| D3 | `fenwick.rs` (851L) | `FrankenTui.Runtime/FenwickTree.cs` | 30 min |
| D4 | `louds.rs` (835L) | `FrankenTui.Runtime/Louds.cs` | 30 min |
| D5 | `countmin_sketch.rs` (1022L) | `FrankenTui.Runtime/CountMinSketch.cs` | 30 min |

## Phase E: Runtime Core (1-2 sessions, ~8 items)

Runtime infrastructure — retry, cancellation, locale, etc.

| # | Module | Target File | Effort |
|---|---|---|---|
| E1 | `retry.rs` (697L) | `FrankenTui.Runtime/RetryPolicy.cs` | 25 min |
| E2 | `cancellation.rs` (251L) | `FrankenTui.Runtime/Cancellation.cs` | 15 min |
| E3 | `locale.rs` (323L) | `FrankenTui.Runtime/Locale.cs` | 15 min |
| E4 | `debug_trace.rs` (93L) | `FrankenTui.Runtime/DebugTrace.cs` | 10 min |
| E5 | `log_sink.rs` (275L) | `FrankenTui.Runtime/LogSink.cs` | 15 min |
| E6 | `subscription.rs` (2037L) | `FrankenTui.Runtime/Subscription.cs` | 45 min |
| E7 | `policy_registry.rs` (452L) | `FrankenTui.Runtime/PolicyRegistry.cs` | 20 min |
| E8 | `schema_compat.rs` (672L) | `FrankenTui.Runtime/SchemaCompat.cs` | 25 min |

## Phase F: Telemetry (1-2 sessions, ~8 items)

Evidence, telemetry, and tracing infrastructure.

| # | Module | Target File | Effort |
|---|---|---|---|
| F1 | `telemetry.rs` (2108L) | `FrankenTui.Runtime/Telemetry.cs` | 45 min |
| F2 | `telemetry_schema.rs` (282L) | `FrankenTui.Runtime/TelemetrySchema.cs` | 15 min |
| F3 | `metrics_registry.rs` (714L) | `FrankenTui.Runtime/MetricsRegistry.cs` | 30 min |
| F4 | `event_trace.rs` (2254L) | `FrankenTui.Runtime/EventTrace.cs` | 45 min |
| F5 | `schedule_trace.rs` (1541L) | `FrankenTui.Runtime/ScheduleTrace.cs` | 30 min |
| F6 | `timeline_aggregator.rs` (990L) | `FrankenTui.Runtime/TimelineAggregator.cs` | 30 min |
| F7 | `evidence_bridges.rs` (520L) | `FrankenTui.Runtime/EvidenceBridges.cs` | 20 min |
| F8 | `evidence_telemetry.rs` (502L) | `FrankenTui.Runtime/EvidenceTelemetry.cs` | 20 min |

## Phase G: Performance Governance (1-2 sessions, ~7 items)

Load governance, scheduling, and quality infrastructure.

| # | Module | Target File | Effort |
|---|---|---|---|
| G1 | `allocation_budget.rs` (1405L) | `FrankenTui.Runtime/AllocationBudget.cs` | 30 min |
| G2 | `eprocess_throttle.rs` (1658L) | `FrankenTui.Runtime/EProcessThrottle.cs` | 45 min |
| G3 | `cost_model.rs` (1788L) | `FrankenTui.Runtime/CostModel.cs` | 45 min |
| G4 | `input_fairness.rs` (1214L) | `FrankenTui.Runtime/InputFairness.cs` | 30 min |
| G5 | `slo.rs` (830L) | `FrankenTui.Runtime/SloManager.cs` | 30 min |
| G6 | `validation_pipeline.rs` (1960L) | `FrankenTui.Runtime/ValidationPipeline.cs` | 45 min |
| G7 | `transparency.rs` (474L) | `FrankenTui.Runtime/Transparency.cs` | 20 min |

## Phase H: Advanced Math/Stats (2 sessions, ~12 items)

Analytical engine — conformal prediction, VOI, decision theory.

| # | Module | Target File | Effort |
|---|---|---|---|
| H1 | `conformal_stages.rs` (532L) | Extend `LoadGovernor.cs` | 20 min |
| H2 | `alpha_investing.rs` (561L) | `FrankenTui.Runtime/AlphaInvesting.cs` | 25 min |
| H3 | `bocpd.rs` (1956L) | `FrankenTui.Runtime/Bocpd.cs` | 45 min |
| H4 | `decision_core.rs` (569L) | `FrankenTui.Runtime/DecisionCore.cs` | 25 min |
| H5 | `voi_sampling.rs` (2299L) | `FrankenTui.Runtime/VoiSampling.cs` | 45 min |
| H6 | `voi_telemetry.rs` (214L) | `FrankenTui.Runtime/VoiTelemetry.cs` | 15 min |
| H7 | `unified_evidence.rs` (1109L) | `FrankenTui.Runtime/UnifiedEvidence.cs` | 30 min |
| H8 | `diff_evidence.rs` (796L) | `FrankenTui.Runtime/DiffEvidence.cs` | 25 min |
| H9 | `ivm.rs` (1549L) | `FrankenTui.Runtime/IncrementalViewMaintenance.cs` | 45 min |
| H10 | `sinkhorn_morph.rs` (1076L) | `FrankenTui.Runtime/SinkhornMorph.cs` | 30 min |
| H11 | `reversible.rs` + `rough_path.rs` (1295L) | `FrankenTui.Runtime/MathPrimitives.cs` | 30 min |
| H12 | `sos_barrier.rs` + `coeffs` (302L) | `FrankenTui.Runtime/SosBarrier.cs` | 20 min |

## Phase I: Remaining Rendering (2 sessions, ~12 items)

Complete the partial rendering ports.

| # | Module | Target File | Effort |
|---|---|---|---|
| I1 | `drawing.rs` full port (1261L) | Extend `DrawingPrimitives.cs` | 30 min |
| I2 | `headless.rs` (844L) | `FrankenTui.Render/HeadlessRenderer.cs` | 30 min |
| I3 | `link_registry.rs` (709L) | `FrankenTui.Render/LinkRegistry.cs` | 25 min |
| I4 | `sanitize.rs` (1564L) | `FrankenTui.Render/Sanitizer.cs` | 30 min |
| I5 | `spatial_hit_index.rs` (1979L) | `FrankenTui.Render/SpatialHitIndex.cs` | 45 min |
| I6 | `diagram.rs` + `dot_parser.rs` (2639L) | `FrankenTui.Extras/DiagramSupport.cs` | 60 min |
| I7 | `diagram_layout.rs` (3085L) | `FrankenTui.Extras/DiagramLayout.cs` | 60 min |
| I8 | `markdown.rs` full port (3896L) | Extend `MarkdownDocumentBuilder.cs` | 60 min |
| I9 | `mermaid*.rs` full port (37K) | Extend `MermaidEngine.cs` | 120 min |
| I10 | `forms.rs` full port (3268L) | Extend `FormWidgets.cs` | 60 min |
| I11 | `help*.rs` (4935L) | `FrankenTui.Widgets/HelpSystem.cs` | 60 min |
| I12 | `asciicast.rs` (453L) | `FrankenTui.Runtime/Asciicast.cs` | 20 min |

## Execution Order

```
A1-A5 → B1-B5 → C1-C6 → D1-D5 → E1-E8 → F1-F8 → G1-G7 → H1-H12 → I1-I12
  1hr      2hr      2hr      1hr      2hr        2hr       2hr       3hr        5hr
```

**Total estimated: ~22 hours across 12-15 sessions.**
Each session: port modules, build, run 699 tests, review, commit.

## Per-Session Checklist

Before commit:
- [ ] `dotnet build` — 0 errors, 0 warnings
- [ ] `dotnet test FrankenTui.Net.sln` — all 699+ pass
- [ ] Review: no unused usings, consistent naming, XML docs on public types
- [ ] Mark items complete in this document
- [ ] Commit with descriptive message mentioning ported modules
