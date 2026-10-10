using System.Globalization;
using System.Text;
using SporeMaster.Core.Compression;
using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Hashing;
using SporeMaster.Core.Locale;
using SporeMaster.Core.Localization;
using SporeMaster.Core.Names;
using SporeMaster.Core.Properties;

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

        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        int? typeFilter = ParseHashFilter(cli.Get("type"), names.GetTypeHash, output);
        int? groupFilter = ParseHashFilter(cli.Get("group"), names.GetFileHash, output);
        string? nameFilter = cli.Get("name");
        bool onlyCompressed = cli.Has("compressed");
        int limit = cli.GetInt("limit") ?? int.MaxValue;

        using (package)
        {
            var rows = new List<string[]>();
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

                string resolved = names.GetRelativePath(entry.Key.TypeId, entry.Key.GroupId, entry.Key.InstanceId);
                if (nameFilter is not null &&
                    resolved.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

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

            output.Table(
                new[] { "#", "Type", "Group", "Instance", "C", "Packed", "Mem", "Path" },
                rows);
            output.Line();
            output.Line($"{rows.Count} resource(s) shown.");
            PrintDiagnostics(package, output);
        }

        return 0;
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

            NameRegistrySet names = NameRegistrySet.LoadEmbedded();
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
            return UsageError(output, "unpack <package> <output-dir> [--raw]");
        }

        if (!TryOpen(cli, output, out DatabasePackedFile? package))
        {
            return 1;
        }

        string outputDir = Path.GetFullPath(cli.Positional[1]);
        bool raw = cli.Has("raw");
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();
        Directory.CreateDirectory(outputDir);

        int written = 0;
        using (package)
        {
            foreach (DbpfEntry entry in package!.Entries)
            {
                byte[] data = package.ReadEntry(entry);

                if (!raw && ResourceTypeIds.IsProperty(entry.Key.TypeId))
                {
                    string propPath = Path.Combine(
                        outputDir,
                        SanitizePath(names.GetRelativePath(entry.Key.TypeId, entry.Key.GroupId, entry.Key.InstanceId) + ".xml"));
                    WriteDirectory(propPath);

                    PropertyList list = PropertyList.Read(new MemoryStream(data));
                    using FileStream xmlOut = File.Create(propPath);
                    list.WriteXml(xmlOut, names);
                }
                else
                {
                    string relative = raw
                        ? $"{entry.Key.TypeId:X8}/{entry.Key.InstanceId:X8}.{entry.Key.GroupId:X8}"
                        : names.GetRelativePath(entry.Key.TypeId, entry.Key.GroupId, entry.Key.InstanceId);

                    string target = Path.Combine(outputDir, SanitizePath(relative));
                    WriteDirectory(target);
                    File.WriteAllBytes(target, data);
                }

                written++;
            }

            output.Success(Localizer.Default.Get("result.unpacked", written, outputDir));
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
            bool isPropXml = relative.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);

            if (!TryResolveInput(relative, names, stripXml: isPropXml,
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
            return UsageError(output, "locale <file.locale>");
        }

        string input = cli.Positional[0];
        if (!File.Exists(input))
        {
            output.Error(Localizer.Default.Get("common.fileNotFound", input));
            return 1;
        }

        LocaleTable table;
        try
        {
            using FileStream stream = File.OpenRead(input);
            table = LocaleTable.Read(stream);
        }
        catch (Exception ex)
        {
            output.Error($"Failed to parse locale table: {ex.Message}");
            return 1;
        }

        var rows = table.Entries
            .Select(e => new[] { Fnv.ToHex(e.InstanceId), Truncate(e.Text, 72) })
            .ToList();

        output.Table(new[] { "Instance", "Text" }, rows);
        output.Line();
        output.Line($"{table.Count} entr(ies) in '{input}'.");
        return 0;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

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

    private static bool TryOpen(CommandLine cli, Output output, out DatabasePackedFile? package)
    {
        package = null;
        if (cli.Positional.Count == 0)
        {
            output.Error("A package path is required.");
            return false;
        }

        string path = cli.Positional[0];
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

    private static int? ParseHashFilter(string? value, Func<string, uint> hasher, Output output)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return unchecked((int)hasher(value));
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
