using System.Diagnostics;
using System.Text;

namespace FrankenTui.Testing.Pty;

public static class ScriptPtyRunner
{
    public static async Task<PtyRunResult> RunCommandAsync(
        string fileName,
        IEnumerable<string> arguments,
        string? stdin = null,
        CancellationToken cancellationToken = default)
    {
        if (!(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()))
        {
            throw new PlatformNotSupportedException("PTY-backed execution currently relies on the Unix 'script' command.");
        }

        var command = BuildShellCommand(fileName, arguments);
        var startInfo = new ProcessStartInfo("/usr/bin/script")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-qefc");
        startInfo.ArgumentList.Add(command);
        startInfo.ArgumentList.Add("/dev/null");

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        if (!string.IsNullOrEmpty(stdin))
        {
            await process.StandardInput.WriteAsync(stdin.AsMemory(), cancellationToken).ConfigureAwait(false);
        }

        process.StandardInput.Close();
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return new PtyRunResult(process.ExitCode, await stdoutTask.ConfigureAwait(false), await stderrTask.ConfigureAwait(false));
    }

    private static string BuildShellCommand(string fileName, IEnumerable<string> arguments)
    {
        var builder = new StringBuilder(ShellEscape(fileName));
        foreach (var argument in arguments)
        {
            builder.Append(' ').Append(ShellEscape(argument));
        }

        return builder.ToString();
    }

    private static string ShellEscape(string text) =>
        $"'{text.Replace("'", "'\"'\"'", StringComparison.Ordinal)}'";
}
