using System.Text;
using FrankenTui.Core;
using FrankenTui.Render;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Backend;

public sealed class InlineTerminalWriter
{
    private readonly Presenter _presenter;
    private readonly Encoding _encoding = Encoding.UTF8;

    private RenderBuffer? _previous;
    private InlineRegion? _lastRegion;
    private Size _terminalSize;

    public InlineTerminalWriter(TerminalCapabilities capabilities, Size terminalSize)
    {
        Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
        _terminalSize = terminalSize.IsEmpty ? new Size(1, 1) : terminalSize;
        _presenter = new Presenter(capabilities);
    }

    public TerminalCapabilities Capabilities { get; }

    public Size TerminalSize
    {
        get => _terminalSize;
        set => _terminalSize = value.IsEmpty ? new Size(1, 1) : value;
    }

    public void ResetState()
    {
        _previous = null;
        _lastRegion = null;
        _presenter.Reset();
    }

    public PresentResult Present(
        RenderBuffer buffer,
        BufferDiff? diff = null,
        IReadOnlyDictionary<uint, string>? links = null)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        var region = InlineRegion.From(TerminalSize, buffer);
        var fullRedraw =
            _previous is null ||
            _previous.Width != buffer.Width ||
            _previous.Height != buffer.Height ||
            !_lastRegion.HasValue ||
            _lastRegion.Value.StartRow != region.StartRow;

        var effectiveDiff = fullRedraw
            ? BufferDiff.Full(buffer.Width, buffer.Height)
            : diff ?? BufferDiff.Compute(_previous!, buffer);

        var builder = new StringBuilder();
        var bytesWritten = 0;
        var usedSyncOutput = Capabilities.UseSyncOutput();

        if (usedSyncOutput)
        {
            ForceAppend(builder, AnsiBuilder.SyncOutputBegin(), ref bytesWritten);
        }

        ForceAppend(builder, AnsiBuilder.CursorSave(), ref bytesWritten);
        AppendClearRegionDiff(builder, region, ref bytesWritten);
        if (fullRedraw)
        {
            AppendClearRows(builder, region.StartRow, region.Height, ref bytesWritten);
        }

        _presenter.Reset();
        var frame = _presenter.Present(
            buffer,
            effectiveDiff,
            links,
            rowOffset: region.StartRow,
            columnOffset: 0,
            wrapSyncOutput: false);
        ForceAppend(builder, frame.Output, ref bytesWritten);
        ForceAppend(builder, AnsiBuilder.CursorRestore(), ref bytesWritten);

        if (usedSyncOutput)
        {
            ForceAppend(builder, AnsiBuilder.SyncOutputEnd(), ref bytesWritten);
        }

        _previous = buffer.Clone();
        _lastRegion = region;
        return new PresentResult(
            builder.ToString(),
            bytesWritten,
            frame.ChangedCells,
            frame.RunCount,
            usedSyncOutput,
            frame.Truncated);
    }

    public string WriteLog(string text, TerminalLogWriteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (!_lastRegion.HasValue || _lastRegion.Value.StartRow == 0)
        {
            return string.Empty;
        }

        var safeText = options?.AllowRaw == true
            ? text
            : TerminalOutputSanitizer.Sanitize(text);
        var safeLine = ToOverlayLogLine(safeText, TerminalSize.Width);
        if (safeLine.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append(AnsiBuilder.CursorSave());
        builder.Append(AnsiBuilder.CursorPosition((ushort)(_lastRegion.Value.StartRow - 1), 0));
        builder.Append(AnsiBuilder.EraseLine(EraseLineMode.All));
        builder.Append(safeLine);
        builder.Append(AnsiBuilder.CursorRestore());
        return builder.ToString();
    }

    private static string ToOverlayLogLine(string text, ushort terminalWidth)
    {
        if (terminalWidth == 0)
        {
            return string.Empty;
        }

        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var firstLine = normalized.Split('\n', 2)[0];
        if (firstLine.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var width = 0;
        foreach (var rune in firstLine.EnumerateRunes())
        {
            var runeWidth = Math.Max(TerminalTextWidth.RuneWidth(rune), 1);
            if (width > terminalWidth - runeWidth)
            {
                break;
            }

            builder.Append(rune.ToString());
            width += runeWidth;
        }

        return builder.ToString();
    }

    private void AppendClearRegionDiff(StringBuilder builder, InlineRegion current, ref int bytesWritten)
    {
        if (!_lastRegion.HasValue)
        {
            return;
        }

        var previous = _lastRegion.Value;
        var previousStart = previous.StartRow;
        var previousEnd = (ushort)(previous.StartRow + previous.Height);
        var currentStart = current.StartRow;
        var currentEnd = (ushort)(current.StartRow + current.Height);

        if (currentStart > previousStart)
        {
            AppendClearRows(builder, previousStart, (ushort)(currentStart - previousStart), ref bytesWritten);
        }

        if (currentEnd < previousEnd)
        {
            AppendClearRows(builder, currentEnd, (ushort)(previousEnd - currentEnd), ref bytesWritten);
        }
    }

    private void AppendClearRows(StringBuilder builder, ushort startRow, ushort height, ref int bytesWritten)
    {
        var endRow = (ushort)Math.Min(startRow + height, TerminalSize.Height);
        for (var row = startRow; row < endRow; row++)
        {
            ForceAppend(builder, AnsiBuilder.CursorPosition((ushort)row, 0), ref bytesWritten);
            ForceAppend(builder, AnsiBuilder.EraseLine(EraseLineMode.All), ref bytesWritten);
        }
    }

    private void ForceAppend(StringBuilder builder, string fragment, ref int bytesWritten)
    {
        if (fragment.Length == 0)
        {
            return;
        }

        builder.Append(fragment);
        bytesWritten += _encoding.GetByteCount(fragment);
    }

    private readonly record struct InlineRegion(ushort StartRow, ushort Height)
    {
        public static InlineRegion From(Size terminalSize, RenderBuffer buffer)
        {
            var height = (ushort)Math.Min(buffer.Height, terminalSize.Height);
            var startRow = (ushort)(terminalSize.Height - height);
            return new InlineRegion(startRow, height);
        }
    }
}
