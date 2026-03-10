namespace FrankenTui.Text;

public interface ITextWrapAccelerator
{
    bool TryWrapLine(string text, ushort width, TextWrapMode mode, List<string> lines);
}
