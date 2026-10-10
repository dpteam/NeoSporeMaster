using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Diagnostics;
using SporeMaster.Core.Names;

namespace SporeMaster.Core.Presentation;

/// <summary>An open package plus the name registry, with helpers to describe its resources.</summary>
public sealed class PackageSession : IDisposable
{
    private readonly DatabasePackedFile _package;

    private PackageSession(DatabasePackedFile package, NameRegistrySet names)
    {
        _package = package;
        Names = names;
    }

    public NameRegistrySet Names { get; }

    public DbpfHeader? Header => _package.Header;

    public IReadOnlyList<DbpfEntry> Entries => _package.Entries;

    public int Count => _package.Entries.Count;

    public string FilePath { get; private set; } = string.Empty;

    public DiagnosticSink Diagnostics => _package.Diagnostics;

    public static PackageSession Open(string path)
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        DatabasePackedFile package = DatabasePackedFile.Open(path);
        return new PackageSession(package, names) { FilePath = path };
    }

    public IReadOnlyList<ResourceItem> BuildItems()
    {
        var items = new List<ResourceItem>(_package.Entries.Count);
        foreach (DbpfEntry entry in _package.Entries)
        {
            items.Add(new ResourceItem(entry, Names));
        }

        return items;
    }

    /// <summary>Merges an extra names registry (e.g. a recovered <c>names.txt</c>).</summary>
    public void LoadNames(string path) => Names.LoadProjectNames(path);

    /// <summary>Groups the resources into Explorer-like folders (one per group id).</summary>
    public List<ResourceFolder> BuildFolders() => ExplorerModel.BuildFolders(BuildItems());

    /// <summary>Reads a resource and (if necessary) decompresses it.</summary>
    public byte[] Read(DbpfEntry entry) => _package.ReadEntry(entry);

    public void Dispose() => _package.Dispose();
}
