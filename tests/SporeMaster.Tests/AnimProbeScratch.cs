using System.Buffers.Binary;
using SporeMaster.Core.Dbpf;

namespace SporeMaster.Tests;

public static class AnimProbeScratch
{
    private const uint AnimType = 0xEE17C6AD;

    [SporeTest("scratch probe .anim headers")]
    public static void Probe()
    {
        int total = 0;
        int magicOk = 0;
        var versions = new SortedDictionary<int, int>();

        foreach (string packagePath in TestData.BetaPackages.Concat(TestData.FinalPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != AnimType)
                {
                    continue;
                }

                total++;
                byte[] data = package.ReadEntry(entry);
                if (data.Length < 0x160)
                {
                    continue;
                }

                uint magic = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0));
                if (magic == 0x4D494E41)
                {
                    magicOk++;
                }

                int version = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(8));
                versions[version] = versions.GetValueOrDefault(version) + 1;

                if (total <= 5)
                {
                    uint size = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4));
                    int id = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0x110));
                    float length = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0x120)));
                    int eventCount = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0x13C));
                    uint eventPtr = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0x140));
                    int channelCount = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0x144));
                    uint channelPtr = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0x148));
                    Console.WriteLine(
                        $"{entry.Key} len={data.Length} magic=0x{magic:X8} size={size} ver={version} " +
                        $"id=0x{id:X8} length={length:0.###} events={eventCount}@{eventPtr} channels={channelCount}@{channelPtr}");
                }
            }
        }

        Console.WriteLine($"total={total} magicOk={magicOk} versions=[{string.Join(",", versions.Select(kv => $"{kv.Key}:{kv.Value}"))}]");
    }
}
