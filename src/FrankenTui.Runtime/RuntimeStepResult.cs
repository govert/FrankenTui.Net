using FrankenTui.Render;

namespace FrankenTui.Runtime;

public sealed record RuntimeStepResult<TModel, TMessage>(
    TModel Model,
    PresentResult Presentation,
    string ScreenText,
    IReadOnlyList<TMessage> EmittedMessages,
    RuntimeTraceEntry<TMessage>? TraceEntry = null);
