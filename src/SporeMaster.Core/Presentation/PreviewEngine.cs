using System.Text;
using SporeMaster.Core.Bitmaps;
using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Imaging;
using SporeMaster.Core.Locale;
using SporeMaster.Core.Names;
using SporeMaster.Core.Properties;
using SporeMaster.Core.RenderWare4;
using SporeMaster.Core.Textures;

namespace SporeMaster.Core.Presentation;

/// <summary>A preview of a resource, ready to be shown in a UI or the terminal.</summary>
public sealed class ResourcePreview
{
    private ResourcePreview()
    {
    }

    /// <summary>One of: empty, image, props, locale, texture, rw4, text, hex, error.</summary>
    public string Kind { get; private init; } = "empty";

    public string? Text { get; private init; }

    /// <summary>Encoded PNG bytes when the resource is (or can be rendered as) an image.</summary>
    public byte[]? PngBytes { get; private init; }

    public bool IsImage => Kind == "image" && PngBytes is not null;

    public static ResourcePreview Empty() => new() { Kind = "empty", Text = "(nothing selected)" };

    public static ResourcePreview AsText(string text, string kind = "text") => new() { Kind = kind, Text = text };

    public static ResourcePreview AsImage(byte[] png) => new() { Kind = "image", PngBytes = png };
}

/// <summary>
/// Builds human-friendly previews for resources (properties as XML, textures/bitmaps as images,
/// RenderWare 4 as a section listing, and a hex fallback). UI-free so it can be tested headlessly.
/// </summary>
public static class PreviewEngine
{
    private const int MaxTextChars = 200_000;
    private const int MaxHexBytes = 8 * 1024;

    public static ResourcePreview Build(ResourceItem item, byte[] data, NameRegistrySet names)
    {
        try
        {
            uint type = item.TypeId;

            if (ResourceTypeIds.IsProperty(type))
            {
                PropertyList list = PropertyList.Read(new MemoryStream(data));
                using var xml = new MemoryStream();
                list.WriteXml(xml, names);
                return ResourcePreview.AsText(Encoding.UTF8.GetString(xml.ToArray()), "props");
            }

            if (type == ResourceTypeIds.Locale)
            {
                LocaleTable table = LocaleTable.Read(new MemoryStream(data));
                return ResourcePreview.AsText(table.Serialize(), "locale");
            }

            if (ResourceTypeIds.IsBitmap(type))
            {
                BitmapImage bitmap = BitmapImage.Read(new MemoryStream(data), type);
                byte[] png = PngCodec.Encode(bitmap.Image);
                return ResourcePreview.AsImage(png);
            }

            if (ResourceTypeIds.IsTexture(type))
            {
                RasterTexture raster = RasterTexture.Read(new MemoryStream(data));
                string info =
                    "RasterTexture\n" +
                    $"  Size: {raster.Width} x {raster.Height}\n" +
                    $"  Format: {TextureFormats.Describe(raster.TextureFormat)} (0x{raster.TextureFormat:X8})\n" +
                    $"  Mipmaps: {raster.MipmapCount}\n" +
                    $"  Pixel width: {raster.PixelWidth}\n\n" +
                    "Use the CLI 'texture' command to convert this to DDS.";
                return ResourcePreview.AsText(info, "texture");
            }

            if (ResourceTypeIds.IsRenderWare4(type))
            {
                using RenderWare4File rw4 = RenderWare4File.Read(new MemoryStream(data));
                var builder = new StringBuilder();
                builder.Append("RenderWare 4 resource\n");
                builder.Append($"  Container: {Rw4SectionTypes.DescribeContainer(rw4.RawType)}\n");
                builder.Append($"  Sections: {rw4.SectionCount}\n\n");
                for (int i = 0; i < rw4.Sections.Count; i++)
                {
                    Rw4SectionInfo s = rw4.Sections[i];
                    builder.Append($"  [{i}] {Rw4SectionTypes.Describe(s.TypeCode)} @ 0x{s.DataOffset:X} size={s.Size} align={s.Alignment}\n");
                }

                return ResourcePreview.AsText(builder.ToString(), "rw4");
            }

            // Some resources are already standard images (e.g. the "png" type stores raw PNG data).
            if (IsPng(data))
            {
                return ResourcePreview.AsImage(data);
            }

            if (LooksLikeText(data))
            {
                string text = TextCodec.Decode(data);
                if (text.Length > MaxTextChars)
                {
                    text = text[..MaxTextChars] + "\n… (truncated)";
                }

                return ResourcePreview.AsText(text, "text");
            }

            return ResourcePreview.AsText(HexDump(data), "hex");
        }
        catch (Exception ex)
        {
            return ResourcePreview.AsText($"Preview unavailable: {ex.Message}\n\n{HexDump(data)}", "error");
        }
    }

    private static bool IsPng(byte[] data) =>
        data.Length >= 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47;

    private static bool LooksLikeText(byte[] data)
    {
        int limit = Math.Min(data.Length, 1024);
        if (limit == 0)
        {
            return true;
        }

        int printable = 0;
        for (int i = 0; i < limit; i++)
        {
            byte b = data[i];
            if (b == 0)
            {
                return false;
            }

            if (b is 9 or 10 or 13 || (b >= 32 && b < 127))
            {
                printable++;
            }
        }

        return printable >= limit * 0.9;
    }

    private static string HexDump(byte[] data)
    {
        int count = Math.Min(data.Length, MaxHexBytes);
        var builder = new StringBuilder();
        builder.Append($"Binary data ({data.Length} bytes)").Append('\n').Append('\n');

        for (int offset = 0; offset < count; offset += 16)
        {
            builder.Append(offset.ToString("X8")).Append("  ");

            var ascii = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                int index = offset + i;
                if (index < count)
                {
                    byte b = data[index];
                    builder.Append(b.ToString("X2")).Append(' ');
                    ascii.Append(b is >= 32 and < 127 ? (char)b : '.');
                }
                else
                {
                    builder.Append("   ");
                }
            }

            builder.Append(' ').Append(ascii).Append('\n');
        }

        if (data.Length > count)
        {
            builder.Append("\n… (truncated)");
        }

        return builder.ToString();
    }
}

/// <summary>UTF-8 decoding that falls back to Latin1 when the bytes are not valid UTF-8.</summary>
internal static class TextCodec
{
    public static string Decode(byte[] data)
    {
        try
        {
            return new UTF8Encoding(false, true).GetString(data);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(data);
        }
    }
}
