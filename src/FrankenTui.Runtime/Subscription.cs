namespace FrankenTui.Runtime;

public sealed record Subscription<TMessage>(string Key, Func<IEnumerable<TMessage>> CreateMessages)
{
    public IEnumerable<TMessage> Invoke() => CreateMessages();
}
