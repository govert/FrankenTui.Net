using System.Text;
using FrankenTui.Render;

namespace FrankenTui.Extras;

/// <summary>System clipboard integration (stub — platform-dependent). Matches upstream clipboard.</summary>
public static class ClipboardHelper
{
    public static string? Read() => null;
    public static void Write(string text) { /* stub */ }
}

/// <summary>Console utility helpers. Matches upstream console.</summary>
public static class ConsoleUtil
{
    public static bool IsRedirected => Console.IsOutputRedirected;
    public static int WindowWidth => Math.Max(Console.WindowWidth, 1);
    public static int WindowHeight => Math.Max(Console.WindowHeight, 1);

    public static string ReadPassword(char mask = '*')
    {
        var sb = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace && sb.Length > 0) sb.Length--;
            else if (key.KeyChar >= 32) sb.Append(key.KeyChar);
        }
        return sb.ToString();
    }
}

/// <summary>Simple logging infrastructure. Matches upstream logging.</summary>
public static class FxLog
{
    public enum Level { Debug, Info, Warn, Error }

    public static event Action<Level, string>? OnLog;

    public static void Debug(string msg) => OnLog?.Invoke(Level.Debug, msg);
    public static void Info(string msg) => OnLog?.Invoke(Level.Info, msg);
    public static void Warn(string msg) => OnLog?.Invoke(Level.Warn, msg);
    public static void Error(string msg) => OnLog?.Invoke(Level.Error, msg);
}

/// <summary>Export utilities for buffers and JSON. Matches upstream export.</summary>
public static class ExportUtil
{
    public static string ToPlainText(FrankenTui.Render.Buffer buffer)
    {
        var lines = HeadlessBufferView.ScreenText(buffer);
        return string.Join(Environment.NewLine, lines);
    }

    public static byte[] ToUtf8Bytes(FrankenTui.Render.Buffer buffer) =>
        Encoding.UTF8.GetBytes(ToPlainText(buffer));
}

/// <summary>Live streaming support stub. Matches upstream live.</summary>
public sealed class LiveStream
{
    private readonly List<string> _lines = [];
    public int MaxLines { get; init; } = 1000;

    public void Append(string line)
    {
        _lines.Add(line);
        while (_lines.Count > MaxLines) _lines.RemoveAt(0);
    }

    public IReadOnlyList<string> Lines => _lines;
    public void Clear() => _lines.Clear();
}

/// <summary>PTY/stdio capture utilities. Matches upstream pty_capture + stdio_capture.</summary>
public static class PtyCapture
{
    public static bool IsAvailable => OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();
    public static bool IsWindowsPtyAvailable => OperatingSystem.IsWindows();

    /// <summary>Capture a command's output via pipe (cross-platform fallback).</summary>
    public static async Task<string> CaptureAsync(string command, string args, CancellationToken ct = default)
    {
        var psi = new System.Diagnostics.ProcessStartInfo(command, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = System.Diagnostics.Process.Start(psi);
        if (proc is null) return "";
        var output = await proc.StandardOutput.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct);
        return output;
    }
}
