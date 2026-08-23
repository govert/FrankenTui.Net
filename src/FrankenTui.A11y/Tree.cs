// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-a11y/src/tree.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: Rust's enum variants with associated data are represented by
// nested records under A11yChange, following the repository mapping rules.
// DIVERGENCE: Build() copies the builder map because C# cannot consume `this`;
// this preserves the upstream guarantee that built snapshots are frozen.

using System.Globalization;
using System.Text;

namespace FrankenTui.A11y;

/// <summary>Accumulates accessibility nodes during one render pass.</summary>
public sealed class A11yTreeBuilder
{
    private readonly Dictionary<ulong, A11yNodeInfo> _nodes;
    private ulong? _root;
    private ulong? _focused;

    public A11yTreeBuilder()
        : this(0)
    {
    }

    private A11yTreeBuilder(int capacity)
    {
        _nodes = new Dictionary<ulong, A11yNodeInfo>(capacity);
    }

    public static A11yTreeBuilder New() => new();

    public static A11yTreeBuilder WithCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        return new A11yTreeBuilder(capacity);
    }

    /// <summary>Inserts or replaces a node by ID.</summary>
    public void AddNode(A11yNodeInfo node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _nodes[node.Id] = node;
    }

    public void SetRoot(ulong id) => _root = id;

    public void SetFocused(ulong? id) => _focused = id;

    /// <summary>Freezes a snapshot. Later builder changes do not affect it.</summary>
    public A11yTree Build() => new(_nodes, _root, _focused);
}

/// <summary>Immutable snapshot of the accessibility tree after a render pass.</summary>
public sealed class A11yTree
{
    private const int MaxTraversalDepth = 1000;

    private readonly Dictionary<ulong, A11yNodeInfo> _nodes;
    private readonly ulong? _root;
    private readonly ulong? _focused;

    internal A11yTree(
        IReadOnlyDictionary<ulong, A11yNodeInfo> nodes,
        ulong? root,
        ulong? focused)
    {
        _nodes = new Dictionary<ulong, A11yNodeInfo>(nodes);
        _root = root;
        _focused = focused;
    }

    public A11yTree()
        : this(new Dictionary<ulong, A11yNodeInfo>(), null, null)
    {
    }

    public static A11yTree Empty() => new();

    public A11yNodeInfo? Node(ulong id) => _nodes.GetValueOrDefault(id);

    public A11yNodeInfo? Root => _root is ulong id ? Node(id) : null;

    public ulong? RootId => _root;

    public A11yNodeInfo? Focused => _focused is ulong id ? Node(id) : null;

    public ulong? FocusedId => _focused;

    /// <summary>Enumerates nodes in unspecified order, matching the upstream contract.</summary>
    public IEnumerable<A11yNodeInfo> Nodes() => _nodes.Values;

    public int NodeCount => _nodes.Count;

    public bool IsEmpty => _nodes.Count == 0;

    public IReadOnlyList<A11yNodeInfo> ChildrenOf(ulong id)
    {
        if (!_nodes.TryGetValue(id, out A11yNodeInfo? node))
            return Array.Empty<A11yNodeInfo>();

        var children = new List<A11yNodeInfo>(node.Children.Count);
        foreach (ulong childId in node.Children)
        {
            if (_nodes.TryGetValue(childId, out A11yNodeInfo? child))
                children.Add(child);
        }
        return children;
    }

    /// <summary>Walks parent IDs from <paramref name="id"/> toward the root.</summary>
    public IReadOnlyList<ulong> Ancestors(ulong id)
    {
        var path = new List<ulong>();
        var visited = new HashSet<ulong>();
        ulong? current = id;

        while (current is ulong currentId)
        {
            if (path.Count >= MaxTraversalDepth || !visited.Add(currentId))
                break;
            if (!_nodes.TryGetValue(currentId, out A11yNodeInfo? node))
                break;
            path.Add(currentId);
            current = node.Parent;
        }

        return path;
    }

    /// <summary>Produces deterministic screen-reader mirror lines.</summary>
    public ScreenReaderMirror ScreenReaderMirror(ScreenReaderPolicy policy)
    {
        var order = new List<(ulong Id, int Depth)>(_nodes.Count);
        var visited = new HashSet<ulong>();

        if (_root is ulong root)
            CollectMirrorOrder(root, 0, visited, order);

        ulong[] disconnected = _nodes.Keys
            .Where(id => !visited.Contains(id))
            .Order()
            .ToArray();
        foreach (ulong id in disconnected)
            CollectMirrorOrder(id, 0, visited, order);

        var lines = new List<string>();
        int omittedNodes = 0;
        foreach ((ulong id, int depth) in order)
        {
            if (!_nodes.TryGetValue(id, out A11yNodeInfo? node))
                continue;
            if (node.Role == A11yRole.Presentation)
                continue;
            if (lines.Count >= policy.MaxMirrorNodes)
            {
                omittedNodes++;
                continue;
            }

            string indent = new(' ', Math.Min(depth, 16) * 2);
            string summary = NodeSummary(node, _focused == id, includeLiveRegion: true);
            lines.Add(LimitText(indent + summary, policy.MaxTextChars));
        }

        return new ScreenReaderMirror(lines.AsReadOnly(), omittedNodes);
    }

    public ScreenReaderAnnouncements ScreenReaderAnnouncementsSince(
        A11yTree previous,
        ScreenReaderPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(previous);
        return Diff(previous).ScreenReaderAnnouncements(this, policy);
    }

    /// <summary>Diffs this snapshot against <paramref name="previous"/>.</summary>
    public A11yTreeDiff Diff(A11yTree previous)
    {
        ArgumentNullException.ThrowIfNull(previous);
        var added = new List<ulong>();
        var removed = new List<ulong>();
        var changed = new List<(ulong Id, IReadOnlyList<A11yChange> Changes)>();

        foreach ((ulong id, A11yNodeInfo node) in _nodes)
        {
            if (!previous._nodes.TryGetValue(id, out A11yNodeInfo? old))
            {
                added.Add(id);
                continue;
            }

            IReadOnlyList<A11yChange> changes = DiffNode(old, node);
            if (changes.Count != 0)
                changed.Add((id, changes));
        }

        foreach (ulong id in previous._nodes.Keys)
        {
            if (!_nodes.ContainsKey(id))
                removed.Add(id);
        }

        added.Sort();
        removed.Sort();
        changed.Sort(static (left, right) => left.Id.CompareTo(right.Id));

        (ulong? Old, ulong? New)? focusChanged = _focused != previous._focused
            ? (previous._focused, _focused)
            : null;

        return new A11yTreeDiff(
            added.AsReadOnly(),
            removed.AsReadOnly(),
            changed.AsReadOnly(),
            focusChanged);
    }

    private void CollectMirrorOrder(
        ulong id,
        int depth,
        HashSet<ulong> visited,
        List<(ulong Id, int Depth)> order)
    {
        if (depth >= MaxTraversalDepth || !visited.Add(id))
            return;
        if (!_nodes.TryGetValue(id, out A11yNodeInfo? node))
            return;

        order.Add((id, depth));
        foreach (ulong childId in node.Children)
            CollectMirrorOrder(childId, depth + 1, visited, order);
    }

    private static IReadOnlyList<A11yChange> DiffNode(A11yNodeInfo old, A11yNodeInfo current)
    {
        var changes = new List<A11yChange>();
        if (old.Name != current.Name)
            changes.Add(new A11yChange.NameChanged(old.Name, current.Name));
        if (old.Role != current.Role)
            changes.Add(new A11yChange.RoleChanged(old.Role, current.Role));
        if (old.Bounds != current.Bounds)
            changes.Add(new A11yChange.BoundsChanged());
        if (!old.Children.SequenceEqual(current.Children))
            changes.Add(new A11yChange.ChildrenChanged());
        if (old.LiveRegion != current.LiveRegion)
            changes.Add(new A11yChange.LiveRegionChanged(old.LiveRegion, current.LiveRegion));
        if (old.Description != current.Description)
            changes.Add(new A11yChange.DescriptionChanged(old.Description, current.Description));
        if (old.Shortcut != current.Shortcut)
            changes.Add(new A11yChange.ShortcutChanged(old.Shortcut, current.Shortcut));
        if (old.Parent != current.Parent)
            changes.Add(new A11yChange.ParentChanged(old.Parent, current.Parent));
        DiffState(old.State, current.State, changes);
        return changes.AsReadOnly();
    }

    private static void DiffState(A11yState old, A11yState current, List<A11yChange> changes)
    {
        AddBool("focused", old.Focused, current.Focused);
        AddBool("disabled", old.Disabled, current.Disabled);
        AddBoolOption("checked", old.Checked, current.Checked);
        AddBoolOption("expanded", old.Expanded, current.Expanded);
        AddBool("selected", old.Selected, current.Selected);
        AddBool("readonly", old.Readonly, current.Readonly);
        AddBool("required", old.Required, current.Required);
        AddBool("busy", old.Busy, current.Busy);
        AddDoubleOption("value_now", old.ValueNow, current.ValueNow);
        AddDoubleOption("value_min", old.ValueMin, current.ValueMin);
        AddDoubleOption("value_max", old.ValueMax, current.ValueMax);
        if (old.ValueText != current.ValueText)
        {
            changes.Add(new A11yChange.StateChanged(
                "value_text",
                current.ValueText ?? "<none>"));
        }

        void AddBool(string field, bool before, bool after)
        {
            if (before != after)
                changes.Add(new A11yChange.StateChanged(field, LowerBool(after)));
        }

        void AddBoolOption(string field, bool? before, bool? after)
        {
            if (before != after)
                changes.Add(new A11yChange.StateChanged(field, FormatDebugOption(after)));
        }

        void AddDoubleOption(string field, double? before, double? after)
        {
            if (before != after)
                changes.Add(new A11yChange.StateChanged(field, FormatDebugOption(after)));
        }
    }

    private static AnnouncementReason? AnnouncementReasonFor(IReadOnlyList<A11yChange> changes)
    {
        if (changes.Any(static change => change is A11yChange.LiveRegionChanged))
            return AnnouncementReason.LiveRegionChanged;

        bool contentChanged = changes.Any(change => change switch
        {
            A11yChange.NameChanged or
            A11yChange.DescriptionChanged or
            A11yChange.RoleChanged => true,
            A11yChange.StateChanged state => state.Field is
                "busy" or "checked" or "expanded" or "selected" or "value_now" or "value_text",
            _ => false,
        });
        return contentChanged ? AnnouncementReason.LiveContentChanged : null;
    }

    private static string? AnnouncementText(A11yNodeInfo node, bool focused, bool requireContent)
    {
        if (node.Role == A11yRole.Presentation)
            return null;
        if (requireContent && !HasAnnouncementContent(node))
            return null;
        return NormalizeText(NodeSummary(node, focused, includeLiveRegion: false));
    }

    private static bool HasAnnouncementContent(A11yNodeInfo node) =>
        NormalizeText(node.Name) is not null ||
        NormalizeText(node.Description) is not null ||
        StateSummaries(node.State).Count != 0;

    private static string NodeSummary(A11yNodeInfo node, bool focused, bool includeLiveRegion)
    {
        var parts = new List<string>();
        string heading = node.Role.ToDisplayString();
        string? name = NormalizeText(node.Name);
        if (name is not null)
            heading += ": " + name;
        parts.Add(heading);

        string? description = NormalizeText(node.Description);
        if (description is not null && description != name)
            parts.Add(description);

        List<string> states = StateSummaries(node.State);
        if (focused || node.State.Focused)
            states.Insert(0, "focused");
        if (states.Count != 0)
            parts.Add(string.Join(", ", states));

        string? shortcut = NormalizeText(node.Shortcut);
        if (shortcut is not null)
            parts.Add("shortcut " + shortcut);
        if (includeLiveRegion && node.LiveRegion is LiveRegion region)
            parts.Add("live " + region.ToDisplayString());

        return string.Join(". ", parts);
    }

    private static List<string> StateSummaries(A11yState state)
    {
        var states = new List<string>();
        if (state.Disabled) states.Add("disabled");
        if (state.Checked is bool check) states.Add(check ? "checked" : "not checked");
        if (state.Expanded is bool expanded) states.Add(expanded ? "expanded" : "collapsed");
        if (state.Selected) states.Add("selected");
        if (state.Readonly) states.Add("read only");
        if (state.Required) states.Add("required");
        if (state.Busy) states.Add("busy");

        string? valueText = NormalizeText(state.ValueText);
        if (valueText is not null)
            states.Add("value " + valueText);
        else if (state.ValueNow is double valueNow)
            states.Add("value " + FormatDisplayDouble(valueNow));
        return states;
    }

    private static string? NormalizeText(string? value)
    {
        if (value is null)
            return null;
        string normalized = string.Join(
            " ",
            value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length == 0 ? null : normalized;
    }

    private static string LimitText(string text, int maxChars)
    {
        if (maxChars <= 0)
            return string.Empty;

        int count = 0;
        var builder = new StringBuilder(text.Length);
        foreach (Rune rune in text.EnumerateRunes())
        {
            if (count++ >= maxChars)
                return builder.ToString();
            builder.Append(rune);
        }
        return text;
    }

    private static string LowerBool(bool value) => value ? "true" : "false";

    private static string FormatDebugOption(bool? value) => value switch
    {
        true => "Some(true)",
        false => "Some(false)",
        null => "None",
    };

    private static string FormatDebugOption(double? value) =>
        value is null ? "None" : $"Some({FormatDebugDouble(value.Value)})";

    private static string FormatDebugDouble(double value)
    {
        string formatted = FormatDisplayDouble(value);
        return double.IsFinite(value) && formatted.IndexOfAny(['.', 'e', 'E']) < 0
            ? formatted + ".0"
            : formatted;
    }

    private static string FormatDisplayDouble(double value)
    {
        if (double.IsPositiveInfinity(value)) return "inf";
        if (double.IsNegativeInfinity(value)) return "-inf";
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    internal static AnnouncementReason? GetAnnouncementReason(IReadOnlyList<A11yChange> changes) =>
        AnnouncementReasonFor(changes);

    internal static string? GetAnnouncementText(A11yNodeInfo node, bool focused, bool requireContent) =>
        AnnouncementText(node, focused, requireContent);
}

/// <summary>Changes between two accessibility tree snapshots.</summary>
public sealed class A11yTreeDiff
{
    public A11yTreeDiff(
        IReadOnlyList<ulong> added,
        IReadOnlyList<ulong> removed,
        IReadOnlyList<(ulong Id, IReadOnlyList<A11yChange> Changes)> changed,
        (ulong? Old, ulong? New)? focusChanged)
    {
        Added = added;
        Removed = removed;
        Changed = changed;
        FocusChanged = focusChanged;
    }

    public IReadOnlyList<ulong> Added { get; }
    public IReadOnlyList<ulong> Removed { get; }
    public IReadOnlyList<(ulong Id, IReadOnlyList<A11yChange> Changes)> Changed { get; }
    public (ulong? Old, ulong? New)? FocusChanged { get; }

    public bool IsEmpty =>
        Added.Count == 0 && Removed.Count == 0 && Changed.Count == 0 && FocusChanged is null;

    /// <summary>Converts this diff into bounded deterministic announcements.</summary>
    public ScreenReaderAnnouncements ScreenReaderAnnouncements(
        A11yTree current,
        ScreenReaderPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(current);
        var candidates = new List<ScreenReaderAnnouncement>();

        if (FocusChanged is { New: ulong newFocus } &&
            current.Node(newFocus) is A11yNodeInfo focusedNode &&
            focusedNode.Role != A11yRole.Presentation &&
            A11yTree.GetAnnouncementText(
                focusedNode,
                current.FocusedId == newFocus,
                requireContent: false) is string focusText)
        {
            candidates.Add(new ScreenReaderAnnouncement(
                newFocus,
                LiveRegion.Polite,
                AnnouncementReason.FocusChanged,
                focusText));
        }

        foreach (ulong id in Added)
        {
            if (current.Node(id) is A11yNodeInfo node &&
                node.LiveRegion is LiveRegion urgency &&
                A11yTree.GetAnnouncementText(
                    node,
                    current.FocusedId == id,
                    requireContent: true) is string text)
            {
                candidates.Add(new ScreenReaderAnnouncement(
                    id,
                    urgency,
                    AnnouncementReason.LiveRegionAdded,
                    text));
            }
        }

        foreach ((ulong id, IReadOnlyList<A11yChange> changes) in Changed)
        {
            if (current.Node(id) is A11yNodeInfo node &&
                node.LiveRegion is LiveRegion urgency &&
                A11yTree.GetAnnouncementReason(changes) is AnnouncementReason reason &&
                A11yTree.GetAnnouncementText(
                    node,
                    current.FocusedId == id,
                    requireContent: true) is string text)
            {
                candidates.Add(new ScreenReaderAnnouncement(id, urgency, reason, text));
            }
        }

        candidates.Sort(CompareAnnouncements);
        int max = Math.Max(0, policy.MaxAnnouncements);
        int droppedCount = Math.Max(0, candidates.Count - max);
        ScreenReaderAnnouncement[] announcements = candidates
            .Take(max)
            .Select(announcement => announcement with
            {
                Text = LimitAnnouncementText(announcement.Text, policy.MaxTextChars),
            })
            .ToArray();

        return new ScreenReaderAnnouncements(
            Array.AsReadOnly(announcements),
            droppedCount);
    }

    private static int CompareAnnouncements(
        ScreenReaderAnnouncement left,
        ScreenReaderAnnouncement right)
    {
        int comparison = ReasonRank(left.Reason).CompareTo(ReasonRank(right.Reason));
        if (comparison != 0) return comparison;
        comparison = UrgencyRank(left.Urgency).CompareTo(UrgencyRank(right.Urgency));
        if (comparison != 0) return comparison;
        comparison = CompareNodeIds(left.NodeId, right.NodeId);
        return comparison != 0
            ? comparison
            : StringComparer.Ordinal.Compare(left.Text, right.Text);
    }

    private static int ReasonRank(AnnouncementReason reason) => reason switch
    {
        AnnouncementReason.FocusChanged => 0,
        AnnouncementReason.LiveRegionChanged => 1,
        AnnouncementReason.LiveRegionAdded or AnnouncementReason.LiveContentChanged => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
    };

    private static int UrgencyRank(LiveRegion urgency) => urgency switch
    {
        LiveRegion.Assertive => 0,
        LiveRegion.Polite => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(urgency), urgency, null),
    };

    private static int CompareNodeIds(ulong? left, ulong? right) => (left, right) switch
    {
        (null, null) => 0,
        (null, _) => -1,
        (_, null) => 1,
        ({ } leftId, { } rightId) => leftId.CompareTo(rightId),
    };

    private static string LimitAnnouncementText(string text, int maxChars)
    {
        if (maxChars <= 0) return string.Empty;
        int count = 0;
        var builder = new StringBuilder(text.Length);
        foreach (Rune rune in text.EnumerateRunes())
        {
            if (count++ >= maxChars) return builder.ToString();
            builder.Append(rune);
        }
        return text;
    }
}

/// <summary>Bounded output policy for mirrors and announcements.</summary>
public readonly record struct ScreenReaderPolicy(
    int MaxMirrorNodes,
    int MaxAnnouncements,
    int MaxTextChars)
{
    public ScreenReaderPolicy()
        : this(128, 8, 240)
    {
    }

    public static ScreenReaderPolicy Default => new();
}

/// <summary>Deterministic text mirror for assistive-technology bridges.</summary>
public sealed class ScreenReaderMirror : IEquatable<ScreenReaderMirror>
{
    public ScreenReaderMirror(IReadOnlyList<string> lines, int omittedNodes)
    {
        Lines = lines;
        OmittedNodes = omittedNodes;
    }

    public IReadOnlyList<string> Lines { get; }
    public int OmittedNodes { get; }

    public string Text() => string.Join('\n', Lines);

    public bool Equals(ScreenReaderMirror? other) =>
        other is not null &&
        OmittedNodes == other.OmittedNodes &&
        Lines.SequenceEqual(other.Lines, StringComparer.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as ScreenReaderMirror);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(OmittedNodes);
        foreach (string line in Lines)
            hash.Add(line, StringComparer.Ordinal);
        return hash.ToHashCode();
    }
}

/// <summary>Bounded announcement batch for one tree transition.</summary>
public sealed class ScreenReaderAnnouncements : IEquatable<ScreenReaderAnnouncements>
{
    public ScreenReaderAnnouncements(
        IReadOnlyList<ScreenReaderAnnouncement> announcements,
        int droppedCount)
    {
        Announcements = announcements;
        DroppedCount = droppedCount;
    }

    public IReadOnlyList<ScreenReaderAnnouncement> Announcements { get; }
    public int DroppedCount { get; }

    public bool Equals(ScreenReaderAnnouncements? other) =>
        other is not null &&
        DroppedCount == other.DroppedCount &&
        Announcements.SequenceEqual(other.Announcements);

    public override bool Equals(object? obj) => Equals(obj as ScreenReaderAnnouncements);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(DroppedCount);
        foreach (ScreenReaderAnnouncement announcement in Announcements)
            hash.Add(announcement);
        return hash.ToHashCode();
    }
}

/// <summary>One announcement derived from focus or live-region changes.</summary>
public sealed record ScreenReaderAnnouncement(
    ulong? NodeId,
    LiveRegion Urgency,
    AnnouncementReason Reason,
    string Text);

public enum AnnouncementReason
{
    FocusChanged,
    LiveRegionAdded,
    LiveContentChanged,
    LiveRegionChanged,
}

/// <summary>A single property change on an accessibility node.</summary>
public abstract record A11yChange
{
    private A11yChange()
    {
    }

    public sealed record NameChanged(string? Old, string? New) : A11yChange;
    public sealed record RoleChanged(A11yRole Old, A11yRole New) : A11yChange;
    public sealed record StateChanged(string Field, string Description) : A11yChange;
    public sealed record BoundsChanged : A11yChange;
    public sealed record ChildrenChanged : A11yChange;
    public sealed record LiveRegionChanged(LiveRegion? Old, LiveRegion? New) : A11yChange;
    public sealed record DescriptionChanged(string? Old, string? New) : A11yChange;
    public sealed record ShortcutChanged(string? Old, string? New) : A11yChange;
    public sealed record ParentChanged(ulong? Old, ulong? New) : A11yChange;
}
