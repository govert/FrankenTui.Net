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
            ShowcaseOutcomeMessage outcome => UpdateResult<ShowcaseDemoState, ShowcaseDemoMessage>.FromModel(
                model with
                {
                    Viewport = outcome.Outcome.ResizeToApply ?? model.Viewport,
                    Session = outcome.Outcome.Session,
                    QuitRequested = model.QuitRequested || outcome.Outcome.QuitRequested
                }),
            _ => UpdateResult<ShowcaseDemoState, ShowcaseDemoMessage>.FromModel(model)
        };

    public IRuntimeView BuildView(ShowcaseDemoState model) =>
        HostedParitySurface.Create(model.Session);

}

internal sealed record ShowcaseDemoState(HostedParitySession Session, Size Viewport, bool QuitRequested = false);

internal abstract record ShowcaseDemoMessage;

internal sealed record ShowcaseOutcomeMessage(HostedParityInputOutcome Outcome) : ShowcaseDemoMessage;
