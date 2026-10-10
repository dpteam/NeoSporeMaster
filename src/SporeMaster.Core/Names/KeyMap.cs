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
    private readonly Dictionary<(uint Type, uint Group, uint Instance), string> _paths = new();

    public List<KeyMapEntry> Entries { get; } = new();

    /// <summary>Number of instance paths whose name really hashes to their id.</summary>
    public int VerifiedInstanceNames { get; private set; }

    /// <summary>Number of entries whose key did not match the path (should be 0).</summary>
    public int UnverifiedEntries { get; private set; }

    /// <summary>Number of <c>PreBakedKeys.txt</c> lines merged (they carry no resource type).</summary>
    public int PreBakedEntries { get; private set; }

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

            if (TryParsePreBaked(line, out uint preBakedInstance, out uint preBakedGroup, out string preBakedName, out string preBakedGroupName))
            {
                map.AddPreBaked(preBakedInstance, preBakedGroup, preBakedName, preBakedGroupName);
                continue;
            }

            if (!TryParseLine(line, out KeyMapEntry entry))
            {
                continue;
            }

            map.Entries.Add(entry);

            // Full-key lookup gives the exact real path for a resource.
            map._paths[(entry.TypeId, entry.GroupId, entry.InstanceId)] = entry.Path;

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

    /// <summary>Returns the exact real path for a resource key, or null when it is not in the map.</summary>
    public string? GetPath(uint typeId, uint groupId, uint instanceId) =>
        _paths.TryGetValue((typeId, groupId, instanceId), out string? path) ? path : null;

    /// <summary>
    /// Returns the real extension (without the dot) used for a resource type, or null when unknown.
    /// For example the text type <c>0x024A0E52</c> resolves to <c>txt</c>.
    /// </summary>
    public string? GetExtension(uint typeId) =>
        _typeNames.TryGetValue(typeId, out string? extension) ? extension : null;

    /// <summary>Builds a <c>type-hash extension</c> table, one entry per type.</summary>
    public string BuildExtensionTable()
    {
        var builder = new System.Text.StringBuilder();
        builder.Append("# Type -> extension map derived from the game key map by SporeMaster 3\n");
        foreach ((uint hash, string extension) in _typeNames.OrderBy(p => p.Key))
        {
            builder.Append($"0x{hash:x8}\t{extension}\n");
        }

        return builder.ToString();
    }

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

    /// <summary>
    /// Parses one <c>PreBakedKeys.txt</c> line:
    /// <c>instanceHash &lt;tab&gt; groupHash &lt;tab&gt; name &lt;tab&gt; path</c>. Returns the instance/group
    /// hashes and the best instance/group names (from the name column, and from the
    /// <c>name_@group!instance.ext</c> path suffix).
    /// </summary>
    private static bool TryParsePreBaked(
        string line,
        out uint instance,
        out uint group,
        out string instanceName,
        out string groupName)
    {
        instance = 0;
        group = 0;
        instanceName = string.Empty;
        groupName = string.Empty;

        string[] fields = line.Split('\t');
        if (fields.Length < 4)
        {
            return false;
        }

        if (!TryParseHex(fields[0], out instance) || !TryParseHex(fields[1], out group))
        {
            return false;
        }

        string path = fields[3].Trim();
        instanceName = fields[2].Trim();

        // Derive names from the "name_@group!instancHash.ext" path when the columns are empty.
        int slash = path.LastIndexOfAny(new[] { '\\', '/' });
        string file = slash >= 0 ? path[(slash + 1)..] : path;
        int bang = file.IndexOf('!');
        string head = bang > 0 ? file[..bang] : file;
        int at = head.IndexOf("_@", StringComparison.Ordinal);
        if (at >= 0)
        {
            if (instanceName.Length == 0)
            {
                instanceName = head[..at];
            }

            groupName = head[(at + 2)..];
        }
        else if (instanceName.Length == 0)
        {
            int dot = head.LastIndexOf('.');
            instanceName = dot > 0 ? head[..dot] : head;
        }

        return true;
    }

    /// <summary>
    /// Merges one <c>PreBakedKeys.txt</c> entry. These lines have no resource type, so they only
    /// contribute instance/group names (kept when they really hash to the id) — never the exact
    /// path lookup.
    /// </summary>
    private void AddPreBaked(uint instance, uint group, string instanceName, string groupName)
    {
        PreBakedEntries++;

        if (instanceName.Length > 0)
        {
            bool matches = Hashing.Fnv.Hash(instanceName) == instance;
            AddName(_instanceNames, instance, instanceName, matches);
            if (matches)
            {
                VerifiedInstanceNames++;
            }
            else
            {
                UnverifiedEntries++;
            }
        }

        if (groupName.Length > 0 && Hashing.Fnv.Hash(groupName) == group)
        {
            AddName(_groupNames, group, groupName, verified: true);
        }
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
