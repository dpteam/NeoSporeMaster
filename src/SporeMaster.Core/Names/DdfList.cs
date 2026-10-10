namespace SporeMaster.Core.Names;

/// <summary>
/// A Spore <c>*_DDFList.txt</c> file: the list of <c>Data</c> directories that go into a particular
/// build (Spore / CSA / Viewer). Files are simple: one directory per line, <c>//</c> comments and
/// <c>#include Other_DDFList.txt</c> directives (resolved relative to the including file).
/// </summary>
/// <remarks>
/// See <c>refs/SporeUnpackedData/SporeHeroWiiPCLeftOvers/Data/README.txt</c>. The master lists
/// (<c>Spore_DDFList</c>, <c>CSA_DDFList</c>, <c>Viewer_DDFList</c>) include the shared sections.
/// </remarks>
public sealed class DdfList
{
    private readonly HashSet<string> _folders = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Normalised directory paths (forward slashes, no leading/trailing slash).</summary>
    public IReadOnlyCollection<string> Folders => _folders;

    /// <summary>Number of <c>#include</c> directives followed.</summary>
    public int IncludeCount { get; private set; }

    /// <summary>Files that could not be read (for diagnostics).</summary>
    public List<string> MissingIncludes { get; } = new();

    public static DdfList ParseFile(string path)
    {
        var list = new DdfList();
        list.Load(path, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        return list;
    }

    private void Load(string path, HashSet<string> visited)
    {
        string full = Path.GetFullPath(path);
        if (!visited.Add(full) || !File.Exists(full))
        {
            if (!File.Exists(full))
            {
                MissingIncludes.Add(full);
            }

            return;
        }

        string directory = Path.GetDirectoryName(full) ?? ".";
        foreach (string raw in File.ReadLines(full))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//"))
            {
                continue;
            }

            if (line.StartsWith("#include", StringComparison.OrdinalIgnoreCase))
            {
                string target = line[8..].Trim().Trim('"');
                if (target.Length > 0)
                {
                    IncludeCount++;
                    Load(Path.Combine(directory, target), visited);
                }

                continue;
            }

            if (line.StartsWith('#'))
            {
                continue;
            }

            _folders.Add(Normalize(line));
        }
    }

    /// <summary>True when a resource path is inside one of the listed directories.</summary>
    public bool Contains(string path)
    {
        string normalized = Normalize(path);
        foreach (string folder in _folders)
        {
            if (normalized.Equals(folder, StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string path) =>
        path.Replace('\\', '/').Trim('/');
}
