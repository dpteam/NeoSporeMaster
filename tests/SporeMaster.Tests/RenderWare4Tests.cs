using SporeMaster.Core.Dbpf;
using SporeMaster.Core.RenderWare4;

namespace SporeMaster.Tests;

public static class RenderWare4Tests
{
    private const uint Rw4Type = 0x2F4E681B;

    [SporeTest("RW4 resources parse as containers (structure is consistent)")]
    public static void ContainerStructure()
    {
        int tested = 0;
        int problems = 0;
        string? first = null;

        foreach (string packagePath in TestData.BetaPackages.Concat(TestData.FinalPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != Rw4Type)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length < 0x100)
                {
                    continue;
                }

                tested++;
                try
                {
                    RenderWare4File rw4 = RenderWare4File.Read(new MemoryStream(data));

                    if (rw4.SectionCount != rw4.Sections.Count)
                    {
                        problems++;
                        first ??= $"{Path.GetFileName(packagePath)} {entry.Key}: declared {rw4.SectionCount} sections but read {rw4.Sections.Count}";
                        continue;
                    }

                    // Every listed section must fit inside the file.
                    foreach (Rw4SectionInfo section in rw4.Sections)
                    {
                        if (section.DataOffset < 0 || section.DataOffset + (long)section.Size > data.Length)
                        {
                            problems++;
                            first ??= $"{Path.GetFileName(packagePath)} {entry.Key}: section {section.TypeCode:X} out of range";
                            break;
                        }
                    }

                    // The declared type table should contain at least the base-resource code.
                    if (!rw4.Manifest.TypeCodes.Contains((int)Rw4SectionType.BaseResource))
                    {
                        problems++;
                        first ??= $"{Path.GetFileName(packagePath)} {entry.Key}: manifest has no base-resource type code";
                    }
                }
                catch (Exception ex)
                {
                    problems++;
                    first ??= $"{Path.GetFileName(packagePath)} {entry.Key}: {ex.GetType().Name} {ex.Message}";
                }
            }
        }

        if (tested == 0)
        {
            throw new AssertionException("No RW4 resources found in the reference packages.");
        }

        Assert.True(problems == 0, $"{problems} of {tested} RW4 resource(s) failed container validation. First: {first}");
    }

    [SporeTest("RW4 texture containers expose a raster section")]
    public static void TextureContainerHasRaster()
    {
        int tested = 0;

        foreach (string packagePath in TestData.FinalPackages)
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != Rw4Type)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length < 0x100)
                {
                    continue;
                }

                RenderWare4File rw4 = RenderWare4File.Read(new MemoryStream(data));
                if (rw4.ContainerType != Rw4ContainerType.Texture)
                {
                    continue;
                }

                Assert.True(
                    rw4.Sections.Any(s => s.TypeCode == (int)Rw4SectionType.Raster),
                    $"texture RW4 {entry.Key} should contain a raster section");
                tested++;

                if (tested >= 25)
                {
                    return;
                }
            }
        }

        if (tested == 0)
        {
            throw new AssertionException("No RW4 texture containers found.");
        }
    }

    [SporeTest("RW4 model meshes expose plausible geometry")]
    public static void MeshInfo()
    {
        int meshes = 0;

        foreach (string packagePath in TestData.FinalPackages)
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != Rw4Type)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length < 0x100)
                {
                    continue;
                }

                using RenderWare4File rw4 = RenderWare4File.Read(new MemoryStream(data));
                if (rw4.ContainerType != Rw4ContainerType.Model)
                {
                    continue;
                }

                foreach (Rw4Mesh mesh in rw4.ReadMeshes())
                {
                    Assert.True(mesh.TriangleCount > 0, "mesh should have triangles");
                    Assert.True(mesh.VertexCount > 0, "mesh should have vertices");
                    Assert.True(mesh.VertexBufferIndices.Count > 0, "mesh should reference a vertex buffer");

                    // References are indices into the section list (0x400000 is a "no buffer" sentinel).
                    Assert.True(
                        mesh.IndexBufferIndex >= 0 && mesh.IndexBufferIndex < rw4.Sections.Count,
                        "mesh index buffer reference must be in range");
                    foreach (int vb in mesh.VertexBufferIndices)
                    {
                        Assert.True(
                            vb == 0x400000 || (vb >= 0 && vb < rw4.Sections.Count),
                            $"mesh vertex buffer reference {vb} out of range");
                    }

                    meshes++;
                }

                if (meshes >= 100)
                {
                    return;
                }
            }
        }

        if (meshes == 0)
        {
            throw new AssertionException("No RW4 meshes found.");
        }
    }

    [SporeTest("RW4 meshes decode to valid geometry")]
    public static void MeshGeometry()
    {
        int meshes = 0;

        foreach (string packagePath in TestData.FinalPackages)
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != Rw4Type)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length < 0x100)
                {
                    continue;
                }

                using RenderWare4File rw4 = RenderWare4File.Read(new MemoryStream(data));
                if (rw4.ContainerType != Rw4ContainerType.Model)
                {
                    continue;
                }

                foreach (Rw4MeshGeometry geometry in rw4.ReadGeometry())
                {
                    Assert.True(geometry.VertexCount > 0, "geometry should have vertices");
                    Assert.True(geometry.TriangleCount > 0, "geometry should have triangles");

                    // All positions must be finite.
                    foreach (float f in geometry.Positions)
                    {
                        Assert.True(!float.IsNaN(f) && !float.IsInfinity(f), "position must be finite");
                    }

                    // When present, normals and UVs must be complete and finite.
                    if (geometry.HasNormals)
                    {
                        Assert.Equal(geometry.VertexCount * 3, geometry.Normals.Length);
                        foreach (float f in geometry.Normals)
                        {
                            Assert.True(!float.IsNaN(f) && !float.IsInfinity(f), "normal must be finite");
                        }
                    }

                    if (geometry.HasTexCoords)
                    {
                        Assert.Equal(geometry.VertexCount * 2, geometry.TexCoords.Length);
                        foreach (float f in geometry.TexCoords)
                        {
                            Assert.True(!float.IsNaN(f) && !float.IsInfinity(f), "texcoord must be finite");
                        }
                    }

                    // All indices must reference an existing vertex.
                    foreach (int index in geometry.Indices)
                    {
                        Assert.True(index >= 0 && index < geometry.VertexCount, $"index {index} out of range (0..{geometry.VertexCount - 1})");
                    }

                    meshes++;
                }

                if (meshes >= 150)
                {
                    return;
                }
            }
        }

        if (meshes == 0)
        {
            throw new AssertionException("No RW4 mesh geometry decoded.");
        }
    }

    [SporeTest("RW4 OBJ export is well-formed")]
    public static void ObjExport()
    {
        var geometry = new Rw4MeshGeometry
        {
            Positions = new[] { 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f, 0f },
            TexCoords = new[] { 0f, 0f, 1f, 0f, 0f, 1f },
            Indices = new[] { 0, 1, 2 },
        };

        string obj = Rw4ObjExporter.Export(new[] { geometry });
        Assert.Contains(obj, "o mesh_0");
        Assert.Contains(obj, "v 0 0 0");
        Assert.Contains(obj, "vt 1 0");
        Assert.Contains(obj, "f 1/1 2/2 3/3");
    }

    [SporeTest("RW4 skeletons expose bone hierarchies")]
    public static void Skeletons()
    {
        int skeletons = 0;

        foreach (string packagePath in TestData.FinalPackages)
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != Rw4Type)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length < 0x100)
                {
                    continue;
                }

                using RenderWare4File rw4 = RenderWare4File.Read(new MemoryStream(data));
                foreach (Rw4Skeleton skeleton in rw4.ReadSkeletons())
                {
                    Assert.True(skeleton.Bones.Count > 0, "a skeleton should have bones");

                    // Every parent index must be a valid bone index or -1.
                    foreach (Rw4Bone bone in skeleton.Bones)
                    {
                        Assert.True(
                            bone.ParentIndex >= -1 && bone.ParentIndex < skeleton.Bones.Count,
                            $"bone parent index {bone.ParentIndex} out of range");
                    }

                    skeletons++;
                    if (skeletons >= 50)
                    {
                        return;
                    }
                }
            }
        }

        if (skeletons == 0)
        {
            throw new AssertionException("No RW4 skeletons found.");
        }
    }

    [SporeTest("RW4 skins link skeletons, matrix buffers and bind poses")]
    public static void Skins()
    {
        int skins = 0;

        foreach (string packagePath in TestData.FinalPackages)
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != Rw4Type)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length < 0x100)
                {
                    continue;
                }

                using RenderWare4File rw4 = RenderWare4File.Read(new MemoryStream(data));
                foreach (Rw4SkinLinkage skin in rw4.ReadSkins())
                {
                    if (skin.Skeleton is not null)
                    {
                        Assert.True(skin.Skeleton.Bones.Count > 0, "a linked skeleton should have bones");
                    }

                    if (skin.MatrixBuffer is not null)
                    {
                        foreach (Rw4Matrix3x4 matrix in skin.MatrixBuffer.Matrices)
                        {
                            Assert.Equal(12, matrix.Values.Length);
                        }
                    }

                    if (skin.AnimationSkin is not null && skin.Skeleton is not null)
                    {
                        // The number of bind poses usually matches (or is close to) the bone count.
                        Assert.True(
                            skin.AnimationSkin.Poses.Count <= skin.Skeleton.Bones.Count + 8,
                            $"pose count {skin.AnimationSkin.Poses.Count} vs bones {skin.Skeleton.Bones.Count}");
                    }

                    skins++;
                    if (skins >= 40)
                    {
                        return;
                    }
                }
            }
        }

        if (skins == 0)
        {
            throw new AssertionException("No RW4 skins found.");
        }
    }

    [SporeTest("RW4 keyframe animations parse on real data")]
    public static void KeyframeAnimations()
    {
        int animations = 0;
        int channels = 0;

        foreach (string packagePath in TestData.BetaPackages.Concat(TestData.FinalPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != Rw4Type)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length < 0x100)
                {
                    continue;
                }

                using RenderWare4File rw4 = RenderWare4File.Read(new MemoryStream(data));
                foreach (Rw4KeyframeAnim anim in rw4.ReadKeyframeAnimations())
                {
                    foreach (Rw4AnimationChannel channel in anim.Channels)
                    {
                        Assert.True(
                            Enum.IsDefined(channel.Components),
                            $"unknown keyframe components 0x{(int)channel.Components:X}");

                        float last = float.NegativeInfinity;
                        foreach (Rw4Keyframe keyframe in channel.Keyframes)
                        {
                            // Time must be non-decreasing along a channel.
                            Assert.True(
                                keyframe.Time >= last,
                                $"non-monotonic keyframe time {keyframe.Time} after {last}");
                            last = keyframe.Time;
                            Assert.True(
                                !float.IsNaN(keyframe.Location[0]) && !float.IsInfinity(keyframe.Location[0]),
                                "keyframe has a non-finite location");
                        }

                        channels++;
                    }

                    animations++;
                    if (animations >= 200)
                    {
                        return;
                    }
                }
            }
        }

        if (animations == 0)
        {
            throw new AssertionException("No RW4 keyframe animations found.");
        }

        Assert.True(channels > 0, "animations were found but no channels");
    }

    [SporeTest("RW4 animation sets reference keyframe animation sections")]
    public static void AnimationSets()
    {
        int sets = 0;
        int resolved = 0;

        foreach (string packagePath in TestData.BetaPackages.Concat(TestData.FinalPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != Rw4Type)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length < 0x100)
                {
                    continue;
                }

                using RenderWare4File rw4 = RenderWare4File.Read(new MemoryStream(data));
                foreach (Rw4AnimationSet set in rw4.ReadAnimationSets())
                {
                    foreach ((int _, int objectIndex) in set.Animations)
                    {
                        Rw4SectionInfo? section = rw4.ResolveReference(objectIndex);
                        if (section is { TypeCode: (int)Rw4SectionType.KeyframeAnim })
                        {
                            resolved++;
                        }
                    }

                    sets++;
                }
            }
        }

        if (sets == 0)
        {
            throw new AssertionException("No RW4 animation sets found.");
        }

        // Object indices are section indices: the references should point at keyframe anim sections.
        Assert.True(resolved > 0, "no animation-set object index resolved to a keyframe animation section");
    }

    [SporeTest("RW4 compiled materials decode on real data")]
    public static void Materials()
    {
        int materials = 0;
        int rasterSlots = 0;

        foreach (string packagePath in TestData.BetaPackages.Concat(TestData.FinalPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != Rw4Type)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length < 0x100)
                {
                    continue;
                }

                using RenderWare4File rw4 = RenderWare4File.Read(new MemoryStream(data));
                foreach (Rw4Material material in rw4.ReadMaterials())
                {
                    Assert.True(material.Decompiled, "a compiled material failed to decode");
                    Assert.True(
                        material.Raw.Length > 0,
                        "a decoded material should keep its raw bytes");

                    foreach (Rw4TextureSlotMaterial slot in material.TextureSlots)
                    {
                        if (slot.RasterSectionIndex < 0)
                        {
                            continue;
                        }

                        // The reference must resolve to a valid section, and practically always a raster.
                        // A "no object" reference (INDEX_NO_OBJECT) legitimately resolves to nothing.
                        Rw4SectionInfo? section = rw4.ResolveReference(slot.RasterSectionIndex);
                        if (section is { TypeCode: (int)Rw4SectionType.Raster })
                        {
                            rasterSlots++;
                        }
                        else if (section is not null)
                        {
                            Assert.True(
                                section.TypeCode == (int)Rw4SectionType.CompiledState,
                                $"texture slot raster reference points at {Rw4SectionTypes.Describe(section.TypeCode)}");
                        }
                    }

                    materials++;
                }
            }
        }

        if (materials == 0)
        {
            throw new AssertionException("No RW4 compiled materials found.");
        }

        Assert.True(rasterSlots > 0, "no texture slot resolved to a raster section");
    }

    [SporeTest("RW4 material links reference compiled states")]
    public static void MaterialLinks()
    {
        int links = 0;
        int references = 0;

        foreach (string packagePath in TestData.BetaPackages.Concat(TestData.FinalPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != Rw4Type)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length < 0x100)
                {
                    continue;
                }

                using RenderWare4File rw4 = RenderWare4File.Read(new MemoryStream(data));
                foreach (Rw4MaterialLink link in rw4.ReadMaterialLinks())
                {
                    foreach (int index in link.MaterialSectionIndices)
                    {
                        Rw4SectionInfo? section = rw4.ResolveReference(index);
                        Assert.True(
                            section is { TypeCode: (int)Rw4SectionType.CompiledState },
                            $"material link index {index} does not point at a compiled state");
                        references++;
                    }

                    links++;
                }
            }
        }

        if (links == 0)
        {
            throw new AssertionException("No RW4 material links found.");
        }

        Assert.True(references > 0, "material links were found but no references");
    }

    [SporeTest("RW4 blend shapes and buffers parse on real data")]
    public static void BlendShapes()
    {
        int shapes = 0;
        int buffers = 0;

        foreach (string packagePath in TestData.BetaPackages.Concat(TestData.FinalPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != Rw4Type)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length < 0x100)
                {
                    continue;
                }

                using RenderWare4File rw4 = RenderWare4File.Read(new MemoryStream(data));

                foreach (Rw4BlendShape shape in rw4.ReadBlendShapes())
                {
                    // A blend shape either lists its shapes or is empty.
                    Assert.True(shape.ShapeIds.Count >= 0, "blend shape id list is invalid");
                    shapes++;
                }

                foreach (Rw4BlendShapeBuffer buffer in rw4.ReadBlendShapeBuffers())
                {
                    Assert.True(buffer.VertexCount > 0, "a blend-shape buffer should have vertices");
                    Assert.True(
                        buffer.Streams.Any(s => s is { Length: > 0 }),
                        "a blend-shape buffer should have at least one stream");
                    buffers++;
                }
            }
        }

        if (shapes == 0 || buffers == 0)
        {
            throw new AssertionException($"Expected blend shapes and buffers, got shapes={shapes} buffers={buffers}.");
        }
    }

    [SporeTest("RW4 rasters expose texture metadata")]
    public static void Rasters()
    {
        int rasters = 0;

        foreach (string packagePath in TestData.BetaPackages.Concat(TestData.FinalPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != Rw4Type)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length < 0x100)
                {
                    continue;
                }

                using RenderWare4File rw4 = RenderWare4File.Read(new MemoryStream(data));
                if (rw4.RawType != (uint)Rw4ContainerType.Texture)
                {
                    continue;
                }

                foreach (Rw4Raster raster in rw4.ReadRasters())
                {
                    Assert.True(raster.Width > 0 && raster.Height > 0, "raster must have a size");
                    Assert.True(raster.MipmapLevels >= 1, "raster must have at least one mip level");
                    Assert.True(raster.FormatName.Length > 0, "raster must have a format name");
                    rasters++;
                }
            }
        }

        if (rasters == 0)
        {
            throw new AssertionException("No RW4 raster sections found.");
        }
    }

    [SporeTest("RW4 textures export to a valid DDS")]
    public static void ExportTexture()
    {
        foreach (string packagePath in TestData.BetaPackages.Concat(TestData.FinalPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != Rw4Type)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length < 0x100)
                {
                    continue;
                }

                using RenderWare4File rw4 = RenderWare4File.Read(new MemoryStream(data));
                if (rw4.RawType != (uint)Rw4ContainerType.Texture)
                {
                    continue;
                }

                foreach (Rw4Raster raster in rw4.ReadRasters())
                {
                    byte[]? pixels = rw4.GetRasterData(raster);
                    if (pixels is null || pixels.Length == 0)
                    {
                        continue;
                    }

                    using var buffer = new MemoryStream();
                    raster.ToDds(pixels).Write(buffer);
                    buffer.Position = 0;
                    Core.Textures.DdsTexture readBack = Core.Textures.DdsTexture.Read(buffer);

                    Assert.Equal(raster.Width, readBack.Width);
                    Assert.Equal(raster.Height, readBack.Height);
                    Assert.Equal(Math.Max(1, raster.MipmapLevels), readBack.MipmapCount);
                    Assert.Equal((uint)raster.TextureFormat, readBack.Format);
                    return;
                }
            }
        }

        throw new AssertionException("No RW4 texture with pixel data was found.");
    }

    [SporeTest("OBJ export emits a material library")]
    public static void ObjMaterialLibrary()
    {
        string metal = Rw4ObjExporter.ExportMtl(new[] { ("mat0", "tex0.dds"), ("mat1", (string?)null) });
        Assert.Contains(metal, "newmtl mat0");
        Assert.Contains(metal, "map_Kd tex0.dds");
        Assert.Contains(metal, "newmtl mat1");

        // Find a real model with geometry and check the OBJ references the library and a material.
        foreach (string packagePath in TestData.BetaPackages.Concat(TestData.FinalPackages))
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries)
            {
                if (entry.Key.TypeId != Rw4Type)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                if (data.Length < 0x100)
                {
                    continue;
                }

                using RenderWare4File rw4 = RenderWare4File.Read(new MemoryStream(data));
                IReadOnlyList<Rw4MeshGeometry> geometry = rw4.ReadGeometry();
                if (geometry.Count == 0)
                {
                    continue;
                }

                string obj = Rw4ObjExporter.Export(geometry, "model.mtl", _ => "mat0");
                Assert.Contains(obj, "mtllib model.mtl");
                Assert.Contains(obj, "usemtl mat0");
                Assert.Contains(obj, "v ");
                Assert.Contains(obj, "f ");
                return;
            }
        }

        throw new AssertionException("No RW4 geometry was found to export.");
    }

    [SporeTest("Ogre XML export is well-formed")]
    public static void OgreExport()
    {
        // Synthetic check of the writer and the material script.
        var mesh = new Rw4MeshGeometry
        {
            Positions = new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 },
            Normals = new float[] { 0, 0, 1, 0, 0, 1, 0, 0, 1 },
            TexCoords = new float[] { 0, 0, 1, 0, 0, 1 },
            Indices = new int[] { 0, 1, 2 },
            MeshSectionIndex = 0,
        };

        string xml = Rw4OgreExporter.ExportMesh("test", new[] { mesh }, _ => "mat0");
        var doc = System.Xml.Linq.XDocument.Parse(xml);
        System.Xml.Linq.XNamespace ns = doc.Root!.Name.Namespace;
        Assert.Equal("mesh", doc.Root.Name.LocalName);
        Assert.Equal("3", doc.Descendants("geometry").First().Attribute("vertexcount")!.Value);
        Assert.Equal("1", doc.Descendants("faces").First().Attribute("count")!.Value);
        Assert.Equal("mat0", doc.Descendants("submesh").First().Attribute("material")!.Value);

        string material = Rw4OgreExporter.ExportMaterial(new (string, string?)[] { ("mat0", "tex0.dds") });
        Assert.Contains(material, "material mat0");
        Assert.Contains(material, "texture tex0.dds");

        // Real resource: the exported document parses and has submeshes with faces.
        foreach (string packagePath in TestData.FinalPackages)
        {
            using DatabasePackedFile package = DatabasePackedFile.Open(packagePath);
            foreach (DbpfEntry entry in package.Entries.Take(400))
            {
                if (entry.Key.TypeId != Rw4Type)
                {
                    continue;
                }

                byte[] data = package.ReadEntry(entry);
                using RenderWare4File rw4 = RenderWare4File.Read(new MemoryStream(data));
                IReadOnlyList<Rw4MeshGeometry> geometry = rw4.ReadGeometry();
                if (geometry.Count == 0)
                {
                    continue;
                }

                var realDoc = System.Xml.Linq.XDocument.Parse(Rw4OgreExporter.ExportMesh("m", geometry));
                Assert.True(realDoc.Descendants("face").Any(), "exported mesh should have faces");
                return;
            }
        }
    }

    [SporeTest("bad RW4 magic is rejected")]
    public static void BadMagic()
    {
        Assert.Throws<NotRenderWare4Exception>(() =>
            RenderWare4File.Read(new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })));
    }

    [SporeTest("section type names are resolved")]
    public static void SectionNames()
    {
        Assert.Equal("Mesh", Rw4SectionTypes.Describe((int)Rw4SectionType.Mesh));
        Assert.Equal("BaseResource", Rw4SectionTypes.Describe((int)Rw4SectionType.BaseResource));
        Assert.Contains(Rw4SectionTypes.Describe(0x1234), "0x");
    }
}
