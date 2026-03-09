namespace FrankenTui.Runtime;

public sealed record UpdateResult<TModel, TMessage>(
    TModel Model,
    AppCommand<TMessage> Commands,
    IReadOnlyList<Subscription<TMessage>> Subscriptions)
{
    public static UpdateResult<TModel, TMessage> FromModel(TModel model) =>
        new(model, AppCommand<TMessage>.None, []);
}
