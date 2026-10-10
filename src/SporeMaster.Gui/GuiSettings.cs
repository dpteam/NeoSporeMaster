using System.IO;

namespace SporeMaster.Gui;

/// <summary>
/// A tiny, dependency-free settings store for the GUI (a plain <c>key=value</c> file under
/// <c>%APPDATA%\SporeMaster3\settings.txt</c>). Currently only the UI language is persisted.
/// </summary>
internal static class GuiSettings
{
    private static readonly string DirectoryPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SporeMaster3");

    private static string FilePath => Path.Combine(DirectoryPath, "settings.txt");

    public static string Language
    {
        get => Read("lang") ?? "en";
        set => Write("lang", value);
    }

    private static string? Read(string key)
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            foreach (string line in File.ReadAllLines(FilePath))
            {
                int equals = line.IndexOf('=');
                if (equals > 0 && line[..equals].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return line[(equals + 1)..].Trim();
                }
            }
        }
        catch
        {
            // best effort
        }

        return null;
    }

    private static void Write(string key, string value)
    {
        try
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(FilePath))
            {
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    int equals = line.IndexOf('=');
                    if (equals > 0)
                    {
                        values[line[..equals].Trim()] = line[(equals + 1)..].Trim();
                    }
                }
            }

            values[key] = value;
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllLines(FilePath, values.Select(kv => $"{kv.Key}={kv.Value}"));
        }
        catch
        {
            // best effort
        }
    }
}
