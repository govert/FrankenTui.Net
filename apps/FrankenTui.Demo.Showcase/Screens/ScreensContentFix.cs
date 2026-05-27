using FrankenTui.Layout;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

// Content-matching batch: screens with biggest gaps

internal static class Screen27FormValidation
{
    public static IWidget Build(ShowcaseDemoState state)
    {
        // view(): mode bar + Flex::vertical([Min(1), Fixed(2)]) with form + status
        // Upstream shows: Mode bar, Registration Form panel (full width), then validation panel
        // FIXED: mode bar is a single line, form fills rest, then validation errors panel at bottom
        var modeBar = new ParagraphWidget("Mode: Real-time [M to toggle]");
        var form = ShowcaseSurface.Panel("Registration Form", string.Join("\n",
            "Username:         Enter username (required)",
            "Email:            user@example.com",
            "Password:         Min 8 characters",
            "Confirm Password: Re-enter password",
            "Age:                25",
            "Bio:              Max 100 characters",
            "Website:          https://example.com",
            "Role:               (Select a role)",
            "Accept Terms:     [ ]"));
        var validation = ShowcaseSurface.Panel("Validation", "No validation errors yet.");
        return new StackWidget(LayoutDirection.Vertical, [
            (LayoutConstraint.Fixed(1), modeBar),
            (LayoutConstraint.Fill(), form),
            (LayoutConstraint.Fixed(4), validation)
        ]);
    }
}

internal static class Screen07FormsInput
{
    public static IWidget Build(ShowcaseDemoState state)
    {
        // Upstream: 3-column layout — Form (left) + Search/Password (mid) + Text Editor (right)
        // FIXED: match upstream panel titles and content exactly
        var form = ShowcaseSurface.Panel("Registration Form", string.Join("\n",
            " DRAFT   REQ 0/3   ERR 0   FIELDS 3/6",
            "",
            "Name *:         Enter your name...",
            "Email *:        user@example.com",
            "Role:             Developer",
            "Theme:          (1) Light",
            "Age:              25",
            "Accept Terms *: [ ]"));
        var searchPwd = new StackWidget(LayoutDirection.Vertical, [
            (LayoutConstraint.Fill(), ShowcaseSurface.Panel("Search", "Search...")),
            (LayoutConstraint.Fixed(6), ShowcaseSurface.Panel("Password", "Password\n\n●●●●●●●●"))
        ]);
        var textEditor = ShowcaseSurface.Panel("Text Editor", string.Join("\n",
            "1  Hello, world!",
            "2",
            "3  This is a multi-line",
            "4  text area with",
            "5  line numbers.",
            "6",
            "7  Use Ctrl+S to save."));
        return new StackWidget(LayoutDirection.Horizontal, [
            (LayoutConstraint.Percentage(50), new StackWidget(LayoutDirection.Vertical, [
                (LayoutConstraint.Fill(), form),
                (LayoutConstraint.Fixed(4), new ParagraphWidget(""))
            ])),
            (LayoutConstraint.Percentage(25), searchPwd),
            (LayoutConstraint.Percentage(25), textEditor)
        ]);
    }
}

internal static class Screen44DragDrop
{
    public static IWidget Build(ShowcaseDemoState state)
    {
        // Upstream: tabs row + single "Sortable List" panel with items + cross-container + keyboard panels
        // FIXED: match upstream structure — tabs, then sortable list panel filling area
        var tabs = new ParagraphWidget("[Sortable List] |  Cross-Container  |  Keyboard Drag");
        var sortable = ShowcaseSurface.Panel("Sortable List", string.Join("\n",
            "> Item 1",
            "  Item 2",
            "  Item 3",
            "  Item 4",
            "  Item 5",
            "  Item 6",
            "  Item 7",
            "  Item 8",
            "",
            "LIST_SIZE=8 deterministic Item/File payloads",
            "cached layout rects feed hit testing"));
        return new StackWidget(LayoutDirection.Vertical, [
            (LayoutConstraint.Fixed(1), tabs),
            (LayoutConstraint.Fill(), sortable)
        ]);
    }
}

internal static class Screen21Notifications
{
    public static IWidget Build(ShowcaseDemoState state)
    {
        // Upstream: single "Notification Demo" panel with instructions
        // FIXED: match upstream content exactly
        return ShowcaseSurface.Panel("Notification Demo", string.Join("\n",
            "Press keys to trigger notifications:",
            "",
            "  s  Success notification",
            "  e  Error with Retry action",
            "  w  Warning notification",
            "  i  Info notification",
            "  u  Urgent with Ack/Snooze actions",
            "  d  Dismiss all notifications",
            "",
            "Queue: 0 visible, 0 pending",
            "Total shown: 0",
            "Last action: (none)"));
    }
}

internal static class Screen11TableTheme
{
    public static IWidget Build(ShowcaseDemoState state)
    {
        // Upstream 80x24 shows empty content (just title) — the table theme gallery
        // renders table presets that may be empty in initial state.
        return ShowcaseSurface.Panel("Table Theme Gallery", "");
    }
}
