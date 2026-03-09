using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Runtime;
using FrankenTui.Style;
using FrankenTui.Widgets;

namespace FrankenTui;

public static class Ui
{
    public static Theme Theme => Theme.DefaultTheme;

    public static MemoryTerminalBackend CreateMemoryBackend(ushort width, ushort height, TerminalCapabilities? capabilities = null) =>
        new(new Size(width, height), capabilities);

    public static AppSimulator<TModel, TMessage> CreateSimulator<TModel, TMessage>(ushort width, ushort height, Theme? theme = null) =>
        new(new Size(width, height), theme);

    public static ParagraphWidget Paragraph(string text) => new(text);

    public static PanelWidget Panel(string title, IWidget child) => new() { Title = title, Child = child };
}
