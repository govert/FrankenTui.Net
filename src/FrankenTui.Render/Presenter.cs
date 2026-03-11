using System.Text;
using FrankenTui.Core;

namespace FrankenTui.Render;

public sealed class Presenter
{
    private static readonly StyleState DefaultStyle = new(PackedRgba.White, PackedRgba.Transparent, CellStyleFlags.None);

    private readonly Encoding _encoding = Encoding.UTF8;
    private StyleState _currentStyle = DefaultStyle;
    private uint _currentLinkId;
    private ushort? _cursorColumn;
    private ushort? _cursorRow;

    public Presenter(TerminalCapabilities capabilities)
    {
        Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
    }

    public TerminalCapabilities Capabilities { get; }

    public int FrameByteBudget { get; set; } = int.MaxValue;

    public void Reset()
    {
        _currentStyle = DefaultStyle;
        _currentLinkId = 0;
        _cursorColumn = null;
        _cursorRow = null;
    }

    public PresentResult Present(
        Buffer buffer,
        BufferDiff diff,
        IReadOnlyDictionary<uint, string>? links = null,
        ushort rowOffset = 0,
        ushort columnOffset = 0,
        bool wrapSyncOutput = true)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(diff);

        if (diff.IsEmpty)
        {
            return new PresentResult(string.Empty, 0, 0, 0, false, false);
        }

        var builder = new StringBuilder();
        var bytesWritten = 0;
        var truncated = false;
        var usedSyncOutput = wrapSyncOutput && Capabilities.UseSyncOutput();
        var runs = diff.Runs();

        if (usedSyncOutput)
        {
            ForceAppend(builder, AnsiBuilder.SyncOutputBegin(), ref bytesWritten);
        }

        foreach (var run in runs)
        {
            var targetColumn = (ushort)Math.Min(ushort.MaxValue, run.X0 + columnOffset);
            var targetRow = (ushort)Math.Min(ushort.MaxValue, run.Y + rowOffset);
            if (!TryAppendMove(builder, targetColumn, targetRow, ref bytesWritten))
            {
                truncated = true;
                break;
            }

            var x = run.X0;
            while (x <= run.X1)
            {
                var cell = buffer.Get(x, run.Y) ?? Cell.Empty;
                var advance = (ushort)Math.Max(cell.Content.Width(), 1);

                if (!cell.IsContinuation)
                {
                    if (!TryApplyHyperlink(builder, cell, links, ref bytesWritten) ||
                        !TryApplyStyle(builder, cell, ref bytesWritten) ||
                        !TryAppendContent(builder, cell, ref bytesWritten))
                    {
                        truncated = true;
                        break;
                    }
                }

                _cursorColumn = (ushort)Math.Min(buffer.Width, x + advance);
                _cursorRow = run.Y;

                if (x > ushort.MaxValue - advance)
                {
                    break;
                }

                x += advance;
            }

            if (truncated)
            {
                break;
            }
        }

        if (_currentLinkId != 0)
        {
            ForceAppend(builder, AnsiBuilder.HyperlinkEnd(), ref bytesWritten);
            _currentLinkId = 0;
        }

        if (_currentStyle != DefaultStyle)
        {
            ForceAppend(builder, AnsiBuilder.SgrReset(), ref bytesWritten);
            _currentStyle = DefaultStyle;
        }

        if (usedSyncOutput)
        {
            ForceAppend(builder, AnsiBuilder.SyncOutputEnd(), ref bytesWritten);
        }

        return new PresentResult(builder.ToString(), bytesWritten, diff.Count, runs.Count, usedSyncOutput, truncated);
    }

    private bool TryAppendMove(StringBuilder builder, ushort targetColumn, ushort targetRow, ref int bytesWritten)
    {
        var moveBuilder = new StringBuilder();
        AnsiBuilder.AppendBestCursorMove(moveBuilder, _cursorColumn, _cursorRow, targetColumn, targetRow);
        if (moveBuilder.Length == 0)
        {
            _cursorColumn = targetColumn;
            _cursorRow = targetRow;
            return true;
        }

        if (!TryAppend(builder, moveBuilder.ToString(), ref bytesWritten))
        {
            return false;
        }

        _cursorColumn = targetColumn;
        _cursorRow = targetRow;
        return true;
    }

    private bool TryApplyStyle(StringBuilder builder, Cell cell, ref int bytesWritten)
    {
        var desired = new StyleState(cell.Foreground, cell.Background, cell.Attributes.Flags);
        if (desired == _currentStyle)
        {
            return true;
        }

        var styleBuilder = new StringBuilder();
        AnsiBuilder.AppendSgrReset(styleBuilder);
        if (desired.Flags != CellStyleFlags.None)
        {
            AnsiBuilder.AppendSgrFlags(styleBuilder, desired.Flags);
        }

        if (desired.Foreground != DefaultStyle.Foreground)
        {
            AnsiBuilder.AppendForeground(styleBuilder, desired.Foreground);
        }

        if (desired.Background != DefaultStyle.Background)
        {
            AnsiBuilder.AppendBackground(styleBuilder, desired.Background);
        }

        if (!TryAppend(builder, styleBuilder.ToString(), ref bytesWritten))
        {
            return false;
        }

        _currentStyle = desired;
        return true;
    }

    private bool TryApplyHyperlink(
        StringBuilder builder,
        Cell cell,
        IReadOnlyDictionary<uint, string>? links,
        ref int bytesWritten)
    {
        var desiredLinkId = cell.Attributes.LinkId;
        if (!Capabilities.UseHyperlinks())
        {
            desiredLinkId = CellAttributes.LinkIdNone;
        }

        if (desiredLinkId == _currentLinkId)
        {
            return true;
        }

        if (_currentLinkId != 0)
        {
            if (!TryAppend(builder, AnsiBuilder.HyperlinkEnd(), ref bytesWritten))
            {
                return false;
            }

            _currentLinkId = 0;
        }

        if (desiredLinkId == 0 || links is null || !links.TryGetValue(desiredLinkId, out var url))
        {
            return true;
        }

        var linkBuilder = new StringBuilder();
        if (!AnsiBuilder.TryAppendHyperlinkStart(linkBuilder, url))
        {
            return true;
        }

        if (!TryAppend(builder, linkBuilder.ToString(), ref bytesWritten))
        {
            return false;
        }

        _currentLinkId = desiredLinkId;
        return true;
    }

    private bool TryAppendContent(StringBuilder builder, Cell cell, ref int bytesWritten)
    {
        var content = RenderContent(cell);
        return TryAppend(builder, content, ref bytesWritten);
    }

    private string RenderContent(Cell cell)
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
        return rune is null
            ? " "
            : AnsiBuilder.SanitizeText(rune.Value.ToString());
    }

    private bool TryAppend(StringBuilder builder, string fragment, ref int bytesWritten)
    {
        var fragmentBytes = _encoding.GetByteCount(fragment);
        if (bytesWritten > FrameByteBudget - fragmentBytes)
        {
            return false;
        }

        builder.Append(fragment);
        bytesWritten += fragmentBytes;
        return true;
    }

    private void ForceAppend(StringBuilder builder, string fragment, ref int bytesWritten)
    {
        builder.Append(fragment);
        bytesWritten += _encoding.GetByteCount(fragment);
    }

    private readonly record struct StyleState(PackedRgba Foreground, PackedRgba Background, CellStyleFlags Flags);
}
