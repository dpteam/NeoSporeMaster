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
            "pack" => Commands.Pack(cli, output),
            "prop" => Commands.Prop(cli, output),
            "locale" => Commands.Locale(cli, output),
            "texture" => Commands.Texture(cli, output),
            "verify" => Commands.Verify(cli, output),
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
        output.Line($"  {loc["cmd.pack.usage"],-58} {loc["cmd.pack.summary"]}");
        output.Line($"  {loc["cmd.prop.usage"],-58} {loc["cmd.prop.summary"]}");
        output.Line($"  {loc["cmd.locale.usage"],-58} {loc["cmd.locale.summary"]}");
        output.Line($"  {loc["cmd.texture.usage"],-58} {loc["cmd.texture.summary"]}");
        output.Line($"  {loc["cmd.verify.usage"],-58} {loc["cmd.verify.summary"]}");
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
