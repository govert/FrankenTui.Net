namespace FrankenTui.Runtime;

public sealed class RuntimeTrace<TMessage>
{
    private readonly List<string> _frames = [];
    private readonly List<TMessage> _messages = [];

    public IReadOnlyList<string> Frames => _frames;

    public IReadOnlyList<TMessage> Messages => _messages;

    public void Record(string frame, IEnumerable<TMessage> messages)
    {
        _frames.Add(frame);
        _messages.AddRange(messages);
    }
}
