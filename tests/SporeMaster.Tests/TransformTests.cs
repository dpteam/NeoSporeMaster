using System.Xml.Linq;
using SporeMaster.Core.Names;
using SporeMaster.Core.Properties;

namespace SporeMaster.Tests;

public static class TransformTests
{
    [SporeTest("transform round-trips byte-exactly through binary")]
    public static void BinaryRoundTrip()
    {
        var source = new TransformProperty();
        source.Add(new TransformProperty.TransformValue
        {
            Flags = 0x0300,
            Count = 1,
            Offset = new[] { 1f, 2f, 3f },
            Scale = 4f,
            Rotation = new[] { 1f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f },
        });

        using var buffer = new MemoryStream();
        source.WriteElement(buffer, 0, false);
        Assert.Equal(56, (int)buffer.Length);

        buffer.Position = 0;
        var reborn = new TransformProperty();
        reborn.ReadElement(buffer, false);

        TransformProperty.TransformValue value = reborn.GetTransform(0);
        Assert.Equal(0x0300, value.Flags);
        Assert.Equal(1, value.Count);
        Assert.Equal(1f, value.Offset[0]);
        Assert.Equal(3f, value.Offset[2]);
        Assert.Equal(4f, value.Scale);
        Assert.Equal(1f, value.Rotation[4]);
    }

    [SporeTest("transform reads and writes the XML form")]
    public static void XmlRoundTrip()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();

        var element = new XElement("transform",
            new XAttribute("flags", "0x0300"),
            new XAttribute("offset", "1,2,3"),
            new XAttribute("scale", "2.5"),
            new XAttribute("row1", "1,0,0"),
            new XAttribute("row2", "0,1,0"),
            new XAttribute("row3", "0,0,1"));

        var property = new TransformProperty();
        property.ReadXmlElement(element, names);

        TransformProperty.TransformValue value = property.GetTransform(0);
        Assert.Equal(0x0300, value.Flags);
        Assert.Equal(2f, value.Offset[1]);
        Assert.Equal(2.5f, value.Scale);
        Assert.Equal(1f, value.Rotation[0]);
        Assert.Equal(1f, value.Rotation[8]);
    }

    [SporeTest("transform accepts SporeModder rotateXYZ form")]
    public static void RotateXyz()
    {
        NameRegistrySet names = NameRegistrySet.LoadEmbedded();

        // Zero rotation must produce the identity basis.
        var element = new XElement("transform", new XAttribute("rotateXYZ", "0,0,0"));
        var property = new TransformProperty();
        property.ReadXmlElement(element, names);

        TransformProperty.TransformValue value = property.GetTransform(0);
        Assert.Equal(1f, value.Rotation[0]);
        Assert.Equal(1f, value.Rotation[4]);
        Assert.Equal(1f, value.Rotation[8]);
        Assert.Equal(0f, value.Rotation[1]);
        Assert.Equal(0f, value.Rotation[6]);
    }
}
