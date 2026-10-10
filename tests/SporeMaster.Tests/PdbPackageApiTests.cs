using SporeMaster.Core.Pdb;

namespace SporeMaster.Tests;

public static class PdbPackageApiTests
{
    [SporeTest("PDB exposes the package (ResourceMan) API symbols when present")]
    public static void ResourceManApi()
    {
        string? path = TestData.Combine("refs", "SporeGames", "SporeBeta190208", "SporeApp.pdb");
        if (path is null || !File.Exists(path))
        {
            Console.WriteLine("SporeApp.pdb not present; skipping ResourceMan API check.");
            return;
        }

        using PdbFile pdb = PdbFile.Open(path);
        IReadOnlyList<PdbSymbol> symbols = pdb.ReadSymbols();
        var names = new HashSet<string>(symbols.Select(s => s.Name));

        // Key package-writer/reader entry points we rely on for the repack story.
        string[] expected =
        {
            "DatabasePackedFile", "PFHoleTable", "CreateIndex", "CreateRecord", "DeleteRecord",
            "CompressData", "ShouldRecordBeCompressed", "VerifyHeaderRecordIntegrity",
        };

        var blob = string.Join('\n', names);
        foreach (string token in expected)
        {
            Assert.True(blob.Contains(token, StringComparison.Ordinal), $"expected to find '{token}' in the PDB");
        }
    }
}
