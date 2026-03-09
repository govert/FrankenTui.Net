using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Render;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tty;

public class ConsoleTerminalBackend : ITerminalBackend
{
    private readonly TextWriter _writer;
    private readonly Queue<TerminalEvent> _events = [];
    private readonly SingleWriterGate _writerGate = new();

    private RenderBuffer _previous;
    private Presenter _presenter;

    public ConsoleTerminalBackend(string name, TerminalCapabilities capabilities, TextWriter? writer = null)
    {
        Name = name;
        Capabilities = capabilities;
        _writer = writer ?? Console.Out;
        Size = new Size((ushort)Math.Clamp(Console.BufferWidth, 1, ushort.MaxValue), (ushort)Math.Clamp(Console.BufferHeight, 1, ushort.MaxValue));
        _previous = new RenderBuffer(Size.Width, Size.Height);
        _presenter = new Presenter(capabilities);
    }

    public string Name { get; }

    public Size Size { get; private set; }

    public TerminalCapabilities Capabilities { get; }

    public TerminalLifecycleState State { get; private set; } = TerminalLifecycleState.Created;

    public void Enqueue(params TerminalEvent[] events)
    {
        foreach (var terminalEvent in events)
        {
            _events.Enqueue(terminalEvent);
        }
    }

    public ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        State = TerminalLifecycleState.Initialized;
        return ValueTask.CompletedTask;
    }

    public async ValueTask WriteControlAsync(string sequence, CancellationToken cancellationToken = default)
    {
        using var lease = _writerGate.Acquire();
        await _writer.WriteAsync(sequence.AsMemory(), cancellationToken).ConfigureAwait(false);
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<PresentResult> PresentAsync(
        RenderBuffer buffer,
        BufferDiff? diff = null,
        IReadOnlyDictionary<uint, string>? links = null,
        CancellationToken cancellationToken = default)
    {
        using var lease = _writerGate.Acquire();
        if (_previous.Width != buffer.Width || _previous.Height != buffer.Height)
        {
            Size = new Size(buffer.Width, buffer.Height);
            _previous = new RenderBuffer(buffer.Width, buffer.Height);
            _presenter = new Presenter(Capabilities);
        }

        diff ??= BufferDiff.Compute(_previous, buffer);
        var result = _presenter.Present(buffer, diff, links);
        await _writer.WriteAsync(result.Output.AsMemory(), cancellationToken).ConfigureAwait(false);
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        _previous = buffer.Clone();
        return result;
    }

    public ValueTask<TerminalEvent?> ReadEventAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_events.Count == 0 ? null : _events.Dequeue());

    public ValueTask ResizeAsync(Size size, CancellationToken cancellationToken = default)
    {
        Size = size.IsEmpty ? new Size(1, 1) : size;
        _previous = new RenderBuffer(Size.Width, Size.Height);
        _presenter = new Presenter(Capabilities);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        State = TerminalLifecycleState.Disposed;
        return ValueTask.CompletedTask;
    }
}
