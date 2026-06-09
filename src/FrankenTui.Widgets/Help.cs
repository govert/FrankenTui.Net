// Port of .external/frankentui/crates/ftui-widgets/src/help.rs
// Help widget for displaying keybinding lists in short (inline) and full (aligned) modes.

using FrankenTui.Core;
using FrankenTui.Render;
using Buffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Widgets;

// ── HelpCategory ────────────────────────────────────────────────────────────

/// <summary>Category for organizing help entries into logical groups.</summary>
public abstract class HelpCategory : IEquatable<HelpCategory>
{
    private HelpCategory() { }

    /// <summary>General/uncategorized keybinding.</summary>
    public static readonly HelpCategory General = new GeneralVariant();
    /// <summary>Navigation keys (arrows, page up/down, home/end, etc.).</summary>
    public static readonly HelpCategory Navigation = new NavigationVariant();
    /// <summary>Editing actions (cut, copy, paste, undo, redo, etc.).</summary>
    public static readonly HelpCategory Editing = new EditingVariant();
    /// <summary>File operations (save, open, close, new, etc.).</summary>
    public static readonly HelpCategory File = new FileVariant();
    /// <summary>View/display controls (zoom, scroll, toggle panels, etc.).</summary>
    public static readonly HelpCategory View = new ViewVariant();
    /// <summary>Application-level shortcuts (quit, settings, help, etc.).</summary>
    public static readonly HelpCategory Global = new GlobalVariant();

    /// <summary>Custom category with a user-defined label.</summary>
    public static HelpCategory Custom(string label) => new CustomVariant(label);

    /// <summary>Return a display label for this category.</summary>
    public abstract string Label();

    public abstract bool Equals(HelpCategory? other);
    public override bool Equals(object? obj) => obj is HelpCategory h && Equals(h);
    public abstract override int GetHashCode();
    public static bool operator ==(HelpCategory? a, HelpCategory? b) =>
        a is null ? b is null : a.Equals(b);
    public static bool operator !=(HelpCategory? a, HelpCategory? b) => !(a == b);

    private sealed class GeneralVariant : HelpCategory
    {
        public override string Label() => "General";
        public override bool Equals(HelpCategory? other) => other is GeneralVariant;
        public override int GetHashCode() => HashCode.Combine(0);
        public override string ToString() => "General";
    }
    private sealed class NavigationVariant : HelpCategory
    {
        public override string Label() => "Navigation";
        public override bool Equals(HelpCategory? other) => other is NavigationVariant;
        public override int GetHashCode() => HashCode.Combine(1);
        public override string ToString() => "Navigation";
    }
    private sealed class EditingVariant : HelpCategory
    {
        public override string Label() => "Editing";
        public override bool Equals(HelpCategory? other) => other is EditingVariant;
        public override int GetHashCode() => HashCode.Combine(2);
        public override string ToString() => "Editing";
    }
    private sealed class FileVariant : HelpCategory
    {
        public override string Label() => "File";
        public override bool Equals(HelpCategory? other) => other is FileVariant;
        public override int GetHashCode() => HashCode.Combine(3);
        public override string ToString() => "File";
    }
    private sealed class ViewVariant : HelpCategory
    {
        public override string Label() => "View";
        public override bool Equals(HelpCategory? other) => other is ViewVariant;
        public override int GetHashCode() => HashCode.Combine(4);
        public override string ToString() => "View";
    }
    private sealed class GlobalVariant : HelpCategory
    {
        public override string Label() => "Global";
        public override bool Equals(HelpCategory? other) => other is GlobalVariant;
        public override int GetHashCode() => HashCode.Combine(5);
        public override string ToString() => "Global";
    }
    internal sealed class CustomVariant : HelpCategory
    {
        internal readonly string _label;
        internal CustomVariant(string label) => _label = label;
        public override string Label() => _label;
        public override bool Equals(HelpCategory? other) =>
            other is CustomVariant c && c._label == _label;
        public override int GetHashCode() => HashCode.Combine(6, _label);
        public override string ToString() => $"Custom({_label})";
    }
}

// ── HelpEntry ───────────────────────────────────────────────────────────────

/// <summary>A single keybinding entry in the help view.</summary>
public sealed class HelpEntry : IEquatable<HelpEntry>
{
    /// <summary>The key or key combination (e.g. "^C", "↑/k").</summary>
    public string Key = "";
    /// <summary>Description of what the key does.</summary>
    public string Desc = "";
    /// <summary>Whether this entry is enabled (disabled entries are hidden).</summary>
    public bool Enabled = true;
    /// <summary>Category for grouping related entries.</summary>
    public HelpCategory Category = HelpCategory.General;

    /// <summary>Construct a default help entry.</summary>
    public HelpEntry() { }

    /// <summary>Construct a new enabled help entry directly.</summary>
    public HelpEntry(string key, string desc) { Key = key; Desc = desc; }

    /// <summary>Create a new enabled help entry.</summary>
    public static HelpEntry New(string key, string desc) =>
        new() { Key = key, Desc = desc, Enabled = true, Category = HelpCategory.General };

    /// <summary>Set whether this entry is enabled.</summary>
    public HelpEntry WithEnabled(bool enabled) { Enabled = enabled; return this; }

    /// <summary>Set the category for this entry.</summary>
    public HelpEntry WithCategory(HelpCategory category) { Category = category; return this; }

    public bool Equals(HelpEntry? other) =>
        other is not null && Key == other.Key && Desc == other.Desc &&
        Enabled == other.Enabled && Category == other.Category;

    public override bool Equals(object? obj) => obj is HelpEntry e && Equals(e);
    public override int GetHashCode() => HashCode.Combine(Key, Desc, Enabled, Category.GetHashCode());
    public override string ToString() =>
        $"HelpEntry {{ key = {Key}, desc = {Desc}, enabled = {Enabled} }}";
}

// ── HelpMode ────────────────────────────────────────────────────────────────

/// <summary>Display mode for the help widget.</summary>
public enum HelpMode
{
    /// <summary>Short inline mode: entries separated by a bullet on one line.</summary>
    Short,
    /// <summary>Full mode: entries stacked vertically with aligned columns.</summary>
    Full,
}

// ── HelpCacheStats ──────────────────────────────────────────────────────────

/// <summary>Cache hit/miss statistics for <see cref="HelpRenderState"/>.</summary>
public struct HelpCacheStats : IEquatable<HelpCacheStats>
{
    public ulong Hits;
    public ulong Misses;
    public ulong DirtyUpdates;
    public ulong LayoutRebuilds;

    public bool Equals(HelpCacheStats other) =>
        Hits == other.Hits && Misses == other.Misses &&
        DirtyUpdates == other.DirtyUpdates && LayoutRebuilds == other.LayoutRebuilds;

    public override bool Equals(object? obj) => obj is HelpCacheStats s && Equals(s);
    public override int GetHashCode() =>
        HashCode.Combine(Hits, Misses, DirtyUpdates, LayoutRebuilds);
    public static bool operator ==(HelpCacheStats a, HelpCacheStats b) => a.Equals(b);
    public static bool operator !=(HelpCacheStats a, HelpCacheStats b) => !a.Equals(b);
    public override string ToString() =>
        $"HelpCacheStats {{ hits = {Hits}, misses = {Misses}, " +
        $"dirty_updates = {DirtyUpdates}, layout_rebuilds = {LayoutRebuilds} }}";
}

// ── HelpRenderState ─────────────────────────────────────────────────────────

/// <summary>
/// Cached render state for <see cref="Help"/>, enabling incremental layout reuse and
/// dirty-rect updates for keybinding hint panels.
/// <para>Invariants:</para>
/// <list type="bullet">
/// <item>Layout is reused only when entry count and slot widths remain compatible.</item>
/// <item>Dirty rects always cover the full prior slot width for changed entries.</item>
/// <item>Layout rebuilds on any change that could cause reflow.</item>
/// </list>
/// <para>Failure Modes:</para>
/// <list type="bullet">
/// <item>If a changed entry exceeds its cached slot width, we rebuild the layout.</item>
/// <item>If enabled entry count changes, we rebuild the layout.</item>
/// </list>
/// </summary>
public sealed class HelpRenderState
{
    internal HelpCache? Cache;
    internal readonly List<int> EnabledIndices = new();
    internal readonly List<int> DirtyIndices = new();
    internal readonly List<Rect> DirtyRectsList = new();
    internal HelpCacheStats CacheStats;

    /// <summary>Return cache statistics.</summary>
    public HelpCacheStats Stats() => CacheStats;

    /// <summary>Clear recorded dirty rects.</summary>
    public void ClearDirtyRects() => DirtyRectsList.Clear();

    /// <summary>Take dirty rects for logging/inspection.</summary>
    public List<Rect> TakeDirtyRects()
    {
        var taken = new List<Rect>(DirtyRectsList);
        DirtyRectsList.Clear();
        return taken;
    }

    /// <summary>Read dirty rects without clearing.</summary>
    public IReadOnlyList<Rect> DirtyRects() => DirtyRectsList;

    /// <summary>Reset cache stats (useful for perf logging).</summary>
    public void ResetStats() => CacheStats = default;
}

// ── Internal cache types ────────────────────────────────────────────────────

internal sealed class HelpCache
{
    public Buffer Buffer;
    public HelpLayout Layout;
    public LayoutKey Key;
    public readonly List<ulong> EntryHashes;
    public int EnabledCount;

    public HelpCache(Buffer buffer, HelpLayout layout, LayoutKey key,
        List<ulong> entryHashes, int enabledCount)
    {
        Buffer = buffer;
        Layout = layout;
        Key = key;
        EntryHashes = entryHashes;
        EnabledCount = enabledCount;
    }
}

internal sealed class HelpLayout
{
    public HelpMode Mode;
    public ushort Width;
    public readonly List<EntrySlot> Entries = new();
    public EllipsisSlot? Ellipsis;
    public int MaxKeyWidth;
    public int SeparatorWidth;
}

internal struct EntrySlot
{
    public ushort X;
    public ushort Y;
    public ushort Width;
    public int KeyWidth;
}

internal struct EllipsisSlot
{
    public ushort X;
    public ushort Width;
    public bool PrefixSpace;
}

internal readonly struct StyleKey : IEquatable<StyleKey>
{
    public readonly PackedRgba? Fg;
    public readonly PackedRgba? Bg;
    public readonly CellStyleFlags? Attrs;

    public StyleKey(PackedRgba? fg, PackedRgba? bg, CellStyleFlags? attrs)
    {
        Fg = fg; Bg = bg; Attrs = attrs;
    }

    public static StyleKey From(WidgetStyle style) =>
        new(style.Fg, style.Bg, style.Attrs);

    public bool Equals(StyleKey other) =>
        Fg == other.Fg && Bg == other.Bg && Attrs == other.Attrs;

    public override bool Equals(object? obj) => obj is StyleKey s && Equals(s);
    public override int GetHashCode() => HashCode.Combine(Fg, Bg, Attrs);
}

internal readonly struct LayoutKey : IEquatable<LayoutKey>
{
    public readonly HelpMode Mode;
    public readonly ushort Width;
    public readonly ushort Height;
    public readonly ulong SeparatorHash;
    public readonly ulong EllipsisHash;
    public readonly StyleKey KeyStyle;
    public readonly StyleKey DescStyle;
    public readonly StyleKey SeparatorStyle;
    public readonly DegradationLevel Degradation;

    public LayoutKey(
        HelpMode mode, ushort width, ushort height,
        ulong separatorHash, ulong ellipsisHash,
        StyleKey keyStyle, StyleKey descStyle, StyleKey separatorStyle,
        DegradationLevel degradation)
    {
        Mode = mode; Width = width; Height = height;
        SeparatorHash = separatorHash; EllipsisHash = ellipsisHash;
        KeyStyle = keyStyle; DescStyle = descStyle; SeparatorStyle = separatorStyle;
        Degradation = degradation;
    }

    public bool Equals(LayoutKey other) =>
        Mode == other.Mode && Width == other.Width && Height == other.Height &&
        SeparatorHash == other.SeparatorHash && EllipsisHash == other.EllipsisHash &&
        KeyStyle.Equals(other.KeyStyle) && DescStyle.Equals(other.DescStyle) &&
        SeparatorStyle.Equals(other.SeparatorStyle) && Degradation == other.Degradation;

    public override bool Equals(object? obj) => obj is LayoutKey k && Equals(k);
    public override int GetHashCode() =>
        HashCode.Combine(
            HashCode.Combine(Mode, Width, Height, SeparatorHash, EllipsisHash),
            KeyStyle.GetHashCode(), DescStyle.GetHashCode(),
            HashCode.Combine(SeparatorStyle.GetHashCode(), (int)Degradation));
    public static bool operator ==(LayoutKey left, LayoutKey right) => left.Equals(right);
    public static bool operator !=(LayoutKey left, LayoutKey right) => !left.Equals(right);
}

// ── Help widget ─────────────────────────────────────────────────────────────

/// <summary>
/// Help widget that renders keybinding entries.
/// <para>
/// In <see cref="HelpMode.Short"/> mode, entries are shown inline separated by a bullet
/// character, truncated with an ellipsis if they exceed the available width.
/// </para>
/// <para>
/// In <see cref="HelpMode.Full"/> mode, entries are rendered in a vertical list with
/// keys and descriptions in aligned columns.
/// </para>
/// </summary>
public sealed class Help : IWidget, IStatefulWidget<HelpRenderState>
{
    internal readonly List<HelpEntry> _entries;
    HelpMode _mode;
    /// <summary>Separator between entries in short mode.</summary>
    internal string _separator;
    /// <summary>Ellipsis shown when truncated.</summary>
    internal string _ellipsis;
    /// <summary>Style for key text.</summary>
    WidgetStyle _keyStyle;
    /// <summary>Style for description text.</summary>
    WidgetStyle _descStyle;
    /// <summary>Style for separator/ellipsis.</summary>
    WidgetStyle _separatorStyle;

    /// <summary>Create a new help widget with no entries.</summary>
    public Help()
    {
        _entries = new List<HelpEntry>();
        _mode = HelpMode.Short;
        _separator = " • ";  // " • "
        _ellipsis = "…";      // "…"
        _keyStyle = new WidgetStyle(null, null, CellStyleFlags.Bold);
        _descStyle = WidgetStyle.Default;
        _separatorStyle = WidgetStyle.Default;
    }

    /// <summary>Add an entry to the help widget.</summary>
    public Help Entry(string key, string desc)
    {
        _entries.Add(HelpEntry.New(key, desc));
        return this;
    }

    /// <summary>Add a pre-built entry.</summary>
    public Help WithEntry(HelpEntry entry)
    {
        _entries.Add(entry);
        return this;
    }

    /// <summary>Set all entries at once.</summary>
    public Help WithEntries(List<HelpEntry> entries)
    {
        _entries.Clear();
        _entries.AddRange(entries);
        return this;
    }

    /// <summary>Set the display mode.</summary>
    public Help WithMode(HelpMode mode) { _mode = mode; return this; }

    /// <summary>Set the separator used between entries in short mode.</summary>
    public Help WithSeparator(string sep) { _separator = sep; return this; }

    /// <summary>Set the ellipsis string.</summary>
    public Help WithEllipsis(string ellipsis) { _ellipsis = ellipsis; return this; }

    /// <summary>Set the style for key text.</summary>
    public Help WithKeyStyle(WidgetStyle style) { _keyStyle = style; return this; }

    /// <summary>Set the style for description text.</summary>
    public Help WithDescStyle(WidgetStyle style) { _descStyle = style; return this; }

    /// <summary>Set the style for separators and ellipsis.</summary>
    public Help WithSeparatorStyle(WidgetStyle style) { _separatorStyle = style; return this; }

    /// <summary>Get the entries.</summary>
    public IReadOnlyList<HelpEntry> Entries() => _entries;

    /// <summary>Get the current mode.</summary>
    public HelpMode Mode() => _mode;

    /// <summary>Toggle between short and full mode.</summary>
    public void ToggleMode()
    {
        _mode = _mode == HelpMode.Short ? HelpMode.Full : HelpMode.Short;
    }

    /// <summary>Add an entry mutably.</summary>
    public void PushEntry(HelpEntry entry) => _entries.Add(entry);

    /// <summary>Collect the enabled entries.</summary>
    internal List<HelpEntry> EnabledEntries()
    {
        var result = new List<HelpEntry>();
        foreach (var e in _entries)
            if (e.Enabled) result.Add(e);
        return result;
    }

    // ── render_short ─────────────────────────────────────────────────────────

    /// <summary>Render short mode: entries inline on one line.</summary>
    void RenderShort(Rect area, Frame frame)
    {
        var entries = EnabledEntries();
        if (entries.Count == 0 || area.Width == 0 || area.Height == 0)
            return;

        var deg = frame.Degradation;
        int sepWidth = DisplayWidth(_separator);
        int ellipsisWidth = DisplayWidth(_ellipsis);
        ushort maxX = area.Right;
        ushort y = area.Y;
        ushort x = area.X;

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry.Key.Length == 0 && entry.Desc.Length == 0)
                continue;

            // Separator before non-first items
            int sepW = i > 0 ? sepWidth : 0;

            // Calculate item width: key + " " + desc
            int keyW = DisplayWidth(entry.Key);
            int descW = DisplayWidth(entry.Desc);
            int itemW = keyW + 1 + descW;
            int totalItemW = sepW + itemW;

            // Check if this item fits, accounting for possible ellipsis
            int spaceLeft = (int)maxX - (int)x;
            if (spaceLeft < 0) spaceLeft = 0;

            if (totalItemW > spaceLeft)
            {
                // Try to fit ellipsis
                int ellTotal = i > 0 ? 1 + ellipsisWidth : ellipsisWidth;
                if (ellTotal <= spaceLeft)
                {
                    var ellipsisStyle = deg.ApplyStyling() ? _separatorStyle : WidgetStyle.Default;
                    if (i > 0)
                        x = WidgetDrawing.DrawTextSpan(frame, x, y, " ", ellipsisStyle, maxX);
                    WidgetDrawing.DrawTextSpan(frame, x, y, _ellipsis, ellipsisStyle, maxX);
                }
                break;
            }

            // Draw separator
            if (i > 0)
            {
                if (deg.ApplyStyling())
                    x = WidgetDrawing.DrawTextSpan(frame, x, y, _separator, _separatorStyle, maxX);
                else
                    x = WidgetDrawing.DrawTextSpan(frame, x, y, _separator, WidgetStyle.Default, maxX);
            }

            // Draw key and description
            if (deg.ApplyStyling())
            {
                x = WidgetDrawing.DrawTextSpan(frame, x, y, entry.Key, _keyStyle, maxX);
                x = WidgetDrawing.DrawTextSpan(frame, x, y, " ", _descStyle, maxX);
                x = WidgetDrawing.DrawTextSpan(frame, x, y, entry.Desc, _descStyle, maxX);
            }
            else
            {
                string text = $"{entry.Key} {entry.Desc}";
                x = WidgetDrawing.DrawTextSpan(frame, x, y, text, WidgetStyle.Default, maxX);
            }
        }
    }

    // ── render_full ──────────────────────────────────────────────────────────

    /// <summary>Render full mode: entries stacked vertically with aligned columns.</summary>
    void RenderFull(Rect area, Frame frame)
    {
        var entries = EnabledEntries();
        if (entries.Count == 0 || area.Width == 0 || area.Height == 0)
            return;

        var deg = frame.Degradation;

        // Find max key width for alignment
        int maxKeyW = 0;
        foreach (var e in entries)
        {
            if (e.Key.Length > 0 || e.Desc.Length > 0)
                maxKeyW = Math.Max(maxKeyW, DisplayWidth(e.Key));
        }

        ushort maxX = area.Right;
        ushort row = 0;
        var keyStyle = deg.ApplyStyling() ? _keyStyle : WidgetStyle.Default;
        var descStyle = deg.ApplyStyling() ? _descStyle : WidgetStyle.Default;

        foreach (var entry in entries)
        {
            if (entry.Key.Length == 0 && entry.Desc.Length == 0)
                continue;
            if (row >= area.Height)
                break;

            ushort y = (ushort)(area.Y + row);
            ushort x = area.X;
            int keyW = DisplayWidth(entry.Key);
            x = WidgetDrawing.DrawTextSpan(frame, x, y, entry.Key, keyStyle, maxX);
            int pad = maxKeyW > keyW ? maxKeyW - keyW : 0;
            for (int p = 0; p < pad; p++)
                x = WidgetDrawing.DrawTextSpan(frame, x, y, " ", WidgetStyle.Default, maxX);
            x = WidgetDrawing.DrawTextSpan(frame, x, y, "  ", WidgetStyle.Default, maxX);
            WidgetDrawing.DrawTextSpan(frame, x, y, entry.Desc, descStyle, maxX);

            row++;
        }
    }

    // ── entry_hash / hash_str ────────────────────────────────────────────────

    internal static ulong EntryHash(HelpEntry entry)
    {
        var h = new HelpFnvHasher();
        h.WriteString(entry.Key);
        h.WriteString(entry.Desc);
        h.WriteBool(entry.Enabled);
        h.WriteInt(entry.Category.GetHashCode());
        return h.Finish();
    }

    internal static ulong HashStr(string value)
    {
        var h = new HelpFnvHasher();
        h.WriteString(value);
        return h.Finish();
    }

    // ── layout_key ───────────────────────────────────────────────────────────

    internal LayoutKey GetLayoutKey(Rect area, DegradationLevel degradation) =>
        new LayoutKey(
            _mode, area.Width, area.Height,
            HashStr(_separator), HashStr(_ellipsis),
            StyleKey.From(_keyStyle), StyleKey.From(_descStyle),
            StyleKey.From(_separatorStyle), degradation);

    // ── build_layout / build_short_layout / build_full_layout ────────────────

    internal HelpLayout BuildLayout(Rect area) =>
        _mode == HelpMode.Short ? BuildShortLayout(area) : BuildFullLayout(area);

    internal HelpLayout BuildShortLayout(Rect area)
    {
        var layout = new HelpLayout { Mode = HelpMode.Short, Width = area.Width };
        int sepWidth = DisplayWidth(_separator);
        int ellipsisWidth = DisplayWidth(_ellipsis);
        ushort maxX = area.Width;
        ushort x = 0;
        bool first = true;

        foreach (var entry in _entries)
        {
            if (!entry.Enabled || (entry.Key.Length == 0 && entry.Desc.Length == 0))
                continue;

            int keyWidth = DisplayWidth(entry.Key);
            int descWidth = DisplayWidth(entry.Desc);
            int itemWidth = keyWidth + 1 + descWidth;
            int totalWidth = first ? itemWidth : sepWidth + itemWidth;
            int spaceLeft = (int)maxX - (int)x;
            if (spaceLeft < 0) spaceLeft = 0;

            if (totalWidth > spaceLeft)
            {
                int ellTotal = first ? ellipsisWidth : 1 + ellipsisWidth;
                if (ellTotal <= spaceLeft)
                {
                    layout.Ellipsis = new EllipsisSlot
                    {
                        X = x,
                        Width = (ushort)ellTotal,
                        PrefixSpace = !first
                    };
                }
                break;
            }

            layout.Entries.Add(new EntrySlot
            {
                X = x,
                Y = 0,
                Width = (ushort)totalWidth,
                KeyWidth = keyWidth
            });
            x = (ushort)(x + totalWidth);
            first = false;
        }

        layout.SeparatorWidth = sepWidth;
        return layout;
    }

    internal HelpLayout BuildFullLayout(Rect area)
    {
        var layout = new HelpLayout { Mode = HelpMode.Full, Width = area.Width };
        int maxKeyWidth = 0;
        foreach (var entry in _entries)
        {
            if (entry.Enabled && (entry.Key.Length > 0 || entry.Desc.Length > 0))
                maxKeyWidth = Math.Max(maxKeyWidth, DisplayWidth(entry.Key));
        }

        ushort row = 0;
        foreach (var entry in _entries)
        {
            if (!entry.Enabled || (entry.Key.Length == 0 && entry.Desc.Length == 0))
                continue;
            if (row >= area.Height)
                break;
            int keyWidth = DisplayWidth(entry.Key);
            int descWidth = DisplayWidth(entry.Desc);
            int entryWidth = SatAdd(maxKeyWidth, SatAdd(2, descWidth));
            ushort slotWidth = (ushort)Math.Min(entryWidth, area.Width);
            layout.Entries.Add(new EntrySlot
            {
                X = 0,
                Y = row,
                Width = slotWidth,
                KeyWidth = keyWidth
            });
            row = (ushort)(row + 1);
        }

        layout.MaxKeyWidth = maxKeyWidth;
        return layout;
    }

    // ── render_cached / render_short_cached / render_full_cached ────────────

    internal void RenderCached(Rect area, Frame frame, HelpLayout layout)
    {
        if (layout.Mode == HelpMode.Short)
            RenderShortCached(area, frame, layout);
        else
            RenderFullCached(area, frame, layout);
    }

    void RenderShortCached(Rect area, Frame frame, HelpLayout layout)
    {
        if (layout.Entries.Count == 0 || area.Width == 0 || area.Height == 0)
            return;

        var deg = frame.Degradation;
        ushort maxX = area.Right;
        var enabledIter = new HelpEnabledIter(_entries);

        for (int idx = 0; idx < layout.Entries.Count; idx++)
        {
            var slot = layout.Entries[idx];
            if (!enabledIter.MoveNext()) break;
            var entry = enabledIter.Current;

            ushort x = (ushort)(area.X + slot.X);
            ushort y = (ushort)(area.Y + slot.Y);

            if (idx > 0)
            {
                var sepStyle = deg.ApplyStyling() ? _separatorStyle : WidgetStyle.Default;
                x = WidgetDrawing.DrawTextSpan(frame, x, y, _separator, sepStyle, maxX);
            }

            var keyStyle = deg.ApplyStyling() ? _keyStyle : WidgetStyle.Default;
            var descStyle = deg.ApplyStyling() ? _descStyle : WidgetStyle.Default;

            x = WidgetDrawing.DrawTextSpan(frame, x, y, entry.Key, keyStyle, maxX);
            x = WidgetDrawing.DrawTextSpan(frame, x, y, " ", descStyle, maxX);
            WidgetDrawing.DrawTextSpan(frame, x, y, entry.Desc, descStyle, maxX);
        }

        if (layout.Ellipsis is { } ellipsis)
        {
            ushort y = area.Y;
            ushort x = (ushort)(area.X + ellipsis.X);
            var ellipsisStyle = deg.ApplyStyling() ? _separatorStyle : WidgetStyle.Default;
            if (ellipsis.PrefixSpace)
                x = WidgetDrawing.DrawTextSpan(frame, x, y, " ", ellipsisStyle, maxX);
            WidgetDrawing.DrawTextSpan(frame, x, y, _ellipsis, ellipsisStyle, maxX);
        }
    }

    void RenderFullCached(Rect area, Frame frame, HelpLayout layout)
    {
        if (layout.Entries.Count == 0 || area.Width == 0 || area.Height == 0)
            return;

        var deg = frame.Degradation;
        ushort maxX = area.Right;
        var enabledIter = new HelpEnabledIter(_entries);

        for (int idx = 0; idx < layout.Entries.Count; idx++)
        {
            var slot = layout.Entries[idx];
            if (!enabledIter.MoveNext()) break;
            var entry = enabledIter.Current;

            ushort y = (ushort)(area.Y + slot.Y);
            ushort x = (ushort)(area.X + slot.X);

            var keyStyle = deg.ApplyStyling() ? _keyStyle : WidgetStyle.Default;
            var descStyle = deg.ApplyStyling() ? _descStyle : WidgetStyle.Default;

            x = WidgetDrawing.DrawTextSpan(frame, x, y, entry.Key, keyStyle, maxX);
            int pad = layout.MaxKeyWidth - slot.KeyWidth;
            for (int p = 0; p < pad; p++)
                x = WidgetDrawing.DrawTextSpan(frame, x, y, " ", WidgetStyle.Default, maxX);
            x = WidgetDrawing.DrawTextSpan(frame, x, y, "  ", WidgetStyle.Default, maxX);
            WidgetDrawing.DrawTextSpan(frame, x, y, entry.Desc, descStyle, maxX);
        }
    }

    // ── render_short_entry / render_full_entry ───────────────────────────────

    void RenderShortEntry(EntrySlot slot, HelpEntry entry, Frame frame)
    {
        var deg = frame.Degradation;
        ushort maxX = (ushort)(slot.X + slot.Width);

        var rect = new Rect(slot.X, slot.Y, slot.Width, 1);
        frame.Buffer.Fill(rect, Cell.Empty);

        ushort x = slot.X;
        if (slot.X > 0)
        {
            var sepStyle = deg.ApplyStyling() ? _separatorStyle : WidgetStyle.Default;
            x = WidgetDrawing.DrawTextSpan(frame, x, slot.Y, _separator, sepStyle, maxX);
        }

        var keyStyle = deg.ApplyStyling() ? _keyStyle : WidgetStyle.Default;
        var descStyle = deg.ApplyStyling() ? _descStyle : WidgetStyle.Default;

        x = WidgetDrawing.DrawTextSpan(frame, x, slot.Y, entry.Key, keyStyle, maxX);
        x = WidgetDrawing.DrawTextSpan(frame, x, slot.Y, " ", descStyle, maxX);
        WidgetDrawing.DrawTextSpan(frame, x, slot.Y, entry.Desc, descStyle, maxX);
    }

    void RenderFullEntry(EntrySlot slot, HelpEntry entry, HelpLayout layout, Frame frame)
    {
        var deg = frame.Degradation;
        ushort maxX = (ushort)(slot.X + slot.Width);

        var rect = new Rect(slot.X, slot.Y, slot.Width, 1);
        frame.Buffer.Fill(rect, Cell.Empty);

        ushort x = slot.X;
        var keyStyle = deg.ApplyStyling() ? _keyStyle : WidgetStyle.Default;
        var descStyle = deg.ApplyStyling() ? _descStyle : WidgetStyle.Default;

        x = WidgetDrawing.DrawTextSpan(frame, x, slot.Y, entry.Key, keyStyle, maxX);
        int pad = layout.MaxKeyWidth - slot.KeyWidth;
        for (int p = 0; p < pad; p++)
            x = WidgetDrawing.DrawTextSpan(frame, x, slot.Y, " ", WidgetStyle.Default, maxX);
        x = WidgetDrawing.DrawTextSpan(frame, x, slot.Y, "  ", WidgetStyle.Default, maxX);
        WidgetDrawing.DrawTextSpan(frame, x, slot.Y, entry.Desc, descStyle, maxX);
    }

    // ── IWidget impl ─────────────────────────────────────────────────────────

    public void Render(Rect area, Frame frame)
    {
        if (area.IsEmpty || area.Width == 0 || area.Height == 0)
            return;

        WidgetDrawing.ClearTextArea(frame, area, WidgetStyle.Default);

        if (_mode == HelpMode.Short)
            RenderShort(area, frame);
        else
            RenderFull(area, frame);
    }

    public bool IsEssential() => false;

    // ── IStatefulWidget<HelpRenderState> impl ────────────────────────────────

    public void Render(Rect area, Frame frame, HelpRenderState state)
    {
        if (area.IsEmpty || area.Width == 0 || area.Height == 0)
        {
            state.Cache = null;
            state.DirtyRectsList.Clear();
            state.DirtyIndices.Clear();
            state.EnabledIndices.Clear();
            return;
        }

        state.DirtyRectsList.Clear();
        state.DirtyIndices.Clear();

        var layoutKey = GetLayoutKey(area, frame.Degradation);
        int enabledCount = CollectEnabledIndices(_entries, state.EnabledIndices);

        bool cacheMiss = state.Cache is null || state.Cache.Key != layoutKey;

        if (cacheMiss)
        {
            RebuildCache(this, area, frame, state, layoutKey, enabledCount);
            BlitCache(state.Cache, area, frame);
            return;
        }

        var cache = state.Cache!;
        if (enabledCount != cache.EnabledCount)
        {
            RebuildCache(this, area, frame, state, layoutKey, enabledCount);
            BlitCache(state.Cache, area, frame);
            return;
        }

        bool layoutChanged = false;
        int visibleCount = cache.Layout.Entries.Count;

        for (int pos = 0; pos < state.EnabledIndices.Count; pos++)
        {
            int entryIdx = state.EnabledIndices[pos];
            var entry = _entries[entryIdx];
            ulong hash = EntryHash(entry);

            if (pos >= cache.EntryHashes.Count)
            {
                layoutChanged = true;
                break;
            }

            if (hash != cache.EntryHashes[pos])
            {
                if (pos >= visibleCount || !HelpModule.EntryFitsSlot(entry, pos, cache.Layout))
                {
                    layoutChanged = true;
                    break;
                }
                cache.EntryHashes[pos] = hash;
                state.DirtyIndices.Add(pos);
            }
        }

        if (layoutChanged)
        {
            RebuildCache(this, area, frame, state, layoutKey, enabledCount);
            BlitCache(state.Cache, area, frame);
            return;
        }

        if (state.DirtyIndices.Count == 0)
        {
            state.CacheStats.Hits++;
            BlitCache(state.Cache, area, frame);
            return;
        }

        // Partial update: only changed entries are redrawn into the cached buffer.
        state.CacheStats.DirtyUpdates++;

        var cacheRef = state.Cache!;
        // DIVERGENCE: upstream does std::mem::take on the cache buffer then creates
        // Frame::from_buffer(...). In .NET we use BufferOverride to attach the existing
        // cache Buffer to a temporary Frame, then render dirty entries directly into it.
        var tmpPool = new GraphemePool();
        var cacheFrame = new Frame(area.Width, area.Height, tmpPool);
        cacheFrame.SetDegradation(frame.Degradation);
        cacheFrame.BufferOverride = cacheRef.Buffer;

        foreach (int idx in state.DirtyIndices)
        {
            if (idx < state.EnabledIndices.Count && idx < cacheRef.Layout.Entries.Count)
            {
                int entryIdx = state.EnabledIndices[idx];
                var entry = _entries[entryIdx];
                var slot = cacheRef.Layout.Entries[idx];
                if (cacheRef.Layout.Mode == HelpMode.Short)
                    RenderShortEntry(slot, entry, cacheFrame);
                else
                    RenderFullEntry(slot, entry, cacheRef.Layout, cacheFrame);
                state.DirtyRectsList.Add(new Rect(slot.X, slot.Y, slot.Width, 1));
            }
        }

        cacheRef.Buffer = cacheFrame.Buffer;

        BlitCache(state.Cache, area, frame);
    }

    // ── Static helpers (wrappers delegating to HelpModule) ───────────────────

    internal static int CollectEnabledIndices(List<HelpEntry> entries, List<int> output) =>
        HelpModule.CollectEnabledIndices(entries, output);

    internal static void RebuildCache(Help help, Rect area, Frame frame,
        HelpRenderState state, LayoutKey layoutKey, int enabledCount) =>
        HelpModule.RebuildCache(help, area, frame, state, layoutKey, enabledCount);

    internal static void BlitCache(HelpCache? cache, Rect area, Frame frame) =>
        HelpModule.BlitCache(cache, area, frame);

    // ── DisplayWidth / SatAdd ────────────────────────────────────────────────

    internal static int DisplayWidth(string s) => WidgetDrawing.GraphemeWidth(s);

    static int SatAdd(int a, int b)
    {
        long r = (long)a + b;
        return r > int.MaxValue ? int.MaxValue : (int)r;
    }
}

// ── HelpModule (module-level free functions from help.rs) ─────────────────────

/// <summary>
/// Module-level free functions from upstream help.rs:
/// collect_enabled_indices, entry_fits_slot, rebuild_cache, blit_cache.
/// DIVERGENCE: In Rust these are module-level fns. In C# they are static methods on
/// an internal helper class and are accessible from test code via this class.
/// </summary>
internal static class HelpModule
{
    public static int CollectEnabledIndices(List<HelpEntry> entries, List<int> output)
    {
        output.Clear();
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e.Enabled && (e.Key.Length > 0 || e.Desc.Length > 0))
                output.Add(i);
        }
        return output.Count;
    }

    public static bool EntryFitsSlot(HelpEntry entry, int index, HelpLayout layout)
    {
        if (layout.Mode == HelpMode.Short)
        {
            int entryWidth = Help.DisplayWidth(entry.Key) + 1 + Help.DisplayWidth(entry.Desc);
            if (index >= layout.Entries.Count) return false;
            var slot = layout.Entries[index];
            int sepWidth = layout.SeparatorWidth;
            int maxWidth = slot.X == 0
                ? slot.Width
                : (slot.Width > sepWidth ? slot.Width - sepWidth : 0);
            return entryWidth <= maxWidth;
        }
        else
        {
            int keyWidth = Help.DisplayWidth(entry.Key);
            int descWidth = Help.DisplayWidth(entry.Desc);
            int entryWidth = SatAdd(layout.MaxKeyWidth, SatAdd(2, descWidth));
            if (index >= layout.Entries.Count) return false;
            var slot = layout.Entries[index];
            if (slot.Width == layout.Width)
                return keyWidth <= layout.MaxKeyWidth;
            else
                return keyWidth <= layout.MaxKeyWidth && entryWidth <= slot.Width;
        }
    }

    public static void RebuildCache(
        Help help, Rect area, Frame frame,
        HelpRenderState state, LayoutKey layoutKey, int enabledCount)
    {
        state.CacheStats.Misses++;
        state.CacheStats.LayoutRebuilds++;

        var layoutArea = new Rect(0, 0, area.Width, area.Height);
        var layout = help.BuildLayout(layoutArea);

        var tmpPool = new GraphemePool();
        var cacheFrame = new Frame(area.Width, area.Height, tmpPool);
        cacheFrame.SetDegradation(frame.Degradation);
        help.RenderCached(layoutArea, cacheFrame, layout);

        var entryHashes = new List<ulong>(state.EnabledIndices.Count);
        foreach (int idx in state.EnabledIndices)
            entryHashes.Add(Help.EntryHash(help._entries[idx]));

        state.Cache = new HelpCache(
            cacheFrame.Buffer, layout, layoutKey, entryHashes, enabledCount);
    }

    public static void BlitCache(HelpCache? cache, Rect area, Frame frame)
    {
        if (cache is null) return;

        foreach (var slot in cache.Layout.Entries)
        {
            for (ushort c = 0; c < slot.Width; c++)
            {
                ushort sx = (ushort)(slot.X + c);
                ushort sy = slot.Y;
                var cell = cache.Buffer.Get(sx, sy);
                if (cell.HasValue)
                    frame.Buffer.SetFast((ushort)(area.X + sx), (ushort)(area.Y + sy), cell.Value);
            }
        }

        if (cache.Layout.Ellipsis is { } ellipsis)
        {
            for (ushort c = 0; c < ellipsis.Width; c++)
            {
                ushort sx = (ushort)(ellipsis.X + c);
                var cell = cache.Buffer.Get(sx, 0);
                if (cell.HasValue)
                    frame.Buffer.SetFast((ushort)(area.X + sx), area.Y, cell.Value);
            }
        }
    }

    static int SatAdd(int a, int b)
    {
        long r = (long)a + b;
        return r > int.MaxValue ? int.MaxValue : (int)r;
    }
}

// ── HelpEnabledIter (replaces Rust filter iterator) ─────────────────────────

internal struct HelpEnabledIter
{
    readonly List<HelpEntry> _entries;
    int _index;
    HelpEntry? _current;

    public HelpEnabledIter(List<HelpEntry> entries)
    {
        _entries = entries;
        _index = 0;
        _current = null;
    }

    public HelpEntry Current => _current!;

    public bool MoveNext()
    {
        while (_index < _entries.Count)
        {
            var e = _entries[_index++];
            if (e.Enabled && (e.Key.Length > 0 || e.Desc.Length > 0))
            {
                _current = e;
                return true;
            }
        }
        return false;
    }
}

// ── HelpFnvHasher ─────────────────────────────────────────────────────────────

/// <summary>
/// FNV-1a 64-bit hasher used to replicate Rust's entry_hash and hash_str.
/// DIVERGENCE: Rust uses std DefaultHasher (SipHash-1-3). .NET does not expose SipHash,
/// so FNV-1a is used for determinism. Hash values differ from Rust; relative
/// inequality/equality semantics are preserved.
/// </summary>
internal struct HelpFnvHasher
{
    ulong _state;
    const ulong OffsetBasis = 14695981039346656037UL;
    const ulong Prime = 1099511628211UL;

    public HelpFnvHasher() => _state = OffsetBasis;

    public void WriteByte(byte b)
    {
        _state ^= b;
        _state *= Prime;
    }

    public void WriteString(string s)
    {
        foreach (char c in s)
        {
            WriteByte((byte)(c & 0xFF));
            WriteByte((byte)(c >> 8));
        }
    }

    public void WriteBool(bool v) => WriteByte(v ? (byte)1 : (byte)0);

    public void WriteInt(int v)
    {
        WriteByte((byte)(v & 0xFF));
        WriteByte((byte)((v >> 8) & 0xFF));
        WriteByte((byte)((v >> 16) & 0xFF));
        WriteByte((byte)((v >> 24) & 0xFF));
    }

    public ulong Finish() => _state;
}

// ── KeyFormat ────────────────────────────────────────────────────────────────

/// <summary>Format for displaying key labels in hints.</summary>
public enum KeyFormat
{
    /// <summary>Plain key display: <c>q quit</c></summary>
    Plain,
    /// <summary>Bracketed key display: <c>[q] quit</c></summary>
    Bracketed,
}

// ── KeybindingHints ───────────────────────────────────────────────────────────

/// <summary>
/// A keybinding hints widget with category grouping and context-aware filtering.
/// <para>
/// Supports two entry scopes:
/// <list type="bullet">
/// <item><b>Global</b>: shortcuts always visible regardless of context.</item>
/// <item><b>Contextual</b>: shortcuts shown only when <see cref="WithShowContext"/> is enabled
///   (typically when a particular widget has focus).</item>
/// </list>
/// </para>
/// <para>
/// In <see cref="HelpMode.Full"/> mode with categories enabled, entries are grouped
/// under category headers. In <see cref="HelpMode.Short"/> mode, entries are rendered inline.
/// </para>
/// </summary>
public sealed class KeybindingHints : IWidget
{
    internal readonly List<HelpEntry> _globalEntries = new();
    internal readonly List<HelpEntry> _contextualEntries = new();
    KeyFormat _keyFormat;
    HelpMode _mode;
    WidgetStyle _keyStyle;
    WidgetStyle _descStyle;
    WidgetStyle _separatorStyle;
    WidgetStyle _categoryStyle;
    internal string _separator;
    string _ellipsis;
    bool _showCategories;
    bool _showContext;

    /// <summary>Create a new hints widget with no entries.</summary>
    public KeybindingHints()
    {
        _keyFormat = KeyFormat.Plain;
        _mode = HelpMode.Short;
        _keyStyle = new WidgetStyle(null, null, CellStyleFlags.Bold);
        _descStyle = WidgetStyle.Default;
        _separatorStyle = WidgetStyle.Default;
        _categoryStyle = new WidgetStyle(null, null, CellStyleFlags.Bold | CellStyleFlags.Underline);
        _separator = " • ";  // " • "
        _ellipsis = "…";      // "…"
        _showCategories = true;
        _showContext = false;
    }

    /// <summary>Add a global entry (always visible).</summary>
    public KeybindingHints GlobalEntry(string key, string desc)
    {
        _globalEntries.Add(HelpEntry.New(key, desc).WithCategory(HelpCategory.Global));
        return this;
    }

    /// <summary>Add a global entry with a specific category.</summary>
    public KeybindingHints GlobalEntryCategorized(string key, string desc, HelpCategory category)
    {
        _globalEntries.Add(HelpEntry.New(key, desc).WithCategory(category));
        return this;
    }

    /// <summary>Add a contextual entry (shown when context is active).</summary>
    public KeybindingHints ContextualEntry(string key, string desc)
    {
        _contextualEntries.Add(HelpEntry.New(key, desc));
        return this;
    }

    /// <summary>Add a contextual entry with a specific category.</summary>
    public KeybindingHints ContextualEntryCategorized(string key, string desc, HelpCategory category)
    {
        _contextualEntries.Add(HelpEntry.New(key, desc).WithCategory(category));
        return this;
    }

    /// <summary>Add a pre-built global entry.</summary>
    public KeybindingHints WithGlobalEntry(HelpEntry entry)
    {
        _globalEntries.Add(entry);
        return this;
    }

    /// <summary>Add a pre-built contextual entry.</summary>
    public KeybindingHints WithContextualEntry(HelpEntry entry)
    {
        _contextualEntries.Add(entry);
        return this;
    }

    /// <summary>Set the key display format.</summary>
    public KeybindingHints WithKeyFormat(KeyFormat format) { _keyFormat = format; return this; }

    /// <summary>Set the display mode.</summary>
    public KeybindingHints WithMode(HelpMode mode) { _mode = mode; return this; }

    /// <summary>Set whether contextual entries are shown.</summary>
    public KeybindingHints WithShowContext(bool show) { _showContext = show; return this; }

    /// <summary>Set whether category headers are shown in full mode.</summary>
    public KeybindingHints WithShowCategories(bool show) { _showCategories = show; return this; }

    /// <summary>Set the style for key text.</summary>
    public KeybindingHints WithKeyStyle(WidgetStyle style) { _keyStyle = style; return this; }

    /// <summary>Set the style for description text.</summary>
    public KeybindingHints WithDescStyle(WidgetStyle style) { _descStyle = style; return this; }

    /// <summary>Set the style for separators.</summary>
    public KeybindingHints WithSeparatorStyle(WidgetStyle style) { _separatorStyle = style; return this; }

    /// <summary>Set the style for category headers.</summary>
    public KeybindingHints WithCategoryStyle(WidgetStyle style) { _categoryStyle = style; return this; }

    /// <summary>Set the separator string for short mode.</summary>
    public KeybindingHints WithSeparator(string sep) { _separator = sep; return this; }

    /// <summary>Get the global entries.</summary>
    public IReadOnlyList<HelpEntry> GlobalEntries() => _globalEntries;

    /// <summary>Get the contextual entries.</summary>
    public IReadOnlyList<HelpEntry> ContextualEntries() => _contextualEntries;

    /// <summary>Get the current mode.</summary>
    public HelpMode Mode() => _mode;

    /// <summary>Get the key format.</summary>
    public KeyFormat KeyFormat_() => _keyFormat;

    /// <summary>Toggle between short and full mode.</summary>
    public void ToggleMode()
    {
        _mode = _mode == HelpMode.Short ? HelpMode.Full : HelpMode.Short;
    }

    /// <summary>Set whether contextual entries are shown (mutable).</summary>
    public void SetShowContext(bool show) { _showContext = show; }

    /// <summary>Format a key string according to the current key format.</summary>
    internal string FormatKey(string key) =>
        _keyFormat == KeyFormat.Plain ? key : $"[{key}]";

    /// <summary>Collect visible entries, applying scope filter and key formatting.</summary>
    public List<HelpEntry> VisibleEntries()
    {
        var entries = new List<HelpEntry>();
        foreach (var e in _globalEntries)
        {
            if (e.Enabled)
                entries.Add(new HelpEntry
                {
                    Key = FormatKey(e.Key),
                    Desc = e.Desc,
                    Enabled = true,
                    Category = e.Category
                });
        }
        if (_showContext)
        {
            foreach (var e in _contextualEntries)
            {
                if (e.Enabled)
                    entries.Add(new HelpEntry
                    {
                        Key = FormatKey(e.Key),
                        Desc = e.Desc,
                        Enabled = true,
                        Category = e.Category
                    });
            }
        }
        return entries;
    }

    /// <summary>Group entries by category, preserving insertion order within each group.</summary>
    public static List<(HelpCategory Cat, List<HelpEntry> Entries)> GroupedEntries(
        IReadOnlyList<HelpEntry> entries)
    {
        var groups = new List<(HelpCategory Cat, List<HelpEntry> Entries)>();
        foreach (var entry in entries)
        {
            bool found = false;
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].Cat == entry.Category)
                {
                    groups[i].Entries.Add(entry);
                    found = true;
                    break;
                }
            }
            if (!found)
                groups.Add((entry.Category, new List<HelpEntry> { entry }));
        }
        return groups;
    }

    /// <summary>Render full mode with category headers.</summary>
    void RenderFullGrouped(IReadOnlyList<HelpEntry> entries, Rect area, Frame frame)
    {
        var groups = GroupedEntries(entries);
        var deg = frame.Degradation;
        ushort maxX = area.Right;
        ushort y = area.Y;

        // Find max key width across all entries for alignment.
        int maxKeyW = 0;
        foreach (var e in entries)
            maxKeyW = Math.Max(maxKeyW, WidgetDrawing.GraphemeWidth(e.Key));

        for (int i = 0; i < groups.Count; i++)
        {
            if (y >= area.Bottom) break;

            var (cat, groupEntries) = groups[i];

            // Category header
            var catStyle = deg.ApplyStyling() ? _categoryStyle : WidgetStyle.Default;
            WidgetDrawing.DrawTextSpan(frame, area.X, y, cat.Label(), catStyle, maxX);
            y++;

            // Entries in this category
            foreach (var entry in groupEntries)
            {
                if (y >= area.Bottom) break;

                var keyStyle = deg.ApplyStyling() ? _keyStyle : WidgetStyle.Default;
                var descStyle = deg.ApplyStyling() ? _descStyle : WidgetStyle.Default;

                ushort x = area.X;
                x = WidgetDrawing.DrawTextSpan(frame, x, y, entry.Key, keyStyle, maxX);
                int pad = maxKeyW - WidgetDrawing.GraphemeWidth(entry.Key);
                for (int p = 0; p < pad; p++)
                    x = WidgetDrawing.DrawTextSpan(frame, x, y, " ", WidgetStyle.Default, maxX);
                x = WidgetDrawing.DrawTextSpan(frame, x, y, "  ", WidgetStyle.Default, maxX);
                WidgetDrawing.DrawTextSpan(frame, x, y, entry.Desc, descStyle, maxX);
                y++;
            }

            // Blank line between groups (except after last)
            if (i + 1 < groups.Count)
                y++;
        }
    }

    // ── IWidget impl ─────────────────────────────────────────────────────────

    public void Render(Rect area, Frame frame)
    {
        if (area.IsEmpty || area.Width == 0 || area.Height == 0)
            return;

        WidgetDrawing.ClearTextArea(frame, area, WidgetStyle.Default);

        var entries = VisibleEntries();
        if (entries.Count == 0)
            return;

        if (_mode == HelpMode.Short)
        {
            // In short mode, render all entries inline using Help widget.
            var help = new Help()
                .WithMode(HelpMode.Short)
                .WithKeyStyle(_keyStyle)
                .WithDescStyle(_descStyle)
                .WithSeparatorStyle(_separatorStyle)
                .WithSeparator(_separator)
                .WithEllipsis(_ellipsis)
                .WithEntries(entries);
            help.Render(area, frame);
        }
        else
        {
            if (_showCategories)
            {
                RenderFullGrouped(entries, area, frame);
            }
            else
            {
                var help = new Help()
                    .WithMode(HelpMode.Full)
                    .WithKeyStyle(_keyStyle)
                    .WithDescStyle(_descStyle)
                    .WithEntries(entries);
                help.Render(area, frame);
            }
        }
    }

    public bool IsEssential() => false;
}
