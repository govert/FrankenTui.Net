using System.Diagnostics;
using System.Text;

namespace FrankenTui.Testing.Harness;

public static class ProcessCommandRunner
{
    public static async Task<ProcessRunResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string?>? environmentVariables = null,
        string? stdin = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(arguments);

        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environmentVariables is not null)
        {
            foreach (var entry in environmentVariables)
            {
                if (entry.Value is null)
                {
                    startInfo.Environment.Remove(entry.Key);
                }
                else
                {
                    startInfo.Environment[entry.Key] = entry.Value;
                }
            }
        }

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        if (!string.IsNullOrEmpty(stdin))
        {
            await process.StandardInput.WriteAsync(stdin.AsMemory(), cancellationToken).ConfigureAwait(false);
        }

        process.StandardInput.Close();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return new ProcessRunResult(
            process.ExitCode,
            await stdoutTask.ConfigureAwait(false),
            await stderrTask.ConfigureAwait(false));
    }

    public static async Task<ProcessRunResult> RunStreamingAsync(
        string fileName,
        IEnumerable<string> arguments,
        Func<ProcessOutputChunk, CancellationToken, ValueTask>? onOutput = null,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string?>? environmentVariables = null,
        string? stdin = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(arguments);

        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environmentVariables is not null)
        {
            foreach (var entry in environmentVariables)
            {
                if (entry.Value is null)
                {
                    startInfo.Environment.Remove(entry.Key);
                }
                else
                {
                    startInfo.Environment[entry.Key] = entry.Value;
                }
            }
        }

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        if (!string.IsNullOrEmpty(stdin))
        {
            await process.StandardInput.WriteAsync(stdin.AsMemory(), cancellationToken).ConfigureAwait(false);
        }

        process.StandardInput.Close();

        var callbackLock = new SemaphoreSlim(1, 1);
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        var stdoutTask = PumpAsync(
            process.StandardOutput,
            ProcessOutputStream.Stdout,
            stdout,
            onOutput,
            callbackLock,
            cancellationToken);
        var stderrTask = PumpAsync(
            process.StandardError,
            ProcessOutputStream.Stderr,
            stderr,
            onOutput,
            callbackLock,
            cancellationToken);

        await Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync(cancellationToken)).ConfigureAwait(false);

        return new ProcessRunResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private static async Task PumpAsync(
        StreamReader reader,
        ProcessOutputStream stream,
        StringBuilder builder,
        Func<ProcessOutputChunk, CancellationToken, ValueTask>? onOutput,
        SemaphoreSlim callbackLock,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            var chunk = new ProcessOutputChunk(stream, line + Environment.NewLine);
            builder.Append(chunk.Text);
            if (onOutput is null)
            {
                continue;
            }

            await callbackLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await onOutput(chunk, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                callbackLock.Release();
            }
        }
    }
}

public sealed record ProcessRunResult(int ExitCode, string Stdout, string Stderr)
{
    public bool Success => ExitCode == 0;
}

public enum ProcessOutputStream
{
    Stdout,
    Stderr
}

public readonly record struct ProcessOutputChunk(ProcessOutputStream Stream, string Text);
