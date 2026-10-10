using System.Reflection;
using System.Text;

namespace SporeMaster.Core.Names;

/// <summary>Reads text from resources embedded in the SporeMaster.Core assembly.</summary>
internal static class EmbeddedResources
{
    private static readonly Assembly Assembly = typeof(EmbeddedResources).Assembly;

    /// <summary>
    /// Finds an embedded resource whose name ends with <paramref name="suffix"/> (the
    /// resource name includes the project's root namespace, so a suffix match keeps this
    /// resilient to project renames) and returns its text, or null when missing.
    /// </summary>
    public static string? ReadText(string suffix)
    {
        string normalizedSuffix = "." + suffix.Replace('\\', '.').Replace('/', '.');
        foreach (string name in Assembly.GetManifestResourceNames())
        {
            if (name.EndsWith(normalizedSuffix, StringComparison.OrdinalIgnoreCase))
            {
                using Stream? stream = Assembly.GetManifestResourceStream(name);
                if (stream is null)
                {
                    continue;
                }

                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
        }

        return null;
    }
}
