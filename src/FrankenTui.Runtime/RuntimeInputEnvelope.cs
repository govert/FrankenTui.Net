using FrankenTui.Core;

namespace FrankenTui.Runtime;

public sealed record RuntimeInputTranslation(
    TerminalEvent SourceEvent,
    TerminalEvent EffectiveEvent,
    bool QuitRequested = false,
    string? Label = null);

public sealed record RuntimeInputEnvelope(
    TerminalEvent? SourceEvent,
    TerminalEvent? EffectiveEvent,
    IReadOnlyList<SemanticEvent> SemanticEvents,
    IReadOnlyList<KeybindingDecision> PolicyDecisions,
    ResizeDecision? ResizeDecision,
    Size? ResizeToApply,
    bool QuitRequested,
    bool HasWork,
    string Label,
    DateTimeOffset Timestamp,
    bool IsTick = false)
{
    public static RuntimeInputEnvelope Idle(DateTimeOffset now) =>
        new(
            null,
            null,
            [],
            [],
            null,
            null,
            QuitRequested: false,
            HasWork: false,
            "idle",
            now,
            IsTick: true);
}
