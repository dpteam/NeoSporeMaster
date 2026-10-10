namespace SporeMaster.Core.Names;

/// <summary>
/// A Spore <c>*_DDFList.txt</c> file: the list of <c>Data</c> directories that go into a particular
/// build (Spore / CSA / Viewer). Files are simple: one directory per line, <c>//</c> comments and
/// <c>#include Other_DDFList.txt</c> directives (resolved next to the including file, then against
/// its ancestor folders).
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
                    Load(ResolveInclude(directory, target), visited);
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

    /// <summary>
    /// Resolves an <c>#include</c> target. Some data trees keep the lists in a <c>DDFLists</c>
    /// sub-folder yet write targets relative to the <c>Data</c> root (for example
    /// <c>DDFLists/Core_DDFList.txt</c>), so the target is looked up next to the including file
    /// first and then against its ancestors.
    /// </summary>
    private static string ResolveInclude(string directory, string target)
    {
        if (Path.IsPathRooted(target))
        {
            return target;
        }

        string current = directory;
        for (int depth = 0; depth < 4; depth++)
        {
            string candidate = Path.Combine(current, target);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            DirectoryInfo? parent = Directory.GetParent(current);
            if (parent is null)
            {
                break;
            }

            current = parent.FullName;
        }

        // Nothing matched; return the direct path so it is reported as a missing include.
        return Path.Combine(directory, target);
    }

    private static string Normalize(string path) =>
        path.Replace('\\', '/').Trim('/');
}
