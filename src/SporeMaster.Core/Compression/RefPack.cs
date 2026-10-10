namespace SporeMaster.Core.Compression;

/// <summary>
/// EA "RefPack" (a.k.a. QFS) compression, as used by Spore .package files.
/// </summary>
/// <remarks>
/// The on-disk stream produced by <see cref="Compress"/> begins with the two-byte
/// marker <c>10 FB</c> followed by a three-byte little-endian uncompressed size and the
/// command stream. This matches SporeModder FX (<c>RefPackCompression</c>) and the
/// legacy SporeMaster readers. It is a faithful C# port, including the original
/// compressor's quirks, so output is byte-compatible with the reference tools.
/// </remarks>
public static class RefPack
{
    /// <summary>REFPCK signature marker; the first byte is <c>0x10</c> or <c>0x50</c>.</summary>
    public const byte Signature0 = 0x10;

    public const byte Signature1 = 0xFB;

    private static readonly int[] CrcTable =
    {
        0x0000, 0xc0c1, 0xc181, 0x0140, 0xc301, 0x03c0, 0x0280, 0xc241,
        0xc601, 0x06c0, 0x0780, 0xc741, 0x0500, 0xc5c1, 0xc481, 0x0440,
        0xcc01, 0x0cc0, 0x0d80, 0xcd41, 0x0f00, 0xcfc1, 0xce81, 0x0e40,
        0x0a00, 0xcac1, 0xcb81, 0x0b40, 0xc901, 0x09c0, 0x0880, 0xc841,
        0xd801, 0x18c0, 0x1980, 0xd941, 0x1b00, 0xdbc1, 0xda81, 0x1a40,
        0x1e00, 0xdec1, 0xdf81, 0x1f40, 0xdd01, 0x1dc0, 0x1c80, 0xdc41,
        0x1400, 0xd4c1, 0xd581, 0x1540, 0xd701, 0x17c0, 0x1680, 0xd641,
        0xd201, 0x12c0, 0x1380, 0xd341, 0x1100, 0xd1c1, 0xd081, 0x1040,
        0xf001, 0x30c0, 0x3180, 0xf141, 0x3300, 0xf3c1, 0xf281, 0x3240,
        0x3600, 0xf6c1, 0xf781, 0x3740, 0xf501, 0x35c0, 0x3480, 0xf441,
        0x3c00, 0xfcc1, 0xfd81, 0x3d40, 0xff01, 0x3fc0, 0x3e80, 0xfe41,
        0xfa01, 0x3ac0, 0x3b80, 0xfb41, 0x3900, 0xf9c1, 0xf881, 0x3840,
        0x2800, 0xe8c1, 0xe981, 0x2940, 0xeb01, 0x2bc0, 0x2a80, 0xea41,
        0xee01, 0x2ec0, 0x2f80, 0xef41, 0x2d00, 0xedc1, 0xec81, 0x2c40,
        0xe401, 0x24c0, 0x2580, 0xe541, 0x2700, 0xe7c1, 0xe681, 0x2640,
        0x2200, 0xe2c1, 0xe381, 0x2340, 0xe101, 0x21c0, 0x2080, 0xe041,
        0xa001, 0x60c0, 0x6180, 0xa141, 0x6300, 0xa3c1, 0xa281, 0x6240,
        0x6600, 0xa6c1, 0xa781, 0x6740, 0xa501, 0x65c0, 0x6480, 0xa441,
        0x6c00, 0xacc1, 0xad81, 0x6d40, 0xaf01, 0x6fc0, 0x6e80, 0xae41,
        0xaa01, 0x6ac0, 0x6b80, 0xab41, 0x6900, 0xa9c1, 0xa881, 0x6840,
        0x7800, 0xb8c1, 0xb981, 0x7940, 0xbb01, 0x7bc0, 0x7a80, 0xba41,
        0xbe01, 0x7ec0, 0x7f80, 0xbf41, 0x7d00, 0xbdc1, 0xbc81, 0x7c40,
        0xb401, 0x74c0, 0x7580, 0xb541, 0x7700, 0xb7c1, 0xb681, 0x7640,
        0x7200, 0xb2c1, 0xb381, 0x7340, 0xb101, 0x71c0, 0x7080, 0xb041,
        0x5000, 0x90c1, 0x9181, 0x5140, 0x9301, 0x53c0, 0x5280, 0x9241,
        0x9601, 0x56c0, 0x5780, 0x9741, 0x5500, 0x95c1, 0x9481, 0x5440,
        0x9c01, 0x5cc0, 0x5d80, 0x9d41, 0x5f00, 0x9fc1, 0x9e81, 0x5e40,
        0x5a00, 0x9ac1, 0x9b81, 0x5b40, 0x9901, 0x59c0, 0x5880, 0x9841,
        0x8801, 0x48c0, 0x4980, 0x8941, 0x4b00, 0x8bc1, 0x8a81, 0x4a40,
        0x4e00, 0x8ec1, 0x8f81, 0x4f40, 0x8d01, 0x4dc0, 0x4c80, 0x8c41,
        0x4400, 0x84c1, 0x8581, 0x4540, 0x8701, 0x47c0, 0x4680, 0x8641,
        0x8201, 0x42c0, 0x4380, 0x8341, 0x4100, 0x81c1, 0x8081, 0x4040,
    };

    /// <summary>Returns true when <paramref name="data"/> starts with a RefPack marker.</summary>
    public static bool LooksCompressed(ReadOnlySpan<byte> data)
    {
        if (data.Length < 2)
        {
            return false;
        }

        byte c0 = data[0];
        return data[1] == Signature1 && (c0 == Signature0 || c0 == 0x50);
    }

    /// <summary>
    /// Reads the uncompressed size from a RefPack stream without decompressing it.
    /// Returns -1 when the buffer is not a recognised RefPack stream.
    /// </summary>
    public static int GetDecompressedSize(ReadOnlySpan<byte> data)
    {
        if (!LooksCompressed(data) || data.Length < 5)
        {
            return -1;
        }

        return (data[2] << 16) | (data[3] << 8) | data[4];
    }

    /// <summary>
    /// Decompresses a RefPack stream. <paramref name="expectedSize"/> is the size recorded
    /// in the DBPF index; when it is zero the size embedded in the stream is used.
    /// </summary>
    public static byte[] Decompress(ReadOnlySpan<byte> input, int expectedSize = 0)
    {
        if (input.Length < 5)
        {
            throw new InvalidDataException("RefPack stream is too short.");
        }

        int pin = 0;
        byte cType = input[pin++];
        pin++; // second signature byte (0xFB)

        if (cType != Signature0 && cType != 0x50)
        {
            throw new InvalidDataException($"Unsupported RefPack signature 0x{cType:X2}.");
        }

        int embeddedSize = (input[pin++] & 0xFF) << 16 | (input[pin++] & 0xFF) << 8 | input[pin++] & 0xFF;
        int outSize = expectedSize > 0 ? expectedSize : embeddedSize;

        byte[] output = new byte[outSize];
        int size = 0;

        while (size < outSize)
        {
            if (pin >= input.Length)
            {
                throw new InvalidDataException("RefPack stream ended before the output buffer was filled.");
            }

            int numPlainData;
            int numToCopy;
            int copyOffset;

            int controlChar = input[pin++] & 0xFF;

            if (controlChar >= 252)
            {
                numPlainData = controlChar & 0x03;
                numToCopy = 0;
                copyOffset = 0;
            }
            else if (controlChar >= 224)
            {
                numPlainData = ((controlChar & 0x1F) << 2) + 4;
                numToCopy = 0;
                copyOffset = 0;
            }
            else if (controlChar >= 192)
            {
                byte b1 = input[pin++];
                byte b2 = input[pin++];
                byte b3 = input[pin++];
                numPlainData = controlChar & 0x03;
                numToCopy = ((controlChar & 0x0C) << 6) + b3 + 5;
                copyOffset = ((controlChar & 0x10) << 12) + (b1 << 8) + b2 + 1;
            }
            else if (controlChar >= 128)
            {
                byte b1 = input[pin++];
                byte b2 = input[pin++];
                numPlainData = (b1 >> 6) & 0x03;
                numToCopy = (controlChar & 0x3F) + 4;
                copyOffset = ((b1 & 0x3F) << 8) + b2 + 1;
            }
            else
            {
                byte b1 = input[pin++];
                numPlainData = controlChar & 0x03;
                numToCopy = ((controlChar & 0x1C) >> 2) + 3;
                copyOffset = ((controlChar & 0x60) << 3) + b1 + 1;
            }

            if (numPlainData > 0)
            {
                if (pin + numPlainData > input.Length || size + numPlainData > outSize)
                {
                    throw new InvalidDataException("RefPack literal run overflows the buffers.");
                }

                input.Slice(pin, numPlainData).CopyTo(output.AsSpan(size));
                pin += numPlainData;
                size += numPlainData;
            }

            if (numToCopy > 0)
            {
                if (copyOffset <= 0 || size - copyOffset < 0 || size + numToCopy > outSize)
                {
                    throw new InvalidDataException("RefPack back-reference is out of range.");
                }

                if (numToCopy > copyOffset)
                {
                    // Overlapping copy must be performed byte by byte (RLE semantics).
                    for (int c = 0; c < numToCopy; c++, size++)
                    {
                        output[size] = output[size - copyOffset];
                    }
                }
                else
                {
                    Array.Copy(output, size - copyOffset, output, size, numToCopy);
                    size += numToCopy;
                }
            }
        }

        return output;
    }

    /// <summary>Compresses <paramref name="input"/> into a RefPack stream (with the 10 FB header).</summary>
    public static byte[] Compress(byte[] input)
    {
        ArgumentNullException.ThrowIfNull(input);

        int len = input.Length;
        byte[] inBuf = new byte[len + 2];
        Array.Copy(input, inBuf, len);

        byte[] outData = new byte[(len * 2) + 8192];
        int to = 0;

        for (int i = 0; i < 3; i++, to++)
        {
            outData[to] = (byte)((len >> ((2 - i) * 8)) & 0xFF);
        }

        int[] hashtbl = new int[65536];
        int[] link = new int[131072];
        Array.Fill(hashtbl, -1);

        int run = 0;
        int cptr = 0;
        int rptr = 0;

        while (len > 0)
        {
            int boffset = 0;
            int blen = 2;
            int bcost = 2;
            int mlen = Math.Min(len, 1028);
            int hash = Hash(inBuf, cptr);
            int hoffset = hashtbl[hash];
            int minhoffset = Math.Max(cptr - 131071, 0);

            if (hoffset >= minhoffset)
            {
                do
                {
                    if (inBuf[cptr + blen] == inBuf[hoffset + blen])
                    {
                        int tlen = MatchLen(inBuf, cptr, inBuf, hoffset, mlen);
                        if (tlen > blen)
                        {
                            int toffset = (cptr - 1) - hoffset;
                            int tcost = toffset < 1024 && tlen <= 10 ? 2
                                : toffset < 16384 && tlen <= 67 ? 3
                                : 4;

                            if (tlen - tcost + 4 > blen - bcost + 4)
                            {
                                blen = tlen;
                                bcost = tcost;
                                boffset = toffset;
                                if (blen >= 1028)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                while ((hoffset = link[hoffset & 131071]) >= minhoffset);
            }

            if (bcost >= blen)
            {
                hoffset = cptr;
                link[hoffset & 131071] = hashtbl[hash];
                hashtbl[hash] = hoffset;

                ++run;
                ++cptr;
                --len;
            }
            else
            {
                // Flush pending literals in blocks of up to 112 bytes.
                while (run > 3)
                {
                    int tlen = Math.Min(112, run & ~3);
                    run -= tlen;
                    outData[to++] = (byte)(0xE0 + (tlen >> 2) - 1);
                    Array.Copy(inBuf, rptr, outData, to, tlen);
                    rptr += tlen;
                    to += tlen;
                }

                if (bcost == 2)
                {
                    outData[to++] = (byte)(((boffset >> 8) << 5) + ((blen - 3) << 2) + run);
                    outData[to++] = (byte)boffset;
                }
                else if (bcost == 3)
                {
                    outData[to++] = (byte)(0x80 + (blen - 4));
                    outData[to++] = (byte)((run << 6) + (boffset >> 8));
                    outData[to++] = (byte)boffset;
                }
                else
                {
                    outData[to++] = (byte)(0xC0 + ((boffset >> 16) << 4) + (((blen - 5) >> 8) << 2) + run);
                    outData[to++] = (byte)(boffset >> 8);
                    outData[to++] = (byte)boffset;
                    outData[to++] = (byte)(blen - 5);
                }

                if (run != 0)
                {
                    Array.Copy(inBuf, rptr, outData, to, run);
                    to += run;
                    run = 0;
                }

                for (int i = 0; i < blen; ++i)
                {
                    hash = Hash(inBuf, cptr);
                    hoffset = cptr;
                    link[hoffset & 131071] = hashtbl[hash];
                    hashtbl[hash] = hoffset;
                    ++cptr;
                }

                rptr = cptr;
                len -= blen;
            }
        }

        while (run > 3)
        {
            int tlen = Math.Min(112, run & ~3);
            run -= tlen;
            outData[to++] = (byte)(0xE0 + (tlen >> 2) - 1);
            Array.Copy(inBuf, rptr, outData, to, tlen);
            rptr += tlen;
            to += tlen;
        }

        outData[to++] = (byte)(0xFC + run);
        if (run != 0)
        {
            Array.Copy(inBuf, rptr, outData, to, run);
            to += run;
        }

        int lengthInBytes = to + 2;
        byte[] result = new byte[lengthInBytes];
        result[0] = Signature0;
        result[1] = Signature1;
        Array.Copy(outData, 0, result, 2, to);
        return result;
    }

    private static int Hash(byte[] array, int ptr)
    {
        unchecked
        {
            // The original code reads the same byte three times; kept verbatim for compatibility.
            int v1 = ptr >= array.Length ? 0xFD : array[ptr];
            int v2 = v1;
            int v3 = v1;

            int crc = CrcTable[v1 & 0xFF];
            crc = CrcTable[(crc ^ v2) & 0xFF] ^ (crc >> 8);
            crc = CrcTable[(crc ^ v3) & 0xFF] ^ (crc >> 8);
            return crc;
        }
    }

    private static int MatchLen(byte[] source, int s, byte[] dst, int d, int maxmatch)
    {
        int current;
        for (current = 0; current < maxmatch && source[s++] == dst[d++]; ++current)
        {
            // intentionally empty
        }

        return current;
    }
}
