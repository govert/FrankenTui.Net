namespace FrankenTui.Testing.Pty;

public sealed record PtyRunResult(int ExitCode, string Stdout, string Stderr)
{
    public bool Success => ExitCode == 0;
}
