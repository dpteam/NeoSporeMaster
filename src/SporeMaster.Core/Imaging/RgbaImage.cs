namespace SporeMaster.Core.Imaging;

/// <summary>
/// A simple 8-bit RGBA image buffer used as the common currency between Spore bitmap files
/// and PNG. Pixels are stored row-major as R, G, B, A bytes.
/// </summary>
public sealed class RgbaImage
{
    public RgbaImage(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Image dimensions must be positive.");
        }

        Width = width;
        Height = height;
        Pixels = new byte[width * height * 4];
    }

    public RgbaImage(int width, int height, byte[] pixels)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Image dimensions must be positive.");
        }

        if (pixels.Length != width * height * 4)
        {
            throw new ArgumentException("Pixel buffer size does not match the dimensions.", nameof(pixels));
        }

        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public int Width { get; }

    public int Height { get; }

    /// <summary>Row-major RGBA bytes (4 per pixel).</summary>
    public byte[] Pixels { get; }

    public void SetPixel(int x, int y, byte r, byte g, byte b, byte a)
    {
        int i = ((y * Width) + x) * 4;
        Pixels[i] = r;
        Pixels[i + 1] = g;
        Pixels[i + 2] = b;
        Pixels[i + 3] = a;
    }

    public (byte R, byte G, byte B, byte A) GetPixel(int x, int y)
    {
        int i = ((y * Width) + x) * 4;
        return (Pixels[i], Pixels[i + 1], Pixels[i + 2], Pixels[i + 3]);
    }
}
