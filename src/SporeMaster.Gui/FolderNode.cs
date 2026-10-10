using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SporeMaster.Core.Presentation;

namespace SporeMaster.Gui;

/// <summary>A node of the Explorer-like folder tree.</summary>
internal sealed class FolderNode : INotifyPropertyChanged
{
    private bool _isChecked;

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

    /// <summary>Explorer-style checkbox used to select several folders for extraction at once.</summary>
    public bool IsChecked
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
