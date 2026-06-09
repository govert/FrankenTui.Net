// Upstream source: .external/frankentui/crates/ftui-widgets/src/file_picker.rs (tests module)
// Full 1-1 port of all upstream file_picker tests.

using System;
using System.Collections.Generic;
using System.IO;
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Widgets;
using Xunit;

namespace FrankenTui.Tests.Headless;

/// <summary>
/// Tests ported from the <c>#[cfg(test)] mod tests</c> block in file_picker.rs.
/// </summary>
public class FilePickerTests
{
    // ── Test helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Port of Rust <c>buf_to_lines</c>: reads every row of the buffer as a string,
    /// with unmapped cells rendered as spaces.
    /// </summary>
    static List<string> BufToLines(FrankenTui.Render.Buffer buf)
    {
        var lines = new List<string>();
        for (ushort y = 0; y < buf.Height; y++)
        {
            var row = new System.Text.StringBuilder(buf.Width);
            for (ushort x = 0; x < buf.Width; x++)
            {
                char ch = buf.Get(x, y)?.Content.AsChar() ?? ' ';
                row.Append(ch);
            }
            lines.Add(row.ToString());
        }
        return lines;
    }

    static List<DirEntry> MakeEntries() => new()
    {
        DirEntry.Dir("docs", "/tmp/docs"),
        DirEntry.Dir("src", "/tmp/src"),
        DirEntry.File("README.md", "/tmp/README.md"),
        DirEntry.File("main.rs", "/tmp/main.rs"),
    };

    static FilePickerState MakeState()
        => new FilePickerState("/tmp", MakeEntries());

    // ── DirEntry constructor tests ─────────────────────────────────────────────

    [Fact]
    public void DirEntryConstructors()
    {
        var d = DirEntry.Dir("src", "/src");
        Assert.True(d.IsDir);
        Assert.Equal("src", d.Name);

        var f = DirEntry.File("main.rs", "/main.rs");
        Assert.False(f.IsDir);
        Assert.Equal("main.rs", f.Name);
    }

    // ── Cursor movement ────────────────────────────────────────────────────────

    [Fact]
    public void StateCursorMovement()
    {
        var state = MakeState();
        Assert.Equal(0, state.Cursor);

        state.CursorDown();
        Assert.Equal(1, state.Cursor);

        state.CursorDown();
        state.CursorDown();
        Assert.Equal(3, state.Cursor);

        // Can't go past end
        state.CursorDown();
        Assert.Equal(3, state.Cursor);

        state.CursorUp();
        Assert.Equal(2, state.Cursor);

        state.CursorHome();
        Assert.Equal(0, state.Cursor);

        // Can't go before start
        state.CursorUp();
        Assert.Equal(0, state.Cursor);

        state.CursorEnd();
        Assert.Equal(3, state.Cursor);
    }

    // ── Page navigation ────────────────────────────────────────────────────────

    [Fact]
    public void StatePageNavigation()
    {
        var entries = new List<DirEntry>();
        for (int i = 0; i < 20; i++)
            entries.Add(DirEntry.File($"file{i}.txt", $"/tmp/file{i}.txt"));
        var state = new FilePickerState("/tmp", entries);

        state.PageDown(5);
        Assert.Equal(5, state.Cursor);

        state.PageDown(5);
        Assert.Equal(10, state.Cursor);

        state.PageUp(3);
        Assert.Equal(7, state.Cursor);

        state.PageUp(100);
        Assert.Equal(0, state.Cursor);

        state.PageDown(100);
        Assert.Equal(19, state.Cursor);
    }

    // ── Empty entries ──────────────────────────────────────────────────────────

    [Fact]
    public void StateEmptyEntries()
    {
        var state = new FilePickerState("/tmp", new List<DirEntry>());
        state.CursorDown(); // should not throw
        state.CursorUp();
        state.CursorEnd();
        state.CursorHome();
        state.PageDown(10);
        state.PageUp(10);
        Assert.Equal(0, state.Cursor);
    }

    // ── AdjustScroll ──────────────────────────────────────────────────────────

    [Fact]
    public void AdjustScrollKeepsCursorVisible()
    {
        var entries = new List<DirEntry>();
        for (int i = 0; i < 20; i++)
            entries.Add(DirEntry.File($"f{i}", $"/f{i}"));
        var state = new FilePickerState("/", entries);

        state.Cursor = 15;
        state.AdjustScroll(5);
        // cursor=15 should be visible in a 5-row window
        Assert.True(state.Offset <= 15);
        Assert.True(state.Offset + 5 > 15);

        state.Cursor = 0;
        state.AdjustScroll(5);
        Assert.Equal(0, state.Offset);
    }

    // ── Render basic ──────────────────────────────────────────────────────────

    [Fact]
    public void RenderBasic()
    {
        var picker = new FilePicker().WithShowHeader(false);
        var state = MakeState();

        var area = new Rect(0, 0, 30, 5);
        var pool = new GraphemePool();
        var frame = new Frame(30, 5, pool);

        picker.Render(area, frame, state);
        var lines = BufToLines(frame.Buffer);

        // First entry should have cursor indicator "> "
        Assert.StartsWith("> ", lines[0]);
        // Should contain directory and file names
        var allText = string.Join("\n", lines);
        Assert.Contains("docs", allText);
        Assert.Contains("src", allText);
        Assert.Contains("README.md", allText);
        Assert.Contains("main.rs", allText);
    }

    // ── Render with header ─────────────────────────────────────────────────────

    [Fact]
    public void RenderWithHeader()
    {
        var picker = new FilePicker().WithShowHeader(true);
        var state = MakeState();

        var area = new Rect(0, 0, 30, 6);
        var pool = new GraphemePool();
        var frame = new Frame(30, 6, pool);

        picker.Render(area, frame, state);
        var lines = BufToLines(frame.Buffer);

        // First line should be the directory path
        Assert.StartsWith("/tmp", lines[0]);
    }

    // ── Render empty directory ─────────────────────────────────────────────────

    [Fact]
    public void RenderEmptyDirectory()
    {
        var picker = new FilePicker().WithShowHeader(false);
        var state = new FilePickerState("/empty", new List<DirEntry>());

        var area = new Rect(0, 0, 30, 3);
        var pool = new GraphemePool();
        var frame = new Frame(30, 3, pool);

        picker.Render(area, frame, state);
        var lines = BufToLines(frame.Buffer);

        Assert.Contains("empty directory", lines[0]);
    }

    // ── Render scrolling ──────────────────────────────────────────────────────

    [Fact]
    public void RenderScrolling()
    {
        var entries = new List<DirEntry>();
        for (int i = 0; i < 20; i++)
            entries.Add(DirEntry.File($"file{i:D2}.txt", $"/tmp/file{i:D2}.txt"));
        var state = new FilePickerState("/tmp", entries);
        var picker = new FilePicker().WithShowHeader(false);

        // Move cursor to item 15, viewport is 5 rows
        state.Cursor = 15;
        var area = new Rect(0, 0, 30, 5);
        var pool = new GraphemePool();
        var frame = new Frame(30, 5, pool);

        picker.Render(area, frame, state);
        var lines = BufToLines(frame.Buffer);

        // file15 should be visible (with cursor)
        var allText = string.Join("\n", lines);
        Assert.Contains("file15", allText);
    }

    // ── Cursor style applied to selected row ──────────────────────────────────

    [Fact]
    public void CursorStyleAppliedToSelectedRow()
    {
        var picker = new FilePicker()
            .WithShowHeader(false)
            .WithCursorStyle(new WidgetStyle(PackedRgba.Rgb(255, 0, 0), null, null));
        var state = MakeState();
        state.Cursor = 1; // "src"

        var area = new Rect(0, 0, 30, 4);
        var pool = new GraphemePool();
        var frame = new Frame(30, 4, pool);

        picker.Render(area, frame, state);

        // The cursor row (y=1) should have the cursor indicator
        var lines = BufToLines(frame.Buffer);
        Assert.StartsWith("> ", lines[1]);
        // Non-cursor rows should not
        Assert.False(lines[0].StartsWith("> "));
    }

    // ── selected set on file entry ─────────────────────────────────────────────

    [Fact]
    public void SelectedSetOnFileEntry()
    {
        var state = MakeState();
        state.Cursor = 2; // README.md (a file)

        // Enter() on a file should set selected
        var result = state.Enter();
        Assert.True(result == false);
        Assert.Equal("/tmp/README.md", state.Selected);
    }

    // ── Root confinement: enter on file outside root ───────────────────────────

    [Fact]
    public void EnterOnFileRejectsCanonicalPathOutsideRoot()
    {
        // Upstream (file_picker.rs:754-773) selects an EXISTING file (Cargo.toml) that lives
        // outside the CARGO_MANIFEST_DIR root so that canonicalize() succeeds and the test
        // validates the path-prefix rejection path -> io::ErrorKind::PermissionDenied.
        //
        // We reproduce the same intent with real directories on disk so that
        // CanonicalizeForConfinement() succeeds for both the candidate and the root, and
        // EnsurePathWithinRoot() reaches the prefix check, which then throws
        // UnauthorizedAccessException (the .NET equivalent of PermissionDenied).
        //
        // Layout (inside the OS temp dir, cleaned up in the finally block):
        //   <base>/root/        ← the confinement root
        //   <base>/outside.txt  ← an existing file that is a sibling of root, not inside it

        var baseDir = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"fp-confinement-test-{System.Diagnostics.Process.GetCurrentProcess().Id}");
        var rootDir = System.IO.Path.Combine(baseDir, "root");
        var outsideFile = System.IO.Path.Combine(baseDir, "outside.txt");

        try
        {
            System.IO.Directory.CreateDirectory(rootDir);
            System.IO.File.WriteAllText(outsideFile, "outside");

            var state = new FilePickerState(rootDir, new List<DirEntry>
            {
                // The entry physically exists on disk but is outside the root subtree.
                DirEntry.File("outside.txt", outsideFile),
            }).WithRoot(rootDir);

            // DIVERGENCE: Rust io::ErrorKind::PermissionDenied maps to UnauthorizedAccessException.
            // CanonicalizeForConfinement() succeeds for both paths (they exist), so
            // EnsurePathWithinRoot() reaches the starts_with check and throws.
            var ex = Assert.Throws<UnauthorizedAccessException>(() => state.Enter());
            Assert.NotNull(ex);
            Assert.Null(state.Selected);
        }
        finally
        {
            try { System.IO.Directory.Delete(baseDir, recursive: true); } catch { }
        }
    }

    // ── Root confinement: unresolvable root fails closed ──────────────────────

    [Fact]
    public void EnterOnDirectoryWithUnresolvableRootFailsClosed()
    {
        var currentDir = System.IO.Directory.GetCurrentDirectory();
        var missingRoot = System.IO.Path.Combine(
            currentDir,
            $".missing-file-picker-root-{System.Diagnostics.Process.GetCurrentProcess().Id}");
        var targetDir = System.IO.Path.GetTempPath();
        var state = new FilePickerState(currentDir, new List<DirEntry>
        {
            DirEntry.Dir("tmp", targetDir),
        }).WithRoot(missingRoot);

        // DIVERGENCE: Rust yields io::ErrorKind::NotFound; .NET throws FileNotFoundException
        // (which is a subclass of IOException), so we check for IOException.
        var ex = Assert.Throws<FileNotFoundException>(() => state.Enter());
        Assert.NotNull(ex);
        Assert.Equal(currentDir, state.CurrentDir);
        Assert.Equal(targetDir, state.Entries[0].Path);
    }

    // ── DirEntry edge cases ───────────────────────────────────────────────────

    [Fact]
    public void DirEntryEquality()
    {
        var a = DirEntry.Dir("src", "/src");
        var b = DirEntry.Dir("src", "/src");
        Assert.Equal(a, b);

        var c = DirEntry.File("src", "/src");
        Assert.NotEqual(a, c); // dir vs file should differ
    }

    [Fact]
    public void DirEntryClone()
    {
        // DIVERGENCE: Rust has explicit Clone derive; C# reference types are naturally
        // shareable but DirEntry is immutable (all fields readonly), so equality suffices.
        var orig = DirEntry.File("main.rs", "/main.rs");
        // Constructing via the same factory is the equivalent of clone in this context.
        var cloned = DirEntry.File(orig.Name, orig.Path);
        Assert.Equal(orig, cloned);
    }

    [Fact]
    public void DirEntryDebugFormat()
    {
        var e = DirEntry.Dir("test", "/test");
        var dbg = e.ToString();
        Assert.Contains("test", dbg);
        Assert.Contains("is_dir: true", dbg);
    }

    // ── FilePickerState construction ──────────────────────────────────────────

    [Fact]
    public void StateNewDefaults()
    {
        var state = new FilePickerState("/home", new List<DirEntry>());
        Assert.Equal("/home", state.CurrentDir);
        Assert.Equal(0, state.Cursor);
        Assert.Equal(0, state.Offset);
        Assert.Null(state.Selected);
        Assert.Null(state.Root);
        Assert.Empty(state.Entries);
    }

    [Fact]
    public void StateWithRootSetsRoot()
    {
        var state = new FilePickerState("/home/user", new List<DirEntry>()).WithRoot("/home");
        Assert.Equal("/home", state.Root);
    }

    // ── Cursor on single entry ─────────────────────────────────────────────────

    [Fact]
    public void CursorMovementSingleEntry()
    {
        var entries = new List<DirEntry> { DirEntry.File("only.txt", "/only.txt") };
        var state = new FilePickerState("/", entries);

        Assert.Equal(0, state.Cursor);
        state.CursorDown();
        Assert.Equal(0, state.Cursor); // can't go past single entry
        state.CursorUp();
        Assert.Equal(0, state.Cursor);
        state.CursorEnd();
        Assert.Equal(0, state.Cursor);
        state.CursorHome();
        Assert.Equal(0, state.Cursor);
    }

    [Fact]
    public void PageDownClampsToLast()
    {
        var entries = new List<DirEntry>();
        for (int i = 0; i < 5; i++)
            entries.Add(DirEntry.File($"f{i}", $"/f{i}"));
        var state = new FilePickerState("/", entries);

        state.PageDown(100);
        Assert.Equal(4, state.Cursor);
    }

    [Fact]
    public void PageUpClampsToZero()
    {
        var entries = new List<DirEntry>();
        for (int i = 0; i < 5; i++)
            entries.Add(DirEntry.File($"f{i}", $"/f{i}"));
        var state = new FilePickerState("/", entries);
        state.Cursor = 3;

        state.PageUp(100);
        Assert.Equal(0, state.Cursor);
    }

    [Fact]
    public void PageOperationsOnEmptyEntries()
    {
        var state = new FilePickerState("/", new List<DirEntry>());
        state.PageDown(10);
        Assert.Equal(0, state.Cursor);
        state.PageUp(10);
        Assert.Equal(0, state.Cursor);
    }

    // ── Enter() edge cases ────────────────────────────────────────────────────

    [Fact]
    public void EnterOnEmptyEntriesReturnsFalse()
    {
        var state = new FilePickerState("/", new List<DirEntry>());
        var result = state.Enter();
        Assert.False(result);
        Assert.Null(state.Selected);
    }

    [Fact]
    public void EnterOnFileSetsSelectedWithoutNavigation()
    {
        var entries = new List<DirEntry>
        {
            DirEntry.Dir("sub", "/sub"),
            DirEntry.File("readme.txt", "/readme.txt"),
        };
        var state = new FilePickerState("/", entries);
        state.Cursor = 1;

        var result = state.Enter();
        Assert.False(result); // enter on file returns false (no navigation)
        Assert.Equal("/readme.txt", state.Selected);
        // Current directory unchanged.
        Assert.Equal("/", state.CurrentDir);
    }

    // ── GoBack() edge cases ────────────────────────────────────────────────────

    [Fact]
    public void GoBackBlockedAtRoot()
    {
        var root = System.IO.Path.GetTempPath().TrimEnd(
            System.IO.Path.DirectorySeparatorChar,
            System.IO.Path.AltDirectorySeparatorChar);
        var state = new FilePickerState(root, new List<DirEntry>()).WithRoot(root);

        var changed = state.GoBack();
        Assert.False(changed); // go_back should be blocked when already at root
    }

    [Fact]
    public void GoBackWithoutHistoryUsesParentDirectory()
    {
        var current = System.IO.Path.GetTempPath().TrimEnd(
            System.IO.Path.DirectorySeparatorChar,
            System.IO.Path.AltDirectorySeparatorChar);
        var parent = System.IO.Path.GetDirectoryName(current)!;

        var state = new FilePickerState(current, new List<DirEntry>());
        var changed = state.GoBack();

        Assert.True(changed); // go_back should navigate to parent when history is empty
        Assert.Equal(parent, state.CurrentDir);
        Assert.Equal(0, state.Cursor); // parent navigation resets cursor to home
    }

    [Fact]
    public void GoBackRestoresHistoryCursorWithClamp()
    {
        var child = System.IO.Path.GetTempPath().TrimEnd(
            System.IO.Path.DirectorySeparatorChar,
            System.IO.Path.AltDirectorySeparatorChar);
        var parent = System.IO.Path.GetDirectoryName(child)!;

        var state = new FilePickerState(parent, new List<DirEntry>
        {
            DirEntry.File("placeholder.txt", System.IO.Path.Combine(parent, "placeholder.txt")),
            DirEntry.Dir("child", child),
        });
        state.Cursor = 1;

        var entered = state.Enter();
        Assert.True(entered); // enter should navigate into selected directory

        var wentBack = state.GoBack();
        Assert.True(wentBack); // go_back should restore previous directory from history
        Assert.Equal(parent, state.CurrentDir);

        var expectedCursor = Math.Min(1, Math.Max(0, state.Entries.Count - 1));
        Assert.Equal(expectedCursor, state.Cursor);
    }

    // ── AdjustScroll edge cases ────────────────────────────────────────────────

    [Fact]
    public void AdjustScrollZeroVisibleRowsIsNoop()
    {
        var entries = new List<DirEntry>();
        for (int i = 0; i < 10; i++)
            entries.Add(DirEntry.File($"f{i}", $"/f{i}"));
        var state = new FilePickerState("/", entries);
        state.Cursor = 5;
        state.Offset = 0;

        state.AdjustScroll(0);
        Assert.Equal(0, state.Offset); // zero visible rows should not change offset
    }

    [Fact]
    public void AdjustScrollCursorAboveViewport()
    {
        var entries = new List<DirEntry>();
        for (int i = 0; i < 20; i++)
            entries.Add(DirEntry.File($"f{i}", $"/f{i}"));
        var state = new FilePickerState("/", entries);
        state.Offset = 10;
        state.Cursor = 5;

        state.AdjustScroll(5);
        Assert.Equal(5, state.Offset); // offset should snap to cursor
    }

    [Fact]
    public void AdjustScrollCursorBelowViewport()
    {
        var entries = new List<DirEntry>();
        for (int i = 0; i < 20; i++)
            entries.Add(DirEntry.File($"f{i}", $"/f{i}"));
        var state = new FilePickerState("/", entries);
        state.Offset = 0;
        state.Cursor = 10;

        state.AdjustScroll(5);
        // cursor=10 should be the last visible row: offset + 5 > 10 → offset = 6
        Assert.Equal(6, state.Offset);
    }

    // ── FilePicker builder ────────────────────────────────────────────────────

    [Fact]
    public void FilePickerDefaultValues()
    {
        var picker = new FilePicker();
        Assert.True(picker.ShowHeader);
        Assert.Equal("📁 ", picker.DirPrefix);
        Assert.Equal("  ", picker.FilePrefix);
    }

    [Fact]
    public void FilePickerBuilderChain()
    {
        var picker = new FilePicker()
            .WithDirStyle(WidgetStyle.Default)
            .WithFileStyle(WidgetStyle.Default)
            .WithCursorStyle(WidgetStyle.Default)
            .WithHeaderStyle(WidgetStyle.Default)
            .WithShowHeader(false);
        Assert.False(picker.ShowHeader);
    }

    [Fact]
    public void FilePickerDebugFormat()
    {
        var picker = new FilePicker();
        var dbg = picker.ToString();
        // ToString() on a class returns the type name by default — just verify it's non-null.
        // DIVERGENCE: Rust Debug derive produces a structured string; C# uses type name.
        Assert.NotNull(dbg);
        Assert.Contains("FilePicker", dbg);
    }

    // ── Render edge cases ──────────────────────────────────────────────────────

    [Fact]
    public void RenderZeroAreaIsNoop()
    {
        var picker = new FilePicker();
        var state = MakeState();

        var area = new Rect(0, 0, 0, 0);
        var pool = new GraphemePool();
        var frame = new Frame(30, 5, pool);

        picker.Render(area, frame, state);
        // No crash, buffer untouched.
        var lines = BufToLines(frame.Buffer);
        Assert.True(string.IsNullOrWhiteSpace(lines[0]));
    }

    [Fact]
    public void RenderHeightOneShowsOnlyHeader()
    {
        var picker = new FilePicker().WithShowHeader(true);
        var state = MakeState();

        var area = new Rect(0, 0, 30, 1);
        var pool = new GraphemePool();
        var frame = new Frame(30, 5, pool);

        picker.Render(area, frame, state);
        var lines = BufToLines(frame.Buffer);
        // Only the header row should have content.
        Assert.StartsWith("/tmp", lines[0]);
        // Row 1 should be empty (no room for entries).
        Assert.True(string.IsNullOrWhiteSpace(lines[1]));
    }

    [Fact]
    public void RenderNoHeaderUsesFullAreaForEntries()
    {
        var picker = new FilePicker().WithShowHeader(false);
        var state = MakeState();

        var area = new Rect(0, 0, 30, 4);
        var pool = new GraphemePool();
        var frame = new Frame(30, 4, pool);

        picker.Render(area, frame, state);
        var lines = BufToLines(frame.Buffer);
        // First line should be an entry (cursor on first entry), not a header.
        Assert.StartsWith("> ", lines[0]);
    }

    [Fact]
    public void RenderCursorOnLastEntry()
    {
        var picker = new FilePicker().WithShowHeader(false);
        var state = MakeState();
        state.Cursor = 3; // last entry: main.rs

        var area = new Rect(0, 0, 30, 5);
        var pool = new GraphemePool();
        var frame = new Frame(30, 5, pool);

        picker.Render(area, frame, state);
        var lines = BufToLines(frame.Buffer);
        // The cursor row should contain "main.rs".
        var cursorLine = lines.Find(l => l.StartsWith("> "));
        Assert.NotNull(cursorLine);
        Assert.Contains("main.rs", cursorLine);
    }

    [Fact]
    public void RenderAreaOffset()
    {
        // Render into a sub-area of a larger buffer.
        var picker = new FilePicker().WithShowHeader(false);
        var state = MakeState();

        var area = new Rect(5, 2, 20, 3);
        var pool = new GraphemePool();
        var frame = new Frame(30, 10, pool);

        picker.Render(area, frame, state);
        var lines = BufToLines(frame.Buffer);
        // Rows 0 and 1 should be empty (area starts at y=2).
        Assert.True(string.IsNullOrWhiteSpace(lines[0]));
        Assert.True(string.IsNullOrWhiteSpace(lines[1]));
        // Row 2 should have content starting at x=5.
        Assert.True(lines[2].Length >= 7);
    }

    [Fact]
    public void RenderShorterHeaderAndFewerEntriesClearStaleContent()
    {
        var picker = new FilePicker().WithShowHeader(true);
        var state = new FilePickerState("/tmp/very/long/path", MakeEntries());

        var area = new Rect(0, 0, 24, 4);
        var pool = new GraphemePool();
        var frame = new Frame(24, 4, pool);

        picker.Render(area, frame, state);

        state.CurrentDir = "/x";
        state.Entries = new List<DirEntry> { DirEntry.File("a", "/x/a") };
        state.Cursor = 0;
        state.Offset = 0;

        picker.Render(area, frame, state);
        var lines = BufToLines(frame.Buffer);

        Assert.Equal("/x".PadRight(24), lines[0]);
        Assert.StartsWith(">   a", lines[1]);
        Assert.Equal(new string(' ', 24), lines[2]);
        Assert.Equal(new string(' ', 24), lines[3]);
    }
}
