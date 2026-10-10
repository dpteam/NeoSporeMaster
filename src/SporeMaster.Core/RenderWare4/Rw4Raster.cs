using SporeMaster.Core.IO;

namespace SporeMaster.Core.RenderWare4;

/// <summary>
/// A RenderWare 4 raster section (type <c>0x20003</c>): the metadata of an embedded texture (D3D
/// format, dimensions, mip levels) and a reference to the section holding its pixel data.
/// </summary>
public sealed class Rw4Raster
{
    /// <summary>Direct3D texture format (a <c>D3DFMT_*</c> constant or a DXT four-character code).</summary>
    public int TextureFormat { get; set; }

    public int TextureFlags { get; set; }

    public int VolumeDepth { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int MipmapLevels { get; set; }

    /// <summary>Section index of the base resource holding the pixel data, or -1.</summary>
    public int DataSectionIndex { get; set; } = -1;

    /// <summary>A readable name for <see cref="TextureFormat"/> (DXT codes or common D3D formats).</summary>
    public string FormatName
    {
        get
        {
            // DXT/ATI four-character codes are stored as little-endian FourCC values.
            string fourCc = new string(new[]
            {
                (char)(TextureFormat & 0xFF),
                (char)((TextureFormat >> 8) & 0xFF),
                (char)((TextureFormat >> 16) & 0xFF),
                (char)((TextureFormat >> 24) & 0xFF),
            });

            if (fourCc is "DXT1" or "DXT3" or "DXT5" or "ATI2" or "ATI1")
            {
                return fourCc;
            }

            return TextureFormat switch
            {
                21 => "A8R8G8B8",
                20 => "A8R8G8B8", // close enough; both are 32-bit ARGB
                22 => "X8R8G8B8",
                23 => "R5G6B5",
                24 => "X1R5G5B5",
                25 => "A1R5G5B5",
                26 => "A4R4G4B4",
                28 => "R8G8B8",
                50 => "L8",
                51 => "A8L8",
                0 => "(none)",
                _ => $"0x{TextureFormat:X}",
            };
        }
    }

    internal static Rw4Raster Read(Stream s, long offset)
    {
        s.Seek(offset, SeekOrigin.Begin);
        var raster = new Rw4Raster
        {
            TextureFormat = s.ReadI32(),
            TextureFlags = s.ReadU16(),
            VolumeDepth = s.ReadU16(),
        };

        s.ReadI32(); // dxBaseTexture (a runtime pointer, unused in the file)
        raster.Width = s.ReadU16();
        raster.Height = s.ReadU16();
        s.ReadU8(); // field_10
        raster.MipmapLevels = s.ReadU8();
        s.ReadU16(); // padding
        s.ReadI32(); // field_14
        s.ReadI32(); // field_18
        raster.DataSectionIndex = s.ReadI32();
        return raster;
    }
}
