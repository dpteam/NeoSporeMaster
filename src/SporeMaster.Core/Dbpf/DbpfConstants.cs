namespace SporeMaster.Core.Dbpf;

/// <summary>The four-byte signatures that may open a package container.</summary>
public enum DbpfContainer
{
    /// <summary>"DBPF" — the classic database packed file.</summary>
    Packed,

    /// <summary>"DBBF" — the large variant that uses 64-bit offsets.</summary>
    BigPacked,

    /// <summary>"DBPP" — an encrypted/older container (best-effort only).</summary>
    Protected,
}

/// <summary>The version numbers found in a DBPF header and its index block.</summary>
public readonly record struct DbpfVersion(uint Major, uint Minor, uint IndexMajor, uint IndexMinor)
{
    public static DbpfVersion Default => new(2, 0, 0, 3);

    /// <summary>True for any header that predates DBPF 2.0 (Sims 2-era inline index).</summary>
    public bool IsLegacyV1 => Major < 2;

    public override string ToString() =>
        $"{Major}.{Minor} (index {IndexMajor}.{IndexMinor})";
}

/// <summary>Well-known DBPF constants.</summary>
public static class DbpfConstants
{
    public const uint MagicDbpf = 0x46504244; // "DBPF"
    public const uint MagicDbbf = 0x46424244; // "DBBF"
    public const uint MagicDbpp = 0x50504244; // "DBPP"

    public const int HeaderSize = 96;
    public const int BigHeaderSize = 120;

    /// <summary>Compression label stored in the index; <c>-1</c> (0xFFFF) means RefPack-compressed.</summary>
    public const short CompressionRefPack = -1;

    public const short CompressionNone = 0;

    /// <summary>Index type-bit meanings: a set bit means the field is constant for every entry.</summary>
    public const int IndexTypeIdShared = 1 << 0;

    public const int IndexGroupIdShared = 1 << 1;

    public const int IndexUnknownShared = 1 << 2;
}
