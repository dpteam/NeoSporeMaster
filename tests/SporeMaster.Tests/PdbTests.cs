using System.Text;
using SporeMaster.Core.Binary;
using SporeMaster.Core.Pdb;

namespace SporeMaster.Tests;

public static class PdbTests
{
    [SporeTest("MSVC string literals are decoded")]
    public static void StringLiterals()
    {
        Assert.True(MsvcStringLiteral.TryDecode("??_C@_04CLCEDBPF@time?$AA@", out string time));
        Assert.Equal("time", time);

        Assert.True(MsvcStringLiteral.TryDecode("??_C@_0N@KFBAOAAA@SP_Simulator?$AA@", out string simulator));
        Assert.Equal("SP_Simulator", simulator);

        // ?1 is '/' and the total byte length includes the trailing null.
        Assert.True(MsvcStringLiteral.TryDecode("??_C@_0BE@JPILMDAK@SP_Simulator?1vector?$AA@", out string path));
        Assert.Equal("SP_Simulator/vector", path);

        // A Windows path: ?2 = '\\' and ?9 = '-'; size 0x20 = 31 chars + null.
        Assert.True(MsvcStringLiteral.TryDecode("??_C@_0CA@JHNEJLPN@?2BuildAgent?2cm4?9build312?9spore?2C?$AA@", out string win));
        Assert.Equal("\\BuildAgent\\cm4-build312-spore\\C", win);

        // Punctuation: ?3 = ':' and ?5 = ' '.
        Assert.True(MsvcStringLiteral.TryDecode("??_C@_0O@MBAHBKHG@uninitialized?$AA@", out string word));
        Assert.Equal("uninitialized", word);

        Assert.False(MsvcStringLiteral.TryDecode("?notAStringLiteral", out _));
    }

    [SporeTest("strings extractor finds ASCII and UTF-16 strings")]
    public static void StringsExtractorWorks()
    {
        var data = new List<byte>();
        data.AddRange(Encoding.ASCII.GetBytes("hello world"));
        data.Add(0);
        data.Add(1);
        data.Add(2);
        data.AddRange(Encoding.Unicode.GetBytes("wide string"));
        data.Add(0);
        data.AddRange(new byte[] { 0xFF, 0x00, 0xFE });
        data.AddRange(Encoding.ASCII.GetBytes("ab")); // too short

        List<BinaryString> results = StringsExtractor.Extract(data.ToArray(), minimumLength: 4);

        Assert.True(results.Any(s => s.Value == "hello world" && !s.Unicode), "ascii string");
        Assert.True(results.Any(s => s.Value == "wide string" && s.Unicode), "unicode string");
        Assert.False(results.Any(s => s.Value == "ab"), "short strings are skipped");

        List<BinaryString> filtered = StringsExtractor.Extract(data.ToArray(), minimumLength: 4, contains: "wide");
        Assert.Equal(1, filtered.Count);
    }

    [SporeTest("MSF container exposes its streams")]
    public static void MsfContainer()
    {
        byte[] msf = BuildMinimalMsf();
        using var stream = new MemoryStream(msf);
        MsfFile file = MsfFile.Read(stream);

        Assert.Equal(1024u, file.PageSize);
        Assert.Equal(4u, file.BlockCount);
        Assert.Equal(2, file.StreamCount);
        Assert.True(file.ReadStream(1).AsSpan().SequenceEqual("hello"u8), "stream 1 payload");
        Assert.Equal(0, file.ReadStream(0).Length);

        // Bad magic is rejected.
        try
        {
            using var bad = new MemoryStream(new byte[128]);
            MsfFile.Read(bad);
            throw new AssertionException("expected NotMsfException");
        }
        catch (NotMsfException)
        {
            // expected
        }
    }

    [SporeTest("real Spore PDB yields symbols and string literals when present")]
    public static void RealPdb()
    {
        string? path = TestData.Combine("refs", "SporeGames", "SporeBeta190208", "SporeApp.pdb");
        if (path is null || !File.Exists(path))
        {
            Console.WriteLine("SporeApp.pdb not present; skipping real-PDB check.");
            return;
        }

        using PdbFile pdb = PdbFile.Open(path);
        Assert.Equal(1024u, pdb.Container.PageSize);

        IReadOnlyList<PdbModule> modules = pdb.ReadModules();
        Assert.True(modules.Count > 0, "modules should be found");

        IReadOnlyList<PdbSymbol> symbols = pdb.ReadSymbols();
        Assert.True(symbols.Count > 10_000, $"expected many symbols, got {symbols.Count}");

        bool foundLiteral = false;
        foreach (PdbSymbol symbol in symbols)
        {
            if (MsvcStringLiteral.IsStringLiteral(symbol.Name)
                && MsvcStringLiteral.TryDecode(symbol.Name, out string decoded)
                && decoded == "SP_Simulator")
            {
                foundLiteral = true;
                break;
            }
        }

        Assert.True(foundLiteral, "expected to decode the 'SP_Simulator' string literal");
    }

    /// <summary>
    /// Builds a minimal but valid MSF 7.00 file with two streams: stream 0 empty, stream 1 = "hello".
    /// </summary>
    private static byte[] BuildMinimalMsf()
    {
        const int PageSize = 1024;
        const int PageCount = 4;

        var file = new byte[PageSize * PageCount];

        // Superblock.
        byte[] magic = { 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74, 0x20, 0x43, 0x2F, 0x43, 0x2B, 0x2B, 0x20,
                         0x4D, 0x53, 0x46, 0x20, 0x37, 0x2E, 0x30, 0x30, 0x0D, 0x0A, 0x1A, 0x44, 0x53, 0x00, 0x00, 0x00 };
        Array.Copy(magic, file, magic.Length);
        WriteU32(file, 0x20, PageSize);   // block size
        WriteU32(file, 0x24, 2);          // free block map
        WriteU32(file, 0x28, PageCount);  // number of blocks
        WriteU32(file, 0x2C, 16);         // directory bytes
        WriteU32(file, 0x30, 0);          // unknown
        WriteU32(file, 0x34, 3);          // block map address

        // Page 1: stream 1 data.
        "hello"u8.ToArray().CopyTo(file, PageSize * 1);

        // Page 2: the stream directory.
        int dir = PageSize * 2;
        WriteU32(file, dir + 0, 2);   // stream count
        WriteU32(file, dir + 4, 0);   // stream 0 size (empty)
        WriteU32(file, dir + 8, 5);   // stream 1 size
        WriteU32(file, dir + 12, 1);  // stream 1 block list -> block 1

        // Page 3: the block map (lists the directory block).
        WriteU32(file, PageSize * 3, 2);

        return file;
    }

    private static void WriteU32(byte[] data, int offset, uint value) =>
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), value);
}
