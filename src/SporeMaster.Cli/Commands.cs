using System.Globalization;
using System.Text;
using SporeMaster.Core.Bitmaps;
using SporeMaster.Core.ArgScript;
using SporeMaster.Core.Anim;
using SporeMaster.Core.Binary;
using SporeMaster.Core.Cell;
using SporeMaster.Core.Cnv;
using SporeMaster.Core.Compression;
using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Diff;
using SporeMaster.Core.Gait;
using SporeMaster.Core.Hashing;
using SporeMaster.Core.Imaging;
using SporeMaster.Core.Locale;
using SporeMaster.Core.Localization;
using SporeMaster.Core.Names;
using SporeMaster.Core.Pctp;
using SporeMaster.Core.Pdb;
using SporeMaster.Core.Presentation;
using SporeMaster.Core.Properties;
using SporeMaster.Core.RenderWare4;
using SporeMaster.Core.Search;
using SporeMaster.Core.Textures;
using SporeMaster.Core.Tlsa;

namespace SporeMaster.Cli;

/// <summary>Implements the individual CLI commands. Every command returns a process exit code.</summary>
public static class Commands
{
    public static int Info(CommandLine cli, Output output)
    {
        if (!TryOpen(cli, output, out DatabasePackedFile? package))
        {
            return 1;
        }

        using (package)
        {
            string path = cli.Positional[0];
            var info = new FileInfo(path);
            Localizer loc = Localizer.Default;

            output.Heading(loc["app.name"]);
            output.Line($"{loc["header.file"]}: {path}");
            output.Line($"Size: {Output.FormatSize(info.Length)} ({info.Length} bytes)");
            output.Line($"Container: {DescribeContainer(package!)}");
            output.Line($"{loc["header.version"]}: {package!.Version}");
            DbpfHeader header = package.Header!;
            output.Line($"{loc["header.indexOffset"]}: 0x{header.IndexOffset:X}");
            output.Line($"{loc["header.indexSize"]}: {Output.FormatSize(header.IndexSize)}");
            output.Line($"{loc["header.indexType"]}: 0x{package.IndexType:X2}");
            output.Line($"{loc["header.entries"]}: {package.Entries.Count}");

            int compressed = package.Entries.Count(e => e.IsCompressed);
            output.Line($"Compressed: {compressed} / {package.Entries.Count}");

            PrintDiagnostics(package, output);
        }

        return 0;
    }

    public static int List(CommandLine cli, Output output)
    {
        if (!TryOpen(cli, output, out DatabasePackedFile? package))
        {
            return 1;
        }

        NameRegistrySet names = OpenNames(cli);
        uint? typeFilter = ParseHashFilter(cli.Get("type"), names.GetTypeHash, output);
        uint? groupFilter = ParseHashFilter(cli.Get("group"), names.GetFileHash, output);
        NameFilter nameFilter = NameFilter.Compile(cli.Get("name"));
        ContentQuery contentFilter = ContentQuery.Parse(cli.Get("content"));
        bool onlyCompressed = cli.Has("compressed");
        int limit = cli.GetInt("limit") ?? int.MaxValue;

        using (package)
        {
            var rows = new List<string[]>();
            var filtered = new List<DbpfEntry>();
            foreach (DbpfEntry entry in package!.Entries)
            {
                if (typeFilter is not null && entry.Key.TypeId != typeFilter)
                {
                    continue;
                }

                if (groupFilter is not null && entry.Key.GroupId != groupFilter)
                {
                    continue;
                }

                if (onlyCompressed && !entry.IsCompressed)
                {
                    continue;
                }

                if (!nameFilter.IsEmpty)
                {
                    ResourceItem probe = new(entry, names);
                    if (!probe.Matches(nameFilter))
                    {
                        continue;
                    }
                }

                if (contentFilter.HasContent)
                {
                    try
                    {
                        if (!contentFilter.Matches(package.ReadEntry(entry)))
                        {
                            continue;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                string resolved = names.GetRelativePath(entry.Key.TypeId, entry.Key.GroupId, entry.Key.InstanceId);

                filtered.Add(entry);
                rows.Add(new[]
                {
                    entry.Index.ToString(CultureInfo.InvariantCulture),
                    $"0x{entry.Key.TypeId:X8}",
                    $"0x{entry.Key.GroupId:X8}",
                    $"0x{entry.Key.InstanceId:X8}",
                    entry.IsCompressed ? "R" : "-",
                    entry.PackedSize.ToString(CultureInfo.InvariantCulture),
                    entry.MemSize.ToString(CultureInfo.InvariantCulture),
                    resolved,
                });

                if (rows.Count >= limit)
                {
                    break;
                }
            }

            string format = cli.Get("format") ?? string.Empty;
            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                string csvPath = cli.Get("out") ?? "resources.csv";
                WriteCsv(csvPath, filtered, names);
                output.Success($"Wrote {filtered.Count} row(s) to {csvPath}");
            }
            else if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                string jsonPath = cli.Get("out") ?? "resources.json";
                File.WriteAllText(jsonPath, BuildJson(filtered, names));
                output.Success($"Wrote {filtered.Count} record(s) to {jsonPath}");
            }
            else
            {
                output.Table(
                    new[] { "#", "Type", "Group", "Instance", "C", "Packed", "Mem", "Path" },
                    rows);
                output.Line();
                output.Line($"{rows.Count} resource(s) shown.");
            }

            PrintDiagnostics(package!, output);

            // --save-names <file>: write a reg_file-style registry of the names used by this package.
            string? saveNames = cli.Get("save-names");
            if (!string.IsNullOrEmpty(saveNames))
            {
                string registry = names.BuildNameRegistry(
                    package!.Entries.Select(e => (e.Key.TypeId, e.Key.GroupId, e.Key.InstanceId)));
                File.WriteAllText(saveNames, registry, new UTF8Encoding(false));
                output.Success($"Wrote name registry to {saveNames}");
            }
        }

        return 0;
    }

    private static void WriteCsv(string path, IReadOnlyList<DbpfEntry> entries, NameRegistrySet names)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        writer.WriteLine("index,type,group,instance,compressed,packedSize,memSize,path");
        foreach (DbpfEntry entry in entries)
        {
            string resolved = names.GetRelativePath(entry.Key.TypeId, entry.Key.GroupId, entry.Key.InstanceId);
            writer.WriteLine(string.Join(',',
                entry.Index.ToString(CultureInfo.InvariantCulture),
                $"0x{entry.Key.TypeId:X8}",
                $"0x{entry.Key.GroupId:X8}",
                $"0x{entry.Key.InstanceId:X8}",
                entry.IsCompressed ? "1" : "0",
                entry.PackedSize.ToString(CultureInfo.InvariantCulture),
                entry.MemSize.ToString(CultureInfo.InvariantCulture),
                CsvEscape(resolved)));
        }
    }

    private static string CsvEscape(string value) =>
        value.Contains(',') || value.Contains('"')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;

    private static string BuildJson(IReadOnlyList<DbpfEntry> entries, NameRegistrySet names)
    {
        var builder = new StringBuilder();
        builder.Append("[\n");
        for (int i = 0; i < entries.Count; i++)
        {
            DbpfEntry entry = entries[i];
            string resolved = names.GetRelativePath(entry.Key.TypeId, entry.Key.GroupId, entry.Key.InstanceId);
            builder.Append("  {")
                .Append("\"index\":").Append(entry.Index).Append(',')
                .Append("\"type\":\"").Append($"0x{entry.Key.TypeId:X8}").Append("\",")
                .Append("\"group\":\"").Append($"0x{entry.Key.GroupId:X8}").Append("\",")
                .Append("\"instance\":\"").Append($"0x{entry.Key.InstanceId:X8}").Append("\",")
                .Append("\"compressed\":").Append(entry.IsCompressed ? "true" : "false").Append(',')
                .Append("\"packedSize\":").Append(entry.PackedSize).Append(',')
                .Append("\"memSize\":").Append(entry.MemSize).Append(',')
                .Append("\"path\":\"").Append(JsonEscape(resolved)).Append('"')
                .Append('}');
            if (i < entries.Count - 1)
            {
                builder.Append(',');
            }

            builder.Append('\n');
        }

        builder.Append("]\n");
        return builder.ToString();
    }

    private static string JsonEscape(string value)
    {
        var builder = new StringBuilder(value.Length + 8);
        foreach (char c in value)
        {
            switch (c)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                default:
                    if (c < 0x20)
                    {
                        builder.Append("\\u").Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        builder.Append(c);
                    }

                    break;
            }
        }

        return builder.ToString();
    }

    public static int Extract(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "extract <package> <type:group:instance> [--out FILE]");
        }

        if (!ResourceKey.TryParse(cli.Positional[1], out ResourceKey key))
        {
            output.Error($"'{cli.Positional[1]}' is not a valid resource key (type:group:instance).");
            return 1;
        }

        if (!TryOpen(cli, output, out DatabasePackedFile? package))
        {
            return 1;
        }

        using (package)
        {
            DbpfEntry? entry = package!.Entries.FirstOrDefault(e => e.Key == key);
            if (entry is null)
            {
                output.Error($"Resource {key} was not found in the package.");
                return 1;
            }

            NameRegistrySet names = OpenNames(cli);
            string? explicitOut = cli.Get("out");
            string outPath = explicitOut ?? SanitizePath(names.GetRelativePath(key.TypeId, key.GroupId, key.InstanceId));

            byte[] data = package.ReadEntry(entry);
            string? directory = Path.GetDirectoryName(Path.GetFullPath(outPath));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(outPath, data);
            output.Success(Localizer.Default.Get("result.extracted", outPath, data.Length));
            PrintDiagnostics(package, output);
        }

        return 0;
    }

    public static int Unpack(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "unpack <package> <output-dir> [--raw] [--clean-autolocales] [--group A,B] [--type T] [--name S]");
        }

        if (!TryOpen(cli, output, out DatabasePackedFile? package))
        {
            return 1;
        }

        string outputDir = Path.GetFullPath(cli.Positional[1]);
        bool raw = cli.Has("raw");
        bool cleanAutoLocales = cli.Has("clean-autolocales");
        NameRegistrySet names = OpenNames(cli);
        Directory.CreateDirectory(outputDir);

        // Selective extraction: choose one or more folders (groups), types and/or a name substring.
        ResourceSelector selector = ResourceSelector.Parse(cli.Get("type"), cli.Get("group"), cli.Get("name"), names);

        // Identify auto-generated locales (by content) so they can be rebuilt on packing.
        var autoLocaleIds = new HashSet<uint>();
        if (!raw)
        {
            foreach (DbpfEntry entry in package!.Entries)
            {
                if (entry.Key.TypeId != ResourceTypeIds.Locale)
                {
                    continue;
                }

                try
                {
                    string text = Encoding.UTF8.GetString(package.ReadEntry(entry));
                    if (AutoLocale.IsAutoLocaleName(names.GetFileName(entry.Key.InstanceId)) ||
                        AutoLocale.IsAutoLocaleContent(text))
                    {
                        autoLocaleIds.Add(entry.Key.InstanceId);
                    }
                }
                catch
                {
                    // ignore unreadable locales
                }
            }
        }

        int written = 0;
        int skipped = 0;
        int filtered = 0;
        using (package)
        {
            foreach (DbpfEntry entry in package!.Entries)
            {
                string resolved = names.GetRelativePath(entry.Key.TypeId, entry.Key.GroupId, entry.Key.InstanceId);
                if (!selector.IsEmpty && !selector.Matches(entry.Key.TypeId, entry.Key.GroupId, resolved))
                {
                    filtered++;
                    continue;
                }

                byte[] data = package.ReadEntry(entry);

                if (cleanAutoLocales &&
                    entry.Key.TypeId == ResourceTypeIds.Locale &&
                    autoLocaleIds.Contains(entry.Key.InstanceId))
                {
                    skipped++;
                    continue;
                }

                if (!raw && ResourceTypeIds.IsProperty(entry.Key.TypeId))
                {
                    string propPath = Path.Combine(
                        outputDir,
                        SanitizePath(names.GetRelativePath(entry.Key.TypeId, entry.Key.GroupId, entry.Key.InstanceId) + ".xml"));
                    WriteDirectory(propPath);

                    PropertyList list = PropertyList.Read(new MemoryStream(data));
                    if (cleanAutoLocales && autoLocaleIds.Count > 0)
                    {
                        list.StripAutoLocaleReferences(id => autoLocaleIds.Contains(id));
                    }

                    using FileStream xmlOut = File.Create(propPath);
                    list.WriteXml(xmlOut, names);
                }
                else if (!raw && ResourceTypeIds.IsTexture(entry.Key.TypeId))
                {
                    string ddsPath = Path.Combine(
                        outputDir,
                        SanitizePath(names.GetRelativePath(entry.Key.TypeId, entry.Key.GroupId, entry.Key.InstanceId) + ".dds"));
                    WriteDirectory(ddsPath);

                    try
                    {
                        RasterTexture raster = RasterTexture.Read(new MemoryStream(data));
                        using FileStream ddsOut = File.Create(ddsPath);
                        raster.ToDds().Write(ddsOut);
                    }
                    catch (Exception ex)
                    {
                        // Fault tolerance: fall back to the raw raster bytes.
                        output.Warn($"Could not convert raster {entry.Key} to DDS ({ex.Message}); writing raw data.");
                        File.WriteAllBytes(ddsPath[..^4], data);
                    }
                }
                else if (!raw && ResourceTypeIds.IsBitmap(entry.Key.TypeId))
                {
                    string pngPath = Path.Combine(
                        outputDir,
                        SanitizePath(names.GetRelativePath(entry.Key.TypeId, entry.Key.GroupId, entry.Key.InstanceId) + ".png"));
                    WriteDirectory(pngPath);

                    try
                    {
                        BitmapImage bitmap = BitmapImage.Read(new MemoryStream(data), entry.Key.TypeId);
                        using FileStream pngOut = File.Create(pngPath);
                        PngCodec.Encode(bitmap.Image, pngOut);
                    }
                    catch (Exception ex)
                    {
                        output.Warn($"Could not convert bitmap {entry.Key} to PNG ({ex.Message}); writing raw data.");
                        File.WriteAllBytes(pngPath[..^4], data);
                    }
                }
                else
                {
                    string relative = raw
                        ? RawKeyPath.Format(entry.Key)
                        : names.GetRelativePath(entry.Key.TypeId, entry.Key.GroupId, entry.Key.InstanceId);

                    string target = Path.Combine(outputDir, SanitizePath(relative));
                    WriteDirectory(target);
                    File.WriteAllBytes(target, data);
                }

                written++;
            }

            output.Success(Localizer.Default.Get("result.unpacked", written, outputDir));
            if (!selector.IsEmpty)
            {
                output.Line($"Selected {written} of {written + filtered} resource(s) (filtered out {filtered}).");
            }

            if (cleanAutoLocales && skipped > 0)
            {
                output.Line($"Cleaned {skipped} auto-generated locale(s).");
            }

            PrintDiagnostics(package, output);
        }

        return 0;
    }

    public static int Pack(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "pack <input-dir> <package> [--version 2.0|3.0] [--compress] [--big]");
        }

        string inputDir = Path.GetFullPath(cli.Positional[0]);
        string outputPath = Path.GetFullPath(cli.Positional[1]);

        if (!Directory.Exists(inputDir))
        {
            output.Error($"Input directory not found: {inputDir}");
            return 1;
        }

        DbpfWriteOptions options = new()
        {
            Compress = cli.Has("compress"),
            Big = cli.Has("big"),
            Version = ParseVersion(cli.Get("version")),
        };

        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        var items = new List<DbpfWriteItem>();

        foreach (string file in Directory.EnumerateFiles(inputDir, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(inputDir, file);

            // Raw-key layout (_raw/type/group/instance.bin): keys come straight from the path and no
            // name registry is needed, so a raw unpack packs back losslessly.
            if (RawKeyPath.TryParse(relative, out ResourceKey rawKey))
            {
                items.Add(new DbpfWriteItem(rawKey, File.ReadAllBytes(file)));
                continue;
            }

            bool isPropXml = relative.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
            bool isTextureDds = relative.EndsWith(".raster.dds", StringComparison.OrdinalIgnoreCase);
            bool isBitmapPng = relative.EndsWith(".png", StringComparison.OrdinalIgnoreCase);

            // Strip the conversion suffix so the remaining group/name.ext resolves normally.
            string resolvable = isPropXml ? relative[..^4]
                : isTextureDds ? relative[..^4]
                : isBitmapPng ? relative[..^4]
                : relative;

            if (!TryResolveInput(resolvable, names, stripXml: false,
                    out uint typeId, out uint groupId, out uint instanceId, out string groupName, out string instanceName))
            {
                output.Warn($"Skipping '{relative}': cannot derive a resource key (expected group/name.ext).");
                continue;
            }

            if (isPropXml && ResourceTypeIds.IsProperty(typeId))
            {
                LocaleTable? autoLocale;
                byte[] binary;
                try
                {
                    using FileStream xmlIn = File.OpenRead(file);
                    PropertyList list = PropertyList.ReadXml(xmlIn, names);

                    // Generate the auto locale from placeholder texts, then embed the ids.
                    autoLocale = AutoLocale.Generate(list, AutoLocale.GetName(groupName, instanceName), names);

                    using var buffer = new MemoryStream();
                    list.Write(buffer);
                    binary = buffer.ToArray();
                }
                catch (Exception ex)
                {
                    output.Error($"Failed to convert '{relative}': {ex.Message}");
                    continue;
                }

                items.Add(new DbpfWriteItem(new ResourceKey(typeId, groupId, instanceId), binary));

                if (autoLocale is not null)
                {
                    uint localeInstance = names.GetFileHash(AutoLocale.GetName(groupName, instanceName));
                    items.Add(new DbpfWriteItem(
                        new ResourceKey(ResourceTypeIds.Locale, AutoLocale.LocaleGroup, localeInstance),
                        Encoding.UTF8.GetBytes(autoLocale.Serialize())));
                }
            }
            else if (isTextureDds && ResourceTypeIds.IsTexture(typeId))
            {
                byte[] binary;
                try
                {
                    using FileStream ddsIn = File.OpenRead(file);
                    DdsTexture dds = DdsTexture.Read(ddsIn);
                    using var buffer = new MemoryStream();
                    RasterTexture.FromDds(dds).Write(buffer);
                    binary = buffer.ToArray();
                }
                catch (Exception ex)
                {
                    output.Error($"Failed to convert '{relative}': {ex.Message}");
                    continue;
                }

                items.Add(new DbpfWriteItem(new ResourceKey(typeId, groupId, instanceId), binary));
            }
            else if (isBitmapPng && ResourceTypeIds.IsBitmap(typeId))
            {
                byte[] binary;
                try
                {
                    using FileStream pngIn = File.OpenRead(file);
                    RgbaImage image = PngCodec.Decode(pngIn);
                    BitmapImage bitmap = BitmapImage.FromImage(typeId, image);
                    using var buffer = new MemoryStream();
                    bitmap.Write(buffer);
                    binary = buffer.ToArray();
                }
                catch (Exception ex)
                {
                    output.Error($"Failed to convert '{relative}': {ex.Message}");
                    continue;
                }

                items.Add(new DbpfWriteItem(new ResourceKey(typeId, groupId, instanceId), binary));
            }
            else
            {
                items.Add(new DbpfWriteItem(new ResourceKey(typeId, groupId, instanceId), File.ReadAllBytes(file)));
            }
        }

        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using (var stream = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite))
        {
            int count = DatabasePackedFile.Write(stream, items, options);
            output.Success(Localizer.Default.Get("result.packed", count, outputPath));
            output.Line($"Version: {options.Version}, compressed: {options.Compress}, big: {options.Big}");
            output.Line($"Output size: {Output.FormatSize(stream.Length)} ({stream.Length} bytes)");
        }

        return 0;
    }

    /// <summary>Converts a property file to XML or back: <c>prop &lt;input&gt; &lt;output&gt;</c>.</summary>
    public static int Prop(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "prop <input.prop|input.prop.xml> <output>");
        }

        string input = cli.Positional[0];
        string outputPath = cli.Positional[1];
        if (!File.Exists(input))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", input));
            return 1;
        }

        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        try
        {
            if (input.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                using FileStream xmlIn = File.OpenRead(input);
                PropertyList list = PropertyList.ReadXml(xmlIn, names);
                using FileStream binOut = File.Create(outputPath);
                list.Write(binOut);
                output.Success($"Wrote binary property file: {outputPath}");
            }
            else
            {
                byte[] data = File.ReadAllBytes(input);
                PropertyList list = PropertyList.Read(new MemoryStream(data));
                using FileStream xmlOut = File.Create(outputPath);
                list.WriteXml(xmlOut, names);
                output.Success($"Wrote {list.Count} propert(ies) to: {outputPath}");
            }
        }
        catch (Exception ex)
        {
            output.Error($"Conversion failed: {ex.Message}");
            return 1;
        }

        return 0;
    }

    /// <summary>Lists the entries of a locale table: <c>locale &lt;file.locale&gt;</c>.</summary>
    public static int Locale(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 1)
        {
            return UsageError(output, "locale <file.locale> [out.csv|--csv] | locale <in.csv> <out.locale>");
        }

        string input = cli.Positional[0];
        if (!File.Exists(input))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", input));
            return 1;
        }

        try
        {
            // CSV -> locale (import): locale <in.csv> <out.locale>
            if (input.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                || input.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase))
            {
                if (cli.Positional.Count < 2)
                {
                    return UsageError(output, "locale <in.csv> <out.locale>");
                }

                string csvText = File.ReadAllText(input);
                LocaleTable imported = LocaleCsv.Import(csvText, LocaleCsv.DetectSeparator(csvText));
                File.WriteAllText(cli.Positional[1], imported.Serialize(), new System.Text.UTF8Encoding(false));
                output.Success($"Imported {imported.Count} entr(ies) -> {cli.Positional[1]}");
                return 0;
            }

            LocaleTable table;
            using (FileStream stream = File.OpenRead(input))
            {
                table = LocaleTable.Read(stream);
            }

            // locale -> CSV (export): locale <in.locale> <out.csv>
            if (cli.Positional.Count >= 2)
            {
                string outPath = cli.Positional[1];
                char separator = outPath.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase) ? '\t' : ',';
                File.WriteAllText(outPath, LocaleCsv.Export(table, separator), new System.Text.UTF8Encoding(false));
                output.Success($"Exported {table.Count} entr(ies) -> {outPath}");
                return 0;
            }

            var rows = table.Entries
                .Select(e => new[] { Fnv.ToHex(e.InstanceId), Truncate(e.Text, 72) })
                .ToList();

            output.Table(new[] { "Instance", "Text" }, rows);
            output.Line();
            output.Line($"{table.Count} entr(ies) in '{input}'.");
            return 0;
        }
        catch (Exception ex)
        {
            output.Error($"Locale failed: {ex.Message}");
            return 1;
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    /// <summary>Converts a texture: <c>texture &lt;in&gt; &lt;out&gt;</c> (raster ⇄ DDS, auto-detected).</summary>
    public static int Texture(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "texture <input.raster|input.dds> <output>");
        }

        string input = cli.Positional[0];
        string outputPath = cli.Positional[1];
        if (!File.Exists(input))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", input));
            return 1;
        }

        try
        {
            byte[] head = new byte[4];
            using (FileStream probe = File.OpenRead(input))
            {
                probe.ReadExactly(head, 0, 4);
            }

            bool isDds = head[0] == (byte)'D' && head[1] == (byte)'D' && head[2] == (byte)'S' && head[3] == (byte)' ';

            if (isDds)
            {
                using FileStream ddsIn = File.OpenRead(input);
                DdsTexture dds = DdsTexture.Read(ddsIn);
                using FileStream rasterOut = File.Create(outputPath);
                RasterTexture.FromDds(dds).Write(rasterOut);
                output.Success($"Wrote raster ({dds.Width}x{dds.Height}, {TextureFormats.Describe(dds.Format)}): {outputPath}");
            }
            else
            {
                using FileStream rasterIn = File.OpenRead(input);
                RasterTexture raster = RasterTexture.Read(rasterIn);
                using FileStream ddsOut = File.Create(outputPath);
                raster.ToDds().Write(ddsOut);
                output.Success($"Wrote DDS ({raster.Width}x{raster.Height}, {TextureFormats.Describe(raster.TextureFormat)}, {raster.MipmapCount} mip(s)): {outputPath}");
            }
        }
        catch (Exception ex)
        {
            output.Error($"Texture conversion failed: {ex.Message}");
            return 1;
        }

        return 0;
    }

    /// <summary>Inspects a RenderWare 4 resource: <c>rw4 &lt;file.rw4&gt;</c>.</summary>
    public static int Rw4(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 1)
        {
            return UsageError(output, "rw4 <file.rw4> [--export file.obj] [--export-ogre file.mesh.xml] [--export-textures DIR] [--pose SECONDS]");
        }

        string input = cli.Positional[0];
        if (!File.Exists(input))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", input));
            return 1;
        }

        RenderWare4File rw4;
        try
        {
            rw4 = RenderWare4File.Open(input);
        }
        catch (NotRenderWare4Exception ex)
        {
            output.Error(ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            output.Error($"Failed to read RW4 resource: {ex.Message}");
            return 1;
        }

        output.Heading($"RenderWare 4: {Path.GetFileName(input)}");
        output.Line($"Container type: {Rw4SectionTypes.DescribeContainer(rw4.RawType)} (0x{rw4.RawType:X8})");
        output.Line($"Sections: {rw4.SectionCount}");
        output.Line($"Section table: 0x{rw4.SectionInfoOffset:X}");
        output.Line($"Buffer data: 0x{rw4.BufferDataOffset:X}");
        output.Line($"Manifest type codes: {rw4.Manifest.TypeCodes.Count}");
        output.Line($"Sub-references: {rw4.Manifest.SubReferences.Count}");
        output.Line();

        var rows = rw4.Sections
            .Select((s, i) => new[]
            {
                i.ToString(CultureInfo.InvariantCulture),
                Rw4SectionTypes.Describe(s.TypeCode),
                $"0x{s.DataOffset:X}",
                s.Size.ToString(CultureInfo.InvariantCulture),
                s.Alignment.ToString(CultureInfo.InvariantCulture),
            })
            .ToList();

        output.Table(new[] { "#", "Type", "Offset", "Size", "Align" }, rows);

        IReadOnlyList<Core.RenderWare4.Rw4Mesh> meshes = rw4.ReadMeshes();
        if (meshes.Count > 0)
        {
            output.Line();
            output.Heading($"Meshes ({meshes.Count})");
            var meshRows = meshes.Select((m, i) => new[]
            {
                i.ToString(CultureInfo.InvariantCulture),
                m.PrimitiveType.ToString(),
                m.TriangleCount.ToString(CultureInfo.InvariantCulture),
                m.VertexCount.ToString(CultureInfo.InvariantCulture),
                m.PrimitiveCount.ToString(CultureInfo.InvariantCulture),
                m.VertexBufferIndices.Count.ToString(CultureInfo.InvariantCulture),
            }).ToList();
            output.Table(new[] { "#", "Primitive", "Triangles", "Vertices", "Prims", "VBufs" }, meshRows);
        }

        IReadOnlyList<Core.RenderWare4.Rw4Skeleton> skeletons = rw4.ReadSkeletons();
        if (skeletons.Count > 0)
        {
            output.Line();
            output.Heading($"Skeletons ({skeletons.Count})");
            var skeletonRows = skeletons.Select((s, i) => new[]
            {
                i.ToString(CultureInfo.InvariantCulture),
                s.Bones.Count.ToString(CultureInfo.InvariantCulture),
                s.Bones.Count(b => b.ParentIndex < 0).ToString(CultureInfo.InvariantCulture),
                $"0x{s.SkeletonId:X8}",
            }).ToList();
            output.Table(new[] { "#", "Bones", "Roots", "Id" }, skeletonRows);
        }

        IReadOnlyList<Core.RenderWare4.Rw4SkinLinkage> skins = rw4.ReadSkins();
        if (skins.Count > 0)
        {
            output.Line();
            output.Heading($"Skins ({skins.Count})");
            var skinRows = skins.Select((s, i) => new[]
            {
                i.ToString(CultureInfo.InvariantCulture),
                s.Skeleton?.Bones.Count.ToString(CultureInfo.InvariantCulture) ?? "-",
                s.MatrixBuffer?.Matrices.Count.ToString(CultureInfo.InvariantCulture) ?? "-",
                s.AnimationSkin?.Poses.Count.ToString(CultureInfo.InvariantCulture) ?? "-",
            }).ToList();
            output.Table(new[] { "#", "Bones", "Matrices", "Poses" }, skinRows);
        }

        IReadOnlyList<Core.RenderWare4.Rw4KeyframeAnim> animations = rw4.ReadKeyframeAnimations();
        if (animations.Count > 0)
        {
            output.Line();
            output.Heading($"Keyframe animations ({animations.Count})");
            var animRows = animations.Select((a, i) => new[]
            {
                i.ToString(CultureInfo.InvariantCulture),
                $"0x{a.SkeletonId:X8}",
                a.Channels.Count.ToString(CultureInfo.InvariantCulture),
                a.Channels.Sum(c => c.Keyframes.Count).ToString(CultureInfo.InvariantCulture),
                a.Length.ToString("0.###", CultureInfo.InvariantCulture),
            }).ToList();
            output.Table(new[] { "#", "Skeleton", "Channels", "Keyframes", "Length" }, animRows);
        }

        IReadOnlyList<Core.RenderWare4.Rw4Raster> rasters = rw4.ReadRasters();
        if (rasters.Count > 0)
        {
            output.Line();
            output.Heading($"Rasters ({rasters.Count})");
            var rasterRows = rasters.Select((r, i) => new[]
            {
                i.ToString(CultureInfo.InvariantCulture),
                r.FormatName,
                $"{r.Width}x{r.Height}",
                r.MipmapLevels.ToString(CultureInfo.InvariantCulture),
                r.DataSectionIndex.ToString(CultureInfo.InvariantCulture),
            }).ToList();
            output.Table(new[] { "#", "Format", "Size", "Mips", "Data" }, rasterRows);
        }

        IReadOnlyList<Core.RenderWare4.Rw4Material> materials = rw4.ReadMaterials();
        if (materials.Count > 0)
        {
            output.Line();
            output.Heading($"Materials ({materials.Count})");
            var materialRows = materials.Select((m, i) => new[]
            {
                i.ToString(CultureInfo.InvariantCulture),
                $"0x{m.RendererId:X8}",
                Core.RenderWare4.Direct3DStates.FormatPrimitiveType(m.PrimitiveType),
                m.RenderGroups.Sum(g => g.States.Count).ToString(CultureInfo.InvariantCulture),
                m.TextureSlots.Count.ToString(CultureInfo.InvariantCulture),
                m.Decompiled ? "yes" : "no",
            }).ToList();
            output.Table(new[] { "#", "Shader", "Primitive", "States", "Slots", "Decoded" }, materialRows);
        }

        IReadOnlyList<Core.RenderWare4.Rw4MaterialLink> materialLinks = rw4.ReadMaterialLinks();
        if (materialLinks.Count > 0)
        {
            int referenced = materialLinks.Sum(l => l.MaterialSectionIndices.Count);
            output.Line();
            output.Line($"Material links: {materialLinks.Count} mesh(es), {referenced} material reference(s).");
        }

        IReadOnlyList<Core.RenderWare4.Rw4MorphHandle> morphs = rw4.ReadMorphHandles();
        if (morphs.Count > 0)
        {
            output.Line();
            output.Heading($"Morph handles ({morphs.Count})");
            var morphRows = morphs.Select((m, i) => new[]
            {
                i.ToString(CultureInfo.InvariantCulture),
                $"0x{m.HandleId:X8}",
                m.DefaultTime.ToString("0.###", CultureInfo.InvariantCulture),
            }).ToList();
            output.Table(new[] { "#", "Handle", "Default time" }, morphRows);
        }

        IReadOnlyList<Core.RenderWare4.Rw4BlendShape> blendShapes = rw4.ReadBlendShapes();
        if (blendShapes.Count > 0)
        {
            output.Line();
            output.Heading($"Blend shapes ({blendShapes.Count})");
            var shapeRows = blendShapes.Select((s, i) => new[]
            {
                i.ToString(CultureInfo.InvariantCulture),
                $"0x{s.Id:X8}",
                s.ShapeIds.Count.ToString(CultureInfo.InvariantCulture),
            }).ToList();
            output.Table(new[] { "#", "Id", "Shapes" }, shapeRows);
        }

        IReadOnlyList<Core.RenderWare4.Rw4BlendShapeBuffer> blendBuffers = rw4.ReadBlendShapeBuffers();
        if (blendBuffers.Count > 0)
        {
            output.Line();
            output.Heading($"Blend-shape buffers ({blendBuffers.Count})");
            var bufferRows = blendBuffers.Select((b, i) => new[]
            {
                i.ToString(CultureInfo.InvariantCulture),
                b.ShapeCount.ToString(CultureInfo.InvariantCulture),
                b.VertexCount.ToString(CultureInfo.InvariantCulture),
                b.Streams.Count(s => s is not null).ToString(CultureInfo.InvariantCulture),
            }).ToList();
            output.Table(new[] { "#", "Shapes", "Vertices", "Streams" }, bufferRows);
        }

        // --export <file.obj>: dump decoded geometry (with a material library when possible).
        string? exportPath = cli.Get("export");
        string? ogrePath = cli.Get("export-ogre");
        string? textureDirOption = cli.Get("export-textures");
        if (exportPath is not null)
        {
            IReadOnlyList<Core.RenderWare4.Rw4MeshGeometry> geometry = rw4.ReadGeometry();
            if (geometry.Count == 0)
            {
                output.Warn("No decodable mesh geometry was found.");
            }
            else
            {
                string objDir = Path.GetDirectoryName(Path.GetFullPath(exportPath)) ?? ".";
                string exportTexturesDir = textureDirOption ?? Path.Combine(objDir, Path.GetFileNameWithoutExtension(exportPath) + "_textures");
                string objBase = Path.GetFileNameWithoutExtension(exportPath);

                Rw4ExportMaterials prepared = PrepareRw4Export(rw4, objDir, objBase, exportTexturesDir, output);
                string mtlName = objBase + ".mtl";
                Core.RenderWare4.Rw4ObjExporter.Export(geometry, exportPath, mtlName, prepared.PickMaterial, prepared.Materials);

                int totalTriangles = geometry.Sum(g => g.TriangleCount);
                int totalVertices = geometry.Sum(g => g.VertexCount);
                output.Success($"Exported {geometry.Count} mesh(es) ({totalVertices} vertices, {totalTriangles} triangles), {prepared.Materials.Count} material(s) and {prepared.TextureCount} texture(s) to {exportPath}");
            }
        }

        // --export-ogre <file.mesh.xml>: dump geometry in the Ogre XML interchange format.
        if (ogrePath is not null)
        {
            IReadOnlyList<Core.RenderWare4.Rw4MeshGeometry> geometry = rw4.ReadGeometry();
            if (geometry.Count == 0)
            {
                output.Warn("No decodable mesh geometry was found.");
            }
            else
            {
                string ogreDir = Path.GetDirectoryName(Path.GetFullPath(ogrePath)) ?? ".";
                string ogreTexturesDir = textureDirOption ?? Path.Combine(ogreDir, Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(ogrePath)) + "_textures");
                string ogreBase = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(ogrePath));

                Rw4ExportMaterials prepared = PrepareRw4Export(rw4, ogreDir, ogreBase, ogreTexturesDir, output);
                Core.RenderWare4.Rw4OgreExporter.Export(ogrePath, geometry, geometry, prepared.PickMaterial, prepared.Materials);

                output.Success($"Exported {geometry.Count} submesh(es) ({geometry.Sum(g => g.TriangleCount)} triangles) in Ogre XML to {ogrePath} ({prepared.Materials.Count} material(s)).");
            }
        }

        // --export-textures <dir>: dump embedded textures as DDS (standalone; --export/--export-ogre handle their own).
        string? textureDir = cli.Get("export-textures");
        if (!string.IsNullOrEmpty(textureDir) && cli.Get("export") is null && cli.Get("export-ogre") is null)
        {
            IReadOnlyList<Core.RenderWare4.Rw4Raster> exportRasters = rw4.ReadRasters();
            Directory.CreateDirectory(textureDir);
            string baseName = Path.GetFileNameWithoutExtension(input);
            int written = 0;

            for (int i = 0; i < exportRasters.Count; i++)
            {
                Core.RenderWare4.Rw4Raster raster = exportRasters[i];
                byte[]? data = rw4.GetRasterData(raster);
                if (data is null || data.Length == 0)
                {
                    output.Warn($"Raster {i} has no pixel data; skipped.");
                    continue;
                }

                try
                {
                    string target = Path.Combine(textureDir, $"{baseName}_tex{i}.dds");
                    using FileStream ddsOut = File.Create(target);
                    raster.ToDds(data).Write(ddsOut);
                    written++;
                }
                catch (Exception ex)
                {
                    output.Warn($"Raster {i} could not be exported: {ex.Message}");
                }
            }

            output.Success($"Exported {written} texture(s) to {textureDir}.");
        }

        // --pose <seconds>: sample the first keyframe animation and print the channel transforms.
        string? poseArg = cli.Get("pose");
        if (poseArg is not null)
        {
            if (!float.TryParse(poseArg, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out float poseTime))
            {
                poseTime = 0;
            }

            IReadOnlyList<Core.RenderWare4.Rw4KeyframeAnim> poseAnims = rw4.ReadKeyframeAnimations();
            if (poseAnims.Count == 0)
            {
                output.Warn("No keyframe animation was found.");
            }
            else
            {
                IReadOnlyList<Core.RenderWare4.Rw4SampledChannel> pose = Core.RenderWare4.Rw4AnimationSampler.Sample(poseAnims[0], poseTime);
                output.Line();
                output.Heading($"Pose at {poseTime:0.###} s ({pose.Count} channel(s))");
                var poseRows = pose.Take(50).Select(c => new[]
                {
                    $"0x{c.ChannelId:X8}",
                    $"{c.Location[0]:0.###} {c.Location[1]:0.###} {c.Location[2]:0.###}",
                    $"{c.Rotation[0]:0.###} {c.Rotation[1]:0.###} {c.Rotation[2]:0.###} {c.Rotation[3]:0.###}",
                }).ToList();
                output.Table(new[] { "Channel", "Location (xyz)", "Rotation (xyzw)" }, poseRows);
            }
        }

        foreach (var diagnostic in rw4.Diagnostics.Items)
        {
            if (diagnostic.Level == Core.Diagnostics.DiagnosticLevel.Error)
            {
                output.Error(diagnostic.Message);
            }
            else if (diagnostic.Level == Core.Diagnostics.DiagnosticLevel.Warning)
            {
                output.Warn(diagnostic.Message);
            }
        }

        return 0;
    }

    /// <summary>Exports every decodable RW4 model from a package: <c>export-models &lt;package&gt; &lt;dir&gt;</c>.</summary>
    public static int ExportModels(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "export-models <package> <output-dir> [--format obj|ogre] [--raw-names]");
        }

        if (!TryOpen(cli, output, out DatabasePackedFile? package))
        {
            return 1;
        }

        string outputDir = Path.GetFullPath(cli.Positional[1]);
        NameRegistrySet names = OpenNames(cli);
        string format = (cli.Get("format") ?? "obj").ToLowerInvariant();
        bool rawNames = cli.Has("raw-names");
        Directory.CreateDirectory(outputDir);

        int exported = 0;
        int skipped = 0;
        int failed = 0;

        using (package)
        {
            foreach (DbpfEntry entry in package!.Entries)
            {
                if (entry.Key.TypeId != ResourceTypeIds.Rw4)
                {
                    continue;
                }

                byte[] data;
                try
                {
                    data = package.ReadEntry(entry);
                }
                catch (Exception ex)
                {
                    output.Warn($"{entry.Key}: read failed ({ex.Message})");
                    failed++;
                    continue;
                }

                try
                {
                    using Core.RenderWare4.RenderWare4File rw4 = Core.RenderWare4.RenderWare4File.Read(new MemoryStream(data));
                    IReadOnlyList<Core.RenderWare4.Rw4MeshGeometry> geometry = rw4.ReadGeometry();
                    if (geometry.Count == 0)
                    {
                        skipped++;
                        continue;
                    }

                    string stem = rawNames
                        ? $"{entry.Key.TypeId:X8}_{entry.Key.GroupId:X8}_{entry.Key.InstanceId:X8}"
                        : SanitizePath(names.GetRelativePath(entry.Key.TypeId, entry.Key.GroupId, entry.Key.InstanceId)).Replace('/', '_');
                    string modelDir = Path.Combine(outputDir, stem);
                    Directory.CreateDirectory(modelDir);
                    string texturesDir = Path.Combine(modelDir, stem + "_textures");

                    Rw4ExportMaterials prepared = PrepareRw4Export(rw4, modelDir, stem, texturesDir, output);

                    if (format == "ogre")
                    {
                        string meshPath = Path.Combine(modelDir, stem + ".mesh.xml");
                        Core.RenderWare4.Rw4OgreExporter.Export(meshPath, geometry, geometry, prepared.PickMaterial, prepared.Materials);
                    }
                    else
                    {
                        string objPath = Path.Combine(modelDir, stem + ".obj");
                        Core.RenderWare4.Rw4ObjExporter.Export(geometry, objPath, stem + ".mtl", prepared.PickMaterial, prepared.Materials);
                    }

                    exported++;
                }
                catch (Exception ex)
                {
                    output.Warn($"{entry.Key}: export failed ({ex.Message})");
                    failed++;
                }
            }

            output.Success($"Exported {exported} model(s) to {outputDir}.");
            output.Line($"  skipped (no static geometry): {skipped}");
            if (failed > 0)
            {
                output.Line($"  failed: {failed}");
            }
        }

        return 0;
    }

    /// <summary>Prepared material/texture data for an RW4 export (OBJ or Ogre).</summary>
    private sealed record Rw4ExportMaterials(
        List<(string Name, string? DiffuseTexture)> Materials,
        Func<Core.RenderWare4.Rw4MeshGeometry, string?> PickMaterial,
        int TextureCount);

    /// <summary>
    /// Exports an RW4 resource's textures to DDS and builds a material list, linking each mesh to the
    /// name of its compiled state's material (meshes -> MeshCompiledStateLink -> CompiledState -> raster).
    /// </summary>
    private static Rw4ExportMaterials PrepareRw4Export(
        Core.RenderWare4.RenderWare4File rw4,
        string outputDir,
        string filePrefix,
        string texturesDir,
        Output output)
    {
        var meshToMaterialSection = new Dictionary<int, int>();
        foreach (Core.RenderWare4.Rw4MaterialLink link in rw4.ReadMaterialLinks())
        {
            if (link.MaterialSectionIndices.Count > 0)
            {
                meshToMaterialSection[link.MeshSectionIndex] = link.MaterialSectionIndices[0];
            }
        }

        IReadOnlyList<Core.RenderWare4.Rw4Material> materials = rw4.ReadMaterials();
        IReadOnlyList<Core.RenderWare4.Rw4Raster> rasters = rw4.ReadRasters();

        Directory.CreateDirectory(texturesDir);
        var texturesWritten = new Dictionary<int, string>();
        for (int i = 0; i < rasters.Count; i++)
        {
            byte[]? pixels = rw4.GetRasterData(rasters[i]);
            if (pixels is null || pixels.Length == 0)
            {
                continue;
            }

            string fileName = $"{filePrefix}_tex{i}.dds";
            try
            {
                using FileStream ddsOut = File.Create(Path.Combine(texturesDir, fileName));
                rasters[i].ToDds(pixels).Write(ddsOut);
                texturesWritten[i] = fileName;
            }
            catch (Exception ex)
            {
                output.Warn($"Texture {i} could not be exported: {ex.Message}");
            }
        }

        var materialList = new List<(string Name, string? DiffuseTexture)>();
        var materialNameBySection = new Dictionary<int, string>();
        for (int i = 0; i < materials.Count; i++)
        {
            Core.RenderWare4.Rw4Material material = materials[i];
            string name = $"material_{i}";
            materialNameBySection[material.SectionIndex] = name;

            string? diffuse = null;
            if (material.TextureSlots.Count > 0)
            {
                int rasterSection = material.TextureSlots[0].RasterSectionIndex;
                int rasterIndex = -1;
                for (int r = 0; r < rasters.Count; r++)
                {
                    if (rasters[r].SectionIndex == rasterSection)
                    {
                        rasterIndex = r;
                        break;
                    }
                }

                if (rasterIndex >= 0 && texturesWritten.TryGetValue(rasterIndex, out string? file))
                {
                    diffuse = Path.GetRelativePath(outputDir, Path.Combine(texturesDir, file)).Replace('\\', '/');
                }
            }

            materialList.Add((name, diffuse));
        }

        Func<Core.RenderWare4.Rw4MeshGeometry, string?> pickMaterial = mesh =>
            meshToMaterialSection.TryGetValue(mesh.MeshSectionIndex, out int state)
                && materialNameBySection.TryGetValue(state, out string? name)
                ? name
                : null;

        return new Rw4ExportMaterials(materialList, pickMaterial, texturesWritten.Count);
    }

    /// <summary>Converts a bitmap or a texture: <c>bitmap &lt;in&gt; &lt;out&gt;</c> (auto-detected).</summary>
    public static int Bitmap(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "bitmap <input.bitImage|input.png> <output>");
        }

        string input = cli.Positional[0];
        string outputPath = cli.Positional[1];
        if (!File.Exists(input))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", input));
            return 1;
        }

        try
        {
            byte[] head = new byte[8];
            using (FileStream probe = File.OpenRead(input))
            {
                probe.ReadExactly(head, 0, 8);
            }

            bool isPng = head[0] == 0x89 && head[1] == (byte)'P' && head[2] == (byte)'N' && head[3] == (byte)'G';

            if (isPng)
            {
                uint type = ResolveBitmapType(input);
                using FileStream pngIn = File.OpenRead(input);
                RgbaImage image = PngCodec.Decode(pngIn);
                BitmapImage bitmap = BitmapImage.FromImage(type, image);
                using FileStream binOut = File.Create(outputPath);
                bitmap.Write(binOut);
                output.Success($"Wrote {SporeBitmapTypes.GetName(type)} ({image.Width}x{image.Height}): {outputPath}");
            }
            else
            {
                uint type = ResolveBitmapType(input);
                using FileStream binIn = File.OpenRead(input);
                BitmapImage bitmap = BitmapImage.Read(binIn, type);
                using FileStream pngOut = File.Create(outputPath);
                PngCodec.Encode(bitmap.Image, pngOut);
                output.Success($"Wrote PNG ({bitmap.Width}x{bitmap.Height}): {outputPath}");
            }
        }
        catch (Exception ex)
        {
            output.Error($"Bitmap conversion failed: {ex.Message}");
            return 1;
        }

        return 0;
    }

    /// <summary>Determines the bitmap type from a file's extension (e.g. <c>foo.32bitImage.png</c>).</summary>
    private static uint ResolveBitmapType(string path)
    {
        string name = Path.GetFileName(path);
        // Strip a trailing ".png" so the extension before it names the bitmap type.
        if (name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^4];
        }

        int dot = name.LastIndexOf('.');
        if (dot >= 0)
        {
            string ext = name[(dot + 1)..];
            foreach (uint type in new[]
            {
                SporeBitmapTypes.BitImage, SporeBitmapTypes.EightBitImage,
                SporeBitmapTypes.ThirtyTwoBitImage, SporeBitmapTypes.FortyEightBitImage,
            })
            {
                if (string.Equals(SporeBitmapTypes.GetName(type), ext, StringComparison.OrdinalIgnoreCase))
                {
                    return type;
                }
            }
        }

        // Default to a full-colour 32-bit bitmap.
        return SporeBitmapTypes.ThirtyTwoBitImage;
    }

    /// <summary>Recovers names from hashes: <c>guess &lt;hash&gt;... [--prefix ...] [--dict DIR]</c>.</summary>
    public static int Guess(CommandLine cli, Output output)
    {
        string? dictDir = cli.Get("dict");
        HashGuesser? guesser = GuessDictionary.TryLoad(dictDir);

        if (guesser is null)
        {
            output.Error(
                "Could not find the guess dictionaries (guess_left.txt / guess_right.txt). " +
                "Pass --dict <directory> pointing at the folder that contains them.");
            return 1;
        }

        string prefix = cli.Get("prefix") ?? string.Empty;
        string suffix = cli.Get("suffix") ?? string.Empty;
        string contains = cli.Get("contains") ?? string.Empty;
        int max = cli.GetInt("max") ?? 50;
        bool iterative = cli.Has("iterative");

        output.Line($"Loaded {guesser.LeftCount} left and {guesser.RightCount} right fragment(s).");

        // --validate <KeyMap.txt>: measure how well the guesser recovers real names.
        string? validatePath = cli.Get("validate");
        if (validatePath is not null)
        {
            if (!File.Exists(validatePath))
            {
                output.Error(Localizer.Default.Get("common.fileNotFound", validatePath));
                return 1;
            }

            KeyMap keyMap = KeyMap.ParseFile(validatePath);
            int limit = cli.GetInt("limit") ?? 500;
            GuesserValidationResult result = GuesserValidation.Validate(
                guesser, keyMap, limit, maxResultsPerHash: Math.Min(max, 10),
                progress: (done, total) =>
                {
                    if (done % 50 == 0 || done == total)
                    {
                        output.Line($"  validating {done}/{total}...");
                    }
                });

            output.Line();
            output.Line($"Attempted:   {result.Attempted}");
            output.Line($"Found:       {result.Found} ({result.FoundRate:P1})");
            output.Line($"Correct:     {result.Correct} ({result.CorrectRate:P1}) — real name among candidates");
            output.Line($"Top-1:       {result.Top1Correct} ({result.Top1Rate:P1}) — real name is the first candidate");

            if (result.Misses.Count > 0)
            {
                output.Line();
                output.Heading("Sample misses (real name -> top guess)");
                foreach ((string real, string? top) in result.Misses.Take(15))
                {
                    output.Line($"  {real}  ->  {top ?? "(nothing found)"}");
                }
            }

            return 0;
        }

        // --package <path>: guess for every unnamed hash in a package.
        string? packagePath = cli.Get("package");
        if (packagePath is not null)
        {
            if (!TryOpenArg(packagePath, output, out DatabasePackedFile? package))
            {
                return 1;
            }

            NameRegistrySet names = OpenNames(cli);
            int limit = cli.GetInt("limit") ?? 200;
            bool groups = cli.Has("groups");

            NameRecoveryResult result;
            using (package)
            {
                result = NameRecovery.Recover(
                    package!, guesser, names,
                    includeGroups: groups,
                    maxHashes: limit,
                    progress: (done, total) => output.Line($"  guessing {done}/{total}..."));
            }

            foreach ((uint hash, string name) in result.Recovered.OrderBy(p => p.Key))
            {
                output.Line($"{Fnv.ToHex(hash)} => {name}");
            }

            if (cli.Has("apply"))
            {
                NameRecovery.Apply(result, names);
            }

            string? save = cli.Get("save");
            if (save is not null)
            {
                NameRecovery.Save(result, save);
                output.Success($"Saved {result.RecoveredCount} name(s) to {save}");
            }

            output.Success($"Recovered {result.RecoveredCount} of {Math.Min(result.UnnamedCount, limit)} inspected hash(es) ({result.UnnamedCount} unnamed total).");
            return 0;
        }

        if (cli.Positional.Count == 0)
        {
            return UsageError(output, "guess <hash>... [--prefix P] [--suffix S] [--contains C] [--dict DIR] [--max N] [--iterative]");
        }

        int anyFound = 0;
        foreach (string token in cli.Positional)
        {
            if (!uint.TryParse(token.TrimStart('0', 'x', '#'), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint hash))
            {
                output.Warn($"Skipping '{token}': not a hexadecimal hash.");
                continue;
            }

            var results = iterative
                ? guesser.GuessIterative(hash, prefix, suffix, contains, rounds: 3, maxResults: max)
                : guesser.Guess(hash, prefix, suffix, contains, maxResults: max);

            if (results.Count == 0)
            {
                output.Line($"{Fnv.ToHex(hash)}: no name found.");
            }
            else
            {
                anyFound++;
                output.Line($"{Fnv.ToHex(hash)}:");
                foreach (string result in results)
                {
                    output.Line($"    {result}");
                }
            }
        }

        return anyFound > 0 ? 0 : 1;
    }

    /// <summary>Searches text files: <c>search &lt;dir&gt; &lt;query&gt; [--ext ...] [--limit N]</c>.</summary>
    public static int Search(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "search <directory> <query> [--regex] [--hex] [--ext .prop.xml,.locale] [--limit N]");
        }

        string directory = cli.Positional[0];
        string query = cli.Positional[1];
        if (!Directory.Exists(directory))
        {
            output.Error($"Directory not found: {directory}");
            return 1;
        }

        string[]? extensions = cli.Get("ext")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int limit = cli.GetInt("limit") ?? 500;

        if (cli.Has("hex") || query.StartsWith("hex:", StringComparison.OrdinalIgnoreCase))
        {
            string hexQuery = query.StartsWith("hex:", StringComparison.OrdinalIgnoreCase) ? query[4..] : query;
            byte[] needle = ParseHexBytes(hexQuery);
            if (needle.Length == 0)
            {
                output.Error("--hex expects hex bytes, e.g. 0B0A2C.");
                return 2;
            }

            List<(string Path, long Offset)> hits = SearchIndex.SearchHex(directory, extensions, needle, limit);
            output.Table(new[] { "File", "Offset" }, hits.Select(h => new[]
            {
                Path.GetRelativePath(directory, h.Path),
                $"0x{h.Offset:X}",
            }).ToList());
            output.Line();
            output.Line($"{hits.Count} match(es).");
            return hits.Count > 0 ? 0 : 1;
        }

        bool regexMode = cli.Has("regex") || query.StartsWith("re:", StringComparison.OrdinalIgnoreCase);
        string pattern = query.StartsWith("re:", StringComparison.OrdinalIgnoreCase) ? query[3..] : query;

        SearchIndex index = SearchIndex.BuildFromDirectory(directory, extensions);
        IReadOnlyList<SearchMatch> results = regexMode
            ? index.SearchRegex(pattern, limit)
            : index.Search(query, limit);

        output.Line($"Indexed {index.FileCount} file(s)" + (index.Skipped.Count > 0 ? $" ({index.Skipped.Count} skipped)" : string.Empty) + ".");
        output.Table(new[] { "File", "Line", "Text" }, results.Select(m => new[]
        {
            Path.GetRelativePath(directory, m.Path),
            m.LineNumber.ToString(CultureInfo.InvariantCulture),
            Truncate(m.Line.Trim(), 100),
        }).ToList());
        output.Line();
        output.Line($"{results.Count} match(es).");
        return results.Count > 0 ? 0 : 1;
    }

    /// <summary>Parses a hex string (spaces and 0x allowed) into bytes; empty on failure.</summary>
    private static byte[] ParseHexBytes(string value)
    {
        string cleaned = value.Replace(" ", string.Empty).Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (cleaned.Length == 0 || cleaned.Length % 2 != 0)
        {
            return Array.Empty<byte>();
        }

        try
        {
            return Convert.FromHexString(cleaned);
        }
        catch (FormatException)
        {
            return Array.Empty<byte>();
        }
    }

    /// <summary>Converts a property file to/from ArgScript text: <c>propt &lt;in&gt; &lt;out&gt;</c>.</summary>
    public static int PropT(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "propt <input.prop|input.prop_t> <output>");
        }

        string input = cli.Positional[0];
        string outputPath = cli.Positional[1];
        if (!File.Exists(input))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", input));
            return 1;
        }

        NameRegistrySet names = OpenNames(cli);
        try
        {
            if (input.EndsWith(".prop_t", StringComparison.OrdinalIgnoreCase))
            {
                PropertyList list = PropertyScript.Parse(File.ReadAllText(input), names);
                using FileStream binOut = File.Create(outputPath);
                list.Write(binOut);
                output.Success($"Wrote binary property file ({list.Count} propert(ies)): {outputPath}");
            }
            else
            {
                PropertyList list = PropertyList.Read(new MemoryStream(File.ReadAllBytes(input)));
                File.WriteAllText(outputPath, PropertyScript.Write(list, names));
                output.Success($"Wrote ArgScript text ({list.Count} propert(ies)): {outputPath}");
            }
        }
        catch (Exception ex)
        {
            output.Error($"Conversion failed: {ex.Message}");
            return 1;
        }

        return 0;
    }

    /// <summary>Converts a part-capabilities file: <c>pctp &lt;in&gt; &lt;out&gt;</c> (binary or text).</summary>
    public static int Pctp(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "pctp <input.pctp|input.pctp_t> <output>");
        }

        string input = cli.Positional[0];
        string outputPath = cli.Positional[1];
        if (!File.Exists(input))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", input));
            return 1;
        }

        NameRegistrySet names = OpenNames(cli);
        try
        {
            if (input.EndsWith(".pctp_t", StringComparison.OrdinalIgnoreCase))
            {
                PctpUnit unit = PctpUnit.Parse(File.ReadAllText(input), names);
                using FileStream binOut = File.Create(outputPath);
                unit.Write(binOut);
                output.Success($"Wrote binary PCTP v{unit.Version} ({unit.Capabilities.Count} capabilit(ies)): {outputPath}");
            }
            else
            {
                using FileStream input0 = File.OpenRead(input);
                PctpUnit unit = PctpUnit.Read(input0);
                File.WriteAllText(outputPath, unit.ToScript(names));
                output.Success($"Wrote PCTP text ({unit.Capabilities.Count} capabilit(ies), {unit.DeformSpecs.Count} deformSpec(s)): {outputPath}");
            }
        }
        catch (Exception ex)
        {
            output.Error($"PCTP conversion failed: {ex.Message}");
            return 1;
        }

        return 0;
    }

    /// <summary>Converts a TLSA file: <c>tlsa &lt;in&gt; &lt;out&gt;</c> (binary or text).</summary>
    public static int Tlsa(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "tlsa <input.tlsa|input.tlsa_t> <output>");
        }

        string input = cli.Positional[0];
        string outputPath = cli.Positional[1];
        if (!File.Exists(input))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", input));
            return 1;
        }

        NameRegistrySet names = OpenNames(cli);
        try
        {
            if (input.EndsWith(".tlsa_t", StringComparison.OrdinalIgnoreCase))
            {
                TlsaUnit unit = TlsaUnit.Parse(File.ReadAllText(input), names);
                using FileStream binOut = File.Create(outputPath);
                unit.Write(binOut);
                output.Success($"Wrote binary TLSA v{unit.Version} ({unit.Groups.Count} group(s)): {outputPath}");
            }
            else
            {
                using FileStream input0 = File.OpenRead(input);
                TlsaUnit unit = TlsaUnit.Read(input0);
                File.WriteAllText(outputPath, unit.ToScript(names));
                output.Success($"Wrote TLSA text ({unit.Groups.Count} group(s)): {outputPath}");
            }
        }
        catch (Exception ex)
        {
            output.Error($"TLSA conversion failed: {ex.Message}");
            return 1;
        }

        return 0;
    }

    /// <summary>Converts a gait file: <c>gait &lt;in&gt; &lt;out&gt;</c> (binary or text).</summary>
    public static int Gait(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "gait <input.gait|input.gait_t> <output>");
        }

        string input = cli.Positional[0];
        string outputPath = cli.Positional[1];
        if (!File.Exists(input))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", input));
            return 1;
        }

        try
        {
            if (input.EndsWith(".gait_t", StringComparison.OrdinalIgnoreCase))
            {
                GaitFile gait = GaitFile.Parse(File.ReadAllText(input));
                using FileStream binOut = File.Create(outputPath);
                gait.Write(binOut);
                output.Success($"Wrote binary GAIT ({gait.StepLengthKeys.Count} step keys, {gait.GaitGroupKeys.Count} group(s)): {outputPath}");
            }
            else
            {
                using FileStream input0 = File.OpenRead(input);
                GaitFile gait = GaitFile.Read(input0);
                File.WriteAllText(outputPath, gait.ToScript());
                output.Success($"Wrote GAIT text ({gait.StepLengthKeys.Count} step keys, {gait.GaitGroupKeys.Count} group(s)): {outputPath}");
            }
        }
        catch (Exception ex)
        {
            output.Error($"GAIT conversion failed: {ex.Message}");
            return 1;
        }

        return 0;
    }

    /// <summary>Dumps a conversation file: <c>cnv &lt;input.cnv&gt; &lt;output.cnv_t&gt;</c>.</summary>
    public static int Cnv(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "cnv <input.cnv> <output.cnv_t>");
        }

        string input = cli.Positional[0];
        string outputPath = cli.Positional[1];
        if (!File.Exists(input))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", input));
            return 1;
        }

        if (input.EndsWith(".cnv_t", StringComparison.OrdinalIgnoreCase))
        {
            output.Error("CNV text import is not implemented yet; only binary -> text dump is supported.");
            return 1;
        }

        NameRegistrySet names = OpenNames(cli);
        try
        {
            using FileStream input0 = File.OpenRead(input);
            CnvUnit unit = CnvUnit.Read(input0);
            File.WriteAllText(outputPath, unit.ToScript(names));
            output.Success($"Wrote CNV text dump ({unit.Dialogs.Count} dialog(s), {unit.Animations.Count} animation(s), {unit.EventLogs.Count} event log(s)): {outputPath}");
        }
        catch (Exception ex)
        {
            output.Error($"CNV conversion failed: {ex.Message}");
            return 1;
        }

        return 0;
    }

    /// <summary>Converts a loot table: <c>loottable &lt;in&gt; &lt;out&gt;</c> (binary or text).</summary>
    public static int LootTable(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "loottable <input.loottable|input.loottable_t> <output>");
        }

        string input = cli.Positional[0];
        string outputPath = cli.Positional[1];
        if (!File.Exists(input))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", input));
            return 1;
        }

        NameRegistrySet names = OpenNames(cli);
        try
        {
            if (input.EndsWith(".loottable_t", StringComparison.OrdinalIgnoreCase))
            {
                CellLootTableFile table = CellLootTableFile.Parse(File.ReadAllText(input), names);
                using FileStream binOut = File.Create(outputPath);
                table.Write(binOut);
                output.Success($"Wrote binary loot table ({table.Entries.Count} entr(ies)): {outputPath}");
            }
            else
            {
                using FileStream input0 = File.OpenRead(input);
                CellLootTableFile table = CellLootTableFile.Read(input0);
                File.WriteAllText(outputPath, table.ToScript(names));
                output.Success($"Wrote loot table text ({table.Entries.Count} entr(ies)): {outputPath}");
            }
        }
        catch (Exception ex)
        {
            output.Error($"Loot table conversion failed: {ex.Message}");
            return 1;
        }

        return 0;
    }

    /// <summary>Converts a cell look table: <c>looktable &lt;in&gt; &lt;out&gt;</c>.</summary>
    public static int LookTable(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "looktable <input.look_table|input.look_table_t> <output>");
        }

        string input = cli.Positional[0];
        string outputPath = cli.Positional[1];
        if (!File.Exists(input))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", input));
            return 1;
        }

        try
        {
            if (input.EndsWith(".look_table_t", StringComparison.OrdinalIgnoreCase))
            {
                CellLookTableFile table = CellLookTableFile.Parse(File.ReadAllText(input));
                using FileStream binOut = File.Create(outputPath);
                table.Write(binOut);
                output.Success($"Wrote binary look table ({table.Entries.Count} entr(ies)): {outputPath}");
            }
            else
            {
                using FileStream input0 = File.OpenRead(input);
                CellLookTableFile table = CellLookTableFile.Read(input0);
                File.WriteAllText(outputPath, table.ToScript());
                output.Success($"Wrote look table text ({table.Entries.Count} entr(ies)): {outputPath}");
            }
        }
        catch (Exception ex)
        {
            output.Error($"Look table conversion failed: {ex.Message}");
            return 1;
        }

        return 0;
    }

    /// <summary>Converts a cell world file: <c>cellworld &lt;in.world&gt; &lt;out.txt&gt;</c>.</summary>
    public static int CellWorld(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "cellworld <input.world> <output.txt>");
        }

        string input = cli.Positional[0];
        string outputPath = cli.Positional[1];
        if (!File.Exists(input))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", input));
            return 1;
        }

        try
        {
            using FileStream input0 = File.OpenRead(input);
            CellWorldFile world = CellWorldFile.Read(input0);
            File.WriteAllText(outputPath, world.ToText());
            output.Success(
                $"Wrote world text ({world.PopulateEntries.Count} populate, {world.AdvectEntries.Count} advect): {outputPath}");
        }
        catch (Exception ex)
        {
            output.Error($"Cell world conversion failed: {ex.Message}");
            return 1;
        }

        return 0;
    }

    /// <summary>Extracts a names registry from a game key map: <c>keymap &lt;KeyMap.txt&gt; &lt;names.txt&gt;</c>.</summary>
    public static int KeyMapCommand(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "keymap <KeyMap.txt> <names.txt> [--types <types.txt>] [--extensions <ext.txt>]");
        }

        string input = cli.Positional[0];
        string outputPath = cli.Positional[1];
        if (!File.Exists(input))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", input));
            return 1;
        }

        try
        {
            Core.Names.KeyMap map = Core.Names.KeyMap.ParseFile(input);
            File.WriteAllText(outputPath, map.BuildFileNameRegistry());

            output.Success($"Parsed {map.Entries.Count} key map entr(ies).");
            output.Line($"  group names:    {map.GroupNames.Count}");
            output.Line($"  instance names: {map.InstanceNames.Count}");
            output.Line($"  type names:     {map.TypeNames.Count}");
            output.Line($"  verified instance names: {map.VerifiedInstanceNames} (unverified: {map.UnverifiedEntries})");
            output.Line($"  wrote file-name registry: {outputPath}");

            string? typesPath = cli.Get("types");
            if (typesPath is not null)
            {
                File.WriteAllText(typesPath, map.BuildTypeRegistry());
                output.Line($"  wrote type registry:      {typesPath}");
            }

            string? extensionsPath = cli.Get("extensions");
            if (extensionsPath is not null)
            {
                File.WriteAllText(extensionsPath, map.BuildExtensionTable());
                output.Line($"  wrote extension table:    {extensionsPath}");
            }
        }
        catch (Exception ex)
        {
            output.Error($"Key map conversion failed: {ex.Message}");
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// Experimental full unpack into a game-like <c>Data</c> layout, using real names from a key map
    /// when available: <c>unpackdata &lt;package&gt; &lt;output-dir&gt; [--keymap KeyMap.txt]</c>.
    /// </summary>
    public static int UnpackData(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "unpackdata <package> <output-dir> [--keymap KeyMap.txt] [--ddflist DDFList.txt] [--group A,B] [--type T] [--name S]");
        }

        if (!TryOpen(cli, output, out DatabasePackedFile? package))
        {
            return 1;
        }

        string outputDir = Path.GetFullPath(cli.Positional[1]);
        NameRegistrySet names = OpenNames(cli);
        ResourceSelector selector = ResourceSelector.Parse(cli.Get("type"), cli.Get("group"), cli.Get("name"), names);

        KeyMap? keyMap = null;
        string? keyMapPath = cli.Get("keymap");
        if (!string.IsNullOrEmpty(keyMapPath))
        {
            if (!File.Exists(keyMapPath))
            {
                output.Error(Localizer.Default.Get("common.fileNotFound", keyMapPath));
                return 1;
            }

            keyMap = KeyMap.ParseFile(keyMapPath);
            output.Line($"Loaded key map with {keyMap.Entries.Count} entr(ies).");
        }

        DdfList? ddfList = null;
        string? ddfPath = cli.Get("ddflist");
        if (!string.IsNullOrEmpty(ddfPath))
        {
            if (!File.Exists(ddfPath))
            {
                output.Error(Localizer.Default.Get("common.fileNotFound", ddfPath));
                return 1;
            }

            ddfList = DdfList.ParseFile(ddfPath);
            output.Line($"Loaded DDFList with {ddfList.Folders.Count} folder(s) ({ddfList.IncludeCount} include(s)).");

            if (keyMap is null)
            {
                output.Warn("--ddflist needs --keymap: without real paths every resource is skipped.");
            }
        }

        Directory.CreateDirectory(outputDir);
        int keyed = 0;
        int fallback = 0;
        int written = 0;
        int skippedByDdf = 0;

        using (package)
        {
            foreach (DbpfEntry entry in package!.Entries)
            {
                string? realPath = keyMap?.GetPath(entry.Key.TypeId, entry.Key.GroupId, entry.Key.InstanceId);
                string relative = realPath ?? FallbackPath(names, keyMap, entry);
                if (!selector.IsEmpty && !selector.Matches(entry.Key.TypeId, entry.Key.GroupId, relative))
                {
                    continue;
                }

                if (ddfList is not null && (realPath is null || !ddfList.Contains(realPath)))
                {
                    skippedByDdf++;
                    continue;
                }

                if (realPath is not null)
                {
                    keyed++;
                }
                else
                {
                    fallback++;
                }

                string target = Path.Combine(outputDir, SanitizePath(relative));
                WriteDirectory(target);

                try
                {
                    byte[] data = package.ReadEntry(entry);
                    File.WriteAllBytes(target, data);
                    written++;
                }
                catch (Exception ex)
                {
                    output.Warn($"Could not write {entry.Key}: {ex.Message}");
                }
            }

            output.Success($"Unpacked {written} resource(s) to {outputDir}.");
            output.Line($"  named from key map: {keyed}");
            output.Line($"  fallback (group/name.ext): {fallback}");
            if (ddfList is not null)
            {
                output.Line($"  skipped (not in DDFList / no real path): {skippedByDdf}");
            }

            PrintDiagnostics(package, output);
        }

        return 0;
    }

    /// <summary>
    /// Fallback name for a resource without an exact key-map path: <c>group/name.ext</c>, preferring
    /// the real extension for the type from the key map (e.g. <c>txt</c> for triggers).
    /// </summary>
    private static string FallbackPath(NameRegistrySet names, KeyMap? keyMap, DbpfEntry entry)
    {
        string extension = keyMap?.GetExtension(entry.Key.TypeId) ?? names.GetTypeName(entry.Key.TypeId);
        string group = names.GetFileName(entry.Key.GroupId);
        string name = names.GetFileName(entry.Key.InstanceId);
        return $"{group}/{name}.{extension}";
    }

    public static int Verify(CommandLine cli, Output output)
    {
        if (!TryOpen(cli, output, out DatabasePackedFile? package))
        {
            return 1;
        }

        int checkedCount = 0;
        int problems = 0;
        using (package)
        {
            foreach (DbpfEntry entry in package!.Entries)
            {
                try
                {
                    byte[] data = package.ReadEntry(entry);
                    checkedCount++;

                    if (!entry.IsCompressed && entry.MemSize != 0 && data.Length != entry.MemSize)
                    {
                        output.Warn(
                            $"Entry {entry.Index} ({entry.Key}): expected {entry.MemSize} bytes, got {data.Length}.");
                        problems++;
                    }
                }
                catch (Exception ex)
                {
                    output.Error($"Entry {entry.Index} ({entry.Key}) failed: {ex.Message}");
                    problems++;
                }
            }

            output.Success(Localizer.Default.Get("result.verified", checkedCount, problems));
            PrintDiagnostics(package, output);
        }

        return problems == 0 ? 0 : 1;
    }

    /// <summary>
    /// Compares two archives: <c>diff &lt;a.package&gt; &lt;b.package&gt; [--no-content]</c>.
    /// </summary>
    public static int Diff(CommandLine cli, Output output)
    {
        if (cli.Positional.Count < 2)
        {
            return UsageError(output, "diff <a.package> <b.package> [--no-content] [--limit N]");
        }

        if (!TryOpenArg(cli.Positional[0], output, out DatabasePackedFile? a) ||
            !TryOpenArg(cli.Positional[1], output, out DatabasePackedFile? b))
        {
            a?.Dispose();
            return 1;
        }

        NameRegistrySet names = OpenNames(cli);
        bool compareContent = !cli.Has("no-content");
        int limit = cli.GetInt("limit") ?? 50;

        using (a)
        using (b)
        {
            ArchiveDiffResult result;
            try
            {
                result = ArchiveDiff.Compare(a!, b!, compareContent);
            }
            catch (Exception ex)
            {
                output.Error($"Comparison failed: {ex.Message}");
                return 1;
            }

            output.Line($"A: {cli.Positional[0]} ({result.CountA} resource(s))");
            output.Line($"B: {cli.Positional[1]} ({result.CountB} resource(s))");
            output.Line(compareContent ? "(comparing content)" : "(comparing keys only)");
            output.Line();
            output.Line($"  added:   {result.Added.Count}   (only in B)");
            output.Line($"  removed: {result.Removed.Count}   (only in A)");
            output.Line($"  changed: {result.Changed.Count}   (both, different content)");
            output.Line($"  same:    {result.Identical}");

            PrintDiffSection(output, names, "Added (only in B)", result.Added, limit);
            PrintDiffSection(output, names, "Removed (only in A)", result.Removed, limit);
            PrintDiffSection(output, names, "Changed", result.Changed, limit);

            output.Line();
            if (result.IsIdentical)
            {
                output.Success("Archives are identical (by key and content).");
            }
            else
            {
                output.Warn(
                    $"Archives differ: {result.Added.Count + result.Removed.Count + result.Changed.Count} difference(s).");
            }
        }

        return 0;
    }

    private static void PrintDiffSection(
        Output output,
        NameRegistrySet names,
        string title,
        List<DiffEntry> entries,
        int limit)
    {
        if (entries.Count == 0)
        {
            return;
        }

        output.Line();
        output.Heading($"{title} ({entries.Count})");
        foreach (DiffEntry entry in entries.Take(limit))
        {
            string path = names.GetRelativePath(entry.Key.TypeId, entry.Key.GroupId, entry.Key.InstanceId);
            string sizes = entry.Kind switch
            {
                DiffKind.Changed => $"{entry.SizeA} -> {entry.SizeB}",
                DiffKind.Added => $"+{entry.SizeB}",
                _ => $"-{entry.SizeA}",
            };
            output.Line($"  [{sizes,-16}] {path}  ({entry.Key})");
        }

        if (entries.Count > limit)
        {
            output.Line($"  … {entries.Count - limit} more");
        }
    }

    /// <summary>Extracts printable strings from any binary: <c>strings &lt;file&gt;</c>.</summary>
    public static int Strings(CommandLine cli, Output output)
    {
        if (cli.Positional.Count == 0)
        {
            return UsageError(output, "strings <file> [--min N] [--ascii] [--unicode] [--filter SUBSTR] [--limit N] [--save out.txt]");
        }

        string path = cli.Positional[0];
        if (!File.Exists(path))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", path));
            return 1;
        }

        int minimum = cli.GetInt("min") ?? 4;
        bool ascii = cli.Has("ascii");
        bool unicode = cli.Has("unicode");
        if (!ascii && !unicode)
        {
            ascii = unicode = true;
        }

        string? contains = cli.Get("filter");
        int display = cli.GetInt("limit") ?? 100;

        List<BinaryString> strings;
        try
        {
            byte[] data = File.ReadAllBytes(path);
            strings = StringsExtractor.Extract(data, minimum, ascii, unicode, contains, limit: 2_000_000);
        }
        catch (Exception ex)
        {
            output.Error($"Failed to read: {ex.Message}");
            return 1;
        }

        output.Line($"{strings.Count} string(s) (min length {minimum}{(contains is null ? string.Empty : $", filter '{contains}'")}).");

        string? save = cli.Get("save");
        if (!string.IsNullOrEmpty(save))
        {
            try
            {
                using var writer = new StreamWriter(save);
                foreach (BinaryString s in strings)
                {
                    writer.WriteLine($"{s.Offset:X8}\t{(s.Unicode ? "u" : "a")}\t{s.Value}");
                }

                output.Success($"Wrote {strings.Count} string(s) to {save}.");
            }
            catch (Exception ex)
            {
                output.Error($"Failed to write {save}: {ex.Message}");
                return 1;
            }
        }
        else
        {
            foreach (BinaryString s in strings.Take(display))
            {
                output.Line($"  {s.Offset:X8}  {(s.Unicode ? "u" : "a")}  {Truncate(s.Value, 120)}");
            }

            if (strings.Count > display)
            {
                output.Line($"  … {strings.Count - display} more (use --save to dump all)");
            }
        }

        return 0;
    }

    /// <summary>Inspects a PDB file: <c>pdb &lt;file.pdb&gt;</c>.</summary>
    public static int Pdb(CommandLine cli, Output output)
    {
        if (cli.Positional.Count == 0)
        {
            return UsageError(output, "pdb <file.pdb> [--modules] [--symbols] [--strings] [--filter S] [--limit N] [--save out.txt]");
        }

        string path = cli.Positional[0];
        if (!File.Exists(path))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", path));
            return 1;
        }

        try
        {
            using PdbFile pdb = PdbFile.Open(path);

            output.Heading($"PDB: {Path.GetFileName(path)}");
            output.Line($"  Page size:     {pdb.Container.PageSize}");
            output.Line($"  Blocks:        {pdb.Container.BlockCount}");
            output.Line($"  Streams:       {pdb.Container.StreamCount}");
            output.Line($"  Info version:  {pdb.InfoVersion}");
            output.Line($"  GUID:          {pdb.InfoGuid}");
            output.Line($"  Age:           {pdb.InfoAge}");
            output.Line($"  DBI version:   {pdb.DbiVersionHeader}, machine: 0x{pdb.DbiMachine:X4}");
            output.Line($"  Symbol stream: {pdb.SymbolRecordStream}, global: {pdb.GlobalSymbolStream}, public: {pdb.PublicSymbolStream}");
            output.Line($"  Modi substream: {pdb.ModiSubstreamSize} bytes");

            IReadOnlyList<PdbModule> modules = pdb.ReadModules();
            output.Line($"  Modules:       {modules.Count}");

            bool showModules = cli.Has("modules");
            bool showSymbols = cli.Has("symbols");
            string? contains = cli.Get("filter");
            int limit = cli.GetInt("limit") ?? 50;

            if (showModules)
            {
                output.Line();
                output.Heading("Modules");
                foreach (PdbModule module in modules.Take(limit))
                {
                    output.Line($"  {module.Name}  <-  {module.ObjectFile}");
                }

                if (modules.Count > limit)
                {
                    output.Line($"  … {modules.Count - limit} more");
                }
            }

            IReadOnlyList<PdbSymbol> symbols = pdb.ReadSymbols();
            var byKind = symbols.GroupBy(s => s.KindName).OrderByDescending(g => g.Count()).ToList();
            output.Line();
            output.Heading($"Symbols ({symbols.Count})");
            foreach (var group in byKind)
            {
                output.Line($"  {group.Key,-10} {group.Count()}");
            }

            if (cli.Has("strings"))
            {
                var literals = new List<string>();
                foreach (PdbSymbol symbol in symbols)
                {
                    if (!MsvcStringLiteral.IsStringLiteral(symbol.Name))
                    {
                        continue;
                    }

                    if (MsvcStringLiteral.TryDecode(symbol.Name, out string decoded) && decoded.Length > 0
                        && (contains is null || decoded.Contains(contains, StringComparison.OrdinalIgnoreCase)))
                    {
                        literals.Add(decoded);
                    }
                }

                output.Line();
                output.Heading($"String literals ({literals.Count})");
                string? literalSave = cli.Get("save");
                if (!string.IsNullOrEmpty(literalSave))
                {
                    try
                    {
                        File.WriteAllLines(literalSave, literals);
                        output.Success($"Wrote {literals.Count} decoded string literal(s) to {literalSave}.");
                    }
                    catch (Exception ex)
                    {
                        output.Error($"Failed to write {literalSave}: {ex.Message}");
                        return 1;
                    }
                }
                else
                {
                    foreach (string literal in literals.Take(limit))
                    {
                        output.Line($"  {Truncate(literal, 160)}");
                    }

                    if (literals.Count > limit)
                    {
                        output.Line($"  … {literals.Count - limit} more (use --save to dump all)");
                    }
                }

                return 0;
            }

            IEnumerable<PdbSymbol> selected = symbols;
            if (contains is not null)
            {
                selected = selected.Where(s => s.Name.Contains(contains, StringComparison.OrdinalIgnoreCase));
            }

            string? save = cli.Get("save");
            if (!string.IsNullOrEmpty(save))
            {
                try
                {
                    using var writer = new StreamWriter(save);
                    int written = 0;
                    foreach (PdbSymbol symbol in selected)
                    {
                        writer.WriteLine(symbol.Name);
                        written++;
                    }

                    output.Success($"Wrote {written} symbol name(s) to {save}.");
                }
                catch (Exception ex)
                {
                    output.Error($"Failed to write {save}: {ex.Message}");
                    return 1;
                }
            }
            else if (showSymbols || contains is not null)
            {
                output.Line();
                output.Heading("Symbol names");
                foreach (PdbSymbol symbol in selected.Take(limit))
                {
                    output.Line($"  [{symbol.KindName}] {symbol.Name}");
                }
            }

            return 0;
        }
        catch (NotMsfException ex)
        {
            output.Error(ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            output.Error($"Failed to read PDB: {ex.Message}");
            return 1;
        }
    }

    /// <summary>Inspects a Spore animation: <c>anim &lt;file.anim&gt;</c>.</summary>
    public static int Anim(CommandLine cli, Output output)
    {
        if (cli.Positional.Count == 0)
        {
            return UsageError(output, "anim <file.anim> [output.anim_t] [--channels] [--limit N]");
        }

        string path = cli.Positional[0];
        if (!File.Exists(path))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", path));
            return 1;
        }

        try
        {
            SpAnimation anim = SpAnimation.Open(path);

            // anim <in.anim> <out.anim_t>: dump the animation to readable text.
            if (cli.Positional.Count >= 2)
            {
                SpAnimationTextWriter.WriteFile(anim, cli.Positional[1]);
                output.Success($"Wrote text dump ({anim.Channels.Count} channel(s), {anim.Events.Count} event(s)): {cli.Positional[1]}");
                return 0;
            }

            output.Heading($"Animation: {Path.GetFileName(path)}");
            output.Line($"  Version:   {anim.Version}");
            output.Line($"  File size: {anim.FileSize}");
            output.Line($"  Id:        0x{anim.Id:X8}");
            output.Line($"  Length:    {anim.Length.ToString("0.###", CultureInfo.InvariantCulture)} s");
            output.Line($"  Channels:  {anim.Channels.Count}");
            output.Line($"  Events:    {anim.Events.Count}");

            if (anim.Events.Count > 0)
            {
                string names = string.Join(", ", anim.Events.Where(e => e.Length > 0).Take(10));
                if (names.Length > 0)
                {
                    output.Line($"    {names}");
                }
            }

            if (cli.Has("channels"))
            {
                int limit = cli.GetInt("limit") ?? 100;
                output.Line();
                output.Heading("Channels");
                var rows = anim.Channels.Take(limit).Select((c, i) => new[]
                {
                    i.ToString(CultureInfo.InvariantCulture),
                    c.Name,
                    c.KeyframeCount.ToString(CultureInfo.InvariantCulture),
                    c.Components.Count.ToString(CultureInfo.InvariantCulture),
                    string.Join(",", c.Components.Select(comp => comp.Type.ToString())),
                    $"0x{c.BindFlags:X}",
                }).ToList();
                output.Table(new[] { "#", "Name", "Keys", "Comps", "Component types", "Bind" }, rows);

                int decoded = anim.Channels.Sum(c => c.Components.Sum(comp => comp.Keyframes.Count));
                int interpolators = anim.Channels.Sum(c => c.Components.Sum(comp => comp.Keyframes.Sum(k => k.Interpolators.Count)));
                output.Line();
                output.Line($"Decoded keyframes: {decoded}, interpolators: {interpolators}");
            }

            foreach (var diagnostic in anim.Diagnostics.Items)
            {
                output.Warn(diagnostic.Message);
            }

            return 0;
        }
        catch (NotAnimationException ex)
        {
            output.Error(ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            output.Error($"Failed to read animation: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Repacks a package with our writer and checks the result is identical (repack validation):
    /// <c>roundtrip &lt;package&gt; [--compress] [--big] [--version X] [--keep out.package]</c>.
    /// </summary>
    public static int RoundTrip(CommandLine cli, Output output)
    {
        if (!TryOpen(cli, output, out DatabasePackedFile? package))
        {
            return 1;
        }

        var options = new DbpfWriteOptions
        {
            Compress = cli.Has("compress"),
            Big = cli.Has("big"),
            Version = ParseVersion(cli.Get("version")),
        };

        string? keep = cli.Get("keep");
        using (package)
        {
            output.Line($"Round-tripping {cli.Positional[0]} ({package!.Entries.Count} resource(s)), " +
                $"compress={options.Compress}, big={options.Big}...");

            ArchiveDiffResult result;
            try
            {
                result = ArchiveRoundTrip.Run(package, options, keep);
            }
            catch (Exception ex)
            {
                output.Error($"Round-trip failed: {ex.Message}");
                return 1;
            }

            output.Line($"  added:   {result.Added.Count}");
            output.Line($"  removed: {result.Removed.Count}");
            output.Line($"  changed: {result.Changed.Count}");
            output.Line($"  same:    {result.Identical}");

            if (result.IsIdentical)
            {
                output.Success("Round-trip is lossless (identical by key and content).");
                if (!string.IsNullOrEmpty(keep))
                {
                    output.Line($"  repacked archive: {keep}");
                }

                return 0;
            }

            output.Warn($"Round-trip differs: {result.Added.Count + result.Removed.Count + result.Changed.Count} difference(s).");
            foreach (DiffEntry entry in result.AllDifferences.Take(20))
            {
                output.Line($"  [{entry.Kind}] {entry.Key} ({entry.SizeA} -> {entry.SizeB})");
            }

            return 1;
        }
    }

    public static int Hash(CommandLine cli, Output output)
    {
        if (cli.Positional.Count == 0)
        {
            return UsageError(output, "hash <name>");
        }
        foreach (string name in cli.Positional)
        {
            uint hash = Fnv.Hash(name);
            output.Line(Localizer.Default.Get("result.hash", name, hash));
            output.Line($"    {Fnv.ToHex(hash)}  {Fnv.ToHexUpper(hash)}");
        }

        return 0;
    }

    public static int Types(CommandLine cli, Output output)
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        var rows = new List<string[]>();
        foreach (string name in names.Types.Names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
        {
            uint? hash = names.Types.GetHash(name);
            if (hash is not null)
            {
                rows.Add(new[] { name, Fnv.ToHexUpper(hash.Value) });
            }
        }

        output.Table(new[] { "Type", "Hash" }, rows);

        if (names.LoadWarnings.Count > 0)
        {
            foreach (string warning in names.LoadWarnings)
            {
                output.Warn(warning);
            }
        }

        return 0;
    }

    // ---- helpers ----

    /// <summary>
    /// Loads the embedded registries plus optional sources: a project <c>names.txt</c> (<c>--names</c>)
    /// and the game's key map (<c>--keymap KeyMap.txt</c>, authoritative real names).
    /// </summary>
    private static NameRegistrySet OpenNames(CommandLine cli)
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        string? projectNames = cli.Get("names");
        if (!string.IsNullOrEmpty(projectNames))
        {
            names.LoadProjectNames(projectNames);
        }

        string? keyMapPath = cli.Get("keymap");
        if (!string.IsNullOrEmpty(keyMapPath) && File.Exists(keyMapPath))
        {
            KeyMap map = KeyMap.ParseFile(keyMapPath);
            map.ApplyTo(names);
        }

        return names;
    }

    private static bool TryOpen(CommandLine cli, Output output, out DatabasePackedFile? package)
    {
        package = null;
        if (cli.Positional.Count == 0)
        {
            output.Error("A package path is required.");
            return false;
        }

        return TryOpenArg(cli.Positional[0], output, out package);
    }

    private static bool TryOpenArg(string path, Output output, out DatabasePackedFile? package)
    {
        package = null;
        if (!File.Exists(path))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", path));
            return false;
        }

        try
        {
            package = DatabasePackedFile.Open(path);
            return true;
        }
        catch (NotAPackageException ex)
        {
            output.Error(ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            output.Error($"Failed to open '{path}': {ex.Message}");
            return false;
        }
    }

    private static uint? ParseHashFilter(string? value, Func<string, uint> hasher, Output output)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return hasher(value);
    }

    private static DbpfVersion ParseVersion(string? value) => value switch
    {
        null or "" => DbpfVersion.Default,
        "2" or "2.0" => new DbpfVersion(2, 0, 0, 3),
        "3" or "3.0" => new DbpfVersion(3, 0, 0, 3),
        _ => DbpfVersion.Default,
    };

    private static string DescribeContainer(DatabasePackedFile package)
    {
        if (package.IsBig)
        {
            return "DBBF (big, 64-bit offsets)";
        }

        return package.IsProtected ? "DBPP (protected)" : "DBPF";
    }

    private static string SanitizePath(string relative)
    {
        string[] parts = relative.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder();
        foreach (string part in parts)
        {
            if (builder.Length > 0)
            {
                builder.Append('/');
            }

            foreach (char c in part)
            {
                builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }
        }

        return builder.ToString();
    }

    private static void WriteDirectory(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>Derives a resource key from a <c>group/name.ext</c> path (optionally with a trailing .xml).</summary>
    private static bool TryResolveInput(
        string relative,
        NameRegistrySet names,
        bool stripXml,
        out uint typeId,
        out uint groupId,
        out uint instanceId,
        out string groupName,
        out string instanceName)
    {
        typeId = groupId = instanceId = 0;
        groupName = instanceName = string.Empty;
        string normalized = relative.Replace('\\', '/');
        int slash = normalized.IndexOf('/');
        if (slash < 0)
        {
            return false;
        }

        string group = normalized[..slash];
        string rest = normalized[(slash + 1)..];
        if (stripXml && rest.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            rest = rest[..^4];
        }

        int dot = rest.LastIndexOf('.');
        if (dot < 0)
        {
            return false;
        }

        groupName = group;
        instanceName = rest[..dot];
        groupId = names.GetFileHash(groupName);
        instanceId = names.GetFileHash(instanceName);
        typeId = names.GetTypeHash(rest[(dot + 1)..]);
        return true;
    }

    private static void PrintDiagnostics(DatabasePackedFile package, Output output)
    {
        foreach (var diagnostic in package.Diagnostics.Items)
        {
            switch (diagnostic.Level)
            {
                case Core.Diagnostics.DiagnosticLevel.Error:
                    output.Error(diagnostic.Message);
                    break;
                case Core.Diagnostics.DiagnosticLevel.Warning:
                    output.Warn(diagnostic.Message);
                    break;
                default:
                    output.Info(diagnostic.Message);
                    break;
            }
        }
    }

    private static int UsageError(Output output, string usage)
    {
        output.Error(Localizer.Default.Get("common.usage", usage));
        return 2;
    }
}
