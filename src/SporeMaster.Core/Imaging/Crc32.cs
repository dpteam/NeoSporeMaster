namespace SporeMaster.Core.Imaging;

/// <summary>The CRC-32 (IEEE 802.3) used by the PNG file format.</summary>
internal static class Crc32
{
    private static readonly uint[] Table = BuildTable();

    private static uint[] BuildTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }

    public static uint Compute(byte[] data, int offset, int count) => Update(0xFFFFFFFFu, data, offset, count) ^ 0xFFFFFFFFu;

    public static uint Update(uint crc, byte[] data, int offset, int count)
    {
        uint value = crc;
        for (int i = 0; i < count; i++)
        {
            value = Table[(value ^ data[offset + i]) & 0xFF] ^ (value >> 8);
        }

        return value;
    }
}
