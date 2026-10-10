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
