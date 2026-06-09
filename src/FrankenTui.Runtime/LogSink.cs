// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/log_sink.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
//
// Log sink for in-process output routing.
//
// The LogSink struct implements ILogWriter and forwards output to
// TerminalWriter.WriteLog, ensuring that:
// 1. Output is line-buffered (to prevent torn lines).
// 2. Content is sanitized (escape sequences stripped).
// 3. The One-Writer Rule is respected.
//
// DIVERGENCE: Uses FrankenTui.Render.OutputSanitizer.Sanitize instead of
// ftui_render::sanitize::sanitize.

using FrankenTui.Render;

namespace FrankenTui.Runtime;

/// <summary>
/// A write adapter that routes output to the terminal's log scrollback.
///
/// Wraps a TerminalWriter and line-buffers output. Flushes when newline
/// encountered, on explicit flush, or on disposal.
/// </summary>
public sealed class LogSink : IDisposable
{
    private readonly TerminalWriter _writer;
    private readonly List<byte> _buffer = new(1024);

    /// <summary>Create a new log sink wrapping the given terminal writer.</summary>
    public LogSink(TerminalWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    /// <summary>
    /// Write bytes to the log sink. Line-buffered: content is flushed when
    /// a newline is encountered.
    /// Returns the number of bytes written.
    /// </summary>
    public int Write(byte[] buf, int offset, int count)
    {
        for (int i = offset; i < offset + count; i++)
        {
            byte b = buf[i];
            if (b == (byte)'\n')
            {
                var line = System.Text.Encoding.UTF8.GetString(_buffer.ToArray());
                var safe = OutputSanitizer.Sanitize(line);
                _writer.WriteLog($"{safe}\n");
                _buffer.Clear();
            }
            else
            {
                _buffer.Add(b);
            }
        }
        return count;
    }

    /// <summary>Write a span of bytes. Line-buffered.</summary>
    public int Write(ReadOnlySpan<byte> buf)
    {
        int count = buf.Length;
        foreach (byte b in buf)
        {
            if (b == (byte)'\n')
            {
                var line = System.Text.Encoding.UTF8.GetString(_buffer.ToArray());
                var safe = OutputSanitizer.Sanitize(line);
                _writer.WriteLog($"{safe}\n");
                _buffer.Clear();
            }
            else
            {
                _buffer.Add(b);
            }
        }
        return count;
    }

    /// <summary>Flush any buffered partial line.</summary>
    public void Flush()
    {
        if (_buffer.Count > 0)
        {
            var line = System.Text.Encoding.UTF8.GetString(_buffer.ToArray());
            var safe = OutputSanitizer.Sanitize(line);
            _writer.WriteLog(safe);
            _buffer.Clear();
        }
        _writer.Flush();
    }

    /// <summary>Dispose: best-effort flush remaining buffer.</summary>
    public void Dispose()
    {
        try { Flush(); } catch { /* best-effort */ }
    }
}

/// <summary>Convenience extension for writing formatted text to a LogSink.</summary>
public static class LogSinkExtensions
{
    public static void Write(this LogSink sink, string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        sink.Write(bytes, 0, bytes.Length);
    }
}
