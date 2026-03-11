using System.Text;
using FrankenTui.Core;
using FrankenTui.Render;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Backend;

public sealed class MemoryTerminalBackend : ITerminalBackend
{
    private readonly Queue<TerminalEvent> _events = [];
    private readonly StringBuilder _output = new();
    private readonly SingleWriterGate _writerGate = new();

    private RenderBuffer _previous;
    private Presenter _presenter;
    private TerminalModel _model;
    private InlineTerminalWriter _inlineWriter;
    private TerminalSessionConfiguration _sessionConfiguration = new();
    private TerminalBackendFeatures _features;

    public MemoryTerminalBackend(Size size, TerminalCapabilities? capabilities = null, string name = "memory")
    {
        Size = size.IsEmpty ? new Size(1, 1) : size;
        Capabilities = capabilities ?? TerminalCapabilities.Modern();
        Name = name;
        _previous = new RenderBuffer(Size.Width, Size.Height);
        _presenter = new Presenter(Capabilities);
        _model = new TerminalModel(Size.Width, Size.Height);
        _inlineWriter = new InlineTerminalWriter(Capabilities, Size);
    }

    public string Name { get; }

    public Size Size { get; private set; }

    public TerminalCapabilities Capabilities { get; }

    public TerminalLifecycleState State { get; private set; } = TerminalLifecycleState.Created;

    public TerminalModel Model => _model;

    public IReadOnlyList<TerminalEvent> PendingEvents => _events.ToArray();

    public void Enqueue(params TerminalEvent[] events)
    {
        foreach (var terminalEvent in events)
        {
            _events.Enqueue(terminalEvent);
        }
    }

    public string DrainOutput()
    {
        var text = _output.ToString();
        _output.Clear();
        return text;
    }

    public ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        State = TerminalLifecycleState.Initialized;
        return ValueTask.CompletedTask;
    }

    public ValueTask ConfigureSessionAsync(
        TerminalSessionConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        _sessionConfiguration = configuration ?? new TerminalSessionConfiguration();
        _previous = new RenderBuffer(Size.Width, Size.Height);
        _presenter = new Presenter(Capabilities);
        _inlineWriter.TerminalSize = Size;
        _inlineWriter.ResetState();
        _model = new TerminalModel(Size.Width, Size.Height);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetFeaturesAsync(
        TerminalBackendFeatures features,
        CancellationToken cancellationToken = default)
    {
        var next = features.Sanitize(Capabilities);
        if (next == _features)
        {
            return ValueTask.CompletedTask;
        }

        using var lease = _writerGate.Acquire();
        var sequence = TerminalFeatureControl.Transition(_features, next);
        if (sequence.Length > 0)
        {
            _output.Append(sequence);
            _model.Process(sequence);
        }

        _features = next;
        return ValueTask.CompletedTask;
    }

    public ValueTask WriteControlAsync(string sequence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sequence);

        using var lease = _writerGate.Acquire();
        _output.Append(sequence);
        _model.Process(sequence);
        return ValueTask.CompletedTask;
    }

    public ValueTask WriteLogAsync(
        string text,
        TerminalLogWriteOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (!_sessionConfiguration.InlineMode)
        {
            return ValueTask.CompletedTask;
        }

        using var lease = _writerGate.Acquire();
        _inlineWriter.TerminalSize = Size;
        var output = _inlineWriter.WriteLog(text, options);
        if (output.Length == 0)
        {
            return ValueTask.CompletedTask;
        }

        _output.Append(output);
        _model.Process(output);
        return ValueTask.CompletedTask;
    }

    public ValueTask<PresentResult> PresentAsync(
        RenderBuffer buffer,
        BufferDiff? diff = null,
        IReadOnlyDictionary<uint, string>? links = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        using var lease = _writerGate.Acquire();
        PresentResult result;
        if (_sessionConfiguration.InlineMode)
        {
            _inlineWriter.TerminalSize = Size;
            result = _inlineWriter.Present(buffer, diff, links);
        }
        else
        {
            if (_previous.Width != buffer.Width || _previous.Height != buffer.Height)
            {
                _previous = new RenderBuffer(buffer.Width, buffer.Height);
                _presenter = new Presenter(Capabilities);
                _model = new TerminalModel(buffer.Width, buffer.Height);
            }

            diff ??= BufferDiff.Compute(_previous, buffer);
            result = _presenter.Present(buffer, diff, links);
            _previous = buffer.Clone();
        }

        _output.Append(result.Output);
        _model.Process(result.Output);
        return ValueTask.FromResult(result);
    }

    public ValueTask<TerminalEvent?> ReadEventAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_events.Count == 0 ? null : _events.Dequeue());

    public ValueTask<bool> PollEventAsync(TimeSpan timeout, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_events.Count > 0);

    public ValueTask ResizeAsync(Size size, CancellationToken cancellationToken = default)
    {
        Size = size.IsEmpty ? new Size(1, 1) : size;
        _previous = new RenderBuffer(Size.Width, Size.Height);
        _presenter = new Presenter(Capabilities);
        _model = new TerminalModel(Size.Width, Size.Height);
        _inlineWriter.TerminalSize = Size;
        _inlineWriter.ResetState();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        State = TerminalLifecycleState.Disposed;
        return ValueTask.CompletedTask;
    }
}
