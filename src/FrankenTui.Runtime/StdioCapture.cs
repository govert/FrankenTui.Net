// Upstream source: crates/ftui-runtime/src/stdio_capture.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Direct 1-1 port of StdioCaptureError, StdioCapture, CapturedWriter, try_capture.
// DIVERGENCE: Uses System.Threading.Channels.Channel instead of mpsc::channel.
// The CapturedWriter implements TextWriter (not Write trait) for .NET idioms.

using System.Text;
using System.Threading.Channels;

namespace FrankenTui.Runtime;

/// <summary>Error type for stdio capture operations.</summary>
public abstract record StdioCaptureError
{
    private StdioCaptureError() { }
    /// <summary>A capture is already installed. Only one can be active at a time.</summary>
    public sealed record AlreadyInstalled : StdioCaptureError;
    /// <summary>The internal lock was poisoned.</summary>
    public sealed record PoisonedLock : StdioCaptureError;
}

/// <summary>Guard that owns the receiving end of the capture channel.</summary>
public sealed class StdioCapture : IDisposable
{
    private static readonly object _lock = new();
    private static ChannelWriter<byte[]>? _globalWriter;

    private readonly ChannelReader<byte[]> _reader;
    private bool _disposed;

    private StdioCapture(ChannelReader<byte[]> reader)
    {
        _reader = reader;
    }

    /// <summary>Install the global stdio capture. Only one can be active at a time.</summary>
    public static StdioCapture Install()
    {
        lock (_lock)
        {
            if (_globalWriter is not null)
                throw new InvalidOperationException("StdioCapture is already installed");

            var channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(1024)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
            _globalWriter = channel.Writer;
            return new StdioCapture(channel.Reader);
        }
    }

    /// <summary>Check whether a capture is currently installed.</summary>
    public static bool IsInstalled
    {
        get { lock (_lock) return _globalWriter is not null; }
    }

    /// <summary>Try to send bytes through the capture channel. Returns true if captured.</summary>
    public static bool TryCapture(byte[] bytes)
    {
        var writer = _globalWriter;
        if (writer is null) return false;
        return writer.TryWrite(bytes);
    }

    /// <summary>Drain all pending captured output into the given stream. Returns total bytes written.</summary>
    public int Drain(Stream sink)
    {
        int total = 0;
        while (_reader.TryRead(out var bytes))
        {
            sink.Write(bytes, 0, bytes.Length);
            total += bytes.Length;
        }
        return total;
    }

    /// <summary>Drain pending output as a string. Invalid UTF-8 is replaced.</summary>
    public string DrainToString()
    {
        using var ms = new MemoryStream();
        Drain(ms);
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        lock (_lock)
        {
            _globalWriter = null;
        }
        // Drain remaining
        while (_reader.TryRead(out _)) { }
    }
}

/// <summary>A TextWriter that sends bytes through the capture channel.</summary>
public sealed class CapturedWriter : TextWriter
{
    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char[] buffer, int index, int count)
    {
        var bytes = Encoding.UTF8.GetBytes(buffer, index, count);
        if (!StdioCapture.TryCapture(bytes))
        {
            // No capture installed — silently discard (black hole pattern)
        }
    }

    public override void Write(char value)
    {
        Write([value], 0, 1);
    }

    public override void Write(string? value)
    {
        if (value is not null)
            Write(value.ToCharArray(), 0, value.Length);
    }

    public override void WriteLine(string? value)
    {
        Write(value);
        Write(NewLine);
    }
}
