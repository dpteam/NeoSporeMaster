using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Names;

namespace SporeMaster.Core.Presentation;

/// <summary>
/// A single resource presented to a user interface (or CLI): resolved name, ids and sizes.
/// This type intentionally has no UI dependency so it can be reused and tested headlessly.
/// </summary>
public sealed class ResourceItem
{
    public ResourceItem(DbpfEntry entry, NameRegistrySet names)
    {
        Entry = entry;
        Index = entry.Index;
        TypeId = entry.Key.TypeId;
        GroupId = entry.Key.GroupId;
        InstanceId = entry.Key.InstanceId;
        Compressed = entry.IsCompressed;
        PackedSize = entry.PackedSize;
        MemSize = entry.MemSize;
        Path = names.GetRelativePath(entry.Key.TypeId, entry.Key.GroupId, entry.Key.InstanceId);
        TypeName = names.GetTypeName(entry.Key.TypeId);
    }

    public DbpfEntry Entry { get; }

    public int Index { get; }

    public uint TypeId { get; }

    public uint GroupId { get; }

    public uint InstanceId { get; }

    public bool Compressed { get; }

    public uint PackedSize { get; }

    public uint MemSize { get; }

    /// <summary>Resolved <c>group/name.ext</c> path.</summary>
    public string Path { get; }

    public string TypeName { get; }

    public string Key => $"0x{TypeId:X8}:0x{GroupId:X8}:0x{InstanceId:X8}";

    public string Compression => Compressed ? "RefPack" : "-";

    /// <summary>Case-insensitive match against the path, type and key.</summary>
    public bool Matches(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        return Path.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || TypeName.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || Key.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }
}
