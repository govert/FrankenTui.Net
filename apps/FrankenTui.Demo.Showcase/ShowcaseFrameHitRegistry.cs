using FrankenTui.Core;

namespace FrankenTui.Demo.Showcase;

internal enum ShowcaseHitLayer
{
    Overlay,
    StatusToggle,
    Tab,
    Category,
    Link,
    Content,
    Pane,
    Unknown
}

internal readonly record struct ShowcaseHitTestResult(
    string LocalHitId,
    ShowcaseHitLayer Layer,
    uint? UpstreamHitId = null,
    int? TargetScreenNumber = null,
    ShowcaseScreenCategory? TargetCategory = null);

internal readonly record struct ShowcaseHitRegion(Rect Bounds, ShowcaseHitTestResult Result);

internal static class ShowcaseFrameHitRegistry
{
    // Mirrors upstream ftui-demo-showcase/src/chrome.rs hit-id bands at the
    // current port basis: tabs 1000+, categories 2000+, panes 4000+,
    // overlays 5000+, status toggles 6000+.
    public const uint TabHitBase = 1000;
    public const uint CategoryHitBase = 2000;
    public const uint PaneHitBase = 4000;
    public const uint OverlayHitBase = 5000;
    public const uint StatusHitBase = 6000;
    public const uint LinkHitBase = 8000;

    public const uint OverlayHelpClose = OverlayHitBase;
    public const uint OverlayHelpContent = OverlayHitBase + 1;
    public const uint OverlayTour = OverlayHitBase + 10;
    public const uint OverlayA11y = OverlayHitBase + 20;
    public const uint OverlayPerfHud = OverlayHitBase + 30;
    public const uint OverlayEvidence = OverlayHitBase + 40;
    public const uint OverlayDebug = OverlayHitBase + 50;

    public const uint StatusHelpToggle = StatusHitBase;
    public const uint StatusPaletteToggle = StatusHitBase + 1;
    public const uint StatusA11yToggle = StatusHitBase + 2;
    public const uint StatusPerfToggle = StatusHitBase + 3;
    public const uint StatusDebugToggle = StatusHitBase + 4;
    public const uint StatusMouseToggle = StatusHitBase + 5;

    public static IReadOnlyList<ShowcaseHitRegion> BuildRegions(ShowcaseDemoState state)
    {
        var regions = new List<ShowcaseHitRegion>();
        var viewport = state.Viewport;
        if (viewport.Width == 0 || viewport.Height == 0)
        {
            return regions;
        }

        RegisterOverlayRegions(state, regions);
        RegisterStatusRegions(state, regions);
        RegisterChromeRegions(state, regions);
        RegisterScreenPaneRegions(state, regions);
        return regions;
    }

    public static ShowcaseHitTestResult HitTest(ShowcaseDemoState state, ushort column, ushort row)
    {
        foreach (var region in BuildRegions(state))
        {
            if (region.Bounds.Contains(column, row))
            {
                return region.Result;
            }
        }

        return new("none", ShowcaseHitLayer.Unknown);
    }

    public static ShowcaseHitTestResult Resolve(
        MouseGesture gesture,
        ShowcaseDemoState before,
        ShowcaseDemoState after)
    {
        var hit = HitTest(before, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Unknown)
        {
            if (hit.TargetScreenNumber is null && before.CurrentScreenNumber != after.CurrentScreenNumber)
            {
                return hit with
                {
                    LocalHitId = hit.LocalHitId == "tab" ? $"tab:{after.CurrentScreenNumber}" :
                        hit.LocalHitId == "category" ? $"category:{after.CurrentScreen.Category}" : hit.LocalHitId,
                    TargetScreenNumber = after.CurrentScreenNumber,
                    TargetCategory = hit.Layer == ShowcaseHitLayer.Category ? after.CurrentScreen.Category : hit.TargetCategory
                };
            }

            return hit;
        }

        if (before.CurrentScreenNumber != after.CurrentScreenNumber)
        {
            return new(
                $"pane:{after.CurrentScreenNumber}",
                ShowcaseHitLayer.Pane,
                PaneRawId(after.CurrentScreenNumber),
                TargetScreenNumber: after.CurrentScreenNumber);
        }

        return hit;
    }

    public static uint TabRawId(int screenNumber) => TabHitBase + (uint)(ShowcaseCatalog.ClampScreenNumber(screenNumber) - 1);

    public static uint PaneRawId(int screenNumber) => PaneHitBase + (uint)(ShowcaseCatalog.ClampScreenNumber(screenNumber) - 1);

    public static uint LinkRawId(int linkIndex) => LinkHitBase + (uint)Math.Max(linkIndex, 0);

    public static uint CategoryRawId(ShowcaseScreenCategory category)
    {
        var index = CategoryIndex(category);
        return CategoryHitBase + (uint)Math.Min(index, ShowcaseCatalog.Categories.Count - 1);
    }

    public static int? ScreenFromRawId(uint rawId)
    {
        if (rawId >= TabHitBase && rawId < TabHitBase + ShowcaseCatalog.Screens.Count)
        {
            return (int)(rawId - TabHitBase) + 1;
        }

        if (rawId >= PaneHitBase && rawId < PaneHitBase + ShowcaseCatalog.Screens.Count)
        {
            return (int)(rawId - PaneHitBase) + 1;
        }

        if (CategoryFromRawId(rawId) is { } category)
        {
            return ShowcaseCatalog.FirstInCategory(category);
        }

        return null;
    }

    public static ShowcaseScreenCategory? CategoryFromRawId(uint rawId)
    {
        if (rawId < CategoryHitBase || rawId >= CategoryHitBase + ShowcaseCatalog.Categories.Count)
        {
            return null;
        }

        return ShowcaseCatalog.Categories[(int)(rawId - CategoryHitBase)];
    }

    private static void RegisterOverlayRegions(ShowcaseDemoState state, List<ShowcaseHitRegion> regions)
    {
        if (state.Session.CommandPalette.IsOpen)
        {
            regions.Add(WholeViewport(state.Viewport, new("palette", ShowcaseHitLayer.Overlay, OverlayHitBase)));
            return;
        }

        if (state.EvidenceLedgerVisible)
        {
            if (TryResolveEvidenceOverlayArea(state.Viewport, out var area))
            {
                regions.Add(new ShowcaseHitRegion(area, new("overlay:evidence", ShowcaseHitLayer.Overlay, OverlayEvidence)));
            }

            return;
        }

        if (state.PerfHudVisible)
        {
            if (TryResolvePerfHudOverlayArea(WholeViewportArea(state.Viewport), out var area))
            {
                regions.Add(new ShowcaseHitRegion(area, new("overlay:perf", ShowcaseHitLayer.Overlay, OverlayPerfHud)));
            }

            return;
        }

        if (state.DebugVisible)
        {
            if (TryResolveDebugOverlayArea(WholeViewportArea(state.Viewport), out var area))
            {
                regions.Add(new ShowcaseHitRegion(area, new("overlay:debug", ShowcaseHitLayer.Overlay, OverlayDebug)));
            }

            return;
        }

        if (state.HelpVisible)
        {
            if (TryResolveHelpOverlayArea(state.Viewport, out var overlayArea, out var innerArea))
            {
                regions.Add(new ShowcaseHitRegion(
                    new Rect(overlayArea.X, overlayArea.Y, overlayArea.Width, 1),
                    new("overlay:help_close", ShowcaseHitLayer.Overlay, OverlayHelpClose)));
                if (!innerArea.IsEmpty)
                {
                    regions.Add(new ShowcaseHitRegion(innerArea, new("overlay:help", ShowcaseHitLayer.Overlay, OverlayHelpContent)));
                }
            }

            return;
        }

        if (state.A11yPanelVisible)
        {
            if (TryResolveContentInnerArea(state.Viewport, out var contentInner) &&
                TryResolveA11yOverlayArea(contentInner, out var area))
            {
                regions.Add(new ShowcaseHitRegion(area, new("overlay:a11y", ShowcaseHitLayer.Overlay, OverlayA11y)));
            }

            return;
        }

        if (state.CurrentScreenNumber == 1 || state.TourActive)
        {
            if (TryResolveContentInnerArea(state.Viewport, out var contentInner) &&
                TryResolveTourOverlayArea(contentInner, state.TourActive, out var area))
            {
                regions.Add(new ShowcaseHitRegion(
                    area,
                    new("overlay:tour", ShowcaseHitLayer.Overlay, OverlayTour, TargetScreenNumber: state.CurrentScreenNumber)));
            }
        }
    }

    private static ShowcaseHitRegion WholeViewport(Size viewport, ShowcaseHitTestResult result) =>
        new(WholeViewportArea(viewport), result);

    private static Rect WholeViewportArea(Size viewport) => new(0, 0, viewport.Width, viewport.Height);

    private static bool TryResolveHelpOverlayArea(Size viewport, out Rect overlayArea, out Rect innerArea)
    {
        overlayArea = default;
        innerArea = default;
        if (viewport.Width < 2 || viewport.Height < 2)
        {
            return false;
        }

        var width = Math.Min((ushort)Math.Clamp(viewport.Width * 60 / 100, 36, 72), (ushort)(viewport.Width - 2));
        var height = Math.Min((ushort)Math.Clamp(viewport.Height * 70 / 100, 14, 28), (ushort)(viewport.Height - 2));
        if (width == 0 || height == 0)
        {
            return false;
        }

        var x = (ushort)((viewport.Width - width) / 2);
        var y = (ushort)((viewport.Height - height) / 2);
        overlayArea = new Rect(x, y, width, height);
        innerArea = InnerBlock(overlayArea);
        return true;
    }

    private static bool TryResolveA11yOverlayArea(Rect bounds, out Rect area)
    {
        area = default;
        if (bounds.Width < 2 || bounds.Height < 2)
        {
            return false;
        }

        var width = Math.Min((ushort)36, (ushort)(bounds.Width - 2));
        var height = Math.Min((ushort)8, (ushort)(bounds.Height - 2));
        if (width < 26 || height < 6)
        {
            return false;
        }

        var x = (ushort)(bounds.X + bounds.Width - width - 1);
        var y = (ushort)(bounds.Y + bounds.Height - height - 1);
        area = new Rect(x, y, width, height);
        return true;
    }

    private static bool TryResolvePerfHudOverlayArea(Rect bounds, out Rect area)
    {
        area = default;
        if (bounds.Width < 4 || bounds.Height < 4)
        {
            return false;
        }

        var width = Math.Min((ushort)48, (ushort)(bounds.Width - 4));
        var height = Math.Min((ushort)16, (ushort)(bounds.Height - 4));
        if (width < 20 || height < 6)
        {
            return false;
        }

        area = new Rect((ushort)(bounds.X + 1), (ushort)(bounds.Y + 1), width, height);
        return true;
    }

    private static bool TryResolveDebugOverlayArea(Rect bounds, out Rect area)
    {
        area = default;
        if (bounds.Width < 4 || bounds.Height < 4)
        {
            return false;
        }

        var width = Math.Min((ushort)40, (ushort)(bounds.Width - 4));
        var height = Math.Min((ushort)8, (ushort)(bounds.Height - 4));
        var x = (ushort)(bounds.X + bounds.Width - width - 1);
        var y = (ushort)(bounds.Y + 1);
        area = new Rect(x, y, width, height);
        return width > 0 && height > 0;
    }

    private static bool TryResolveEvidenceOverlayArea(Size viewport, out Rect area)
    {
        area = default;
        if (viewport.Width < 4 || viewport.Height < 4)
        {
            return false;
        }

        var width = Math.Min((ushort)74, (ushort)(viewport.Width - 4));
        var height = Math.Min((ushort)18, (ushort)(viewport.Height - 4));
        if (width < 40 || height < 10)
        {
            return false;
        }

        var x = (ushort)(viewport.Width - width - 1);
        var y = (ushort)(viewport.Height - height - 1);
        area = new Rect(x, y, width, height);
        return true;
    }

    private static bool TryResolveTourOverlayArea(Rect bounds, bool active, out Rect area)
    {
        area = default;
        if (bounds.Width == 0 || bounds.Height == 0)
        {
            return false;
        }

        if (!active)
        {
            var width = bounds.Width < 24 ? bounds.Width : Math.Clamp((int)bounds.Width, 24, 62);
            var height = bounds.Height < 7 ? bounds.Height : Math.Clamp((int)bounds.Height, 7, 11);
            var x = (ushort)(bounds.X + (bounds.Width - width) / 2);
            var y = (ushort)(bounds.Y + (bounds.Height - height) / 2);
            area = new Rect(x, y, (ushort)width, (ushort)height);
            return true;
        }

        var activeWidth = Math.Min((ushort)56, bounds.Width);
        var activeHeight = Math.Min((ushort)14, bounds.Height);
        if (activeWidth < 28 || activeHeight < 7)
        {
            return false;
        }

        var activeX = (ushort)(bounds.X + bounds.Width - activeWidth);
        area = new Rect(activeX, bounds.Y, activeWidth, activeHeight);
        return true;
    }

    private static Rect InnerBlock(Rect area)
    {
        if (area.Width <= 2 || area.Height <= 2)
        {
            return new Rect(area.X, area.Y, 0, 0);
        }

        return new Rect(
            (ushort)(area.X + 1),
            (ushort)(area.Y + 1),
            (ushort)(area.Width - 2),
            (ushort)(area.Height - 2));
    }

    private static void RegisterStatusRegions(ShowcaseDemoState state, List<ShowcaseHitRegion> regions)
    {
        var viewport = state.Viewport;
        if (viewport.Height == 0)
        {
            return;
        }

        var y = (ushort)(viewport.Height - 1);
        var position = $"[{state.CurrentScreenNumber}/{ShowcaseCatalog.Screens.Count}]";
        var theme = "  default";
        var help = state.HelpVisible ? " [H]" : " [h]";
        var palette = state.Session.CommandPalette.IsOpen ? " [Cmd]" : " [cmd]";
        var perf = state.PerfHudVisible ? " [P]" : " [p]";
        var debug = state.DebugVisible ? " [D]" : " [d]";
        var mouseMode = state.InlineMode ? "inline" : "alt";
        var mouse = state.MouseCaptureEnabled ? $"  Mouse: AUTO ({mouseMode}:ON)" : $"  Mouse: AUTO ({mouseMode}:OFF)";
        var x = 1 + state.CurrentScreen.Title.Length + 1 + position.Length + theme.Length;
        AddRegion(regions, x, y, help.Length, new("status:help", ShowcaseHitLayer.StatusToggle, StatusHelpToggle));
        x += help.Length;
        AddRegion(regions, x, y, palette.Length, new("status:palette", ShowcaseHitLayer.StatusToggle, StatusPaletteToggle));
        x += palette.Length;
        AddRegion(regions, x, y, perf.Length, new("status:perf", ShowcaseHitLayer.StatusToggle, StatusPerfToggle));
        x += perf.Length;
        AddRegion(regions, x, y, debug.Length, new("status:debug", ShowcaseHitLayer.StatusToggle, StatusDebugToggle));
        x += debug.Length;
        AddRegion(regions, x, y, mouse.Length, new("status:mouse", ShowcaseHitLayer.StatusToggle, StatusMouseToggle));
        x += mouse.Length;

        var a11y = StatusA11yLabel(state);
        if (!string.IsNullOrEmpty(a11y))
        {
            AddRegion(regions, x, y, a11y.Length, new("status:a11y", ShowcaseHitLayer.StatusToggle, StatusA11yToggle));
        }
    }

    private static string StatusA11yLabel(ShowcaseDemoState state)
    {
        var flags = new List<string>(3);
        if (state.A11yHighContrast)
        {
            flags.Add("HC");
        }

        if (state.A11yReducedMotion)
        {
            flags.Add("RM");
        }

        if (state.A11yLargeText)
        {
            flags.Add("LT");
        }

        return flags.Count == 0 ? string.Empty : $" A11y:{string.Join(" ", flags)}";
    }

    private static void RegisterChromeRegions(ShowcaseDemoState state, List<ShowcaseHitRegion> regions)
    {
        if (state.Viewport.Height < 2)
        {
            return;
        }

        var x = 0;
        for (var index = 0; index < ShowcaseCatalog.Screens.Count; index++)
        {
            var screen = ShowcaseCatalog.Screens[index];
            var keyLabel = index < 9 ? $"{index + 1}" : index == 9 ? "0" : "-";
            var label = $"{keyLabel}: {screen.ShortLabel}";
            var width = label.Length + 2;
            AddRegion(
                regions,
                x,
                0,
                width,
                new(
                    $"tab:{screen.Number}",
                    ShowcaseHitLayer.Tab,
                    TabRawId(screen.Number),
                    TargetScreenNumber: screen.Number));
            x += width + 1;
            if (x >= state.Viewport.Width)
            {
                break;
            }
        }
    }

    private static void RegisterScreenPaneRegions(ShowcaseDemoState state, List<ShowcaseHitRegion> regions)
    {
        if (state.CurrentScreenNumber == 39 && ShowcaseDemoState.TryResolvePaletteLabPaletteArea(state.Viewport, out var paletteLabArea))
        {
            regions.Add(new ShowcaseHitRegion(
                paletteLabArea,
                new("palette_lab", ShowcaseHitLayer.Pane, PaneRawId(39), TargetScreenNumber: 39)));
        }

        if (state.CurrentScreenNumber == 43 && TryResolveContentInnerArea(state.Viewport, out var liveMarkdownInner) && liveMarkdownInner.Width >= 30 && liveMarkdownInner.Height >= 10)
        {
            AddRegion(
                regions,
                liveMarkdownInner.X,
                liveMarkdownInner.Y + 3,
                liveMarkdownInner.Width,
                new("live_markdown:search", ShowcaseHitLayer.Content, 43_000));
            var split = Math.Max(1, liveMarkdownInner.Width / 2);
            var paneY = liveMarkdownInner.Y + 5;
            var paneHeight = Math.Max(1, liveMarkdownInner.Height - 5);
            regions.Add(new ShowcaseHitRegion(
                new Rect(
                    liveMarkdownInner.X,
                    (ushort)paneY,
                    (ushort)Math.Max(1, split),
                    (ushort)Math.Max(1, paneHeight)),
                new("live_markdown:editor", ShowcaseHitLayer.Content, 43_001)));
            regions.Add(new ShowcaseHitRegion(
                new Rect(
                    (ushort)(liveMarkdownInner.X + split),
                    (ushort)paneY,
                    (ushort)Math.Max(1, liveMarkdownInner.Width - split),
                    (ushort)Math.Max(1, paneHeight)),
                new("live_markdown:preview", ShowcaseHitLayer.Content, 43_002)));
        }

        if (state.CurrentScreenNumber == 44 && TryResolveContentInnerArea(state.Viewport, out var dragDropInner) && dragDropInner.Width >= 30 && dragDropInner.Height >= 8)
        {
            var tabY = dragDropInner.Y;
            var tabWidth = Math.Max(1, dragDropInner.Width / 3);
            for (var index = 0; index < 3; index++)
            {
                AddRegion(
                    regions,
                    dragDropInner.X + index * tabWidth,
                    tabY,
                    tabWidth,
                    new($"drag_drop:tab:{index}", ShowcaseHitLayer.Content, (uint)(44_000 + index)));
            }

            var listY = dragDropInner.Y + 3;
            var listHeight = Math.Max(1, Math.Min(8, dragDropInner.Height - 4));
            var halfWidth = Math.Max(1, dragDropInner.Width / 2);
            for (var row = 0; row < listHeight; row++)
            {
                AddRegion(
                    regions,
                    dragDropInner.X,
                    listY + row,
                    halfWidth,
                    new($"drag_drop:item:0:{row}", ShowcaseHitLayer.Content, (uint)row));
                AddRegion(
                    regions,
                    dragDropInner.X + halfWidth,
                    listY + row,
                    Math.Max(1, dragDropInner.Width - halfWidth),
                    new($"drag_drop:item:1:{row}", ShowcaseHitLayer.Content, (uint)(8 + row)));
            }
        }

        if (state.CurrentScreenNumber == 42 && TryResolveContentInnerArea(state.Viewport, out var kanbanInner) && kanbanInner.Width >= 30 && kanbanInner.Height >= 8)
        {
            var board = state.KanbanBoard ?? ShowcaseKanbanState.CreateDefault();
            var colWidth = Math.Max(1, kanbanInner.Width / 3);
            for (var col = 0; col < 3; col++)
            {
                var cards = board.Column(col);
                for (var row = 0; row < cards.Count; row++)
                {
                    var card = cards[row];
                    regions.Add(new ShowcaseHitRegion(
                        new Rect(
                            (ushort)(kanbanInner.X + col * colWidth),
                            (ushort)(kanbanInner.Y + 2 + row * 3),
                            (ushort)Math.Max(1, colWidth),
                            3),
                        new($"kanban:{col}:{row}:{card.Id}", ShowcaseHitLayer.Content, (uint)card.Id)));
                }
            }
        }

        if (state.CurrentScreenNumber == 9 && TryResolveContentInnerArea(state.Viewport, out var fileBrowserInner) && fileBrowserInner.Width >= 30 && fileBrowserInner.Height >= 8)
        {
            var leftWidth = Math.Max(1, fileBrowserInner.Width / 2);
            var treeRows = Math.Max(1, Math.Min(6, fileBrowserInner.Height - 2));
            for (var row = 0; row < treeRows; row++)
            {
                AddRegion(
                    regions,
                    fileBrowserInner.X,
                    fileBrowserInner.Y + 2 + row,
                    leftWidth,
                    new($"file_browser:tree:{row}", ShowcaseHitLayer.Content, (uint)(9_000 + row)));
            }

            regions.Add(new ShowcaseHitRegion(
                new Rect(
                    (ushort)(fileBrowserInner.X + leftWidth),
                    (ushort)(fileBrowserInner.Y + 1),
                    (ushort)Math.Max(1, fileBrowserInner.Width - leftWidth),
                    (ushort)Math.Max(1, fileBrowserInner.Height - 1)),
                new("file_browser:preview", ShowcaseHitLayer.Content, 9_100)));
        }

        if (state.CurrentScreenNumber == 21 && TryResolveContentInnerArea(state.Viewport, out var notificationInner) && notificationInner.Width >= 30 && notificationInner.Height >= 8)
        {
            var triggerWidth = Math.Max(1, notificationInner.Width * 40 / 100);
            var triggerRows = new[] { "success", "error", "warning", "info", "urgent", "dismiss_all" };
            for (var index = 0; index < triggerRows.Length; index++)
            {
                AddRegion(
                    regions,
                    notificationInner.X,
                    notificationInner.Y + 3 + index,
                    triggerWidth,
                    new(
                        $"notifications:trigger:{triggerRows[index]}",
                        ShowcaseHitLayer.Content,
                        (uint)(21_000 + index)));
            }

            var stackX = notificationInner.X + triggerWidth;
            var stackWidth = Math.Max(1, notificationInner.Width - triggerWidth);
            for (var row = 0; row < 5; row++)
            {
                AddRegion(
                    regions,
                    stackX,
                    notificationInner.Y + 2 + row,
                    stackWidth,
                    new($"notifications:toast:{row}", ShowcaseHitLayer.Content, (uint)(21_100 + row)));
            }

            regions.Add(new ShowcaseHitRegion(
                new Rect(
                    (ushort)stackX,
                    (ushort)(notificationInner.Y + 11),
                    (ushort)stackWidth,
                    (ushort)Math.Max(1, notificationInner.Height - 11)),
                new("notifications:lifecycle", ShowcaseHitLayer.Content, 21_200)));
        }

        if (state.CurrentScreenNumber == 13 && TryResolveContentInnerArea(state.Viewport, out var macroInner) && macroInner.Width >= 40 && macroInner.Height >= 12)
        {
            regions.Add(new ShowcaseHitRegion(
                new Rect(macroInner.X, macroInner.Y, macroInner.Width, (ushort)Math.Min(8, (int)macroInner.Height)),
                new("macro_recorder:controls", ShowcaseHitLayer.Content, 13_000)));

            var bodyY = macroInner.Y + 8;
            var bodyHeight = Math.Max(1, macroInner.Height - 8);
            var timelineWidth = Math.Max(1, macroInner.Width * 60 / 100);
            for (var row = 0; row < Math.Min(10, bodyHeight); row++)
            {
                AddRegion(
                    regions,
                    macroInner.X,
                    bodyY + row,
                    timelineWidth,
                    new($"macro_recorder:timeline:{row}", ShowcaseHitLayer.Content, (uint)(13_100 + row)));
            }

            var rightX = macroInner.X + timelineWidth;
            var rightWidth = Math.Max(1, macroInner.Width - timelineWidth);
            var detailHeight = Math.Max(1, bodyHeight * 65 / 100);
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)rightX, (ushort)bodyY, (ushort)rightWidth, (ushort)detailHeight),
                new("macro_recorder:event_detail", ShowcaseHitLayer.Content, 13_200)));
            regions.Add(new ShowcaseHitRegion(
                new Rect(
                    (ushort)rightX,
                    (ushort)(bodyY + detailHeight),
                    (ushort)rightWidth,
                    (ushort)Math.Max(1, bodyHeight - detailHeight)),
                new("macro_recorder:scenario_runner", ShowcaseHitLayer.Content, 13_300)));
        }

        if (state.CurrentScreenNumber == 20 && TryResolveContentInnerArea(state.Viewport, out var logSearchInner) && logSearchInner.Width >= 40 && logSearchInner.Height >= 12)
        {
            var searchWidth = Math.Max(1, logSearchInner.Width * 55 / 100);
            var sideX = logSearchInner.X + searchWidth;
            var sideWidth = Math.Max(1, logSearchInner.Width - searchWidth);
            for (var row = 0; row < Math.Min(10, (int)logSearchInner.Height); row++)
            {
                AddRegion(
                    regions,
                    logSearchInner.X,
                    logSearchInner.Y + row,
                    searchWidth,
                    new($"log_search:result:{row}", ShowcaseHitLayer.Content, (uint)(20_000 + row)));
            }

            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)sideX, logSearchInner.Y, (ushort)sideWidth, (ushort)Math.Min(8, (int)logSearchInner.Height)),
                new("log_search:live_stream", ShowcaseHitLayer.Content, 20_100)));
            AddRegion(
                regions,
                sideX,
                logSearchInner.Y + 8,
                sideWidth,
                new("log_search:controls", ShowcaseHitLayer.Content, 20_200));
            var diagnosticsY = logSearchInner.Y + 16;
            for (var row = 0; row < Math.Max(1, Math.Min(10, logSearchInner.Height - 16)); row++)
            {
                AddRegion(
                    regions,
                    sideX,
                    diagnosticsY + row,
                    sideWidth,
                    new($"log_search:diagnostic:{row}", ShowcaseHitLayer.Content, (uint)(20_300 + row)));
            }
        }

        if (state.CurrentScreenNumber == 25 && TryResolveContentInnerArea(state.Viewport, out var editorInner) && editorInner.Width >= 40 && editorInner.Height >= 12)
        {
            var editorWidth = Math.Max(1, editorInner.Width / 2);
            var sideX = editorInner.X + editorWidth;
            var sideWidth = Math.Max(1, editorInner.Width - editorWidth);
            var editorRows = Math.Max(1, Math.Min(14, (int)editorInner.Height));
            for (var row = 0; row < editorRows; row++)
            {
                AddRegion(
                    regions,
                    editorInner.X,
                    editorInner.Y + row,
                    editorWidth,
                    new($"advanced_text_editor:line:{row}", ShowcaseHitLayer.Content, (uint)(25_000 + row)));
            }

            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)sideX, editorInner.Y, (ushort)sideWidth, (ushort)Math.Min(10, (int)editorInner.Height)),
                new("advanced_text_editor:search", ShowcaseHitLayer.Content, 25_100)));
            var historyY = editorInner.Y + 10;
            var historyHeight = Math.Max(1, Math.Min(11, editorInner.Height - 10));
            for (var row = 0; row < historyHeight; row++)
            {
                AddRegion(
                    regions,
                    sideX,
                    historyY + row,
                    sideWidth,
                    new($"advanced_text_editor:history:{row}", ShowcaseHitLayer.Content, (uint)(25_200 + row)));
            }

            var diagnosticsY = editorInner.Y + 21;
            for (var row = 0; row < Math.Max(1, Math.Min(10, editorInner.Height - 21)); row++)
            {
                AddRegion(
                    regions,
                    sideX,
                    diagnosticsY + row,
                    sideWidth,
                    new($"advanced_text_editor:diagnostic:{row}", ShowcaseHitLayer.Content, (uint)(25_300 + row)));
            }
        }

        if (state.CurrentScreenNumber == 28 && TryResolveContentInnerArea(state.Viewport, out var virtualSearchInner) && virtualSearchInner.Width >= 40 && virtualSearchInner.Height >= 12)
        {
            regions.Add(new ShowcaseHitRegion(
                new Rect(virtualSearchInner.X, virtualSearchInner.Y, virtualSearchInner.Width, (ushort)Math.Min(3, (int)virtualSearchInner.Height)),
                new("virtualized_search:search_bar", ShowcaseHitLayer.Content, 28_000)));

            var bodyY = virtualSearchInner.Y + 3;
            var bodyHeight = Math.Max(1, virtualSearchInner.Height - 3);
            var resultsWidth = Math.Max(1, virtualSearchInner.Width * 70 / 100);
            for (var row = 0; row < Math.Min(12, bodyHeight); row++)
            {
                AddRegion(
                    regions,
                    virtualSearchInner.X,
                    bodyY + row,
                    resultsWidth,
                    new($"virtualized_search:result:{row}", ShowcaseHitLayer.Content, (uint)(28_100 + row)));
            }

            var sideX = virtualSearchInner.X + resultsWidth;
            var sideWidth = Math.Max(1, virtualSearchInner.Width - resultsWidth);
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)sideX, (ushort)bodyY, (ushort)sideWidth, (ushort)Math.Min(12, bodyHeight)),
                new("virtualized_search:stats", ShowcaseHitLayer.Content, 28_300)));

            var diagnosticsY = bodyY + 12;
            for (var row = 0; row < Math.Max(1, Math.Min(10, virtualSearchInner.Height - 15)); row++)
            {
                AddRegion(
                    regions,
                    sideX,
                    diagnosticsY + row,
                    sideWidth,
                    new($"virtualized_search:diagnostic:{row}", ShowcaseHitLayer.Content, (uint)(28_400 + row)));
            }
        }

        if (state.CurrentScreenNumber == 29 && TryResolveContentInnerArea(state.Viewport, out var asyncInner) && asyncInner.Width >= 40 && asyncInner.Height >= 12)
        {
            regions.Add(new ShowcaseHitRegion(
                new Rect(asyncInner.X, asyncInner.Y, asyncInner.Width, (ushort)Math.Min(4, (int)asyncInner.Height)),
                new("async_tasks:scheduler", ShowcaseHitLayer.Content, 29_000)));

            var bodyY = asyncInner.Y + 4;
            var bodyHeight = Math.Max(1, asyncInner.Height - 5);
            var queueWidth = Math.Max(1, asyncInner.Width * 60 / 100);
            for (var row = 0; row < Math.Min(8, bodyHeight); row++)
            {
                AddRegion(
                    regions,
                    asyncInner.X,
                    bodyY + row,
                    queueWidth,
                    new($"async_tasks:task:{row}", ShowcaseHitLayer.Content, (uint)(29_100 + row)));
            }

            var rightX = asyncInner.X + queueWidth;
            var rightWidth = Math.Max(1, asyncInner.Width - queueWidth);
            var detailsHeight = Math.Max(1, bodyHeight * 36 / 100);
            var activityHeight = Math.Max(1, bodyHeight * 26 / 100);
            var evidenceHeight = Math.Max(1, bodyHeight * 19 / 100);
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)rightX, (ushort)bodyY, (ushort)rightWidth, (ushort)detailsHeight),
                new("async_tasks:details", ShowcaseHitLayer.Content, 29_200)));
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)rightX, (ushort)(bodyY + detailsHeight), (ushort)rightWidth, (ushort)activityHeight),
                new("async_tasks:activity", ShowcaseHitLayer.Content, 29_210)));
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)rightX, (ushort)(bodyY + detailsHeight + activityHeight), (ushort)rightWidth, (ushort)evidenceHeight),
                new("async_tasks:evidence", ShowcaseHitLayer.Content, 29_220)));
            regions.Add(new ShowcaseHitRegion(
                new Rect(
                    (ushort)rightX,
                    (ushort)(bodyY + detailsHeight + activityHeight + evidenceHeight),
                    (ushort)rightWidth,
                    (ushort)Math.Max(1, bodyHeight - detailsHeight - activityHeight - evidenceHeight)),
                new("async_tasks:hazard", ShowcaseHitLayer.Content, 29_230)));
            AddRegion(
                regions,
                asyncInner.X,
                Math.Max(asyncInner.Y, asyncInner.Bottom - 1),
                asyncInner.Width,
                new("async_tasks:footer", ShowcaseHitLayer.Content, 29_300));
        }

        if (state.CurrentScreenNumber == 30 && TryResolveContentInnerArea(state.Viewport, out var themeInner) && themeInner.Width >= 40 && themeInner.Height >= 12)
        {
            var bodyHeight = Math.Max(1, themeInner.Height - 1);
            var presetWidth = Math.Max(1, themeInner.Width * 25 / 100);
            for (var row = 0; row < Math.Min(5, bodyHeight); row++)
            {
                AddRegion(
                    regions,
                    themeInner.X,
                    themeInner.Y + row,
                    presetWidth,
                    new($"theme_studio:preset:{row}", ShowcaseHitLayer.Content, (uint)(30_000 + row)));
            }

            var inspectorX = themeInner.X + presetWidth;
            var inspectorWidth = Math.Max(1, themeInner.Width - presetWidth);
            var tokenHeight = Math.Max(1, bodyHeight * 52 / 100);
            for (var row = 0; row < Math.Min(12, tokenHeight); row++)
            {
                AddRegion(
                    regions,
                    inspectorX,
                    themeInner.Y + row,
                    inspectorWidth,
                    new($"theme_studio:token:{row}", ShowcaseHitLayer.Content, (uint)(30_100 + row)));
            }

            var exportY = themeInner.Y + tokenHeight;
            var exportHeight = Math.Max(1, bodyHeight * 24 / 100);
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)inspectorX, (ushort)exportY, (ushort)inspectorWidth, (ushort)exportHeight),
                new("theme_studio:export", ShowcaseHitLayer.Content, 30_300)));
            regions.Add(new ShowcaseHitRegion(
                new Rect(
                    (ushort)inspectorX,
                    (ushort)(exportY + exportHeight),
                    (ushort)inspectorWidth,
                    (ushort)Math.Max(1, bodyHeight - tokenHeight - exportHeight)),
                new("theme_studio:diagnostics", ShowcaseHitLayer.Content, 30_310)));
            AddRegion(
                regions,
                themeInner.X,
                Math.Max(themeInner.Y, themeInner.Bottom - 1),
                themeInner.Width,
                new("theme_studio:footer", ShowcaseHitLayer.Content, 30_400));
        }

        if (state.CurrentScreenNumber == 31 && TryResolveContentInnerArea(state.Viewport, out var snapshotInner) && snapshotInner.Width >= 40 && snapshotInner.Height >= 12)
        {
            var leftWidth = Math.Max(1, snapshotInner.Width * 60 / 100);
            var rightX = snapshotInner.X + leftWidth;
            var rightWidth = Math.Max(1, snapshotInner.Width - leftWidth);
            regions.Add(new ShowcaseHitRegion(
                new Rect(snapshotInner.X, snapshotInner.Y, (ushort)leftWidth, (ushort)Math.Min(4, (int)snapshotInner.Height)),
                new("snapshot_player:timeline", ShowcaseHitLayer.Content, 31_000)));

            var bodyY = snapshotInner.Y + 4;
            var bodyHeight = Math.Max(1, snapshotInner.Height - 4);
            var previewWidth = Math.Max(1, leftWidth / 2);
            regions.Add(new ShowcaseHitRegion(
                new Rect(snapshotInner.X, (ushort)bodyY, (ushort)previewWidth, (ushort)bodyHeight),
                new("snapshot_player:preview", ShowcaseHitLayer.Content, 31_100)));
            regions.Add(new ShowcaseHitRegion(
                new Rect(
                    (ushort)(snapshotInner.X + previewWidth),
                    (ushort)bodyY,
                    (ushort)Math.Max(1, leftWidth - previewWidth),
                    (ushort)bodyHeight),
                new("snapshot_player:compare", ShowcaseHitLayer.Content, 31_110)));

            var infoHeight = Math.Max(1, snapshotInner.Height * 45 / 100);
            var controlsHeight = Math.Max(1, snapshotInner.Height * 28 / 100);
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)rightX, snapshotInner.Y, (ushort)rightWidth, (ushort)infoHeight),
                new("snapshot_player:frame_info", ShowcaseHitLayer.Content, 31_200)));
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)rightX, (ushort)(snapshotInner.Y + infoHeight), (ushort)rightWidth, (ushort)controlsHeight),
                new("snapshot_player:controls", ShowcaseHitLayer.Content, 31_210)));
            regions.Add(new ShowcaseHitRegion(
                new Rect(
                    (ushort)rightX,
                    (ushort)(snapshotInner.Y + infoHeight + controlsHeight),
                    (ushort)rightWidth,
                    (ushort)Math.Max(1, snapshotInner.Height - infoHeight - controlsHeight)),
                new("snapshot_player:diagnostics", ShowcaseHitLayer.Content, 31_220)));
        }

        if (state.CurrentScreenNumber == 32 && TryResolveContentInnerArea(state.Viewport, out var perfInner) && perfInner.Width >= 60 && perfInner.Height >= 12)
        {
            AddRegion(
                regions,
                perfInner.X,
                perfInner.Y,
                perfInner.Width,
                new("performance_challenge:header", ShowcaseHitLayer.Content, 32_000));

            var bodyY = perfInner.Y + 1;
            var bodyHeight = Math.Max(1, perfInner.Height - 2);
            var metricsWidth = Math.Min(34, Math.Max(1, perfInner.Width / 3));
            var budgetWidth = Math.Min(42, Math.Max(1, perfInner.Width / 3));
            var middleX = perfInner.X + metricsWidth;
            var budgetX = perfInner.Right - budgetWidth;
            var middleWidth = Math.Max(1, budgetX - middleX);
            regions.Add(new ShowcaseHitRegion(
                new Rect(perfInner.X, (ushort)bodyY, (ushort)metricsWidth, (ushort)bodyHeight),
                new("performance_challenge:metrics", ShowcaseHitLayer.Content, 32_100)));

            var sparklineHeight = Math.Max(1, bodyHeight * 52 / 100);
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)middleX, (ushort)bodyY, (ushort)middleWidth, (ushort)sparklineHeight),
                new("performance_challenge:sparkline", ShowcaseHitLayer.Content, 32_200)));
            regions.Add(new ShowcaseHitRegion(
                new Rect(
                    (ushort)middleX,
                    (ushort)(bodyY + sparklineHeight),
                    (ushort)middleWidth,
                    (ushort)Math.Max(1, bodyHeight - sparklineHeight)),
                new("performance_challenge:evidence", ShowcaseHitLayer.Content, 32_210)));

            var budgetPanelHeight = Math.Max(1, bodyHeight * 42 / 100);
            var stressHeight = Math.Max(1, bodyHeight * 28 / 100);
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)budgetX, (ushort)bodyY, (ushort)budgetWidth, (ushort)budgetPanelHeight),
                new("performance_challenge:budget", ShowcaseHitLayer.Content, 32_300)));
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)budgetX, (ushort)(bodyY + budgetPanelHeight), (ushort)budgetWidth, (ushort)stressHeight),
                new("performance_challenge:stress", ShowcaseHitLayer.Content, 32_310)));
            for (var row = 0; row < Math.Max(1, Math.Min(4, bodyHeight - budgetPanelHeight - stressHeight)); row++)
            {
                AddRegion(
                    regions,
                    budgetX,
                    bodyY + budgetPanelHeight + stressHeight + row,
                    budgetWidth,
                    new($"performance_challenge:tier:{row}", ShowcaseHitLayer.Content, (uint)(32_400 + row)));
            }

            AddRegion(
                regions,
                perfInner.X,
                Math.Max(perfInner.Y, perfInner.Bottom - 1),
                perfInner.Width,
                new("performance_challenge:footer", ShowcaseHitLayer.Content, 32_500));
        }

        if (state.CurrentScreenNumber == 33 && TryResolveContentInnerArea(state.Viewport, out var explainInner) && explainInner.Width >= 45 && explainInner.Height >= 16)
        {
            regions.Add(new ShowcaseHitRegion(
                new Rect(explainInner.X, explainInner.Y, explainInner.Width, (ushort)Math.Min(2, (int)explainInner.Height)),
                new("explainability:header", ShowcaseHitLayer.Content, 33_000)));

            var bodyY = explainInner.Y + 2;
            var bodyHeight = Math.Max(1, explainInner.Height - 16);
            var diffWidth = Math.Max(1, explainInner.Width * 34 / 100);
            var resizeWidth = Math.Max(1, explainInner.Width * 33 / 100);
            var budgetWidth = Math.Max(1, explainInner.Width - diffWidth - resizeWidth);
            var resizeX = explainInner.X + diffWidth;
            var budgetX = resizeX + resizeWidth;
            regions.Add(new ShowcaseHitRegion(
                new Rect(explainInner.X, (ushort)bodyY, (ushort)diffWidth, (ushort)bodyHeight),
                new("explainability:diff_strategy", ShowcaseHitLayer.Content, 33_100)));
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)resizeX, (ushort)bodyY, (ushort)resizeWidth, (ushort)bodyHeight),
                new("explainability:resize_regime", ShowcaseHitLayer.Content, 33_110)));
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)budgetX, (ushort)bodyY, (ushort)budgetWidth, (ushort)bodyHeight),
                new("explainability:budget_decisions", ShowcaseHitLayer.Content, 33_120)));

            regions.Add(new ShowcaseHitRegion(
                new Rect(explainInner.X, (ushort)(bodyY + bodyHeight), explainInner.Width, 7),
                new("explainability:timeline", ShowcaseHitLayer.Content, 33_200)));
            regions.Add(new ShowcaseHitRegion(
                new Rect(
                    explainInner.X,
                    (ushort)(bodyY + bodyHeight + 7),
                    explainInner.Width,
                    (ushort)Math.Max(1, explainInner.Height - bodyHeight - 9)),
                new("explainability:source_controls", ShowcaseHitLayer.Content, 33_300)));
        }

        if (state.CurrentScreenNumber == 34 && TryResolveContentInnerArea(state.Viewport, out var i18nInner) && i18nInner.Width >= 40 && i18nInner.Height >= 12)
        {
            regions.Add(new ShowcaseHitRegion(
                new Rect(i18nInner.X, i18nInner.Y, i18nInner.Width, (ushort)Math.Min(3, (int)i18nInner.Height)),
                new("i18n:locale_bar", ShowcaseHitLayer.Content, 34_000)));

            var bodyY = i18nInner.Y + 3;
            var bodyHeight = Math.Max(1, i18nInner.Height - 4);
            var leftWidth = Math.Max(1, i18nInner.Width / 2);
            var rightX = i18nInner.X + leftWidth;
            var rightWidth = Math.Max(1, i18nInner.Width - leftWidth);
            var topHeight = Math.Max(1, bodyHeight / 2);
            var bottomHeight = Math.Max(1, bodyHeight - topHeight);
            regions.Add(new ShowcaseHitRegion(
                new Rect(i18nInner.X, (ushort)bodyY, (ushort)leftWidth, (ushort)topHeight),
                new("i18n:string_lookup", ShowcaseHitLayer.Content, 34_100)));
            regions.Add(new ShowcaseHitRegion(
                new Rect(i18nInner.X, (ushort)(bodyY + topHeight), (ushort)leftWidth, (ushort)bottomHeight),
                new("i18n:plural_rules", ShowcaseHitLayer.Content, 34_110)));
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)rightX, (ushort)bodyY, (ushort)rightWidth, (ushort)topHeight),
                new("i18n:rtl_layout", ShowcaseHitLayer.Content, 34_120)));
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)rightX, (ushort)(bodyY + topHeight), (ushort)rightWidth, (ushort)bottomHeight),
                new("i18n:stress_lab", ShowcaseHitLayer.Content, 34_130)));
            AddRegion(
                regions,
                i18nInner.X,
                Math.Max(i18nInner.Y, i18nInner.Bottom - 1),
                i18nInner.Width,
                new("i18n:footer", ShowcaseHitLayer.Content, 34_200));
        }

        if (state.CurrentScreenNumber == 35 && TryResolveContentInnerArea(state.Viewport, out var voiInner) && voiInner.Width >= 45 && voiInner.Height >= 12)
        {
            AddRegion(
                regions,
                voiInner.X,
                voiInner.Y,
                voiInner.Width,
                new("voi_overlay:header", ShowcaseHitLayer.Content, 35_000));

            var bodyY = voiInner.Y + 1;
            var bodyHeight = Math.Max(1, voiInner.Height - 2);
            var leftWidth = Math.Max(1, voiInner.Width * 34 / 100);
            var middleWidth = Math.Max(1, voiInner.Width * 33 / 100);
            var rightWidth = Math.Max(1, voiInner.Width - leftWidth - middleWidth);
            var middleX = voiInner.X + leftWidth;
            var rightX = middleX + middleWidth;
            var halfHeight = Math.Max(1, bodyHeight / 2);
            var lowerHeight = Math.Max(1, bodyHeight - halfHeight);
            regions.Add(new ShowcaseHitRegion(
                new Rect(voiInner.X, (ushort)bodyY, (ushort)leftWidth, (ushort)halfHeight),
                new("voi_overlay:decision", ShowcaseHitLayer.Content, 35_100)));
            regions.Add(new ShowcaseHitRegion(
                new Rect(voiInner.X, (ushort)(bodyY + halfHeight), (ushort)leftWidth, (ushort)lowerHeight),
                new("voi_overlay:posterior", ShowcaseHitLayer.Content, 35_110)));
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)middleX, (ushort)bodyY, (ushort)middleWidth, (ushort)halfHeight),
                new("voi_overlay:observation", ShowcaseHitLayer.Content, 35_120)));
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)middleX, (ushort)(bodyY + halfHeight), (ushort)middleWidth, (ushort)lowerHeight),
                new("voi_overlay:ledger", ShowcaseHitLayer.Content, 35_130)));
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)rightX, (ushort)bodyY, (ushort)rightWidth, (ushort)bodyHeight),
                new("voi_overlay:controls", ShowcaseHitLayer.Content, 35_200)));
            AddRegion(
                regions,
                voiInner.X,
                Math.Max(voiInner.Y, voiInner.Bottom - 1),
                voiInner.Width,
                new("voi_overlay:footer", ShowcaseHitLayer.Content, 35_300));
        }

        if (state.CurrentScreenNumber == 36 && TryResolveContentInnerArea(state.Viewport, out var inlineInner) && inlineInner.Width >= 45 && inlineInner.Height >= 12)
        {
            regions.Add(new ShowcaseHitRegion(
                new Rect(inlineInner.X, inlineInner.Y, inlineInner.Width, (ushort)Math.Min(2, (int)inlineInner.Height)),
                new("inline_mode:header", ShowcaseHitLayer.Content, 36_000)));

            var bodyY = inlineInner.Y + 2;
            var bodyHeight = Math.Max(1, inlineInner.Height - 3);
            var inlineWidth = Math.Max(1, inlineInner.Width * 36 / 100);
            var altWidth = Math.Max(1, inlineInner.Width * 32 / 100);
            var sideWidth = Math.Max(1, inlineInner.Width - inlineWidth - altWidth);
            var altX = inlineInner.X + inlineWidth;
            var sideX = altX + altWidth;
            regions.Add(new ShowcaseHitRegion(
                new Rect(inlineInner.X, (ushort)bodyY, (ushort)inlineWidth, (ushort)bodyHeight),
                new("inline_mode:inline_story", ShowcaseHitLayer.Content, 36_100)));
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)altX, (ushort)bodyY, (ushort)altWidth, (ushort)bodyHeight),
                new("inline_mode:alt_story", ShowcaseHitLayer.Content, 36_110)));

            var controlsHeight = Math.Max(1, bodyHeight / 2);
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)sideX, (ushort)bodyY, (ushort)sideWidth, (ushort)controlsHeight),
                new("inline_mode:controls", ShowcaseHitLayer.Content, 36_200)));
            regions.Add(new ShowcaseHitRegion(
                new Rect(
                    (ushort)sideX,
                    (ushort)(bodyY + controlsHeight),
                    (ushort)sideWidth,
                    (ushort)Math.Max(1, bodyHeight - controlsHeight)),
                new("inline_mode:state_limits", ShowcaseHitLayer.Content, 36_210)));
            AddRegion(
                regions,
                inlineInner.X,
                Math.Max(inlineInner.Y, inlineInner.Bottom - 1),
                inlineInner.Width,
                new("inline_mode:footer", ShowcaseHitLayer.Content, 36_300));
        }

        if (state.CurrentScreenNumber == 37 && TryResolveContentInnerArea(state.Viewport, out var a11yInner) && a11yInner.Width >= 45 && a11yInner.Height >= 12)
        {
            regions.Add(new ShowcaseHitRegion(
                new Rect(a11yInner.X, a11yInner.Y, a11yInner.Width, (ushort)Math.Min(7, (int)a11yInner.Height)),
                new("accessibility:overview", ShowcaseHitLayer.Content, 37_000)));

            var bodyY = a11yInner.Y + 7;
            var bodyHeight = Math.Max(1, a11yInner.Height - 8);
            var leftWidth = Math.Max(1, a11yInner.Width / 2);
            var rightX = a11yInner.X + leftWidth;
            var rightWidth = Math.Max(1, a11yInner.Width - leftWidth);
            var togglesHeight = Math.Min(8, bodyHeight);
            var wcagHeight = Math.Min(10, bodyHeight);
            regions.Add(new ShowcaseHitRegion(
                new Rect(a11yInner.X, (ushort)bodyY, (ushort)leftWidth, (ushort)Math.Max(1, togglesHeight)),
                new("accessibility:toggles", ShowcaseHitLayer.Content, 37_100)));
            regions.Add(new ShowcaseHitRegion(
                new Rect(
                    a11yInner.X,
                    (ushort)(bodyY + togglesHeight),
                    (ushort)leftWidth,
                    (ushort)Math.Max(1, bodyHeight - togglesHeight)),
                new("accessibility:preview", ShowcaseHitLayer.Content, 37_110)));
            regions.Add(new ShowcaseHitRegion(
                new Rect((ushort)rightX, (ushort)bodyY, (ushort)rightWidth, (ushort)Math.Max(1, wcagHeight)),
                new("accessibility:wcag", ShowcaseHitLayer.Content, 37_200)));
            regions.Add(new ShowcaseHitRegion(
                new Rect(
                    (ushort)rightX,
                    (ushort)(bodyY + wcagHeight),
                    (ushort)rightWidth,
                    (ushort)Math.Max(1, bodyHeight - wcagHeight)),
                new("accessibility:telemetry", ShowcaseHitLayer.Content, 37_210)));
            AddRegion(
                regions,
                a11yInner.X,
                Math.Max(a11yInner.Y, a11yInner.Bottom - 1),
                a11yInner.Width,
                new("accessibility:footer", ShowcaseHitLayer.Content, 37_300));
        }

        if (state.CurrentScreenNumber == 7 && TryResolveContentInnerArea(state.Viewport, out var formsInner) && formsInner.Width >= 30 && formsInner.Height >= 8)
        {
            var formWidth = Math.Max(1, formsInner.Width / 2);
            for (var row = 0; row < FormFieldCount; row++)
            {
                AddRegion(
                    regions,
                    formsInner.X,
                    formsInner.Y + 1 + row,
                    formWidth,
                    new($"forms_input:field:{row}", ShowcaseHitLayer.Content, (uint)(7_000 + row)));
            }

            regions.Add(new ShowcaseHitRegion(
                new Rect(
                    (ushort)(formsInner.X + formWidth),
                    formsInner.Y,
                    (ushort)Math.Max(1, formsInner.Width - formWidth),
                    formsInner.Height),
                new("forms_input:text_area", ShowcaseHitLayer.Content, 7_100)));
        }

        if (state.CurrentScreenNumber == 27 && TryResolveContentInnerArea(state.Viewport, out var validationInner) && validationInner.Width >= 45 && validationInner.Height >= 10)
        {
            var leftWidth = Math.Max(1, validationInner.Width * 42 / 100);
            var centerWidth = Math.Max(1, validationInner.Width * 34 / 100);
            var rightWidth = Math.Max(1, validationInner.Width - leftWidth - centerWidth);
            var centerX = validationInner.X + leftWidth;
            var rightX = centerX + centerWidth;

            AddRegion(
                regions,
                validationInner.X,
                validationInner.Y,
                leftWidth,
                new("form_validation:mode", ShowcaseHitLayer.Content, 27_000));
            for (var row = 0; row < FormValidationFieldCount; row++)
            {
                AddRegion(
                    regions,
                    validationInner.X,
                    validationInner.Y + 4 + row,
                    leftWidth,
                    new($"form_validation:field:{row}", ShowcaseHitLayer.Content, (uint)(27_010 + row)));
            }

            AddRegion(
                regions,
                validationInner.X,
                Math.Max(validationInner.Y, validationInner.Bottom - 4),
                leftWidth,
                new("form_validation:touched_dirty", ShowcaseHitLayer.Content, 27_030));
            var errorRows = Math.Max(1, Math.Min(8, validationInner.Height * 58 / 100 - 2));
            for (var row = 0; row < errorRows; row++)
            {
                AddRegion(
                    regions,
                    centerX,
                    validationInner.Y + 2 + row,
                    centerWidth,
                    new($"form_validation:error:{row}", ShowcaseHitLayer.Content, (uint)(27_100 + row)));
            }

            regions.Add(new ShowcaseHitRegion(
                new Rect(
                    (ushort)centerX,
                    (ushort)(validationInner.Y + Math.Max(1, validationInner.Height * 58 / 100)),
                    (ushort)centerWidth,
                    (ushort)Math.Max(1, validationInner.Height - Math.Max(1, validationInner.Height * 58 / 100))),
                new("form_validation:rules", ShowcaseHitLayer.Content, 27_130)));
            AddRegion(
                regions,
                rightX,
                validationInner.Y,
                rightWidth,
                new("form_validation:controls", ShowcaseHitLayer.Content, 27_200));
            AddRegion(
                regions,
                rightX,
                validationInner.Y + 9,
                rightWidth,
                new("form_validation:notifications", ShowcaseHitLayer.Content, 27_210));
            regions.Add(new ShowcaseHitRegion(
                new Rect(
                    (ushort)rightX,
                    (ushort)(validationInner.Y + 16),
                    (ushort)rightWidth,
                    (ushort)Math.Max(1, validationInner.Height - 16)),
                new("form_validation:diagnostics", ShowcaseHitLayer.Content, 27_220)));
        }

        if (state.CurrentScreenNumber == 26 && TryResolveContentInnerArea(state.Viewport, out var mouseInner) && mouseInner.Width >= 16 && mouseInner.Height >= 6)
        {
            var targetPanelWidth = Math.Max(4, mouseInner.Width * 42 / 100);
            var gridX = (ushort)(mouseInner.X + 1);
            var gridY = (ushort)(mouseInner.Y + 1);
            var gridWidth = Math.Max(4, targetPanelWidth - 2);
            var gridHeight = Math.Max(3, mouseInner.Height - 2);
            var cellWidth = Math.Max(1, gridWidth / 4);
            var cellHeight = Math.Max(1, gridHeight / 3);
            for (var row = 0; row < 3; row++)
            {
                for (var col = 0; col < 4; col++)
                {
                    var target = (row * 4) + col + 1;
                    regions.Add(new ShowcaseHitRegion(
                        new Rect(
                            (ushort)(gridX + col * cellWidth),
                            (ushort)(gridY + row * cellHeight),
                            (ushort)Math.Max(1, cellWidth),
                            (ushort)Math.Max(1, cellHeight)),
                        new($"target:{target}", ShowcaseHitLayer.Content, (uint)target)));
                }
            }
        }

        if (state.CurrentScreenNumber == 41 && TryResolveContentInnerArea(state.Viewport, out var hyperlinkInner) && hyperlinkInner.Width >= 32 && hyperlinkInner.Height >= 9)
        {
            var linkPanelWidth = Math.Max(1, hyperlinkInner.Width * 45 / 100);
            var linkRowX = hyperlinkInner.X + 1;
            var linkRowWidth = Math.Max(1, linkPanelWidth - 2);
            for (var index = 0; index < 5; index++)
            {
                AddRegion(
                    regions,
                    linkRowX,
                    hyperlinkInner.Y + 5 + index,
                    linkRowWidth,
                    new(
                        $"link:{index + 1}",
                        ShowcaseHitLayer.Link,
                        LinkRawId(index)));
            }
        }

        if (state.CurrentScreenNumber == 2 && TryResolveContentInnerArea(state.Viewport, out var contentInner) && contentInner.Width >= 18 && contentInner.Height >= 6)
        {
            var rightColumnStart = contentInner.X + Math.Max(1, contentInner.Width * 52 / 100);
            var leftWidth = Math.Max(1, rightColumnStart - contentInner.X);
            var leftLinks = new[] { 18, 8, 4, 14 };
            for (var index = 0; index < leftLinks.Length; index++)
            {
                var target = leftLinks[index];
                AddRegion(
                    regions,
                    contentInner.X,
                    contentInner.Y + 3 + index,
                    leftWidth,
                    new(
                        $"pane:{target}",
                        ShowcaseHitLayer.Pane,
                        PaneRawId(target),
                        TargetScreenNumber: target));
            }

            var x = rightColumnStart;
            var width = Math.Max(1, contentInner.Right - x);
            var links = new[] { 18, 8, 4, 14, 6, 44, 22, 15 };
            for (var index = 0; index < links.Length; index++)
            {
                var target = links[index];
                AddRegion(
                    regions,
                    x,
                    contentInner.Y + 3 + index,
                    width,
                    new(
                        $"pane:{target}",
                        ShowcaseHitLayer.Pane,
                        PaneRawId(target),
                        TargetScreenNumber: target));
            }
        }

        RegisterCurrentScreenBodyPane(state, regions);
    }

    private static void RegisterCurrentScreenBodyPane(ShowcaseDemoState state, List<ShowcaseHitRegion> regions)
    {
        if (!TryResolveContentInnerArea(state.Viewport, out var contentInner))
        {
            return;
        }

        var current = state.CurrentScreenNumber;
        regions.Add(new ShowcaseHitRegion(
            contentInner,
            new($"pane:{current}", ShowcaseHitLayer.Pane, PaneRawId(current), TargetScreenNumber: current)));
    }

    private static bool TryResolveContentInnerArea(Size viewport, out Rect inner)
    {
        inner = default;
        if (viewport.Width <= 2 || viewport.Height <= 4)
        {
            return false;
        }

        inner = new Rect(1, 2, (ushort)(viewport.Width - 2), (ushort)(viewport.Height - 4));
        return !inner.IsEmpty;
    }

    private static void AddRegion(
        List<ShowcaseHitRegion> regions,
        int x,
        int y,
        int width,
        ShowcaseHitTestResult result)
    {
        if (width <= 0 || x >= ushort.MaxValue || y >= ushort.MaxValue)
        {
            return;
        }

        regions.Add(new ShowcaseHitRegion(
            new Rect((ushort)Math.Max(x, 0), (ushort)Math.Max(y, 0), (ushort)Math.Min(width, ushort.MaxValue), 1),
            result));
    }

    private const int FormFieldCount = 3;
    private const int FormValidationFieldCount = 9;

    private static int CategoryIndex(ShowcaseScreenCategory category)
    {
        for (var index = 0; index < ShowcaseCatalog.Categories.Count; index++)
        {
            if (ShowcaseCatalog.Categories[index] == category)
            {
                return index;
            }
        }

        return 0;
    }
}
