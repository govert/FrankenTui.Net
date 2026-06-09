// Port of .external/frankentui/crates/ftui-widgets/src/tree.rs
// Tree widget for hierarchical display with guide chars, search, keyboard/mouse navigation, and state persistence.

using System.Globalization;
using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Widgets;

// ── TreeGuides ────────────────────────────────────────────────────────────────

/// <summary>Guide character styles for tree rendering.</summary>
public enum TreeGuides
{
    /// <summary>ASCII guides: <c>|</c>, <c>+-- </c>, <c>`-- </c>.</summary>
    Ascii,
    /// <summary>Unicode box-drawing characters (default).</summary>
    Unicode,
    /// <summary>Bold Unicode box-drawing characters.</summary>
    Bold,
    /// <summary>Double-line Unicode characters.</summary>
    Double,
    /// <summary>Rounded Unicode characters.</summary>
    Rounded,
}

/// <summary>Extension methods providing guide strings for each <see cref="TreeGuides"/> variant.</summary>
public static class TreeGuidesExt
{
    /// <summary>Vertical continuation (item has siblings below).</summary>
    public static string Vertical(this TreeGuides g) => g switch
    {
        TreeGuides.Ascii => "|   ",
        TreeGuides.Bold => "┃   ",
        TreeGuides.Double => "║   ",
        _ => "│   ",   // Unicode and Rounded
    };

    /// <summary>Branch guide (item has siblings below).</summary>
    public static string Branch(this TreeGuides g) => g switch
    {
        TreeGuides.Ascii => "+-- ",
        TreeGuides.Bold => "┣━━ ",
        TreeGuides.Double => "╠══ ",
        _ => "├── ",  // Unicode and Rounded
    };

    /// <summary>Last-item guide (no siblings below).</summary>
    public static string Last(this TreeGuides g) => g switch
    {
        TreeGuides.Ascii => "`-- ",
        TreeGuides.Bold => "┗━━ ",
        TreeGuides.Double => "╚══ ",
        TreeGuides.Rounded => "╰── ",
        _ => "└── ",  // Unicode
    };

    /// <summary>Empty indentation (no guide needed).</summary>
    public static string Space(this TreeGuides _) => "    ";

    /// <summary>Width in columns of each guide segment.</summary>
    public static int Width(this TreeGuides _) => 4;
}

// ── TreeNode ──────────────────────────────────────────────────────────────────

/// <summary>A node in the tree hierarchy.</summary>
public sealed class TreeNode
{
    internal string _label;
    internal string? _icon;
    /// <summary>Child nodes (internal for undo support).</summary>
    internal List<TreeNode> _children = new();
    /// <summary>Lazily materialized children.</summary>
    internal List<TreeNode>? _lazyChildren;
    /// <summary>Whether this node is expanded (internal for undo support).</summary>
    internal bool _expanded = true;

    /// <summary>Create a new tree node with the given label.</summary>
    public TreeNode(string label) => _label = label;

    /// <summary>Create a new tree node with the given label and children.</summary>
    public TreeNode(string label, IReadOnlyList<TreeNode> children)
    {
        _label = label;
        _children = new List<TreeNode>(children);
    }

    /// <summary>Add a child node.</summary>
    public TreeNode Child(TreeNode node) { _children.Add(node); return this; }

    /// <summary>Set children from a list.</summary>
    public TreeNode WithChildren(List<TreeNode> nodes) { _children = nodes; return this; }

    /// <summary>Set an icon prefix rendered before the label.</summary>
    public TreeNode WithIcon(string icon) { _icon = icon; return this; }

    /// <summary>
    /// Configure lazily materialized children.
    /// The node starts collapsed and children are attached when first expanded.
    /// </summary>
    public TreeNode WithLazyChildren(List<TreeNode> nodes)
    {
        _lazyChildren = nodes;
        _expanded = false;
        return this;
    }

    /// <summary>Set whether this node is expanded.</summary>
    public TreeNode WithExpanded(bool expanded)
    {
        if (expanded) MaterializeLazyChildren();
        _expanded = expanded;
        return this;
    }

    /// <summary>Get the label.</summary>
    public string Label => _label;

    /// <summary>Get the children.</summary>
    public IReadOnlyList<TreeNode> Children() => _children;

    /// <summary>Optional icon rendered before label.</summary>
    public string? Icon => _icon;

    /// <summary>Whether this node has loaded or lazy children.</summary>
    public bool HasChildren => _children.Count > 0 || (_lazyChildren?.Count ?? 0) > 0;

    /// <summary>Whether this node is expanded.</summary>
    public bool IsExpanded => _expanded;

    /// <summary>Toggle the expanded state.</summary>
    public void ToggleExpanded()
    {
        if (!_expanded) MaterializeLazyChildren();
        _expanded = !_expanded;
    }

    internal void MaterializeLazyChildren()
    {
        if (_lazyChildren is { } lazy)
        {
            _children.AddRange(lazy);
            _lazyChildren = null;
        }
    }

    internal void MaterializeAllLazyChildren()
    {
        MaterializeLazyChildren();
        foreach (var c in _children) c.MaterializeAllLazyChildren();
    }

    /// <summary>Count all visible (expanded) nodes, including this one.</summary>
    public int VisibleCount()
    {
        int count = 1;
        if (_expanded)
            foreach (var c in _children) count += c.VisibleCount();
        return count;
    }

    /// <summary>Collect all expanded node paths into a set.</summary>
    internal void CollectExpanded(string prefix, HashSet<string> out_)
    {
        string path = string.IsNullOrEmpty(prefix) ? _label : $"{prefix}/{_label}";
        if (_expanded && HasChildren) out_.Add(path);
        foreach (var c in _children) c.CollectExpanded(path, out_);
    }

    /// <summary>Apply expanded state from a set of paths.</summary>
    internal void ApplyExpanded(string prefix, HashSet<string> expandedPaths)
    {
        string path = string.IsNullOrEmpty(prefix) ? _label : $"{prefix}/{_label}";
        if (HasChildren)
        {
            _expanded = expandedPaths.Contains(path);
            if (_expanded) MaterializeLazyChildren();
        }
        foreach (var c in _children) c.ApplyExpanded(path, expandedPaths);
    }

    /// <summary>Deep-clone this node (mirrors the Rust Clone derive used by filter_node).</summary>
    internal TreeNode Clone()
    {
        var copy = new TreeNode(_label) { _icon = _icon, _expanded = _expanded };
        foreach (var c in _children) copy._children.Add(c.Clone());
        if (_lazyChildren != null)
        {
            copy._lazyChildren = new List<TreeNode>();
            foreach (var c in _lazyChildren) copy._lazyChildren.Add(c.Clone());
        }
        return copy;
    }
}

// ── FilteredPathNode (internal helper) ────────────────────────────────────────

/// <summary>Internal helper used by filtered visible-index path finding.</summary>
sealed class FilteredPathNode
{
    public bool Expanded;
    public List<(int Idx, FilteredPathNode Node)> Children = new();
}

// ── TreePersistState ──────────────────────────────────────────────────────────

/// <summary>
/// Persistable state for a <see cref="Tree"/> widget.
/// Stores the set of expanded node paths to restore tree expansion state.
/// </summary>
public sealed class TreePersistState
{
    /// <summary>Set of expanded node paths (e.g., "root/src/main.rs").</summary>
    public HashSet<string> ExpandedPaths { get; set; } = new();

    public TreePersistState() { }

    internal TreePersistState(HashSet<string> paths) => ExpandedPaths = paths;

    /// <summary>Equality comparison (matches Rust PartialEq derive).</summary>
    public bool Equals(TreePersistState? other) =>
        other != null && ExpandedPaths.SetEquals(other.ExpandedPaths);
}

// ── Tree ──────────────────────────────────────────────────────────────────────

/// <summary>Tree widget for rendering hierarchical data.</summary>
public sealed class Tree : IWidget, IStateful<TreePersistState>, ITreeUndoExt
{
    /// <summary>Unique ID for undo tracking.</summary>
    readonly UndoWidgetId _undoId = UndoWidgetId.New();

    TreeNode _root;
    /// <summary>Whether to show the root node.</summary>
    bool _showRoot = true;
    /// <summary>Guide character style.</summary>
    TreeGuides _guides = TreeGuides.Unicode;
    /// <summary>Style for guide characters.</summary>
    WidgetStyle _guideStyle;
    /// <summary>Style for node labels.</summary>
    WidgetStyle _labelStyle;
    /// <summary>Style for the root node label.</summary>
    WidgetStyle _rootStyle;
    /// <summary>Optional persistence ID for state saving/restoration.</summary>
    string? _persistenceId;
    /// <summary>Optional hit ID for mouse interaction.</summary>
    HitId? _hitId;
    /// <summary>Optional case-insensitive search query.</summary>
    string? _searchQuery;

    /// <summary>Create a tree widget with the given root node.</summary>
    public Tree(TreeNode root) => _root = root;

    /// <summary>Set whether to show the root node.</summary>
    public Tree WithShowRoot(bool show) { _showRoot = show; return this; }

    /// <summary>Set the guide character style.</summary>
    public Tree WithGuides(TreeGuides guides) { _guides = guides; return this; }

    /// <summary>Set the style for guide characters.</summary>
    public Tree WithGuideStyle(WidgetStyle style) { _guideStyle = style; return this; }

    /// <summary>Set the style for node labels.</summary>
    public Tree WithLabelStyle(WidgetStyle style) { _labelStyle = style; return this; }

    /// <summary>Set the style for the root label.</summary>
    public Tree WithRootStyle(WidgetStyle style) { _rootStyle = style; return this; }

    /// <summary>Set a persistence ID for state saving.</summary>
    public Tree WithPersistenceId(string id) { _persistenceId = id; return this; }

    /// <summary>Get the persistence ID, if set.</summary>
    public string? PersistenceId => _persistenceId;

    /// <summary>Set a hit ID for mouse interaction.</summary>
    public Tree HitId_(HitId id) { _hitId = id; return this; }

    /// <summary>Apply a case-insensitive search query filter.</summary>
    public Tree WithSearchQuery(string query)
    {
        _searchQuery = query.Trim().Length == 0 ? null : query;
        return this;
    }

    /// <summary>Clear search filtering.</summary>
    public Tree WithoutSearchQuery() { _searchQuery = null; return this; }

    /// <summary>Get a reference to the root node.</summary>
    public TreeNode Root => _root;

    /// <summary>Get a mutable reference to the root node.</summary>
    public TreeNode RootMut() => _root;

    // ── IStateful<TreePersistState> ───────────────────────────────────────────

    /// <summary>State key for persistence (uses "Tree" + persistence_id or "default").</summary>
    public StateKey StateKey => new StateKey("Tree", _persistenceId ?? "default");

    /// <summary>Save the current expanded state.</summary>
    public TreePersistState SaveState()
    {
        var paths = new HashSet<string>();
        _root.CollectExpanded("", paths);
        return new TreePersistState(paths);
    }

    /// <summary>Restore the expanded state from a saved state.</summary>
    public void RestoreState(TreePersistState state) =>
        _root.ApplyExpanded("", state.ExpandedPaths);

    // ── IUndoSupport / ITreeUndoExt ───────────────────────────────────────────

    /// <summary>Get the undo widget ID for this tree.</summary>
    public UndoWidgetId UndoId() => _undoId;

    UndoWidgetId IUndoSupport.UndoWidgetId => _undoId;

    /// <summary>Create a snapshot of the current state for undo purposes.</summary>
    public object CreateSnapshot() => SaveState();

    /// <summary>Restore state from a snapshot. Returns true if the snapshot type matches.</summary>
    public bool RestoreSnapshot(object snapshot)
    {
        if (snapshot is TreePersistState snap)
        {
            RestoreState(snap);
            return true;
        }
        return false;
    }

    /// <summary>Check if a node is expanded (by path indices from root).</summary>
    public bool IsNodeExpanded(int[] path) =>
        GetNodeAtPath(path) is { } node && node.IsExpanded;

    /// <summary>Expand a node (by path indices from root); materializes lazy children.</summary>
    public void ExpandNode(int[] path)
    {
        if (GetNodeAtPathMut(path) is { } node)
        {
            node.MaterializeLazyChildren();
            node._expanded = true;
        }
    }

    /// <summary>Collapse a node (by path indices from root).</summary>
    public void CollapseNode(int[] path)
    {
        if (GetNodeAtPathMut(path) is { } node)
            node._expanded = false;
    }

    // ── Node access by path ───────────────────────────────────────────────────

    /// <summary>Get a reference to a node at the given path (indices from root).</summary>
    public TreeNode? GetNodeAtPath(int[] path)
    {
        var current = _root;
        foreach (var idx in path)
        {
            if (idx >= current._children.Count) return null;
            current = current._children[idx];
        }
        return current;
    }

    /// <summary>Get a mutable reference to a node at the given path (indices from root).</summary>
    TreeNode? GetNodeAtPathMut(int[] path)
    {
        var current = _root;
        foreach (var idx in path)
        {
            if (idx >= current._children.Count) return null;
            current = current._children[idx];
        }
        return current;
    }

    // ── Visible-index navigation ──────────────────────────────────────────────

    /// <summary>
    /// Get a mutable reference to the node at the given visible (flattened) index.
    /// The traversal order matches <c>RenderNode</c>: if <c>show_root</c> is true the
    /// root is row 0; otherwise children of the root are the top-level rows.
    /// Only expanded nodes' children are visited.
    /// </summary>
    public TreeNode? NodeAtVisibleIndexMut(int target)
    {
        var path = FindPathIndicesAtVisibleIndex(target);
        if (path == null) return null;
        var current = _root;
        foreach (var idx in path)
        {
            current.MaterializeLazyChildren();
            if (idx >= current._children.Count) return null;
            current = current._children[idx];
        }
        return current;
    }

    bool ToggleNodeAtVisibleIndex(int index, string source)
    {
        var node = NodeAtVisibleIndexMut(index);
        if (node == null || !node.HasChildren) return false;
        node.ToggleExpanded();
        return true;
    }

    // ── Keyboard navigation ───────────────────────────────────────────────────

    /// <summary>
    /// Handle keyboard navigation at the currently selected visible row.
    /// <list type="bullet">
    /// <item><description><b>Enter / Space</b>: Toggle expand/collapse on the selected node.</description></item>
    /// <item><description><b>Right</b>: Expand the selected node (if collapsed and has children).</description></item>
    /// <item><description><b>Left</b>: Collapse the selected node (if expanded).</description></item>
    /// </list>
    /// Returns <c>true</c> when an expand/collapse action was applied.
    /// </summary>
    public bool HandleKey(KeyEvent key, int selectedVisibleIndex)
    {
        // KeyCode is a closed class hierarchy (abstract record). Use pattern matching.
        switch (key.Code)
        {
            case KeyCode.Enter:
                return ToggleNodeAtVisibleIndex(selectedVisibleIndex, "keyboard");

            case KeyCode.Char charKey when charKey.Character == ' ':
                // Upstream: KeyCode::Char(' ') => toggle
                return ToggleNodeAtVisibleIndex(selectedVisibleIndex, "keyboard");

            case KeyCode.Right:
            {
                var node = NodeAtVisibleIndexMut(selectedVisibleIndex);
                if (node != null && !node.IsExpanded && node.HasChildren)
                {
                    node.ToggleExpanded();
                    return true;
                }
                return false;
            }

            case KeyCode.Left:
            {
                var node = NodeAtVisibleIndexMut(selectedVisibleIndex);
                if (node != null && node.IsExpanded && node.HasChildren)
                {
                    node.ToggleExpanded();
                    return true;
                }
                return false;
            }

            default:
                return false;
        }
    }

    // ── Mouse handling ────────────────────────────────────────────────────────

    /// <summary>
    /// Handle a mouse event for this tree.
    /// <para>
    /// The hit data (<c>ulong</c>) encodes the flattened visible row index. When the
    /// tree renders with a hit id, each visible row registers <see cref="HitRegionKind.Content"/>
    /// with <c>data = visible_row_index</c>.
    /// </para>
    /// <para>
    /// Clicking a parent node (one with children) toggles its expanded state
    /// and returns <see cref="MouseResult.Activated"/>. Clicking a leaf returns <see cref="MouseResult.Selected"/>.
    /// </para>
    /// </summary>
    /// <param name="event_">The mouse event from the terminal.</param>
    /// <param name="hit">Result of frame hit-test at (event.x, event.y), if available.</param>
    /// <param name="expectedId">The <see cref="HitId"/> this tree was rendered with.</param>
    public MouseResult HandleMouse(MouseEvent event_, (HitId id, HitRegionKind region, ulong data)? hit, HitId expectedId)
    {
        if (event_.Kind is MouseEventKind.Down down && down.Button == MouseButton.Left)
        {
            if (hit is { } h && h.id == expectedId && h.region == HitRegionKind.Content)
            {
                int index = (int)h.data;
                var node = NodeAtVisibleIndexMut(index);
                if (node != null && !node.HasChildren)
                    return MouseResult.Selected(index);
                if (ToggleNodeAtVisibleIndex(index, "mouse"))
                    return MouseResult.Activated(index);
            }
        }
        return MouseResult.Ignored;
    }

    // ── Path finding ──────────────────────────────────────────────────────────

    int[]? FindPathIndicesAtVisibleIndex(int target)
    {
        int counter = 0;
        var path = new List<int>();

        string? query = _searchQuery?.Trim();
        if (!string.IsNullOrEmpty(query))
        {
            string queryLower = query.ToLowerInvariant();
            var result = FilterNodePaths(_root, queryLower);
            if (result == null) return null;
            var fp = new FilteredPathNode { Expanded = result.Value.expanded, Children = result.Value.children };

            if (_showRoot)
                return WalkFilteredPath(fp, target, ref counter, path);
            else if (fp.Expanded)
            {
                foreach (var (idx, child) in fp.Children)
                {
                    path.Add(idx);
                    var found = WalkFilteredPath(child, target, ref counter, path);
                    if (found != null) return found;
                    path.RemoveAt(path.Count - 1);
                }
            }
            return null;
        }
        else
        {
            if (_showRoot)
                return WalkVisibleIndexPath(_root, target, ref counter, path);
            else if (_root._expanded)
            {
                for (int i = 0; i < _root._children.Count; i++)
                {
                    path.Add(i);
                    var found = WalkVisibleIndexPath(_root._children[i], target, ref counter, path);
                    if (found != null) return found;
                    path.RemoveAt(path.Count - 1);
                }
            }
            return null;
        }
    }

    static int[]? WalkFilteredPath(FilteredPathNode node, int target, ref int counter, List<int> currentPath)
    {
        if (counter == target) return currentPath.ToArray();
        counter++;
        if (node.Expanded)
        {
            foreach (var (idx, child) in node.Children)
            {
                currentPath.Add(idx);
                var found = WalkFilteredPath(child, target, ref counter, currentPath);
                if (found != null) return found;
                currentPath.RemoveAt(currentPath.Count - 1);
            }
        }
        return null;
    }

    static int[]? WalkVisibleIndexPath(TreeNode node, int target, ref int counter, List<int> currentPath)
    {
        if (counter == target) return currentPath.ToArray();
        counter++;
        if (node._expanded)
        {
            for (int i = 0; i < node._children.Count; i++)
            {
                currentPath.Add(i);
                var found = WalkVisibleIndexPath(node._children[i], target, ref counter, currentPath);
                if (found != null) return found;
                currentPath.RemoveAt(currentPath.Count - 1);
            }
        }
        return null;
    }

    // ── Filter helpers ────────────────────────────────────────────────────────

    static bool ContainsIgnoreCase(string text, string queryLower) =>
        CultureInfo.InvariantCulture.CompareInfo.IndexOf(text, queryLower, CompareOptions.IgnoreCase) >= 0;

    static TreeNode? FilterNode(TreeNode node, string queryLower)
    {
        bool labelMatches = ContainsIgnoreCase(node._label, queryLower)
            || (node._icon != null && ContainsIgnoreCase(node._icon, queryLower));

        var filteredChildren = new List<TreeNode>();
        foreach (var c in node._children)
        {
            if (FilterNode(c, queryLower) is { } f)
                filteredChildren.Add(f);
        }

        var filteredLazy = new List<TreeNode>();
        if (node._lazyChildren != null)
        {
            foreach (var c in node._lazyChildren)
            {
                if (FilterNode(c, queryLower) is { } f)
                    filteredLazy.Add(f);
            }
        }

        if (!labelMatches && filteredChildren.Count == 0 && filteredLazy.Count == 0)
            return null;

        var filtered = node.Clone();
        if (labelMatches)
        {
            // Search-mode render walks `_children`, not `_lazyChildren`. When a node
            // itself matches, render the same full subtree that path bookkeeping already
            // exposes by eagerly materializing lazy descendants in the filtered clone.
            filtered.MaterializeAllLazyChildren();
            filtered._expanded = true;
        }
        else
        {
            // Materialize filtered lazy matches into `_children` so render/flatten traversal,
            // which walks `_children`, includes lazy descendants that matched the query.
            filtered._children = filteredChildren;
            filtered._children.AddRange(filteredLazy);
            filtered._lazyChildren = null;
            filtered._expanded = true;
        }
        return filtered;
    }

    static FilteredPathNode CreateUnfilteredPathNode(TreeNode node)
    {
        var children = new List<(int, FilteredPathNode)>();
        for (int i = 0; i < node._children.Count; i++)
            children.Add((i, CreateUnfilteredPathNode(node._children[i])));
        int lazyOffset = node._children.Count;
        if (node._lazyChildren != null)
        {
            for (int i = 0; i < node._lazyChildren.Count; i++)
                children.Add((lazyOffset + i, CreateUnfilteredPathNode(node._lazyChildren[i])));
        }
        return new FilteredPathNode { Expanded = node._expanded, Children = children };
    }

    static (bool expanded, List<(int, FilteredPathNode)> children)? FilterNodePaths(TreeNode node, string queryLower)
    {
        bool labelMatches = ContainsIgnoreCase(node._label, queryLower)
            || (node._icon != null && ContainsIgnoreCase(node._icon, queryLower));

        if (labelMatches)
        {
            var children = new List<(int, FilteredPathNode)>();
            for (int i = 0; i < node._children.Count; i++)
                children.Add((i, CreateUnfilteredPathNode(node._children[i])));
            int lazyOffset = node._children.Count;
            if (node._lazyChildren != null)
            {
                for (int i = 0; i < node._lazyChildren.Count; i++)
                    children.Add((lazyOffset + i, CreateUnfilteredPathNode(node._lazyChildren[i])));
            }
            return (true, children);
        }

        var filteredChildren = new List<(int, FilteredPathNode)>();
        for (int i = 0; i < node._children.Count; i++)
        {
            var r = FilterNodePaths(node._children[i], queryLower);
            if (r != null)
                filteredChildren.Add((i, new FilteredPathNode { Expanded = r.Value.expanded, Children = r.Value.children }));
        }

        var filteredLazy = new List<(int, FilteredPathNode)>();
        int lzyOffset = node._children.Count;
        if (node._lazyChildren != null)
        {
            for (int i = 0; i < node._lazyChildren.Count; i++)
            {
                var r = FilterNodePaths(node._lazyChildren[i], queryLower);
                if (r != null)
                    filteredLazy.Add((lzyOffset + i, new FilteredPathNode { Expanded = r.Value.expanded, Children = r.Value.children }));
            }
        }

        if (filteredChildren.Count == 0 && filteredLazy.Count == 0) return null;

        filteredChildren.AddRange(filteredLazy);
        return (true, filteredChildren);
    }

    // ── IWidget render ────────────────────────────────────────────────────────

    /// <summary>Render the tree widget into the given frame area.</summary>
    public void Render(Rect area, Frame frame)
    {
        if (area.Width == 0 || area.Height == 0) return;

        var deg = frame.Degradation;
        var baseStyle = deg.ApplyStyling() ? _labelStyle : WidgetStyle.Default;
        WidgetDrawing.ClearTextArea(frame, area, baseStyle);

        int currentRow = 0;
        var isLast = new List<bool>(8);

        string? searchQuery = _searchQuery?.Trim();
        bool isSearching = !string.IsNullOrEmpty(searchQuery);
        TreeNode? filteredRoot = isSearching
            ? FilterNode(_root, searchQuery!.ToLowerInvariant())
            : _root.Clone();

        if (isSearching && filteredRoot == null) return;

        var root = filteredRoot ?? _root;

        if (_showRoot)
        {
            RenderNode(root, 0, isLast, area, frame, ref currentRow, deg);
        }
        else if (root._expanded)
        {
            int childCount = root._children.Count;
            for (int i = 0; i < childCount; i++)
            {
                if (currentRow >= area.Height) break;
                isLast.Add(i == childCount - 1);
                RenderNode(root._children[i], 0, isLast, area, frame, ref currentRow, deg);
                isLast.RemoveAt(isLast.Count - 1);
            }
        }
    }

    void RenderNode(TreeNode node, int depth, List<bool> isLast, Rect area, Frame frame, ref int currentRow, DegradationLevel deg)
    {
        if (currentRow >= area.Height) return;

        ushort y = (ushort)(area.Y + currentRow);
        ushort x = area.X;
        ushort maxX = area.Right;

        // Draw guide characters for each depth level
        if (depth > 0 && deg.ApplyStyling())
        {
            for (int d = 0; d < depth; d++)
            {
                bool isLastAtDepth = d < isLast.Count && isLast[d];
                string guide = d == depth - 1
                    ? (isLastAtDepth ? _guides.Last() : _guides.Branch())
                    : (isLastAtDepth ? _guides.Space() : _guides.Vertical());
                x = WidgetDrawing.DrawTextSpan(frame, x, y, guide, _guideStyle, maxX);
            }
        }
        else if (depth > 0)
        {
            // Minimal rendering: indent with spaces
            for (int d = 0; d < depth; d++)
            {
                x = WidgetDrawing.DrawTextSpan(frame, x, y, "    ", WidgetStyle.Default, maxX);
                if (x >= maxX) break;
            }
        }

        // Draw label
        var style = depth == 0 && _showRoot ? _rootStyle : _labelStyle;
        if (node.Icon is { } icon)
        {
            var iconStyle = deg.ApplyStyling() ? style : WidgetStyle.Default;
            x = WidgetDrawing.DrawTextSpan(frame, x, y, icon, iconStyle, maxX);
            if (x < maxX)
                x = WidgetDrawing.DrawTextSpan(frame, x, y, " ", iconStyle, maxX);
        }

        if (deg.ApplyStyling())
            WidgetDrawing.DrawTextSpan(frame, x, y, node._label, style, maxX);
        else
            WidgetDrawing.DrawTextSpan(frame, x, y, node._label, WidgetStyle.Default, maxX);

        // Register hit region for the row
        if (_hitId is { } hitId)
        {
            var rowArea = new Rect(area.X, y, area.Width, 1);
            frame.RegisterHit(rowArea, hitId, HitRegionKind.Content, (ulong)currentRow);
        }

        currentRow++;
        if (!node._expanded) return;

        int childCount = node._children.Count;
        for (int i = 0; i < childCount; i++)
        {
            if (currentRow >= area.Height) break;
            isLast.Add(i == childCount - 1);
            RenderNode(node._children[i], depth + 1, isLast, area, frame, ref currentRow, deg);
            isLast.RemoveAt(isLast.Count - 1);
        }
    }

    /// <inheritdoc/>
    public bool IsEssential() => false;

    // ── Test-only flatten helpers ─────────────────────────────────────────────
    // These mirror the #[cfg(test)] helpers in tree.rs.

    internal List<FlatNode> Flatten()
    {
        var out_ = new List<FlatNode>();
        string? searchQuery = _searchQuery?.Trim();
        bool isSearching = !string.IsNullOrEmpty(searchQuery);
        TreeNode? filteredRoot = isSearching
            ? FilterNode(_root, searchQuery!.ToLowerInvariant())
            : _root.Clone();

        if (isSearching && filteredRoot == null) return out_;

        var root = filteredRoot ?? _root;
        if (_showRoot)
        {
            FlattenVisible(root, 0, out_);
        }
        else if (root._expanded)
        {
            foreach (var child in root._children)
                FlattenVisible(child, 0, out_);
        }
        return out_;
    }

    static void FlattenVisible(TreeNode node, int depth, List<FlatNode> out_)
    {
        out_.Add(new FlatNode(node._label, depth));
        if (node._expanded)
        {
            foreach (var child in node._children)
                FlattenVisible(child, depth + 1, out_);
        }
    }

    /// <summary>Internal test helper: a flattened (label, depth) pair.</summary>
    internal sealed class FlatNode : IEquatable<FlatNode>
    {
        public string Label { get; }
        public int Depth { get; }
        public FlatNode(string label, int depth) { Label = label; Depth = depth; }
        public bool Equals(FlatNode? other) => other != null && Label == other.Label && Depth == other.Depth;
        public override bool Equals(object? obj) => obj is FlatNode f && Equals(f);
        public override int GetHashCode() => HashCode.Combine(Label, Depth);
    }
}
