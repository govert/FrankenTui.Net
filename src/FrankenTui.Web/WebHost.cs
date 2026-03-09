using System.Net;
using System.Text;
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Web;

public static class WebHost
{
    public static WebFrame Render(IRuntimeView view, Size size, Theme? theme = null, WebRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(view);

        var buffer = new RenderBuffer(size.Width, size.Height);
        view.Render(new RuntimeRenderContext(buffer, Rect.FromSize(size.Width, size.Height), theme ?? Theme.DefaultTheme));
        return Render(buffer, options);
    }

    public static WebFrame Render(RenderBuffer buffer, WebRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        var resolvedOptions = options ?? WebRenderOptions.Default;
        var rows = HeadlessBufferView.ScreenText(buffer);
        var html = BuildFrameHtml(buffer, resolvedOptions);
        var documentHtml = BuildDocumentHtml(html, resolvedOptions);
        return new WebFrame
        {
            Rows = rows,
            Html = html,
            DocumentHtml = documentHtml,
            Title = resolvedOptions.Title,
            Language = resolvedOptions.Language,
            Direction = resolvedOptions.Direction,
            Accessibility = resolvedOptions.Accessibility ?? new(),
            Metadata = resolvedOptions.Metadata ?? new Dictionary<string, string>(StringComparer.Ordinal)
        };
    }

    private static string BuildFrameHtml(RenderBuffer buffer, WebRenderOptions options)
    {
        var builder = new StringBuilder();
        builder.Append("<div class=\"frankentui-host\"");
        builder.Append(" lang=\"").Append(WebUtility.HtmlEncode(options.Language)).Append('"');
        builder.Append(" dir=\"").Append(WebUtility.HtmlEncode(options.Direction)).Append('"');
        if (options.Metadata is not null)
        {
            foreach (var entry in options.Metadata)
            {
                builder.Append(" data-")
                    .Append(WebUtility.HtmlEncode(entry.Key))
                    .Append("=\"")
                    .Append(WebUtility.HtmlEncode(entry.Value))
                    .Append('"');
            }
        }

        builder.Append('>');
        builder.Append("<pre class=\"frankentui-frame\" role=\"img\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(options.AriaLabel))
            .Append("\">");
        for (ushort rowIndex = 0; rowIndex < buffer.Height; rowIndex++)
        {
            if (rowIndex > 0)
            {
                builder.AppendLine();
            }

            builder.Append("<span class=\"ft-row\" data-row=\"")
                .Append(rowIndex)
                .Append("\">");
            AppendRow(builder, buffer.GetRow(rowIndex));
            builder.Append("</span>");
        }

        builder.Append("</pre>");
        if ((options.Accessibility?.Nodes.Count ?? 0) > 0)
        {
            builder.Append("<ul class=\"ft-a11y\" hidden>");
            foreach (var node in options.Accessibility!.Nodes)
            {
                builder.Append("<li data-role=\"")
                    .Append(WebUtility.HtmlEncode(node.Role))
                    .Append("\"><strong>")
                    .Append(WebUtility.HtmlEncode(node.Label))
                    .Append("</strong>");
                if (!string.IsNullOrWhiteSpace(node.Description))
                {
                    builder.Append(": ").Append(WebUtility.HtmlEncode(node.Description));
                }

                builder.Append("</li>");
            }

            builder.Append("</ul>");
        }

        builder.Append("</div>");
        return builder.ToString();
    }

    private static void AppendRow(StringBuilder builder, ReadOnlySpan<Cell> cells)
    {
        if (cells.IsEmpty)
        {
            builder.Append("<span class=\"ft-run\"> </span>");
            return;
        }

        var hasOutput = false;
        var runStyle = cells[0];
        var runText = new StringBuilder();

        foreach (var cell in cells)
        {
            if (cell.IsContinuation)
            {
                continue;
            }

            hasOutput = true;
            var cellText = GetCellText(cell);
            if (runText.Length == 0)
            {
                runStyle = cell;
                runText.Append(cellText);
                continue;
            }

            if (SameStyle(runStyle, cell))
            {
                runText.Append(cellText);
                continue;
            }

            AppendRun(builder, runStyle, runText.ToString());
            runStyle = cell;
            runText.Clear();
            runText.Append(cellText);
        }

        if (!hasOutput)
        {
            builder.Append("<span class=\"ft-run\"> </span>");
            return;
        }

        if (runText.Length > 0)
        {
            AppendRun(builder, runStyle, runText.ToString());
        }
    }

    private static void AppendRun(StringBuilder builder, Cell cell, string text)
    {
        builder.Append("<span class=\"ft-run\" style=\"")
            .Append(BuildCss(cell))
            .Append("\">")
            .Append(WebUtility.HtmlEncode(text))
            .Append("</span>");
    }

    private static string BuildCss(Cell cell)
    {
        var foreground = cell.Foreground;
        var background = cell.Background;
        var attributes = cell.Attributes.Flags;

        if (attributes.HasFlag(CellStyleFlags.Reverse))
        {
            (foreground, background) = (background, foreground);
        }

        var fragments = new List<string>(6);
        if (foreground.A > 0)
        {
            fragments.Add($"color:{ToCssColor(foreground)}");
        }

        if (background.A > 0)
        {
            fragments.Add($"background:{ToCssColor(background)}");
        }

        if (attributes.HasFlag(CellStyleFlags.Bold))
        {
            fragments.Add("font-weight:700");
        }

        if (attributes.HasFlag(CellStyleFlags.Italic))
        {
            fragments.Add("font-style:italic");
        }

        if (attributes.HasFlag(CellStyleFlags.Underline) || attributes.HasFlag(CellStyleFlags.Strikethrough))
        {
            var decorations = new List<string>(2);
            if (attributes.HasFlag(CellStyleFlags.Underline))
            {
                decorations.Add("underline");
            }

            if (attributes.HasFlag(CellStyleFlags.Strikethrough))
            {
                decorations.Add("line-through");
            }

            fragments.Add($"text-decoration:{string.Join(' ', decorations)}");
        }

        if (attributes.HasFlag(CellStyleFlags.Dim))
        {
            fragments.Add("opacity:0.7");
        }

        if (attributes.HasFlag(CellStyleFlags.Hidden))
        {
            fragments.Add("visibility:hidden");
        }

        return string.Join(';', fragments);
    }

    private static string BuildDocumentHtml(string html, WebRenderOptions options) =>
        $$"""
        <!doctype html>
        <html lang="{{WebUtility.HtmlEncode(options.Language)}}" dir="{{WebUtility.HtmlEncode(options.Direction)}}">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>{{WebUtility.HtmlEncode(options.Title)}}</title>
          <style>
            :root {
              --ft-bg: #0f1722;
              --ft-panel: rgba(15, 23, 34, 0.86);
              --ft-border: rgba(102, 138, 170, 0.55);
              --ft-shadow: rgba(7, 12, 20, 0.38);
              --ft-copy: #eef5ff;
              --ft-muted: #9ab3c9;
              --ft-glow: rgba(90, 190, 255, 0.18);
            }
            * { box-sizing: border-box; }
            body {
              margin: 0;
              min-height: 100vh;
              padding: 24px;
              color: var(--ft-copy);
              font-family: "Cascadia Code", "JetBrains Mono", "Iosevka Term", monospace;
              background:
                radial-gradient(circle at top left, rgba(255, 178, 64, 0.18), transparent 34%),
                radial-gradient(circle at bottom right, rgba(64, 185, 255, 0.18), transparent 32%),
                linear-gradient(135deg, #071018, #142130 55%, #0d1824);
            }
            .frankentui-host {
              max-width: min(96vw, 1120px);
              margin: 0 auto;
              padding: 22px;
              border: 1px solid var(--ft-border);
              border-radius: 20px;
              background: var(--ft-panel);
              box-shadow: 0 24px 60px var(--ft-shadow);
              backdrop-filter: blur(10px);
            }
            .frankentui-frame {
              margin: 0;
              overflow-x: auto;
              white-space: pre;
              color: var(--ft-copy);
              text-shadow: 0 0 18px var(--ft-glow);
              line-height: 1.25;
            }
            .ft-row { display: block; }
            .ft-run { white-space: pre; }
          </style>
        </head>
        <body>
        {{html}}
        </body>
        </html>
        """;

    private static bool SameStyle(Cell left, Cell right) =>
        left.Foreground == right.Foreground &&
        left.Background == right.Background &&
        left.Attributes == right.Attributes;

    private static string GetCellText(Cell cell)
    {
        if (cell.IsEmpty)
        {
            return " ";
        }

        if (cell.Content.IsGrapheme)
        {
            return "\u25A1";
        }

        var rune = cell.Content.AsRune();
        return rune?.ToString() ?? " ";
    }

    private static string ToCssColor(PackedRgba color) =>
        color.A == 255
            ? $"rgb({color.R} {color.G} {color.B})"
            : $"rgb({color.R} {color.G} {color.B} / {color.A / 255d:0.###})";
}
