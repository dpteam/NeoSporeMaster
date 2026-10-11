using System.Reflection;
using System.Text;
using SporeMaster.Core.Localization;

namespace SporeMaster.Cli;

/// <summary>Entry point for the headless SporeMaster CLI.</summary>
public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch
        {
            // Some hosts reject encoding changes; ignore.
        }

        CommandLine cli = CommandLine.Parse(args);

        if (cli.Has("lang"))
        {
            Localizer.Use(cli.Get("lang") ?? "en");
        }

        if (cli.Command is null && cli.Has("version"))
        {
            Console.WriteLine($"{GetVersion()} (language: {Localizer.Default.Language})");
            return 0;
        }

        var output = new Output
        {
            Quiet = cli.Has("quiet") || cli.Has("q"),
            NoColor = cli.Has("no-color"),
        };

        if (cli.Has("help") || cli.Command == "help")
        {
            PrintUsage(output);
            return 0;
        }

        if (cli.Command is null)
        {
            PrintUsage(output);
            return 2;
        }

        return cli.Command.ToLowerInvariant() switch
        {
            "info" => Commands.Info(cli, output),
            "list" or "ls" => Commands.List(cli, output),
            "extract" => Commands.Extract(cli, output),
            "unpack" => Commands.Unpack(cli, output),
            "unpackdata" => Commands.UnpackData(cli, output),
            "pack" => Commands.Pack(cli, output),
            "prop" => Commands.Prop(cli, output),
            "propt" => Commands.PropT(cli, output),
            "locale" => Commands.Locale(cli, output),
            "texture" => Commands.Texture(cli, output),
            "bitmap" => Commands.Bitmap(cli, output),
            "image" => Commands.Image(cli, output),
            "guess" => Commands.Guess(cli, output),
            "search" => Commands.Search(cli, output),
            "pctp" => Commands.Pctp(cli, output),
            "tlsa" => Commands.Tlsa(cli, output),
            "gait" => Commands.Gait(cli, output),
            "cnv" => Commands.Cnv(cli, output),
            "loottable" => Commands.LootTable(cli, output),
            "looktable" => Commands.LookTable(cli, output),
            "cellworld" => Commands.CellWorld(cli, output),
            "cellmap" => Commands.CellMap(cli, output),
            "summary" => Commands.Summary(cli, output),
            "pollen" => Commands.Pollen(cli, output),
            "arth" => Commands.Arth(cli, output),
            "scn" => Commands.Scn(cli, output),
            "creaturedata" => Commands.CreatureData(cli, output),
            "spui" => Commands.Spui(cli, output),
            "keymap" => Commands.KeyMapCommand(cli, output),
            "rw4" => Commands.Rw4(cli, output),
            "export-models" => Commands.ExportModels(cli, output),
            "verify" => Commands.Verify(cli, output),
            "roundtrip" => Commands.RoundTrip(cli, output),
            "diff" => Commands.Diff(cli, output),
            "strings" => Commands.Strings(cli, output),
            "pdb" => Commands.Pdb(cli, output),
            "anim" => Commands.Anim(cli, output),
            "hash" => Commands.Hash(cli, output),
            "types" => Commands.Types(cli, output),
            _ => UnknownCommand(cli.Command, output),
        };
    }

    private static int UnknownCommand(string command, Output output)
    {
        output.Error($"Unknown command: {command}");
        PrintUsage(output);
        return 2;
    }

    private static void PrintUsage(Output output)
    {
        Localizer loc = Localizer.Default;
        output.Heading($"{GetVersion()}");
        output.Line();
        output.Line("Usage: spore <command> [args] [options]");
        output.Line();
        output.Line("Commands:");
        output.Line($"  {loc["cmd.info.usage"],-58} {loc["cmd.info.summary"]}");
        output.Line($"  {loc["cmd.list.usage"],-58} {loc["cmd.list.summary"]}");
        output.Line($"  {loc["cmd.extract.usage"],-58} {loc["cmd.extract.summary"]}");
        output.Line($"  {loc["cmd.unpack.usage"],-58} {loc["cmd.unpack.summary"]}");
        output.Line($"  {loc["cmd.unpackdata.usage"],-58} {loc["cmd.unpackdata.summary"]}");
        output.Line($"  {loc["cmd.pack.usage"],-58} {loc["cmd.pack.summary"]}");
        output.Line($"  {loc["cmd.prop.usage"],-58} {loc["cmd.prop.summary"]}");
        output.Line($"  {loc["cmd.propt.usage"],-58} {loc["cmd.propt.summary"]}");
        output.Line($"  {loc["cmd.locale.usage"],-58} {loc["cmd.locale.summary"]}");
        output.Line($"  {loc["cmd.texture.usage"],-58} {loc["cmd.texture.summary"]}");
        output.Line($"  {loc["cmd.bitmap.usage"],-58} {loc["cmd.bitmap.summary"]}");
        output.Line($"  {loc["cmd.image.usage"],-58} {loc["cmd.image.summary"]}");
        output.Line($"  {loc["cmd.guess.usage"],-58} {loc["cmd.guess.summary"]}");
        output.Line($"  {loc["cmd.search.usage"],-58} {loc["cmd.search.summary"]}");
        output.Line($"  {loc["cmd.pctp.usage"],-58} {loc["cmd.pctp.summary"]}");
        output.Line($"  {loc["cmd.tlsa.usage"],-58} {loc["cmd.tlsa.summary"]}");
        output.Line($"  {loc["cmd.gait.usage"],-58} {loc["cmd.gait.summary"]}");
        output.Line($"  {loc["cmd.cnv.usage"],-58} {loc["cmd.cnv.summary"]}");
        output.Line($"  {loc["cmd.loottable.usage"],-58} {loc["cmd.loottable.summary"]}");
        output.Line($"  {loc["cmd.looktable.usage"],-58} {loc["cmd.looktable.summary"]}");
        output.Line($"  {loc["cmd.cellworld.usage"],-58} {loc["cmd.cellworld.summary"]}");
        output.Line($"  {loc["cmd.cellmap.usage"],-58} {loc["cmd.cellmap.summary"]}");
        output.Line($"  {loc["cmd.summary.usage"],-58} {loc["cmd.summary.summary"]}");
        output.Line($"  {loc["cmd.pollen.usage"],-58} {loc["cmd.pollen.summary"]}");
        output.Line($"  {loc["cmd.arth.usage"],-58} {loc["cmd.arth.summary"]}");
        output.Line($"  {loc["cmd.scn.usage"],-58} {loc["cmd.scn.summary"]}");
        output.Line($"  {loc["cmd.creaturedata.usage"],-58} {loc["cmd.creaturedata.summary"]}");
        output.Line($"  {loc["cmd.spui.usage"],-58} {loc["cmd.spui.summary"]}");
        output.Line($"  {loc["cmd.keymap.usage"],-58} {loc["cmd.keymap.summary"]}");
        output.Line($"  {loc["cmd.rw4.usage"],-58} {loc["cmd.rw4.summary"]}");
        output.Line($"  {loc["cmd.export-models.usage"],-58} {loc["cmd.export-models.summary"]}");
        output.Line($"  {loc["cmd.verify.usage"],-58} {loc["cmd.verify.summary"]}");
        output.Line($"  {loc["cmd.roundtrip.usage"],-58} {loc["cmd.roundtrip.summary"]}");
        output.Line($"  {loc["cmd.diff.usage"],-58} {loc["cmd.diff.summary"]}");
        output.Line($"  {loc["cmd.strings.usage"],-58} {loc["cmd.strings.summary"]}");
        output.Line($"  {loc["cmd.pdb.usage"],-58} {loc["cmd.pdb.summary"]}");
        output.Line($"  {loc["cmd.anim.usage"],-58} {loc["cmd.anim.summary"]}");
        output.Line($"  {loc["cmd.hash.usage"],-58} {loc["cmd.hash.summary"]}");
        output.Line($"  {loc["cmd.types.usage"],-58} {loc["cmd.types.summary"]}");
        output.Line();
        output.Line("Global options:");
        output.Line("  --lang <code>    UI language (default: en; available: " + string.Join(", ", Localizer.AvailableLanguages) + ")");
        output.Line("  --quiet, -q      Suppress normal output");
        output.Line("  --no-color       Disable coloured output");
        output.Line("  --version        Print the version");
        output.Line("  --help, -h       Show this help");
    }

    private static string GetVersion()
    {
        Assembly assembly = typeof(Program).Assembly;
        string? info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return $"{Localizer.Default["app.name"]} {info ?? assembly.GetName().Version?.ToString() ?? "0.0.0"}";
    }
}
