using System.Collections.ObjectModel;
using SporeMaster.Core.Presentation;

namespace SporeMaster.Gui;

/// <summary>A node of the Explorer-like folder tree.</summary>
internal sealed class FolderNode
{
    public FolderNode(string name, uint groupId, bool isRoot)
    {
        Name = name;
        GroupId = groupId;
        IsRoot = isRoot;
    }

    public string Name { get; }

    public uint GroupId { get; }

    public bool IsRoot { get; }

    public ObservableCollection<FolderNode> Children { get; } = new();

    /// <summary>The resources directly inside this folder (empty for the root).</summary>
    public List<ResourceItem> Items { get; } = new();

    public string CountText => Items.Count > 0 ? $"{Items.Count}" : string.Empty;

    public override string ToString() => Name;
}
