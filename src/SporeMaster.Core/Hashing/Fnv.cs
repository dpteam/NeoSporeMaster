using System.Globalization;

namespace SporeMaster.Core.Hashing;

/// <summary>
/// The 32-bit hash used throughout Spore's data files.
/// </summary>
/// <remarks>
/// The algorithm is the one EA uses everywhere in Spore (and SporeModder FX calls it
/// <c>fnvHash</c>): it is case-insensitive and multiplies before XOR-ing, i.e. it is the
/// original FNV-1 variant rather than FNV-1a. It is kept byte-for-byte compatible with
/// both legacy SporeMaster (<c>StringHelpers.FNV</c>) and SporeModder FX
/// (<c>HashManager.fnvHash</c>).
///
///   h = 0x811C9DC5
///   for each lowercased char c:
///       h = h * 0x1000193
///       h = h ^ c
///
/// Note: this is the hash used for <em>naming</em> resources (groups and instances). Resource
/// <em>type</em> and property IDs are constants assigned by EA (for example <c>description</c>
/// and <c>parent</c> have consecutive IDs), so their names are aliases resolved through the
/// registries rather than preimages of this function.
/// </remarks>
public static class Fnv
{
    public const uint OffsetBasis = 0x811C9DC5;
    public const uint Prime = 0x1000193;

    /// <summary>Computes the Spore FNV hash of <paramref name="value"/> (case-insensitive).</summary>
    public static uint Hash(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return OffsetBasis;
        }

        unchecked
        {
            uint hash = OffsetBasis;
            foreach (char c in value)
            {
                char lower = char.ToLowerInvariant(c);
                hash *= Prime;
                hash ^= lower;
            }

            return hash;
        }
    }

    /// <summary>
    /// Parses a name that may already be a hash written as <c>#xxxxxxxx</c> or
    /// <c>0xXXXXXXXX</c>; otherwise returns the FNV hash of the name.
    /// </summary>
    public static uint HashOrLiteral(string? value)
    {
        if (TryParseLiteral(value, out uint literal))
        {
            return literal;
        }

        return Hash(value);
    }

    /// <summary>Attempts to parse <c>#xxxxxxxx</c> / <c>0xXXXXXXXX</c> / plain decimal.</summary>
    public static bool TryParseLiteral(string? value, out uint result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string text = value.Trim();
        if (text.StartsWith('#'))
        {
            return uint.TryParse(text.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(text.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        return uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>Formats a hash the way SporeModder FX does: <c>0x006e62ba</c> (lower case).</summary>
    public static string ToHex(uint value) => "0x" + value.ToString("x8", CultureInfo.InvariantCulture);

    /// <summary>Formats a hash in upper case: <c>0x006E62BA</c>.</summary>
    public static string ToHexUpper(uint value) => "0x" + value.ToString("X8", CultureInfo.InvariantCulture);
}
