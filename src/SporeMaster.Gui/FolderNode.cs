using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SporeMaster.Core.Presentation;

namespace SporeMaster.Gui;

/// <summary>A node of the Explorer-like folder tree.</summary>
/// <remarks>
/// Checkboxes follow the standard tree-view convention: the package (root) node is a tri-state
/// "select all" control (checked when every folder is checked, indeterminate when only some are),
/// while each folder is a normal two-state check.
/// </remarks>
internal sealed class FolderNode : INotifyPropertyChanged
{
    private bool? _isChecked;

    public FolderNode(string name, uint groupId, bool isRoot)
    {
        Name = name;
        GroupId = groupId;
        IsRoot = isRoot;
        IsThreeState = isRoot;
    }

    public string Name { get; }

    public uint GroupId { get; }

    public bool IsRoot { get; }

    /// <summary>Only the root allows the indeterminate (partially checked) visual.</summary>
    public bool IsThreeState { get; }

    public ObservableCollection<FolderNode> Children { get; } = new();

    /// <summary>The resources directly inside this folder (empty for the root).</summary>
    public List<ResourceItem> Items { get; } = new();

    /// <summary>
    /// Check state: true/false for a folder, or null for a partially checked root. The GUI drives this
    /// value (see <c>MainWindow</c>), so it is only ever set by the window's checkbox logic.
    /// </summary>
    public bool? IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }
    }

    public string CountText => Items.Count > 0 ? $"{Items.Count}" : string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public override string ToString() => Name;
}
