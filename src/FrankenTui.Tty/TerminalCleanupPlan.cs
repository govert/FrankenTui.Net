namespace FrankenTui.Tty;

public sealed record TerminalCleanupPlan(IReadOnlyList<string> EnterSequences, IReadOnlyList<string> ExitSequences)
{
    public static readonly TerminalCleanupPlan Empty = new([], []);
}
