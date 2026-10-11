using SporeMaster.Core.Dbpf;

namespace SporeMaster.Tests;

public static class ConversionSuffixTests
{
    [SporeTest("converted-png detection distinguishes native png from converted bitmaps")]
    public static void DetectConvertedPng()
    {
        // Native png resources: ".png" is the resource type, so it must not be stripped.
        Assert.False(ConversionSuffix.IsConvertedPng("common~/bp2-button-icon.png"));
        Assert.False(ConversionSuffix.IsConvertedPng("CreaturePartIcons~/ce_mouthTech_01.png"));
        Assert.False(ConversionSuffix.IsConvertedPng("group\\name.PNG"));

        // Converted bitmap images: the part before ".png" still has a type extension.
        Assert.True(ConversionSuffix.IsConvertedPng("group~/thing.32bitImage.png"));
        Assert.True(ConversionSuffix.IsConvertedPng("group~/thing.8bitImage.png"));
        Assert.True(ConversionSuffix.IsConvertedPng("a/b/c.48bitImage.png"));

        // Not png at all.
        Assert.False(ConversionSuffix.IsConvertedPng("group~/thing.prop"));
        Assert.False(ConversionSuffix.IsConvertedPng("group~/thing.prop.xml"));
    }

    [SporeTest("repacking a named unpack keeps native png resources")]
    public static void NativePngSurvivesRepack()
    {
        string? packagePath = TestData.FinalPackages.FirstOrDefault(p => p.EndsWith("BP2_Data.package"));
        if (packagePath is null)
        {
            Console.WriteLine("BP2_Data.package not present; skipping.");
            return;
        }

        int pngCount = 0;
        using (DatabasePackedFile package = DatabasePackedFile.Open(packagePath))
        {
            pngCount = package.Entries.Count(e => e.Key.TypeId == 0x2F7D0004);
        }

        Assert.True(pngCount > 0, "BP2_Data should contain native png resources");

        // Every native png path must be recognised as a *native* png (not a converted bitmap), so the
        // packer keeps its ".png" type instead of resolving a key-less name and dropping the file.
        using (DatabasePackedFile package = DatabasePackedFile.Open(packagePath))
        {
            foreach (DbpfEntry entry in package.Entries.Where(e => e.Key.TypeId == 0x2F7D0004))
            {
                string name = $"group~/{entry.Key.InstanceId:X8}.png";
                Assert.False(ConversionSuffix.IsConvertedPng(name), name);
            }
        }
    }
}
