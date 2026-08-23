// SPDX-License-Identifier: Apache-2.0
// Compatibility projection for the accessibility DTOs that historically lived in Input.cs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: These FrankenTui.Widgets types predate the canonical ftui-a11y port.
// They remain public for source compatibility and are populated from canonical nodes by
// LegacyAccessibilityAdapter; new integrations should use FrankenTui.A11y.IAccessible.

using FrankenTui.Core;
using CanonicalA11y = FrankenTui.A11y;

namespace FrankenTui.Widgets;

/// <summary>Legacy accessibility roles retained for existing widget consumers.</summary>
public enum A11yRole
{
    TextInput,
    Button,
    Checkbox,
    List,
    ListItem,
    Table,
    TableRow,
    TreeItem,
    ProgressBar,
    Label,
    Generic,
    Window,
    Dialog,
    RadioButton,
    Slider,
    Group,
    TableCell,
    Tab,
    TabPanel,
    Menu,
    MenuItem,
    Toolbar,
    ScrollBar,
    Separator,
    Presentation,
}

/// <summary>
/// Legacy mutable accessibility state retained for existing widget consumers.
/// <see cref="CheckedValue"/> and <see cref="ExpandedValue"/> preserve the canonical
/// nullable states while the original Boolean properties continue to project null as false.
/// </summary>
public sealed class A11yState
{
    private bool? _checked;
    private bool? _expanded;

    public bool Focused { get; set; }
    public bool Disabled { get; set; }
    public bool Checked { get => _checked ?? false; set => _checked = value; }
    public bool Selected { get; set; }
    public bool Expanded { get => _expanded ?? false; set => _expanded = value; }
    public bool Hidden { get; set; }
    public bool Readonly { get; set; }
    public bool Required { get; set; }
    public bool Busy { get; set; }
    public bool? CheckedValue { get => _checked; set => _checked = value; }
    public bool? ExpandedValue { get => _expanded; set => _expanded = value; }
    public double? ValueNow { get; set; }
    public double? ValueMin { get; set; }
    public double? ValueMax { get; set; }
    public string? ValueText { get; set; }
}

/// <summary>Legacy mutable node projection retained for existing widget consumers.</summary>
public sealed class A11yNodeInfo
{
    public ulong Id { get; }
    public A11yRole Role { get; }
    public Rect Area { get; }
    public Rect Bounds => Area;
    public string? Name { get; private set; }
    public string? Description { get; private set; }
    public IReadOnlyList<ulong> Children { get; private set; } = Array.Empty<ulong>();
    public ulong? Parent { get; private set; }
    public string? Shortcut { get; private set; }
    public A11yState State { get; private set; } = new();
    public CanonicalA11y.LiveRegion? LiveRegion { get; private set; }

    public A11yNodeInfo(ulong id, A11yRole role, Rect area) =>
        (Id, Role, Area) = (id, role, area);

    public static A11yNodeInfo New(ulong id, A11yRole role, Rect area) =>
        new(id, role, area);

    public A11yNodeInfo WithName(string name) { Name = name; return this; }
    public A11yNodeInfo WithDescription(string description) { Description = description; return this; }
    public A11yNodeInfo WithState(A11yState state) { State = state; return this; }
    public A11yNodeInfo WithChildren(IEnumerable<ulong> children)
    {
        Children = Array.AsReadOnly(children.ToArray());
        return this;
    }
    public A11yNodeInfo WithParent(ulong parent) { Parent = parent; return this; }
    public A11yNodeInfo WithShortcut(string shortcut) { Shortcut = shortcut; return this; }
    public A11yNodeInfo WithLiveRegion(CanonicalA11y.LiveRegion liveRegion)
    {
        LiveRegion = liveRegion;
        return this;
    }
}

/// <summary>
/// Legacy widget accessibility interface. New integrations should use
/// <see cref="CanonicalA11y.IAccessible"/>.
/// </summary>
public interface IAccessible
{
    List<A11yNodeInfo> AccessibilityNodes(Rect area);
}

/// <summary>Creates the legacy compatibility projection from canonical accessibility nodes.</summary>
public static class LegacyAccessibilityAdapter
{
    public static List<A11yNodeInfo> FromCanonical(
        IEnumerable<CanonicalA11y.A11yNodeInfo> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        return nodes.Select(FromCanonical).ToList();
    }

    public static A11yNodeInfo FromCanonical(CanonicalA11y.A11yNodeInfo node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var state = new A11yState
        {
            Focused = node.State.Focused,
            Disabled = node.State.Disabled,
            CheckedValue = node.State.Checked,
            ExpandedValue = node.State.Expanded,
            Selected = node.State.Selected,
            Readonly = node.State.Readonly,
            Required = node.State.Required,
            Busy = node.State.Busy,
            ValueNow = node.State.ValueNow,
            ValueMin = node.State.ValueMin,
            ValueMax = node.State.ValueMax,
            ValueText = node.State.ValueText,
        };

        A11yNodeInfo projected = A11yNodeInfo.New(node.Id, ProjectRole(node.Role), node.Bounds)
            .WithState(state)
            .WithChildren(node.Children);

        if (node.Name is { } name) projected.WithName(name);
        if (node.Description is { } description) projected.WithDescription(description);
        if (node.Parent is { } parent) projected.WithParent(parent);
        if (node.Shortcut is { } shortcut) projected.WithShortcut(shortcut);
        if (node.LiveRegion is { } liveRegion) projected.WithLiveRegion(liveRegion);
        return projected;
    }

    private static A11yRole ProjectRole(CanonicalA11y.A11yRole role) => role switch
    {
        CanonicalA11y.A11yRole.Window => A11yRole.Window,
        CanonicalA11y.A11yRole.Dialog => A11yRole.Dialog,
        CanonicalA11y.A11yRole.Button => A11yRole.Button,
        CanonicalA11y.A11yRole.TextInput => A11yRole.TextInput,
        CanonicalA11y.A11yRole.Label => A11yRole.Label,
        CanonicalA11y.A11yRole.List => A11yRole.List,
        CanonicalA11y.A11yRole.ListItem => A11yRole.ListItem,
        CanonicalA11y.A11yRole.Table => A11yRole.Table,
        CanonicalA11y.A11yRole.TableRow => A11yRole.TableRow,
        CanonicalA11y.A11yRole.TableCell => A11yRole.TableCell,
        CanonicalA11y.A11yRole.Checkbox => A11yRole.Checkbox,
        CanonicalA11y.A11yRole.RadioButton => A11yRole.RadioButton,
        CanonicalA11y.A11yRole.ProgressBar => A11yRole.ProgressBar,
        CanonicalA11y.A11yRole.Slider => A11yRole.Slider,
        CanonicalA11y.A11yRole.Tab => A11yRole.Tab,
        CanonicalA11y.A11yRole.TabPanel => A11yRole.TabPanel,
        CanonicalA11y.A11yRole.Menu => A11yRole.Menu,
        CanonicalA11y.A11yRole.MenuItem => A11yRole.MenuItem,
        CanonicalA11y.A11yRole.Toolbar => A11yRole.Toolbar,
        CanonicalA11y.A11yRole.ScrollBar => A11yRole.ScrollBar,
        CanonicalA11y.A11yRole.Separator => A11yRole.Separator,
        CanonicalA11y.A11yRole.Group => A11yRole.Group,
        CanonicalA11y.A11yRole.Presentation => A11yRole.Presentation,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
    };
}
