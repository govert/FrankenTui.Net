using System.Text;

namespace FrankenTui.Core;

public sealed class CursorManager
{
    private static readonly byte[] DecSaveSequence = "\u001b7"u8.ToArray();
    private static readonly byte[] DecRestoreSequence = "\u001b8"u8.ToArray();
    private static readonly byte[] AnsiSaveSequence = "\u001b[s"u8.ToArray();
    private static readonly byte[] AnsiRestoreSequence = "\u001b[u"u8.ToArray();

    public CursorManager(CursorSaveStrategy strategy)
    {
        Strategy = strategy;
    }

    public CursorSaveStrategy Strategy { get; }

    public (ushort Column, ushort Row)? SavedPosition { get; private set; }

    public static CursorManager Detect(TerminalCapabilities capabilities) =>
        new(capabilities.InScreen ? CursorSaveStrategy.Ansi : CursorSaveStrategy.Dec);

    public void Save(Stream output, ushort column, ushort row)
    {
        ArgumentNullException.ThrowIfNull(output);

        switch (Strategy)
        {
            case CursorSaveStrategy.Dec:
                output.Write(DecSaveSequence);
                break;
            case CursorSaveStrategy.Ansi:
                output.Write(AnsiSaveSequence);
                break;
            case CursorSaveStrategy.Emulated:
                SavedPosition = (column, row);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Restore(Stream output)
    {
        ArgumentNullException.ThrowIfNull(output);

        switch (Strategy)
        {
            case CursorSaveStrategy.Dec:
                output.Write(DecRestoreSequence);
                break;
            case CursorSaveStrategy.Ansi:
                output.Write(AnsiRestoreSequence);
                break;
            case CursorSaveStrategy.Emulated:
                if (SavedPosition is { } position)
                {
                    var sequence = Encoding.ASCII.GetBytes(
                        $"\u001b[{position.Row + 1};{position.Column + 1}H");
                    output.Write(sequence);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Clear() => SavedPosition = null;
}
