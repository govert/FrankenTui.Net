namespace FrankenTui.Testing.Harness;

public static class ArtifactPathBuilder
{
    public static string For(string category, string fileName)
    {
        var root = RepositoryPaths.FindRepositoryRoot();
        var directory = Path.Combine(root, "artifacts", category);
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, fileName);
    }
}
