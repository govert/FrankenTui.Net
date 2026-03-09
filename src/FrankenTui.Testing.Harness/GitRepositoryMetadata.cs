using System.Diagnostics;

namespace FrankenTui.Testing.Harness;

internal sealed record GitRepositoryMetadata(string? RepoUrl, string? Commit)
{
    public static GitRepositoryMetadata? TryRead(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);

        var repoUrl = RunGit(root, "config --get remote.origin.url");
        var commit = RunGit(root, "rev-parse HEAD");
        if (string.IsNullOrWhiteSpace(repoUrl) && string.IsNullOrWhiteSpace(commit))
        {
            return null;
        }

        return new GitRepositoryMetadata(repoUrl, commit);
    }

    private static string? RunGit(string root, string arguments)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo("git", arguments)
                {
                    WorkingDirectory = root,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return process.ExitCode == 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }
}
