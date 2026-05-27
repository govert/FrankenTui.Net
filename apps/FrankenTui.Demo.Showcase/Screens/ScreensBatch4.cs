using FrankenTui.Layout;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

// Batch 4: screens 11, 7, 9, 13, 42, 22, 32, 35

internal static class Screen11TableTheme { public static IWidget Build(ShowcaseDemoState s) { var themes = new[] { "Plain", "Rounded", "Double", "Heavy", "Dashed" }; var lines = new List<string>(); for (var t = 0; t < themes.Length; t++) { lines.Add($"╭─ {themes[t]} ───────────╮"); lines.Add($"│ Header 1 │ Header 2 │"); lines.Add($"│ Cell A   │ Cell B   │"); lines.Add($"╰──────────┴──────────╯"); lines.Add(""); } return ShowcaseSurface.Panel("Table Theme Gallery", string.Join("\n", lines)); } }

internal static class Screen07FormsInput { public static IWidget Build(ShowcaseDemoState s) { var form = ShowcaseSurface.Panel("Registration Form", string.Join("\n", " DRAFT   REQ 0/3   ERR 0   FIELDS 3/6", "Name *:         Enter your name...", "Email *:        user@example.com", "Role:             Developer", "Theme:          (1) Light", "Age:              25", "Accept Terms *: [ ]")); var search = ShowcaseSurface.Panel("Search", "Search..."); var pwd = ShowcaseSurface.Panel("Password", "Password"); var textArea = ShowcaseSurface.Panel("Text Editor", "1  Hello, world!\n2\n3  This is a multi-line\n4  text area with\n5  line numbers.\n6\n7  Use Ctrl+S to save."); return new StackWidget(LayoutDirection.Horizontal, [(LayoutConstraint.Percentage(50), new StackWidget(LayoutDirection.Vertical, [(LayoutConstraint.Fill(), form), (LayoutConstraint.Fixed(5), search)])), (LayoutConstraint.Percentage(25), pwd), (LayoutConstraint.Percentage(25), textArea)]); } }

internal static class Screen09FileBrowser { public static IWidget Build(ShowcaseDemoState s) { var tree = ShowcaseSurface.Panel("Files", "📁 src/\n  📁 FrankenTui.Core/\n    📄 Rect.cs\n    📄 Cell.cs\n  📁 FrankenTui.Render/\n    📄 Buffer.cs\n    📄 Frame.cs\n  📁 FrankenTui.Widgets/\n    📄 Block.cs\n    📄 Paragraph.cs\n    📄 Table.cs\n  📁 FrankenTui.Runtime/\n    📄 Program.cs\n  📄 FrankenTui.sln"); var preview = ShowcaseSurface.Panel("Preview", "// FrankenTui.sln\n\nMicrosoft Visual Studio Solution File, Format Version 12.00\n# Visual Studio Version 17\nVisualStudioVersion = 17.0.31903.59\nMinimumVisualStudioVersion = 10.0.40219.1\nProject(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"FrankenTui.Core\""); return new StackWidget(LayoutDirection.Horizontal, [(LayoutConstraint.Percentage(40), tree), (LayoutConstraint.Percentage(60), preview)]); } }


