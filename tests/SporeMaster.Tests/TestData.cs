namespace SporeMaster.Tests;

/// <summary>Locates the repository's sample data so tests can run against real packages.</summary>
public static class TestData
{
    private static readonly Lazy<string?> RepoRootLazy = new(FindRepoRoot);

    public static string? RepoRoot => RepoRootLazy.Value;

    public static string? BetaDirectory => Combine("refs", "_PackageFilesBetaVer");

    public static string? FinalDirectory => Combine("refs", "_PackageFilesFinalVer");

    public static IEnumerable<string> BetaPackages => EnumeratePackages(BetaDirectory);

    public static IEnumerable<string> FinalPackages => EnumeratePackages(FinalDirectory);

    public static string? Combine(params string[] parts)
    {
        if (RepoRoot is null)
        {
            return null;
        }

        return Path.Combine(new[] { RepoRoot }.Concat(parts).ToArray());
    }

    public static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "sporemaster-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public static void DeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch
        {
            // best effort
        }
    }

    private static IEnumerable<string> EnumeratePackages(string? directory)
    {
        if (directory is null || !Directory.Exists(directory))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(directory, "*.package", SearchOption.TopDirectoryOnly)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
    }

    private static string? FindRepoRoot()
    {
        string? current = AppContext.BaseDirectory;
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current, "refs")) &&
                Directory.Exists(Path.Combine(current, "src")))
            {
                return current;
            }

            current = Path.GetDirectoryName(current.TrimEnd(Path.DirectorySeparatorChar));
        }

        return null;
    }
}
