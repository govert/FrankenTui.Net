using System.Text;

namespace FrankenTui.Render;

public static class HeadlessBufferView
{
    public static string RowText(Buffer buffer, ushort row)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        var cells = buffer.GetRow(row);
        if (cells.IsEmpty)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(cells.Length);
        foreach (var cell in cells)
        {
            if (cell.IsContinuation)
            {
                continue;
            }

            if (cell.IsEmpty)
            {
                builder.Append(' ');
                continue;
            }

            if (cell.Content.IsGrapheme)
            {
                builder.Append('\u25A1');
                continue;
            }

            var rune = cell.Content.AsRune();
            if (rune is not null)
            {
                builder.Append(rune.Value.ToString());
            }
        }

        return builder.ToString().TrimEnd();
    }

    public static IReadOnlyList<string> ScreenText(Buffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        var rows = new string[buffer.Height];
        for (ushort y = 0; y < buffer.Height; y++)
        {
            rows[y] = RowText(buffer, y);
        }

        return rows;
    }

    public static string ScreenString(Buffer buffer) =>
        string.Join(Environment.NewLine, ScreenText(buffer));
}
