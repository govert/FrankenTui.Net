using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Layout;
using FrankenTui.Runtime;
using FrankenTui.Text;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

internal static class ShowcaseSurface
{
    private static readonly IReadOnlyList<string> ShakespeareLines =
    [
        "To be, or not to be, that is the question:",
        "Whether 'tis nobler in the mind to suffer",
        "The slings and arrows of outrageous fortune,",
        "Or to take arms against a sea of troubles,",
        "And by opposing end them."
    ];

    private static readonly IReadOnlyList<string> LogLines =
    [
        "08:00 info  showcase booted",
        "08:01 debug diff decision FULL rows=42",
        "08:02 warn  replay ledger missing one artifact",
        "08:03 info  command palette ranked 11 results",
        "08:04 info  macro replay loop stabilized",
        "08:05 error benchmark gate exceeded target",
        "08:06 info  windows terminal viewport detected"
    ];

    private static readonly IReadOnlyList<FormTextField> FormFields =
    [
        new("repo", "Repository", "FrankenTui.Net", "Port target"),
        new("screen", "Start Screen", "5", "Widget gallery"),
        new("seed", "Seed", "42", "Deterministic demo seed")
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<TextValidator>> FormValidators =
        new Dictionary<string, IReadOnlyList<TextValidator>>(StringComparer.Ordinal)
        {
            ["repo"] = [ValidationRules.Required()],
            ["screen"] = [ValidationRules.Required(), ValidationRules.ContainsDigit()],
            ["seed"] = [ValidationRules.Required(), ValidationRules.ContainsDigit()]
        };

    private static readonly IReadOnlyList<string> NotificationLines =
    [
        "sync completed",
        "macro captured",
        "palette preview ready",
        "perf HUD degraded",
        "doctor artifacts refreshed"
    ];

    internal const int PaletteLabBenchStepTicks = 3;

    private static readonly IReadOnlyList<string> PaletteLabBenchQueries =
    [
        "open",
        "theme",
        "perf",
        "markdown",
        "log",
        "palette",
        "inline",
        "help"
    ];

    public static IRuntimeView Create(ShowcaseDemoState state) =>
        new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed((ushort)1), BuildAppNavigation()),
                (LayoutConstraint.Fill(), BuildBody(state)),
                (LayoutConstraint.Fixed((ushort)1), BuildStatus(state))
            ]);

    public static string BuildHelpText() =>
        """
        Keybindings
        1-9 / 0  jump to screens 1-10
        Tab      next screen
        Shift+Tab previous screen
        Shift+H/L previous/next screen
        Left/Right cycle screens
        F6 / m   toggle mouse capture
        Ctrl+K   command palette
        Ctrl+F   favorite selected palette item
        Ctrl+Shift+F favorites-only palette filter
        Ctrl+0..N palette category filter
        Ctrl+I   toggle evidence ledger
        Ctrl+P   toggle performance HUD
        F12      toggle debug overlay
        Shift+A  toggle accessibility panel
        ?        toggle help
        q        quit

        Guided Tour
        Enter or Space starts the tour
        n / p step while touring
        Space pauses/resumes the tour
        Esc exits the tour
        """;

    private static IWidget BuildBody(ShowcaseDemoState state)
    {
        var content = BuildContent(state);
        if (!state.EvidenceLedgerVisible &&
            !state.PerfHudVisible &&
            !state.DebugVisible &&
            !state.HelpVisible &&
            !state.A11yPanelVisible &&
            !state.Session.CommandPalette.IsOpen)
        {
            return content;
        }

        var overlayChildren = new List<(LayoutConstraint Constraint, IWidget Widget)>();
        if (state.A11yPanelVisible)
        {
            overlayChildren.Add((LayoutConstraint.Fill(), BuildA11yPanel(state)));
        }

        if (state.HelpVisible)
        {
            overlayChildren.Add((LayoutConstraint.Fill(), new PanelWidget
            {
                Title = "Help",
                Child = new ParagraphWidget(BuildHelpText())
            }));
        }

        if (state.DebugVisible)
        {
            overlayChildren.Add((LayoutConstraint.Fill(), BuildDebugPanel(state)));
        }

        if (state.PerfHudVisible)
        {
            overlayChildren.Add((LayoutConstraint.Fill(), BuildPerfHudPanel(state)));
        }

        if (state.EvidenceLedgerVisible)
        {
            overlayChildren.Add((LayoutConstraint.Fill(), BuildEvidenceLedgerPanel(state)));
        }

        if (state.Session.CommandPalette.IsOpen)
        {
            overlayChildren.Add((LayoutConstraint.Fill(), BuildPalettePanel(state)));
        }

        return new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Percentage((ushort)68), content),
                (LayoutConstraint.Fill(), new StackWidget(LayoutDirection.Vertical, overlayChildren))
            ]);
    }

    private static IWidget BuildEvidenceLedgerPanel(ShowcaseDemoState state)
    {
        var stats = state.RuntimeStats ?? RuntimeFrameStats.Empty;
        var palette = state.Session.CommandPalette;
        return new PanelWidget
        {
            Title = "Evidence Ledger",
            Child = new ParagraphWidget(
                $"screen: {state.CurrentScreen.Number} {state.CurrentScreen.Slug}\n" +
                $"frame_ms: {stats.FrameDurationMs:0.##} changed={stats.ChangedCells} dirty_rows={stats.DirtyRows}\n" +
                $"degradation: {stats.DegradationLevel} bytes={stats.BytesEmitted} sync={BoolFlag(stats.SyncOutput)}\n" +
                $"pane_hash: {state.Session.PaneWorkspace.SnapshotHash()}\n" +
                $"tour: active={BoolFlag(state.TourActive)} paused={BoolFlag(state.TourPaused)} speed={state.TourSpeed:0.##}\n" +
                $"palette: open={BoolFlag(palette.IsOpen)} query='{palette.Query}' selected={palette.SelectedIndex}\n" +
                $"mouse_capture: {BoolFlag(state.MouseCaptureEnabled)}\n" +
                $"overlays: {OverlayList(state)}")
        };
    }

    private static IWidget BuildA11yPanel(ShowcaseDemoState state) =>
        new PanelWidget
        {
            Title = "A11y",
            Child = new ParagraphWidget(
                $"Theme: {(state.A11yHighContrast ? "high contrast" : "standard")}\n" +
                $"Reduced motion: {BoolFlag(state.A11yReducedMotion)}\n" +
                $"Large text: {BoolFlag(state.A11yLargeText)}\n" +
                $"Flow: {state.FlowDirection}\n" +
                $"Language: {state.Language}\n" +
                "Shift+H contrast | Shift+M motion | Shift+L large text")
        };

    private static IWidget BuildDebugPanel(ShowcaseDemoState state)
    {
        var stats = state.RuntimeStats ?? RuntimeFrameStats.Empty;
        return new PanelWidget
        {
            Title = "Debug",
            Child = new ParagraphWidget(
                $"screen: {state.CurrentScreen.Number} {state.CurrentScreen.Slug}\n" +
                $"viewport: {state.Viewport.Width}x{state.Viewport.Height}\n" +
                $"inline: {state.InlineMode}\n" +
                $"mouse_capture: {state.MouseCaptureEnabled}\n" +
                $"tour: {(state.TourActive ? "active" : "off")} paused={state.TourPaused} speed={state.TourSpeed:0.##}\n" +
                $"palette: {(state.Session.CommandPalette.IsOpen ? "open" : "closed")} help={state.HelpVisible}\n" +
                $"pane: {state.Session.PaneWorkspace.SnapshotHash()}\n" +
                $"changed={stats.ChangedCells} rows={stats.DirtyRows} bytes={stats.BytesEmitted}")
        };
    }

    private static IWidget BuildPerfHudPanel(ShowcaseDemoState state) =>
        new PerformanceHudWidget
        {
            Snapshot = state.RuntimeStats is { } stats
                ? PerformanceHudSnapshot.FromRuntime(stats, stats.SyncOutput, scrollRegion: true, hyperlinks: true)
                : PerformanceHudSnapshot.FromSession(state.Session) with { Level = PerformanceHudLevel.Full }
        };

    private static string BoolFlag(bool value) => value ? "on" : "off";

    private static IWidget BuildContent(ShowcaseDemoState state) =>
        state.CurrentScreen.Slug switch
        {
            "guided_tour" => BuildGuidedTour(state),
            "dashboard" => BuildDashboard(state),
            "shakespeare" => BuildShakespeare(state),
            "code_explorer" => BuildCodeExplorer(state),
            "widget_gallery" => BuildWidgetGallery(state),
            "layout_lab" => BuildLayoutLab(state),
            "forms_input" => BuildFormsInput(state),
            "data_viz" => BuildDataViz(state),
            "file_browser" => BuildFileBrowser(state),
            "advanced_features" => BuildAdvancedFeatures(state),
            "table_theme_gallery" => BuildTableThemeGallery(state),
            "terminal_capabilities" => BuildTerminalCapabilities(state),
            "macro_recorder" => BuildMacroRecorder(state),
            "performance" => BuildPerformance(state),
            "markdown_rich_text" => BuildMarkdown(state),
            "mermaid_showcase" => BuildMermaid(state),
            "mermaid_mega_showcase" => BuildMermaidMega(state),
            "visual_effects" => BuildVisualEffects(state),
            "responsive_demo" => BuildResponsive(state),
            "log_search" => BuildLogSearch(state),
            "notifications" => BuildNotifications(state),
            "action_timeline" => BuildActionTimeline(state),
            "intrinsic_sizing" => BuildIntrinsicSizing(state),
            "layout_inspector" => BuildLayoutInspector(state),
            "advanced_text_editor" => BuildAdvancedTextEditor(state),
            "mouse_playground" => BuildMousePlayground(state),
            "form_validation" => BuildFormValidation(state),
            "virtualized_search" => BuildVirtualizedSearch(state),
            "async_tasks" => BuildAsyncTasks(state),
            "theme_studio" => BuildThemeStudio(state),
            "snapshot_player" => BuildSnapshotPlayer(state),
            "performance_hud" => BuildPerformanceChallenge(state),
            "explainability_cockpit" => BuildExplainability(state),
            "i18n_demo" => BuildI18n(state),
            "voi_overlay" => BuildVoiOverlay(state),
            "inline_mode_story" => BuildInlineModeStory(state),
            "accessibility_panel" => BuildAccessibility(state),
            "widget_builder" => BuildWidgetBuilder(state),
            "command_palette_lab" => BuildCommandPaletteLab(state),
            "determinism_lab" => BuildDeterminismLab(state),
            "hyperlink_playground" => BuildHyperlinkPlayground(state),
            "kanban_board" => BuildKanbanBoard(state),
            "markdown_live_editor" => BuildMarkdownLiveEditor(state),
            "drag_drop" => BuildDragDrop(state),
            "quake_easter_egg" => BuildQuake(state),
            _ => BuildPlaceholder(state)
        };

    private static IWidget BuildCategoryTabs(ShowcaseDemoState state) =>
        new TabsWidget
        {
            Tabs = ShowcaseCatalog.Categories.Select(static category => category.ToString()).ToArray(),
            SelectedIndex = Array.IndexOf(ShowcaseCatalog.Categories.ToArray(), state.CurrentScreen.Category)
        };

    private static IWidget BuildScreenTabs(ShowcaseDemoState state)
    {
        var window = ShowcaseCatalog.WindowAround(state.CurrentScreenNumber);
        var tabs = window
            .Select(screen => screen.Number == state.CurrentScreenNumber
                ? $"{screen.Number}:{screen.ShortLabel}"
                : $"{screen.Number}")
            .ToArray();
        var selectedIndex = window
            .Select((screen, index) => (screen, index))
            .First(candidate => candidate.screen.Number == state.CurrentScreenNumber)
            .index;
        return new TabsWidget
        {
            Tabs = tabs,
            SelectedIndex = selectedIndex
        };
    }

    private static IWidget BuildAppNavigation() =>
        new ParagraphWidget(" 1: Tour │ 2: Dash │ 3: Shakes │ 4: Code │ 5: Widgets │ 6: Layout │ 7: Forms");

    private static IWidget BuildStatus(ShowcaseDemoState state)
    {
        var details = $" {state.CurrentScreen.Title} [{state.CurrentScreenNumber}/{ShowcaseCatalog.Screens.Count}] [h] [cmd] [p] [d]   Tab/Shift+Tab: next/prev    0x0 00:00";
        if (state.TourActive)
        {
            details += $" | tour {(state.TourPaused ? "paused" : "live")} @{state.TourSpeed:0.##}x";
        }

        if (state.EvidenceLedgerVisible || state.PerfHudVisible || state.DebugVisible || state.HelpVisible || state.A11yPanelVisible)
        {
            details += $" | overlays:{OverlayList(state)}";
        }

        if (state.RuntimeStats is { } stats)
        {
            details += $" | frame {stats.FrameDurationMs:0.0}ms | changed {stats.ChangedCells} | {stats.DegradationLevel}";
        }

        return new ParagraphWidget(details);
    }

    private static string OverlayList(ShowcaseDemoState state)
    {
        var overlays = new List<string>();
        if (state.EvidenceLedgerVisible)
        {
            overlays.Add("evidence");
        }

        if (state.PerfHudVisible)
        {
            overlays.Add("perf");
        }

        if (state.DebugVisible)
        {
            overlays.Add("debug");
        }

        if (state.HelpVisible)
        {
            overlays.Add("help");
        }

        if (state.A11yPanelVisible)
        {
            overlays.Add("a11y");
        }

        return string.Join(",", overlays);
    }

    private static string PaneWorkspaceLoadStatus(ShowcaseDemoState state)
    {
        if (state.PaneWorkspaceRecoveryError is not null)
        {
            return $"recovered from invalid snapshot ({state.PaneWorkspaceRecoveryError})";
        }

        return state.PaneWorkspaceLoaded ? "restored from workspace file" : "default workspace";
    }

    private static IWidget BuildPalettePanel(ShowcaseDemoState state)
    {
        var palette = state.Session.CommandPalette;
        var results = CommandPaletteController.Results(palette, ShowcaseCommandPalette.Entries());
        return new CommandPaletteWidget
        {
            Query = palette.Query,
            Results = results,
            SelectedIndex = Math.Min(palette.SelectedIndex, Math.Max(results.Count - 1, 0)),
            ShowPreview = true
        };
    }

    private static IWidget BuildGuidedTour(ShowcaseDemoState state)
    {
        if (state.TourActive)
        {
            var callout = state.TourCallout ?? ShowcaseTourStoryboard.ForScreen(state.CurrentScreen);
            var highlight = ShowcaseTourStoryboard.FormatRect(ShowcaseTourStoryboard.ResolveHighlight(callout, state.Viewport));
            return new StackWidget(
                LayoutDirection.Vertical,
                [
                    (LayoutConstraint.Fixed((ushort)7), new PanelWidget
                    {
                        Title = "Tour Callout",
                        Child = new ParagraphWidget(
                            $"{callout.Title}\n" +
                            $"{callout.Body}\n" +
                            $"Hint: {callout.Hint}\n" +
                            $"Highlight: {highlight}")
                    }),
                    (LayoutConstraint.Fixed((ushort)5), new PanelWidget
                    {
                        Title = "Tour Status",
                        Child = new ParagraphWidget(
                            $"Touring screen {state.CurrentScreenNumber} of {ShowcaseCatalog.Screens.Count}\n" +
                            $"Step: {state.TourStepIndex + 1} of {ShowcaseTourStoryboard.Count}\n" +
                            $"Speed: {state.TourSpeed:0.##}x\n" +
                            $"Status: {(state.TourPaused ? "paused" : "playing")}\n" +
                            "Press Space to pause/resume, n/p to step, Esc to stop.")
                    }),
                    (LayoutConstraint.Fill(), BuildDashboard(state with { CurrentScreenNumber = 2 }))
                ]);
        }

        return new PanelWidget
        {
            Title = "Guided Tour",
            Child = new ParagraphWidget(
                "FrankenTui.Net now launches an upstream-shaped terminal showcase.\n\n" +
                "Start a guided tour through the screen catalog with Enter or Space.\n" +
                $"Start screen: {state.TourStartScreen} | Speed: {state.TourSpeed:0.##}x\n" +
                "Use Up/Down or j/k to choose a start screen, +/- to tune speed, r to reset.\n" +
                "Use --tour to launch directly into autoplay, or --screen N to start on a specific screen.\n\n" +
                "Featured stops:\n2 Dashboard\n5 Widget Gallery\n6 Layout Lab\n15 Markdown\n16 Mermaid Showcase\n30 Theme Studio\n40 Determinism Lab")
        };
    }

    private static IWidget BuildDashboard(ShowcaseDemoState state) =>
        TwoColumn(
            DashboardSurface.CreateDefault(
                "Overview",
                [
                    "45-screen showcase catalog",
                    "upstream-shaped numbering and titles",
                    "runtime-backed interactive loop",
                    "Windows Terminal viewport support"
                ]),
            new PanelWidget
            {
                Title = "Highlights",
                Child = new ListWidget
                {
                    Items =
                    [
                        "Guided tour + screen tabs",
                        "Command palette overlay",
                        "Mermaid / markdown / macro surfaces",
                        "Pane workspace and operator tools",
                        "Determinism and runtime evidence panels"
                    ]
                }
            });

    private static IWidget BuildShakespeare(ShowcaseDemoState state)
    {
        var searchState = state.Session.LogSearch with
        {
            Query = string.IsNullOrWhiteSpace(state.Session.LogSearch.Query) ? "be" : state.Session.LogSearch.Query,
            SearchOpen = true
        };

        return TwoColumn(
            new LogSearchWidget
            {
                State = searchState,
                SourceLines = ShakespeareLines
            },
            Panel("Notes", "A text-heavy screen driven through the same search primitives used elsewhere in the demo."));
    }

    private static IWidget BuildCodeExplorer(ShowcaseDemoState state) =>
        TwoColumn(
            new PanelWidget
            {
                Title = "Tree",
                Child = new TreeWidget
                {
                    Nodes =
                    [
                        new TreeNode("src", [new TreeNode("app.rs", []), new TreeNode("chrome.rs", []), new TreeNode("screens", [new TreeNode("widget_gallery.rs", []), new TreeNode("theme_studio.rs", [])])]),
                        new TreeNode("docs", [new TreeNode("spec", []), new TreeNode("adr", [])]),
                        new TreeNode("tests", [new TreeNode("showcase_smoke.rs", [])])
                    ]
                }
            },
            new TextAreaWidget
            {
                Document = TextDocument.FromString(
                    "fn render_gallery(frame: &mut Frame) {\n" +
                    "    let tabs = screen_registry();\n" +
                    "    chrome::render_tab_bar(frame, &tabs);\n" +
                    "}\n"),
                Cursor = new TextCursor(1, 7),
                HasFocus = true,
                StatusText = "Rust syntax preview"
            });

    private static IWidget BuildWidgetGallery(ShowcaseDemoState state)
    {
        var left = new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed((ushort)6), new PanelWidget
                {
                    Title = "Progress",
                    Child = new ProgressWidget
                    {
                        Value = ((state.ScriptFrame % 9) + 1) / 10.0,
                        Label = "render completeness"
                    }
                }),
                (LayoutConstraint.Fill(), new PanelWidget
                {
                    Title = "List",
                    Child = new ListWidget
                    {
                        Items = ["Paragraph", "Panel", "Table", "Tabs", "Tree", "TextArea", "Progress"],
                        SelectedIndex = 4
                    }
                })
            ]);

        var right = new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed((ushort)6), new PanelWidget
                {
                    Title = "Tabs",
                    Child = new TabsWidget
                    {
                        Tabs = ["Core", "Text", "Layout", "Extras"],
                        SelectedIndex = 2
                    }
                }),
                (LayoutConstraint.Fill(), new PanelWidget
                {
                    Title = "Table",
                    Child = new TableWidget
                    {
                        Headers = ["Widget", "State", "Notes"],
                        Rows =
                        [
                            ["List", "ready", "selection + focus"],
                            ["Tree", "ready", "hierarchy view"],
                            ["TextArea", "ready", "cursor + status"],
                            ["Mermaid", "contract", "extras surface"]
                        ],
                        SelectedRow = 1
                    }
                })
            ]);

        return TwoColumn(left, right);
    }

    private static IWidget BuildLayoutLab(ShowcaseDemoState state) =>
        TwoColumn(
            new PaneWorkspaceWidget
            {
                Workspace = state.Session.PaneWorkspace
            },
            Panel(
                "Workspace Metrics",
                $"Selected: {state.Session.PaneWorkspace.SelectedPaneId}\n" +
                $"Mode: {state.Session.PaneWorkspace.Mode}\n" +
                $"Hash: {state.Session.PaneWorkspace.SnapshotHash()}\n" +
                $"Timeline: {state.Session.PaneWorkspace.Timeline.Count} actions\n" +
                $"Load: {PaneWorkspaceLoadStatus(state)}"));

    private static IWidget BuildFormsInput(ShowcaseDemoState state)
    {
        var validation = FormValidator.Validate(FormFields, FormValidators);
        return TwoColumn(
            new FormWidget
            {
                Fields = FormFields,
                Validation = validation,
                SelectedFieldIndex = 1
            },
            new TextAreaWidget
            {
                Document = TextDocument.FromString("name = \"FrankenTui.Net\"\nscreen = 7\nseed = 42"),
                Cursor = new TextCursor(1, 8),
                HasFocus = true,
                StatusText = "Text input and editing surface"
            });
    }

    private static IWidget BuildDataViz(ShowcaseDemoState state)
    {
        var value = ((state.ScriptFrame % 6) + 2) / 8.0;
        return TwoColumn(
            new StackWidget(
                LayoutDirection.Vertical,
                [
                    (LayoutConstraint.Fixed((ushort)4), new ProgressWidget { Value = value, Label = "frame budget used" }),
                    (LayoutConstraint.Fixed((ushort)4), new ProgressWidget { Value = 1.0 - value / 2, Label = "cache reuse" }),
                    (LayoutConstraint.Fill(), new TableWidget
                    {
                        Headers = ["Metric", "Value"],
                        Rows =
                        [
                            ["FPS target", "60"],
                            ["Dirty rows", $"{Math.Max(state.ScriptFrame, 1) * 3}"],
                            ["Frames", $"{state.ScriptFrame + 1}"],
                            ["Cells", $"{Math.Max(state.RuntimeStats?.ChangedCells ?? 128, 128)}"]
                        ]
                    })
                ]),
            Panel("Narrative", "The .NET port does not yet have upstream chart/canvas widgets, so this screen uses density through tables, progress lanes, and runtime counters."));
    }

    private static IWidget BuildFileBrowser(ShowcaseDemoState state) =>
        TwoColumn(
            new PanelWidget
            {
                Title = "Files",
                Child = new TreeWidget
                {
                    Nodes =
                    [
                        new TreeNode(".external", [new TreeNode("frankentui", [new TreeNode("README.md", []), new TreeNode("crates", [new TreeNode("ftui-demo-showcase", [])])])]),
                        new TreeNode("apps", [new TreeNode("FrankenTui.Demo.Showcase", [])]),
                        new TreeNode("src", [new TreeNode("FrankenTui.Extras", []), new TreeNode("FrankenTui.Tty", [])]),
                        new TreeNode("tests", [new TreeNode("FrankenTui.Tests.Headless", [])])
                    ]
                }
            },
            Panel("Preview", "README.md\n\nFrankenTui.Net is a traceable, updateable .NET 10 port of FrankenTUI.\n\nUse --screen N to land directly on a showcase screen."));

    private static IWidget BuildAdvancedFeatures(ShowcaseDemoState state) =>
        TwoColumn(
            new PanelWidget
            {
                Title = "Patterns",
                Child = new ListWidget
                {
                    Items = ["inline mode contract", "runtime input pipeline", "evidence capture", "SIMD acceleration", "web parity reuse"]
                }
            },
            Panel("Composite", "This screen groups the port-specific composites that now sit under an upstream-shaped showcase shell instead of the earlier hosted-parity dashboard."));

    private static IWidget BuildTableThemeGallery(ShowcaseDemoState state)
    {
        IReadOnlyList<IReadOnlyList<string>> rows =
        [
            new[] { "Default", "stable", "terminal-safe" },
            new[] { "Contrast", "warn", "high-visibility" },
            new[] { "Muted", "info", "documentation" },
            new[] { "Alert", "critical", "ops surfaces" }
        ];
        return ThreeColumn(
            new PanelWidget { Title = "Preset A", Child = new TableWidget { Headers = ["Theme", "Tone", "Use"], Rows = rows, SelectedRow = 0 } },
            new PanelWidget { Title = "Preset B", Child = new TableWidget { Headers = ["Theme", "Tone", "Use"], Rows = rows, SelectedRow = 1 } },
            new PanelWidget { Title = "Preset C", Child = new TableWidget { Headers = ["Theme", "Tone", "Use"], Rows = rows, SelectedRow = 2 } });
    }

    private static IWidget BuildTerminalCapabilities(ShowcaseDemoState state)
    {
        var syncOutput = state.RuntimeStats?.SyncOutput ?? false;
        var mouseState = state.MouseCaptureEnabled ? "enabled" : "standby";
        var host = OperatingSystem.IsWindows() ? "windows-console" : "pty";
        IReadOnlyList<IReadOnlyList<string>> rows =
        [
            ["true_color", "yes", "yes", "allow"],
            ["sync_output", syncOutput ? "yes" : "probe", syncOutput ? "enabled" : "defer", "safe outside mux"],
            ["hyperlinks", "osc", "osc8", "allow"],
            ["kitty_keyboard", OperatingSystem.IsWindows() ? "no" : "partial", "fallback", "key parser"],
            ["focus_events", "probe", "tracked", "input pipeline"],
            ["mouse_sgr", mouseState, mouseState, "user toggle"]
        ];
        var selectedRow = state.ScriptFrame % rows.Count;

        var evidence = $"""
            View modes: Matrix -> Evidence -> Simulation
            Selected: {rows[selectedRow][0]}

            Evidence Ledger
            environment TERM={Environment.GetEnvironmentVariable("TERM") ?? "unset"}
            da1 primary response cached
            da2 extended response cached
            decrpm sync-output probe={syncOutput.ToString().ToLowerInvariant()}
            osc color and hyperlink probes
            timeout bounded terminal reads
            prior profile={host}

            diagnostics:
            view_mode_changed
            selection_changed
            capability_inspected
            evidence_ledger_accessed
            """;

        var simulation = $"""
            Profile Simulation
            detected -> modern -> xterm-256color
            xterm -> vt100 -> dumb
            tmux -> screen -> zellij
            kitty -> windows-console -> linux-console

            profile_cycled
            profile_reset
            simulation_activated
            environment_read
            report_exported

            P cycle | R reset | 0-5 quick profile | E export JSONL
            host={host}
            viewport={state.Viewport.Width}x{state.Viewport.Height}
            mode={(state.InlineMode ? "inline" : "alt")}
            degradation={state.RuntimeStats?.DegradationLevel ?? "FULL"}
            """;

        return ThreeColumn(
            new PanelWidget
            {
                Title = "Capability Matrix",
                Child = new TableWidget
                {
                    Headers = ["Capability", "Detected", "Effective", "Policy"],
                    Rows = rows,
                    SelectedRow = selectedRow
                }
            },
            Panel("Evidence Ledger", evidence),
            Panel("Profile Simulation", simulation));
    }

    private static IWidget BuildMacroRecorder(ShowcaseDemoState state)
    {
        var macro = state.Session.Macro.Macro ?? MacroRecorder.FromEvents("showcase-demo", state.Session.AppliedEvents, "Showcase operator sample");
        if (macro.Events.Count == 0)
        {
            macro = new MacroDefinition(
                "tab-tour",
                DateTimeOffset.UnixEpoch.ToUnixTimeMilliseconds(),
                [
                    new MacroRecordedEvent(180, "Key", "key:Tab"),
                    new MacroRecordedEvent(360, "Key", "key:Tab"),
                    new MacroRecordedEvent(610, "Key", "key:?"),
                    new MacroRecordedEvent(810, "Key", "key:BackTab"),
                    new MacroRecordedEvent(1060, "Key", "key:?")
                ],
                "Preset scenario: Tab Tour");
        }

        var replay = MacroRecorder.ReplayPlan(macro, tickMs: 100);
        var selected = replay.Count == 0 ? 0 : state.ScriptFrame % replay.Count;
        var selectedEvent = replay.Count == 0 ? null : replay[selected];
        var effectiveState = state.Session.Macro with { Macro = macro };
        var progress = macro.Events.Count == 0
            ? 0
            : Math.Min(100, (int)Math.Round((selected + 1) * 100.0 / macro.Events.Count));

        var controls = $"""
            State: {effectiveState.Mode.ToString().ToLowerInvariant()}   Events: {macro.Events.Count}   Duration: {macro.Events.LastOrDefault()?.ScheduledMs ?? 0}ms
            Speed: {effectiveState.Speed:0.00}x   Loop: {(effectiveState.Loop ? "On" : "Off")}   Progress: {progress,3}%   Filtered: 0
            Focus: [Controls] [Timeline] [Scenarios]   Alt+Arrows: Controls/Timeline/Scenarios
            Quick Start: Enter load scenario | Space record | Up/Down scrub timeline
            Space/r record/stop | Enter/p play/pause | Esc stop | l loop | +/- speed
            Status: {effectiveState.Status}
            """;

        var timelineRows = replay
            .Take(10)
            .Select((item, index) => new[]
            {
                index == selected ? ">" : " ",
                $"{index + 1:000}",
                $"+{item.ScheduledMs:0000}ms",
                $"@{item.ScheduledMs:0000}ms",
                item.Display
            })
            .Cast<IReadOnlyList<string>>()
            .ToArray();

        var eventDetail = selectedEvent is null
            ? "Select a scenario or record a macro to see details."
            : $"""
                Selected: #{selected:000}
                Delay: {selectedEvent.ScheduledMs}ms   At: {selectedEvent.ScheduledMs}ms

                Event: {selectedEvent.Display}
                Kind: {selectedEvent.EventType}
                Modifiers: None

                macro_event: playback_tick
                macro_id: {macro.Id}
                """;

        var scenarios = """
            Preset scenarios (Enter to load)

            > Tab Tour - cycle tabs and help
              Search Flow - palette, type, confirm
              Layout Lab - screens and n/p
            """;

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(8), Panel("Macro Recorder", controls)),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(60), new PanelWidget
                        {
                            Title = "Timeline",
                            Child = new TableWidget
                            {
                                Headers = ["", "#", "Delay", "At", "Event"],
                                Rows = timelineRows,
                                SelectedRow = selected
                            }
                        }),
                        (LayoutConstraint.Fill(), new StackWidget(
                            LayoutDirection.Vertical,
                            [
                                (LayoutConstraint.Percentage(65), Panel("Event Detail", eventDetail)),
                                (LayoutConstraint.Fill(), Panel("Scenario Runner", scenarios))
                            ]))
                    ]))
            ]);
    }

    private static IWidget BuildPerformance(ShowcaseDemoState state)
    {
        const int totalItems = 10_000;
        var viewportRows = Math.Max(8, Math.Min(18, state.Viewport.Height - 10));
        var selected = Math.Min(totalItems - 1, (state.ScriptFrame * 37) % totalItems);
        var scrollOffset = Math.Max(0, selected - viewportRows / 2);
        var visibleEnd = Math.Min(totalItems, scrollOffset + viewportRows);
        var rows = Enumerable.Range(scrollOffset, visibleEnd - scrollOffset)
            .Select(index =>
            {
                var severity = (index % 5) switch
                {
                    0 => "INFO",
                    1 => "DEBUG",
                    2 => "WARN",
                    3 => "ERROR",
                    _ => "TRACE"
                };
                var module = (index % 7) switch
                {
                    0 => "server::http",
                    1 => "db::pool",
                    2 => "auth::jwt",
                    3 => "cache::redis",
                    4 => "queue::worker",
                    5 => "api::handler",
                    _ => "core::runtime"
                };
                return new[]
                {
                    index == selected ? ">" : " ",
                    $"{index,5}",
                    severity,
                    module,
                    $"Event #{index:00000}: simulated log entry with payload data"
                };
            })
            .Cast<IReadOnlyList<string>>()
            .ToArray();

        var progress = selected * 100.0 / (totalItems - 1);
        var stats = $"""
            Total items:  {totalItems}
            Selected:     {selected + 1} / {totalItems}
            Scroll:       {scrollOffset}
            Viewport:     {viewportRows} rows
            Visible:      {scrollOffset}..{visibleEnd}
            Progress:     {progress:0.0}%
            Tick:         {state.ScriptFrame}

            Only visible rows are rendered.
            Rendering {visibleEnd - scrollOffset} of {totalItems} items.
            """;

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(68), new PanelWidget
                        {
                            Title = $"Virtualized List ({totalItems} items)",
                            Child = new TableWidget
                            {
                                Headers = ["", "Index", "Level", "Module", "Message"],
                                Rows = rows,
                                SelectedRow = selected - scrollOffset
                            }
                        }),
                        (LayoutConstraint.Fill(), Panel("Performance Stats", stats))
                    ])),
                (LayoutConstraint.Fixed(1), new ParagraphWidget($"Item {selected + 1}/{totalItems} | j/k: scroll | Ctrl+D/U: page | g/G: jump"))
            ]);
    }

    private static IWidget BuildMarkdown(ShowcaseDemoState state)
    {
        const string sampleMarkdown = """
            # GitHub-Flavored Markdown (Rich Demo)

            ## LaTeX + Symbols

            Inline math: $E = mc^2$, $\alpha + \beta = \gamma$.

            ## Admonitions

            > [!NOTE]
            > Information note with **rich emphasis**.

            > [!TIP]
            > Use <kbd>Tab</kbd> and <kbd>Shift+Tab</kbd> to navigate.

            > [!WARNING]
            > Unsafe mode is forbidden in this project.

            ## Task Lists + Links

            - [x] Inline mode + scrollback
            - [x] Deterministic output
            - [ ] Time-travel diff heatmap
            - [ ] Conformal frame-time predictor

            Link: <https://example.com>

            ```rust
            pub enum Strategy { Full, DirtyRows, Redraw }
            ```

            | Feature | Status | Notes |
            |--------|:------:|------:|
            | Inline mode | done | Scrollback preserved |
            | Diff engine | done | SIMD-friendly |
            | Evidence logs | done | JSONL |
            """;

        const string streamingMarkdown = """
            # FrankenTUI Streaming Report

            > [!NOTE]
            > This stream simulates an LLM response rendered incrementally.

            ## TL;DR

            - Zero-flicker rendering via Buffer -> Diff -> Presenter
            - Evidence-ledger decisions for strategy selection
            - Inline mode preserves scrollback
            - 16-byte cells enable SIMD comparisons

            ```json
            { "event": "diff_decision", "strategy": "DirtyRows", "posterior_mean": 0.032 }
            ```

            Space toggles streaming, r restarts, f toggles turbo.
            """;

        var streamChars = Math.Min(streamingMarkdown.Length, 72 + state.ScriptFrame * 81);
        var streamFragment = streamingMarkdown[..streamChars];
        var complete = streamChars == streamingMarkdown.Length;
        var progress = streamChars * 100.0 / streamingMarkdown.Length;
        var detection = $"""
            Detection: 7 indicators | {(complete ? "Confident" : "Likely")}
            Confidence: {(complete ? 100 : Math.Min(95, 35 + state.ScriptFrame * 8))}%
            Chars: {streamChars}/{streamingMarkdown.Length}
            Space: play/pause | r: restart | f: turbo | Up/Down: scroll stream
            """;

        var styleSampler = """
            Bold  Dim  Italic  Underline
            Strikethrough  Reverse  Blink
            Dbl-Underline  Curly-Underline  [Hidden]

            Error  Success  Warning  Link  Code
            """;

        IReadOnlyList<IReadOnlyList<string>> unicodeRows =
        [
            ["Hello", "ASCII", "5"],
            ["ni hao", "CJK", "8"],
            ["konnichiwa", "Hiragana", "10"],
            ["alpha beta", "Greek", "10"],
            ["arrows", "Symbols", "7"],
            ["blocks", "Block el.", "4"]
        ];

        return new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Percentage(35), new PanelWidget
                {
                    Title = "Markdown Renderer",
                    Child = new ParagraphWidget(string.Empty)
                    {
                        Document = MarkdownDocumentBuilder.ParseCached(sampleMarkdown),
                        RenderOptions = new TextRenderOptions(TextWrapMode.Word)
                    }
                }),
                (LayoutConstraint.Percentage(35), new StackWidget(
                    LayoutDirection.Vertical,
                    [
                        (LayoutConstraint.Fill(), new PanelWidget
                        {
                            Title = $"LLM Streaming Simulation | {(complete ? "Complete" : $"Streaming... {progress:0}%")}",
                            Child = new ParagraphWidget(string.Empty)
                            {
                                Document = MarkdownDocumentBuilder.ParseCached(streamFragment),
                                RenderOptions = new TextRenderOptions(TextWrapMode.Word)
                            }
                        }),
                        (LayoutConstraint.Fixed(5), Panel("Markdown Detection", detection))
                    ])),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Vertical,
                    [
                        (LayoutConstraint.Fixed(8), Panel("Style Sampler", styleSampler)),
                        (LayoutConstraint.Fixed(10), new PanelWidget
                        {
                            Title = "Unicode Showcase",
                            Child = new TableWidget
                            {
                                Headers = ["Text", "Type", "Cells"],
                                Rows = unicodeRows
                            }
                        }),
                        (LayoutConstraint.Fill(), Panel(
                            "Wrap: Word | Align: Left",
                            "w: cycle wrap | a: cycle alignment\n\nThe quick brown fox jumps over the lazy dog. Supercalifragilisticexpialidocious tests character-level wrapping behavior."))
                    ]))
            ]);
    }

    private static IWidget BuildMermaid(ShowcaseDemoState state) =>
        MermaidShowcaseSurface.CreateWidget(MermaidShowcaseSurface.BuildState(state.Session));

    private static IWidget BuildMermaidMega(ShowcaseDemoState state)
    {
        var mermaidState = MermaidShowcaseSurface.BuildState(state.Session);
        var catalog = MermaidShowcaseSurface.Catalog();
        var selected = Math.Clamp(state.Session.Mermaid.SelectedSampleIndex, 0, catalog.Count - 1);
        var categories = catalog
            .GroupBy(static sample => sample.Category, StringComparer.Ordinal)
            .Select(static group => $"{group.Key}:{group.Count()}")
            .OrderBy(static item => item, StringComparer.Ordinal)
            .ToArray();

        var libraryRows = catalog
            .Take(8)
            .Select((sample, index) => new[]
            {
                index == selected ? ">" : " ",
                sample.Name,
                sample.Category,
                sample.ComplexityHint,
                $"{sample.NodeCount}/{sample.EdgeCount}"
            })
            .Cast<IReadOnlyList<string>>()
            .ToArray();

        var controlText = $"""
            Mode: normal | Render: adaptive | Palette: default
            Sample filter: all ({catalog.Count} samples)
            Categories: {string.Join(", ", categories.Take(5))}
            Selected: {mermaidState.Sample.Name}
            Node navigation: h/j/k/l or arrows
            Inspect/search: / query | n/N match | Enter inspect
            Viewport: zoom=100% pan=0,0 override=off
            Debounce: 50ms | Layout budget: 16ms
            Keymap: m metrics | c controls | i status | p palette | g guard
            """;

        var detailText = $"""
            Node Detail
            selected_node: 0
            incoming_edges: 0
            outgoing_edges: {mermaidState.Diagram.Edges.Count}
            clusters: 0

            Recompute
            event=mermaid_mega_recompute
            parse_ms={mermaidState.Summary.ParseMs:0.00}
            layout_ms={mermaidState.Summary.LayoutMs:0.00}
            render_ms={mermaidState.Summary.RenderMs:0.00}
            objective={mermaidState.Summary.ObjectiveScore:0.00}
            warnings={mermaidState.Diagnostics.Count(static item => item.Severity is MermaidDiagnosticSeverity.Warn)}
            errors={mermaidState.Diagnostics.Count(static item => item.Severity is MermaidDiagnosticSeverity.Error)}
            """;

        return new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Percentage(58), MermaidShowcaseSurface.CreateWidget(mermaidState)),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Vertical,
                    [
                        (LayoutConstraint.Fixed(11), new PanelWidget
                        {
                            Title = "Mega Sample Library",
                            Child = new TableWidget
                            {
                                Headers = ["", "Sample", "Family", "Tier", "N/E"],
                                Rows = libraryRows,
                                SelectedRow = Math.Min(selected, libraryRows.Length - 1)
                            }
                        }),
                        (LayoutConstraint.Fixed(12), Panel("Mega Controls", controlText)),
                        (LayoutConstraint.Fill(), Panel("Node Detail / Recompute", detailText))
                    ]))
            ]);
    }

    private static IWidget BuildVisualEffects(ShowcaseDemoState state)
    {
        var effect = NormalizeVfxEffectLabel(state.VfxEffect);
        return TwoColumn(
            new PanelWidget
            {
                Title = ShowcaseVfxEffects.DisplayName(effect),
                Child = new DeterministicVfxCanvasWidget(state.ScriptFrame, effect)
            },
            Panel(
                "Harness",
                $"Effect: {effect}\nLabel: {ShowcaseVfxEffects.DisplayName(effect)}\nRenderer: {ShowcaseVfxEffects.RendererName(effect)}\nMode: braille\n{ShowcaseVfxEffects.Description(effect)}"));
    }

    private static IWidget BuildResponsive(ShowcaseDemoState state)
    {
        var width = state.Viewport.Width;
        var breakpoint = width switch
        {
            < 60 => "XS (<60)",
            < 90 => "SM (60-89)",
            < 120 => "MD (90-119)",
            < 160 => "LG (120-159)",
            _ => "XL (160+)"
        };
        var columns = width < 90 ? 1 : width < 120 ? 2 : 3;
        var sidebarVisible = width >= 90;
        var asideVisible = width >= 120;
        var padding = width < 60 ? 1 : width < 90 ? 2 : width < 120 ? 3 : 4;
        var style = width < 60 ? "compact" : width < 90 ? "normal" : width < 120 ? "comfortable" : "spacious";
        var thresholds = "sm>=60 md>=90 lg>=120 xl>=160";

        var indicator = $"Breakpoint: {breakpoint} | Width: {width} | Thresholds: {thresholds}";
        var sidebar = $"""
            Breakpoint: {breakpoint}
            Layout: {(columns == 1 ? "stacked" : columns == 2 ? "2-col" : "3-col")}

            Sidebar: {(sidebarVisible ? "visible" : "hidden")} (md+)
            Aside:   {(asideVisible ? "visible" : "hidden")} (lg+)

            [b] Toggle BPs
            [Current: default]
            """;
        var content = $"""
            Columns: {columns} | Breakpoint: {breakpoint} | {state.Viewport.Width}x{state.Viewport.Height}

            Padding: {padding} | Style: {style}

            The layout adapts to the terminal width.
            Resize your terminal to see the layout switch between 1-column,
            2-column, and 3-column modes.

            Controls: b custom breakpoints | +/- simulated width | click indicator | scroll width
            """;
        var aside = $"""
            Only visible at Lg+ ({breakpoint}).

            Tick: {state.ScriptFrame}
            Custom thresholds: sm>=50 md>=80 lg>=110 xl>=110+
            Mouse: left toggles, right resets, wheel adjusts width.
            """;

        var body = columns switch
        {
            1 => new StackWidget(
                LayoutDirection.Vertical,
                [
                    (LayoutConstraint.Fixed(7), Panel("Layout Info", content)),
                    (LayoutConstraint.Fixed(6), Panel("Visibility", $"Sidebar: {(sidebarVisible ? "visible" : "hidden")} (md+)\nAside:   {(asideVisible ? "visible" : "hidden")} (lg+)")),
                    (LayoutConstraint.Fill(), Panel("Responsive Values", $"Padding: {padding}\nStyle: {style}"))
                ]),
            2 => new StackWidget(
                LayoutDirection.Horizontal,
                [
                    (LayoutConstraint.Fixed(28), Panel("Sidebar", sidebar)),
                    (LayoutConstraint.Fill(), Panel("Content", content))
                ]),
            _ => new StackWidget(
                LayoutDirection.Horizontal,
                [
                    (LayoutConstraint.Fixed(28), Panel("Sidebar", sidebar)),
                    (LayoutConstraint.Fill(), Panel("Content", content)),
                    (LayoutConstraint.Fixed(24), Panel("Aside", aside))
                ])
        };

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(1), new ParagraphWidget(indicator)),
                (LayoutConstraint.Fill(), body)
            ]);
    }

    private static IWidget BuildLogSearch(ShowcaseDemoState state)
    {
        var searchState = state.Session.LogSearch with
        {
            Query = string.IsNullOrWhiteSpace(state.Session.LogSearch.Query) ? "info" : state.Session.LogSearch.Query,
            SearchOpen = true
        };
        var result = LogSearchEngine.Apply(LogLines, searchState);
        var liveStream = $"""
            Max lines: 5000
            Burst: every 3 ticks / 2 lines
            Follow mode: on
            Paused: false
            Generated: {LogLines.Count + (state.ScriptFrame % 3) * 2}
            Query: {searchState.Query} | Matches: {result.MatchCount} | Tier: {result.Tier.ToString().ToLowerInvariant()}
            """;
        var controls = """
            / open search | Enter submit | Esc close
            n/N next/prev match | f filter
            Ctrl+C case sensitivity
            Ctrl+X context lines
            Up/Down scroll | Home/End jump
            FTUI_LOGSEARCH_DETERMINISTIC=true
            """;
        var diagnosticsRows = new[]
        {
            new[] { "search_opened", "query", searchState.Query },
            new[] { "query_updated", "result_count", result.MatchCount.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            new[] { "filter_applied", "mode", searchState.RegexMode ? "regex" : "literal" },
            new[] { "filter_cleared", "match_position", "0" },
            new[] { "match_navigation", "direction", "next" },
            new[] { "scroll_navigation", "offset", "0" },
            new[] { "pause_toggle", "paused", "false" },
            new[] { "log_generated", "burst_size", "2" },
            new[] { "mode_change", "filter", "off" },
            new[] { "tick", "checksum", "stable" }
        };

        return new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Percentage(55), new LogSearchWidget
                {
                    State = searchState,
                    SourceLines = LogLines
                }),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Vertical,
                    [
                        (LayoutConstraint.Fixed(8), Panel("Live Stream", liveStream)),
                        (LayoutConstraint.Fixed(8), Panel("Search Controls", controls)),
                        (LayoutConstraint.Fill(), new PanelWidget
                        {
                            Title = "Diagnostics",
                            Child = new StackWidget(
                                LayoutDirection.Vertical,
                                [
                                    (LayoutConstraint.Fixed(2), new ParagraphWidget("FTUI_LOGSEARCH_DIAGNOSTICS=true\nJSONL fields: seq timestamp_us query result_count match_position checksum")),
                                    (LayoutConstraint.Fill(), new TableWidget
                                    {
                                        Headers = ["Event", "Field", "Value"],
                                        Rows = diagnosticsRows,
                                        SelectedRow = state.ScriptFrame % diagnosticsRows.Length
                                    })
                                ])
                        })
                    ]))
            ]);
    }

    private static IWidget BuildNotifications(ShowcaseDemoState state)
    {
        var visible = Math.Min(4, 1 + state.ScriptFrame % 4);
        var pending = Math.Max(0, NotificationLines.Count - visible);
        var totalShown = 12 + state.ScriptFrame;
        var toastRows = new[]
        {
            new[] { "Urgent", "Critical Alert", "Ack / Snooze", "persistent" },
            new[] { "High", "Error", "Retry", "8s" },
            new[] { "Normal", "Operation completed", "-", "5s" },
            new[] { "Normal", "Low disk space", "-", "6s" },
            new[] { "Low", "Update available", "-", "4s" }
        };
        var instructions = $"""
            Press keys to trigger notifications:

              s  Success notification
              e  Error with Retry action
              w  Warning notification
              i  Info notification
              u  Urgent with Ack/Snooze actions
              d  Dismiss all notifications

            Queue: {visible} visible, {pending} pending
            Config: max_visible=4 max_queued=20
            Position: TopRight
            Total shown: {totalShown}
            Last action: {(state.ScriptFrame % 5 == 0 ? "retry" : "(none)")}

            Mouse: click instructions to trigger by row
            Click stack dismisses all | Scroll pushes info/success
            Tick: queue expiry + promotion at 100ms
            """;
        var lifecycle = """
            Lifecycle
            push -> display -> auto-dismiss
            manual dismiss -> promote pending
            action invocation -> last_action

            Toast styles
            Success | Error | Warning | Info | Urgent

            Priority order
            Urgent > High > Normal > Low
            """;

        return new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Percentage(40), Panel("Notification Demo", instructions)),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Vertical,
                    [
                        (LayoutConstraint.Fixed(11), new PanelWidget
                        {
                            Title = "Notification Stack",
                            Child = new TableWidget
                            {
                                Headers = ["Priority", "Toast", "Actions", "TTL"],
                                Rows = toastRows,
                                SelectedRow = state.ScriptFrame % toastRows.Length
                            }
                        }),
                        (LayoutConstraint.Fill(), Panel("Toast Queue Lifecycle", lifecycle))
                    ]))
            ]);
    }

    private static IWidget BuildActionTimeline(ShowcaseDemoState state)
    {
        var selected = state.ScriptFrame % 8;
        var rows = Enumerable.Range(0, 8)
            .Select(index =>
            {
                var tick = Math.Max(0, state.ScriptFrame * 2 - 7 + index);
                var severity = new[] { "TRACE", "DEBUG", "INFO", "WARN", "ERROR" }[tick % 5];
                var component = new[] { "core", "runtime", "render", "widgets" }[(tick / 2) % 4];
                var kind = new[] { "input", "cmd", "sub", "render", "present", "caps", "budget" }[(tick / 3) % 7];
                var summary = kind switch
                {
                    "input" => "Key event processed",
                    "cmd" => "Command dispatched to model",
                    "sub" => "Subscription tick delivered",
                    "render" => "Frame diff computed",
                    "present" => "Presenter emitted ANSI batch",
                    "caps" => "Capability probe updated",
                    _ => "Render budget degraded"
                };
                return new[] { tick.ToString(System.Globalization.CultureInfo.InvariantCulture), severity, component, kind, summary };
            })
            .Cast<IReadOnlyList<string>>()
            .ToArray();
        var selectedRow = rows[Math.Min(selected, rows.Length - 1)];
        var details = $"""
            ID: {100 + state.ScriptFrame}
            Tick: {selectedRow[0]}
            Severity: {selectedRow[1]}
            Component: {selectedRow[2]}
            Type: {selectedRow[3]}

            Summary:
              {selectedRow[4]}

            Fields:
              latency_ms: {2 + (state.ScriptFrame % 7) * 3}
              diff_cells: {(state.ScriptFrame * 13) % 120}
              ansi_bytes: {(state.ScriptFrame * 47) % 2048}

            Evidence:
              {(selectedRow[3] == "caps" ? "evidence: env + probe signal" : selectedRow[3] == "budget" ? "budget: frame_time > p95" : "decision: follow guard")}
            """;
        var controls = """
            Follow[F]: ON  Component[C]: all  Severity[S]: all  Type[T]: all  Clear[X]
            Max events: 500 | Burst: every 2 ticks | Initial events: 12
            Enter toggles detail expansion | Up/Down or j/k navigate
            PgUp/PgDn page | Home/End jump | Click select | Scroll navigate

            Diagnostic spans:
            action_timeline::new
            action_timeline::update
            action_timeline::tick
            action_timeline::filter_change
            action_timeline::follow_change
            action_timeline::buffer_eviction
            RUST_LOG=ftui_demo_showcase::screens::action_timeline=debug
            """;

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(14), Panel("Filters + Follow", controls)),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(62), new PanelWidget
                        {
                            Title = "Event Timeline",
                            Child = new TableWidget
                            {
                                Headers = ["Tick", "Severity", "Component", "Type", "Summary"],
                                Rows = rows,
                                SelectedRow = selected
                            }
                        }),
                        (LayoutConstraint.Fill(), Panel("Event Detail", details))
                    ]))
            ]);
    }

    private static IWidget BuildIntrinsicSizing(ShowcaseDemoState state)
    {
        var scenario = state.ScriptFrame % 4;
        var effectiveWidth = state.Viewport.Width switch
        {
            < 70 => 50,
            < 110 => 80,
            _ => 120
        };
        var scenarioRows = new[]
        {
            new[] { "1", "Adaptive Sidebar", "icon-only <60, full labels >=60" },
            new[] { "2", "Flexible Cards", "stacked <60, side-by-side >=60" },
            new[] { "3", "Auto-Sizing Table", "ID/status/score fixed, name fills" },
            new[] { "4", "Responsive Form", "one column <70, two columns >=70" }
        };
        var activeScenario = scenarioRows[scenario];
        var scenarioDetail = scenario switch
        {
            0 => $"""
                Adaptive Sidebar
                Effective width: {effectiveWidth}
                Sidebar: {(effectiveWidth < 60 ? "icon-only 4 cols" : "full labels 20 cols")}

                Menu
                  Dashboard
                  Settings
                  Help
                  Notifications
                  Profile
                """,
            1 => """
                Flexible Cards
                User Info: Name Alice | Role Admin | Team Platform
                Stats: Requests 1,234 | Latency 42ms
                Uptime 99.9% | Cache 847 hits

                Layout switches from stacked to horizontal at width 60.
                """,
            2 => """
                Auto-Sizing Table
                ID: fixed 6 | Status: fixed 12 | Score: fixed 10
                Name: remaining width, minimum 10

                Rows include Bob (Long Name Here) to force content measurement.
                """,
            _ => $"""
                Responsive Form
                Effective width: {effectiveWidth}
                Layout: {(effectiveWidth >= 70 ? "wide 2 columns" : "narrow 1 column")}

                Fields: Name, Email, Phone, Location, Department, Role
                """
        };
        var controls = """
            Intrinsic Sizing Demo
            1-4 switch scenario | Left/Right or n/p cycle
            w cycle width preset: 50 -> 80 -> 120 -> auto
            +/- adjust simulated width by 10 | r reset
            Click/Scroll content cycles scenario

            Embedded Pane Studio
            visible when content >=72x12
            Drag panes | Right click mode | Wheel magnetism
            """;

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(4), new PanelWidget
                {
                    Title = "Intrinsic Sizing Demo",
                    Child = new ParagraphWidget($"Scenario: {activeScenario[1]} ({scenario + 1}/4) | Effective width: {effectiveWidth} | Terminal: {state.Viewport.Width}x{state.Viewport.Height}")
                }),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(38), new PanelWidget
                        {
                            Title = "Scenarios",
                            Child = new TableWidget
                            {
                                Headers = ["#", "Scenario", "Intrinsic rule"],
                                Rows = scenarioRows,
                                SelectedRow = scenario
                            }
                        }),
                        (LayoutConstraint.Percentage(34), Panel(activeScenario[1], scenarioDetail)),
                        (LayoutConstraint.Fill(), Panel("Controls + Pane Studio", controls))
                    ]))
            ]);
    }

    private static IWidget BuildLayoutInspector(ShowcaseDemoState state)
    {
        var scenario = state.ScriptFrame % 3;
        var step = state.ScriptFrame % 3;
        var scenarios = new[]
        {
            new[] { "Flex Trio", "Vertical flex: Fixed + Min + Max" },
            new[] { "Tight Grid", "2x2 grid with intentional constraint pressure" },
            new[] { "FitContent Clamp", "FitContent bounded by min/max" }
        };
        var steps = new[] { "Constraints", "Allocation", "Final" };
        IReadOnlyList<IReadOnlyList<string>> records = scenario switch
        {
            0 =>
            [
                new[] { "FlexRoot", "0x0", $"{state.Viewport.Width}x{Math.Max(state.Viewport.Height - 3, 0)}", "OK" },
                new[] { "  Fixed", "0x3", $"{state.Viewport.Width / 2}x3", "OK" },
                new[] { "  Min", "0x4", $"{state.Viewport.Width / 2}x8", "OK" },
                new[] { "  Max", "0x6", $"{state.Viewport.Width / 2}x6", "OK" }
            ],
            1 =>
            [
                new[] { "GridRoot", "0x0", "48x16", "OK" },
                new[] { "  Cell A", "min 0 max 22", "24x7", "OVER" },
                new[] { "  Cell B", "min 26 max 0", "24x7", "UNDER" },
                new[] { "  Cell C", "min 0 max 22", "24x7", "OVER" },
                new[] { "  Cell D", "min 26 max 0", "24x7", "UNDER" }
            ],
            _ =>
            [
                new[] { "FitRoot", "0x0", "50x12", "OK" },
                new[] { "  FitContent", "min 12 max 18", "18x12", "OK" },
                new[] { "  Min", "min 10 max 0", "30x12", "OK" }
            ]
        };
        var inspector = $"""
            Scenario: {scenarios[scenario][0]}
            Details: {scenarios[scenario][1]}
            Step: {steps[step]}
            Overlay: on   Tree: on

            Keys: n/p scenario  [/] step  o overlay  t tree  r reset
            Mouse: click info cycles scenario | click viz steps | right click toggles overlay
            Pane rail: Drag panes | Right click mode | Wheel magnetism
            """;
        var overlay = $"""
            Constraint Overlay
            Step {steps[step]} style:
              show_borders={(step == 0 ? "false" : "true")}
              show_size_diff={(step == 2 ? "false" : "true")}
              show_labels=true

            Scenario blocks:
              {scenarios[scenario][0]}
              requested outlines vs computed rects
              overflow/underflow flags
            """;

        return new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Percentage(34), Panel("Layout Inspector", inspector)),
                (LayoutConstraint.Percentage(40), new StackWidget(
                    LayoutDirection.Vertical,
                    [
                        (LayoutConstraint.Fixed(10), Panel("Constraint Overlay", overlay)),
                        (LayoutConstraint.Fill(), new PanelWidget
                        {
                            Title = "Layout Tree",
                            Child = new TableWidget
                            {
                                Headers = ["Widget", "Requested", "Received", "Status"],
                                Rows = records,
                                SelectedRow = Math.Min(step + 1, records.Count - 1)
                            }
                        })
                    ])),
                (LayoutConstraint.Fill(), Panel(
                    "Pane Studio",
                    "Embedded workspace visible at wide sizes.\n\nFlexRoot / GridRoot / FitRoot records are compared against requested and received rects.\n\nDrag panes | Right click mode | Wheel magnetism\n\nConstraintOverlay and LayoutDebugger parity hooks remain tracked under 364-DEM."))
            ]);
    }

    private static IWidget BuildAdvancedTextEditor(ShowcaseDemoState state)
    {
        var sample = """
            Welcome to the Advanced Text Editor!

            This is a demonstration of FrankenTUI's text editing capabilities.
            You can edit text, select regions, search, and replace.

            Features:
            - Multi-line editing with line numbers
            - Selection with Shift+Arrow keys
            - Undo (Ctrl+Z) and Redo (Ctrl+Y)
            - Search (Ctrl+F) with next/prev match
            - Replace (Ctrl+H) single or all matches

            Unicode support: emoji, CJK, accented text
            """;
        var searchPanel = """
            Search / Replace
            Search: editor
            Replace: buffer
            2/5 | Enter: Next | Shift+Enter: Prev
            Ctrl+R: Replace | Ctrl+A: Replace all

            Focus: editor -> search -> replace
            Ctrl+Left/Right, Tab/Shift+Tab cycle focus
            Esc closes search or clears selection
            """;
        var history = """
            Undo History
            Undo (3)
              text_edited insert "buffer"
              replace_performed editor->buffer
              selection_cleared

            Redo (1)
              redo_performed replace

            Ctrl+U: Toggle history panel
            Limit: 64 entries
            """;
        var diagnosticsRows = new[]
        {
            new[] { "search_opened", "focus", "search" },
            new[] { "query_updated", "match_count", "5" },
            new[] { "match_navigation", "direction", "next" },
            new[] { "replace_performed", "replacement", "buffer" },
            new[] { "replace_all_performed", "replace_count", "5" },
            new[] { "undo_performed", "undo_depth", "3" },
            new[] { "redo_performed", "redo_depth", "1" },
            new[] { "text_edited", "text_len", "420" },
            new[] { "focus_changed", "focus", "replace" },
            new[] { "history_panel_toggled", "panel_visible", "true" }
        };
        var diagnostics = new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(4), new ParagraphWidget("FTUI_TEXTEDITOR_DIAGNOSTICS=true\nFTUI_TEXTEDITOR_DETERMINISTIC=true\nJSONL fields: seq ts_us kind query replacement checksum\nEvents include search_opened replace_all_performed history_panel_toggled")),
                (LayoutConstraint.Fill(), new TableWidget
                {
                    Headers = ["Event", "Field", "Value"],
                    Rows = diagnosticsRows,
                    SelectedRow = state.ScriptFrame % diagnosticsRows.Length
                })
            ]);

        return new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Percentage(50), new TextAreaWidget
                {
                    Document = TextDocument.FromString(sample),
                    Cursor = new TextCursor(8, 25),
                    HasFocus = true,
                    StatusText = "Ln 9, Col 26 | Match 2/5 | Undo:3 Redo:1 | Ctrl+U: Show history"
                }),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Vertical,
                    [
                        (LayoutConstraint.Fixed(10), Panel("Search / Replace", searchPanel)),
                        (LayoutConstraint.Fixed(11), Panel("Undo History", history)),
                        (LayoutConstraint.Fill(), new PanelWidget
                        {
                            Title = "Diagnostics",
                            Child = diagnostics
                        })
                    ]))
            ]);
    }

    private static IWidget BuildMousePlayground(ShowcaseDemoState state)
    {
        var selected = state.ScriptFrame % 12;
        var targets = Enumerable.Range(1, 12)
            .Select(index => new[]
            {
                index == selected + 1 ? ">" : " ",
                $"T{index}",
                $"id={index}",
                index == selected + 1 ? "hover" : "idle",
                index == 3 ? "2" : index == 7 ? "1" : "0"
            })
            .Cast<IReadOnlyList<string>>()
            .ToArray();
        var eventRows = new[]
        {
            new[] { "mouse_down", "Left Down", "12,5", "target=3" },
            new[] { "mouse_up", "Left Up", "12,5", "target=3" },
            new[] { "mouse_drag", "Left Drag", "18,8", "target=4" },
            new[] { "mouse_move", "Move", "25,9", "target=7" },
            new[] { "mouse_scroll", "Scroll Down", "40,12", "target=none" },
            new[] { "hit_test", "Hit Test", "25,9", "target=7" },
            new[] { "hover_change", "Hover", "25,9", "prev=3 curr=7" },
            new[] { "target_click", "Click", "12,5", "clicks=2" },
            new[] { "overlay_toggle", "Overlay", "-", "enabled=true" },
            new[] { "jitter_stats_toggle", "Jitter", "-", "enabled=true" }
        };
        var controls = """
            Tab cycles focus: Targets -> Event Log -> Stats
            Arrow keys / hjkl navigate targets
            Space/Enter clicks focused target
            Home/g first target | End/G last target
            O toggle overlay | J toggle jitter stats | C clear log

            FTUI_MOUSE_DIAGNOSTICS=true
            FTUI_MOUSE_DETERMINISTIC=true
            JSONL: seq ts_us kind tick x y target_id prev_target_id checksum
            Events include hover_change target_click jitter_stats_toggle
            Telemetry hooks: on_hit_test on_hover_change on_target_click on_any
            """;
        var stats = $"""
            Hover: T{selected + 1}
            Pos: ({10 + selected}, {4 + selected % 6})
            Overlay: ON
            Jitter Stats: ON
            Grid: 4 cols x 3 rows
            Event log: max 12

            HoverStabilizerConfig: default
            Hit regions registered as Content ids 1..12
            Overlay draws crosshair at mouse position
            """;

        return new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Percentage(42), new PanelWidget
                {
                    Title = "Hit-Test Targets",
                    Child = new TableWidget
                    {
                        Headers = ["", "Target", "HitId", "State", "Clicks"],
                        Rows = targets,
                        SelectedRow = selected
                    }
                }),
                (LayoutConstraint.Percentage(34), new StackWidget(
                    LayoutDirection.Vertical,
                    [
                        (LayoutConstraint.Fixed(13), new PanelWidget
                        {
                            Title = "Event Log",
                            Child = new TableWidget
                            {
                                Headers = ["Kind", "Event", "Pos", "Context"],
                                Rows = eventRows,
                                SelectedRow = state.ScriptFrame % eventRows.Length
                            }
                        }),
                        (LayoutConstraint.Fill(), Panel("Stats + Overlay", stats))
                    ])),
                (LayoutConstraint.Fill(), Panel("Controls + Diagnostics", controls))
            ]);
    }

    private static IWidget BuildFormValidation(ShowcaseDemoState state)
    {
        var fields =
            new[]
            {
                new FormTextField("username", "Username", "ab", "Username must be at least 3 characters"),
                new FormTextField("email", "Email", "not-an-email", "Please enter a valid email address"),
                new FormTextField("password", "Password", "short", "Password must be at least 8 characters"),
                new FormTextField("confirm", "Confirm Password", "different", "Passwords do not match"),
                new FormTextField("age", "Age", "25", "13..120"),
                new FormTextField("bio", "Bio", new string('x', 110), "Bio must be 100 characters or less"),
                new FormTextField("website", "Website", "not-a-url", "Website must start with http:// or https://"),
                new FormTextField("role", "Role", "(Select a role)", "Please select a role"),
                new FormTextField("terms", "Accept Terms", "false", "You must accept the terms")
            };
        var validators = new Dictionary<string, IReadOnlyList<TextValidator>>(StringComparer.Ordinal)
        {
            ["username"] = [ValidationRules.Required(), ValidationRules.MinLength(3)],
            ["email"] = [ValidationRules.Required()],
            ["password"] = [ValidationRules.Required(), ValidationRules.MinLength(8)],
            ["confirm"] = [ValidationRules.Required()],
            ["age"] = [ValidationRules.ContainsDigit()],
            ["bio"] = [field => field.Value.Length <= 100 ? [] : [new ValidationMessage("max-length", field.Id, "Bio must be 100 characters or less.")]],
            ["website"] = [ValidationRules.Required()],
            ["role"] = [ValidationRules.Required()],
            ["terms"] = [ValidationRules.Required()]
        };
        var validation = FormValidator.Validate(fields, validators);
        var selected = state.ScriptFrame % fields.Length;
        var formRows = fields
            .Select((field, index) => new[]
            {
                index == selected ? ">" : "",
                field.Label,
                field.Value.Length > 24 ? field.Value[..21] + "..." : field.Value,
                validation.ForField(field.Id).Count == 0 ? "ok" : "error"
            })
            .ToArray();
        var errorRows = validation.Messages
            .Select(error => new[]
            {
                fields.First(field => field.Id == error.FieldId).Label,
                error.Message
            })
            .ToArray();

        var left = new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(3), Panel(
                    "Mode",
                    "Mode: Real-time [M to toggle]\nStatus: Error injection active")),
                (LayoutConstraint.Fill(), new PanelWidget
                {
                    Title = "Registration Form",
                    Child = new TableWidget
                    {
                        Headers = ["", "Field", "Value", "State"],
                        Rows = formRows,
                        SelectedRow = selected
                    }
                }),
                (LayoutConstraint.Fixed(4), Panel(
                    "Touched / Dirty",
                    $"Touched: 9/9 | Dirty: 8/9\nSubmitted: false | Tick: {state.ScriptFrame}"))
            ]);
        var center = new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Percentage(58), new PanelWidget
                {
                    Title = "Error Summary",
                    Child = new TableWidget
                    {
                        Headers = ["Field", "Message"],
                        Rows = errorRows,
                        SelectedRow = selected % Math.Max(errorRows.Length, 1)
                    }
                }),
                (LayoutConstraint.Fill(), Panel(
                    "Validation Rules",
                    "Username: required, min 3\nEmail: required, contains @ and .\nPassword: required, min 8\nConfirm Password: match password\nAge: bounded 13..120\nBio: max 100 characters\nWebsite: http:// or https://\nRole: not placeholder\nAccept Terms: checked"))
            ]);
        var right = new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(9), Panel(
                    "Controls",
                    "Tab/S-Tab: navigate fields\nUp/Down: change value / navigate\nSpace: toggle checkbox\nEnter: submit form\nM: Toggle validation mode\nE: Inject errors\nR: Reset form\nC: Clear errors")),
                (LayoutConstraint.Fixed(7), Panel(
                    "Notifications",
                    "QueueConfig: max_visible=3 max_queued=10\nPosition: TopRight\nSuccess toast: Registration successful!\nError toast: Validation Failed\nPriority: High on submit errors")),
                (LayoutConstraint.Fill(), Panel(
                    "Mouse + Diagnostics",
                    "Click error panel: toggle mode\nScroll form: navigate focused field\nEvents: mode_toggled, form_submitted, errors_injected, errors_cleared\nState hooks: touched_fields, dirty_fields, focused\nValidationMode: Real-time | On Submit"))
            ]);

        return new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Percentage(42), left),
                (LayoutConstraint.Percentage(34), center),
                (LayoutConstraint.Fill(), right)
            ]);
    }

    private static IWidget BuildVirtualizedSearch(ShowcaseDemoState state)
    {
        const int totalItems = 10_000;
        const string query = "cfg";
        var selected = state.ScriptFrame % 12;
        var results = Enumerable.Range(0, 12)
            .Select(index =>
            {
                var itemIndex = index * 48;
                var component = (index % 5) switch
                {
                    0 => "CoreService",
                    1 => "ApiGateway",
                    2 => "WorkerPool",
                    3 => "CacheManager",
                    _ => "EventBus"
                };
                var action = (index % 4) switch
                {
                    0 => "initialized",
                    1 => "validated",
                    2 => "cached",
                    _ => "completed"
                };
                var score = 61 - (index * 3);
                return new[]
                {
                    index == selected ? ">" : "",
                    $"[{itemIndex:D5}] Configuration :: {component} {action} - payload_{itemIndex % 1000:D3}",
                    $"{score}",
                    "0,7,13"
                };
            })
            .ToArray();
        var stats = new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(12), Panel(
                    "Stats",
                    $"Total:    {totalItems} items\nMatches:  1250\nSelected: {selected + 1}\nQuery:    \"{query}\"\nTop score: 61\n\nKeybindings:\n  /        Focus search\n  Esc      Clear search\n  j/k      Navigate\n  g/G      Top/Bottom")),
                (LayoutConstraint.Fill(), Panel(
                    "Diagnostics",
                    "FTUI_VSEARCH_DIAGNOSTICS=true\nFTUI_VSEARCH_DETERMINISTIC=true\nDataset: Configuration/CoreService/ApiGateway/WorkerPool\nEvents: query_change, filter_update, navigate, focus_change, page_scroll, jump_to_edge, fuzzy_match, render, tick\nJSONL fields: seq, ts_us, kind, query, filtered_count, selected, scroll_offset, focus_search, direction, match_score, checksum\nTelemetryHooks: on_query_change, on_filter_update, on_navigate, on_any"))
            ]);

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(3), Panel(
                    "Search (/ to focus, Esc to clear)",
                    $"Type to search...  query=\"{query}\"  focus=List")),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(70), new PanelWidget
                        {
                            Title = $"Results (1250 of {totalItems} match)",
                            Child = new TableWidget
                            {
                                Headers = ["", "Item", "Score", "MatchPos"],
                                Rows = results,
                                SelectedRow = selected
                            }
                        }),
                        (LayoutConstraint.Fill(), stats)
                    ]))
            ]);
    }

    private static IWidget BuildAsyncTasks(ShowcaseDemoState state)
    {
        var selected = state.ScriptFrame % 8;
        string[][] tasks =
        [
            [selected == 0 ? ">" : "", "Running", "M", "Initial Setup", "64%", "30", "19"],
            [selected == 1 ? ">" : "", "Running", "L", "Data Sync", "38%", "50", "19"],
            [selected == 2 ? ">" : "", "Queued", "H", "Cache Warm", "0%", "20", "0"],
            [selected == 3 ? ">" : "", "Queued", "H", "Async Build #1", "0%", "27", "0"],
            [selected == 4 ? ">" : "", "Done", "M", "Quick Test #2", "100%", "34", "34"],
            [selected == 5 ? ">" : "", "Failed", "L", "Full Deploy #7", "100%", "69", "69"],
            [selected == 6 ? ">" : "", "Canceled", "M", "Batch Backup #8", "42%", "76", "32"],
            [selected == 7 ? ">" : "", "Queued", "H", "Parallel Index #9", "0%", "23", "0"]
        ];
        var header = Panel(
            "Scheduler",
            "Q:3 R:2 D:1 F:1 | SRPT[Shortest Remaining Time] | Aging:on | max_concurrent=3\nInvariant: bounded_concurrency");
        var queue = new PanelWidget
        {
            Title = "Task Queue",
            Child = new TableWidget
            {
                Headers = ["", "State", "Pr", "Task", "Prog", "Est", "Elap"],
                Rows = tasks,
                SelectedRow = selected
            }
        };
        var selectedTask = tasks[selected];
        var details = Panel(
            "Task Details",
            $"ID: {selected + 1}\nName: {selectedTask[3]}\nState: {selectedTask[1]}\nPriority: {selectedTask[2]}\nProgress: {selectedTask[4]}\nElapsed: {selectedTask[6]} ticks\nEstimated: {selectedTask[5]} ticks\nError: Simulated failure");
        var activity = Panel(
            "Activity",
            "Spawned: Initial Setup\nStarted: Initial Setup\nStarted: Data Sync\nScheduler: SRPT\nAging: ON\nCanceled: Batch Backup #8\nRetrying: Full Deploy #7");
        var evidence = Panel(
            "Policy + Evidence",
            "Policies: FIFO, SJF, SRPT, Smith, Priority, RoundRobin\nAging formula: effective_priority = priority + aging_factor * wait_time\nInvariants: bounded_concurrency, bounded_progress, terminal_stability, monotonic_ids, bounded_wait\nMetrics: tasks_scheduled, tasks_completed, mean_wait, mean_completion, max_wait, aging_boosts_applied");
        var hazard = Panel(
            "Hazard + Diagnostics",
            "Hazard: base=0.001 factor=0.1 exponent=2.0 threshold=1.0\nDecision: loss_continue vs loss_cancel, bayes_factor, recommend_cancel\nJSONL: state_transition, scheduling_decision, policy_change, aging_toggle, invariant_check, starvation_warning, metrics_snapshot, cancellation_decision\nMouse: Click selects task row; Wheel scrolls task list");

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(4), header),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(60), queue),
                        (LayoutConstraint.Fill(), new StackWidget(
                            LayoutDirection.Vertical,
                            [
                                (LayoutConstraint.Percentage(36), details),
                                (LayoutConstraint.Percentage(26), activity),
                                (LayoutConstraint.Percentage(19), evidence),
                                (LayoutConstraint.Fill(), hazard)
                            ]))
                    ])),
                (LayoutConstraint.Fixed(1), new ParagraphWidget("n:spawn c:cancel s:scheduler a:aging | bounded_concurrency scheduling_decision cancellation_decision"))
            ]);
    }

    private static IWidget BuildThemeStudio(ShowcaseDemoState state)
    {
        var presetIndex = state.ScriptFrame % 5;
        string[][] presets =
        [
            [presetIndex == 0 ? ">" : "o", "Cyberpunk Aurora", "current"],
            [presetIndex == 1 ? ">" : "", "Darcula", "preset"],
            [presetIndex == 2 ? ">" : "", "Solar", "preset"],
            [presetIndex == 3 ? ">" : "", "Nord", "preset"],
            [presetIndex == 4 ? ">" : "", "High Contrast", "preset"]
        ];
        string[][] tokens =
        [
            [">", "fg::PRIMARY", "Foreground", "#F8FAFC", "13.6:1", "AAA"],
            ["", "fg::SECONDARY", "Foreground", "#CBD5E1", "9.7:1", "AAA"],
            ["", "fg::MUTED", "Foreground", "#94A3B8", "5.8:1", "AA"],
            ["", "bg::BASE", "Background", "#0F172A", "1.0:1", "Fail"],
            ["", "bg::SURFACE", "Background", "#1E293B", "1.4:1", "Fail"],
            ["", "accent::PRIMARY", "Accent", "#22D3EE", "8.4:1", "AAA"],
            ["", "accent::SUCCESS", "Accent", "#22C55E", "6.9:1", "AA"],
            ["", "accent::WARNING", "Accent", "#F59E0B", "8.9:1", "AAA"],
            ["", "accent::ERROR", "Accent", "#EF4444", "4.6:1", "AA"],
            ["", "StatusInProgress", "Status", "#38BDF8", "7.9:1", "AAA"],
            ["", "PriorityP0", "Priority", "#F43F5E", "4.5:1", "AA"],
            ["", "PriorityP4", "Priority", "#64748B", "3.9:1", "AA Large"]
        ];
        var inspector = new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Percentage(52), new PanelWidget
                {
                    Title = "Token Inspector",
                    Child = new TableWidget
                    {
                        Headers = ["", "Token", "Category", "Hex", "Contrast", "WCAG"],
                        Rows = tokens,
                        SelectedRow = 0
                    }
                }),
                (LayoutConstraint.Percentage(24), Panel(
                    "Export",
                    "Press E to export theme\nJSON keys: bg_base, bg_surface, fg_primary, fg_secondary, accent_primary, accent_secondary, accent_success, accent_warning, accent_error\nGhostty: background, foreground, selection-background, palette=0..7")),
                (LayoutConstraint.Fill(), Panel(
                    "Diagnostics + Telemetry",
                    "FTUI_THEME_STUDIO_DIAGNOSTICS=true\nFTUI_THEME_STUDIO_DETERMINISTIC=true\nEvents: focus_changed, preset_changed, token_changed, theme_applied, theme_cycled, theme_exported, tick\nJSONL fields: seq, ts_us, kind, focus, preset, preset_index, token, token_index, export_bytes, checksum\nTelemetryHooks: on_focus_change, on_preset_change, on_token_change, on_theme_applied, on_theme_cycled, on_theme_exported, on_any"))
            ]);

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(25), new PanelWidget
                        {
                            Title = "Presets",
                            Child = new TableWidget
                            {
                                Headers = ["", "Theme", "State"],
                                Rows = presets,
                                SelectedRow = presetIndex
                            }
                        }),
                        (LayoutConstraint.Fill(), inspector)
                    ])),
                (LayoutConstraint.Fixed(1), new ParagraphWidget("Presets: Cyberpunk Aurora, Darcula, Solar, Nord, High Contrast | Ctrl+T cycle | e JSON | E Ghostty"))
            ]);
    }

    private static IWidget BuildSnapshotPlayer(ShowcaseDemoState state)
    {
        const int frameCount = 50;
        var current = state.ScriptFrame % frameCount;
        var timeline = new string('=', Math.Max(1, current / 2)).PadRight(25, '.');
        string[][] previewRows =
        [
            ["Frame", $"{current + 1}/{frameCount}", "Pattern", "Time Travel Mode"],
            ["Delta", "42 cells", "Render", $"{100 + current * 10}us"],
            ["Checksum", $"0x{0x8f34ab1200000000UL + (ulong)current:X16}", "Chain", "verified"],
            ["Marker", current % 7 == 0 ? "yes" : "no", "State", "Paused"]
        ];
        string[][] compareRows =
        [
            ["A", "frame 1", "checksum", "0x8F34AB1200000000"],
            ["B", "frame 2", "checksum", "0x8F34AB1200000001"],
            ["Diff", "37 cells", "content", "24"],
            ["Style", "13", "Heatmap", "Overlay"]
        ];
        var left = new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(4), Panel(
                    $"Timeline ({current + 1}/{frameCount})",
                    $"{timeline}\nMarkers: frame 1, 8, 16 | Click timeline / Drag timeline scrubs frames")),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(50), new PanelWidget
                        {
                            Title = "Frame Preview",
                            Child = new TableWidget
                            {
                                Headers = ["Metric", "Value", "Field", "Data"],
                                Rows = previewRows
                            }
                        }),
                        (LayoutConstraint.Fill(), new PanelWidget
                        {
                            Title = "Frame A/B Compare",
                            Child = new TableWidget
                            {
                                Headers = ["Slot", "Frame", "Field", "Value"],
                                Rows = compareRows
                            }
                        })
                    ]))
            ]);
        var right = new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Percentage(45), Panel(
                    "Frame Info",
                    $"Status: Paused\nFrame: {current + 1}/{frameCount}\nSize: 40x15\nChanges: 42 cells\nMemory: 28800 bytes\nChecksum: {0x8f34ab1200000000UL + (ulong)current:X16}\nChain hash: 0x0F0E0D0C0B0A0908\nMarkers: 3\nView: A/B Compare\nHeatmap: Overlay")),
                (LayoutConstraint.Percentage(28), Panel(
                    "Controls",
                    "Space: Play/Pause\nLeft/Right or h/l: Step frame\nHome/End or g/G: First/Last\nM: Toggle marker\nR: Toggle record\nC: Clear\nD: Diagnostics\nV: Toggle compare view\nA/B: Pin compare A/B\nX: Swap A/B\nH: Heatmap overlay\nE: Export JSONL report")),
                (LayoutConstraint.Fill(), Panel(
                    "Diagnostics + Export",
                    "Events: nav, playback, record, marker, clear\nJSONL report: time_travel_report\nFields: seq, action, from, to, frame, changes, checksum, chain, diff_cells, diff_pct, content_diff, style_diff\nInvariants: playback determinism, progress bounds, checksum integrity, memory budget\nMouse: Right-click timeline toggles marker; Right-click preview toggles heatmap"))
            ]);

        return new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Percentage(60), left),
                (LayoutConstraint.Fill(), right)
            ]);
    }

    private static IWidget BuildPerformanceChallenge(ShowcaseDemoState state)
    {
        var frameMs = state.RuntimeStats?.FrameDurationMs ?? 12.8;
        var observedFps = frameMs > 0 ? 1000.0 / frameMs : 60.0;
        var stressLevel = (state.ScriptFrame % 12) * 8;
        var tier = observedFps >= 50
            ? "Full Fidelity"
            : observedFps >= 20
                ? "Reduced (no FX)"
                : observedFps >= 5
                    ? "Minimal"
                    : "SAFETY MODE";
        string[][] samples =
        [
            ["avg", $"{frameMs:0.00}ms", "p50", "14.20ms"],
            ["p95", "18.70ms", "p99", "31.40ms"],
            ["min", "8.10ms", "max", "42.80ms"],
            ["views", $"{state.RuntimeStats?.StepIndex ?? state.ScriptFrame}", "samples", "120"],
            ["V/Tick", "1.30", "Tick Rate", "60.0 tps"]
        ];
        string[][] tiers =
        [
            [tier == "Full Fidelity" ? ">" : "", "Full", ">=50fps", "full fidelity"],
            [tier.StartsWith("Reduced", StringComparison.Ordinal) ? ">" : "", "Reduced", "20-49fps", "no FX"],
            [tier == "Minimal" ? ">" : "", "Minimal", "5-19fps", "minimal rendering"],
            [tier == "SAFETY MODE" ? ">" : "", "Safety", "<5fps", "text only"]
        ];
        var metrics = new PanelWidget
        {
            Title = "Real-Time Metrics",
            Child = new TableWidget
            {
                Headers = ["Metric", "Value", "Metric", "Value"],
                Rows = samples
            }
        };
        var sparkline = Panel(
            "Tick Intervals (us)",
            "Sparkline: ▁▂▃▄▅▆▇█▆▄▂\nMode: intervals | alternate: FPS Estimate\nRing buffer: 120 samples\nScroll sparkline: FPS / intervals\nDeterministic: FTUI_DEMO_PERF_HUD_VIEWS_PER_TICK");
        var budget = new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Percentage(42), Panel(
                    "Render Budget",
                    $"Budget: 16.67ms (60fps target)\nObserved: {frameMs:0.00}ms avg\nSimulated: {frameMs + 12.5:0.00}ms (+12.5ms)\nUsage: {(frameMs / 16.67 * 100):0}%\nTier: {tier}\nBudget bar: ####|....")),
                (LayoutConstraint.Percentage(28), Panel(
                    "Stress Harness",
                    $"Mode: Ramp | Load {stressLevel}% | +{stressLevel * 2.0:0.0}ms\nDecision: degrade\nStates: Off, Ramp, Peak, Cool\ns:stress | c:cool | Click stress section")),
                (LayoutConstraint.Fill(), new PanelWidget
                {
                    Title = "Degradation Tiers",
                    Child = new TableWidget
                    {
                        Headers = ["", "Tier", "Threshold", "Response"],
                        Rows = tiers
                    }
                })
            ]);
        var evidence = Panel(
            "JSONL + Mouse Evidence",
            "Logger: perf_challenge\nEvent: perf_challenge_tier_change\nFields: tier_from, tier_to, frame_time_ms, penalty_ms, stress_level, stress_mode, decision, outcome\nKeys: r reset, p pause, m mode, s stress, c cool, 1-4 force tier\nMouse: click tier rows, click stress, scroll budget, scroll sparkline");

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(1), new ParagraphWidget("PERFORMANCE CHALLENGE MODE - DEGRADATION TIERS")),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Fixed(34), metrics),
                        (LayoutConstraint.Fill(), new StackWidget(
                            LayoutDirection.Vertical,
                            [
                                (LayoutConstraint.Percentage(52), sparkline),
                                (LayoutConstraint.Fill(), evidence)
                            ])),
                        (LayoutConstraint.Fixed(42), budget)
                    ])),
                (LayoutConstraint.Fixed(1), new ParagraphWidget($"s:stress(Ramp) | c:cool | r:reset | p:pause | m:mode(intervals) | 1-4:tier({tier}) | budget:17ms | samples:120/120"))
            ]);
    }

    private static IWidget BuildExplainability(ShowcaseDemoState state)
    {
        var stats = state.RuntimeStats;
        var changedCells = stats?.ChangedCells ?? 0;
        var bytes = stats?.BytesEmitted ?? 0;
        var diffMs = stats?.DiffDurationMs ?? 0;
        var presentMs = stats?.PresentDurationMs ?? 0;
        var frame = state.RuntimeStats?.StepIndex ?? state.ScriptFrame;
        var sampleIndex = Math.Abs(frame);
        var strategy = changedCells > 500 ? "dirty" : "full";
        var regime = sampleIndex % 3 == 0 ? "burst" : sampleIndex % 3 == 1 ? "settle" : "steady";
        var action = regime == "burst" ? "coalesce" : "apply";
        var decision = presentMs + diffMs > 12.0 ? "degrade" : "hold";
        var eValue = Math.Clamp((diffMs + presentMs) / 16.7, 0.05, 4.0);

        var diff = Panel(
            "Diff Strategy",
            $"Decision: {strategy}\n" +
            $"Why: strategy {strategy}; guard_reason=none; fallback_reason=none\n" +
            $"Posterior: mu=0.{(sampleIndex % 7) + 31:00} sigma2=0.12 alpha=1.20 beta=2.30\n" +
            $"Dirty rows: {Math.Max(1, changedCells / 80)}/24 ({Math.Min(99, changedCells / 20):0}%)\n" +
            $"Hysteresis: applied (1.10x)\n" +
            $"Coverage: dirty_tile_ratio=0.07 dirty_cell_ratio=0.08\n" +
            $"JSONL: event=diff_decision event_idx={frame} strategy posterior_mean posterior_variance alpha beta dirty_rows total_rows");

        var resize = Panel(
            "Resize Regime (BOCPD)",
            $"Decision: {action} ({regime})\n" +
            $"Why: burst regime; dt_ms=5.0; event_rate=20.0/s\n" +
            $"Evidence: log_bayes_factor=1.23 (regime 0.50, timing 0.30, rate 0.20)\n" +
            $"Render gap: {presentMs:0.0}ms\n" +
            $"Forced apply: false\n" +
            "JSONL: event=decision_evidence event=decision regime action time_since_render_ms forced");

        var budget = Panel(
            "Budget Decisions",
            $"Decision: {decision}\n" +
            $"Frame: {(diffMs + presentMs):0.00}ms / 16.00ms\n" +
            $"E-value: {eValue:0.000}\n" +
            "Phase: steady\n" +
            "Conformal: alpha=0.05 q=1.0 upper=17.0ms (safe)\n" +
            $"Controller: {decision}\n" +
            "JSONL: event=budget_decision frame_idx decision_controller degradation_before degradation_after frame_time_us budget_us risk");

        var timeline = Panel(
            "Decision Timeline",
            $"diff   #{frame,3} strategy {strategy} | mu=0.{(sampleIndex % 7) + 31:00} sigma2=0.12\n" +
            $"resize #{frame + 1,3} {action} {regime} | LBF=1.23\n" +
            $"budget #{frame + 2,3} {decision} budget | e={eValue:0.00}\n" +
            "scroll: n/p or Up/Down; click panels to focus; mouse wheel over timeline");

        var source = Panel(
            "Source + Controls",
            "Explainability Cockpit | source: (disabled)\n" +
            "Evidence source disabled; enable evidence logging to populate this cockpit.\n" +
            "Set FTUI_DEMO_EVIDENCE_JSONL or FTUI_HARNESS_EVIDENCE_JSONL to a writable path.\n" +
            "Refresh every 5 ticks unless paused; max evidence lines=400; max timeline rows=10.\n" +
            "r refresh | Space pause/resume | c clear+re-read | 1/2/3/4 focus panels | n/p scroll timeline");

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(2), new ParagraphWidget("Explainability Cockpit | source: (disabled)\nLoaded deterministic sample evidence for diff, resize, budget, and timeline panels")),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(34), diff),
                        (LayoutConstraint.Percentage(33), resize),
                        (LayoutConstraint.Percentage(33), budget)
                    ])),
                (LayoutConstraint.Fixed(7), timeline),
                (LayoutConstraint.Fixed(7), source)
            ]);
    }

    private static IWidget BuildI18n(ShowcaseDemoState state)
    {
        var locale = string.IsNullOrWhiteSpace(state.Language) ? "en" : state.Language;
        var localeBar = "[English]  Espanol  Francais  Русский  العربية  日本語";
        var overview = Panel(
            "String Lookup",
            "--- Internationalization ---\n\n" +
            "Hello!  Welcome, Alice!\n" +
            $"Locale: {locale} (English)\n" +
            "Direction: Left-to-Right\n" +
            "Flow: Ltr\n\n" +
            "Coverage Report\n" +
            "Total keys: 6 | Locales: en es fr ru ar ja\n" +
            "Fallback chain: en");

        var plurals = Panel(
            "Pluralization Rules",
            "--- Pluralization Demo (count = 1) ---\n\n" +
            "English (en): items: 1 item | files: 1 file\n" +
            "Spanish (es): items: 1 elemento | files: 1 archivo\n" +
            "Russian (ru): one/few/many forms for 1, 3, 5, 21\n" +
            "Arabic (ar): zero/one/two/few/many/other categories\n\n" +
            "Use Up/Down or mouse wheel to change count");

        var rtl = Panel(
            "RTL Layout Mirroring",
            "Flex children reverse in RTL flow.\n\n" +
            "LTR sample: [first] [second] [third]\n" +
            "RTL sample: [third] [second] [first]\n\n" +
            "Arabic greeting: مرحبا بالعالم\n" +
            "Hebrew greeting: שלום עולם\n" +
            "Mixed: مرحبا world 123\n" +
            "D toggles RTL/LTR locale; click locale bar left/right half");

        var stress = Panel(
            "Stress Lab",
            "Sample sets: Combining Marks | CJK Width | RTL Text | Emoji & ZWJ\n" +
            "combining_e_acute: école | display_width=5 | grapheme_count=5\n" +
            "cjk_hello: 你好世界 | display_width=8\n" +
            "zwj_astronaut: 👩‍🚀 🚀 | grapheme_width=2\n" +
            "flag: 🇺🇸 | expected_width=2\n" +
            "truncate_to_width_with_info max_width=32 ellipsis=...\n" +
            "JSONL: event=i18n_stress_report set_id sample_id width_metrics truncation_state outcome");

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(3), new ParagraphWidget(
                    "i18n Stress Lab\n" +
                    $"{localeBar}")),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(50), new StackWidget(
                            LayoutDirection.Vertical,
                            [
                                (LayoutConstraint.Percentage(50), overview),
                                (LayoutConstraint.Percentage(50), plurals)
                            ])),
                        (LayoutConstraint.Percentage(50), new StackWidget(
                            LayoutDirection.Vertical,
                            [
                                (LayoutConstraint.Percentage(50), rtl),
                                (LayoutConstraint.Percentage(50), stress)
                            ]))
                    ])),
                (LayoutConstraint.Fixed(1), new ParagraphWidget("Left/Right locale | Shift+Left/Right grapheme cursor | [/] sample set | E export report | Tab/1-4 panel | R reset"))
            ]);
    }

    private static IWidget BuildVoiOverlay(ShowcaseDemoState state)
    {
        var tick = state.RuntimeStats?.StepIndex ?? state.ScriptFrame;
        var shouldSample = tick % 3 != 0;
        var alpha = 2.0 + (tick % 5) * 0.25;
        var beta = 3.0 + (tick % 7) * 0.20;
        var mean = alpha / (alpha + beta);
        var variance = (alpha * beta) / (Math.Pow(alpha + beta, 2.0) * (alpha + beta + 1.0));
        var expectedAfter = variance * 0.72;
        var gain = Math.Max(0.0, variance - expectedAfter);

        var decision = Panel(
            "Decision",
            $"event_idx: {tick}\n" +
            $"should_sample: {shouldSample.ToString().ToLowerInvariant()}\n" +
            $"reason: {(shouldSample ? "voi_gain_exceeds_cost" : "blocked_by_min_interval")}\n" +
            $"score: {gain + 0.10:0.000} | cost: 0.100\n" +
            "log_bayes_factor: 1.20\n" +
            "e_value: 1.10 | e_threshold: 2.00 | boundary_score: 0.70");

        var posterior = Panel(
            "Posterior",
            $"alpha: {alpha:0.00} | beta: {beta:0.00}\n" +
            $"mean: {mean:0.000} | variance: {variance:0.000}\n" +
            $"expected_variance_after: {expectedAfter:0.000}\n" +
            $"voi_gain: {gain:0.000}\n" +
            "InlineAutoRemeasureConfig.voi enable_logging=true max_log_entries=96");

        var observation = Panel(
            "Observation",
            $"sample_idx: {Math.Max(0, tick / 2)}\n" +
            $"violated: {(tick % 17 < 3).ToString().ToLowerInvariant()}\n" +
            $"posterior_mean: {mean + 0.03:0.000}\n" +
            $"alpha: {alpha + 1.0:0.00} | beta: {beta:0.00}\n" +
            "source: runtime:inline-auto or demo:fallback");

        var ledger = Panel(
            "VOI Ledger",
            $"Decision  #{tick,3} sample={shouldSample.ToString().ToLowerInvariant()} voi_gain={gain:0.000} LBF=1.20\n" +
            $"Observation #{Math.Max(0, tick / 2),3} violated={(tick % 17 < 3).ToString().ToLowerInvariant()} posterior_mean={mean + 0.03:0.000}\n" +
            $"Decision  #{tick + 1,3} sample=true voi_gain={gain + 0.010:0.000} LBF=1.35\n" +
            "Ledger entries map VoiLogEntry::Decision and VoiLogEntry::Observation");

        var controls = Panel(
            "Overlay Controls",
            "VOI Overlay | src: inline-auto|fallback | focus: Decision/Posterior/Observation/Ledger\n" +
            "Centered overlay area clamps to terminal size; style uses rounded border and deep background.\n" +
            "Tab cycle section | v toggle detail | n/p or Up/Down navigate ledger | r reset sampler | Esc clear focus\n" +
            "Mouse: click section to focus; wheel over ledger changes selected_ledger_idx; click outside clears focus\n" +
            "Expanded hint: ledger[index] | click section to focus | Esc to clear");

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(1), new ParagraphWidget("VOI Overlay | Galaxy-Brain sampler debug overlay | source: demo:fallback")),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(34), new StackWidget(
                            LayoutDirection.Vertical,
                            [
                                (LayoutConstraint.Percentage(50), decision),
                                (LayoutConstraint.Percentage(50), posterior)
                            ])),
                        (LayoutConstraint.Percentage(33), new StackWidget(
                            LayoutDirection.Vertical,
                            [
                                (LayoutConstraint.Percentage(50), observation),
                                (LayoutConstraint.Percentage(50), ledger)
                            ])),
                        (LayoutConstraint.Percentage(33), controls)
                    ])),
                (LayoutConstraint.Fixed(1), new ParagraphWidget("r reset | v detail | n/p ledger | Tab section | mouse click/scroll hit regions | inline_auto_voi_snapshot fallback"))
            ]);
    }

    private static IWidget BuildInlineModeStory(ShowcaseDemoState state)
    {
        var tick = state.RuntimeStats?.StepIndex ?? state.ScriptFrame;
        var rate = tick % 4 switch
        {
            0 => 1,
            1 => 2,
            2 => 5,
            _ => 10
        };
        var uiHeight = 1 + Math.Abs(tick % 4);
        var linesGenerated = 60 + Math.Max(0, tick * rate);
        var anchor = tick % 2 == 0 ? "Bottom" : "Top";

        var header = new ParagraphWidget(
            $"Mode: Inline | Compare: OFF | Anchor: {anchor} | UI height: {uiHeight} | Rate: {rate}/tick\n" +
            $"Status: Live | Lines: {linesGenerated} | Scrollback preserved in inline mode");

        var inlinePane = Panel(
            "Inline Mode Story",
            "000060 [INFO ] core    scrollback ok\n" +
            "000061 [WARN ] render  inline anchor\n" +
            "000062 [ERROR] runtime budget check\n" +
            "000063 [DEBUG] widgets diff pass\n\n" +
            $"INLINE MODE - SCROLLBACK PRESERVED\nAnchor: {anchor.ToUpperInvariant()} | UI height: {uiHeight} | Log rate: {rate}/tick\n" +
            "Logs stream underneath while the stable chrome bar remains anchored.");

        var altPane = Panel(
            "Alt-screen Story",
            "ALT-SCREEN MODE - SCROLLBACK HIDDEN\n" +
            "Full-screen takeover (logs do not persist)\n\n" +
            "Compare mode renders Inline (scrollback preserved) beside Alt-screen (scrollback hidden).\n" +
            "Clicking the alt header drills into alt-screen mode when comparing.");

        var controls = Panel(
            "Controls + Mouse",
            "Space pause/resume stream\n" +
            "A toggle chrome anchor (top/bottom)\n" +
            "C toggle inline vs alt comparison\n" +
            "D reset story to defaults\n" +
            "H cycle UI height | M toggle single view mode | R cycle log rate | T scrollback stress burst\n" +
            "Mouse: click header=compare, click bar=anchor, click log=pause, wheel=log rate");

        var statePanel = Panel(
            "State + Limits",
            "MAX_LOG_LINES=2000 | INITIAL_LOG_LINES=60\n" +
            "LOG_RATE_OPTIONS=[1,2,5,10]\n" +
            "UI_HEIGHT_OPTIONS=[1,2,3,4]\n" +
            "Levels: INFO WARN ERROR DEBUG\n" +
            "Modules: core render runtime widgets io layout\n" +
            "Events: diff pass, present frame, flush writer, resize coalesce, cursor sync, scrollback ok, inline anchor, budget check\n" +
            "Layout rects: header, content, inline_bar, alt_header");

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(2), header),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(36), inlinePane),
                        (LayoutConstraint.Percentage(32), altPane),
                        (LayoutConstraint.Percentage(32), new StackWidget(
                            LayoutDirection.Vertical,
                            [
                                (LayoutConstraint.Percentage(50), controls),
                                (LayoutConstraint.Percentage(50), statePanel)
                            ]))
                    ])),
                (LayoutConstraint.Fixed(1), new ParagraphWidget("Space pause | A anchor | C compare | D defaults | H height | M mode | R rate | T burst | mouse hit regions preserve scrollback"))
            ]);
    }

    private static IWidget BuildAccessibility(ShowcaseDemoState state)
    {
        var tick = state.RuntimeStats?.StepIndex ?? state.ScriptFrame;
        var highContrast = tick % 2 == 0;
        var reducedMotion = tick % 3 == 0;
        var largeText = tick % 4 == 0;

        var overview = Panel(
            "Accessibility Control Panel",
            "Active Theme: current_theme\n" +
            "Base Theme: CyberpunkAurora\n" +
            $"Mode: {(highContrast ? "High Contrast" : "Standard")}\n" +
            $"Motion: {(reducedMotion ? "Reduced (0.0x)" : "Full (1.0x)")}\n" +
            $"Large Text: {(largeText ? "ON" : "OFF")}\n" +
            "Shortcuts: h = contrast, m = motion, l = large text");

        var toggles = Panel(
            "Toggles",
            $"[h] High Contrast: {(highContrast ? "ON" : "OFF")}\n" +
            $"[m] Reduced Motion: {(reducedMotion ? "ON" : "OFF")}\n" +
            $"[l] Large Text: {(largeText ? "ON" : "OFF")}\n" +
            "Shift+A opens the compact overlay\n" +
            "Click rows dispatch A11yToggleAction: HighContrast, ReducedMotion, LargeText");

        var wcag = Panel(
            "WCAG Contrast",
            "Primary on Base     12.4:1 AAA\n" +
            "Secondary on Base    8.8:1 AAA\n" +
            "Accent Primary       5.7:1 AA\n" +
            "Accent Warning       4.8:1 AA\n" +
            "Accent Error         3.4:1 AA Large\n\n" +
            "Minimum ratio: 3.4:1 AA Large\n" +
            "AA >= 4.5, AAA >= 7.0, Large Text >= 3.0");

        var preview = Panel(
            "Live Preview",
            "Preview text\n" +
            "The quick brown fox jumps over the lazy dog.\n" +
            "Links look like this and code looks like fn main()\n" +
            "Status: OK  Error\n" +
            $"{(reducedMotion ? "Animations paused" : "Animations active")}\n" +
            "theme::apply_large_text adjusts label/key styles");

        var telemetry = Panel(
            "A11y Telemetry",
            $"[{tick,4}] Panel | HC:{(highContrast ? "ON" : "OFF")} RM:{(reducedMotion ? "ON" : "OFF")} LT:{(largeText ? "ON" : "OFF")}\n" +
            $"[{tick + 1,4}] High Contrast | HC:ON RM:{(reducedMotion ? "ON" : "OFF")} LT:{(largeText ? "ON" : "OFF")}\n" +
            $"[{tick + 2,4}] Reduced Motion | HC:{(highContrast ? "ON" : "OFF")} RM:ON LT:{(largeText ? "ON" : "OFF")}\n" +
            "A11yEventKind: Panel, HighContrast, ReducedMotion, LargeText\n" +
            "A11yTelemetryEvent carries tick, high_contrast, reduced_motion, large_text");

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(7), overview),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(50), new StackWidget(
                            LayoutDirection.Vertical,
                            [
                                (LayoutConstraint.Fixed(8), toggles),
                                (LayoutConstraint.Fill(), preview)
                            ])),
                        (LayoutConstraint.Percentage(50), new StackWidget(
                            LayoutDirection.Vertical,
                            [
                                (LayoutConstraint.Fixed(10), wcag),
                                (LayoutConstraint.Fill(), telemetry)
                            ]))
                    ])),
                (LayoutConstraint.Fixed(1), new ParagraphWidget("h contrast | m motion | l large text | Shift+A overlay | Ctrl+T theme | Click toggle setting | layout_toggles hit rows"))
            ]);
    }

    private static IWidget BuildWidgetBuilder(ShowcaseDemoState state)
    {
        var tick = state.RuntimeStats?.StepIndex ?? state.ScriptFrame;
        var activePreset = Math.Abs(tick) % 3;
        var presetName = activePreset switch
        {
            0 => "Starter Kit",
            1 => "Status Wall",
            _ => "Minimal"
        };
        var selected = Math.Abs(tick) % 4;
        var value = 40 + (Math.Abs(tick) % 12) * 5;

        var header = new ParagraphWidget(
            $"Preset: {presetName} | Widgets: 4 | [P] cycle [S] save [X] export\n" +
            "Widget Builder Sandbox | deterministic presets, editable props, preview, and JSONL export");

        var presets = Panel(
            "Presets",
            $"{(activePreset == 0 ? "> " : "  ")}Starter Kit\n" +
            $"{(activePreset == 1 ? "> " : "  ")}Status Wall\n" +
            $"{(activePreset == 2 ? "> " : "  ")}Minimal\n" +
            "  Custom 1*\n\n" +
            "Right-click presets saves current as Custom N\n" +
            "FTUI_WIDGET_BUILDER_EXPORT_PATH controls export destination");

        var tree = Panel(
            "Widget Tree",
            $"{(selected == 0 ? "> " : "  ")}01. Paragraph [on]\n" +
            $"{(selected == 1 ? "> " : "  ")}02. List [on]\n" +
            $"{(selected == 2 ? "> " : "  ")}03. Progress [on]\n" +
            $"{(selected == 3 ? "> " : "  ")}04. Sparkline [on]\n" +
            "05. Badge [off]\n\n" +
            "WidgetKind ids: paragraph, list, progress, sparkline, badge");

        var preview = Panel(
            "Live Preview",
            "Intro Paragraph: Compose widgets, tweak props, and observe layout changes.\n" +
            "Checklist: * Wireframe | Implement | Polish | Ship\n" +
            $"Build Progress: {Math.Min(100, value)}%\n" +
            "Throughput Sparkline: 3 5 2 6 7 4 8 6 5 7 3 6\n" +
            "Status Badge: ACTIVE/STANDBY\n" +
            "Selected widget uses accent border and theme::panel_border_style");

        var props = Panel(
            "Props",
            $"Selected: {(selected == 0 ? "Paragraph" : selected == 1 ? "List" : selected == 2 ? "Progress" : "Sparkline")} (#{selected + 1})\n" +
            "Enabled: on (E)\n" +
            "Border: on (B)\n" +
            "Title: on (T)\n" +
            $"Accent: {(selected % 6) + 1} (C)\n" +
            $"Value: {Math.Min(100, value)} ([ / ])\n" +
            "J/K or Up/Down select widget | P/Shift+P cycle presets | R reset");

        var export = Panel(
            "Export + Mouse",
            "event=widget_builder_export\n" +
            "run_id=FTUI_WIDGET_BUILDER_RUN_ID | preset_id | preset_name | preset_index\n" +
            "widget_count | props_hash | preset.widgets[] | outcome=ok\n" +
            "WidgetSnapshot: kind,title,enabled,bordered,show_title,accent_idx,value\n" +
            "Mouse: click preset loads, click tree selects, click preview toggles enabled\n" +
            "Scroll navigates lists; right-click presets saves, right-click tree toggles border");

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(2), header),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Fixed(30), new StackWidget(
                            LayoutDirection.Vertical,
                            [
                                (LayoutConstraint.Percentage(45), presets),
                                (LayoutConstraint.Percentage(55), tree)
                            ])),
                        (LayoutConstraint.Fill(), preview),
                        (LayoutConstraint.Fixed(48), new StackWidget(
                            LayoutDirection.Vertical,
                            [
                                (LayoutConstraint.Percentage(48), props),
                                (LayoutConstraint.Percentage(52), export)
                            ]))
                    ])),
                (LayoutConstraint.Fixed(1), new ParagraphWidget("J/K select | P presets | S save | X export JSONL | E/B/T/C props | [/] value | Click/Scroll/Right-click mouse routing"))
            ]);
    }

    private static IWidget BuildCommandPaletteLab(ShowcaseDemoState state)
    {
        var palette = state.Session.CommandPalette;
        var labQuery = ResolvePaletteLabQuery(state, palette);
        var labPalette = palette with
        {
            IsOpen = true,
            Query = labQuery,
            SelectedIndex = palette.SelectedIndex
        };
        var results = FilterPaletteLabResultsForDemo(
            CommandPaletteController.Results(labPalette, ShowcaseCommandPalette.EvidenceLabEntries()),
            state.PaletteLabMatchFilter);
        var selectedIndex = results.Count == 0 ? -1 : Math.Clamp(labPalette.SelectedIndex, 0, results.Count - 1);
        var selected = selectedIndex >= 0 ? results[selectedIndex] : null;

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed((ushort)2), new ParagraphWidget(
                    $"Match Mode: 0 All 1 Exact 2 Prefix 3 WordStart 4 Substring 5 Fuzzy | active={state.PaletteLabMatchFilter}\n" +
                    $"Type to filter | Up/Down navigate | Enter execute | b bench {(state.PaletteLabBenchEnabled ? "ON" : "OFF")} {state.PaletteLabBenchFrame:000}/{state.PaletteLabBenchProcessed:000} '{labQuery}' | m cycle")),
                (LayoutConstraint.Fill(), PaletteLabColumns(
                    new CommandPaletteWidget
                    {
                        Query = labPalette.Query,
                        Results = results,
                        SelectedIndex = Math.Max(selectedIndex, 0),
                        ShowPreview = false
                    },
                    BuildPaletteEvidenceLabPanel(state, labQuery, results, selected, BuildPaletteLabHintLedger())))
            ]);
    }

    internal static IReadOnlyList<HintRankingEvidence> BuildPaletteLabHintLedger()
    {
        var ranker = new HintRanker();
        var openId = ranker.Register("Ctrl+P Open Palette", 14.0, new HintContext.Global(), 1);
        var execId = ranker.Register("Enter Execute", 10.0, new HintContext.Global(), 2);
        var navId = ranker.Register("Up/Down Navigate", 10.0, new HintContext.Global(), 3);
        var benchId = ranker.Register("b Toggle Bench", 12.0, new HintContext.Global(), 4);
        var modeId = ranker.Register("0-5 Match Filter", 14.0, new HintContext.Global(), 5);

        for (var index = 0; index < 6; index++)
        {
            ranker.RecordUsage(openId);
        }

        for (var index = 0; index < 4; index++)
        {
            ranker.RecordUsage(execId);
        }

        for (var index = 0; index < 3; index++)
        {
            ranker.RecordUsage(navId);
        }

        for (var index = 0; index < 2; index++)
        {
            ranker.RecordUsage(modeId);
        }

        ranker.RecordShownNotUsed(benchId);
        return ranker.Rank().Ledger;
    }

    internal static string ResolvePaletteLabQuery(ShowcaseDemoState state, CommandPaletteState palette)
    {
        if (!string.IsNullOrWhiteSpace(palette.Query))
        {
            return palette.Query;
        }

        if (!state.PaletteLabBenchEnabled)
        {
            return "log";
        }

        var index = Math.Abs(state.PaletteLabBenchProcessed) % PaletteLabBenchQueries.Count;
        return PaletteLabBenchQueries[index];
    }

    internal static IReadOnlyList<CommandPaletteSearchResult> FilterPaletteLabResultsForDemo(
        IReadOnlyList<CommandPaletteSearchResult> results,
        ShowcasePaletteLabMatchFilter filter) =>
        filter switch
        {
            ShowcasePaletteLabMatchFilter.Exact => results.Where(static result => result.MatchKind == CommandPaletteMatchKind.Exact).ToArray(),
            ShowcasePaletteLabMatchFilter.Prefix => results.Where(static result => result.MatchKind == CommandPaletteMatchKind.Prefix).ToArray(),
            ShowcasePaletteLabMatchFilter.WordStart => results.Where(static result => result.MatchKind == CommandPaletteMatchKind.WordStart).ToArray(),
            ShowcasePaletteLabMatchFilter.Substring => results.Where(static result => result.MatchKind == CommandPaletteMatchKind.Substring).ToArray(),
            ShowcasePaletteLabMatchFilter.Fuzzy => results.Where(static result => result.MatchKind == CommandPaletteMatchKind.Fuzzy).ToArray(),
            _ => results
        };

    private static IWidget BuildPaletteEvidenceLabPanel(
        ShowcaseDemoState state,
        string labQuery,
        IReadOnlyList<CommandPaletteSearchResult> results,
        CommandPaletteSearchResult? selected,
        IReadOnlyList<HintRankingEvidence> hintLedger)
    {
        var summary = selected is null
            ? "Selected Result\nNo matching results."
            : "Selected Result\n" +
              $"{selected.Entry.Title}\n" +
              $"Match: {selected.MatchKind}  P={selected.Score * 100.0:0.0}%\n" +
              $"Top command: {selected.Entry.Id}";
        var evidence = selected?.Evidence.Count is null or 0
            ? "No evidence entries."
            : string.Join(
                Environment.NewLine,
                selected.Evidence.Select(entry => $"{entry.Kind}  bf={entry.Factor:0.###}  {entry.Description}"));

        return new PanelWidget
        {
            Title = "Evidence Ledger",
            Child = new StackWidget(
                LayoutDirection.Vertical,
                [
                    (LayoutConstraint.Fixed((ushort)4), new ParagraphWidget(summary)),
                    (LayoutConstraint.Fill(), new ParagraphWidget(evidence)),
                    (LayoutConstraint.Fixed((ushort)7), BuildPaletteLabFooter(state, labQuery, results, selected, hintLedger))
                ])
        };
    }

    private static IWidget BuildPaletteLabFooter(
        ShowcaseDemoState state,
        string labQuery,
        IReadOnlyList<CommandPaletteSearchResult> results,
        CommandPaletteSearchResult? selected,
        IReadOnlyList<HintRankingEvidence> hintLedger)
    {
        var hintEvidence = string.Join(
            Environment.NewLine,
            hintLedger.Take(3).Select(static entry =>
                $"{entry.Rank + 1}. {entry.Label} EU={entry.ExpectedUtility:0.00} V={entry.NetValue:0.00} VOI={entry.ValueOfInformation:0.00}"));

        return new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Percentage((ushort)50), Panel(
                    "Bench (deterministic)",
                    $"Bench: {(state.PaletteLabBenchEnabled ? "ON" : "OFF")} frame={state.PaletteLabBenchFrame:000} processed={state.PaletteLabBenchProcessed} step={PaletteLabBenchStepTicks} query='{labQuery}' candidates={results.Count}\n" +
                    $"Latency: 0us (< 1000us)\n" +
                    $"Top command: {selected?.Entry.Id ?? "none"}")),
                (LayoutConstraint.Fill(), Panel(
                    "Hint Ranker",
                    hintEvidence))
            ]);
    }

    private static IWidget BuildDeterminismLab(ShowcaseDemoState state)
    {
        var frame = state.RuntimeStats?.StepIndex ?? state.ScriptFrame;
        var seed = 7 + Math.Abs(frame % 11);
        var fault = frame % 5 == 0;
        var full = 0x8A2F3D9CUL + (ulong)Math.Abs(frame);
        var dirty = fault ? full ^ 0x0000_00FFUL : full;
        var redraw = full;
        var status = fault ? "MISMATCH" : "OK";

        var header = new ParagraphWidget(
            $"Determinism Lab | seed={seed} | frame={frame} | active=DirtyRows | fault={(fault ? "ON" : "OFF")}\n" +
            "Checksum equivalence across Full, DirtyRows, and FullRedraw diff strategies");

        var equivalence = Panel(
            "Equivalence",
            "Strategy    Changes   Checksum             Status\n" +
            $"Full           118   0x{full:x16}   OK\n" +
            $"DirtyRows       42   0x{dirty:x16}   {status}\n" +
            $"FullRedraw    1080   0x{redraw:x16}   OK\n\n" +
            (fault
                ? "Mismatch: DirtyRows first at (0, 0) delta 1\n"
                : "Mismatch: none\n") +
            "Timeline: 2f3d9c 2f3d9d 2f3d9e 2f3d9f 2f3da0\n" +
            "Controls: 1/2/3 strategy | [/] seed | Space pause | F fault | E export | Enter/R run | A all | C checksum | X reset");

        var preview = Panel(
            "Scene Preview",
            "A..F....Q.....T..O....M....X....C....P....Z....\n" +
            "..B....L....R....D....H....N....V....E....K....\n" +
            "....O....cursor..Y....S....I....W....G....U....\n" +
            "Deterministic scene size: 60x18\n" +
            "LCG seed stream mutates rows and cells; dirty rows are diff-applied into a cloned buffer.");

        var checks = Panel(
            "Checks",
            "Scenarios (click/Enter):\n" +
            "> Baseline (10f)\n" +
            "  Drift (30f)\n" +
            "  Fault Injection (1f)\n\n" +
            "Runs (click row to inspect):\n" +
            $"  {status} Baseline (10f)\n" +
            "  OK Drift (30f)\n" +
            "  MISMATCH Fault Injection (1f)\n" +
            "Mouse: click scenario to run; click run row to inspect; wheel scrolls details");

        var report = Panel(
            "Report + Determinism Env",
            "JSONL export path: FTUI_DETERMINISM_LAB_REPORT or determinism_lab_report.jsonl\n" +
            "event=determinism_env timestamp run_id hash_key seed width height env\n" +
            "event=determinism_report scenario frame strategy checksum change_count status first_mismatch\n" +
            "demo env keys: FTUI_DEMO_DETERMINISTIC, FTUI_DEMO_SEED, FTUI_SEED, E2E_SEED, FTUI_DEMO_TICK_MS\n" +
            "hash_key format: screen_mode-60x18-seedN\n" +
            "checksum_buffer uses FNV-1a over cell content, fg, bg, attrs");

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(2), header),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(58), new StackWidget(
                            LayoutDirection.Vertical,
                            [
                                (LayoutConstraint.Percentage(55), equivalence),
                                (LayoutConstraint.Percentage(45), report)
                            ])),
                        (LayoutConstraint.Percentage(42), new StackWidget(
                            LayoutDirection.Vertical,
                            [
                                (LayoutConstraint.Percentage(52), preview),
                                (LayoutConstraint.Percentage(48), checks)
                            ]))
                    ])),
                (LayoutConstraint.Fixed(1), new ParagraphWidget("1/2/3 strategy | [/] seed | F fault | E export JSONL | A all scenarios | C log checksum | mouse hit regions"))
            ]);
    }

    private static IWidget BuildHyperlinkPlayground(ShowcaseDemoState state)
    {
        var active = Math.Abs((state.RuntimeStats?.StepIndex ?? state.ScriptFrame) % 5);
        var links = Panel(
            "Links (OSC-8)",
            $"{(active == 0 ? "> " : "  ")}FrankenTUI  https://ftui.dev\n" +
            $"{(active == 1 ? "> " : "  ")}Docs        https://ftui.dev/docs\n" +
            $"{(active == 2 ? "> " : "  ")}GitHub      https://github.com/Dicklesworthstone/frankentui\n" +
            $"{(active == 3 ? "> " : "  ")}OSC 8 Spec  https://iterm2.com/documentation-escape-codes.html\n" +
            $"{(active == 4 ? "> " : "  ")}ANSI Reference https://vt100.net/docs/vt510-rm/OSC.html\n\n" +
            "Each row registers LinkRegistry id plus HitRegion::Link data.");

        var details = Panel(
            "Details & Registry",
            "Selected: FrankenTUI\n" +
            "URL: https://ftui.dev\n" +
            "Registry ID: 1\n" +
            "Hit: id=8000 region=Link data=1\n" +
            "OSC 8 open: \\x1b]8;;https://ftui.dev\\x1b\\\n" +
            "OSC 8 close: \\x1b]8;;\\x1b\\\n" +
            "Notes: Project home + overview\n" +
            "Hover: None | Action: Copied URL / Activated link\n\n" +
            "Registry map: [1] FrankenTUI [2] Docs [3] GitHub [4] OSC 8 Spec [5] ANSI Reference");

        var controls = Panel(
            "Controls + JSONL",
            "Up/Down move focus | Tab cycle links | Enter activate | Space activate | c copy URL\n" +
            "Mouse: move/drag hover, down selects, up activates when inside hit rect\n" +
            "Logging env: FTUI_LINK_REPORT_PATH, FTUI_LINK_RUN_ID\n" +
            "JSONL: run_id, link_id, focus_idx, action, outcome\n" +
            "Actions: focus_move, activate_keyboard, copy_url, mouse_select, mouse_activate\n" +
            "LINK_HIT_BASE=8000; layouts store rect,index,link_id,hit_id");

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(3), new ParagraphWidget("Hyperlink Playground | OSC-8 + Hit Regions\nUp/Down move | Tab cycle | Enter activate | Mouse hover/click")),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(45), links),
                        (LayoutConstraint.Percentage(35), details),
                        (LayoutConstraint.Percentage(20), controls)
                    ])),
                (LayoutConstraint.Fixed(1), new ParagraphWidget("LinkRegistry | HitRegion::Link | OSC-8 open/close | FTUI_LINK_REPORT_PATH JSONL | keyboard and mouse accessibility"))
            ]);
    }

    private static IWidget BuildKanbanBoard(ShowcaseDemoState state)
    {
        var board = state.KanbanBoard ?? ShowcaseKanbanState.CreateDefault();
        var moveCount = board.History.Count;
        return new ParagraphWidget(string.Join('\n', BuildKanbanBoardLines(board, moveCount)));
    }

    private static string RenderKanbanCards(ShowcaseKanbanState board, int col)
    {
        var cards = board.Column(col);
        if (cards.Count == 0)
        {
            return "(empty)";
        }

        return string.Join(
            "\n",
            cards.Select((card, index) =>
                $"{(board.FocusCol == col && board.FocusRow == index ? "> " : "  ")}{card.Title}\n  [{card.Tag}] id={card.Id}"));
    }

    private static IReadOnlyList<string> BuildKanbanBoardLines(ShowcaseKanbanState board, int moveCount)
    {
        var lines = new List<string>
        {
            "╭" + CenterTitle("Kanban Board", 78, '─') + "╮",
            "│" + KanbanColumnTop("Todo", board.FocusCol == 0) + KanbanColumnTop("In Progress", board.FocusCol == 1) + KanbanColumnTop("Done", board.FocusCol == 2) + "│"
        };

        var columns = new[]
        {
            KanbanColumnRows(board, 0),
            KanbanColumnRows(board, 1),
            KanbanColumnRows(board, 2)
        };

        for (var row = 0; row < 18; row++)
        {
            lines.Add("│" + columns[0][row] + columns[1][row] + columns[2][row] + "│");
        }

        lines.Add("│" + FitCell($" h/l: column | j/k: card | H/L: move | u/r: undo/redo | mouse: drag | moves: {moveCount}", 78) + "│");
        lines.Add("╰" + new string('─', 78) + "╯");
        return lines;
    }

    private static string KanbanColumnTop(string title, bool focused) =>
        focused
            ? "┏" + CenterTitle(title, 24, '━') + "┓"
            : "╭" + CenterTitle(title, 24, '─') + "╮";

    private static string KanbanColumnBottom(bool focused) =>
        focused
            ? "┗" + new string('━', 24) + "┛"
            : "╰" + new string('─', 24) + "╯";

    private static IReadOnlyList<string> KanbanColumnRows(ShowcaseKanbanState board, int col)
    {
        var rows = new List<string>(18);
        var cards = board.Column(col);
        for (var index = 0; index < cards.Count && rows.Count < 18; index++)
        {
            var card = cards[index];
            rows.Add(KanbanColumnLine(board, col, $"{(board.FocusCol == col && board.FocusRow == index ? "> " : "  ")}{card.Title}"));
            rows.Add(KanbanColumnLine(board, col, $"  [{card.Tag}]"));
            rows.Add(KanbanColumnLine(board, col, string.Empty));
        }

        while (rows.Count < 18)
        {
            rows.Add(KanbanColumnLine(board, col, string.Empty));
        }

        rows[^1] = KanbanColumnBottom(board.FocusCol == col);
        return rows;
    }

    private static string KanbanColumnLine(ShowcaseKanbanState board, int col, string content)
    {
        var left = board.FocusCol == col ? "┃" : "│";
        var right = board.FocusCol == col ? "┃" : "│";
        return left + FitCell(content, 24) + right;
    }

    private static string CenterTitle(string title, int width, char fill)
    {
        var remaining = Math.Max(0, width - title.Length);
        var left = remaining / 2;
        var right = remaining - left;
        return new string(fill, left) + title + new string(fill, right);
    }

    private static string FitCell(string text, int width) =>
        text.Length >= width
            ? text[..width]
            : text.PadRight(width);

    private static IWidget BuildMarkdownLiveEditor(ShowcaseDemoState state)
    {
        var focus = Math.Abs((state.RuntimeStats?.StepIndex ?? state.ScriptFrame) % 3) switch
        {
            0 => "Editor",
            1 => "Search",
            _ => "Preview"
        };
        var diffMode = state.ScriptFrame % 2 == 1;
        var previewScroll = Math.Abs(state.ScriptFrame % 4);
        const string sample = "# Live Markdown Editor\n\nWrite Markdown on the left, preview on the right.\n\n## Goals\n\n- Split view editor + preview\n- Live updates without flicker\n- Search with highlighted matches\n- Diff mode: raw vs rendered width\n\n## Notes\n\nInline math: $E = mc^2$\n\n```rust\nfn render(frame: &mut Frame) {\n    // Draw widgets then diff\n}\n```\n\n| Feature | Status |\n| --- | --- |\n| Live preview | on |\n| Search | on |\n| Diff mode | Ctrl+D |\n";

        var search = Panel(
            focus == "Search" ? "Search [focus]" : "Search",
            "Query: preview\n" +
            "2/4 matches | search_ascii_case_insensitive\n" +
            "Ctrl+F focus search | Ctrl+N/P next/prev match\n" +
            "Selection maps byte range through grapheme CursorPosition\n" +
            "TextInput placeholder: Search in editor (Ctrl+F)\n" +
            "layout_search Rect cached for mouse focus");

        var editor = new PanelWidget
        {
            Title = focus == "Editor" ? "Editor [focus]" : "Editor",
            Child = new TextAreaWidget
            {
                Document = TextDocument.FromString(sample),
                Cursor = new TextCursor(6, 2),
                HasFocus = focus == "Editor",
                StatusText = "TextArea | line_numbers=true | soft_wrap=true | placeholder=Start writing Markdown..."
            }
        };

        var preview = new PanelWidget
        {
            Title = diffMode ? "Preview (Diff Mode)" : focus == "Preview" ? "Preview [focus]" : "Preview",
            Child = new ParagraphWidget(string.Empty)
            {
                Document = MarkdownDocumentBuilder.ParseCached(sample),
                RenderOptions = new TextRenderOptions(TextWrapMode.Word)
            }
        };

        var diff = Panel(
            "Raw vs Rendered Width",
            "01 raw  22 md  20 delta -2\n" +
            "02 raw   0 md   0 delta +0\n" +
            "03 raw  48 md  45 delta -3\n" +
            "04 raw   0 md   0 delta +0\n" +
            "05 raw   8 md   5 delta -3\n" +
            $"diff_mode={diffMode} preview_scroll={previewScroll} RULE_WIDTH=36");

        var controls = Panel(
            "Focus + Evidence",
            "MarkdownRenderer + SyntaxHighlighter + table_effect_phase\n" +
            "JSONL fields: run_id, tick, focus, query, diff_mode, preview_scroll, action\n" +
            $"focus={focus} tick_count={state.ScriptFrame} consumes_text_input={focus is "Editor" or "Search"}\n" +
            "Esc: Preview mode | Down from search returns editor | Up on editor line 0 returns search\n" +
            "Ctrl+D toggles diff mode | Ctrl+Up/Down scroll preview\n" +
            "Mouse: click search/editor/preview rects to focus, wheel preview to scroll\n" +
            "MarkdownTheme: h1/h2/code/link/table/task/math/admonition styles\n" +
            "Search state: current_match, result byte range, grapheme cursor");

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(3), new ParagraphWidget("Live Markdown | split editor + preview with search\nTextArea rope editor | MarkdownRenderer preview | highlighted search | raw-vs-rendered diff")),
                (LayoutConstraint.Fixed(8), search),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(50), editor),
                        (LayoutConstraint.Percentage(50), preview)
                    ])),
                (LayoutConstraint.Fixed(8), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(45), diff),
                        (LayoutConstraint.Percentage(55), controls)
                    ]))
            ]);
    }

    private static IWidget BuildDragDrop(ShowcaseDemoState state)
    {
        var modeIndex = Math.Abs((state.RuntimeStats?.StepIndex ?? state.ScriptFrame) % 3);
        var mode = modeIndex switch
        {
            0 => "Sortable List",
            1 => "Cross-Container",
            _ => "Keyboard Drag"
        };
        var selected = Math.Abs(state.ScriptFrame % 8);
        var focusedList = modeIndex == 1 && state.ScriptFrame % 2 == 1 ? 1 : 0;
        var keyboardActive = mode == "Keyboard Drag" && state.ScriptFrame % 2 == 0;

        var tabs = new ParagraphWidget(
            $"[{(mode == "Sortable List" ? "Sortable List" : "sortable list")}]  " +
            $"[{(mode == "Cross-Container" ? "Cross-Container" : "cross-container")}]  " +
            $"[{(mode == "Keyboard Drag" ? "Keyboard Drag" : "keyboard drag")}]\n" +
            "Tab / Shift+Tab cycles DemoMode; mouse down on layout_tabs selects mode");

        var left = Panel(
            focusedList == 0 ? "List A [focus]" : mode == "Sortable List" ? "Sortable List" : "List A",
            $"{(focusedList == 0 && selected == 0 ? "> " : "  ")}Item 1 id=0 color=primary\n" +
            $"{(focusedList == 0 && selected == 1 ? "> " : "  ")}Item 2 id=1 color=secondary\n" +
            $"{(focusedList == 0 && selected == 2 ? "> " : "  ")}Item 3 id=2 color=success\n" +
            $"{(focusedList == 0 && selected == 3 ? "> " : "  ")}Item 4 id=3 color=warning\n" +
            "  Item 5 id=4 | Item 6 id=5 | Item 7 id=6 | Item 8 id=7\n" +
            "layout_left Rect cached; scroll up/down moves selection");

        var right = Panel(
            focusedList == 1 ? "List B [focus]" : mode == "Cross-Container" ? "Target List" : "List B",
            $"{(focusedList == 1 && selected == 0 ? "> " : "  ")}File 1 id=8 color=primary\n" +
            $"{(focusedList == 1 && selected == 1 ? "> " : "  ")}File 2 id=9 color=secondary\n" +
            $"{(focusedList == 1 && selected == 2 ? "> " : "  ")}File 3 id=10 color=success\n" +
            $"{(focusedList == 1 && selected == 3 ? "> " : "  ")}File 4 id=11 color=warning\n" +
            "  File 5 id=12 | File 6 id=13 | File 7 id=14 | File 8 id=15\n" +
            "layout_right Rect cached; Enter transfers in cross-container mode");

        var status = Panel(
            "Keyboard Drag + Announcements",
            $"KeyboardDragManager active={keyboardActive} mode={(keyboardActive ? "Holding item" : "Inactive")}\n" +
            "Space/Enter: start or drop | arrows navigate DropTargetInfo | Esc cancels\n" +
            "Drop targets: WidgetId, target name, Rect from rendered list rows\n" +
            "Announcements: Started dragging Item 1; Target: Right: File 1; Dropped item at target 8\n" +
            "Payload: DragPayload::text(label); source_id from item id\n" +
            "A11y mode: fully keyboard-accessible drag-and-drop");

        var controls = Panel(
            "Modes + Mouse Evidence",
            $"mode={mode} selected_index={selected} focused_list={focusedList} tick_count={state.ScriptFrame}\n" +
            "Sortable: j/k navigate, u/d or Shift+K/J reorder items within list\n" +
            "Cross-container: h/l switches list, Enter transfers selected item\n" +
            "Mouse: click tabs/list rows, wheel selects, right-click reorders or transfers\n" +
            "Small terminal fallback: Drag & Drop needs a little more space.\n" +
            "JSONL fields: run_id, tick, mode, selected_index, focused_list, action, source_id, target_index, announcement");

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(3), new ParagraphWidget("Drag & Drop Lab | Sortable List | Cross-Container | Keyboard Drag\nLIST_SIZE=8 deterministic Item/File payloads | cached layout rects feed hit testing")),
                (LayoutConstraint.Fixed(3), tabs),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(50), left),
                        (LayoutConstraint.Percentage(50), right)
                    ])),
                (LayoutConstraint.Fixed(8), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(50), status),
                        (LayoutConstraint.Percentage(50), controls)
                    ]))
            ]);
    }

    private static IWidget BuildQuake(ShowcaseDemoState state)
    {
        var quality = Math.Abs(state.ScriptFrame % 4) switch
        {
            0 => "Full",
            1 => "Reduced",
            2 => "Minimal",
            _ => "Off"
        };

        var view = new PanelWidget
        {
            Title = "Quake E1M1",
            Child = new DeterministicVfxCanvasWidget(state.ScriptFrame, "quake-e1m1")
        };

        var statePanel = Panel(
            "Player + Physics",
            $"pos=(0.54,0.51,{0.18 + (state.ScriptFrame % 3) * 0.01:0.00}) yaw={state.ScriptFrame * 0.07:0.00} pitch=0.00 quality={quality}\n" +
            "W/A/S/D move | Arrows look yaw | j/k look pitch | Space jump | f fire | v quality | r reset\n" +
            "Constants: EYE_HEIGHT=0.18 GRAVITY=-0.28 JUMP=0.22 COLLISION_RADIUS=0.06\n" +
            "Physics: accel, friction, wall segment collision, floor-triangle ground height\n" +
            "Fire flash decays by 0.1 per tick; pitch clamps -1.2..1.2; yaw wraps TAU");

        var renderer = Panel(
            "Mesh Raster Evidence",
            $"Renderer: {ShowcaseVfxEffects.RendererName("quake-e1m1")}\n" +
            $"Mode: braille | Frame: {state.ScriptFrame} | FxQuality={quality}\n" +
            "Source data: QUAKE_E1M1_VERTS + QUAKE_E1M1_TRIS from 3d_data.rs\n" +
            "Pipeline: world_vertices -> camera_vertices -> clip_triangle_near -> depth buffer\n" +
            "Palette: palette_quake_stone dark mud/mid brown/tan/grey stone\n" +
            "Quality tiers: Full tri_step=1, Reduced=2, Minimal=4, Off=0");

        var controls = Panel(
            "Harness + Divergence",
            $"{ShowcaseVfxEffects.Description("quake-e1m1")}\n" +
            "Canvas.ensure_for_area(mode=Braille), painter.clear, Canvas.from_painter_ref\n" +
            "Small terminal fallback: Need a bit more space for Quake.\n" +
            "JSONL fields: run_id, frame, effect=quake-e1m1, quality, pos, yaw, pitch, hash, input\n" +
            "Local status: deterministic FPS Braille canvas; real Quake asset/raster parity remains tracked under 364-DEM-E5");

        return new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(3), new ParagraphWidget($"Quake E1M1 (Easter Egg) | WASD move | Arrows look | Space jump | F fire | V quality [{quality}] | R reset\nReal upstream screen renders QUAKE_E1M1 mesh with physics, collision, depth, and quality tiers")),
                (LayoutConstraint.Fill(), new StackWidget(
                    LayoutDirection.Horizontal,
                    [
                        (LayoutConstraint.Percentage(55), view),
                        (LayoutConstraint.Percentage(45), new StackWidget(
                            LayoutDirection.Vertical,
                            [
                                (LayoutConstraint.Percentage(34), statePanel),
                                (LayoutConstraint.Percentage(33), renderer),
                                (LayoutConstraint.Percentage(33), controls)
                            ]))
                    ]))
            ]);
    }

    private static IWidget BuildPlaceholder(ShowcaseDemoState state) =>
        Panel("Placeholder", $"{state.CurrentScreen.Title}\n\n{state.CurrentScreen.Blurb}");

    private static IWidget Panel(string title, string text) =>
        new PanelWidget
        {
            Title = title,
            Child = new ParagraphWidget(text)
        };

    private sealed class DeterministicVfxCanvasWidget(int frame, string? effect) : IWidget
    {
        public void Render(RuntimeRenderContext context)
        {
            if (context.Bounds.IsEmpty)
            {
                return;
            }

            var width = (ushort)Math.Max(context.Bounds.Width * CanvasPixelRect.ColsPerCell(CanvasMode.Braille), 1);
            var height = (ushort)Math.Max(context.Bounds.Height * CanvasPixelRect.RowsPerCell(CanvasMode.Braille), 1);
            var painter = new CanvasPainter(width, height);
            var centerX = width / 2.0;
            var centerY = height / 2.0;
            var mode = NormalizeVfxEffectLabel(effect);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (ShouldLightVfxPixel(mode, x, y, width, height, centerX, centerY, frame))
                    {
                        painter.Point(x, y);
                    }
                }
            }

            new CanvasWidget
            {
                Painter = painter,
                Template = context.Theme.Accent.ToCell()
            }.Render(context);
        }
    }

    private static string NormalizeVfxEffectLabel(string? effect) =>
        ShowcaseVfxEffects.NormalizeOrDefault(effect);

    private static bool ShouldLightVfxPixel(
        string effect,
        int x,
        int y,
        ushort width,
        ushort height,
        double centerX,
        double centerY,
        int frame)
    {
        var dx = x - centerX;
        var dy = y - centerY;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        return effect switch
        {
            "matrix" => ((x * 31 + frame * 7) % 23) < 3 && ((y + frame + x / 3) % 11) < 8,
            "fire" => y > height * 0.35 && Math.Sin(x * 0.34 + frame * 0.9 + y * 0.19) > ((height - y) / (double)height),
            "tunnel" => Math.Abs(Math.Sin(distance * 0.32 - frame * 0.8 + Math.Atan2(dy, dx) * 4.0)) > 0.72,
            "particles" => ((x * 17 + y * 29 + frame * 13) % 97) < 5 || Math.Abs(distance - ((frame * 5) % Math.Max(width, height))) < 1.4,
            "metaballs" => MetaballField(x, y, width, height, frame) > 1.15,
            "shape3d" => Math.Abs(dx * Math.Cos(frame * 0.2) + dy * Math.Sin(frame * 0.2)) < 1.2 || Math.Abs(dx - dy) < 1.0,
            "reaction-diffusion" => ((x / 3 + y / 2 + frame) % 5 is 0) ^ (Math.Sin(x * 0.11 + frame * 0.2) > Math.Cos(y * 0.13)),
            "strange-attractor" => Math.Abs(Math.Sin(dx * 0.09 + Math.Cos(dy * 0.07 + frame * 0.2) * 3.0)) < 0.12,
            "mandelbrot" => FractalBoundary(x, y, width, height, frame, julia: false),
            "lissajous" => Math.Abs((y - centerY) - Math.Sin((x + frame) * 0.08) * height * 0.28) < 1.5 ||
                Math.Abs((x - centerX) - Math.Cos((y + frame) * 0.10) * width * 0.22) < 1.5,
            "flow-field" => Math.Sin(x * 0.10 + Math.Sin(y * 0.07 + frame * 0.17) * 4.0) > 0.82 ||
                Math.Cos(y * 0.16 + frame * 0.11 + x * 0.03) > 0.90,
            "julia" => FractalBoundary(x, y, width, height, frame, julia: true),
            "wave-interference" => Math.Abs(Math.Sin(distance * 0.21 - frame * 0.4) + Math.Sin(Math.Sqrt(Math.Pow(x - width * 0.25, 2) + Math.Pow(y - height * 0.7, 2)) * 0.25)) < 0.18,
            "spiral" => Math.Abs(Math.Sin(Math.Atan2(dy, dx) * 3.0 + Math.Log(distance + 1.0) * 2.8 - frame * 0.3)) > 0.92,
            "spin-lattice" => ((x / 4 + y / 3 + frame) % 2 is 0) && Math.Sin(x * 0.19 + y * 0.23 + frame * 0.15) > -0.35,
            "threejs-model" => Math.Abs(dx * Math.Cos(frame * 0.16) - dy * Math.Sin(frame * 0.16)) < 1.0 ||
                Math.Abs(dx * Math.Sin(frame * 0.16) + dy * Math.Cos(frame * 0.16)) < 1.0,
            "doom-e1m1" => x % 7 is 0 || y % 5 is 0 || (x > width / 3 && x < width / 3 * 2 && y > height / 3 && y < height / 3 * 2),
            "quake-e1m1" => Math.Abs(Math.Sin(x * 0.17 + frame * 0.2) + Math.Cos(y * 0.31)) > 1.25,
            _ => Math.Sin(distance * 0.38 - frame * 0.7) + Math.Cos((x + frame) * 0.21) > 0.45 ||
                ((x + y + frame) % 17 == 0 && distance < Math.Min(width, height) * 0.48)
        };
    }

    private static double MetaballField(int x, int y, ushort width, ushort height, int frame)
    {
        var t = frame * 0.35;
        var ax = width * (0.35 + 0.18 * Math.Sin(t));
        var ay = height * (0.45 + 0.16 * Math.Cos(t * 0.8));
        var bx = width * (0.62 + 0.14 * Math.Cos(t * 1.2));
        var by = height * (0.50 + 0.18 * Math.Sin(t * 1.1));
        return 55.0 / (Math.Pow(x - ax, 2) + Math.Pow(y - ay, 2) + 12.0) +
            48.0 / (Math.Pow(x - bx, 2) + Math.Pow(y - by, 2) + 12.0);
    }

    private static bool FractalBoundary(int x, int y, ushort width, ushort height, int frame, bool julia)
    {
        var zx = (x - width * 0.5) / Math.Max(width * 0.24, 1.0);
        var zy = (y - height * 0.5) / Math.Max(height * 0.32, 1.0);
        var cx = julia ? -0.72 + Math.Sin(frame * 0.05) * 0.08 : zx - 0.55;
        var cy = julia ? 0.26 + Math.Cos(frame * 0.07) * 0.08 : zy;
        if (!julia)
        {
            zx = 0;
            zy = 0;
        }

        var iterations = 0;
        for (; iterations < 18; iterations++)
        {
            var nextX = zx * zx - zy * zy + cx;
            zy = 2 * zx * zy + cy;
            zx = nextX;
            if (zx * zx + zy * zy > 4.0)
            {
                break;
            }
        }

        return iterations is > 3 and < 18 && ((iterations + frame) % 3 != 0);
    }

    private static IWidget TwoColumn(IWidget left, IWidget right) =>
        new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Percentage((ushort)52), left),
                (LayoutConstraint.Fill(), right)
            ]);

    private static IWidget PaletteLabColumns(IWidget left, IWidget right) =>
        new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Percentage((ushort)55), left),
                (LayoutConstraint.Fill(), right)
            ]);

    private static IWidget ThreeColumn(IWidget left, IWidget center, IWidget right) =>
        new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Percentage((ushort)34), left),
                (LayoutConstraint.Percentage((ushort)33), center),
                (LayoutConstraint.Fill(), right)
            ]);
}
