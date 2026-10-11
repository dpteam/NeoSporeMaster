using SporeMaster.Core.Cell;
using SporeMaster.Core.Dbpf;

namespace SporeMaster.Tests;

public static class CellMapTests
{
    [SporeTest("cell background map round-trips byte-exactly")]
    public static void BackgroundMap()
    {
        RoundTripAll(0x612B3191, data => CellBackgroundMapFile.Read(new MemoryStream(data)), f => f.Write);
    }

    [SporeTest("cell effect map round-trips byte-exactly")]
    public static void EffectMap()
    {
        RoundTripAll(0x433FB70C, data => CellEffectMapFile.Read(new MemoryStream(data)), f => f.Write);
    }

    [SporeTest("cell random creature table round-trips byte-exactly")]
    public static void RandomCreature()
    {
        RoundTripAll(0xF9C3D770, data => CellRandomCreatureFile.Read(new MemoryStream(data)), f => f.Write);
    }

    [SporeTest("cell background map rejects a bad offset")]
    public static void BadOffset()
    {
        var data = new byte[8];
        BitConverter.GetBytes(0).CopyTo(data, 0);   // count
        BitConverter.GetBytes(12).CopyTo(data, 4);  // wrong offset
        bool threw = false;
        try
        {
            CellBackgroundMapFile.Read(new MemoryStream(data));
        }
        catch (InvalidDataException)
        {
            threw = true;
        }

        Assert.True(threw, "offset 12 should be rejected");
    }

    [SporeTest("cell structure round-trips byte-exactly (release)")]
    public static void StructureRoundTrip()
    {
        RoundTripRelease(0x4B9EF6DC, data => CellStructureFile.Read(new MemoryStream(data)), f => f.Write);
    }

    [SporeTest("cell populate round-trips byte-exactly (release)")]
    public static void PopulateRoundTrip()
    {
        RoundTripRelease(0xDA141C1B, data => CellPopulateFile.Read(new MemoryStream(data)), f => f.Write);
    }

    private static void RoundTripAll<T>(
        uint typeId,
        Func<byte[], T> read,
        Func<T, Action<Stream>> writer)
    {
        RoundTripCore(typeId, read, writer, TestData.BetaPackages.Concat(TestData.FinalPackages));
    }

    private static void RoundTripRelease<T>(
        uint typeId,
        Func<byte[], T> read,
        Func<T, Action<Stream>> writer)
    {
        // The beta variants of structure/populate use a different layout, so only check release data.
        RoundTripCore(typeId, read, writer, TestData.FinalPackages);
    }

    private static void RoundTripCore<T>(
        uint typeId,
        Func<byte[], T> read,
        Func<T, Action<Stream>> writer,
        IEnumerable<string> packages)
    {
        int tested = 0;
        foreach (string packagePath in packages)
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != typeId)
                {
                    continue;
                }

                byte[] original = package.ReadEntry(entry);
                T parsed = read(original);
                using var buffer = new MemoryStream();
                writer(parsed)(buffer);
                Assert.True(
                    original.AsSpan().SequenceEqual(buffer.ToArray()),
                    $"type 0x{typeId:X8} {entry.Key} did not round-trip byte-exactly");

                tested++;
                if (tested >= 20)
                {
                    return;
                }
            }
        }

        if (tested == 0)
        {
            Console.WriteLine($"No resources of type 0x{typeId:X8} found; skipping.");
        }
    }
}
