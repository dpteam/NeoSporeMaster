using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Presentation;
using CoreBitmapImage = SporeMaster.Core.Bitmaps.BitmapImage;

namespace SporeMaster.Gui;

/// <summary>
/// The main window: an Explorer-like browser for Spore packages. Resources are shown as files
/// grouped into folders (by group id); the left pane is a folder tree, the middle pane is the file
/// list and the right pane is a preview. Navigation (back/forward/up), a filter box, column
/// sorting, context menus and extraction are all supported.
/// </summary>
/// <remarks>
/// Every handler is defensive (see <see cref="Guarded"/>); a global handler in <see cref="App"/> is
/// the last line of defence.
/// </remarks>
public partial class MainWindow : Window
{
    private PackageSession? _session;
    private List<ResourceItem> _items = new();
    private FolderNode? _root;
    private FolderNode? _current;
    private readonly List<FolderNode> _history = new();
    private int _historyIndex = -1;
    private bool _flatView;
    private bool _navigating;
    private ResourceSort _sort = ResourceSort.Path;
    private bool _sortDescending;
    private int _treeWidth = 260;
    private bool _updatingAddress;

    private const string AllResourcesLabel = "(all resources)";

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(OnColumnHeaderClick));
        Loaded += (_, _) => Guarded(TryOpenFromCommandLine);
    }

    // ---- opening / closing ----

    private void OnOpenPackage(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open a Spore package",
            Filter = "Spore package (*.package)|*.package|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) == true)
        {
            OpenPackage(dialog.FileName);
        }
    });

    private void TryOpenFromCommandLine()
    {
        string[] args = Environment.GetCommandLineArgs();
        string? candidate = args.Skip(1).FirstOrDefault(a => !a.StartsWith("--") && File.Exists(a));
        if (candidate is not null)
        {
            OpenPackage(candidate);
        }
    }

    private void OpenPackage(string path)
    {
        try
        {
            _session?.Dispose();
            _session = PackageSession.Open(path);
            _items = _session.BuildItems().ToList();
            RebuildTree();
            Title = $"SporeMaster 3 — {Path.GetFileName(path)}";

            _history.Clear();
            _historyIndex = -1;
            Navigate(_root, record: true, resetHistory: true);

            StatusText.Text = $"{_session.Count} resource(s) — DBPF {_session.Header?.Version}";
        }
        catch (NotAPackageException ex)
        {
            ShowError("Not a package", ex.Message);
        }
        catch (Exception ex)
        {
            ShowError("Failed to open package", ex);
        }
    }

    private void RebuildTree()
    {
        _root = new FolderNode(Path.GetFileName(_session?.FilePath ?? "package"), 0, isRoot: true);
        foreach (ResourceItem item in _items)
        {
            _root.Items.Add(item);
        }

        foreach (ResourceFolder folder in ExplorerModel.BuildFolders(_items))
        {
            var node = new FolderNode(folder.Name, folder.GroupId, isRoot: false);
            node.Items.AddRange(folder.Items);
            _root.Children.Add(node);
        }

        FolderTree.ItemsSource = new[] { _root };
        RefreshAddressItems();
    }

    /// <summary>Fills the address bar with "(all)" plus each folder, Explorer-style.</summary>
    private void RefreshAddressItems()
    {
        _updatingAddress = true;
        try
        {
            var items = new List<string> { AllResourcesLabel };
            if (_root is not null)
            {
                items.AddRange(_root.Children.Select(c => $"📁 {c.Name}"));
            }

            AddressBox.ItemsSource = items;
            UpdateBreadcrumb();
        }
        finally
        {
            _updatingAddress = false;
        }
    }

    private void OnAddressSelected(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (!_updatingAddress && AddressBox.SelectedItem is string text)
        {
            NavigateAddress(text);
        }
    });

    private void OnAddressKeyDown(object sender, KeyEventArgs e) => Guarded(() =>
    {
        if (e.Key == Key.Enter)
        {
            NavigateAddress(AddressBox.Text);
            e.Handled = true;
        }
    });

    /// <summary>Navigates to a folder typed in the address bar (name, or "all" for everything).</summary>
    private void NavigateAddress(string? text)
    {
        if (_root is null || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        string value = text.Trim();
        if (value.Equals(AllResourcesLabel, StringComparison.OrdinalIgnoreCase)
            || value.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            SelectNode(_root);
            return;
        }

        // Strip a leading folder glyph if present.
        value = value.Replace("📁", string.Empty).Trim();

        FolderNode? folder = _root.Children.FirstOrDefault(
            c => string.Equals(c.Name, value, StringComparison.OrdinalIgnoreCase)
                 || c.Name.TrimEnd('~').Equals(value.TrimEnd('~'), StringComparison.OrdinalIgnoreCase));

        if (folder is not null)
        {
            SelectNode(folder);
        }
        else
        {
            StatusText.Text = $"Folder not found: {value}";
            UpdateBreadcrumb();
        }
    }

    private void OnClosePackage(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        _session?.Dispose();
        _session = null;
        _items = new List<ResourceItem>();
        _root = null;
        _current = null;
        _history.Clear();
        _historyIndex = -1;
        FolderTree.ItemsSource = null;
        ResourceList.ItemsSource = null;
        AddressBox.ItemsSource = null;
        AddressBox.Text = string.Empty;
        PreviewText.Text = string.Empty;
        PreviewImage.Source = null;
        PreviewHeader.Text = "Preview";
        Breadcrumb.Text = string.Empty;
        Title = "SporeMaster 3";
        StatusText.Text = "Open a .package file to begin.";
        CountText.Text = string.Empty;
    });

    private void OnExit(object sender, RoutedEventArgs e) => Guarded(Close);

    // ---- navigation ----

    private void OnFolderSelected(object sender, RoutedPropertyChangedEventArgs<object> e) => Guarded(() =>
    {
        if (!_navigating && !_flatView && e.NewValue is FolderNode node)
        {
            Navigate(node, record: true, resetHistory: false);
        }
    });

    private void Navigate(FolderNode? node, bool record, bool resetHistory)
    {
        if (node is null)
        {
            return;
        }

        _current = node;

        if (record)
        {
            if (resetHistory)
            {
                _history.Clear();
                _historyIndex = -1;
            }

            if (_history.Count == 0 || !ReferenceEquals(_history[_historyIndex == -1 ? 0 : _historyIndex], node))
            {
                _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
                _history.Add(node);
                _historyIndex = _history.Count - 1;
            }
        }

        RefreshList();
        UpdateBreadcrumb();
        UpdateNavigationButtons();
    }

    private void OnBack(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (_historyIndex > 0)
        {
            _historyIndex--;
            _navigating = true;
            SelectCurrent(record: false);
            _navigating = false;
        }
    });

    private void OnForward(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (_historyIndex >= 0 && _historyIndex < _history.Count - 1)
        {
            _historyIndex++;
            _navigating = true;
            SelectCurrent(record: false);
            _navigating = false;
        }
    });

    private void OnUp(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (_current is { IsRoot: false } && _root is not null)
        {
            SelectNode(_root);
        }
    });

    private void SelectCurrent(bool record)
    {
        if (_historyIndex >= 0 && _historyIndex < _history.Count)
        {
            Navigate(_history[_historyIndex], record: false, resetHistory: false);
        }
    }

    private void SelectNode(FolderNode node)
    {
        _navigating = true;
        SelectInTree(node);
        _navigating = false;
        Navigate(node, record: true, resetHistory: false);
    }

    private void SelectInTree(FolderNode node)
    {
        // The tree only has two levels (root + folders); find the matching container.
        if (node.IsRoot)
        {
            if (FolderTree.ItemContainerGenerator.ContainerFromItem(_root) is TreeViewItem rootItem)
            {
                rootItem.IsSelected = true;
            }

            return;
        }

        if (FolderTree.ItemContainerGenerator.ContainerFromItem(_root) is TreeViewItem parent)
        {
            parent.IsExpanded = true;
            parent.UpdateLayout();
            if (parent.ItemContainerGenerator.ContainerFromItem(node) is TreeViewItem child)
            {
                child.IsSelected = true;
            }
        }
    }

    private void UpdateBreadcrumb()
    {
        string package = _session is null ? string.Empty : Path.GetFileName(_session.FilePath);
        if (_flatView)
        {
            Breadcrumb.Text = $"{package}  ▸  (flat view, {_items.Count} items)";
            SetAddressText(AllResourcesLabel);
        }
        else if (_current is null || _current.IsRoot)
        {
            Breadcrumb.Text = $"{package}  ▸  (all {_items.Count} items)";
            SetAddressText(AllResourcesLabel);
        }
        else
        {
            Breadcrumb.Text = $"{package}  ▸  📁 {_current.Name}  ({_current.Items.Count} items)";
            SetAddressText($"📁 {_current.Name}");
        }
    }

    /// <summary>Updates the address bar text without re-triggering navigation.</summary>
    private void SetAddressText(string text)
    {
        _updatingAddress = true;
        try
        {
            AddressBox.Text = text;
        }
        finally
        {
            _updatingAddress = false;
        }
    }

    private void UpdateNavigationButtons() => Title = Title; // buttons are always enabled; kept simple

    // ---- filtering / sorting / listing ----

    private void OnFilterChanged(object sender, RoutedEventArgs e) => Guarded(RefreshList);

    private void RefreshList()
    {
        IEnumerable<ResourceItem> source = _flatView
            ? _items
            : _current?.Items ?? _items;

        List<ResourceItem> filtered = ExplorerModel.Filter(source, FilterBox?.Text);
        List<ResourceItem> sorted = ExplorerModel.Sort(filtered, _sort, _sortDescending);

        ResourceList.ItemsSource = sorted;
        CountText.Text = $"{sorted.Count} / {source.Count()}";
    }

    private void OnColumnHeaderClick(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (e.OriginalSource is not GridViewColumnHeader header || header.Column is null)
        {
            return;
        }

        ResourceSort sort = (header.Column.Header as string) switch
        {
            "Type" => ResourceSort.Type,
            "Size" => ResourceSort.PackedSize,
            "Mem" => ResourceSort.MemSize,
            "Comp" => ResourceSort.PackedSize,
            _ => ResourceSort.Path,
        };

        if (sort == _sort)
        {
            _sortDescending = !_sortDescending;
        }
        else
        {
            _sort = sort;
            _sortDescending = false;
        }

        RefreshList();
    });

    private void OnRefresh(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (_session is not null)
        {
            OpenPackage(_session.FilePath);
        }
    });

    private void OnViewDetails(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        ResourceList.View = BuildDetailsView();
    });

    private void OnViewLargeIcons(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        var view = new GridView();
        view.Columns.Add(new GridViewColumn { Header = "Name", DisplayMemberBinding = new System.Windows.Data.Binding("LargeName"), Width = 300 });
        ResourceList.View = view;
    });

    private GridView BuildDetailsView()
    {
        var view = new GridView();
        view.Columns.Add(new GridViewColumn { Header = "", DisplayMemberBinding = new System.Windows.Data.Binding("Glyph"), Width = 26 });
        view.Columns.Add(new GridViewColumn { Header = "Name", DisplayMemberBinding = new System.Windows.Data.Binding("ShortName"), Width = 240 });
        view.Columns.Add(new GridViewColumn { Header = "Type", DisplayMemberBinding = new System.Windows.Data.Binding("TypeName"), Width = 110 });
        view.Columns.Add(new GridViewColumn { Header = "Size", DisplayMemberBinding = new System.Windows.Data.Binding("PackedSize"), Width = 80 });
        view.Columns.Add(new GridViewColumn { Header = "Mem", DisplayMemberBinding = new System.Windows.Data.Binding("MemSize"), Width = 80 });
        view.Columns.Add(new GridViewColumn { Header = "Comp", DisplayMemberBinding = new System.Windows.Data.Binding("Compression"), Width = 60 });
        return view;
    }

    private void OnFlatViewToggle(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        _flatView = !_flatView;

        if (_flatView)
        {
            _treeWidth = (int)TreeColumn.Width.Value;
            TreeColumn.MinWidth = 0;
            TreeColumn.Width = new GridLength(0);
        }
        else
        {
            TreeColumn.MinWidth = 160;
            TreeColumn.Width = new GridLength(_treeWidth <= 0 ? 260 : _treeWidth);
        }

        RefreshList();
        UpdateBreadcrumb();
    });

    // ---- preview ----

    private void OnSelectionChanged(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (ResourceList.SelectedItem is ResourceItem row)
        {
            ShowPreview(row);
            UpdateDetailsPane(row);
        }
        else
        {
            DetailsPane.Visibility = Visibility.Collapsed;
        }
    });

    /// <summary>Fills the Explorer-style details pane for the selected resource.</summary>
    private void UpdateDetailsPane(ResourceItem row)
    {
        DetailsName.Text = $"{row.Glyph}  {row.ShortName}";
        DetailsType.Text = row.TypeName;
        DetailsKey.Text = row.Key;
        DetailsSize.Text = $"{row.PackedSize} bytes packed, {row.MemSize} bytes in memory ({row.Compression}) — {row.Path}";
        DetailsPane.Visibility = Visibility.Visible;
    }

    private void OnToggleDetailsPane(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        DetailsPane.Visibility = DetailsPane.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;

        if (DetailsPane.Visibility == Visibility.Visible && ResourceList.SelectedItem is ResourceItem row)
        {
            UpdateDetailsPane(row);
        }
    });

    private void OnPreviewSelected(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (ResourceList.SelectedItem is ResourceItem row)
        {
            ShowPreview(row);
        }
    });

    /// <summary>Renders the preview for a resource, never throwing.</summary>
    internal void ShowPreview(ResourceItem row)
    {
        PreviewHeader.Text = $"{row.Path}   [{row.Key}]";

        if (_session is null)
        {
            ShowText(ResourcePreview.Empty().Text ?? string.Empty);
            return;
        }

        byte[] data;
        try
        {
            data = _session.Read(row.Entry);
        }
        catch (Exception ex)
        {
            ShowText($"Failed to read resource: {ex.Message}");
            return;
        }

        ResourcePreview preview = PreviewEngine.Build(row, data, _session.Names);

        if (preview.IsImage && TryDecodeImage(preview.PngBytes!, out BitmapSource? bitmap))
        {
            PreviewImage.Source = bitmap;
            PreviewImageScroller.Visibility = Visibility.Visible;
            PreviewText.Visibility = Visibility.Collapsed;
        }
        else if (preview.IsImage)
        {
            ShowText("(image could not be decoded; showing a hex preview)\n\n" +
                Convert.ToHexString(data.AsSpan(0, Math.Min(data.Length, 512))));
        }
        else
        {
            ShowText(preview.Text ?? string.Empty);
        }
    }

    private static bool TryDecodeImage(byte[] bytes, out BitmapSource? bitmap)
    {
        bitmap = null;
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = new MemoryStream(bytes);
            image.EndInit();
            image.Freeze();
            bitmap = image;
            return true;
        }
        catch (Exception ex)
        {
            App.LogCrash("image decode", ex);
            return false;
        }
    }

    private void ShowText(string text)
    {
        PreviewText.Text = text;
        PreviewText.Visibility = Visibility.Visible;
        PreviewImageScroller.Visibility = Visibility.Collapsed;
    }

    // ---- extraction ----

    private void OnListDoubleClick(object sender, MouseButtonEventArgs e) => Guarded(() =>
    {
        if (ResourceList.SelectedItem is ResourceItem)
        {
            ExtractSelected();
        }
    });

    private void OnExtractSelected(object sender, RoutedEventArgs e) => Guarded(ExtractSelected);

    private void ExtractSelected()
    {
        if (_session is null)
        {
            MessageBox.Show(this, "Open a package first.", "No package");
            return;
        }

        var rows = ResourceList.SelectedItems.Cast<ResourceItem>().ToList();
        if (rows.Count == 0)
        {
            MessageBox.Show(this, "Select a resource first.", "Nothing selected");
            return;
        }

        // A single resource: let the user pick a file name (with a suggested extension).
        if (rows.Count == 1)
        {
            ResourceItem row = rows[0];
            var dialog = new SaveFileDialog
            {
                Title = "Extract resource",
                FileName = Path.GetFileName(row.ShortName),
                Filter = "All files (*.*)|*.*",
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            byte[] data = _session.Read(row.Entry);
            WriteConverted(row, data, dialog.FileName);
            StatusText.Text = $"Extracted {row.Path} ({data.Length} bytes).";
            return;
        }

        // Multiple resources: pick a folder and extract each (as an Explorer "extract selection").
        var folderDialog = new OpenFolderDialog { Title = "Choose an output folder" };
        if (folderDialog.ShowDialog(this) != true)
        {
            return;
        }

        (int written, int failed) = ExtractItems(rows, folderDialog.FolderName);
        StatusText.Text = $"Extracted {written} resource(s) to {folderDialog.FolderName}" +
            (failed > 0 ? $" ({failed} failed)" : string.Empty);
        MessageBox.Show(
            this,
            $"Extracted {written} resource(s)." + (failed > 0 ? $"\n{failed} failed (see status bar)." : string.Empty),
            "Extract selected",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OnExtractFolder(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (_session is null)
        {
            MessageBox.Show(this, "Open a package first.", "No package");
            return;
        }

        List<ResourceItem> source = _flatView || _current?.IsRoot == true
            ? _items
            : _current?.Items ?? _items;

        var dialog = new OpenFolderDialog { Title = "Choose an output folder" };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        (int written, int failed) = ExtractItems(source, dialog.FolderName);
        StatusText.Text = $"Extracted {written} resource(s) to {dialog.FolderName}" + (failed > 0 ? $" ({failed} failed)" : string.Empty);
        MessageBox.Show(this, $"Extracted {written} resource(s)." + (failed > 0 ? $"\n{failed} failed (see status bar)." : string.Empty),
            "Extract folder", MessageBoxButton.OK, MessageBoxImage.Information);
    });

    private void OnExtractAll(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (_session is null)
        {
            MessageBox.Show(this, "Open a package first.", "No package");
            return;
        }

        var dialog = new OpenFolderDialog { Title = "Choose an output folder" };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        (int written, int failed) = ExtractItems(_items, dialog.FolderName);
        StatusText.Text = $"Extracted {written} resource(s) to {dialog.FolderName}" + (failed > 0 ? $" ({failed} failed)" : string.Empty);
        MessageBox.Show(this, $"Extracted {written} resource(s)." + (failed > 0 ? $"\n{failed} failed (see status bar)." : string.Empty),
            "Extract all", MessageBoxButton.OK, MessageBoxImage.Information);
    });

    // ---- checked folders (multi-select) ----

    /// <summary>All folders currently ticked in the tree.</summary>
    private List<FolderNode> CheckedFolders() =>
        _root is null ? new List<FolderNode>() : _root.Children.Where(c => c.IsChecked).ToList();

    /// <summary>All resources inside the checked folders.</summary>
    private List<ResourceItem> CheckedItems()
    {
        var items = new List<ResourceItem>();
        foreach (FolderNode folder in CheckedFolders())
        {
            items.AddRange(folder.Items);
        }

        return items;
    }

    private void OnFolderCheckChanged(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        int checkedCount = CheckedFolders().Count;
        StatusText.Text = checkedCount > 0
            ? $"{checkedCount} folder(s) checked ({CheckedItems().Count} resource(s)) — use Tools → Extract Checked Folders."
            : "No folders checked.";
    });

    private void OnCheckAllFolders(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (_root is null)
        {
            return;
        }

        foreach (FolderNode folder in _root.Children)
        {
            folder.IsChecked = true;
        }

        StatusText.Text = $"{CheckedFolders().Count} folder(s) checked.";
    });

    private void OnUncheckAllFolders(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (_root is null)
        {
            return;
        }

        foreach (FolderNode folder in _root.Children)
        {
            folder.IsChecked = false;
        }

        StatusText.Text = "No folders checked.";
    });

    private void OnExtractChecked(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (_session is null)
        {
            MessageBox.Show(this, "Open a package first.", "No package");
            return;
        }

        List<ResourceItem> items = CheckedItems();
        if (items.Count == 0)
        {
            MessageBox.Show(this, "Tick one or more folders in the tree first.", "Nothing checked");
            return;
        }

        var dialog = new OpenFolderDialog { Title = "Choose an output folder" };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        (int written, int failed) = ExtractItems(items, dialog.FolderName);
        StatusText.Text = $"Extracted {written} resource(s) to {dialog.FolderName}" + (failed > 0 ? $" ({failed} failed)" : string.Empty);
        MessageBox.Show(this, $"Extracted {written} resource(s) from {CheckedFolders().Count} folder(s)." + (failed > 0 ? $"\n{failed} failed (see status bar)." : string.Empty),
            "Extract checked folders", MessageBoxButton.OK, MessageBoxImage.Information);
    });

    // ---- drag & drop ----

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnFileDrop(object sender, DragEventArgs e) => Guarded(() =>
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files)
        {
            return;
        }

        string? package = files.FirstOrDefault(f => f.EndsWith(".package", StringComparison.OrdinalIgnoreCase));
        if (package is not null)
        {
            OpenPackage(package);
        }
        else if (files.Length > 0)
        {
            StatusText.Text = "Drop a .package file to open it.";
        }
    });

    private (int Written, int Failed) ExtractItems(IEnumerable<ResourceItem> items, string outputDir)
    {
        int written = 0;
        int failed = 0;        foreach (ResourceItem row in items)
        {
            try
            {
                byte[] data = _session!.Read(row.Entry);
                string relative = row.Path.Replace('/', Path.DirectorySeparatorChar);
                if (NeedsSuffix(row))
                {
                    relative += SuffixFor(row);
                }

                string target = Path.Combine(outputDir, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                WriteConverted(row, data, target);
                written++;
            }
            catch
            {
                failed++;
            }
        }

        return (written, failed);
    }

    /// <summary>Writes a resource, converting properties/textures/bitmaps to friendly formats.</summary>
    private void WriteConverted(ResourceItem row, byte[] data, string targetPath)
    {
        uint type = row.TypeId;

        if (ResourceTypeIds.IsProperty(type))
        {
            Core.Properties.PropertyList list = Core.Properties.PropertyList.Read(new MemoryStream(data));
            using FileStream output = File.Create(targetPath);
            list.WriteXml(output, _session!.Names);
        }
        else if (ResourceTypeIds.IsTexture(type))
        {
            Core.Textures.RasterTexture raster = Core.Textures.RasterTexture.Read(new MemoryStream(data));
            using FileStream output = File.Create(targetPath);
            raster.ToDds().Write(output);
        }
        else if (ResourceTypeIds.IsBitmap(type))
        {
            CoreBitmapImage bitmap = CoreBitmapImage.Read(new MemoryStream(data), type);
            using FileStream output = File.Create(targetPath);
            Core.Imaging.PngCodec.Encode(bitmap.Image, output);
        }
        else
        {
            File.WriteAllBytes(targetPath, data);
        }
    }

    private static bool NeedsSuffix(ResourceItem row) =>
        ResourceTypeIds.IsProperty(row.TypeId) || ResourceTypeIds.IsTexture(row.TypeId) || ResourceTypeIds.IsBitmap(row.TypeId);

    private static string SuffixFor(ResourceItem row) =>
        ResourceTypeIds.IsProperty(row.TypeId) ? ".xml"
        : ResourceTypeIds.IsTexture(row.TypeId) ? ".dds"
        : ".png";

    // ---- misc commands ----

    private void OnLoadNames(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (_session is null)
        {
            MessageBox.Show(this, "Open a package first.", "No package");
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Load recovered names",
            Filter = "Names registry (*.txt)|*.txt|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _session.LoadNames(dialog.FileName);
        _items = _session.BuildItems().ToList();
        RebuildTree();
        Navigate(_root, record: false, resetHistory: false);
        StatusText.Text = $"Loaded names from {Path.GetFileName(dialog.FileName)}.";
    });

    private void OnCopyKey(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (ResourceList.SelectedItem is ResourceItem row)
        {
            Clipboard.SetText(row.Key);
            StatusText.Text = $"Copied {row.Key}";
        }
    });

    private void OnCopyName(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (ResourceList.SelectedItem is ResourceItem row)
        {
            Clipboard.SetText(row.ShortName);
            StatusText.Text = $"Copied {row.ShortName}";
        }
    });

    private void OnAbout(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        MessageBox.Show(
            this,
            "SporeMaster 3\n\nA .NET 9 toolkit for exploring and editing Spore .package (DBPF) files.\n" +
            "Supports DBPF/DBBF, properties, locales, textures, bitmaps, RenderWare 4 and hash guessing.",
            "About SporeMaster 3",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    });

    // ---- fault tolerance helpers ----

    private void Guarded(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            ShowError("Unexpected error", ex);
        }
    }

    private void ShowError(string title, string message)
    {
        App.LogCrash(title, new Exception(message));
        try
        {
            StatusText.Text = $"{title}: {message}";
        }
        catch
        {
            // ignore
        }

        MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ShowError(string title, Exception ex)
    {
        App.LogCrash(title, ex);
        ShowError(title, ex.Message);
    }

    // ---- test hooks (used by --selftest; no UI shown) ----

    internal int RowCount => _items.Count;

    internal int ResourceCount => _session?.Count ?? 0;

    internal int FolderCount => _root?.Children.Count ?? 0;

    internal void OpenPackageForTest(string path) => OpenPackage(path);

    internal IReadOnlyList<ResourceItem> RowsForTest() => _items;

    internal IReadOnlyList<ResourceItem> VisibleRowsForTest() =>
        ResourceList.ItemsSource as IReadOnlyList<ResourceItem> ?? Array.Empty<ResourceItem>();

    internal void SelectFolderForTest(int index)
    {
        if (_root is not null && index >= 0 && index < _root.Children.Count)
        {
            SelectNode(_root.Children[index]);
        }
    }

    internal void ApplyFilterForTest(string filter)
    {
        FilterBox.Text = filter;
        RefreshList();
    }

    internal void CheckFolderForTest(int index, bool isChecked)
    {
        if (_root is not null && index >= 0 && index < _root.Children.Count)
        {
            _root.Children[index].IsChecked = isChecked;
        }
    }

    internal int CheckedResourceCountForTest() => CheckedItems().Count;

    internal int CheckedFolderCountForTest() => CheckedFolders().Count;

    internal void NavigateAddressForTest(string text) => NavigateAddress(text);

    internal int AddressItemCountForTest() => AddressBox.ItemsSource is System.Collections.IEnumerable items
        ? items.Cast<object>().Count()
        : 0;

    internal ResourcePreview PreviewForTest(ResourceItem row)
    {
        if (_session is null)
        {
            return ResourcePreview.Empty();
        }

        try
        {
            byte[] data = _session.Read(row.Entry);
            return PreviewEngine.Build(row, data, _session.Names);
        }
        catch (Exception ex)
        {
            return ResourcePreview.AsText($"Preview failed: {ex.Message}", "error");
        }
    }

    /// <summary>Runs the full preview path (including image decoding) for a row, as the UI would.</summary>
    internal string? SelectionForTest(ResourceItem row)
    {
        try
        {
            ShowPreview(row);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <summary>Fills the details pane for a row (as selection does) and returns the rendered text.</summary>
    internal string DetailsForTest(ResourceItem row)
    {
        UpdateDetailsPane(row);
        return $"{DetailsName.Text} | {DetailsType.Text} | {DetailsKey.Text} | {DetailsSize.Text}";
    }
}
