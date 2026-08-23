// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-a11y/src/node.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: C# enums cannot override ToString(); upstream Display output is
// exposed by ToDisplayString() extension methods.
// DIVERGENCE: Rust's consuming node builders map to immutable C# records whose
// With* methods return copies, preserving the same observable value semantics.

using FrankenTui.Core;

namespace FrankenTui.A11y;

/// <summary>ARIA-like role for terminal accessibility nodes.</summary>
public enum A11yRole
{
    Window,
    Dialog,
    Button,
    TextInput,
    Label,
    List,
    ListItem,
    Table,
    TableRow,
    TableCell,
    Checkbox,
    RadioButton,
    ProgressBar,
    Slider,
    Tab,
    TabPanel,
    Menu,
    MenuItem,
    Toolbar,
    ScrollBar,
    Separator,
    Group,
    Presentation,
}

public static class A11yRoleExtensions
{
    /// <summary>Whether the role represents a focusable control.</summary>
    public static bool IsInteractive(this A11yRole role) => role is
        A11yRole.Button or
        A11yRole.TextInput or
        A11yRole.Checkbox or
        A11yRole.RadioButton or
        A11yRole.Slider or
        A11yRole.Tab or
        A11yRole.MenuItem;

    /// <summary>Returns the exact upstream Display spelling.</summary>
    public static string ToDisplayString(this A11yRole role) => role switch
    {
        A11yRole.Window => "window",
        A11yRole.Dialog => "dialog",
        A11yRole.Button => "button",
        A11yRole.TextInput => "textInput",
        A11yRole.Label => "label",
        A11yRole.List => "list",
        A11yRole.ListItem => "listItem",
        A11yRole.Table => "table",
        A11yRole.TableRow => "tableRow",
        A11yRole.TableCell => "tableCell",
        A11yRole.Checkbox => "checkbox",
        A11yRole.RadioButton => "radioButton",
        A11yRole.ProgressBar => "progressBar",
        A11yRole.Slider => "slider",
        A11yRole.Tab => "tab",
        A11yRole.TabPanel => "tabPanel",
        A11yRole.Menu => "menu",
        A11yRole.MenuItem => "menuItem",
        A11yRole.Toolbar => "toolbar",
        A11yRole.ScrollBar => "scrollBar",
        A11yRole.Separator => "separator",
        A11yRole.Group => "group",
        A11yRole.Presentation => "presentation",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
    };
}

/// <summary>Accessibility state flags for a node.</summary>
public sealed record A11yState
{
    public bool Focused { get; init; }
    public bool Disabled { get; init; }
    public bool? Checked { get; init; }
    public bool? Expanded { get; init; }
    public bool Selected { get; init; }
    public bool Readonly { get; init; }
    public bool Required { get; init; }
    public bool Busy { get; init; }
    public double? ValueNow { get; init; }
    public double? ValueMin { get; init; }
    public double? ValueMax { get; init; }
    public string? ValueText { get; init; }
}

/// <summary>Live-region announcement urgency.</summary>
public enum LiveRegion
{
    Polite,
    Assertive,
}

public static class LiveRegionExtensions
{
    /// <summary>Returns the exact upstream Display spelling.</summary>
    public static string ToDisplayString(this LiveRegion region) => region switch
    {
        LiveRegion.Polite => "polite",
        LiveRegion.Assertive => "assertive",
        _ => throw new ArgumentOutOfRangeException(nameof(region), region, null),
    };
}

/// <summary>A single node in an accessibility tree.</summary>
public sealed record A11yNodeInfo
{
    private static readonly IReadOnlyList<ulong> NoChildren = Array.AsReadOnly(Array.Empty<ulong>());

    private A11yNodeInfo(ulong id, A11yRole role, Rect bounds)
    {
        Id = id;
        Role = role;
        Bounds = bounds;
    }

    public ulong Id { get; init; }
    public A11yRole Role { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public Rect Bounds { get; init; }
    public IReadOnlyList<ulong> Children { get; init; } = NoChildren;
    public ulong? Parent { get; init; }
    public string? Shortcut { get; init; }
    public A11yState State { get; init; } = new();
    public LiveRegion? LiveRegion { get; init; }

    /// <summary>Creates a node with only the required fields populated.</summary>
    public static A11yNodeInfo New(ulong id, A11yRole role, Rect bounds) =>
        new(id, role, bounds);

    public A11yNodeInfo WithName(string name) => this with { Name = name };

    public A11yNodeInfo WithDescription(string description) =>
        this with { Description = description };

    public A11yNodeInfo WithShortcut(string shortcut) => this with { Shortcut = shortcut };

    public A11yNodeInfo WithLiveRegion(LiveRegion liveRegion) =>
        this with { LiveRegion = liveRegion };

    public A11yNodeInfo WithChildren(IEnumerable<ulong> children)
    {
        ArgumentNullException.ThrowIfNull(children);
        return this with { Children = Array.AsReadOnly(children.ToArray()) };
    }

    public A11yNodeInfo WithParent(ulong parent) => this with { Parent = parent };

    public A11yNodeInfo WithState(A11yState state) =>
        this with { State = state ?? throw new ArgumentNullException(nameof(state)) };
}
