using SporeMaster.Core.Arithmetica;
using SporeMaster.Core.Dbpf;

namespace SporeMaster.Tests;

public static class ArithmeticaTests
{
    private const uint ArthType = 0x0472329B;

    [SporeTest("Arithmetica reads and writes back byte-exactly")]
    public static void RoundTrip()
    {
        int tested = 0;

        foreach (string packagePath in TestData.FinalPackages.Concat(TestData.BetaPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != ArthType)
                {
                    continue;
                }

                byte[] original = package.ReadEntry(entry);
                ArithmeticaFile arth = ArithmeticaFile.Read(new MemoryStream(original));

                using var buffer = new MemoryStream();
                arth.Write(buffer);
                Assert.True(
                    original.AsSpan().SequenceEqual(buffer.ToArray()),
                    $"Arithmetica {entry.Key} did not round-trip byte-exactly");

                Assert.True(arth.Operations.Count > 0, "Arithmetica has operations");

                tested++;
                if (tested >= 20)
                {
                    return;
                }
            }
        }

        if (tested == 0)
        {
            Console.WriteLine("No Arithmetica resources found; skipping.");
        }
    }

    [SporeTest("Arithmetica text dump lists functions and opcodes")]
    public static void TextDump()
    {
        var arth = new ArithmeticaFile();
        arth.Strings.Add("main");
        arth.Functions.Add(new ArithmeticaFunction { StringIndex = 0, FirstOpIndex = 0 });
        arth.Operations.Add(new ArithmeticaOperation { Line = 1, OpCode = 5, Value = 2f }); // push
        arth.Operations.Add(new ArithmeticaOperation { Line = 1, OpCode = 24 });           // return

        string text = arth.ToText();
        Assert.Contains(text, "main -> op 0");
        Assert.Contains(text, "push value=2");
        Assert.Contains(text, "return");
    }

    [SporeTest("Arithmetica rejects a wrong magic")]
    public static void BadMagic()
    {
        var data = new byte[20];
        data[7] = 0x01; // wrong magic in the second dword
        bool threw = false;
        try
        {
            ArithmeticaFile.Read(new MemoryStream(data));
        }
        catch (InvalidDataException)
        {
            threw = true;
        }

        Assert.True(threw, "a wrong magic should be rejected");
    }
}
