using System.Text;
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
    private readonly Func<Size?> _sizeReader;

    private RenderBuffer _previous;
    private Presenter _presenter;
    private InlineTerminalWriter _inlineWriter;
    private TerminalSessionConfiguration _sessionConfiguration = new();
    private TerminalBackendFeatures _features;

    public ConsoleTerminalBackend(string name, TerminalCapabilities capabilities, TextWriter? writer = null)
    {
        Name = name;
        Capabilities = capabilities;
        _writer = writer ?? Console.Out;
        _sizeReader = ReadConsoleSize;
        Size = _sizeReader() ?? new Size(80, 25);
        _previous = new RenderBuffer(Size.Width, Size.Height);
        _presenter = new Presenter(capabilities);
        _inlineWriter = new InlineTerminalWriter(capabilities, Size);
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
        Size = _sizeReader() ?? Size;
        _inlineWriter.TerminalSize = Size;
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
        _inlineWriter.ResetState();
        return ValueTask.CompletedTask;
    }

    public async ValueTask SetFeaturesAsync(
        TerminalBackendFeatures features,
        CancellationToken cancellationToken = default)
    {
        var next = features.Sanitize(Capabilities);
        if (next == _features)
        {
            return;
        }

        using var lease = _writerGate.Acquire();
        var sequence = TerminalFeatureControl.Transition(_features, next);
        if (sequence.Length > 0)
        {
            await _writer.WriteAsync(sequence.AsMemory(), cancellationToken).ConfigureAwait(false);
            await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        _features = next;
    }

    public async ValueTask WriteControlAsync(string sequence, CancellationToken cancellationToken = default)
    {
        using var lease = _writerGate.Acquire();
        await _writer.WriteAsync(sequence.AsMemory(), cancellationToken).ConfigureAwait(false);
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask WriteLogAsync(
        string text,
        TerminalLogWriteOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (!_sessionConfiguration.InlineMode)
        {
            return;
        }

        using var lease = _writerGate.Acquire();
        _inlineWriter.TerminalSize = _sizeReader() ?? Size;
        var output = _inlineWriter.WriteLog(text, options);
        if (output.Length == 0)
        {
            return;
        }

        await _writer.WriteAsync(output.AsMemory(), cancellationToken).ConfigureAwait(false);
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<PresentResult> PresentAsync(
        RenderBuffer buffer,
        BufferDiff? diff = null,
        IReadOnlyDictionary<uint, string>? links = null,
        CancellationToken cancellationToken = default)
    {
        using var lease = _writerGate.Acquire();
        var terminalSize = _sizeReader() ?? Size;
        Size = terminalSize;

        PresentResult result;
        if (_sessionConfiguration.InlineMode)
        {
            _inlineWriter.TerminalSize = terminalSize;
            result = _inlineWriter.Present(buffer, diff, links);
        }
        else
        {
            if (_previous.Width != buffer.Width || _previous.Height != buffer.Height)
            {
                _previous = new RenderBuffer(buffer.Width, buffer.Height);
                _presenter = new Presenter(Capabilities);
            }

            diff ??= BufferDiff.Compute(_previous, buffer);
            result = _presenter.Present(buffer, diff, links);
            _previous = buffer.Clone();
        }

        await _writer.WriteAsync(result.Output.AsMemory(), cancellationToken).ConfigureAwait(false);
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    public ValueTask<TerminalEvent?> ReadEventAsync(CancellationToken cancellationToken = default)
    {
        CapturePlatformEvents();
        return ValueTask.FromResult(_events.Count == 0 ? null : _events.Dequeue());
    }

    public async ValueTask<bool> PollEventAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (_events.Count > 0)
        {
            return true;
        }

        var started = DateTime.UtcNow;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (CapturePlatformEvents())
            {
                return true;
            }

            if (timeout <= TimeSpan.Zero)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(timeout.TotalMilliseconds, 20)), cancellationToken).ConfigureAwait(false);
        }
        while (DateTime.UtcNow - started < timeout);

        return _events.Count > 0;
    }

    public ValueTask ResizeAsync(Size size, CancellationToken cancellationToken = default)
    {
        Size = size.IsEmpty ? new Size(1, 1) : size;
        _presenter = new Presenter(Capabilities);
        _previous = new RenderBuffer(Size.Width, Size.Height);
        _inlineWriter.TerminalSize = Size;
        _inlineWriter.ResetState();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        State = TerminalLifecycleState.Disposed;
        return ValueTask.CompletedTask;
    }

    private bool CapturePlatformEvents()
    {
        var captured = false;

        if (_sizeReader() is { } size && size != Size)
        {
            Size = size;
            _inlineWriter.TerminalSize = size;
            _events.Enqueue(TerminalEvent.Resize(size));
            captured = true;
        }

        if (TryReadConsoleKey(out var terminalEvent))
        {
            _events.Enqueue(terminalEvent);
            captured = true;
        }

        return captured;
    }

    internal static Size? ReadConsoleSize()
    {
        return ReadConsoleSize(
            static () => Console.WindowWidth,
            static () => Console.WindowHeight,
            static () => Console.BufferWidth,
            static () => Console.BufferHeight);
    }

    internal static Size? ReadConsoleSize(
        Func<int> windowWidthReader,
        Func<int> windowHeightReader,
        Func<int> bufferWidthReader,
        Func<int> bufferHeightReader)
    {
        var width = ReadConsoleDimension(windowWidthReader) ?? ReadConsoleDimension(bufferWidthReader);
        var height = ReadConsoleDimension(windowHeightReader) ?? ReadConsoleDimension(bufferHeightReader);
        if (width is null || height is null)
        {
            return null;
        }

        return new Size(
            (ushort)Math.Clamp(width.Value, 1, ushort.MaxValue),
            (ushort)Math.Clamp(height.Value, 1, ushort.MaxValue));
    }

    private static int? ReadConsoleDimension(Func<int> reader)
    {
        try
        {
            var value = reader();
            return value > 0 ? value : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryReadConsoleKey(out TerminalEvent terminalEvent)
    {
        try
        {
            if (!Console.KeyAvailable)
            {
                terminalEvent = null!;
                return false;
            }

            terminalEvent = MapConsoleKey(Console.ReadKey(intercept: true));
            return true;
        }
        catch
        {
            terminalEvent = null!;
            return false;
        }
    }

    private static TerminalEvent MapConsoleKey(ConsoleKeyInfo keyInfo)
    {
        var modifiers =
            (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift) ? TerminalModifiers.Shift : TerminalModifiers.None) |
            (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt) ? TerminalModifiers.Alt : TerminalModifiers.None) |
            (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) ? TerminalModifiers.Control : TerminalModifiers.None);

        return keyInfo.Key switch
        {
            ConsoleKey.Tab => TerminalEvent.Key(new KeyGesture(TerminalKey.Tab, modifiers)),
            ConsoleKey.Enter => TerminalEvent.Key(new KeyGesture(TerminalKey.Enter, modifiers)),
            ConsoleKey.Escape => TerminalEvent.Key(new KeyGesture(TerminalKey.Escape, modifiers)),
            ConsoleKey.UpArrow => TerminalEvent.Key(new KeyGesture(TerminalKey.Up, modifiers)),
            ConsoleKey.DownArrow => TerminalEvent.Key(new KeyGesture(TerminalKey.Down, modifiers)),
            ConsoleKey.LeftArrow => TerminalEvent.Key(new KeyGesture(TerminalKey.Left, modifiers)),
            ConsoleKey.RightArrow => TerminalEvent.Key(new KeyGesture(TerminalKey.Right, modifiers)),
            ConsoleKey.Home => TerminalEvent.Key(new KeyGesture(TerminalKey.Home, modifiers)),
            ConsoleKey.End => TerminalEvent.Key(new KeyGesture(TerminalKey.End, modifiers)),
            ConsoleKey.PageUp => TerminalEvent.Key(new KeyGesture(TerminalKey.PageUp, modifiers)),
            ConsoleKey.PageDown => TerminalEvent.Key(new KeyGesture(TerminalKey.PageDown, modifiers)),
            ConsoleKey.Insert => TerminalEvent.Key(new KeyGesture(TerminalKey.Insert, modifiers)),
            ConsoleKey.Delete => TerminalEvent.Key(new KeyGesture(TerminalKey.Delete, modifiers)),
            ConsoleKey.Backspace => TerminalEvent.Key(new KeyGesture(TerminalKey.Backspace, modifiers)),
            ConsoleKey.F1 => TerminalEvent.Key(new KeyGesture(TerminalKey.F1, modifiers)),
            ConsoleKey.F2 => TerminalEvent.Key(new KeyGesture(TerminalKey.F2, modifiers)),
            ConsoleKey.F3 => TerminalEvent.Key(new KeyGesture(TerminalKey.F3, modifiers)),
            ConsoleKey.F4 => TerminalEvent.Key(new KeyGesture(TerminalKey.F4, modifiers)),
            ConsoleKey.F5 => TerminalEvent.Key(new KeyGesture(TerminalKey.F5, modifiers)),
            ConsoleKey.F6 => TerminalEvent.Key(new KeyGesture(TerminalKey.F6, modifiers)),
            ConsoleKey.F7 => TerminalEvent.Key(new KeyGesture(TerminalKey.F7, modifiers)),
            ConsoleKey.F8 => TerminalEvent.Key(new KeyGesture(TerminalKey.F8, modifiers)),
            ConsoleKey.F9 => TerminalEvent.Key(new KeyGesture(TerminalKey.F9, modifiers)),
            ConsoleKey.F10 => TerminalEvent.Key(new KeyGesture(TerminalKey.F10, modifiers)),
            ConsoleKey.F11 => TerminalEvent.Key(new KeyGesture(TerminalKey.F11, modifiers)),
            ConsoleKey.F12 => TerminalEvent.Key(new KeyGesture(TerminalKey.F12, modifiers)),
            _ when keyInfo.KeyChar != '\0' => TerminalEvent.Key(
                new KeyGesture(TerminalKey.Character, modifiers, new Rune(keyInfo.KeyChar))),
            _ => TerminalEvent.Key(new KeyGesture(TerminalKey.Unknown, modifiers))
        };
    }
}
