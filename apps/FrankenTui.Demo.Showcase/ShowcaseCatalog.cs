namespace FrankenTui.Demo.Showcase;

internal enum ShowcaseScreenCategory
{
    Tour,
    Core,
    Visuals,
    Interaction,
    Text,
    Systems
}

internal readonly record struct ShowcaseScreen(
    int Number,
    string Id,
    string Title,
    string ShortLabel,
    string Slug,
    ShowcaseScreenCategory Category,
    string Blurb);

internal static class ShowcaseCatalog
{
    public static readonly IReadOnlyList<ShowcaseScreen> Screens =
    [
        new(1, "GuidedTour", "Guided Tour", "Tour", "guided_tour", ShowcaseScreenCategory.Tour, "Cinematic auto-play tour across key screens."),
        new(2, "Dashboard", "Dashboard", "Dash", "dashboard", ShowcaseScreenCategory.Tour, "Cinematic overview of key features and live tiles."),
        new(3, "Shakespeare", "Shakespeare", "Shakes", "shakespeare", ShowcaseScreenCategory.Text, "Live search over Shakespeare with animated highlights."),
        new(4, "CodeExplorer", "Code Explorer", "Code", "code_explorer", ShowcaseScreenCategory.Text, "Code browser with pane routing and syntax preview."),
        new(5, "WidgetGallery", "Widget Gallery", "Widgets", "widget_gallery", ShowcaseScreenCategory.Core, "Library of core widgets in a compact gallery."),
        new(6, "LayoutLab", "Layout Lab", "Layout", "layout_lab", ShowcaseScreenCategory.Interaction, "Constraint lab + interactive pane studio (drag, resize, dock, undo/redo)."),
        new(7, "FormsInput", "Forms & Input", "Forms", "forms_input", ShowcaseScreenCategory.Interaction, "Form fields, validation cues, and input widgets."),
        new(8, "DataViz", "Data Viz", "DataViz", "data_viz", ShowcaseScreenCategory.Visuals, "Dense charts and small-multiple visualizations."),
        new(9, "FileBrowser", "File Browser", "Files", "file_browser", ShowcaseScreenCategory.Interaction, "File tree with previews and pane routing."),
        new(10, "AdvancedFeatures", "Advanced", "Adv", "advanced_features", ShowcaseScreenCategory.Core, "Advanced widget patterns and composite layouts."),
        new(11, "TableThemeGallery", "Table Theme Gallery", "Tables", "table_theme_gallery", ShowcaseScreenCategory.Visuals, "Preset gallery for TableTheme across widget + markdown tables."),
        new(12, "TerminalCapabilities", "Terminal Capabilities", "Caps", "terminal_capabilities", ShowcaseScreenCategory.Systems, "Terminal capability detection and feature matrix."),
        new(13, "MacroRecorder", "Macro Recorder", "Macro", "macro_recorder", ShowcaseScreenCategory.Interaction, "Record, edit, and replay input macros."),
        new(14, "Performance", "Performance", "Perf", "performance", ShowcaseScreenCategory.Systems, "Render performance metrics and budgets."),
        new(15, "MarkdownRichText", "Markdown", "MD", "markdown_rich_text", ShowcaseScreenCategory.Text, "Markdown rendering with styling and layout."),
        new(16, "MermaidShowcase", "Mermaid Showcase", "Mermaid", "mermaid_showcase", ShowcaseScreenCategory.Visuals, "Interactive Mermaid diagrams with layout diagnostics and live controls."),
        new(17, "MermaidMegaShowcase", "Mermaid Mega Showcase", "MermaidMega", "mermaid_mega_showcase", ShowcaseScreenCategory.Visuals, "Comprehensive Mermaid diagram lab with performance knobs and diagnostics."),
        new(18, "VisualEffects", "Visual Effects", "VFX", "visual_effects", ShowcaseScreenCategory.Visuals, "High-performance visual effects playground."),
        new(19, "ResponsiveDemo", "Responsive Layout", "Resp", "responsive_demo", ShowcaseScreenCategory.Core, "Responsive layout behavior across sizes."),
        new(20, "LogSearch", "Log Search", "Logs", "log_search", ShowcaseScreenCategory.Text, "Search and filter logs with live updates."),
        new(21, "Notifications", "Notifications", "Notify", "notifications", ShowcaseScreenCategory.Interaction, "Toast notifications and transient UI patterns."),
        new(22, "ActionTimeline", "Action Timeline", "Timeline", "action_timeline", ShowcaseScreenCategory.Systems, "Event stream and action timeline viewer."),
        new(23, "IntrinsicSizing", "Intrinsic Sizing", "Sizing", "intrinsic_sizing", ShowcaseScreenCategory.Core, "Intrinsic sizing and content measurement."),
        new(24, "LayoutInspector", "Layout Inspector", "Inspect", "layout_inspector", ShowcaseScreenCategory.Core, "Constraint solver visual inspector."),
        new(25, "AdvancedTextEditor", "Advanced Text Editor", "Editor", "advanced_text_editor", ShowcaseScreenCategory.Text, "Advanced multi-line editor with search."),
        new(26, "MousePlayground", "Mouse Playground", "Mouse", "mouse_playground", ShowcaseScreenCategory.Interaction, "Mouse and hit-test interactions."),
        new(27, "FormValidation", "Form Validation", "Validate", "form_validation", ShowcaseScreenCategory.Interaction, "Form validation states and error cues."),
        new(28, "VirtualizedSearch", "Virtualized Search", "VirtSearch", "virtualized_search", ShowcaseScreenCategory.Systems, "Virtualized list with fast search."),
        new(29, "AsyncTasks", "Async Tasks", "Tasks", "async_tasks", ShowcaseScreenCategory.Systems, "Async tasks and job queue visualization."),
        new(30, "ThemeStudio", "Theme Studio", "Themes", "theme_studio", ShowcaseScreenCategory.Visuals, "Live theme and palette studio."),
        new(31, "SnapshotPlayer", "Time-Travel Studio", "TimeTravel", "snapshot_player", ShowcaseScreenCategory.Visuals, "Time-travel snapshots with replay controls."),
        new(32, "PerformanceHud", "Performance Challenge", "PerfChal", "performance_hud", ShowcaseScreenCategory.Systems, "Stress harness for degradation tiers and budget recovery."),
        new(33, "ExplainabilityCockpit", "Explainability Cockpit", "Explain", "explainability_cockpit", ShowcaseScreenCategory.Systems, "Diff, resize, and budget evidence in one cockpit."),
        new(34, "I18nDemo", "i18n Stress Lab", "i18n", "i18n_demo", ShowcaseScreenCategory.Text, "International text and width edge cases."),
        new(35, "VoiOverlay", "VOI Overlay", "VOI", "voi_overlay", ShowcaseScreenCategory.Systems, "Value-of-information overlay and evidence."),
        new(36, "InlineModeStory", "Inline Mode", "Inline", "inline_mode_story", ShowcaseScreenCategory.Tour, "Inline mode story and scrollback preservation."),
        new(37, "AccessibilityPanel", "Accessibility", "A11y", "accessibility_panel", ShowcaseScreenCategory.Systems, "Accessibility settings and telemetry."),
        new(38, "WidgetBuilder", "Widget Builder", "Builder", "widget_builder", ShowcaseScreenCategory.Core, "Interactive widget builder sandbox."),
        new(39, "CommandPaletteLab", "Command Palette Evidence Lab", "Palette", "command_palette_lab", ShowcaseScreenCategory.Interaction, "Command palette ranking with evidence details."),
        new(40, "DeterminismLab", "Determinism Lab", "Determinism", "determinism_lab", ShowcaseScreenCategory.Systems, "Checksum equivalence and determinism checks."),
        new(41, "HyperlinkPlayground", "Hyperlink Playground", "Links", "hyperlink_playground", ShowcaseScreenCategory.Interaction, "OSC-8 hyperlink playground and hit regions."),
        new(42, "KanbanBoard", "Kanban Board", "Kanban", "kanban_board", ShowcaseScreenCategory.Interaction, "Interactive Kanban board with drag-drop task management."),
        new(43, "MarkdownLiveEditor", "Live Markdown Editor", "MD Live", "markdown_live_editor", ShowcaseScreenCategory.Text, "Split-pane editor with live Markdown preview, search, and diff mode."),
        new(44, "DragDrop", "Drag & Drop Lab", "DragDrop", "drag_drop", ShowcaseScreenCategory.Interaction, "Sortable and cross-container drag/drop interactions with keyboard parity."),
        new(45, "QuakeEasterEgg", "Quake E1M1 (Easter Egg)", "Quake", "quake_easter_egg", ShowcaseScreenCategory.Visuals, "Retro Quake E1M1 renderer as the final easter-egg screen.")
    ];

    public static IReadOnlyList<ShowcaseScreenCategory> Categories { get; } =
    [
        ShowcaseScreenCategory.Tour,
        ShowcaseScreenCategory.Core,
        ShowcaseScreenCategory.Visuals,
        ShowcaseScreenCategory.Interaction,
        ShowcaseScreenCategory.Text,
        ShowcaseScreenCategory.Systems
    ];

    public static ShowcaseScreen Get(int screenNumber) => Screens[ClampScreenNumber(screenNumber) - 1];

    public static int ClampScreenNumber(int screenNumber) => Math.Clamp(screenNumber, 1, Screens.Count);

    public static int Move(int currentScreenNumber, int delta) =>
        Wrap(ClampScreenNumber(currentScreenNumber) - 1 + delta, Screens.Count) + 1;

    public static int FirstInCategory(ShowcaseScreenCategory category) =>
        Screens.First(screen => screen.Category == category).Number;

    public static IReadOnlyList<ShowcaseScreen> WindowAround(int currentScreenNumber, int maxItems = 8)
    {
        var clamped = ClampScreenNumber(currentScreenNumber);
        if (Screens.Count <= maxItems)
        {
            return Screens;
        }

        var radius = Math.Max(maxItems / 2, 1);
        var start = Math.Max(clamped - radius, 1);
        var end = Math.Min(start + maxItems - 1, Screens.Count);
        start = Math.Max(end - maxItems + 1, 1);
        return Screens.Skip(start - 1).Take(end - start + 1).ToArray();
    }

    public static int ResolveLegacyScenario(string? scenario) =>
        scenario?.Trim().ToLowerInvariant() switch
        {
            "overview" => 2,
            "interaction" => 6,
            "tooling" => 14,
            "extras" => 16,
            _ => 1
        };

    private static int Wrap(int index, int count)
    {
        var wrapped = index % count;
        return wrapped < 0 ? wrapped + count : wrapped;
    }
}
