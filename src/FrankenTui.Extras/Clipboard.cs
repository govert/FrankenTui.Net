// Upstream source: crates/ftui-extras/src/clipboard.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Port of ClipboardSystem for reading/writing system clipboard.
// DIVERGENCE: Uses platform-specific shell commands (clip.exe, xclip, pbcopy/pbpaste)
// instead of the arboard Rust crate. Falls back to in-memory buffer when no
// clipboard tool is available. Cross-platform by design — no Windows-only dependency.

using System.Diagnostics;

namespace FrankenTui.Extras;

/// <summary>System clipboard access. Cross-platform with tool-based fallback.</summary>
public static class ClipboardSystem
{
    private static string? _memoryBuffer;

    /// <summary>Read text from the system clipboard. Returns null if unavailable.</summary>
    public static string? Read()
    {
        // Try platform clipboard tools first
        var fromTool = ReadFromPlatform();
        if (fromTool is not null) return fromTool;
        return _memoryBuffer;
    }

    /// <summary>Write text to the system clipboard.</summary>
    public static void Write(string text)
    {
        _memoryBuffer = text;
        WriteToPlatform(text);
    }

    /// <summary>Clear the clipboard.</summary>
    public static void Clear()
    {
        _memoryBuffer = null;
        WriteToPlatform("");
    }

    private static string? ReadFromPlatform()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                // PowerShell Get-Clipboard is available on Windows 10+
                var psi = new ProcessStartInfo("powershell", "-NoProfile -Command \"Get-Clipboard\"")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var proc = Process.Start(psi);
                if (proc is null) return null;
                var output = proc.StandardOutput.ReadToEnd().TrimEnd('\r', '\n');
                return string.IsNullOrEmpty(output) ? null : output;
            }
            if (OperatingSystem.IsMacOS())
            {
                var psi = new ProcessStartInfo("pbpaste")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var proc = Process.Start(psi);
                if (proc is null) return null;
                return proc.StandardOutput.ReadToEnd().TrimEnd('\n');
            }
            if (OperatingSystem.IsLinux())
            {
                var psi = new ProcessStartInfo("xclip", "-o -selection clipboard")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var proc = Process.Start(psi);
                if (proc is null) return null;
                return proc.StandardOutput.ReadToEnd().TrimEnd('\n');
            }
        }
        catch { }
        return null;
    }

    private static void WriteToPlatform(string text)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var psi = new ProcessStartInfo("powershell", $"-NoProfile -Command \"Set-Clipboard -Value '{text.Replace("'", "''")}'\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(2000);
                return;
            }
            if (OperatingSystem.IsMacOS())
            {
                var psi = new ProcessStartInfo("pbcopy")
                {
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var proc = Process.Start(psi);
                if (proc is not null) { proc.StandardInput.Write(text); proc.StandardInput.Close(); proc.WaitForExit(2000); }
                return;
            }
            if (OperatingSystem.IsLinux())
            {
                var psi = new ProcessStartInfo("xclip", "-selection clipboard")
                {
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var proc = Process.Start(psi);
                if (proc is not null) { proc.StandardInput.Write(text); proc.StandardInput.Close(); proc.WaitForExit(2000); }
                return;
            }
        }
        catch { }
    }
}
