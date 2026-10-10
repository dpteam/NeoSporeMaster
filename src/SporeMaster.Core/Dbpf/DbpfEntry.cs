namespace SporeMaster.Core.Dbpf;

/// <summary>Thrown when a stream does not look like a Spore package at all.</summary>
public sealed class NotAPackageException : Exception
{
    public NotAPackageException(string message)
        : base(message)
    {
    }
}

/// <summary>One resource entry in a DBPF index.</summary>
public sealed class DbpfEntry
{
    /// <summary>Position of this entry inside the index (useful for diagnostics).</summary>
    public int Index { get; set; }

    public ResourceKey Key { get; set; }

    /// <summary>Absolute offset of the (possibly compressed) payload.</summary>
    public long ChunkOffset { get; set; }

    /// <summary>Size of the payload as stored, with the high bit masked off.</summary>
    public uint PackedSize { get; set; }

    /// <summary>Size of the payload once decompressed.</summary>
    public uint MemSize { get; set; }

    /// <summary>True when the payload is RefPack-compressed.</summary>
    public bool IsCompressed { get; set; }

    /// <summary>Raw compression label as stored (0 or 0xFFFF for valid Spore packages).</summary>
    public short CompressionLabel { get; set; }

    /// <summary>The trailing 16-bit flags field (1 == "saved" in Spore).</summary>
    public ushort Flags { get; set; }

    /// <summary>The optional third shared index field; unused by Spore (kept for round-tripping).</summary>
    public uint Unknown { get; set; }

    public override string ToString() =>
        $"{Key} @ 0x{ChunkOffset:X} size={PackedSize} mem={MemSize} compressed={IsCompressed}";
}
