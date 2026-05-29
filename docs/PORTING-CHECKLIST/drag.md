# drag.rs (1,549L: 795 code + 754 tests)

## Status: ✅ Complete (795 code lines ported)

## Types

### `DragPayload` class ✅
- [x] `DragPayload(dragType, data)` — constructor
- [x] `static Text(text)` — creates text/plain payload
- [x] `WithDisplayText(text)` — builder
- [x] `AsText()` -> string?
- [x] `DataLen` -> int
- [x] `MatchesType(pattern)` -> bool (supports `*`, `*/*`, `prefix/*`)

### `DragConfig` class ✅
- [x] Properties: `ThresholdCells` (3), `StartDelayMs` (0), `CancelOnEscape` (true)
- [x] `WithThreshold(cells)`, `WithDelay(ms)`, `NoEscapeCancel()`

### `DragState` class ✅
- [x] `DragState(sourceId, payload, startX, startY)` — constructor
- [x] Properties: `SourceId`, `Payload`, `StartPos`, `CurrentPos`, `Preview`
- [x] `WithPreview(widget)` — builder
- [x] `UpdatePosition(x, y)`
- [x] `Distance()` -> uint (Manhattan)
- [x] `Delta()` -> (int Dx, int Dy)

### `IDraggable` interface ✅
- [x] `DragType` property
- [x] `CanStartDrag(x, y)`
- [x] `OnDragStart(x, y)` -> DragPayload?
- [x] `OnDragEnd(success)`

### `DropPosition` enum ✅
- [x] `Before`, `After`, `On`
- [x] `DropPositionHelper.FromList(y, itemHeight, itemCount)`

### `DropResult` class ✅
- [x] `static Accepted()`, `Rejected(reason?)`, `Cancelled()`
- [x] `IsAccepted` property

### `IDropTarget` interface ✅
- [x] `AcceptsType` property
- [x] `CanDrop(payload, x, y)`
- [x] `OnDrop(payload, x, y, position)` -> DropResult
- [x] `GetDropPosition(x, y)` -> DropPosition

### `DragPreviewConfig` class ✅
- [x] Properties with defaults: `Opacity` (0.8), `Offset` (2,0), `Size`, `Background`, `ShowBorder`
- [x] `WithOpacity()`, `WithOffset()`, `WithSize()`, `WithBackground()`, `WithBorder()`
- [x] `PreviewRect(cursorX, cursorY, viewport)` -> Rect?

### `DragPreview` class ✅
- [x] `DragPreview(dragState)` — constructor
- [x] `DragPreview(dragState, config)` — constructor
- [x] `Render(buffer, viewport)` — renders preview overlay

## Tests (754 lines)
- [ ] All ported to test project (not yet)
