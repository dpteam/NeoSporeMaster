using SporeMaster.Core.Diagnostics;
using SporeMaster.Core.IO;

namespace SporeMaster.Core.RenderWare4;

/// <summary>One entry of the RenderWare 4 section table.</summary>
public sealed class Rw4SectionInfo
{
    /// <summary>Offset of the section data. For base resources this is relative to the buffer area.</summary>
    public long DataOffset { get; set; }

    public int Field04 { get; set; }

    public int Size { get; set; }

    public int Alignment { get; set; }

    public int TypeCodeIndex { get; set; }

    public int TypeCode { get; set; }

    public Rw4SectionType Type => Rw4SectionTypes.TryGet(TypeCode, out Rw4SectionType t) ? t : Rw4SectionType.Unknown;

    public override string ToString() =>
        $"{Rw4SectionTypes.Describe(TypeCode)} @ 0x{DataOffset:X} size={Size} align={Alignment}";
}

/// <summary>The RenderWare 4 section manifest and its sub-blocks.</summary>
public sealed class Rw4SectionManifest
{
    public int Field0 { get; set; }

    public int Field4 { get; set; }

    /// <summary>The list of section type codes declared by the file.</summary>
    public List<int> TypeCodes { get; } = new();

    public Rw4ExternalArenas ExternalArenas { get; } = new();

    public Rw4SubReferences SubReferences { get; } = new();

    public Rw4Atoms Atoms { get; } = new();

    public static Rw4SectionManifest Read(Stream stream, long baseOffset, DiagnosticSink sink)
    {
        var manifest = new Rw4SectionManifest();

        stream.Seek(baseOffset, SeekOrigin.Begin);
        stream.ReadI32(); // section type code (0x10004)
        manifest.Field0 = stream.ReadI32();
        manifest.Field4 = stream.ReadI32();

        int offset1 = stream.ReadI32();
        int offset2 = stream.ReadI32();
        int offset3 = stream.ReadI32();
        int offset4 = stream.ReadI32();

        // Types.
        try
        {
            stream.Seek(baseOffset + offset1, SeekOrigin.Begin);
            stream.ReadI32(); // subsection type code
            int count = stream.ReadI32();
            stream.ReadI32(); // field_04
            if (count < 0 || count > 4096)
            {
                sink.Warn($"RW4 manifest type count {count} looks invalid.");
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    manifest.TypeCodes.Add(stream.ReadI32());
                }
            }
        }
        catch (EndOfStreamException)
        {
            sink.Warn("RW4 manifest type list is truncated.");
        }

        manifest.ExternalArenas.Read(stream, baseOffset + offset2, sink);
        manifest.SubReferences.Read(stream, baseOffset + offset3, sink);
        manifest.Atoms.Read(stream, baseOffset + offset4, sink);

        return manifest;
    }
}

/// <summary>External-arena block of the RW4 manifest (opaque fields kept for round-tripping).</summary>
public sealed class Rw4ExternalArenas
{
    public int[] Fields { get; } = new int[8];

    internal void Read(Stream stream, long offset, DiagnosticSink sink)
    {
        try
        {
            stream.Seek(offset, SeekOrigin.Begin);
            stream.ReadI32(); // subsection type code (0x10006)
            for (int i = 0; i < Fields.Length; i++)
            {
                Fields[i] = stream.ReadI32();
            }
        }
        catch (EndOfStreamException)
        {
            sink.Warn("RW4 external arenas block is truncated.");
        }
    }
}

/// <summary>Sub-references block of the RW4 manifest.</summary>
public sealed class Rw4SubReferences
{
    public int Count { get; private set; }

    public int Field4 { get; private set; }

    public int Field8 { get; private set; }

    public long Offset { get; private set; }

    /// <summary>The (objectIndex, fieldOffset) pairs referenced by this block.</summary>
    public List<(int ObjectIndex, int FieldOffset)> References { get; } = new();

    internal void Read(Stream stream, long offset, DiagnosticSink sink)
    {
        try
        {
            stream.Seek(offset, SeekOrigin.Begin);
            stream.ReadI32(); // subsection type code (0x10007)
            Count = stream.ReadI32();
            Field4 = stream.ReadI32();
            Field8 = stream.ReadI32();
            stream.ReadI32(); // end offset of the objects
            Offset = stream.ReadU32();
            stream.ReadI32(); // count again
        }
        catch (EndOfStreamException)
        {
            sink.Warn("RW4 sub-references block is truncated.");
        }
    }

    /// <summary>Reads the actual reference pairs (pointed to by <see cref="Offset"/>).</summary>
    internal void ReadReferences(Stream stream, DiagnosticSink sink)
    {
        if (Count <= 0 || Count > 1_000_000 || Offset <= 0)
        {
            return;
        }

        try
        {
            stream.Seek(Offset, SeekOrigin.Begin);
            for (int i = 0; i < Count; i++)
            {
                References.Add((stream.ReadI32(), stream.ReadI32()));
            }
        }
        catch (EndOfStreamException)
        {
            sink.Warn("RW4 sub-reference table is truncated.");
        }
    }
}

/// <summary>Atoms block of the RW4 manifest.</summary>
public sealed class Rw4Atoms
{
    public int Field0 { get; private set; }

    public int Field4 { get; private set; }

    internal void Read(Stream stream, long offset, DiagnosticSink sink)
    {
        try
        {
            stream.Seek(offset, SeekOrigin.Begin);
            stream.ReadI32(); // subsection type code (0x10008)
            Field0 = stream.ReadI32();
            Field4 = stream.ReadI32();
        }
        catch (EndOfStreamException)
        {
            sink.Warn("RW4 atoms block is truncated.");
        }
    }
}
