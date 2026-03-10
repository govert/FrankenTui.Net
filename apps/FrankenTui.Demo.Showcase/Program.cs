using System.Text;
using FrankenTui.Demo.Showcase;
using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;
using FrankenTui.Tty;
using FrankenTui.Widgets;
using RenderBuffer = FrankenTui.Render.Buffer;

var inlineMode = HasFlag(args, "--inline");
var interactiveMode = HasFlag(args, "--interactive");
var width = ParseUShort(args, "--width", 64);
var height = ParseUShort(args, "--height", 18);
var frames = ParseInt(args, "--frames", 2);
var language = Parse(args, "--lang") ?? "en-US";
var flowDirection = HasFlag(args, "--rtl")
    ? WidgetFlowDirection.RightToLeft
    : WidgetFlowDirection.LeftToRight;
var scenario = ParseScenario(Parse(args, "--scenario"));

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

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

await session.EnterAsync(cancellation.Token);

if (interactiveMode)
{
    await RunInteractiveAsync(
        backend,
        inlineMode,
        width,
        height,
        scenario,
        language,
        flowDirection,
        cancellation.Token);
}
else
{
    var current = new RenderBuffer(width, height);
    var theme = Theme.DefaultTheme;
    for (var frame = 0; frame < Math.Max(frames, 1); frame++)
    {
        current.Clear();
        IWidget view = ShowcaseViewFactory.Build(inlineMode, scenario, frame, language, flowDirection);
        view.Render(new RuntimeRenderContext(current, Rect.FromSize(width, height), theme));
        await session.PresentAsync(current, frame == 0 ? BufferDiff.Full(width, height) : null, cancellationToken: cancellation.Token);
        if (inlineMode)
        {
            await backend.WriteControlAsync(Environment.NewLine, cancellation.Token);
        }
    }
}

static ushort ParseUShort(string[] arguments, string name, ushort fallback) =>
    ushort.TryParse(Parse(arguments, name), out var value) ? value : fallback;

static int ParseInt(string[] arguments, string name, int fallback) =>
    int.TryParse(Parse(arguments, name), out var value) ? value : fallback;

static bool HasFlag(string[] arguments, string name) =>
    Array.Exists(arguments, argument => argument.Equals(name, StringComparison.OrdinalIgnoreCase));

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

static HostedParityScenarioId ParseScenario(string? value) =>
    value?.Trim().ToLowerInvariant() switch
    {
        "interaction" => HostedParityScenarioId.Interaction,
        "tooling" => HostedParityScenarioId.Tooling,
        _ => HostedParityScenarioId.Overview
    };

static async Task RunInteractiveAsync(
    ITerminalBackend backend,
    bool inlineMode,
    ushort width,
    ushort height,
    HostedParityScenarioId scenario,
    string language,
    WidgetFlowDirection flowDirection,
    CancellationToken cancellationToken)
{
    var runtime = new AppRuntime<ShowcaseDemoState, ShowcaseDemoMessage>(
        backend,
        new Size(width, height),
        Theme.DefaultTheme);
    var program = new ShowcaseInteractiveProgram(
        inlineMode,
        scenario,
        language,
        flowDirection,
        new Size(width, height));
    var app = new AppSession<ShowcaseDemoState, ShowcaseDemoMessage>(runtime, program);
    await runtime.ResizeAsync(new Size(width, height), cancellationToken);
    await app.RenderCurrentAsync(cancellationToken);
    if (inlineMode)
    {
        await backend.WriteControlAsync(Environment.NewLine, cancellationToken);
    }

    var knownSize = runtime.Size;
    while (!app.Model.QuitRequested && !cancellationToken.IsCancellationRequested)
    {
        var consoleSize = ReadConsoleSize();
        if (consoleSize is { } resized && resized != knownSize)
        {
            knownSize = resized;
            await app.ResizeAsync(
                resized,
                static size => new ShowcaseResizeMessage(size),
                cancellationToken);
            if (inlineMode)
            {
                await backend.WriteControlAsync(Environment.NewLine, cancellationToken);
            }
        }

        var terminalEvent = await ReadConsoleEventAsync(cancellationToken);
        if (terminalEvent is null)
        {
            continue;
        }

        app.Enqueue(new ShowcaseInputMessage(terminalEvent));
        var batch = await app.DrainAsync(cancellationToken: cancellationToken);
        if (inlineMode && batch.Steps.Count > 0)
        {
            await backend.WriteControlAsync(Environment.NewLine, cancellationToken);
        }
    }
}

static Size? ReadConsoleSize()
{
    try
    {
        var width = (ushort)Math.Clamp(Console.BufferWidth, 1, ushort.MaxValue);
        var height = (ushort)Math.Clamp(Console.BufferHeight, 1, ushort.MaxValue);
        return new Size(width, height);
    }
    catch
    {
        return null;
    }
}

static async Task<TerminalEvent?> ReadConsoleEventAsync(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        if (Console.KeyAvailable)
        {
            return MapConsoleKey(Console.ReadKey(intercept: true));
        }

        await Task.Delay(20, cancellationToken);
    }

    return null;
}

static TerminalEvent MapConsoleKey(ConsoleKeyInfo keyInfo)
{
    var modifiers =
        (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift) ? TerminalModifiers.Shift : TerminalModifiers.None) |
        (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt) ? TerminalModifiers.Alt : TerminalModifiers.None) |
        (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) ? TerminalModifiers.Control : TerminalModifiers.None);

    return keyInfo.Key switch
    {
        ConsoleKey.Tab => TerminalEvent.Key(new KeyGesture(TerminalKey.Tab, modifiers)),
        ConsoleKey.Enter => TerminalEvent.Key(new KeyGesture(TerminalKey.Enter, modifiers)),
        ConsoleKey.Escape => TerminalEvent.Key(new KeyGesture(TerminalKey.Escape, modifiers)),
        ConsoleKey.UpArrow => TerminalEvent.Key(new KeyGesture(TerminalKey.Up, modifiers)),
        ConsoleKey.DownArrow => TerminalEvent.Key(new KeyGesture(TerminalKey.Down, modifiers)),
        ConsoleKey.LeftArrow => TerminalEvent.Key(new KeyGesture(TerminalKey.Left, modifiers)),
        ConsoleKey.RightArrow => TerminalEvent.Key(new KeyGesture(TerminalKey.Right, modifiers)),
        ConsoleKey.Backspace => TerminalEvent.Key(new KeyGesture(TerminalKey.Backspace, modifiers)),
        _ when keyInfo.KeyChar != '\0' => TerminalEvent.Key(
            new KeyGesture(TerminalKey.Character, modifiers, new Rune(keyInfo.KeyChar))),
        _ => TerminalEvent.Key(new KeyGesture(TerminalKey.Unknown, modifiers))
    };
}
