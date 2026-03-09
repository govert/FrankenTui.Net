using FrankenTui.Demo.Showcase;
using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;
using FrankenTui.Tty;
using FrankenTui.Widgets;
using RenderBuffer = FrankenTui.Render.Buffer;

var inlineMode = args.Contains("--inline", StringComparer.OrdinalIgnoreCase);
var width = ParseUShort(args, "--width", 64);
var height = ParseUShort(args, "--height", 18);
var frames = ParseInt(args, "--frames", 2);

ITerminalBackend backend = OperatingSystem.IsWindows()
    ? new WindowsTerminalBackend()
    : new UnixTerminalBackend();
await using var session = new TerminalSession(
    backend,
    new TerminalSessionOptions
    {
        InlineMode = inlineMode,
        UseMouseTracking = true
    });

await session.EnterAsync();

var current = new RenderBuffer(width, height);
var theme = Theme.DefaultTheme;
for (var frame = 0; frame < Math.Max(frames, 1); frame++)
{
    current.Clear();
    IWidget view = ShowcaseViewFactory.Build(inlineMode);
    view.Render(new RuntimeRenderContext(current, Rect.FromSize(width, height), theme));
    await session.PresentAsync(current, frame == 0 ? BufferDiff.Full(width, height) : null);
    if (inlineMode)
    {
        await backend.WriteControlAsync(Environment.NewLine);
    }
}

static ushort ParseUShort(string[] arguments, string name, ushort fallback) =>
    ushort.TryParse(Parse(arguments, name), out var value) ? value : fallback;

static int ParseInt(string[] arguments, string name, int fallback) =>
    int.TryParse(Parse(arguments, name), out var value) ? value : fallback;

static string? Parse(string[] arguments, string name)
{
    for (var index = 0; index < arguments.Length - 1; index++)
    {
        if (arguments[index].Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            return arguments[index + 1];
        }
    }

    return null;
}
