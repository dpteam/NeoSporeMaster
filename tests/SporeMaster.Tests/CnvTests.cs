using SporeMaster.Core.Cnv;
using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Names;

namespace SporeMaster.Tests;

public static class CnvTests
{
    private const uint CnvType = 0x055ADA24;

    [SporeTest("CNV binary round-trip is byte-exact")]
    public static void BinaryRoundTrip()
    {
        int tested = 0;

        foreach (string path in TestData.FinalPackages)
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(path);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != CnvType)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                CnvUnit unit = CnvUnit.Read(new MemoryStream(data));

                using var stream = new MemoryStream();
                unit.Write(stream);
                Assert.True(
                    data.AsSpan().SequenceEqual(stream.ToArray()),
                    $"CNV binary round-trip changed bytes for {entry.Key} in {Path.GetFileName(path)}");

                tested++;
                if (tested >= 30)
                {
                    break;
                }
            }

            if (tested >= 30)
            {
                break;
            }
        }

        if (tested == 0)
        {
            throw new AssertionException("No CNV resources found.");
        }
    }

    [SporeTest("CNV text dump is produced")]
    public static void TextDump()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();

        foreach (string path in TestData.FinalPackages)
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(path);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != CnvType)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                CnvUnit unit = CnvUnit.Read(new MemoryStream(data));
                string text = unit.ToScript(names);
                Assert.True(text.Length > 0, "the text dump should not be empty");
                return;
            }
        }

        throw new AssertionException("No CNV resources found.");
    }
}
