// FrankenTui.SideBySide — renders a screen via the .NET port and compares
// it against the Rust golden snapshot side by side in the terminal.
//
// Usage:
//   dotnet run --project tools/FrankenTui.SideBySide -- [screen] [width] [height]
//   dotnet run --project tools/FrankenTui.SideBySide -- --toggle 2   (press R/D to switch)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FrankenTui.Core;
using FrankenTui.Demo.Showcase;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;
using RenderBuffer = FrankenTui.Render.Buffer;
using Theme = FrankenTui.Style.Theme;

var screenNumber = 2;
ushort width = 80;
ushort height = 24;
bool toggleMode = false;
bool runRust = true;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--toggle": toggleMode = true; break;
        case "--no-rust": runRust = false; break;
        default:
            if (i == 0 || (i == 1 && args[0] == "--toggle")) screenNumber = int.Parse(args[i]);
            else if (i == 1 || (i == 2 && args[0] == "--toggle")) width = ushort.Parse(args[i]);
            else if (i == 2 || (i == 3 && args[0] == "--toggle")) height = ushort.Parse(args[i]);
            break;
    }
}

// ── Read Rust golden snapshot ───────────────────────────────────────────
string[] rustLines = Array.Empty<string>();
if (runRust)
{
    var slug = SlugMap(screenNumber);
    var goldenDir = Path.GetFullPath(Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..",
        "artifacts", "golden-rust"));
    var snapPath = Path.Combine(goldenDir, $"app_{slug}_{width}x{height}.snap");

    if (!File.Exists(snapPath))
    {
        Console.Error.WriteLine("Rust golden not found. Run:");
        Console.Error.WriteLine($"  cd .external\\frankentui && cargo run --bin dump_screens -p ftui-demo-showcase -- --out ../../artifacts/golden-rust --width {width} --height {height}");
    }
    else
    {
        rustLines = File.ReadAllText(snapPath)
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(l => l.TrimEnd())
            .ToArray();
    }
}

// ── Render .NET version ─────────────────────────────────────────────────
var view = ShowcaseViewFactory.Build(
    inlineMode: false, screenNumber: screenNumber, frame: 0,
    width: width, height: height);
var buffer = new RenderBuffer(width, height);
view.Render(new RuntimeRenderContext(buffer, Rect.FromSize(width, height), Theme.DefaultTheme));
var dotnetLines = HeadlessBufferView.ScreenText(buffer)
    .Select(l => l.TrimEnd())
    .ToArray();

int maxLines = Math.Max(rustLines.Length, dotnetLines.Length);
int windowW = 80;
try { windowW = Console.WindowWidth; } catch { }
int leftWidth = Math.Min(width + 2, Math.Max(30, windowW / 2 - 3));

if (toggleMode && !Console.IsInputRedirected)
{
    // Toggle mode with full ANSI colors for .NET, plain text for Rust.
    // R = Rust golden snapshot (plain text)
    // D = .NET port (rendered with full ANSI color)
    // C = side-by-side comparison
    // Q = quit
    try { Console.CursorVisible = false; } catch { }
    var mode = 0; // 0=Rust, 1=.NET, 2=side-by-side
    while (true)
    {
        try { Console.SetCursorPosition(0, 0); } catch { }
        Console.CursorVisible = false;

        if (mode == 0)
        {
            // Rust golden snapshot — plain text with key highlights
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== RUST NATIVE (golden snapshot) === R=rust D=dotnet C=compare Q=quit");
            int maxH = Math.Min(rustLines.Length, Console.WindowHeight - 2);
            for (int i = 0; i < maxH; i++)
            {
                var line = rustLines[i];
                if (line.Length > Console.WindowWidth - 1)
                    line = line[..(Console.WindowWidth - 1)];
                Console.WriteLine(line);
            }
        }
        else if (mode == 1)
        {
            // .NET port — full ANSI color render
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== .NET PORT (full color ANSI) === R=rust D=dotnet C=compare Q=quit");
            PrintBufferAnsi(buffer, 1, Console.WindowHeight - 2);
        }
        else
        {
            // Side-by-side: Rust plain text | .NET plain text with diff highlighting
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== SIDE BY SIDE (Rust left | .NET right) === R=rust D=dotnet C=compare Q=quit");
            int maxH = Math.Min(maxLines, Console.WindowHeight - 2);
            for (int i = 0; i < maxH; i++)
            {
                var rl = i < rustLines.Length ? rustLines[i] : "";
                var dl = i < dotnetLines.Length ? dotnetLines[i] : "";
                bool same = rl == dl;
                var rp = rl.Length > leftWidth ? rl[..leftWidth] : rl.PadRight(leftWidth);
                var dp = dl.Length > leftWidth ? dl[..leftWidth] : dl.PadRight(leftWidth);
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(rp);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(" │ ");
                Console.ForegroundColor = same ? ConsoleColor.DarkGray : ConsoleColor.Yellow;
                Console.WriteLine(dp);
            }
        }
        // Clear remaining rows
        for (int i = Console.CursorTop; i < Console.WindowHeight; i++)
            Console.Write(new string(' ', Console.WindowWidth - 1));

        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.R) mode = 0;
        else if (key.Key == ConsoleKey.D) mode = 1;
        else if (key.Key == ConsoleKey.C) mode = 2;
        else if (key.Key == ConsoleKey.Q) break;
        Console.Clear();
    }
    Console.CursorVisible = true;
}
else
{
    // Side-by-side mode
    Console.ForegroundColor = ConsoleColor.Cyan;
    int equal = 0;
    for (int i = 0; i < maxLines; i++)
    {
        var rl = i < rustLines.Length ? rustLines[i] : "";
        var dl = i < dotnetLines.Length ? dotnetLines[i] : "";
        bool same = rl == dl;
        if (same) equal++;
        var rp = rl.Length > leftWidth ? rl[..leftWidth] : rl.PadRight(leftWidth);
        var dp = dl.Length > leftWidth ? dl[..leftWidth] : dl.PadRight(leftWidth);
        if (same) { Console.ForegroundColor = ConsoleColor.DarkGray; Console.Write(rp); Console.ForegroundColor = ConsoleColor.Gray; Console.Write(" │ "); Console.ForegroundColor = ConsoleColor.DarkGray; Console.WriteLine(dp); }
        else { Console.ForegroundColor = ConsoleColor.White; Console.Write(rp); Console.ForegroundColor = ConsoleColor.DarkGray; Console.Write(" │ "); Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine(dp); }
    }
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\nEqual: {equal}/{maxLines}  Diff: {maxLines - equal}");
    Console.ResetColor();
}

Console.ResetColor();

static string SlugMap(int s) => s switch
{
    1 => "guidedtour", 2 => "dashboard", 3 => "shakespeare", 4 => "codeexplorer",
    5 => "widgetgallery", 6 => "layoutlab", 7 => "formsinput", 8 => "dataviz",
    9 => "filebrowser", 10 => "advancedfeatures", 11 => "tablethemegallery",
    12 => "terminalcapabilities", 13 => "macrorecorder", 14 => "performance",
    15 => "markdownrichtext", 16 => "mermaidshowcase", 17 => "mermaidmegashowcase",
    18 => "visualeffects", 19 => "responsivedemo", 20 => "logsearch",
    21 => "notifications", 22 => "actiontimeline", 23 => "intrinsicsizing",
    24 => "layoutinspector", 25 => "advancedtexteditor", 26 => "mouseplayground",
    27 => "formvalidation", 28 => "virtualizedsearch", 29 => "asynctasks",
    30 => "themestudio", 31 => "snapshotplayer", 32 => "performancehud",
    33 => "explainabilitycockpit", 34 => "i18ndemo", 35 => "voioverlay",
    36 => "inlinemodestory", 37 => "accessibilitypanel", 38 => "widgetbuilder",
    39 => "commandpalettelab", 40 => "determinismlab", 41 => "hyperlinkplayground",
    42 => "kanbanboard", 43 => "markdownliveeditor", 44 => "dragdrop",
    45 => "quakeeasteregg", _ => $"screen{s}"
};

static void PrintBufferAnsi(RenderBuffer buf, int startRow, int maxRows)
{
    // Render buffer cells as ANSI escape sequences with true-color fg/bg.
    PackedRgba? lastFg = null, lastBg = null;
    for (ushort y = (ushort)startRow; y < buf.Height && y < startRow + maxRows; y++)
    {
        for (ushort x = 0; x < buf.Width; x++)
        {
            var cell = buf.Get(x, y);
            if (cell?.IsContinuation == true) continue;
            var fg = cell?.Foreground ?? PackedRgba.Transparent;
            var bg = cell?.Background ?? PackedRgba.Transparent;
            char ch = ' ';
            if (cell != null && !cell.Value.IsEmpty)
            {
                var text = buf.ResolveText(cell.Value);
                if (!string.IsNullOrEmpty(text)) ch = text[0];
            }
            if (lastFg != fg || lastBg != bg)
            {
                if (lastFg.HasValue || lastBg.HasValue) Console.Write("\x1b[0m");
                Console.Write($"\x1b[38;2;{fg.R};{fg.G};{fg.B}m\x1b[48;2;{bg.R};{bg.G};{bg.B}m");
                lastFg = fg; lastBg = bg;
            }
            Console.Write(ch);
        }
    }
    Console.Write("\x1b[0m");
}
