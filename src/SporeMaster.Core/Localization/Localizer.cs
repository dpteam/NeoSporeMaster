using System.Globalization;
using System.Reflection;

namespace SporeMaster.Core.Localization;

/// <summary>
/// A tiny, dependency-free string catalog. The default (and currently only shipped) language
/// is English; the framework is ready for additional languages (see <c>lang/&lt;code&gt;.lang</c>).
/// </summary>
/// <remarks>
/// Application messages are intentionally English; project documentation is Russian.
/// Adding full Russian UI support is tracked as an idea in MEMORY.md.
/// </remarks>
public sealed class Localizer
{
    private readonly Dictionary<string, string> _strings;
    private readonly Dictionary<string, string> _fallback;

    private Localizer(string language, Dictionary<string, string> strings, Dictionary<string, string> fallback)
    {
        Language = language;
        _strings = strings;
        _fallback = fallback;
    }

    public string Language { get; }

    public static Localizer Default { get; private set; } = Load("en");

    /// <summary>Languages discovered as embedded <c>lang/*.lang</c> resources.</summary>
    public static IReadOnlyList<string> AvailableLanguages { get; } = DiscoverLanguages();

    /// <summary>Loads a language, falling back to English for missing keys and unknown languages.</summary>
    public static Localizer Load(string language)
    {
        var english = Parse(ReadEmbedded("en.lang"));
        if (string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
        {
            return new Localizer("en", english, english);
        }

        string? text = ReadEmbedded($"{language}.lang");
        if (text is null)
        {
            return new Localizer("en", english, english);
        }

        return new Localizer(language, Parse(text), english);
    }

    public static Localizer Use(string language)
    {
        Default = Load(language);
        return Default;
    }

    /// <summary>Looks up a key, returning the key itself when it is not defined.</summary>
    public string this[string key] => Get(key);

    public string Get(string key)
    {
        if (_strings.TryGetValue(key, out string? value))
        {
            return value;
        }

        if (_fallback.TryGetValue(key, out string? fallback))
        {
            return fallback;
        }

        return key;
    }

    public string Get(string key, params object?[] args)
    {
        string template = Get(key);
        try
        {
            return string.Format(CultureInfo.InvariantCulture, template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    private static Dictionary<string, string> Parse(string? text)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (text is null)
        {
            return result;
        }

        foreach (string rawLine in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            int equals = line.IndexOf('=');
            if (equals <= 0)
            {
                continue;
            }

            string key = line[..equals].Trim();
            string value = line[(equals + 1)..].Trim();
            result[key] = value;
        }

        return result;
    }

    private static string? ReadEmbedded(string fileName)
    {
        Assembly assembly = typeof(Localizer).Assembly;
        string suffix = "." + fileName;
        foreach (string name in assembly.GetManifestResourceNames())
        {
            if (!name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using Stream? stream = assembly.GetManifestResourceStream(name);
            if (stream is null)
            {
                continue;
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        return null;
    }

    private static IReadOnlyList<string> DiscoverLanguages()
    {
        var languages = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        Assembly assembly = typeof(Localizer).Assembly;
        foreach (string name in assembly.GetManifestResourceNames())
        {
            int idx = name.LastIndexOf(".lang/", StringComparison.OrdinalIgnoreCase);
            int alt = name.LastIndexOf(".lang.", StringComparison.OrdinalIgnoreCase);
            string? tail = idx >= 0 ? name[(idx + ".lang/".Length)..] : alt >= 0 ? name[(alt + ".lang.".Length)..] : null;
            if (tail is not null && tail.EndsWith(".lang", StringComparison.OrdinalIgnoreCase))
            {
                languages.Add(tail[..^".lang".Length]);
            }
        }

        return languages.Count == 0 ? new[] { "en" } : languages.ToList();
    }
}
