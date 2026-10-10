using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
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
    private bool _syncingChecks;
    private bool _detailsPaneVisible = true;

    /// <summary>Paths of packages opened before the current one (so nested packages can be left again).</summary>
    private readonly List<string> _packageStack = new();

    private static string AllResourcesLabel => Core.Localization.Localizer.Default["gui.all"];

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(OnColumnHeaderClick));
        Loaded += (_, _) => Guarded(TryOpenFromCommandLine);

        // Reflect the active language in the View -> Language submenu.
        bool russian = string.Equals(Core.Localization.Localizer.Default.Language, "ru", StringComparison.OrdinalIgnoreCase);
        LanguageRuMenuItem.IsChecked = russian;
        LanguageEnMenuItem.IsChecked = !russian;
    }

    // ---- language ----

    private void OnLanguageEn(object sender, RoutedEventArgs e) => Guarded(() => SwitchLanguage("en"));

    private void OnLanguageRu(object sender, RoutedEventArgs e) => Guarded(() => SwitchLanguage("ru"));

    /// <summary>Switches the UI language, persists it and reloads the window (keeping the open package).</summary>
    private void SwitchLanguage(string code)
    {
        if (string.Equals(Core.Localization.Localizer.Default.Language, code, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Core.Localization.Localizer.Use(code);
        GuiSettings.Language = code;

        string? path = _session?.FilePath;
        var window = new MainWindow();
        Application.Current.MainWindow = window;
        window.Show();
        if (!string.IsNullOrEmpty(path))
        {
            window.OpenPackageForTest(path);
        }

        Close();
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

    private void OnOpenPreviousPackage(object sender, RoutedEventArgs e) => OpenPreviousPackage();

    // ---- compare with another package ----

    private void OnComparePackages(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (_session is null)
        {
            MessageBox.Show(this, Core.Localization.Localizer.Default["gui.msg.openFirst"], "SporeMaster 3");
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = Core.Localization.Localizer.Default["gui.menu.compare"],
            Filter = "Spore package (*.package)|*.package|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            using PackageSession other = PackageSession.Open(dialog.FileName);
            Core.Diff.ArchiveDiffResult result = _session.Compare(other);

            var report = new System.Text.StringBuilder();
            report.AppendLine($"A: {Path.GetFileName(_session.FilePath)} ({result.CountA} resource(s))");
            report.AppendLine($"B: {Path.GetFileName(dialog.FileName)} ({result.CountB} resource(s))");
            report.AppendLine();
            report.AppendLine($"added:   {result.Added.Count}   (only in B)");
            report.AppendLine($"removed: {result.Removed.Count}   (only in A)");
            report.AppendLine($"changed: {result.Changed.Count}   (both, different content)");
            report.AppendLine($"same:    {result.Identical}");
            report.AppendLine();

            AppendDiff(report, _session.Names, "Added (only in B)", result.Added);
            AppendDiff(report, _session.Names, "Removed (only in A)", result.Removed);
            AppendDiff(report, _session.Names, "Changed", result.Changed);

            if (result.IsIdentical)
            {
                report.AppendLine("Archives are identical (by key and content).");
            }

            ShowText(report.ToString());
            PreviewHeader.Text = Core.Localization.Localizer.Default["gui.menu.compare"];
            StatusText.Text = result.IsIdentical
                ? "Archives are identical."
                : $"{result.Added.Count + result.Removed.Count + result.Changed.Count} difference(s).";
        }
        catch (Exception ex)
        {
            ShowError("Compare failed", ex);
        }
    });

    private static void AppendDiff(System.Text.StringBuilder report, Core.Names.NameRegistrySet names, string title, List<Core.Diff.DiffEntry> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        report.AppendLine($"{title} ({entries.Count}):");
        foreach (Core.Diff.DiffEntry entry in entries.Take(200))
        {
            report.AppendLine($"  {names.GetRelativePath(entry.Key.TypeId, entry.Key.GroupId, entry.Key.InstanceId)}");
        }

        if (entries.Count > 200)
        {
            report.AppendLine($"  … {entries.Count - 200} more");
        }

        report.AppendLine();
    }

    // ---- export all models ----

    private void OnExportAllModelsObj(object sender, RoutedEventArgs e) => Guarded(() => ExportAllModels("obj"));

    private void OnExportAllModelsOgre(object sender, RoutedEventArgs e) => Guarded(() => ExportAllModels("ogre"));

    /// <summary>Exports every decodable RW4 model from the current package (OBJ or Ogre XML).</summary>
    private void ExportAllModels(string format)
    {
        if (_session is null)
        {
            MessageBox.Show(this, Core.Localization.Localizer.Default["gui.msg.openFirst"], "SporeMaster 3");
            return;
        }

        var dialog = new OpenFolderDialog { Title = Core.Localization.Localizer.Default["gui.menu.exportModels"] };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        int exported = 0;
        int skipped = 0;
        int failed = 0;

        foreach (ResourceItem row in _items.Where(r => r.TypeId == ResourceTypeIds.Rw4))
        {
            try
            {
                byte[] data = _session.Read(row.Entry);
                using Core.RenderWare4.RenderWare4File rw4 = Core.RenderWare4.RenderWare4File.Read(new MemoryStream(data));
                IReadOnlyList<Core.RenderWare4.Rw4MeshGeometry> geometry = rw4.ReadGeometry();
                if (geometry.Count == 0)
                {
                    skipped++;
                    continue;
                }

                string stem = row.Path.Replace('/', '_');
                string modelDir = Path.Combine(dialog.FolderName, stem);
                Directory.CreateDirectory(modelDir);

                var meshToMaterial = new Dictionary<int, int>();
                foreach (Core.RenderWare4.Rw4MaterialLink link in rw4.ReadMaterialLinks())
                {
                    if (link.MaterialSectionIndices.Count > 0)
                    {
                        meshToMaterial[link.MeshSectionIndex] = link.MaterialSectionIndices[0];
                    }
                }

                var materials = rw4.ReadMaterials();
                var rasters = rw4.ReadRasters();
                string texturesDir = Path.Combine(modelDir, stem + "_textures");
                Directory.CreateDirectory(texturesDir);

                var materialList = new List<(string Name, string? DiffuseTexture)>();
                var materialNameBySection = new Dictionary<int, string>();
                var texturesWritten = new Dictionary<int, string>();
                for (int i = 0; i < rasters.Count; i++)
                {
                    byte[]? pixels = rw4.GetRasterData(rasters[i]);
                    if (pixels is null || pixels.Length == 0)
                    {
                        continue;
                    }

                    string file = $"{stem}_tex{i}.dds";
                    using (FileStream ddsOut = File.Create(Path.Combine(texturesDir, file)))
                    {
                        rasters[i].ToDds(pixels).Write(ddsOut);
                    }

                    texturesWritten[i] = file;
                }

                for (int i = 0; i < materials.Count; i++)
                {
                    string name = $"material_{i}";
                    materialNameBySection[materials[i].SectionIndex] = name;

                    string? diffuse = null;
                    if (materials[i].TextureSlots.Count > 0)
                    {
                        int rasterSection = materials[i].TextureSlots[0].RasterSectionIndex;
                        for (int r = 0; r < rasters.Count; r++)
                        {
                            if (rasters[r].SectionIndex == rasterSection && texturesWritten.TryGetValue(r, out string? file))
                            {
                                diffuse = Path.GetRelativePath(modelDir, Path.Combine(texturesDir, file)).Replace('\\', '/');
                            }
                        }
                    }

                    materialList.Add((name, diffuse));
                }

                Func<Core.RenderWare4.Rw4MeshGeometry, string?> pick = mesh =>
                    meshToMaterial.TryGetValue(mesh.MeshSectionIndex, out int state)
                        && materialNameBySection.TryGetValue(state, out string? name)
                        ? name
                        : null;

                if (format == "ogre")
                {
                    Core.RenderWare4.Rw4OgreExporter.Export(
                        Path.Combine(modelDir, stem + ".mesh.xml"), geometry, geometry, pick, materialList);
                }
                else
                {
                    Core.RenderWare4.Rw4ObjExporter.Export(
                        geometry, Path.Combine(modelDir, stem + ".obj"), stem + ".mtl", pick, materialList);
                }

                exported++;
            }
            catch (Exception ex)
            {
                App.LogCrash($"export model {row.Path}", ex);
                failed++;
            }
        }

        StatusText.Text = $"Exported {exported} model(s) to {dialog.FolderName}" + (failed > 0 ? $" ({failed} failed)" : string.Empty);
        MessageBox.Show(
            this,
            Core.Localization.Localizer.Default.Get("gui.msg.modelsExported", exported, skipped),
            Core.Localization.Localizer.Default["gui.menu.exportModels"],
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

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
        // Explicitly opening a package (dialog/drag-drop/command line) starts a fresh top-level session.
        _packageStack.Clear();
        LoadPackage(path);
    }

    /// <summary>
    /// Opens a package that lives inside the current one (e.g. a nested <c>.package</c>); the current
    /// package is remembered so it can be left again.
    /// </summary>
    private void OpenNestedPackage(string path)
    {
        if (_session is not null && !string.Equals(_session.FilePath, path, StringComparison.OrdinalIgnoreCase))
        {
            _packageStack.Add(_session.FilePath);
        }

        LoadPackage(path);
    }

    /// <summary>Opens the previously opened package (leaving a nested one).</summary>
    private void OpenPreviousPackage() => Guarded(() =>
    {
        if (_packageStack.Count == 0)
        {
            return;
        }

        string previous = _packageStack[^1];
        _packageStack.RemoveAt(_packageStack.Count - 1);
        LoadPackage(previous);
    });

    private void LoadPackage(string path)
    {
        try
        {
            // Open the new package first; only then dispose the old one (so a failure keeps the old one).
            PackageSession session = PackageSession.Open(path);
            _session?.Dispose();
            _session = session;
            _items = _session.BuildItems().ToList();
            RebuildTree();
            Title = $"SporeMaster 3 — {Path.GetFileName(path)}";

            _history.Clear();
            _historyIndex = -1;
            Navigate(_root, record: true, resetHistory: true);

            StatusText.Text = Core.Localization.Localizer.Default.Get("gui.status.resources", _session.Count, _session.Header?.Version);
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
        UpdateRootCheckState();
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
        // If this package was opened from inside another, "Close" returns to the parent.
        if (_packageStack.Count > 0)
        {
            OpenPreviousPackage();
            return;
        }

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
        PreviewHeader.Text = Core.Localization.Localizer.Default["gui.preview"];
        Breadcrumb.Text = string.Empty;
        Title = "SporeMaster 3";
        StatusText.Text = Core.Localization.Localizer.Default["gui.status.open"];
        CountText.Text = string.Empty;
    });

    /// <summary>Extracts a nested <c>.package</c> resource to a temp file and opens it.</summary>
    private void OnOpenResourceAsPackage(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (_session is null || ResourceList.SelectedItem is not ResourceItem row)
        {
            return;
        }

        byte[] data = _session.Read(row.Entry);
        string temp = Path.Combine(Path.GetTempPath(), "SporeMaster-nested", Path.GetFileName(row.ShortName));
        Directory.CreateDirectory(Path.GetDirectoryName(temp)!);
        File.WriteAllBytes(temp, data);

        // OpenPackage records the current package, so Up/Close/Back can return to it.
        OpenNestedPackage(temp);
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
        else if (_current is { IsRoot: true } && _packageStack.Count > 0)
        {
            // At the top of a nested package: go back to the parent package.
            OpenPreviousPackage();
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
            Breadcrumb.Text = Core.Localization.Localizer.Default.Get("gui.crumb.flat", package, _items.Count);
            SetAddressText(AllResourcesLabel);
        }
        else if (_current is null || _current.IsRoot)
        {
            Breadcrumb.Text = Core.Localization.Localizer.Default.Get("gui.crumb.all", package, _items.Count);
            SetAddressText(AllResourcesLabel);
        }
        else
        {
            Breadcrumb.Text = Core.Localization.Localizer.Default.Get("gui.crumb.folder", package, _current.Name, _current.Items.Count);
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

    private void OnViewDetails(object sender, RoutedEventArgs e) => Guarded(ApplyDetailsView);

    private void OnViewLargeIcons(object sender, RoutedEventArgs e) => Guarded(ApplyLargeIconsView);

    private void ApplyDetailsView()
    {
        ResourceList.View = BuildDetailsView();
        DetailsMenuItem.IsChecked = true;
        LargeIconsMenuItem.IsChecked = false;
    }

    private void ApplyLargeIconsView()
    {
        var view = new GridView();
        view.Columns.Add(new GridViewColumn { Header = "Name", DisplayMemberBinding = new System.Windows.Data.Binding("LargeName"), Width = 300 });
        ResourceList.View = view;
        LargeIconsMenuItem.IsChecked = true;
        DetailsMenuItem.IsChecked = false;
    }

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
        int selected = ResourceList.SelectedItems.Count;

        if (selected > 1)
        {
            // Explorer-style aggregate info for a multiple selection; preview the focused item.
            UpdateDetailsPaneForSelection();
            if (ResourceList.SelectedItem is ResourceItem current)
            {
                ShowPreview(current);
            }
        }
        else if (ResourceList.SelectedItem is ResourceItem row)
        {
            ShowPreview(row);
            UpdateDetailsPane(row);
        }
        else
        {
            DetailsPane.Visibility = Visibility.Collapsed;
        }
    });

    /// <summary>Shows aggregate information (count and sizes) for a multiple selection.</summary>
    private void UpdateDetailsPaneForSelection()
    {
        var rows = ResourceList.SelectedItems.Cast<ResourceItem>().ToList();
        long packed = rows.Sum(r => (long)r.PackedSize);
        long mem = rows.Sum(r => (long)r.MemSize);
        DetailsName.Text = $"📦  {rows.Count} items selected";
        DetailsType.Text = string.Empty;
        DetailsKey.Text = string.Empty;
        DetailsSize.Text = $"{packed} bytes packed, {mem} bytes in memory";
        ApplyDetailsPaneVisibility();
    }

    private void OnSelectAll(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        ResourceList.SelectAll();
    });

    private void OnInvertSelection(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        var selected = ResourceList.SelectedItems.Cast<ResourceItem>().ToHashSet();
        ResourceList.SelectedItems.Clear();
        if (ResourceList.ItemsSource is System.Collections.IEnumerable items)
        {
            foreach (object item in items)
            {
                if (item is ResourceItem row && !selected.Contains(row))
                {
                    ResourceList.SelectedItems.Add(row);
                }
            }
        }
    });

    /// <summary>Fills the Explorer-style details pane for the selected resource.</summary>
    private void UpdateDetailsPane(ResourceItem row)
    {
        DetailsName.Text = $"{row.Glyph}  {row.ShortName}";
        DetailsType.Text = row.TypeName;
        DetailsKey.Text = row.Key;
        DetailsSize.Text = $"{row.PackedSize} bytes packed, {row.MemSize} bytes in memory ({row.Compression}) — {row.Path}";
        ApplyDetailsPaneVisibility();
    }

    private void OnToggleDetailsPane(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        // The MenuItem's own IsChecked has already toggled; treat it as the single source of truth.
        _detailsPaneVisible = DetailsPaneMenuItem.IsChecked;
        ApplyDetailsPaneVisibility();

        if (_detailsPaneVisible && ResourceList.SelectedItem is ResourceItem row)
        {
            UpdateDetailsPane(row);
        }
    });

    private void ApplyDetailsPaneVisibility() =>
        DetailsPane.Visibility = _detailsPaneVisible ? Visibility.Visible : Visibility.Collapsed;

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

        if (preview.IsModel && preview.Model!.Meshes.Count > 0)
        {
            try
            {
                ShowModel(preview.Model);
            }
            catch (Exception ex)
            {
                App.LogCrash("3D preview", ex);
                ShowText((preview.Text ?? string.Empty) + "\n\n(3D preview failed: " + ex.Message + ")");
            }

            return;
        }

        if (preview.IsImage && TryDecodeImage(preview.PngBytes!, out BitmapSource? bitmap))
        {
            PreviewImage.Source = bitmap;
            PreviewImageScroller.Visibility = Visibility.Visible;
            PreviewText.Visibility = Visibility.Collapsed;
            PreviewModelHost.Visibility = Visibility.Collapsed;
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

    private void ShowText(string text)
    {
        PreviewText.Text = text;
        PreviewText.Visibility = Visibility.Visible;
        PreviewImageScroller.Visibility = Visibility.Collapsed;
        PreviewModelHost.Visibility = Visibility.Collapsed;
    }

    // ---- 3D model preview ----

    private PerspectiveCamera? _modelCamera;
    private PreviewModel? _currentModel;
    private bool _showSkeleton;
    private Point3D _modelCenter;
    private double _modelRadius = 1;
    private double _modelAzimuth = 0.6;
    private double _modelElevation = 0.4;
    private double _modelDistance = 3;
    private bool _modelDragging;
    private Point _modelDragLast;

    /// <summary>Builds a WPF 3D scene from the preview model and shows it in the preview pane.</summary>
    private void ShowModel(PreviewModel model)
    {
        _currentModel = model;
        var group = new Model3DGroup();

        // Pre-decode the model-wide texture; individual meshes may override it with their own.
        ImageBrush? modelBrush = null;
        if (model.TexturePng is not null && TryDecodeImage(model.TexturePng, out BitmapSource? modelImage) && modelImage is not null)
        {
            modelBrush = new ImageBrush(modelImage) { Stretch = Stretch.Fill, Opacity = _showSkeleton ? 0.3 : 1.0 };
        }

        var greyMaterial = new MaterialGroup();
        var greyBrush = new SolidColorBrush(Color.FromRgb(0xC8, 0xC8, 0xC8)) { Opacity = _showSkeleton ? 0.3 : 1.0 };
        greyMaterial.Children.Add(new DiffuseMaterial(greyBrush));
        greyMaterial.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)), 20));

        foreach (PreviewMesh mesh in model.Meshes)
        {
            var geometry = new MeshGeometry3D
            {
                Positions = new Point3DCollection(mesh.VertexCount),
                TriangleIndices = new Int32Collection(mesh.Indices),
            };

            for (int i = 0; i + 2 < mesh.Positions.Length; i += 3)
            {
                geometry.Positions.Add(new Point3D(mesh.Positions[i], mesh.Positions[i + 1], mesh.Positions[i + 2]));
            }

            if (mesh.HasNormals)
            {
                geometry.Normals = new Vector3DCollection(mesh.VertexCount);
                for (int i = 0; i + 2 < mesh.Normals.Length; i += 3)
                {
                    geometry.Normals.Add(new Vector3D(mesh.Normals[i], mesh.Normals[i + 1], mesh.Normals[i + 2]));
                }
            }

            // Prefer this mesh's own texture, then the model-wide one.
            ImageBrush? brush = modelBrush;
            if (mesh.TexturePng is not null && TryDecodeImage(mesh.TexturePng, out BitmapSource? meshImage) && meshImage is not null)
            {
                brush = new ImageBrush(meshImage) { Stretch = Stretch.Fill };
            }

            Material material = greyMaterial;
            if (brush is not null && mesh.HasTexCoords)
            {
                geometry.TextureCoordinates = new PointCollection(mesh.VertexCount);
                for (int i = 0; i + 1 < mesh.TexCoords.Length; i += 2)
                {
                    // Flip V so the texture is not upside-down (Spore uses top-left origin).
                    geometry.TextureCoordinates.Add(new Point(mesh.TexCoords[i], 1 - mesh.TexCoords[i + 1]));
                }

                brush.Opacity = _showSkeleton ? 0.3 : 1.0;
                var textured = new MaterialGroup();
                textured.Children.Add(new DiffuseMaterial(brush));
                textured.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)), 20));
                material = textured;
            }

            group.Children.Add(new GeometryModel3D
            {
                Geometry = geometry,
                Material = material,
                BackMaterial = material,
            });
        }

        (float[] min, float[] max) = model.Bounds();
        _modelCenter = new Point3D((min[0] + max[0]) / 2.0, (min[1] + max[1]) / 2.0, (min[2] + max[2]) / 2.0);
        double dx = max[0] - min[0];
        double dy = max[1] - min[1];
        double dz = max[2] - min[2];
        _modelRadius = Math.Max(0.001, Math.Max(dx, Math.Max(dy, dz)) * 0.5);
        _modelDistance = _modelRadius * 3;

                group.Children.Add(new AmbientLight(Color.FromRgb(0x60, 0x60, 0x60)));
                group.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-1, -1.5, -1)));
                group.Children.Add(new DirectionalLight(Color.FromRgb(0x90, 0x90, 0x90), new Vector3D(1, 0.5, 1)));

                if (_showSkeleton && model.SkeletonJoints.Count > 0)
                {
                    group.Children.Add(BuildSkeleton(model, _modelRadius));
                }

        _modelCamera = new PerspectiveCamera { FieldOfView = 45 };
        PreviewViewport.Children.Clear();
        PreviewViewport.Children.Add(new ModelVisual3D { Content = group });
        PreviewViewport.Camera = _modelCamera;
        UpdateModelCamera();

        PreviewModelHost.Visibility = Visibility.Visible;
        PreviewText.Visibility = Visibility.Collapsed;
        PreviewImageScroller.Visibility = Visibility.Collapsed;
        PreviewImage.Source = null;
    }

    private void UpdateModelCamera()
    {
        if (_modelCamera is null)
        {
            return;
        }

        double x = Math.Cos(_modelElevation) * Math.Sin(_modelAzimuth);
        double y = Math.Sin(_modelElevation);
        double z = Math.Cos(_modelElevation) * Math.Cos(_modelAzimuth);
        _modelCamera.Position = new Point3D(
            _modelCenter.X + x * _modelDistance,
            _modelCenter.Y + y * _modelDistance,
            _modelCenter.Z + z * _modelDistance);
        _modelCamera.LookDirection = new Vector3D(
            _modelCenter.X - _modelCamera.Position.X,
            _modelCenter.Y - _modelCamera.Position.Y,
            _modelCenter.Z - _modelCamera.Position.Z);
        _modelCamera.UpDirection = new Vector3D(0, 1, 0);
        _modelCamera.NearPlaneDistance = _modelDistance * 0.01;
        _modelCamera.FarPlaneDistance = _modelDistance * 10;
    }

    private void OnModelMouseDown(object sender, MouseButtonEventArgs e)
    {
        _modelDragging = true;
        _modelDragLast = e.GetPosition(PreviewModelHost);
        PreviewModelHost.CaptureMouse();
    }

    private void OnModelMouseUp(object sender, MouseButtonEventArgs e)
    {
        _modelDragging = false;
        PreviewModelHost.ReleaseMouseCapture();
    }

    private void OnModelMouseMove(object sender, MouseEventArgs e) => Guarded(() =>
    {
        if (!_modelDragging)
        {
            return;
        }

        Point position = e.GetPosition(PreviewModelHost);
        _modelAzimuth -= (position.X - _modelDragLast.X) * 0.01;
        _modelElevation = Math.Clamp(_modelElevation + (position.Y - _modelDragLast.Y) * 0.01, -1.5, 1.5);
        _modelDragLast = position;
        UpdateModelCamera();
    });

    private void OnModelMouseWheel(object sender, MouseWheelEventArgs e) => Guarded(() =>
    {
        _modelDistance = Math.Clamp(_modelDistance * (e.Delta > 0 ? 0.9 : 1.1), _modelRadius * 0.2, _modelRadius * 20);
        UpdateModelCamera();
    });

    // ---- skeleton overlay ----

    private static MeshGeometry3D? _boxMesh;

    private void OnToggleSkeleton(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        _showSkeleton = ShowSkeletonMenuItem.IsChecked;
        if (_currentModel is not null)
        {
            ShowModel(_currentModel);
        }
    });

    /// <summary>Builds cube markers at each bone and thin boxes along the bones.</summary>
    private static Model3DGroup BuildSkeleton(PreviewModel model, double radius)
    {
        var group = new Model3DGroup();
        var jointMaterial = new MaterialGroup();
        jointMaterial.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromRgb(0xFF, 0xC0, 0x30))));
        jointMaterial.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0xFF, 0xC0, 0x30))));
        var boneMaterial = new MaterialGroup();
        boneMaterial.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromRgb(0x30, 0xB0, 0xFF))));
        boneMaterial.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0x30, 0xB0, 0xFF))));
        double jointSize = Math.Max(0.001, radius * 0.1);
        double thickness = Math.Max(0.0005, radius * 0.02);

        for (int i = 0; i < model.SkeletonJoints.Count; i++)
        {
            float[] p = model.SkeletonJoints[i];
            var transform = new Matrix3D();
            transform.Scale(new Vector3D(jointSize, jointSize, jointSize));
            transform.Translate(new Vector3D(p[0], p[1], p[2]));
            group.Children.Add(new GeometryModel3D
            {
                Geometry = BoxMesh,
                Transform = new MatrixTransform3D(transform),
                Material = jointMaterial,
                BackMaterial = jointMaterial,
            });
        }

        foreach ((int child, int parent) in model.SkeletonBones)
        {
            if (parent < 0 || child >= model.SkeletonJoints.Count || parent >= model.SkeletonJoints.Count)
            {
                continue;
            }

            float[] a = model.SkeletonJoints[parent];
            float[] b = model.SkeletonJoints[child];
            var va = new Vector3D(a[0], a[1], a[2]);
            var vb = new Vector3D(b[0], b[1], b[2]);
            Vector3D dir = vb - va;
            double length = dir.Length;
            if (length < 1e-6)
            {
                continue;
            }

            dir.Normalize();

            var transform = new Matrix3D();
            transform.Scale(new Vector3D(thickness, length, thickness));
            transform.Rotate(QuaternionFromY(dir));
            var mid = (va + vb) / 2;
            transform.Translate(mid);

            group.Children.Add(new GeometryModel3D
            {
                Geometry = BoxMesh,
                Transform = new MatrixTransform3D(transform),
                Material = boneMaterial,
                BackMaterial = boneMaterial,
            });
        }

        return group;
    }

    /// <summary>Rotation that maps +Y to the given direction.</summary>
    private static Quaternion QuaternionFromY(Vector3D direction)
    {
        var y = new Vector3D(0, 1, 0);
        double dot = Vector3D.DotProduct(y, direction);
        if (dot > 0.999999)
        {
            return Quaternion.Identity;
        }

        if (dot < -0.999999)
        {
            return new Quaternion(new Vector3D(1, 0, 0), 180);
        }

        Vector3D axis = Vector3D.CrossProduct(y, direction);
        axis.Normalize();
        double angle = Math.Acos(Math.Clamp(dot, -1, 1)) * 180 / Math.PI;
        return new Quaternion(axis, angle);
    }

    private static MeshGeometry3D BoxMesh => _boxMesh ??= CreateBox();

    private static MeshGeometry3D CreateBox()
    {
        var mesh = new MeshGeometry3D();
        foreach (double[] corner in new[]
                 {
                     new[] { -0.5, -0.5, -0.5 }, new[] { 0.5, -0.5, -0.5 }, new[] { 0.5, 0.5, -0.5 }, new[] { -0.5, 0.5, -0.5 },
                     new[] { -0.5, -0.5, 0.5 }, new[] { 0.5, -0.5, 0.5 }, new[] { 0.5, 0.5, 0.5 }, new[] { -0.5, 0.5, 0.5 },
                 })
        {
            mesh.Positions.Add(new Point3D(corner[0], corner[1], corner[2]));
        }

        foreach (int face in new[]
                 {
                     0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7, 0, 1, 5, 0, 5, 4,
                     1, 2, 6, 1, 6, 5, 2, 3, 7, 2, 7, 6, 3, 0, 4, 3, 4, 7,
                 })
        {
            mesh.TriangleIndices.Add(face);
        }

        return mesh;
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

    // ---- extraction ----

    private void OnListDoubleClick(object sender, MouseButtonEventArgs e) => Guarded(() =>
    {
        if (ResourceList.SelectedItem is ResourceItem)
        {
            ExtractSelected();
        }
    });

    // ---- drag out (export to Explorer) ----

    private Point _dragStart;
    private bool _draggingOut;

    private void OnListMouseDown(object sender, MouseButtonEventArgs e) =>
        _dragStart = e.GetPosition(null);

    /// <summary>
    /// Explorer-style "drag out": dragging a resource writes it to a temporary file (with the usual
    /// conversions) and starts a file drop, so it can be dropped into Explorer or another app.
    /// </summary>
    private void OnListMouseMove(object sender, MouseEventArgs e) => Guarded(() =>
    {
        if (_draggingOut || e.LeftButton != MouseButtonState.Pressed || _session is null)
        {
            return;
        }

        Point position = e.GetPosition(null);
        if (Math.Abs(position.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(position.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        if (ItemsUnder(e.OriginalSource).FirstOrDefault() is not { } row)
        {
            return;
        }

        _draggingOut = true;
        try
        {
            string temp = ExtractToTemp(row);
            StatusText.Text = $"Dragging {row.ShortName} (extracted to {temp}).";
            DragDrop.DoDragDrop(ResourceList, new DataObject(DataFormats.FileDrop, new[] { temp }), DragDropEffects.Copy);
        }
        finally
        {
            _draggingOut = false;
        }
    });

    /// <summary>Finds the resource under a visual (used by drag-out).</summary>
    private IEnumerable<ResourceItem> ItemsUnder(object originalSource)
    {
        var element = originalSource as System.Windows.Media.Visual;
        while (element is not null)
        {
            if (element is FrameworkElement { DataContext: ResourceItem item })
            {
                yield return item;
                yield break;
            }

            element = System.Windows.Media.VisualTreeHelper.GetParent(element) as System.Windows.Media.Visual;
        }
    }

    /// <summary>Extracts a resource to a uniquely named temporary file (with conversions).</summary>
    private string ExtractToTemp(ResourceItem row)
    {
        string directory = Path.Combine(Path.GetTempPath(), "SporeMaster-dnd");
        Directory.CreateDirectory(directory);

        string name = row.ShortName;
        string suffix = NeedsSuffix(row) ? SuffixFor(row) : string.Empty;
        string target = Path.Combine(directory, $"{row.Key.Replace(':', '_')}_{name}{suffix}");

        byte[] data = _session!.Read(row.Entry);
        WriteConverted(row, data, target);
        return target;
    }

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
        _root is null ? new List<FolderNode>() : _root.Children.Where(c => c.IsChecked == true).ToList();

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

    /// <summary>
    /// Standard tree-view checkbox behaviour: a folder toggles itself; the package (root) node acts as
    /// a tri-state "select all" (checked = all folders, indeterminate = some).
    /// </summary>
    private void OnFolderCheckChanged(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (_syncingChecks || sender is not CheckBox { DataContext: FolderNode node } || _root is null)
        {
            return;
        }

        if (node.IsRoot)
        {
            // Clicking the root selects all when not all are checked, otherwise clears all.
            bool selectAll = _root.Children.Any(c => c.IsChecked != true);
            _syncingChecks = true;
            foreach (FolderNode child in _root.Children)
            {
                child.IsChecked = selectAll;
            }

            _syncingChecks = false;
        }

        UpdateRootCheckState();
        UpdateCheckStatus();
    });

    /// <summary>Recomputes the root checkbox state from its folders (tri-state).</summary>
    private void UpdateRootCheckState()
    {
        if (_root is null || _root.Children.Count == 0)
        {
            return;
        }

        int checkedCount = _root.Children.Count(c => c.IsChecked == true);
        _syncingChecks = true;
        _root.IsChecked = checkedCount == 0
            ? false
            : checkedCount == _root.Children.Count
                ? true
                : null;
        _syncingChecks = false;
    }

    private void UpdateCheckStatus()
    {
        int checkedFolders = CheckedFolders().Count;
        StatusText.Text = checkedFolders > 0
            ? $"{checkedFolders} folder(s) checked ({CheckedItems().Count} resource(s)) — Tools → Extract Checked Folders."
            : "No folders checked.";
    }

    private void OnCheckAllFolders(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        SetAllFoldersChecked(true);
    });

    private void OnUncheckAllFolders(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        SetAllFoldersChecked(false);
    });

    // ---- folder tree context menu ----

    private FolderNode? SelectedFolderNode() => FolderTree.SelectedItem as FolderNode;

    private void OnFolderMenuOpen(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (SelectedFolderNode() is { } node)
        {
            SelectNode(node);
        }
    });

    private void OnFolderMenuCheck(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (SelectedFolderNode() is { IsRoot: false } node)
        {
            node.IsChecked = true;
            UpdateRootCheckState();
            UpdateCheckStatus();
        }
    });

    private void OnFolderMenuUncheck(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (SelectedFolderNode() is { IsRoot: false } node)
        {
            node.IsChecked = false;
            UpdateRootCheckState();
            UpdateCheckStatus();
        }
    });

    private void OnFolderMenuCopyName(object sender, RoutedEventArgs e) => Guarded(() =>
    {
        if (SelectedFolderNode() is { } node)
        {
            string name = node.IsRoot && _session is not null ? Path.GetFileName(_session.FilePath) : node.Name;
            Clipboard.SetText(name);
            StatusText.Text = $"Copied folder name: {name}";
        }
    });

    private void SetAllFoldersChecked(bool value)
    {
        if (_root is null)
        {
            return;
        }

        _syncingChecks = true;
        foreach (FolderNode folder in _root.Children)
        {
            folder.IsChecked = value;
        }

        _syncingChecks = false;

        UpdateRootCheckState();
        UpdateCheckStatus();
    }

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
            Core.Localization.Localizer.Default["gui.about.text"],
            Core.Localization.Localizer.Default["gui.menu.about"].Replace("_", string.Empty),
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

    internal void OpenNestedPackageForTest(string path) => OpenNestedPackage(path);

    internal string? CurrentPackagePathForTest() => _session?.FilePath;

    internal int PackageStackDepthForTest() => _packageStack.Count;

    internal void OpenPreviousPackageForTest() => OpenPreviousPackage();

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

    /// <summary>Shows the preview for a row and captures the preview pane to a PNG (for verification).</summary>
    internal string? CapturePreviewPngForTest(ResourceItem row, string path)
    {
        ShowPreview(row);
        UpdateLayout();

        FrameworkElement target = PreviewModelHost.Visibility == Visibility.Visible
            ? PreviewModelHost
            : PreviewImageScroller.Visibility == Visibility.Visible
                ? PreviewImageScroller
                : PreviewText;

        return CaptureElement(target, path);
    }

    /// <summary>Captures the whole window to a PNG (used by the self-test to verify the layout).</summary>
    internal string? CaptureWindowForTest(string path)
    {
        UpdateLayout();
        return CaptureElement(this, path);
    }

    private static string? CaptureElement(FrameworkElement target, string path)
    {
        if (target.ActualWidth < 1 || target.ActualHeight < 1)
        {
            return null;
        }

        var bitmap = new RenderTargetBitmap(
            (int)target.ActualWidth, (int)target.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(target);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(path);
        encoder.Save(stream);
        return path;
    }

    /// <summary>Extracts a row to a temporary file (used by the drag-out path) for the self-test.</summary>
    internal string ExtractToTempForTest(ResourceItem row) => ExtractToTemp(row);

    internal void CheckFolderForTest(int index, bool isChecked)
    {
        if (_root is not null && index >= 0 && index < _root.Children.Count)
        {
            _root.Children[index].IsChecked = isChecked;
            UpdateRootCheckState();
        }
    }

    internal int CheckedResourceCountForTest() => CheckedItems().Count;

    internal int CheckedFolderCountForTest() => CheckedFolders().Count;

    /// <summary>Root check state: true = all, false = none, null = some (tri-state).</summary>
    internal bool? RootCheckStateForTest() => _root?.IsChecked;

    internal void SetAllFoldersCheckedForTest(bool value) => SetAllFoldersChecked(value);

    internal void ToggleDetailsPaneForTest()
    {
        DetailsPaneMenuItem.IsChecked = !DetailsPaneMenuItem.IsChecked;
        _detailsPaneVisible = DetailsPaneMenuItem.IsChecked;
        ApplyDetailsPaneVisibility();
    }

    internal bool DetailsPaneVisibleForTest() => DetailsPane.Visibility == Visibility.Visible;

    internal void SetSkeletonForTest(bool show)
    {
        ShowSkeletonMenuItem.IsChecked = show;
        _showSkeleton = show;
        if (_currentModel is not null)
        {
            ShowModel(_currentModel);
        }
    }

    internal int SkeletonJointCountForTest(ResourceItem row)
    {
        ResourcePreview preview = PreviewForTest(row);
        return preview.Model?.SkeletonJoints.Count ?? 0;
    }

    /// <summary>Compares the current package with another and returns the report text (for the self-test).</summary>
    internal string CompareWithForTest(string otherPath)
    {
        if (_session is null)
        {
            return string.Empty;
        }

        using PackageSession other = PackageSession.Open(otherPath);
        Core.Diff.ArchiveDiffResult result = _session.Compare(other);
        return $"added={result.Added.Count} removed={result.Removed.Count} changed={result.Changed.Count} same={result.Identical}";
    }

    internal (bool Details, bool LargeIcons) ViewMenuStateForTest() => (DetailsMenuItem.IsChecked, LargeIconsMenuItem.IsChecked);

    internal void ApplyDetailsViewForTest() => ApplyDetailsView();

    internal void ApplyLargeIconsViewForTest() => ApplyLargeIconsView();

    internal int SelectAllForTest()
    {
        ResourceList.SelectAll();
        return ResourceList.SelectedItems.Count;
    }

    internal void ClearSelectionForTest() => ResourceList.SelectedItems.Clear();

    internal string DetailsTextForTest() => $"{DetailsName.Text} | {DetailsSize.Text}";

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
