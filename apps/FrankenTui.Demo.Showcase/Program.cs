using System.Text;
using FrankenTui.Demo.Showcase;
using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Simd;
using FrankenTui.Style;
using FrankenTui.Tty;
using FrankenTui.Widgets;
using RenderBuffer = FrankenTui.Render.Buffer;

SimdAccelerators.EnableIfSupported();

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
        "extras" => HostedParityScenarioId.Extras,
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
    var controller = new RuntimeInputController<ShowcaseDemoState, ShowcaseDemoMessage>(
        static model => model.Session.CreateKeybindingState(),
        static (_, input) => [new ShowcaseInputMessage(input)],
        static (_, terminalEvent) => HostedParityInputEngine.Translate(terminalEvent),
        keybindingConfig: KeybindingConfig.FromEnvironment());
    await runtime.ResizeAsync(new Size(width, height), cancellationToken);
    await app.RenderCurrentAsync(cancellationToken);

    while (!app.Model.QuitRequested && !cancellationToken.IsCancellationRequested)
    {
        if (!await backend.PollEventAsync(TimeSpan.FromMilliseconds(20), cancellationToken))
        {
            await controller.TickAsync(app, DateTimeOffset.UtcNow, cancellationToken);
            continue;
        }

        var terminalEvent = await backend.ReadEventAsync(cancellationToken);
        if (terminalEvent is null)
        {
            continue;
        }

        await controller.ProcessAsync(app, terminalEvent, cancellationToken);
    }
}
