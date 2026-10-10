namespace SporeMaster.Core.Imaging;

/// <summary>
/// Decodes raw image bytes by sniffing the container: PNG, Windows icon/cursor (ICO/CUR) or TGA.
/// Used by the CLI and GUI to preview/convert the <c>tga</c> and <c>cur</c> resource types.
/// </summary>
public static class ImageFormats
{
    public static RgbaImage Decode(byte[] data, int index = -1)
    {
        if (PngCodec.LooksLikePng(data))
        {
            return PngCodec.Decode(data);
        }

        if (IcoCodec.LooksLikeIco(data))
        {
            return IcoCodec.Decode(data, index);
        }

        return TgaCodec.Decode(data);
    }
}
