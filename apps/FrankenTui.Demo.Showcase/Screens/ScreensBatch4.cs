using FrankenTui.Layout;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

// Batch 4: screens 11, 7, 9, 13, 42, 22, 32, 35

internal static class Screen11TableTheme { public static IWidget Build(ShowcaseDemoState s) { var themes = new[] { "Plain", "Rounded", "Double", "Heavy", "Dashed" }; var lines = new List<string>(); for (var t = 0; t < themes.Length; t++) { lines.Add($"╭─ {themes[t]} ───────────╮"); lines.Add($"│ Header 1 │ Header 2 │"); lines.Add($"│ Cell A   │ Cell B   │"); lines.Add($"╰──────────┴──────────╯"); lines.Add(""); } return ShowcaseSurface.Panel("Table Theme Gallery", string.Join("\n", lines)); } }


