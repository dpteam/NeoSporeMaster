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

    [SporeTest("CNV text dump includes response identity and text flags")]
    public static void TextDumpCompleteness()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();

        var unit = new CnvUnit();
        var dialog = new CnvDialog { Id = 0x11111111, Name = "d0", GroupId = 0x22222222, TypeId = 0x33333333, InstanceId = 0x44444444 };
        var response = new CnvDialogResponse { Action = 0x55555555, GroupId = 0x66666666, TypeId = 0x77777777, InstanceId = unchecked((int)0x88888888u) };
        var text = new CnvText { TableId = unchecked((int)0x99999999u), InstanceId = unchecked((int)0xAAAAAAAAu) };
        text.RequireFlags[3] = true;
        response.Texts.Add(text);
        dialog.Responses.Add(response);
        unit.Dialogs.Add(dialog);

        string script = unit.ToScript(names);

        Assert.Contains(script, "dialog d0");
        // The response identity must be present (previously omitted).
        Assert.Contains(script, "group 0x66666666");
        Assert.Contains(script, "type 0x77777777");
        Assert.Contains(script, "instance 0x88888888");
        // The per-text require flags must be present (previously omitted).
        Assert.Contains(script, "require 3");
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
