namespace SporeMaster.Tests;

public static class Program
{
    public static int Main(string[] args)
    {
        string? filter = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is "--filter" or "-f" && i + 1 < args.Length)
            {
                filter = args[i + 1];
            }
        }

        Console.WriteLine("SporeMaster 3 test suite");
        Console.WriteLine($"Repository root: {TestData.RepoRoot ?? "<not found>"}");
        Console.WriteLine();
        return TestRunner.Run(filter);
    }
}
