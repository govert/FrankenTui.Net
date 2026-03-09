namespace FrankenTui.Testing.Harness;

public static class RepositoryPaths
{
    public static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FrankenTui.Net.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate FrankenTui.Net.sln from the current execution root.");
    }
}
