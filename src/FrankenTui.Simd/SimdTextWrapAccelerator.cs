using System.Buffers;
using System.Text;
using FrankenTui.Core;
using FrankenTui.Text;

namespace FrankenTui.Simd;

internal sealed class SimdTextWrapAccelerator : ITextWrapAccelerator
{
    public bool TryWrapLine(string text, ushort width, TextWrapMode mode, List<string> lines)
    {
        if (mode != TextWrapMode.Word || text.Length == 0)
        {
            return false;
        }

        var words = text.Split(' ', StringSplitOptions.None);
        var widths = ArrayPool<int>.Shared.Rent(words.Length);
        try
        {
            for (var index = 0; index < words.Length; index++)
            {
                widths[index] = TerminalTextWidth.DisplayWidth(words[index]);
            }

            var lineBuilder = new StringBuilder(text.Length);
            var lineWidth = 0;
            for (var index = 0; index < words.Length; index++)
            {
                var word = words[index];
                var wordWidth = widths[index];
                var candidateWidth = lineBuilder.Length == 0 ? wordWidth : lineWidth + 1 + wordWidth;
                if (candidateWidth <= width)
                {
                    if (lineBuilder.Length > 0)
                    {
                        lineBuilder.Append(' ');
                    }

                    lineBuilder.Append(word);
                    lineWidth = candidateWidth;
                    continue;
                }

                if (lineBuilder.Length > 0)
                {
                    lines.Add(lineBuilder.ToString());
                    lineBuilder.Clear();
                    lineWidth = 0;
                }

                if (wordWidth <= width)
                {
                    lineBuilder.Append(word);
                    lineWidth = wordWidth;
                    continue;
                }

                lines.AddRange(TextWrapper.WrapLine(word, width, TextWrapMode.Character));
            }

            if (lineBuilder.Length > 0)
            {
                lines.Add(lineBuilder.ToString());
            }
        }
        finally
        {
            ArrayPool<int>.Shared.Return(widths);
        }

        return true;
    }
}
