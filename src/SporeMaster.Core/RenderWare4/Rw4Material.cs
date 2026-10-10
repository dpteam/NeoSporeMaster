using SporeMaster.Core.IO;

namespace SporeMaster.Core.RenderWare4;

/// <summary>A single render/stage/sampler state entry: a state id and its raw integer value.</summary>
public sealed class Rw4StateEntry
{
    public int Id { get; init; }

    public int Value { get; init; }
}

/// <summary>A group of render states (materials may define several state groups).</summary>
public sealed class Rw4RenderGroup
{
    public int Group { get; init; }

    public List<Rw4StateEntry> States { get; } = new();
}

/// <summary>A texture slot of a compiled material: a sampler index, a raster reference and its states.</summary>
public sealed class Rw4TextureSlotMaterial
{
    public int SamplerIndex { get; set; }

    /// <summary>Section index of the referenced raster (texture), or -1.</summary>
    public int RasterSectionIndex { get; set; } = -1;

    public int StageStatesMask { get; set; }

    public List<Rw4StateEntry> StageStates { get; } = new();

    public int SamplerStatesMask { get; set; }

    public List<Rw4StateEntry> SamplerStates { get; } = new();
}

/// <summary>One shader-constant entry of a compiled material.</summary>
public sealed class Rw4ShaderData
{
    public int Index { get; set; }

    public int Offset { get; set; }

    public int[] Data { get; set; } = Array.Empty<int>();
}

/// <summary>
/// A RenderWare 4 compiled material state (section type <c>0x2000B</c>). This is a decode of the
/// Direct3D state bytecode: primitive type, shader id, material/ambient colours, render-state groups,
/// texture slots (with their sampler and stage states) and shader constants.
/// </summary>
/// <remarks>
/// Reading (decompilation) is implemented; re-compilation is not, because the engine regenerates the
/// bytecode when writing. The original bytes are always kept in <see cref="Raw"/>.
/// </remarks>
public sealed class Rw4Material
{
    // Header flags (see SporeModder FX's MaterialStateCompiler).
    public const int FlagShaderData = 0x8;
    public const int FlagMaterialColor = 0x10;
    public const int FlagAmbientColor = 0x20;
    public const int FlagVertexDescription = 0x100000;
    public const int FlagUseBooleans = 0x8000;
    public const int FlagModelToWorld = 0x1;
    public const int FlagModelToWorldObject = 0x2;

    public const int Flag3RenderStates = 0x20000;
    public const int Flag3PaletteEntries = 0x100000;
    public const int Flag3TextureSlots = 0x1FFFF;

    /// <summary>The raw section bytes, exactly as stored.</summary>
    public byte[] Raw { get; set; } = Array.Empty<byte>();

    /// <summary>Section index of this compiled state inside the resource.</summary>
    public int SectionIndex { get; set; } = -1;

    /// <summary>True when the whole section was decoded successfully.</summary>
    public bool Decompiled { get; set; }

    public int PrimitiveType { get; set; }

    public int Flags1 { get; set; }

    public int Flags2 { get; set; }

    public int Flags3 { get; set; }

    public int Field14 { get; set; }

    /// <summary>FNV hash of the shader (renderer) name.</summary>
    public int RendererId { get; set; }

    public int ModelToWorldSectionIndex { get; set; } = -1;

    public float[]? ModelToWorld { get; set; }

    public Rw4VertexDescription? VertexDescription { get; set; }

    public List<Rw4ShaderData> ShaderData { get; } = new();

    /// <summary>Material colour (r, g, b, a) when present.</summary>
    public float[]? MaterialColor { get; set; }

    /// <summary>Ambient colour (r, g, b) when present.</summary>
    public float[]? AmbientColor { get; set; }

    public float?[] UnkData2 { get; } = new float?[8];

    public bool[]? UnkData3 { get; set; }

    public int? UnkData4 { get; set; }

    public float[]? UnkData5 { get; set; }

    public float? UnkData6 { get; set; }

    public float? UnkData7 { get; set; }

    public int[]? UnkData8 { get; set; }

    public int[]? UnkData9 { get; set; }

    public int[]? UnkData10 { get; set; }

    public List<Rw4RenderGroup> RenderGroups { get; } = new();

    public int PaletteEntriesSectionIndex { get; set; } = -1;

    public List<Rw4TextureSlotMaterial> TextureSlots { get; } = new();

    /// <summary>Decodes a compiled-state section.</summary>
    internal static Rw4Material Read(Stream s, long offset, int size, long fileLength)
    {
        var material = new Rw4Material();

        if (size <= 0 || offset < 0 || offset + size > fileLength)
        {
            return material;
        }

        s.Seek(offset, SeekOrigin.Begin);
        material.Raw = s.ReadBytesExactly(size);

        using var stream = new MemoryStream(material.Raw, writable: false);
        try
        {
            material.Decompile(stream);
            material.Decompiled = true;
        }
        catch
        {
            material.Decompiled = false;
        }

        return material;
    }

    private void Decompile(Stream s)
    {
        s.ReadU32(); // size (not always reliable)
        PrimitiveType = s.ReadI32();
        Flags1 = s.ReadI32();
        Flags2 = s.ReadI32();
        Flags3 = s.ReadI32();
        Field14 = s.ReadI32();
        RendererId = s.ReadI32();
        s.ReadU32(); // padding

        if ((Flags1 & FlagModelToWorld) != 0)
        {
            if ((Flags1 & FlagModelToWorldObject) != 0)
            {
                ModelToWorldSectionIndex = s.ReadI32();
            }
            else
            {
                var m = new float[16];
                for (int i = 0; i < 16; i++)
                {
                    m[i] = s.ReadF32LE();
                }

                ModelToWorld = m;
            }
        }

        if ((Flags1 & FlagVertexDescription) != 0)
        {
            VertexDescription = Rw4VertexDescription.Read(s);
        }

        if ((Flags1 & FlagShaderData) != 0)
        {
            short index = s.ReadI16();
            while (index != 0)
            {
                if (index > 0)
                {
                    short entryOffset = s.ReadI16();
                    int length = s.ReadI32();
                    if (length < 0 || length % 4 != 0)
                    {
                        throw new InvalidDataException($"Unexpected shader-data length {length}.");
                    }

                    var entry = new Rw4ShaderData { Index = index, Offset = entryOffset };
                    if (entryOffset > 0)
                    {
                        s.Seek(entryOffset, SeekOrigin.Current);
                    }

                    var data = new int[length / 4];
                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] = s.ReadI32();
                    }

                    entry.Data = data;
                    ShaderData.Add(entry);

                    if (data.Length == 0)
                    {
                        s.ReadU32();
                    }
                }

                index = s.ReadI16();
            }

            s.Seek(6, SeekOrigin.Current);
        }

        if ((Flags1 & FlagMaterialColor) != 0)
        {
            MaterialColor = new float[4];
            for (int i = 0; i < 4; i++)
            {
                MaterialColor[i] = s.ReadF32LE();
            }
        }

        if ((Flags1 & FlagAmbientColor) != 0)
        {
            AmbientColor = new float[3];
            for (int i = 0; i < 3; i++)
            {
                AmbientColor[i] = s.ReadF32LE();
            }
        }

        if ((Flags1 & 0x3FC0) != 0)
        {
            for (int i = 0; i < 8; i++)
            {
                if ((Flags1 & (1 << (6 + i))) != 0)
                {
                    UnkData2[i] = s.ReadF32LE();
                }
            }
        }

        if ((Flags1 & FlagUseBooleans) != 0)
        {
            var booleans = new bool[17];
            for (int i = 0; i < 17; i++)
            {
                booleans[i] = s.ReadU8() != 0;
            }

            UnkData3 = booleans;
        }

        if ((Flags1 & 0xF0000) != 0)
        {
            if ((Flags1 & 0x10000) != 0)
            {
                UnkData4 = s.ReadI32();
            }

            if ((Flags1 & 0x20000) != 0)
            {
                UnkData5 = new float[3];
                for (int i = 0; i < 3; i++)
                {
                    UnkData5[i] = s.ReadF32LE();
                }
            }

            if ((Flags1 & 0x40000) != 0)
            {
                UnkData6 = s.ReadF32LE();
            }

            if ((Flags1 & 0x80000) != 0)
            {
                UnkData7 = s.ReadF32LE();
            }
        }

        if (Field14 != 0)
        {
            if ((Field14 & 0x20000) != 0)
            {
                UnkData8 = ReadInts(s, 7);
            }

            if ((Field14 & 0x40000) != 0)
            {
                UnkData9 = ReadInts(s, 11);
            }

            if ((Field14 & 0x80000) != 0)
            {
                UnkData10 = ReadInts(s, 11);
            }
        }

        if ((Flags3 & Flag3RenderStates) != 0)
        {
            int group = s.ReadI32();
            while (group != -1)
            {
                var renderGroup = new Rw4RenderGroup { Group = group };
                int state = s.ReadI32();
                while (state != -1)
                {
                    renderGroup.States.Add(new Rw4StateEntry { Id = state, Value = s.ReadI32() });
                    state = s.ReadI32();
                }

                RenderGroups.Add(renderGroup);
                group = s.ReadI32();
            }
        }

        PaletteEntriesSectionIndex = s.ReadI32();

        if ((Flags3 & Flag3TextureSlots) != 0)
        {
            int samplerIndex;
            while ((samplerIndex = s.ReadI32()) != -1)
            {
                var slot = new Rw4TextureSlotMaterial
                {
                    SamplerIndex = samplerIndex,
                    RasterSectionIndex = s.ReadI32(),
                    StageStatesMask = s.ReadI32(),
                };

                if (slot.StageStatesMask != 0)
                {
                    int state;
                    while ((state = s.ReadI32()) != -1)
                    {
                        slot.StageStates.Add(new Rw4StateEntry { Id = state, Value = s.ReadI32() });
                    }
                }

                slot.SamplerStatesMask = s.ReadI32();
                if (slot.SamplerStatesMask != 0)
                {
                    int state;
                    while ((state = s.ReadI32()) != -1)
                    {
                        slot.SamplerStates.Add(new Rw4StateEntry { Id = state, Value = s.ReadI32() });
                    }
                }

                TextureSlots.Add(slot);
            }
        }
    }

    private static int[] ReadInts(Stream s, int count)
    {
        var values = new int[count];
        for (int i = 0; i < count; i++)
        {
            values[i] = s.ReadI32();
        }

        return values;
    }
}

/// <summary>A mesh-compiled-state link section (type <c>0x2001A</c>): a mesh and its materials.</summary>
public sealed class Rw4MaterialLink
{
    /// <summary>Section index of the linked mesh.</summary>
    public int MeshSectionIndex { get; set; } = -1;

    /// <summary>Section indices of the compiled material states used by the mesh.</summary>
    public List<int> MaterialSectionIndices { get; } = new();
}
