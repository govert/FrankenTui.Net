// Port of .external/frankentui/crates/ftui-widgets/src/file_picker.rs
// File picker widget for browsing and selecting files with keyboard navigation.

using System;
using System.Collections.Generic;
using System.IO;
using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Widgets;

/// <summary>
/// File picker widget for browsing and selecting files.
///
/// Provides a TUI file browser with keyboard navigation. The widget
/// renders a directory listing with cursor selection and supports
/// entering subdirectories and navigating back to parents.
///
/// Architecture:
/// <list type="bullet">
/// <item><see cref="FilePicker"/> — stateless configuration and rendering</item>
/// <item><see cref="FilePickerState"/> — mutable navigation state (cursor, directory, entries)</item>
/// <item><see cref="DirEntry"/> — a single file/directory entry</item>
/// </list>
///
/// The widget implements <see cref="IStatefulWidget{FilePickerState}"/> so the application
/// owns the state and can read the selected path.
/// </summary>

// ── DirEntry ──────────────────────────────────────────────────────────────────

/// <summary>A single entry in a directory listing.</summary>
public sealed class DirEntry : IEquatable<DirEntry>
{
    /// <summary>Display name.</summary>
    public string Name { get; }
    /// <summary>Full path.</summary>
    public string Path { get; }
    /// <summary>Whether this is a directory.</summary>
    public bool IsDir { get; }

    DirEntry(string name, string path, bool isDir)
    {
        Name = name;
        Path = path;
        IsDir = isDir;
    }

    /// <summary>Create a directory entry.</summary>
    public static DirEntry Dir(string name, string path) => new(name, path, true);

    /// <summary>Create a file entry.</summary>
    public static DirEntry File(string name, string path) => new(name, path, false);

    public bool Equals(DirEntry? other)
    {
        if (other is null) return false;
        return Name == other.Name && Path == other.Path && IsDir == other.IsDir;
    }

    public override bool Equals(object? obj) => obj is DirEntry other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Name, Path, IsDir);

    public static bool operator ==(DirEntry? left, DirEntry? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(DirEntry? left, DirEntry? right) => !(left == right);

    public override string ToString()
        => $"DirEntry {{ name: \"{Name}\", path: \"{Path}\", is_dir: {IsDir.ToString().ToLowerInvariant()} }}";
}

// ── FilePickerState ───────────────────────────────────────────────────────────

/// <summary>Mutable state for the file picker.</summary>
public sealed class FilePickerState
{
    /// <summary>Current directory being displayed.</summary>
    public string CurrentDir { get; set; }
    /// <summary>Root directory for confinement (if set, cannot navigate above this).</summary>
    public string? Root { get; set; }
    /// <summary>Directory entries (sorted: dirs first, then files).</summary>
    public List<DirEntry> Entries { get; set; }
    /// <summary>Currently highlighted index.</summary>
    public int Cursor { get; set; }
    /// <summary>Scroll offset (first visible row).</summary>
    public int Offset { get; set; }
    /// <summary>The selected/confirmed path (set when user presses enter on a file).</summary>
    public string? Selected { get; set; }
    /// <summary>Navigation history for going back.</summary>
    readonly List<(string Dir, int Cursor)> _history = new();

    /// <summary>Create a new state with the given directory and entries.</summary>
    public FilePickerState(string currentDir, List<DirEntry> entries)
    {
        CurrentDir = currentDir;
        Entries = entries;
        Cursor = 0;
        Offset = 0;
        Selected = null;
        Root = null;
    }

    /// <summary>
    /// Set a root directory to confine navigation.
    ///
    /// When set, the user cannot navigate to a parent directory above this root.
    /// </summary>
    public FilePickerState WithRoot(string root)
    {
        Root = root;
        return this;
    }

    /// <summary>
    /// Create state from a directory path by reading the filesystem.
    ///
    /// Sorts entries: directories first (alphabetical), then files (alphabetical).
    /// Throws <see cref="IOException"/> if the directory cannot be read.
    /// </summary>
    public static FilePickerState FromPath(string path)
    {
        var entries = FilePickerHelpers.ReadDirectory(path);
        return new FilePickerState(path, entries);
    }

    /// <summary>Move cursor up.</summary>
    public void CursorUp()
    {
        if (Cursor > 0)
            Cursor--;
    }

    /// <summary>Move cursor down.</summary>
    public void CursorDown()
    {
        if (Entries.Count > 0 && Cursor < Entries.Count - 1)
            Cursor++;
    }

    /// <summary>Move cursor to the first entry.</summary>
    public void CursorHome() => Cursor = 0;

    /// <summary>Move cursor to the last entry.</summary>
    public void CursorEnd()
    {
        if (Entries.Count > 0)
            Cursor = Entries.Count - 1;
    }

    /// <summary>Page up by <paramref name="pageSize"/> rows.</summary>
    public void PageUp(int pageSize)
    {
        Cursor = Math.Max(0, Cursor - pageSize);
    }

    /// <summary>Page down by <paramref name="pageSize"/> rows.</summary>
    public void PageDown(int pageSize)
    {
        if (Entries.Count > 0)
            Cursor = Math.Min(Cursor + pageSize, Entries.Count - 1);
    }

    /// <summary>
    /// Enter the selected directory (if cursor is on a directory).
    ///
    /// Returns <c>true</c> if navigation succeeded, <c>false</c> if cursor is on a file,
    /// or throws if the directory cannot be read or root confinement is violated.
    /// </summary>
    public bool Enter()
    {
        if (Cursor >= Entries.Count)
            return false;

        var entry = Entries[Cursor];

        if (!entry.IsDir)
        {
            if (Root is not null)
            {
                FilePickerHelpers.EnsurePathWithinRoot(
                    entry.Path,
                    Root,
                    "Cannot select a file outside root directory");
            }
            Selected = entry.Path;
            return false;
        }

        var newDir = entry.Path;

        if (Root is not null)
        {
            FilePickerHelpers.EnsurePathWithinRoot(
                newDir,
                Root,
                "Cannot traverse outside root directory via symlink");
        }

        var newEntries = FilePickerHelpers.ReadDirectory(newDir);

        _history.Add((CurrentDir, Cursor));
        CurrentDir = newDir;
        Entries = newEntries;
        Cursor = 0;
        Offset = 0;
        return true;
    }

    /// <summary>
    /// Go back to the parent directory.
    ///
    /// Returns <c>true</c> if navigation succeeded.
    /// </summary>
    public bool GoBack()
    {
        // If root is set, prevent going above it using canonicalized paths
        if (Root is not null)
        {
            var (resolvedCurr, resolvedRoot) = FilePickerHelpers.CanonicalizeCandidateAndRoot(
                CurrentDir,
                Root,
                "current file picker directory");
            if (resolvedCurr == resolvedRoot ||
                !FilePickerHelpers.PathStartsWith(resolvedCurr, resolvedRoot))
                return false;
        }

        if (_history.Count > 0)
        {
            var (prevDir, prevCursor) = _history[_history.Count - 1];
            if (Root is not null)
            {
                FilePickerHelpers.EnsurePathWithinRoot(
                    prevDir,
                    Root,
                    "Cannot restore directory outside root directory");
            }
            _history.RemoveAt(_history.Count - 1);
            var entries = FilePickerHelpers.ReadDirectory(prevDir);
            CurrentDir = prevDir;
            Entries = entries;
            Cursor = Math.Min(prevCursor, Math.Max(0, Entries.Count - 1));
            Offset = 0;
            return true;
        }

        // No history — try parent directory
        var parent = System.IO.Path.GetDirectoryName(CurrentDir);
        if (parent is not null)
        {
            if (Root is not null && !FilePickerHelpers.PathIsWithinRoot(parent, Root))
                return false; // Block parent traversal outside root

            var entries = FilePickerHelpers.ReadDirectory(parent);
            CurrentDir = parent;
            Entries = entries;
            Cursor = 0;
            Offset = 0;
            return true;
        }

        return false;
    }

    /// <summary>Ensure scroll offset keeps cursor visible for the given viewport height.</summary>
    internal void AdjustScroll(int visibleRows)
    {
        if (visibleRows == 0)
            return;
        if (Cursor < Offset)
            Offset = Cursor;
        if (Cursor >= Offset + visibleRows)
            Offset = Cursor + 1 - visibleRows;
    }
}

// ── Path confinement helpers ───────────────────────────────────────────────────
// DIVERGENCE: Rust free functions (canonicalize_for_confinement, canonicalize_candidate_and_root,
// path_is_within_root, ensure_path_within_root, read_directory) live at module scope.
// In C# we group them in an internal static class.

internal static class FilePickerHelpers
{
    /// <summary>
    /// Resolve <paramref name="path"/> to its canonical absolute form.
    /// Throws <see cref="IOException"/> (mapping Rust's io::Error) if the path cannot be resolved.
    /// </summary>
    internal static string CanonicalizeForConfinement(string path, string label)
    {
        try
        {
            // DIVERGENCE: Rust uses std::fs::canonicalize which resolves symlinks.
            // System.IO.Path.GetFullPath does NOT resolve symlinks. For genuine symlink
            // resolution we use FileInfo.ResolveLinkTarget when the path exists.
            var info = new FileInfo(path);
            if (info.Exists)
            {
                var resolved = info.ResolveLinkTarget(returnFinalTarget: true);
                if (resolved is not null)
                    return resolved.FullName;
                return info.FullName;
            }
            var dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists)
            {
                var resolved = dirInfo.ResolveLinkTarget(returnFinalTarget: true);
                if (resolved is not null)
                    return resolved.FullName;
                return dirInfo.FullName;
            }
            // Path doesn't exist — throw like Rust's canonicalize
            throw new FileNotFoundException($"Cannot resolve {label} {path}: path does not exist", path);
        }
        catch (IOException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new IOException($"Cannot resolve {label} {path}: {ex.Message}", ex);
        }
    }

    internal static (string ResolvedCandidate, string ResolvedRoot) CanonicalizeCandidateAndRoot(
        string candidate,
        string root,
        string label)
    {
        var resolvedCandidate = CanonicalizeForConfinement(candidate, label);
        var resolvedRoot = CanonicalizeForConfinement(root, "file picker root");
        return (resolvedCandidate, resolvedRoot);
    }

    /// <summary>
    /// Returns true if <paramref name="resolvedCurr"/> starts with
    /// <paramref name="resolvedRoot"/> as a path prefix (not merely a string prefix).
    /// Used after both paths are already canonicalized.
    /// </summary>
    internal static bool PathStartsWith(string resolvedCurr, string resolvedRoot)
    {
        var rootWithSep = resolvedRoot.TrimEnd(
            System.IO.Path.DirectorySeparatorChar,
            System.IO.Path.AltDirectorySeparatorChar)
            + System.IO.Path.DirectorySeparatorChar;
        return resolvedCurr.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool PathIsWithinRoot(string candidate, string root)
    {
        var (resolvedCandidate, resolvedRoot) =
            CanonicalizeCandidateAndRoot(candidate, root, "file picker path");
        return resolvedCandidate.Equals(resolvedRoot, StringComparison.OrdinalIgnoreCase)
            || PathStartsWith(resolvedCandidate, resolvedRoot);
    }

    internal static void EnsurePathWithinRoot(string candidate, string root, string message)
    {
        if (PathIsWithinRoot(candidate, root))
            return;
        // DIVERGENCE: Rust throws io::Error(PermissionDenied). .NET uses
        // UnauthorizedAccessException which maps semantically to PermissionDenied.
        throw new UnauthorizedAccessException(message);
    }

    /// <summary>Read a directory and return sorted entries (dirs first, then files).</summary>
    internal static List<DirEntry> ReadDirectory(string path)
    {
        var dirs = new List<DirEntry>();
        var files = new List<DirEntry>();

        foreach (var fse in new DirectoryInfo(path).EnumerateFileSystemInfos())
        {
            var name = fse.Name;
            var fullPath = fse.FullName;
            bool isDir;

            if (fse is FileInfo fi)
            {
                // If it's a symlink, check what it points to (mirrors Rust symlink handling)
                var target = fi.ResolveLinkTarget(returnFinalTarget: true);
                isDir = target is DirectoryInfo
                    || (fi.Attributes & FileAttributes.Directory) != 0;
            }
            else
            {
                isDir = true;
            }

            if (isDir)
                dirs.Add(DirEntry.Dir(name, fullPath));
            else
                files.Add(DirEntry.File(name, fullPath));
        }

        dirs.Sort((a, b) => string.Compare(
            a.Name, b.Name,
            StringComparison.OrdinalIgnoreCase));
        files.Sort((a, b) => string.Compare(
            a.Name, b.Name,
            StringComparison.OrdinalIgnoreCase));

        dirs.AddRange(files);
        return dirs;
    }
}

// ── FilePicker ────────────────────────────────────────────────────────────────

/// <summary>
/// Configuration and rendering for the file picker widget.
///
/// <example>
/// <code>
/// var picker = new FilePicker()
///     .WithDirStyle(new WidgetStyle(PackedRgba.Rgb(100, 100, 255), null, null))
///     .WithCursorStyle(new WidgetStyle(null, null, CellStyleFlags.Bold));
///
/// var state = FilePickerState.FromPath(".");
/// picker.Render(area, frame, state);
/// </code>
/// </example>
/// </summary>
public sealed class FilePicker : IStatefulWidget<FilePickerState>
{
    /// <summary>Style for directory entries.</summary>
    public WidgetStyle DirStyle { get; private set; }
    /// <summary>Style for file entries.</summary>
    public WidgetStyle FileStyle { get; private set; }
    /// <summary>Style for the cursor row.</summary>
    public WidgetStyle CursorStyle { get; private set; }
    /// <summary>Style for the header (current directory).</summary>
    public WidgetStyle HeaderStyle { get; private set; }
    /// <summary>Whether to show the current directory path as a header.</summary>
    public bool ShowHeader { get; private set; }
    /// <summary>Prefix for directory entries.</summary>
    public string DirPrefix { get; private set; }
    /// <summary>Prefix for file entries.</summary>
    public string FilePrefix { get; private set; }

    /// <summary>Create a new file picker with default styles.</summary>
    public FilePicker()
    {
        DirStyle = WidgetStyle.Default;
        FileStyle = WidgetStyle.Default;
        CursorStyle = WidgetStyle.Default;
        HeaderStyle = WidgetStyle.Default;
        ShowHeader = true;
        DirPrefix = "📁 ";
        FilePrefix = "  ";
    }

    /// <summary>Set the directory entry style.</summary>
    public FilePicker WithDirStyle(WidgetStyle style) { DirStyle = style; return this; }

    /// <summary>Set the file entry style.</summary>
    public FilePicker WithFileStyle(WidgetStyle style) { FileStyle = style; return this; }

    /// <summary>Set the cursor (highlight) style.</summary>
    public FilePicker WithCursorStyle(WidgetStyle style) { CursorStyle = style; return this; }

    /// <summary>Set the header style.</summary>
    public FilePicker WithHeaderStyle(WidgetStyle style) { HeaderStyle = style; return this; }

    /// <summary>Toggle header display.</summary>
    public FilePicker WithShowHeader(bool show) { ShowHeader = show; return this; }

    /// <summary>
    /// Render the file picker into <paramref name="frame"/> at <paramref name="area"/>.
    /// Mirrors the Rust <c>StatefulWidget::render</c> implementation exactly.
    /// </summary>
    public void Render(Rect area, Frame frame, FilePickerState state)
    {
        if (area.IsEmpty)
            return;

        var deg = frame.Degradation;
        if (!deg.RenderContent())
            return;

        WidgetDrawing.ClearTextArea(frame, area, WidgetStyle.Default);

        var headerStyle = deg.ApplyStyling() ? HeaderStyle : WidgetStyle.Default;
        var dirStyle    = deg.ApplyStyling() ? DirStyle    : WidgetStyle.Default;
        var fileStyle   = deg.ApplyStyling() ? FileStyle   : WidgetStyle.Default;
        var cursorStyle = deg.ApplyStyling() ? CursorStyle : WidgetStyle.Default;

        ushort y = area.Y;
        ushort maxY = area.Bottom;

        // Header: current directory path
        if (ShowHeader && y < maxY)
        {
            WidgetDrawing.ClearTextRow(frame, new Rect(area.X, y, area.Width, 1), headerStyle);
            WidgetDrawing.DrawTextSpan(frame, area.X, y, state.CurrentDir, headerStyle, area.Right);
            y++;
        }

        if (y >= maxY)
            return;

        int visibleRows = maxY - y;
        state.AdjustScroll(visibleRows);

        if (state.Entries.Count == 0)
        {
            WidgetDrawing.ClearTextRow(frame, new Rect(area.X, y, area.Width, 1), fileStyle);
            WidgetDrawing.DrawTextSpan(
                frame,
                area.X,
                y,
                "(empty directory)",
                fileStyle,
                area.Right);
            return;
        }

        int endIdx = Math.Min(state.Offset + visibleRows, state.Entries.Count);
        for (int i = 0; i < endIdx - state.Offset; i++)
        {
            if (y >= maxY)
                break;

            var entry = state.Entries[state.Offset + i];
            int actualIdx = state.Offset + i;
            bool isCursor = actualIdx == state.Cursor;

            var prefix = entry.IsDir ? DirPrefix : FilePrefix;
            var baseStyle = entry.IsDir ? dirStyle : fileStyle;

            // cursor_style.merge(&base_style): cursor fields take priority, base fills gaps
            WidgetStyle style;
            if (isCursor)
            {
                style = new WidgetStyle(
                    cursorStyle.Fg ?? baseStyle.Fg,
                    cursorStyle.Bg ?? baseStyle.Bg,
                    cursorStyle.Attrs ?? baseStyle.Attrs);
            }
            else
            {
                style = baseStyle;
            }

            WidgetDrawing.ClearTextRow(frame, new Rect(area.X, y, area.Width, 1), style);

            // Draw cursor indicator (always advance x by 2 for alignment)
            ushort x = area.X;
            if (isCursor)
            {
                WidgetDrawing.DrawTextSpan(frame, x, y, "> ", cursorStyle, area.Right);
                x = (ushort)Math.Min((int)x + 2, ushort.MaxValue);
            }
            else
            {
                x = (ushort)Math.Min((int)x + 2, ushort.MaxValue);
            }

            // Draw prefix + name
            x = WidgetDrawing.DrawTextSpan(frame, x, y, prefix, style, area.Right);
            WidgetDrawing.DrawTextSpan(frame, x, y, entry.Name, style, area.Right);

            y++;
        }
    }
}
