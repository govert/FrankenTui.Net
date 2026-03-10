using System.Text;
using FrankenTui.Render;
using FrankenTui.Style;
using FrankenTui.Text;

namespace FrankenTui.Extras;

public static class SyntaxHighlighter
{
    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "async",
        "await",
        "bool",
        "break",
        "class",
        "else",
        "false",
        "for",
        "foreach",
        "if",
        "int",
        "namespace",
        "new",
        "null",
        "private",
        "public",
        "record",
        "return",
        "sealed",
        "static",
        "string",
        "switch",
        "Task",
        "true",
        "using",
        "var",
        "void"
    };

    public static TextDocument HighlightCSharpDocument(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var lines = source.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(HighlightCSharpLine)
            .ToArray();
        return new TextDocument(lines);
    }

    public static TextLine HighlightCSharpLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var spans = new List<TextSpan>();
        var index = 0;
        while (index < line.Length)
        {
            if (index + 1 < line.Length && line[index] == '/' && line[index + 1] == '/')
            {
                spans.Add(new TextSpan(line[index..], UiStyle.Muted.WithFlags(CellStyleFlags.Italic)));
                break;
            }

            if (line[index] == '"')
            {
                var end = index + 1;
                while (end < line.Length)
                {
                    if (line[end] == '"' && line[end - 1] != '\\')
                    {
                        end++;
                        break;
                    }

                    end++;
                }

                spans.Add(new TextSpan(line[index..Math.Min(end, line.Length)], UiStyle.Success));
                index = Math.Min(end, line.Length);
                continue;
            }

            if (char.IsLetter(line[index]) || line[index] == '_')
            {
                var end = index + 1;
                while (end < line.Length && (char.IsLetterOrDigit(line[end]) || line[end] == '_'))
                {
                    end++;
                }

                var token = line[index..end];
                var style = CSharpKeywords.Contains(token)
                    ? UiStyle.Accent
                    : UiStyle.Default;
                spans.Add(new TextSpan(token, style));
                index = end;
                continue;
            }

            if (char.IsDigit(line[index]))
            {
                var end = index + 1;
                while (end < line.Length && char.IsDigit(line[end]))
                {
                    end++;
                }

                spans.Add(new TextSpan(line[index..end], UiStyle.Warning));
                index = end;
                continue;
            }

            spans.Add(new TextSpan(line[index].ToString(), UiStyle.Default));
            index++;
        }

        return new TextLine(spans);
    }
}
