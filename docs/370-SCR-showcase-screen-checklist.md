# Showcase Screen Porting Checklist

Each screen must produce **bit-identical terminal output** to its Rust upstream at 80x24.
Compare with: `dotnet run --project tools/FrankenTui.ShowcaseCompare -- --screens <N>`

> **Status legend:** `[ ]` not started · `[~]` in progress · `[x]` complete (snapshot match)

## Summary

| Category | Total | Done |
|---|---|---|
| Tour | 1 | 0 |
| Core (widget gallery, layout, etc) | 10 | 0 |
| Visuals (charts, effects, mermaid) | 4 | 0 |
| Interaction (forms, mouse, drag) | 7 | 0 |
| Text (markdown, editor, shakespeare) | 6 | 0 |
| Systems (perf, async, determinism) | 17 | 0 |
| **Total** | **45** | **0** |

---

## Tour (1 screen)

- [ ] **1. Guided Tour** — `guided_tour.rs` (1 session)
  - Upstream: `crates/ftui-demo-showcase/src/screens/guided_tour.rs`
  - .NET target: `apps/FrankenTui.Demo.Showcase/ShowcaseSurface.cs` → `BuildGuidedTour()`

## Core Widgets & Layout (10 screens)

- [ ] **2. Dashboard** — `dashboard.rs` (7,057L) — 2 sessions
  - .NET target: `BuildDashboard()`
- [ ] **5. Widget Gallery** — `widget_gallery.rs` (2,348L) — 1 session
  - .NET target: `BuildWidgetGallery()`
- [ ] **6. Layout Lab** — `layout_lab.rs` (4,019L) — 2 sessions
  - .NET target: `BuildLayoutLab()`
- [x] **10. Advanced** — `advanced_features.rs` (1,057L)
  - .NET target: `BuildAdvancedFeatures()`
- [ ] **18. Visual Effects** — `visual_effects.rs` (5,784L) — 2 sessions
  - .NET target: `BuildVisualEffects()`
- [x] **19. Responsive Layout** — `responsive_demo.rs` (732L)
  - .NET target: `BuildResponsive()`
- [x] **23. Intrinsic Sizing** — `intrinsic_sizing.rs` (985L)
  - .NET target: `BuildIntrinsicSizing()`
- [x] **24. Layout Inspector** — `layout_inspector.rs` (1,029L)
  - .NET target: `BuildLayoutInspector()`
- [x] **38. Widget Builder** — `widget_builder.rs` (1,122L)
  - .NET target: `BuildWidgetBuilder()`
- [x] **44. Drag & Drop Lab** — `drag_drop.rs` (1,269L)
  - .NET target: `BuildDragDrop()`

## Visuals & Charts (4 screens)

- [ ] **8. Data Viz** — `3d_data.rs` (5,791L) — 2 sessions
  - .NET target: `BuildDataViz()`
- [x] **11. Table Theme Gallery** — `table_theme_gallery.rs` (1,260L)
  - .NET target: `BuildTableThemeGallery()`
- [ ] **16. Mermaid Showcase** — `mermaid_showcase.rs` (6,494L) — 2 sessions
  - .NET target: `BuildMermaid()`
- [ ] **17. Mermaid Mega Showcase** — `mermaid_mega_showcase.rs` (7,934L) — 3 sessions
  - .NET target: `BuildMermaidMega()`

## Interaction & Forms (7 screens)

- [x] **7. Forms & Input** — `forms_input.rs` (1,377L)
  - .NET target: `BuildFormsInput()`
- [x] **9. File Browser** — `file_browser.rs` (1,197L)
  - .NET target: `BuildFileBrowser()`
- [x] **13. Macro Recorder** — `macro_recorder.rs` (1,629L)
  - .NET target: `BuildMacroRecorder()`
- [ ] **26. Mouse Playground** — `mouse_playground.rs` (2,631L) — 1 session
  - .NET target: `BuildMousePlayground()`
- [x] **27. Form Validation** — `form_validation.rs` (861L)
  - .NET target: `BuildFormValidation()`
- [x] **39. Command Palette Evidence Lab** — `command_palette_lab.rs` (1,013L)
  - .NET target: `BuildCommandPaletteLab()`
- [x] **42. Kanban Board** — `kanban_board.rs` (1,355L)
  - .NET target: `BuildKanbanBoard()`

## Text & Editing (6 screens)

- [ ] **3. Shakespeare** — `shakespeare.rs` (1,882L)
  - .NET target: `BuildShakespeare()`
- [ ] **4. Code Explorer** — `code_explorer.rs` (2,478L) — 1 session
  - .NET target: `BuildCodeExplorer()`
- [ ] **15. Markdown** — `markdown_rich_text.rs` (1,578L)
  - .NET target: `BuildMarkdown()`
- [ ] **25. Advanced Text Editor** — `advanced_text_editor.rs` (2,133L) — 1 session
  - .NET target: `BuildAdvancedTextEditor()`
- [ ] **34. i18n Stress Lab** — `i18n_demo.rs` (1,556L)
  - .NET target: `BuildI18n()`
- [x] **43. Live Markdown Editor** — `markdown_live_editor.rs` (729L)
  - .NET target: `BuildMarkdownLiveEditor()`

## Systems & Infrastructure (17 screens)

- [ ] **12. Terminal Capabilities** — `terminal_capabilities.rs` (2,477L) — 1 session
  - .NET target: `BuildTerminalCapabilities()`
- [x] **14. Performance** — `performance.rs` (591L)
  - .NET target: `BuildPerformance()`
- [ ] **20. Log Search** — `log_search.rs` (2,531L) — 1 session
  - .NET target: `BuildLogSearch()`
- [x] **21. Notifications** — `notifications.rs` (636L)
  - .NET target: `BuildNotifications()`
- [x] **22. Action Timeline** — `action_timeline.rs` (1,707L)
  - .NET target: `BuildActionTimeline()`
- [ ] **28. Virtualized Search** — `virtualized_search.rs` (2,192L) — 1 session
  - .NET target: `BuildVirtualizedSearch()`
- [ ] **29. Async Tasks** — `async_tasks.rs` (4,597L) — 2 sessions
  - .NET target: `BuildAsyncTasks()`
- [ ] **30. Theme Studio** — `theme_studio.rs` (2,028L) — 1 session
  - .NET target: `BuildThemeStudio()`
- [ ] **31. Time-Travel Studio** — `snapshot_player.rs` (2,586L) — 1 session
  - .NET target: `BuildSnapshotPlayer()`
- [x] **32. Performance Challenge** — `performance_hud.rs` (1,604L)
  - .NET target: `BuildPerformanceChallenge()`
- [ ] **33. Explainability Cockpit** — `explainability_cockpit.rs` (1,400L)
  - .NET target: `BuildExplainability()`
- [x] **35. VOI Overlay** — `voi_overlay.rs` (785L)
  - .NET target: `BuildVoiOverlay()`
- [x] **36. Inline Mode** — `inline_mode_story.rs` (723L)
  - .NET target: `BuildInlineModeStory()`
- [x] **37. Accessibility** — `accessibility_panel.rs` (571L)
  - .NET target: `BuildAccessibility()`
- [x] **40. Determinism Lab** — `determinism_lab.rs` (1,353L)
  - .NET target: `BuildDeterminismLab()`
- [x] **41. Hyperlink Playground** — `hyperlink_playground.rs` (564L)
  - .NET target: `BuildHyperlinkPlayground()`
- [ ] **45. Quake E1M1 (Easter Egg)** — `quake.rs` (1,548L)
  - .NET target: `BuildQuake()`

---

## Porting Procedure (per screen)

1. **Study upstream**: Read the Rust screen file fully. Note the structs, state, widget tree.
2. **Build state model**: Create a .NET state class (or extend `ShowcaseDemoState`) with the same fields and defaults.
3. **Port the widget tree**: Translate the Rust `fn render()`/`fn view()` into a .NET `Build...()` method that constructs the identical widget hierarchy with identical text content and styling.
4. **Capture Rust snapshot**: Ensure upstream snapshot exists (`python tools/snapshot_rust.py N`)
5. **Compare**: `dotnet run --project tools/FrankenTui.ShowcaseCompare -- --screens N`
6. **Iterate**: Fix diffs row-by-row until `Exact: yes` and `Local/upstream: 1.000`
7. **Tick the checkbox** and move to the next screen.

## Tooling

```powershell
# Single screen comparison
dotnet run --project tools/FrankenTui.ShowcaseCompare -- --screens 7

# Full comparison (after batch of screens)
dotnet run --project tools/FrankenTui.ShowcaseCompare -- --screens 1-45

# Capture fresh Rust snapshot for one screen
python -c "import sys; sys.path.insert(0,'tools'); from snapshot_rust import write_snapshot; write_snapshot(7)"
```
