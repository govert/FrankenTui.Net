# inspector.rs (3,280L: 1,585 code + 1,695 tests)

## Status: ✅ Complete (1,585 code lines ported)

## Public free functions (in `InspectorDiagnostics` static class)
- [x] `Init()` — sets INSPECTOR_DIAGNOSTICS from env
- [x] `IsEnabled()` -> bool
- [x] `SetEnabled(enabled: bool)`
- [x] `ResetEventCounter()`
- [x] `IsDeterministicMode()` -> bool

## Types

### `DiagnosticEventKind` enum (11 variants) ✅
- [x] All 11 variants: InspectorToggled, ModeChanged, HoverChanged, SelectionChanged, DetailPanelToggled, HitsToggled, BoundsToggled, NamesToggled, TimesToggled, WidgetsCleared, WidgetRegistered
- [x] `AsStr()` extension method

### `DiagnosticEntry` class (17 fields + builder) ✅
- [x] All 17 fields: Seq, TimestampUs, Kind, Mode, PreviousMode, HoverPos, Selected, WidgetName, WidgetArea, WidgetDepth, WidgetHitId, WidgetCount, Flag, Enabled, Context, Checksum
- [x] `DiagnosticEntry(kind)` constructor
- [x] Builder: `WithMode()`, `WithPreviousMode()`, `WithHoverPos()`, `WithSelected()`, `WithWidget()`, `WithWidgetCount()`, `WithFlag()`, `WithContext()`, `WithChecksum()`
- [x] `IDiagnosticRecord.ToJsonl()` — full JSONL serialization

### `TelemetryHooks` class ✅
- [x] `OnToggle()`, `OnModeChange()`, `OnHoverChange()`, `OnSelectionChange()`, `OnAny()` builder methods
- [x] `IDiagnosticHookDispatch<DiagnosticEntry>.Dispatch()` — dispatches to registered callbacks

### `InspectorMode` enum (4 variants) ✅
- [x] Hits, Bounds, Names, Times
- [x] `Cycle()`, `IsActive()`, `AsStr()`, `ShowHitRegions()`, `ShowWidgetBounds()` extensions

### `WidgetInfo` class ✅
- [x] `WidgetInfo(name, area)` constructor
- [x] Fields: Name, Area, HitId, RenderTimeUs, Depth, Children, HitRegions
- [x] Builder: `WithHitId()`, `WithRenderTimeUs()`, `WithDepth()`
- [x] `AddChild()`, `AddHitRegion()`
- [x] `BoundColor(depth)` static method
- [x] `RegionColor(region)` static method

### `InspectorState` class ✅
- [x] `WithDiagnostics()`, `WithTelemetryHooks()` builder methods
- [x] `DiagnosticLog` property
- [x] `Toggle()`, `IsActive`
- [x] `CycleMode()`, `SetMode()`, `SetHover()`
- [x] `Select()`, `ClearSelection()`, `ToggleDetailPanel()`
- [x] `ToggleHits()`, `ToggleBounds()`, `ToggleNames()`, `ToggleTimes()`
- [x] `ClearWidgets()`, `RegisterWidget()`
- [x] `ShouldShowHits()`, `ShouldShowBounds()`

### `InspectorOverlay` class ✅
- [x] `InspectorOverlay(InspectorState)` — constructor
- [x] `Render(buffer, area, hitGrid)` — renders hit regions, widget bounds, detail panel
- [x] `RenderHitRegions()` — colored dot overlay on each hit region cell
- [x] `RenderWidgetBounds()` — colored borders per widget depth + name/time labels
- [x] `RenderDetailPanel()` — info panel for selected widget

### `OverlayHitInfo` struct ✅
- [x] `FromCell(HitCell, x, y)` -> OverlayHitInfo?

### Supporting types added to `FrankenTui.Render/HitRegion.cs` ✅
- [x] `HitId` — uint wrapper
- [x] `HitRegionKind` — None/Content/Border/Scrollbar/Handle/Button/Link/Custom
- [x] `HitData`, `HitOwner`, `HitTestResult` — metadata structs
- [x] `HitCell` — per-cell hit state
- [x] `HitGrid` — full hit testing grid with register/hit-test/clear

### `HitInfo` struct ✅
- [x] `HitInfo(HitId, Area)` constructor
- [x] `FromCell(HitCell, x, y)` -> HitInfo?

## Tests (1,695 lines)
- [ ] All ported to test project (not yet)
