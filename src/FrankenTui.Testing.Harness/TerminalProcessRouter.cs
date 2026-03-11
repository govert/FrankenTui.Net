using FrankenTui.Backend;

namespace FrankenTui.Testing.Harness;

public static class TerminalProcessRouter
{
    public static Task<ProcessRunResult> RunAsync(
        ITerminalBackend backend,
        string fileName,
        IEnumerable<string> arguments,
        TerminalLogWriteOptions? options = null,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string?>? environmentVariables = null,
        string? stdin = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backend);

        return ProcessCommandRunner.RunStreamingAsync(
            fileName,
            arguments,
            async (chunk, token) =>
            {
                if (chunk.Text.Length == 0)
                {
                    return;
                }

                await backend.WriteLogAsync(chunk.Text, options, token).ConfigureAwait(false);
            },
            workingDirectory,
            environmentVariables,
            stdin,
            cancellationToken);
    }
}
