namespace FrankenTui.Testing.Harness;

public static class SnapshotAssert
{
    public static void Equal(string expected, RenderSnapshot snapshot)
    {
        if (!string.Equals(expected, snapshot.Text, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Snapshot mismatch.{Environment.NewLine}Expected:{Environment.NewLine}{expected}{Environment.NewLine}Actual:{Environment.NewLine}{snapshot.Text}");
        }
    }
}
