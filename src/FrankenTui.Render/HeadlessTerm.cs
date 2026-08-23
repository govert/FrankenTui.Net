using System.Text;

namespace FrankenTui.Render;

/// <summary>
/// A terminal emulator-backed test harness for CI and snapshot verification.
/// </summary>
/// <remarks>
/// Ported from <c>crates/ftui-render/src/headless.rs</c> at upstream commit
/// <c>15cc6543f76b814394c590f9e7719dedd6684e4c</c>.
/// </remarks>
public sealed class HeadlessTerm
{
    private readonly List<byte> _capturedOutput = [];

    public HeadlessTerm(ushort width, ushort height)
    {
        ArgumentOutOfRangeException.ThrowIfZero(width);
        ArgumentOutOfRangeException.ThrowIfZero(height);
        Model = new TerminalModel(width, height);
    }

    public ushort Width => Model.Width;

    public ushort Height => Model.Height;

    public (ushort Column, ushort Row) Cursor => (Model.CursorColumn, Model.CursorRow);

    public TerminalModel Model { get; }

    /// <summary>
    /// Gets a stable snapshot of every byte supplied to <see cref="Process(ReadOnlySpan{byte})"/>.
    /// </summary>
    public ReadOnlyMemory<byte> CapturedOutput => _capturedOutput.ToArray();

    public void Process(ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes)
        {
            _capturedOutput.Add(value);
        }

        Model.Process(bytes);
    }

    /// <summary>
    /// Managed convenience overload that captures the UTF-8 encoding of <paramref name="text"/>.
    /// </summary>
    public void Process(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Process(Encoding.UTF8.GetBytes(text));
    }

    public string RowText(int row) =>
        row >= 0 && row < Height ? Model.RowText((ushort)row) : string.Empty;

    public IReadOnlyList<string> ScreenText()
    {
        var rows = new string[Height];
        for (ushort row = 0; row < Height; row++)
        {
            rows[row] = Model.RowText(row);
        }

        return rows;
    }

    public string ScreenString() => string.Join('\n', ScreenText());

    public void Reset()
    {
        Model.Reset();
        _capturedOutput.Clear();
    }

    public void AssertMatches(IReadOnlyList<string> expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        var actual = ScreenText();
        if (actual.Count != expected.Count)
        {
            throw new InvalidOperationException(
                $"HeadlessTerm: line count mismatch: got {actual.Count} lines, expected {expected.Count} lines\n" +
                $"Hint: expected list length must equal terminal height ({Height})");
        }

        var mismatches = FindMismatches(actual, expected);
        if (mismatches.Count != 0)
        {
            throw new InvalidOperationException(
                $"HeadlessTerm: screen content mismatch\n{FormatDiff(mismatches)}");
        }
    }

    public void AssertRow(int row, string expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        var actual = RowText(row);
        var expectedTrimmed = expected.TrimEnd();
        if (!string.Equals(actual, expectedTrimmed, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"HeadlessTerm: row {row} mismatch\n  got:  {DebugString(actual)}\n  want: {DebugString(expectedTrimmed)}");
        }
    }

    public void AssertCursor(ushort column, ushort row)
    {
        var actual = Cursor;
        if (actual != (column, row))
        {
            throw new InvalidOperationException(
                $"HeadlessTerm: cursor position mismatch\n  got:  ({actual.Column}, {actual.Row})\n  want: ({column}, {row})");
        }
    }

    public ScreenDiff? Diff(IReadOnlyList<string> expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        var actual = ScreenText();
        var mismatches = FindMismatches(actual, expected);
        return mismatches.Count == 0 && actual.Count == expected.Count
            ? null
            : new ScreenDiff(actual.Count, expected.Count, mismatches);
    }

    public void Export(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var output = new StringBuilder();
        output.Append("=== HeadlessTerm Export ===\n");
        output.Append("Size: ").Append(Width).Append('x').Append(Height).Append('\n');
        output.Append("Cursor: (").Append(Cursor.Column).Append(", ").Append(Cursor.Row).Append(")\n");
        output.Append("Captured output: ").Append(_capturedOutput.Count).Append(" bytes\n\n");
        output.Append("--- Screen Content ---\n");

        for (var row = 0; row < Height; row++)
        {
            output.Append(row.ToString().PadLeft(3)).Append("| ").Append(RowText(row)).Append('\n');
        }

        output.Append("\n--- ANSI Dump ---\n");
        output.Append(TerminalModel.DumpSequences(_capturedOutput.ToArray())).Append('\n');
        File.WriteAllText(path, output.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public string ExportString()
    {
        var output = new StringBuilder();
        output.Append(Width).Append('x').Append(Height)
            .Append(" cursor=(").Append(Cursor.Column).Append(',').Append(Cursor.Row).Append(")\n");

        for (var row = 0; row < Height; row++)
        {
            output.Append(row.ToString().PadLeft(3)).Append("| ").Append(RowText(row)).Append('\n');
        }

        return output.ToString();
    }

    internal static string FormatDiff(IReadOnlyList<LineDiff> mismatches)
    {
        var output = new StringBuilder();
        foreach (var difference in mismatches)
        {
            output.Append("  line ").Append(difference.Line).Append(":\n");
            output.Append("    got:  ").Append(DebugString(difference.Got)).Append('\n');
            output.Append("    want: ").Append(DebugString(difference.Want)).Append('\n');

            var gotRunes = difference.Got.EnumerateRunes().ToArray();
            var wantRunes = difference.Want.EnumerateRunes().ToArray();
            var commonLength = Math.Min(gotRunes.Length, wantRunes.Length);
            var firstDifference = -1;
            for (var index = 0; index < commonLength; index++)
            {
                if (gotRunes[index] != wantRunes[index])
                {
                    firstDifference = index;
                    break;
                }
            }

            if (firstDifference >= 0)
            {
                output.Append("    first difference at column ").Append(firstDifference).Append('\n');
            }
            else if (Encoding.UTF8.GetByteCount(difference.Got) != Encoding.UTF8.GetByteCount(difference.Want))
            {
                var shorter = Math.Min(
                    Encoding.UTF8.GetByteCount(difference.Got),
                    Encoding.UTF8.GetByteCount(difference.Want));
                output.Append("    diverges at column ").Append(shorter).Append(" (length difference)\n");
            }
        }

        return output.ToString();
    }

    private static List<LineDiff> FindMismatches(
        IReadOnlyList<string> actual,
        IReadOnlyList<string> expected)
    {
        var count = Math.Min(actual.Count, expected.Count);
        var mismatches = new List<LineDiff>();
        for (var index = 0; index < count; index++)
        {
            var expectedTrimmed = expected[index].TrimEnd();
            if (!string.Equals(actual[index], expectedTrimmed, StringComparison.Ordinal))
            {
                mismatches.Add(new LineDiff(index, actual[index], expectedTrimmed));
            }
        }

        return mismatches;
    }

    private static string DebugString(string value)
    {
        var output = new StringBuilder(value.Length + 2).Append('"');
        foreach (var rune in value.EnumerateRunes())
        {
            output.Append(rune.Value switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ when Rune.IsControl(rune) => $"\\u{{{rune.Value:x}}}",
                _ => rune.ToString(),
            });
        }

        return output.Append('"').ToString();
    }
}

public sealed class LineDiff
{
    public LineDiff(int line, string got, string want)
    {
        Line = line;
        Got = got ?? throw new ArgumentNullException(nameof(got));
        Want = want ?? throw new ArgumentNullException(nameof(want));
    }

    public int Line { get; set; }

    public string Got { get; set; }

    public string Want { get; set; }

    public LineDiff Clone() => new(Line, Got, Want);
}

public sealed class ScreenDiff
{
    public ScreenDiff(int actualLines, int expectedLines, IEnumerable<LineDiff> mismatches)
    {
        ArgumentNullException.ThrowIfNull(mismatches);
        ActualLines = actualLines;
        ExpectedLines = expectedLines;
        Mismatches = [.. mismatches];
    }

    public int ActualLines { get; set; }

    public int ExpectedLines { get; set; }

    public List<LineDiff> Mismatches { get; set; }

    public ScreenDiff Clone() =>
        new(ActualLines, ExpectedLines, Mismatches.Select(difference => difference.Clone()));

    public override string ToString()
    {
        var output = new StringBuilder();
        if (ActualLines != ExpectedLines)
        {
            output.Append("Line count: got ").Append(ActualLines)
                .Append(", expected ").Append(ExpectedLines).Append('\n');
        }

        return output.Append(HeadlessTerm.FormatDiff(Mismatches)).ToString();
    }
}
