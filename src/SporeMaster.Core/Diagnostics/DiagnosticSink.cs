namespace SporeMaster.Core.Diagnostics;

public enum DiagnosticLevel
{
    Info,
    Warning,
    Error,
}

/// <summary>A single non-fatal problem or note produced while parsing or writing a package.</summary>
public sealed record Diagnostic(DiagnosticLevel Level, string Message)
{
    public override string ToString() => $"[{Level}] {Message}";
}

/// <summary>
/// Collects diagnostics while keeping the operation going. The whole library is written to
/// be "dynamically fault-tolerant": malformed input produces diagnostics instead of exceptions
/// wherever a best-effort result is still useful.
/// </summary>
public sealed class DiagnosticSink
{
    private readonly List<Diagnostic> _items = new();

    public IReadOnlyList<Diagnostic> Items => _items;

    public int ErrorCount { get; private set; }

    public int WarningCount { get; private set; }

    public bool HasProblems => ErrorCount > 0;

    public void Info(string message) => _items.Add(new Diagnostic(DiagnosticLevel.Info, message));

    public void Warn(string message)
    {
        WarningCount++;
        _items.Add(new Diagnostic(DiagnosticLevel.Warning, message));
    }

    public void Error(string message)
    {
        ErrorCount++;
        _items.Add(new Diagnostic(DiagnosticLevel.Error, message));
    }
}
