namespace SporeMaster.Cli;

/// <summary>A very small, dependency-free command line parser.</summary>
public sealed class CommandLine
{
    private readonly List<string> _positional = new();
    private readonly Dictionary<string, string?> _options = new(StringComparer.OrdinalIgnoreCase);

    public string? Command { get; private set; }

    public IReadOnlyList<string> Positional => _positional;

    public static CommandLine Parse(string[] args)
    {
        var result = new CommandLine();
        int i = 0;

        // First non-option token is the command.
        while (i < args.Length)
        {
            string token = args[i];

            if (token.StartsWith('-'))
            {
                string key = token.TrimStart('-');
                key = key switch
                {
                    "h" or "?" => "help",
                    _ => key,
                };

                string? value = null;
                if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                {
                    value = args[i + 1];
                    i++;
                }

                result._options[key] = value ?? string.Empty;
                i++;
                continue;
            }

            if (result.Command is null)
            {
                result.Command = token;
            }
            else
            {
                result._positional.Add(token);
            }

            i++;
        }

        return result;
    }

    public bool Has(string key) => _options.ContainsKey(key);

    public string? Get(string key) => _options.TryGetValue(key, out string? value) ? value : null;

    public int? GetInt(string key) =>
        _options.TryGetValue(key, out string? value) &&
        int.TryParse(value, out int parsed)
            ? parsed
            : null;
}
