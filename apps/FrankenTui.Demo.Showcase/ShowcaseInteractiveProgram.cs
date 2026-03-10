using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

internal sealed class ShowcaseInteractiveProgram : IAppProgram<ShowcaseDemoState, ShowcaseDemoMessage>
{
    private readonly bool _inlineMode;
    private readonly HostedParityScenarioId _scenarioId;
    private readonly string _language;
    private readonly WidgetFlowDirection _flowDirection;
    private readonly Size _initialSize;

    public ShowcaseInteractiveProgram(
        bool inlineMode,
        HostedParityScenarioId scenarioId,
        string language,
        WidgetFlowDirection flowDirection,
        Size initialSize)
    {
        _inlineMode = inlineMode;
        _scenarioId = scenarioId;
        _language = language;
        _flowDirection = flowDirection;
        _initialSize = initialSize;
    }

    public ShowcaseDemoState Initialize() =>
        new(
            HostedParitySession.Create(_inlineMode, _scenarioId, _language, _flowDirection),
            _initialSize);

    public UpdateResult<ShowcaseDemoState, ShowcaseDemoMessage> Update(ShowcaseDemoState model, ShowcaseDemoMessage message) =>
        message switch
        {
            ShowcaseQuitMessage => UpdateResult<ShowcaseDemoState, ShowcaseDemoMessage>.FromModel(model with { QuitRequested = true }),
            ShowcaseResizeMessage resize => UpdateResult<ShowcaseDemoState, ShowcaseDemoMessage>.FromModel(
                model with
                {
                    Viewport = resize.Size,
                    Session = model.Session.Advance(TerminalEvent.Resize(resize.Size))
                }),
            ShowcaseInputMessage input => UpdateResult<ShowcaseDemoState, ShowcaseDemoMessage>.FromModel(ApplyInput(model, input.Event)),
            _ => UpdateResult<ShowcaseDemoState, ShowcaseDemoMessage>.FromModel(model)
        };

    public IRuntimeView BuildView(ShowcaseDemoState model) =>
        HostedParitySurface.Create(model.Session);

    private static ShowcaseDemoState ApplyInput(ShowcaseDemoState model, TerminalEvent terminalEvent)
    {
        if (terminalEvent is KeyTerminalEvent keyEvent)
        {
            if (IsQuit(keyEvent.Gesture))
            {
                return model with { QuitRequested = true };
            }

            terminalEvent = TranslateAliases(keyEvent.Gesture) ?? terminalEvent;
        }

        return model with
        {
            Session = model.Session.Advance(terminalEvent)
        };
    }

    private static bool IsQuit(KeyGesture gesture) =>
        gesture.Key == TerminalKey.Escape ||
        (gesture.IsCharacter &&
         gesture.Character is { } rune &&
         rune.ToString().Equals("q", StringComparison.OrdinalIgnoreCase));

    private static TerminalEvent? TranslateAliases(KeyGesture gesture)
    {
        if (!gesture.IsCharacter || gesture.Character is null)
        {
            return null;
        }

        var alias = gesture.Character.Value.ToString();
        return alias.ToLowerInvariant() switch
        {
            "h" => TerminalEvent.Key(new KeyGesture(TerminalKey.Left, TerminalModifiers.None)),
            "j" => TerminalEvent.Key(new KeyGesture(TerminalKey.Down, TerminalModifiers.None)),
            "k" => TerminalEvent.Key(new KeyGesture(TerminalKey.Up, TerminalModifiers.None)),
            "l" => TerminalEvent.Key(new KeyGesture(TerminalKey.Right, TerminalModifiers.None)),
            _ => null
        };
    }
}

internal sealed record ShowcaseDemoState(HostedParitySession Session, Size Viewport, bool QuitRequested = false);

internal abstract record ShowcaseDemoMessage;

internal sealed record ShowcaseInputMessage(TerminalEvent Event) : ShowcaseDemoMessage;

internal sealed record ShowcaseResizeMessage(Size Size) : ShowcaseDemoMessage;

internal sealed record ShowcaseQuitMessage() : ShowcaseDemoMessage;
