namespace SporeMaster.Cli;

/// <summary>Console output helper with optional quiet and colour modes.</summary>
public sealed class Output
{
    public bool Quiet { get; init; }

    public bool NoColor { get; init; }

    public void Line(string text = "")
    {
        if (!Quiet)
        {
            Console.WriteLine(text);
        }
    }

    public void Info(string text) => Colored(text, ConsoleColor.Gray);

    public void Success(string text) => Colored(text, ConsoleColor.Green);

    public void Warn(string text) => Colored(text, ConsoleColor.Yellow);

    public void Error(string text) => Colored(text, ConsoleColor.Red);

    public void Heading(string text)
    {
        if (Quiet)
        {
            return;
        }

        Colored(text, ConsoleColor.Cyan);
    }

    private void Colored(string text, ConsoleColor color)
    {
        if (Quiet)
        {
            return;
        }

        if (NoColor || Console.IsOutputRedirected)
        {
            Console.WriteLine(text);
            return;
        }

        ConsoleColor previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = previous;
    }

    public static string FormatSize(long bytes)
    {
        const long kb = 1024;
        const long mb = kb * 1024;
        const long gb = mb * 1024;

        return bytes switch
        {
            >= gb => $"{bytes / (double)gb:F2} GB",
            >= mb => $"{bytes / (double)mb:F2} MB",
            >= kb => $"{bytes / (double)kb:F1} KB",
            _ => $"{bytes} B",
        };
    }

    /// <summary>Prints a simple left-aligned table with a header underline.</summary>
    public void Table(IReadOnlyList<string> headers, IReadOnlyList<string[]> rows)
    {
        if (Quiet)
        {
            return;
        }

        int columns = headers.Count;
        var widths = new int[columns];
        for (int c = 0; c < columns; c++)
        {
            widths[c] = headers[c].Length;
        }

        foreach (string[] row in rows)
        {
            for (int c = 0; c < columns && c < row.Length; c++)
            {
                widths[c] = Math.Max(widths[c], row[c].Length);
            }
        }

        Console.WriteLine(FormatRow(headers, widths));
        Console.WriteLine(string.Join("  ", widths.Select(w => new string('-', w))));
        foreach (string[] row in rows)
        {
            Console.WriteLine(FormatRow(row, widths));
        }
    }

    private static string FormatRow(IReadOnlyList<string> cells, int[] widths)
    {
        var parts = new string[widths.Length];
        for (int c = 0; c < widths.Length; c++)
        {
            string cell = c < cells.Count ? cells[c] : string.Empty;
            parts[c] = cell.PadRight(widths[c]);
        }

        return string.Join("  ", parts);
    }
}
