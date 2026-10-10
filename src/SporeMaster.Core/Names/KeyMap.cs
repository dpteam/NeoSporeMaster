using System.Globalization;

namespace SporeMaster.Core.Names;

/// <summary>One entry of a Spore key map: a real resource key and its real path.</summary>
public readonly record struct KeyMapEntry(uint TypeId, uint GroupId, uint InstanceId, string Path);

/// <summary>
/// Parses the game's own key maps — <c>KeyMap.txt</c>
/// (<c>{0xTYPE 0xGROUP 0xINSTANCE} "Folder\File.ext"</c>) and <c>PreBakedKeys.txt</c> — into the
/// name registry. These are authoritative (real file names), so they beat fragment-based guessing.
/// </summary>
public sealed class KeyMap
{
    private readonly Dictionary<uint, string> _typeNames = new();
    private readonly Dictionary<uint, string> _groupNames = new();
    private readonly Dictionary<uint, string> _instanceNames = new();

    public List<KeyMapEntry> Entries { get; } = new();

    /// <summary>Number of instance paths whose name really hashes to their id.</summary>
    public int VerifiedInstanceNames { get; private set; }

    /// <summary>Number of entries whose key did not match the path (should be 0).</summary>
    public int UnverifiedEntries { get; private set; }

    public IReadOnlyDictionary<uint, string> TypeNames => _typeNames;

    public IReadOnlyDictionary<uint, string> GroupNames => _groupNames;

    public IReadOnlyDictionary<uint, string> InstanceNames => _instanceNames;

    public static KeyMap Parse(IEnumerable<string> lines)
    {
        var map = new KeyMap();
        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//") || line.StartsWith('#'))
            {
                continue;
            }

            if (!TryParseLine(line, out KeyMapEntry entry))
            {
                continue;
            }

            map.Entries.Add(entry);

            string path = entry.Path.Replace('\\', '/');
            int slash = path.LastIndexOf('/');
            string fileName = slash >= 0 ? path[(slash + 1)..] : path;
            string folder = slash > 0 ? path[..slash].Split('/')[^1] : string.Empty;

            int dot = fileName.LastIndexOf('.');
            string instanceName = dot > 0 ? fileName[..dot] : fileName;
            string extension = dot > 0 ? fileName[(dot + 1)..] : string.Empty;

            bool instanceMatches = Hashing.Fnv.Hash(instanceName) == entry.InstanceId;
            if (instanceMatches)
            {
                map.VerifiedInstanceNames++;
            }
            else
            {
                map.UnverifiedEntries++;
            }

            AddName(map._instanceNames, entry.InstanceId, instanceName, instanceMatches);
            if (folder.Length > 0)
            {
                AddName(map._groupNames, entry.GroupId, folder, Hashing.Fnv.Hash(folder) == entry.GroupId);
            }

            if (extension.Length > 0)
            {
                AddName(map._typeNames, entry.TypeId, extension, Hashing.Fnv.Hash(extension) == entry.TypeId);
            }
        }

        return map;
    }

    public static KeyMap ParseFile(string path) => Parse(File.ReadLines(path));

    private static void AddName(Dictionary<uint, string> target, uint hash, string name, bool verified)
    {
        // Prefer a name that actually hashes to the id; otherwise keep the first seen.
        if (!target.TryGetValue(hash, out string? existing))
        {
            target[hash] = name;
            return;
        }

        if (verified && Hashing.Fnv.Hash(existing) != hash)
        {
            target[hash] = name;
        }
    }

    private static bool TryParseLine(string line, out KeyMapEntry entry)
    {
        entry = default;

        int open = line.IndexOf('{');
        int close = line.IndexOf('}', open + 1);
        if (open < 0 || close < 0)
        {
            return false;
        }

        string[] keys = line[(open + 1)..close].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (keys.Length < 3 ||
            !TryParseHex(keys[0], out uint type) ||
            !TryParseHex(keys[1], out uint group) ||
            !TryParseHex(keys[2], out uint instance))
        {
            return false;
        }

        int quoteStart = line.IndexOf('"', close);
        string path;
        if (quoteStart >= 0)
        {
            int quoteEnd = line.IndexOf('"', quoteStart + 1);
            if (quoteEnd < 0)
            {
                return false;
            }

            path = line[(quoteStart + 1)..quoteEnd];
        }
        else
        {
            path = line[(close + 1)..].Trim();
        }

        entry = new KeyMapEntry(type, group, instance, path);
        return true;
    }

    private static bool TryParseHex(string token, out uint value)
    {
        token = token.Trim();
        if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || token.StartsWith('#'))
        {
            return uint.TryParse(token[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        return uint.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Merges these names into a registry set (authoritative names win).</summary>
    public void ApplyTo(NameRegistrySet names)
    {
        foreach ((uint hash, string name) in _instanceNames)
        {
            names.AddFileName(hash, name);
        }

        foreach ((uint hash, string name) in _groupNames)
        {
            names.AddFileName(hash, name);
        }

        foreach ((uint hash, string name) in _typeNames)
        {
            names.AddTypeName(hash, name);
        }
    }

    /// <summary>Serialises the group/instance names as a <c>reg_file.txt</c>-style registry.</summary>
    public string BuildFileNameRegistry()
    {
        var merged = new Dictionary<uint, string>();
        foreach ((uint hash, string name) in _groupNames)
        {
            merged[hash] = name;
        }

        foreach ((uint hash, string name) in _instanceNames)
        {
            merged[hash] = name;
        }

        var builder = new System.Text.StringBuilder();
        builder.Append("# Names extracted from the game key map by SporeMaster 3\n");
        foreach ((uint hash, string name) in merged.OrderBy(p => p.Key))
        {
            bool alias = name.EndsWith('~') || Hashing.Fnv.Hash(name) != hash;
            builder.Append(alias ? $"{name}\t0x{hash:x8}\n" : $"{name}\n");
        }

        return builder.ToString();
    }

    public string BuildTypeRegistry()
    {
        var builder = new System.Text.StringBuilder();
        builder.Append("# Type names extracted from the game key map by SporeMaster 3\n");
        foreach ((uint hash, string name) in _typeNames.OrderBy(p => p.Key))
        {
            bool alias = name.EndsWith('~') || Hashing.Fnv.Hash(name) != hash;
            builder.Append(alias ? $"{name}\t0x{hash:x8}\n" : $"{name}\n");
        }

        return builder.ToString();
    }
}
