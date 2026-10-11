using System.Reflection;
using SporeMaster.Core.Hashing;

namespace SporeMaster.Core.Names;

/// <summary>
/// The collection of name registries (types, files/groups, properties, simulator attributes)
/// used to translate between names and hashes, mirroring SporeModder FX's <c>HashManager</c>.
/// </summary>
public sealed class NameRegistrySet
{
    private readonly NameRegistry _types = new("Types") { FileName = "reg_type.txt" };
    private readonly NameRegistry _files = new("File Names") { FileName = "reg_file.txt" };
    private readonly NameRegistry _properties = new("Properties") { FileName = "reg_property.txt" };
    private readonly NameRegistry _simulator = new("Simulator Attributes") { FileName = "reg_simulator.txt" };

    public NameRegistry Types => _types;

    /// <summary>Group and instance names share the same registry (as in SporeModder FX).</summary>
    public NameRegistry Files => _files;

    public NameRegistry Groups => _files;

    public NameRegistry Properties => _properties;

    public NameRegistry Simulator => _simulator;

    public bool IsLoaded { get; private set; }

    public IReadOnlyList<string> LoadWarnings { get; private set; } = Array.Empty<string>();

    /// <summary>
    /// Loads the built-in registries embedded in <c>SporeMaster.Core</c> (copied from
    /// SporeModder FX: <c>reg_type.txt</c>, <c>reg_file.txt</c>, <c>reg_property.txt</c>,
    /// <c>reg_simulator.txt</c>).
    /// </summary>
    public static NameRegistrySet LoadEmbedded()
    {
        var set = new NameRegistrySet();
        var warnings = new List<string>();

        LoadEmbeddedInto(set._types, "reg_type.txt", warnings);
        LoadEmbeddedInto(set._files, "reg_file.txt", warnings);
        LoadEmbeddedInto(set._properties, "reg_property.txt", warnings);

        if (!LoadEmbeddedInto(set._simulator, "reg_simulator.txt", warnings))
        {
            LoadEmbeddedInto(set._simulator, "reg_simulator_stub.txt", warnings);
        }

        set.LoadWarnings = warnings;
        set.IsLoaded = true;
        return set;
    }

    /// <summary>Loads registries from a directory of <c>reg_*.txt</c> files (external override).</summary>
    public static NameRegistrySet LoadFromDirectory(string directory)
    {
        var set = new NameRegistrySet();
        var warnings = new List<string>();

        LoadFileInto(set._types, Path.Combine(directory, "reg_type.txt"), warnings);
        LoadFileInto(set._files, Path.Combine(directory, "reg_file.txt"), warnings);
        LoadFileInto(set._properties, Path.Combine(directory, "reg_property.txt"), warnings);

        if (!LoadFileInto(set._simulator, Path.Combine(directory, "reg_simulator.txt"), warnings))
        {
            LoadFileInto(set._simulator, Path.Combine(directory, "reg_simulator_stub.txt"), warnings);
        }

        set.LoadWarnings = warnings;
        set.IsLoaded = true;
        return set;
    }

    private static bool LoadEmbeddedInto(NameRegistry registry, string fileName, List<string> warnings)
    {
        string? text = EmbeddedResources.ReadText(fileName);
        if (text is null)
        {
            warnings.Add($"Embedded registry '{fileName}' was not found.");
            return false;
        }

        registry.Read(SplitLines(text));
        return true;
    }

    private static bool LoadFileInto(NameRegistry registry, string path, List<string> warnings)
    {
        if (!File.Exists(path))
        {
            warnings.Add($"Registry file '{path}' was not found.");
            return false;
        }

        registry.Read(SplitLines(File.ReadAllText(path)));
        return true;
    }

    private static IEnumerable<string> SplitLines(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

    // ---- Hash helpers (SporeModder FX semantics) ----

    public uint GetFileHash(string name)
    {
        if (Fnv.TryParseLiteral(name, out uint literal) && (name.StartsWith('#') || name.StartsWith("0x", StringComparison.OrdinalIgnoreCase)))
        {
            return literal;
        }

        if (name.EndsWith('~'))
        {
            uint? alias = _files.GetHash(name);
            return alias ?? Fnv.Hash(name);
        }

        return Fnv.Hash(name);
    }

    /// <summary>True when a name is registered for this file/group/instance hash.</summary>
    public bool IsFileNameKnown(uint hash) => _files.GetName(hash) is not null;

    /// <summary>Registers a name for a file/group/instance hash (used for recovered names).</summary>
    public void AddFileName(uint hash, string name) => _files.Add(name, hash);

    /// <summary>Registers a name for a type hash (used for key-map derived names).</summary>
    public void AddTypeName(uint hash, string name) => _types.Add(name, hash);

    /// <summary>Merges additional names (e.g. a recovered <c>names.txt</c>) into the registry.</summary>
    public void LoadProjectNames(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        _files.Read(File.ReadAllLines(path));
    }

    /// <summary>Merges the game's key map (<c>KeyMap.txt</c>): authoritative real names.</summary>
    public void ApplyKeyMap(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        KeyMap.ParseFile(path).ApplyTo(this);
    }

    /// <summary>
    /// Builds a <c>reg_file.txt</c>-style registry for exactly the keys used by a package: one line
    /// per group/instance name. Names that do not hash to their id (aliases) are written as
    /// <c>name\thash</c>. Useful for making an unpacked mod self-describing.
    /// </summary>
    public string BuildNameRegistry(IEnumerable<(uint Type, uint Group, uint Instance)> keys)
    {
        var groups = new HashSet<uint>();
        var instances = new HashSet<uint>();
        foreach ((uint _, uint group, uint instance) in keys)
        {
            groups.Add(group);
            instances.Add(instance);
        }

        var merged = new SortedDictionary<uint, string>();
        foreach (uint hash in groups)
        {
            merged[hash] = GetFileName(hash);
        }

        foreach (uint hash in instances)
        {
            merged[hash] = GetFileName(hash);
        }

        var builder = new System.Text.StringBuilder();
        builder.Append("# Names used by the package (SporeMaster 3)\n");
        foreach ((uint hash, string name) in merged)
        {
            bool alias = name.EndsWith('~') || Hashing.Fnv.Hash(name) != hash;
            builder.Append(alias ? $"{name}\t0x{hash:x8}\n" : $"{name}\n");
        }

        return builder.ToString();
    }

    public uint GetTypeHash(string name)
    {
        if (name.StartsWith('#') || name.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return Fnv.TryParseLiteral(name, out uint literal) ? literal : Fnv.Hash(name);
        }

        return _types.GetHash(name) ?? Fnv.Hash(name);
    }

    public uint GetPropHash(string name)
    {
        if (name.StartsWith('#') || name.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return Fnv.TryParseLiteral(name, out uint literal) ? literal : Fnv.Hash(name);
        }

        return _properties.GetHash(name) ?? Fnv.Hash(name);
    }

    public string GetTypeName(uint hash) => _types.GetName(hash) ?? Fnv.ToHexUpper(hash);

    public string GetFileName(uint hash) => _files.GetName(hash) ?? Fnv.ToHexUpper(hash);

    public string GetPropName(uint hash) => _properties.GetName(hash) ?? Fnv.ToHexUpper(hash);

    public string GetSimulatorName(uint hash) => _simulator.GetName(hash) ?? Fnv.ToHexUpper(hash);

    /// <summary>Builds the canonical relative path used when unpacking: <c>group/name.ext</c>.</summary>
    public string GetRelativePath(uint typeId, uint groupId, uint instanceId)
    {
        string group = PathSegment(groupId, isType: false);
        string name = PathSegment(instanceId, isType: false);
        string extension = PathSegment(typeId, isType: true);
        return $"{group}/{name}.{extension}";
    }

    /// <summary>
    /// A path segment for an id: the registry name when it really hashes back to the id, otherwise a
    /// hex literal. Aliased names (with a <c>~</c>) and names shared by several ids (for example the
    /// type name <c>xml</c>) do not round-trip through the bare name, so a hex id is written instead.
    /// </summary>
    private string PathSegment(uint id, bool isType)
    {
        string name = isType ? GetTypeName(id) : GetFileName(id);
        bool roundTrips = isType ? GetTypeHash(name) == id : GetFileHash(name) == id;
        return roundTrips
            ? name
            : "0x" + id.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Parses a <c>group/name.ext</c> path back into hashes.</summary>
    public bool TryParseRelativePath(string relativePath, out uint typeId, out uint groupId, out uint instanceId)
    {
        typeId = groupId = instanceId = 0;
        string normalized = relativePath.Replace('\\', '/');
        int slash = normalized.IndexOf('/');
        if (slash < 0)
        {
            return false;
        }

        string group = normalized[..slash];
        string filePart = normalized[(slash + 1)..];
        int dot = filePart.LastIndexOf('.');
        if (dot < 0)
        {
            return false;
        }

        string name = filePart[..dot];
        string extension = filePart[(dot + 1)..];

        groupId = GetFileHash(group);
        instanceId = GetFileHash(name);
        typeId = GetTypeHash(extension);
        return true;
    }
}
