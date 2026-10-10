using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Presentation;
using CoreBitmapImage = SporeMaster.Core.Bitmaps.BitmapImage;

namespace SporeMaster.Gui;

/// <summary>The main application window: a resource explorer for Spore packages.</summary>
public partial class MainWindow : Window
{
    private PackageSession? _session;
    private List<ResourceItem> _rows = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => TryOpenFromCommandLine();
    }

    // ---- opening ----

    private void OnOpenPackage(object sender, RoutedEventArgs e)
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
        try
        {
            _session?.Dispose();
            _session = PackageSession.Open(path);
            _rows = _session.BuildItems().ToList();
            Title = $"SporeMaster 3 — {Path.GetFileName(path)}";
            ApplyFilter();
            StatusText.Text = $"{_session.Count} resource(s) — DBPF {_session.Header?.Version}";
        }
        catch (NotAPackageException ex)
        {
            MessageBox.Show(this, ex.Message, "Not a package", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Failed to open package", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnClosePackage(object sender, RoutedEventArgs e)
    {
        _session?.Dispose();
        _session = null;
        _rows = new List<ResourceItem>();
        ResourceList.ItemsSource = null;
        PreviewText.Text = string.Empty;
        PreviewImage.Source = null;
        PreviewHeader.Text = "Preview";
        Title = "SporeMaster 3";
        StatusText.Text = "Open a .package file to begin.";
        CountText.Text = string.Empty;
    }

    private void OnExit(object sender, RoutedEventArgs e) => Close();

    // ---- filtering ----

    private void OnFilterChanged(object sender, RoutedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        string filter = FilterBox?.Text ?? string.Empty;
        List<ResourceItem> filtered = _rows.Where(r => r.Matches(filter)).ToList();
        ResourceList.ItemsSource = filtered;
        CountText.Text = $"{filtered.Count} / {_rows.Count}";
    }

    // ---- preview ----

    private void OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (_session is null || ResourceList.SelectedItem is not ResourceItem row)
        {
            return;
        }

        PreviewHeader.Text = $"{row.Path}   [{row.Key}]";

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

        if (preview.IsImage)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = new MemoryStream(preview.PngBytes!);
            bitmap.EndInit();
            bitmap.Freeze();

            PreviewImage.Source = bitmap;
            PreviewImageScroller.Visibility = Visibility.Visible;
            PreviewText.Visibility = Visibility.Collapsed;
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
    }

    // ---- extraction ----

    private void OnExtractSelected(object sender, RoutedEventArgs e)
    {
        if (_session is null || ResourceList.SelectedItem is not ResourceItem row)
        {
            MessageBox.Show(this, "Select a resource first.", "Nothing selected");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Extract resource",
            FileName = Path.GetFileName(row.Path),
            Filter = "All files (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            byte[] data = _session.Read(row.Entry);
            WriteConverted(row, data, dialog.FileName);
            StatusText.Text = $"Extracted {row.Path} ({data.Length} bytes).";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Extraction failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnExtractAll(object sender, RoutedEventArgs e)
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

        string outputDir = dialog.FolderName;
        int written = 0;
        int failed = 0;

        foreach (ResourceItem row in _rows)
        {
            try
            {
                byte[] data = _session.Read(row.Entry);
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

        StatusText.Text = $"Extracted {written} resource(s) to {outputDir}" + (failed > 0 ? $" ({failed} failed)" : string.Empty);
        MessageBox.Show(this, $"Extracted {written} resource(s)." + (failed > 0 ? $"\n{failed} failed (see status bar)." : string.Empty),
            "Extract all", MessageBoxButton.OK, MessageBoxImage.Information);
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

    private void OnLoadNames(object sender, RoutedEventArgs e)
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

        try
        {
            _session.LoadNames(dialog.FileName);
            _rows = _session.BuildItems().ToList();
            ApplyFilter();
            StatusText.Text = $"Loaded names from {Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Failed to load names", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCopyKey(object sender, RoutedEventArgs e)
    {
        if (ResourceList.SelectedItem is ResourceItem row)
        {
            Clipboard.SetText(row.Key);
            StatusText.Text = $"Copied {row.Key}";
        }
    }

    // ---- test hooks (used by --selftest; no UI shown) ----

    internal int RowCount => _rows.Count;

    internal int ResourceCount => _session?.Count ?? 0;

    internal void OpenPackageForTest(string path) => OpenPackage(path);

    internal IReadOnlyList<ResourceItem> RowsForTest() => _rows;

    internal ResourcePreview PreviewForTest(ResourceItem row)
    {
        if (_session is null)
        {
            return ResourcePreview.Empty();
        }

        byte[] data = _session.Read(row.Entry);
        return PreviewEngine.Build(row, data, _session.Names);
    }

    private void OnAbout(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            this,
            "SporeMaster 3\n\nA .NET 9 toolkit for exploring and editing Spore .package (DBPF) files.\n" +
            "Supports DBPF/DBBF, properties, locales, textures, bitmaps, RenderWare 4 and hash guessing.",
            "About SporeMaster 3",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
