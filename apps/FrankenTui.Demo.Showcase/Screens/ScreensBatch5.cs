using FrankenTui.Layout;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

// Batch 5: screens 1, 3, 4, 5, 6, 8, 12, 15

internal static class Screen01GuidedTour { public static IWidget Build(ShowcaseDemoState s) { return ShowcaseSurface.Panel("Guided Tour", "Welcome to the FrankenTUI Demo Showcase!\n\nThis tour walks you through the key screens.\n\nPress Enter or Space to start the tour.\n\n  n / p  step through tour steps\n  Space  pause/resume tour\n  Esc    exit tour\n  Tab    next screen\n\nScreens in tour:\n  1. Dashboard\n  2. Widget Gallery\n  3. Layout Lab\n  4. Forms & Input\n  5. Data Viz"); } }


internal static class Screen04CodeExplorer { public static IWidget Build(ShowcaseDemoState s) { return ShowcaseSurface.Panel("Code Explorer", "📁 src/\n  📁 FrankenTui.Core/\n    📄 Rect.cs          \"Rectangle with position and size\"\n    📄 Cell.cs           \"Terminal cell with char and style\"\n  📁 FrankenTui.Render/\n    📄 Buffer.cs         \"2D cell buffer for rendering\"\n    📄 Frame.cs          \"Frame with buffer and registry\"\n  📁 FrankenTui.Widgets/\n    📄 Block.cs          \"Bordered block widget\"\n    📄 Paragraph.cs      \"Text paragraph widget\"\n  📁 FrankenTui.Runtime/\n    📄 Program.cs        \"Main program loop\""); } }

internal static class Screen05WidgetGallery { public static IWidget Build(ShowcaseDemoState s) { var widgets = string.Join("\n\n", new[] { "Block: bordered container with optional title", "Paragraph: \"The quick brown fox jumps over the lazy dog.\"", "Table:\n| Col A | Col B | Col C |\n|-------|-------|-------|\n|   a1  |   b1  |   c1  |\n|   a2  |   b2  |   c2  |", "List:\n  • Item one\n  • Item two\n  • Item three", "Gauge: [████████░░] 80%", "Sparkline: ▁▂▃▄▅▆▇█▇▆▅▄▃▂▁", "Badge: [INFO]", "Tabs: [Tab1] Tab2 Tab3" }); return ShowcaseSurface.Panel("Widget Gallery", widgets); } }

internal static class Screen06LayoutLab { public static IWidget Build(ShowcaseDemoState s) { return ShowcaseSurface.Panel("Layout Lab", "Constraint Types:\n\n  Fixed(10)    — exactly 10 rows/cols\n  Min(5)       — at least 5, grows if space\n  Max(20)      — at most 20, shrinks if needed\n  Percentage(50) — 50% of available space\n  Fill(1)     — takes remaining space\n\nLayout direction: Vertical\n\nChildren:\n  Fixed 3  → ███\n  Min 5    → ████████\n  Max 6    → ██████\n  Fill     → ████████████████"); } }


