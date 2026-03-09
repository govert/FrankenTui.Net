using System.Text;

namespace FrankenTui.Core;

public readonly record struct KeyGesture(TerminalKey Key, TerminalModifiers Modifiers, Rune? Character = null)
{
    public bool IsCharacter => Key == TerminalKey.Character && Character is not null;
}
