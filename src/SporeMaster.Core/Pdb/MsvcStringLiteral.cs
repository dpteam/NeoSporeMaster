using System.Text;

namespace SporeMaster.Core.Pdb;

/// <summary>
/// Decodes the encoded string literals that MSVC stores in symbol names (the <c>??_C@...</c>
/// "string literal symbols"). PDB files contain thousands of these, which reveals the program's
/// readable string constants: file paths, format strings, configuration keys and asset names.
/// </summary>
/// <remarks>
/// This follows the classic MSVC name-mangling algorithm (see LLVM's
/// <c>MicrosoftDemangle.cpp</c>, <c>demangleStringLiteral</c>): the payload after <c>??_C@_</c> is a
/// character-width flag, a byte length, a CRC and then the encoded characters terminated by
/// <c>@</c>. A <c>?</c> introduces an escape: <c>?$XY</c> is a byte in a rebased hex alphabet
/// (A=0..J=9, K=10..P=15); <c>?0</c>..<c>?9</c> are punctuation; <c>?A</c>..<c>?Z</c> and
/// <c>?a</c>..<c>?z</c> are Latin-1 letters.
/// </remarks>
public static class MsvcStringLiteral
{
    private static readonly char[] Punctuation = ",/\\:. \n\t'-".ToCharArray();

    /// <summary>True when a raw symbol name is an encoded string literal.</summary>
    public static bool IsStringLiteral(string symbolName) =>
        symbolName.StartsWith("??_C@", StringComparison.Ordinal);

    /// <summary>Decodes an encoded string-literal symbol name, or returns false on any malformed input.</summary>
    public static bool TryDecode(string symbolName, out string text)
    {
        text = string.Empty;

        if (!symbolName.StartsWith("??_C@", StringComparison.Ordinal))
        {
            return false;
        }

        int i = 4; // after "??_C"
        if (i + 1 >= symbolName.Length || symbolName[i] != '@' || symbolName[i + 1] != '_')
        {
            return false;
        }

        i += 2;
        if (i >= symbolName.Length)
        {
            return false;
        }

        char type = symbolName[i++];
        bool wide = type == '1';
        if (type != '0' && type != '1')
        {
            return false;
        }

        if (!TryReadNumber(symbolName, ref i, out long byteSize) || byteSize < (wide ? 2 : 1))
        {
            return false;
        }

        int crcEnd = symbolName.IndexOf('@', i);
        if (crcEnd < 0)
        {
            return false;
        }

        i = crcEnd + 1;

        try
        {
            if (wide)
            {
                return DecodeWide(symbolName, ref i, byteSize, out text);
            }

            return DecodeNarrow(symbolName, ref i, byteSize, out text);
        }
        catch (IndexOutOfRangeException)
        {
            text = string.Empty;
            return false;
        }
    }

    private static bool DecodeNarrow(string s, ref int i, long byteSize, out string text)
    {
        const int MaxBytes = 32 * 4;
        var bytes = new List<byte>();
        while (i < s.Length && s[i] != '@')
        {
            if (bytes.Count >= MaxBytes)
            {
                break;
            }

            bytes.Add(ReadCharLiteral(s, ref i));
        }

        if (i < s.Length && s[i] == '@')
        {
            i++;
        }

        bool truncated = byteSize > bytes.Count;
        int charBytes = GuessCharByteSize(bytes, byteSize);
        int numChars = charBytes > 0 ? bytes.Count / charBytes : 0;

        var sb = new StringBuilder();
        for (int c = 0; c < numChars; c++)
        {
            uint value = 0;
            for (int b = 0; b < charBytes; b++)
            {
                value |= (uint)bytes[c * charBytes + b] << (8 * b);
            }

            // The final character is a terminator and is dropped unless the string was truncated.
            if (c + 1 < numChars || truncated)
            {
                AppendEscaped(sb, value);
            }
        }

        text = sb.ToString();
        return true;
    }

    private static bool DecodeWide(string s, ref int i, long byteSize, out string text)
    {
        bool truncated = byteSize > 64;
        var sb = new StringBuilder();
        while (i < s.Length && s[i] != '@')
        {
            byte c1 = ReadCharLiteral(s, ref i);
            byte c2 = ReadCharLiteral(s, ref i);
            uint value = (uint)((c1 << 8) | c2);
            if (byteSize != 2 || truncated)
            {
                AppendEscaped(sb, value);
            }

            byteSize -= 2;
        }

        if (i < s.Length && s[i] == '@')
        {
            i++;
        }

        text = sb.ToString();
        return true;
    }

    private static int GuessCharByteSize(List<byte> bytes, long numBytes)
    {
        if (numBytes % 2 == 1)
        {
            return 1;
        }

        if (numBytes < 32)
        {
            int trailingNulls = 0;
            for (int i = bytes.Count - 1; i >= 0 && bytes[i] == 0; i--)
            {
                trailingNulls++;
            }

            if (trailingNulls >= 4 && numBytes % 4 == 0)
            {
                return 4;
            }

            if (trailingNulls >= 2)
            {
                return 2;
            }

            return 1;
        }

        int nulls = bytes.Count(b => b == 0);
        if (bytes.Count > 0 && nulls >= 2 * bytes.Count / 3 && numBytes % 4 == 0)
        {
            return 4;
        }

        if (bytes.Count > 0 && nulls >= bytes.Count / 3)
        {
            return 2;
        }

        return 1;
    }

    private static byte ReadCharLiteral(string s, ref int i)
    {
        if (i >= s.Length)
        {
            throw new IndexOutOfRangeException();
        }

        if (s[i] != '?')
        {
            return (byte)s[i++];
        }

        i++; // consume '?'
        if (i >= s.Length)
        {
            throw new IndexOutOfRangeException();
        }

        if (s[i] == '$')
        {
            if (i + 2 >= s.Length)
            {
                throw new IndexOutOfRangeException();
            }

            byte hi = RebasedHex(s[i + 1]);
            byte lo = RebasedHex(s[i + 2]);
            i += 3;
            return (byte)((hi << 4) | lo);
        }

        char c = s[i];
        i++;
        if (c is >= '0' and <= '9')
        {
            int index = c - '0';
            return index < Punctuation.Length ? (byte)Punctuation[index] : (byte)'?';
        }

        if (c is >= 'a' and <= 'z')
        {
            return (byte)(0xE1 + (c - 'a'));
        }

        if (c is >= 'A' and <= 'Z')
        {
            return (byte)(0xC1 + (c - 'A'));
        }

        return (byte)'?';
    }

    /// <summary>Rebased hex digit: A..J = 0..9, K..P = 10..15.</summary>
    private static byte RebasedHex(char c)
    {
        if (c is >= 'A' and <= 'J')
        {
            return (byte)(c - 'A');
        }

        if (c is >= 'K' and <= 'P')
        {
            return (byte)(10 + c - 'K');
        }

        throw new IndexOutOfRangeException();
    }

    private static bool TryReadNumber(string s, ref int i, out long value)
    {
        value = 0;
        if (i >= s.Length)
        {
            return false;
        }

        if (s[i] is >= '0' and <= '9')
        {
            value = s[i] - '0' + 1;
            i++;
            return true;
        }

        long result = 0;
        while (i < s.Length)
        {
            char c = s[i];
            if (c == '@')
            {
                i++;
                value = result;
                return true;
            }

            if (c is >= 'A' and <= 'P')
            {
                result = (result << 4) + (c - 'A');
                i++;
                continue;
            }

            return false;
        }

        return false;
    }

    private static void AppendEscaped(StringBuilder sb, uint c)
    {
        switch (c)
        {
            case 0:
                sb.Append("\\0");
                return;
            case (uint)'\n':
                sb.Append("\\n");
                return;
            case (uint)'\r':
                sb.Append("\\r");
                return;
            case (uint)'\t':
                sb.Append("\\t");
                return;
        }

        if (c is > 0x1F and < 0x7F)
        {
            sb.Append((char)c);
        }
        else
        {
            sb.Append(c <= 0xFFFF ? $"\\u{c:X4}" : $"\\U{c:X8}");
        }
    }
}
