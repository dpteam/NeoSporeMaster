using SporeMaster.Core.Hashing;

namespace SporeMaster.Core.Presentation;

/// <summary>A "folder" of resources that share the same group id (the Explorer-like tree node).</summary>
public sealed class ResourceFolder
{
    public ResourceFolder(uint groupId, string name)
    {
        GroupId = groupId;
        Name = name;
    }

    /// <summary>The group id shared by every resource inside.</summary>
    public uint GroupId { get; }

    /// <summary>Human-readable folder name (the resolved group name, or <c>#hex</c>).</summary>
    public string Name { get; }

    /// <summary>The resources in this folder, already sorted by path.</summary>
    public List<ResourceItem> Items { get; } = new();

    public int Count => Items.Count;

    /// <summary>Number of distinct resource types in the folder.</summary>
    public int TypeCount => Items.Select(i => i.TypeId).Distinct().Count();

    public override string ToString() => $"{Name} ({Count})";
}

/// <summary>How a resource list is ordered.</summary>
public enum ResourceSort
{
    Path,
    Type,
    Group,
    Instance,
    PackedSize,
    MemSize,
}

/// <summary>
/// Groups a package's resources into Explorer-like "folders" (one per group id) and offers
/// filtering/sorting helpers. UI-free so it can be reused and tested headlessly.
/// </summary>
public static class ExplorerModel
{
    /// <summary>Builds the folder list, sorted by folder name.</summary>
    public static List<ResourceFolder> BuildFolders(IEnumerable<ResourceItem> items)
    {
        var folders = new Dictionary<uint, ResourceFolder>();

        foreach (ResourceItem item in items)
        {
            if (!folders.TryGetValue(item.GroupId, out ResourceFolder? folder))
            {
                folder = new ResourceFolder(item.GroupId, FolderName(item));
                folders[item.GroupId] = folder;
            }

            folder.Items.Add(item);
        }

        foreach (ResourceFolder folder in folders.Values)
        {
            folder.Items.Sort((a, b) => string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase));
        }

        return folders.Values
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(f => f.GroupId)
            .ToList();
    }

    private static string FolderName(ResourceItem item)
    {
        // The path is "group/name.ext"; the portion before the first slash is the group name.
        int slash = item.Path.IndexOf('/');
        return slash > 0 ? item.Path[..slash] : Fnv.ToHex(item.GroupId);
    }

    /// <summary>
    /// Filters items by a query over the path, type name and key. Supports plain substrings,
    /// wildcards (<c>*</c>/<c>?</c>) and regex (<c>re:</c>). A content clause
    /// (<c>text:</c>/<c>hex:</c>) is ignored here (use <see cref="ResourceContentSearch"/>).
    /// </summary>
    public static List<ResourceItem> Filter(IEnumerable<ResourceItem> items, string? query)
    {
        NameFilter filter = NameFilter.Compile(ContentQuery.Parse(query).NamePart);
        if (filter.IsEmpty)
        {
            return items.ToList();
        }

        return items.Where(i => i.Matches(filter)).ToList();
    }

    /// <summary>Sorts items for the given column, ascending or descending.</summary>
    public static List<ResourceItem> Sort(IEnumerable<ResourceItem> items, ResourceSort sort, bool descending)
    {
        IOrderedEnumerable<ResourceItem> ordered = sort switch
        {
            ResourceSort.Type => items.OrderBy(i => i.TypeName, StringComparer.OrdinalIgnoreCase).ThenBy(i => i.Path, StringComparer.OrdinalIgnoreCase),
            ResourceSort.Group => items.OrderBy(i => i.GroupId).ThenBy(i => i.Path, StringComparer.OrdinalIgnoreCase),
            ResourceSort.Instance => items.OrderBy(i => i.InstanceId),
            ResourceSort.PackedSize => items.OrderBy(i => i.PackedSize),
            ResourceSort.MemSize => items.OrderBy(i => i.MemSize),
            _ => items.OrderBy(i => i.Path, StringComparer.OrdinalIgnoreCase),
        };

        return (descending ? ordered.Reverse() : ordered).ToList();
    }
}
