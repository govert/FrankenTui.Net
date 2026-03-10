using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Runtime;
using FrankenTui.Style;
using FrankenTui.Web;
using FrankenTui.Widgets;

namespace FrankenTui;

public static class Ui
{
    public static Theme Theme => Theme.DefaultTheme;

    public static MemoryTerminalBackend CreateMemoryBackend(ushort width, ushort height, TerminalCapabilities? capabilities = null) =>
        new(new Size(width, height), capabilities);

    public static AppSimulator<TModel, TMessage> CreateSimulator<TModel, TMessage>(ushort width, ushort height, Theme? theme = null) =>
        new(new Size(width, height), theme);

    public static AppSimulator<TModel, TMessage> CreateSimulator<TModel, TMessage>(
        ushort width,
        ushort height,
        Theme? theme,
        RuntimeExecutionPolicy policy) =>
        new(new Size(width, height), theme, policy);

    public static AppSession<TModel, TMessage> CreateSession<TModel, TMessage>(
        ushort width,
        ushort height,
        IAppProgram<TModel, TMessage> program,
        Theme? theme = null,
        RuntimeExecutionPolicy? policy = null,
        TModel? model = default) =>
        new(
            new AppRuntime<TModel, TMessage>(
                new MemoryTerminalBackend(new Size(width, height)),
                new Size(width, height),
                theme,
                policy),
            program,
            model);

    public static WidgetInputState CreateInputState(IEnumerable<string>? focusOrder = null, string language = "en-US")
    {
        var state = WidgetInputState.Default with { Language = language };
        return focusOrder is null ? state : state.WithFocusOrder(focusOrder);
    }

    public static ParagraphWidget Paragraph(string text) => new(text);

    public static ParagraphWidget Markdown(string markdown) =>
        new(string.Empty)
        {
            Document = MarkdownDocumentBuilder.Parse(markdown)
        };

    public static PanelWidget Panel(string title, IWidget child) => new() { Title = title, Child = child };

    public static IWidget HostedParityView(
        bool inlineMode = false,
        HostedParityScenarioId scenarioId = HostedParityScenarioId.Overview,
        int frame = 0,
        string language = "en-US",
        WidgetFlowDirection flowDirection = WidgetFlowDirection.LeftToRight) =>
        HostedParitySurface.Create(HostedParitySession.ForFrame(inlineMode, frame, scenarioId, language, flowDirection));

    public static WebFrame RenderWeb(
        IRuntimeView view,
        ushort width,
        ushort height,
        Theme? theme = null,
        WebRenderOptions? options = null) =>
        WebHost.Render(view, new Size(width, height), theme, options);

    public static WebFrame RenderHostedParity(
        bool inlineMode = false,
        ushort width = 64,
        ushort height = 18,
        HostedParityScenarioId scenarioId = HostedParityScenarioId.Overview,
        int frame = 0,
        string language = "en-US",
        WidgetFlowDirection flowDirection = WidgetFlowDirection.LeftToRight,
        Theme? theme = null)
    {
        var session = HostedParitySession.ForFrame(inlineMode, frame, scenarioId, language, flowDirection);
        var view = HostedParitySurface.Create(session);
        return WebHost.Render(view, new Size(width, height), theme, HostedParitySurface.CreateWebOptions(session));
    }
}
