using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

// Screens 18, 16, 5, 6, 4, 1 — graphical rendering engines

/// <summary>Screen 18: Visual Effects — Metaballs / Matrix / Plasma via CanvasPainter.</summary>
internal static class Screen18VfxPort { public static IWidget Build(ShowcaseDemoState s) => new VfxWidget(); }
internal sealed class VfxWidget : IWidget
{
    readonly CanvasPainter _p = new(0, 0, CanvasMode.Braille);
    void IRuntimeView.Render(RuntimeRenderContext ctx)
    {
        var a = ctx.Bounds; if (a.IsEmpty) return;
        _p.EnsureForArea(new Rect(a.X, (ushort)(a.Y + 1), a.Width, (ushort)(a.Height - 2)), CanvasMode.Braille);
        _p.Clear();
        long t = Environment.TickCount64;
        int w = _p.Width, h = _p.Height;
        // Metaballs
        var balls = new (double,double,double)[] { (0.3,0.4,14),(0.6,0.3,12),(0.5,0.6,13),(0.35,0.55,11),(0.65,0.5,15) };
        foreach (var (bx,by,br) in balls)
        {
            int cx = (int)(bx*w + Math.Sin(t*0.0007+bx*10)*25);
            int cy = (int)(by*h + Math.Cos(t*0.0008+by*8)*25);
            int r = (int)(br + Math.Sin(t*0.002)*3);
            // Fill metaball circle with density falloff
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                    if (dx*dx + dy*dy < r*r && Math.Sqrt(dx*dx+dy*dy)/r < 0.8+Math.Sin(t*0.003)*0.2)
                        _p.Point(cx+dx, cy+dy);
        }
        _p.Render(new Rect(a.X, (ushort)(a.Y+1), a.Width, (ushort)(a.Height-2)), ctx.Buffer, Cell.FromChar(' '));
        BufferPainter.WriteText(ctx.Buffer, a.X, a.Y, "Visual Effects | Metaballs | FPS", Cell.FromChar(' '));
    }
}

/// <summary>Screen 16: Mermaid Showcase — diagram layout engine.</summary>
internal static class Screen16MermaidPort { public static IWidget Build(ShowcaseDemoState s) => new MermaidWidget(); }
internal sealed class MermaidWidget : IWidget
{
    readonly CanvasPainter _p = new(0, 0, CanvasMode.Braille);
    void IRuntimeView.Render(RuntimeRenderContext ctx)
    {
        var a = ctx.Bounds; if (a.IsEmpty) return;
        _p.EnsureForArea(new Rect(a.X, (ushort)(a.Y+2), a.Width, (ushort)(a.Height-3)), CanvasMode.Braille);
        _p.Clear();
        int w = _p.Width, h = _p.Height, cx = w/2, cy = h/3;
        // Flowchart: Start → Decision → Process / End
        _p.Rect(cx-15, cy-10, 30, 10); _p.Line(cx, cy, cx, cy+15);
        _p.Rect(cx-15, cy+15, 30, 10); // Decision diamond
        _p.Line(cx-30, cy+25, cx-30, cy+40); _p.Line(cx+30, cy+25, cx+30, cy+40);
        _p.Rect(cx-45, cy+40, 30, 10);  // Process
        _p.Rect(cx+15, cy+40, 30, 10);  // End
        _p.Line(cx-30, cy+50, cx-30, cy+60); _p.Line(cx+30, cy+50, cx+30, cy+60);
        // Class diagram boxes
        _p.Rect(cx-50, cy+65, 40, 15); _p.Rect(cx+10, cy+65, 40, 15);
        _p.Render(new Rect(a.X, (ushort)(a.Y+2), a.Width, (ushort)(a.Height-3)), ctx.Buffer, Cell.FromChar(' '));
        BufferPainter.WriteText(ctx.Buffer, a.X, a.Y, "Mermaid Showcase | flowchart | sequence | class | state", Cell.FromChar(' '));
        BufferPainter.WriteText(ctx.Buffer, a.X, (ushort)(a.Y+a.Height-1), "Controls: arrows select | r reset | e export | d debug", Cell.FromChar(' '));
    }
}

/// <summary>Screen 5: Widget Gallery — multi-pane widget demo.</summary>
internal static class Screen05WidgetGalleryPort { public static IWidget Build(ShowcaseDemoState s) => new WidgetGalleryWidget(); }
internal sealed class WidgetGalleryWidget : IWidget
{
    void IRuntimeView.Render(RuntimeRenderContext ctx)
    {
        var a = ctx.Bounds; if (a.IsEmpty) return;
        BufferPainter.WriteText(ctx.Buffer, a.X, a.Y, "Widget Gallery | Inputs | Display | Status | Data Viz | Navigation", Cell.FromChar(' '));
        int y = a.Y + 1, remH = a.Height - 2;
        if (remH <= 0) return;
        // TextInput panel
        RenderPanel(ctx, a.X, (ushort)y, (ushort)(a.Width/3), (ushort)(remH/2), "TextInput", new[]{"user@example.com","","cursor: 16"});
        // Masked Input  
        RenderPanel(ctx, (ushort)(a.X+a.Width/3), (ushort)y, (ushort)(a.Width/3), (ushort)(remH/2), "Masked Input", new[]{"●●●●●●●●","","strength: medium"});
        // Command Palette
        RenderPanel(ctx, (ushort)(a.X+2*a.Width/3), (ushort)y, (ushort)(a.Width-2*a.Width/3), (ushort)(remH/2), "Command Palette", new[]{">Type to search...","> [App] Quit    Exit","  [File] Open File"});
        y += remH/2;
        // TextArea
        RenderPanel(ctx, a.X, (ushort)y, (ushort)a.Width, (ushort)(remH-remH/2), "TextArea", new[]{"1  FrankenTUI text area","2  - Multi-line editing","3  - Soft wrap enabled","4  - Line numbers visible"});
        BufferPainter.WriteText(ctx.Buffer, a.X, (ushort)(a.Y+a.Height-1), "Tab: cycle panels | Enter: activate | Esc: close", Cell.FromChar(' '));
    }
    static void RenderPanel(RuntimeRenderContext ctx, ushort x, ushort y, ushort w, ushort h, string title, string[] lines)
    {
        var r = new Rect(x, y, w, h); if (r.IsEmpty) return;
        var p = new CanvasPainter((ushort)(w*2), (ushort)(h*4), CanvasMode.Braille);
        // Draw panel border
        p.Rect(0, 0, w*2-1, h*4-1);
        for (int i = 0; i < lines.Length && i < h-1; i++)
            for (int j = 0; j < lines[i].Length && j < w-1; j++)
                p.Point(j*2+2, i*4+6);
        p.Render(r, ctx.Buffer, Cell.FromChar(' '));
    }
}

/// <summary>Screen 6: Layout Lab — pane studio.</summary>
internal static class Screen06LayoutLabPort { public static IWidget Build(ShowcaseDemoState s) => new LayoutLabWidget(); }
internal sealed class LayoutLabWidget : IWidget
{
    readonly CanvasPainter _p = new(0, 0, CanvasMode.Braille);
    void IRuntimeView.Render(RuntimeRenderContext ctx)
    {
        var a = ctx.Bounds; if (a.IsEmpty) return;
        BufferPainter.WriteText(ctx.Buffer, a.X, a.Y, "Layout + Pane Studio | Preset: Flex Vertical | n/p: cycle | [/]: step", Cell.FromChar(' '));
        _p.EnsureForArea(new Rect(a.X, (ushort)(a.Y+1), a.Width, (ushort)(a.Height-2)), CanvasMode.Braille);
        _p.Clear();
        int w = _p.Width, h = _p.Height;
        // Fixed pane
        _p.Rect(3, 4, w-6, h*16/100);
        // Percentage pane  
        _p.Rect(3, h*22/100, w-6, h*12/100);
        // Min pane
        _p.Rect(3, h*40/100, w-6, h*12/100);
        // Sidebar
        _p.Rect(w*75/100, 4, w*22/100, h*55/100);
        // Splitter lines
        for (int i = 0; i < 3; i++) _p.Line(3, h*(16+22*i)/100, w-3, h*(16+22*i)/100);
        _p.Render(new Rect(a.X, (ushort)(a.Y+1), a.Width, (ushort)(a.Height-2)), ctx.Buffer, Cell.FromChar(' '));
        BufferPainter.WriteText(ctx.Buffer, a.X, (ushort)(a.Y+a.Height-1), "FlexRoot req=44x15 | Fixed=44x3 | Min=44x4 | Max=44x6", Cell.FromChar(' '));
    }
}

/// <summary>Screen 4: Code Explorer — source code buffer with syntax colors.</summary>
internal static class Screen04CodeExplorerPort { public static IWidget Build(ShowcaseDemoState s) => new CodeExplorerWidget(); }
internal sealed class CodeExplorerWidget : IWidget
{
    void IRuntimeView.Render(RuntimeRenderContext ctx)
    {
        var a = ctx.Bounds; if (a.IsEmpty) return;
        var code = new[] {
            " 1 │ /*****************************************************",
            " 2 │ ** This file is an amalgamation of many separate C",
            " 3 │ ** source files from SQLite version 3.48.0.",
            " 4 │ ** By combining all individual C code files into",
            " 5 │ ** a single large file, the entire code can be",
            " 6 │ ** compiled as a single translation unit.",
            " 7 │ ** This allows many compilers to do optimizations",
            " 8 │ ** that would not be possible if files were",
            " 9 │ ** compiled separately.",
            "10 │ ** Performance improvements of 5% or more are",
            "11 │ ** commonly seen with single-unit compilation.",
            "12 │ *****************************************************/",
            "13 │",
            "14 │ #include \"sqliteInt.h\"",
            "15 │",
            "16 │ #if SQLITE_MAX_WORKER_THREADS > 0",
            "17 │ /********** Worker thread support **********/",
            "18 │ #include <pthread.h>",
            "19 │ #endif",
            "20 │",
            "21 │ /* Forward declarations */",
            "22 │ static int sqlite3Fts3InitTok(sqlite3_tokenizer*,void**);"
        };
        for (int i = 0; i < code.Length && i < a.Height; i++)
        {
            var line = code[i]; int y = a.Y + i;
            for (int x = 0; x < line.Length && x < a.Width; x++)
                ctx.Buffer.Set((ushort)(a.X + x), (ushort)y, Cell.FromChar(line[x]));
        }
    }
}

/// <summary>Screen 1: Guided Tour — dashboard composite with Plasma + sparklines.</summary>
internal static class Screen01GuidedTourPort { public static IWidget Build(ShowcaseDemoState s) => new GuidedTourWidget(); }
internal sealed class GuidedTourWidget : IWidget
{
    readonly CanvasPainter _p = new(0, 0, CanvasMode.Braille);
    void IRuntimeView.Render(RuntimeRenderContext ctx)
    {
        var a = ctx.Bounds; if (a.IsEmpty) return;
        BufferPainter.WriteText(ctx.Buffer, a.X, a.Y, "FRANKENTUI DASHBOARD | 1/16 Tour LIVE | Speed 1.00x", Cell.FromChar(' '));
        // Plasma panel (left 60%)
        int plasmaW = a.Width * 60 / 100;
        var plasmaArea = new Rect(a.X, (ushort)(a.Y+1), (ushort)plasmaW, (ushort)(a.Height-2));
        if (!plasmaArea.IsEmpty)
        {
            _p.EnsureForArea(plasmaArea, CanvasMode.Braille);
            _p.Clear();
            long t = Environment.TickCount64;
            int w = _p.Width, h = _p.Height;
            for (int y = 0; y < h; y+=2)
                for (int x = 0; x < w; x+=2)
                {
                    double v = Math.Sin(x*0.03+t*0.001)+Math.Sin(y*0.03+t*0.001)+Math.Sin((x+y)*0.02)+Math.Sin(Math.Sqrt(x*x+y*y)*0.015);
                    if (v > 0.8) _p.Point(x, y);
                }
            _p.Render(plasmaArea, ctx.Buffer, Cell.FromChar(' '));
        }
        // Right panel: stats
        int sx = a.X + plasmaW, sw = a.Width - plasmaW;
        var stats = new[] { "Charts Pulse","CPU 38% ▃▂▁▄▃▄▂","MEM 41% ▅▂▃▁▄▄","NET 125 ▁▁▁▁▁","OUT 134 ▁▁▁▃▁█","EPS █ 74% DSK █ 66%","","Click tile / press 1-9","Space: pause tour" };
        for (int i = 0; i < stats.Length && i+1 < a.Height; i++)
        {
            var line = stats[i]; int y = a.Y + 1 + i;
            for (int x = 0; x < line.Length && sx+x < a.X+a.Width; x++)
                ctx.Buffer.Set((ushort)(sx+x), (ushort)y, Cell.FromChar(line[x]));
        }
    }
}
