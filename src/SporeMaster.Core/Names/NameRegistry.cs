using System.Globalization;
using System.Text;
using SporeMaster.Core.Hashing;

namespace SporeMaster.Core.Names;

/// <summary>
/// A two-way map between names and the 32-bit hashes used by Spore. Multiple names may
/// point at the same hash (aliases, written with a trailing <c>~</c>).
/// </summary>
public sealed class NameRegistry
{
    private readonly Dictionary<string, uint> _nameToHash = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<uint, string> _hashToName = new();

    public NameRegistry(string name)
    {
        Name = name;
    }

    /// <summary>Human readable registry name (e.g. "Types").</summary>
    public string Name { get; }

    /// <summary>Source file name this registry is normally read from (e.g. "reg_type.txt").</summary>
    public string? FileName { get; init; }

    public int Count => _hashToName.Count;

    public bool IsEmpty => _hashToName.Count == 0 && _nameToHash.Count == 0;

    public IEnumerable<string> Names => _hashToName.Values;

    public void Add(string name, uint hash)
    {
        _nameToHash[name] = hash;

        if (!_hashToName.TryGetValue(hash, out string? existing))
        {
            _hashToName[hash] = name;
            return;
        }

        // Prefer a "true" name (one whose FNV hash matches) over an alias.
        bool existingTrue = Fnv.Hash(existing) == hash;
        bool newTrue = Fnv.Hash(name) == hash;
        if (!existingTrue && newTrue)
        {
            _hashToName[hash] = name;
        }
    }

    /// <summary>Returns the hash for a name, or null when it is not registered.</summary>
    public uint? GetHash(string name) =>
        _nameToHash.TryGetValue(name, out uint hash) ? hash : null;

    /// <summary>Returns the name for a hash, or null when it is not registered.</summary>
    public string? GetName(uint hash) =>
        _hashToName.TryGetValue(hash, out string? name) ? name : null;

    public void Clear()
    {
        _nameToHash.Clear();
        _hashToName.Clear();
    }

    /// <summary>Parses registry lines (name, or name&lt;tab&gt;hash, with # and // comments).</summary>
    public void Read(IEnumerable<string> lines)
    {
        foreach (string line in lines)
        {
            ReadLine(line);
        }
    }

    public void Read(TextReader reader)
    {
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            ReadLine(line);
        }
    }

    private void ReadLine(string rawLine)
    {
        int comment = rawLine.IndexOf("//", StringComparison.Ordinal);
        string line = (comment >= 0 ? rawLine[..comment] : rawLine).Trim();
        if (line.Length == 0 || line.StartsWith('#'))
        {
            return;
        }

        string[] parts = line.Split('\t');
        string name = parts[0].Trim();
        if (name.Length == 0)
        {
            return;
        }

        uint hash;
        if (parts.Length >= 2 && Fnv.TryParseLiteral(parts[1].Trim(), out uint parsed))
        {
            hash = parsed;
        }
        else
        {
            hash = Fnv.Hash(name);
        }

        Add(name, hash);
    }

    /// <summary>Serialises the registry back to the SporeModder FX text format.</summary>
    public string Serialize()
    {
        var builder = new StringBuilder();
        foreach ((uint hash, string name) in _hashToName.OrderBy(p => p.Key))
        {
            bool alias = name.EndsWith('~') || Fnv.Hash(name) != hash;
            builder.Append(alias
                ? $"{name}\t0x{hash.ToString("x8", CultureInfo.InvariantCulture)}\n"
                : $"{name}\n");
        }

        return builder.ToString();
    }
}
