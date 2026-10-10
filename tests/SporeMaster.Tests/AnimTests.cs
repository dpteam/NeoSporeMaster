using System.Buffers.Binary;
using SporeMaster.Core.Anim;
using SporeMaster.Core.Dbpf;

namespace SporeMaster.Tests;

public static class AnimTests
{
    private const uint AnimType = 0xEE17C6AD;

    [SporeTest("animation header reports id, length, channels and events")]
    public static void HeaderFields()
    {
        byte[] data = BuildMinimalAnimation();
        SpAnimation anim = SpAnimation.Read(new MemoryStream(data));

        Assert.Equal(21, anim.Version);
        Assert.Equal(0x11223344, anim.Id);
        Assert.True(Math.Abs(anim.Length - 2.5f) < 1e-5, "length");
        Assert.Equal(1, anim.Channels.Count);
        Assert.Equal("root", anim.Channels[0].Name);
        Assert.Equal(3, anim.Channels[0].KeyframeCount);
        Assert.Equal(9, anim.Channels[0].ComponentCount);
        Assert.Equal(1, anim.Events.Count);
        Assert.Equal("myEvent", anim.Events[0]);
    }

    [SporeTest("bad animation magic is rejected")]
    public static void BadMagic()
    {
        byte[] data = BuildMinimalAnimation();
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0), 0xDEADBEEF);

        try
        {
            SpAnimation.Read(new MemoryStream(data));
            throw new AssertionException("expected NotAnimationException");
        }
        catch (NotAnimationException)
        {
            // expected
        }
    }

    [SporeTest("real animations parse from the test archives")]
    public static void RealAnimations()
    {
        int animations = 0;
        int channels = 0;
        int namedChannels = 0;
        int components = 0;
        int keyframes = 0;

        foreach (string packagePath in TestData.BetaPackages.Concat(TestData.FinalPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != AnimType)
                {
                    continue;
                }

                try
                {
                    SpAnimation anim = SpAnimation.Read(new MemoryStream(package.ReadEntry(entry)));
                    Assert.True(anim.Version >= SpAnimation.MinVersion, "version");
                    Assert.True(anim.Length >= 0, "length should not be negative");

                    foreach (SpAnimationChannel channel in anim.Channels)
                    {
                        channels++;
                        if (channel.Name.Length > 0)
                        {
                            namedChannels++;
                        }

                        foreach (SpAnimationComponent component in channel.Components)
                        {
                            components++;
                            foreach (SpAnimationKeyframe keyframe in component.Keyframes)
                            {
                                foreach (float value in keyframe.Values)
                                {
                                    Assert.True(
                                        !float.IsNaN(value) && !float.IsInfinity(value),
                                        "keyframe value must be finite");
                                }

                                keyframes++;
                            }
                        }
                    }

                    animations++;
                    if (animations >= 300)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    throw new AssertionException($"{entry.Key}: {ex.Message}");
                }
            }

            if (animations >= 300)
            {
                break;
            }
        }

        if (animations == 0)
        {
            throw new AssertionException("No animations found in the test archives.");
        }

        Assert.True(channels > 0, "animations were found but no channels");
        Assert.True(namedChannels > 0, "channels were found but none were named");
        Assert.True(components > 0, "channels were found but no components decoded");
        Assert.True(keyframes > 0, "components were found but no keyframes decoded");
    }

    /// <summary>Builds a minimal animation: header + one channel table + one channel + one event.</summary>
    private static byte[] BuildMinimalAnimation()
    {
        const int Size = 0x400;
        var data = new byte[Size];

        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0), SpAnimation.Magic);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), Size);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(8), 21);

        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0x110), 0x11223344);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0x120), BitConverter.SingleToInt32Bits(2.5f));

        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0x13C), 1);       // event count
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x140), 0x300);  // event table
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0x144), 1);       // channel count
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x148), 0x200);  // channel pointer table

        // Channel pointer table at 0x200 -> channel at 0x240.
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x200), 0x240);

        // Channel.
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x240), 0x4E414843); // CHAN
        "root\0"u8.CopyTo(data.AsSpan(0x248));
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0x240 + 0xD4), 3); // keyframe count
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0x240 + 0xDC), 9); // component count

        // Event record at 0x300 (0x60 bytes); name pointer at +0x40 -> "myEvent" at 0x380.
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x300 + 0x40), 0x380);
        "myEvent\0"u8.CopyTo(data.AsSpan(0x380));

        return data;
    }
}
