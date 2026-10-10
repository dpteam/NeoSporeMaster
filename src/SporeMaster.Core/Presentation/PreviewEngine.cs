using System.Text;
using SporeMaster.Core.Anim;
using SporeMaster.Core.Bitmaps;
using SporeMaster.Core.Dbpf;
using SporeMaster.Core.Imaging;
using SporeMaster.Core.Locale;
using SporeMaster.Core.Names;
using SporeMaster.Core.Properties;
using SporeMaster.Core.RenderWare4;
using SporeMaster.Core.Textures;

namespace SporeMaster.Core.Presentation;

/// <summary>A preview of a resource, ready to be shown in a UI or the terminal.</summary>
public sealed class ResourcePreview
{
    private ResourcePreview()
    {
    }

    /// <summary>One of: empty, image, props, locale, texture, rw4, anim, model, text, hex, error.</summary>
    public string Kind { get; private init; } = "empty";

    public string? Text { get; private init; }

    /// <summary>Encoded PNG bytes when the resource is (or can be rendered as) an image.</summary>
    public byte[]? PngBytes { get; private init; }

    /// <summary>3D geometry when the resource is a model that can be viewed.</summary>
    public PreviewModel? Model { get; private init; }

    public bool IsImage => Kind == "image" && PngBytes is not null;

    public bool IsModel => Kind == "model" && Model is not null;

    public static ResourcePreview Empty() => new() { Kind = "empty", Text = "(nothing selected)" };

    public static ResourcePreview AsText(string text, string kind = "text") => new() { Kind = kind, Text = text };

    public static ResourcePreview AsImage(byte[] png) => new() { Kind = "image", PngBytes = png };

    public static ResourcePreview AsModel(PreviewModel model, string text) =>
        new() { Kind = "model", Model = model, Text = text };
}

/// <summary>
/// Builds human-friendly previews for resources (properties as XML, textures/bitmaps as images,
/// RenderWare 4 as a section listing, and a hex fallback). UI-free so it can be tested headlessly.
/// </summary>
public static class PreviewEngine
{
    private const int MaxTextChars = 200_000;
    private const int MaxHexBytes = 8 * 1024;
    private const int MaxPreviewTriangles = 300_000; // keep the 3D preview responsive

    public static ResourcePreview Build(ResourceItem item, byte[] data, NameRegistrySet names)
    {
        try
        {
            uint type = item.TypeId;

            if (ResourceTypeIds.IsProperty(type))
            {
                PropertyList list = PropertyList.Read(new MemoryStream(data));
                using var xml = new MemoryStream();
                list.WriteXml(xml, names);
                return ResourcePreview.AsText(Encoding.UTF8.GetString(xml.ToArray()), "props");
            }

            if (type == ResourceTypeIds.Locale)
            {
                LocaleTable table = LocaleTable.Read(new MemoryStream(data));
                return ResourcePreview.AsText(table.Serialize(), "locale");
            }

            if (ResourceTypeIds.IsBitmap(type))
            {
                BitmapImage bitmap = BitmapImage.Read(new MemoryStream(data), type);
                byte[] png = PngCodec.Encode(bitmap.Image);
                return ResourcePreview.AsImage(png);
            }

            // TGA images and Windows icon/cursor resources decode to PNG. (Some "cur" resources are
            // RIFF AVI animated cursors, which we do not decode — fall back to the hex view.)
            if (ResourceTypeIds.IsTgaOrCursor(type))
            {
                try
                {
                    RgbaImage image = ImageFormats.Decode(data);
                    return ResourcePreview.AsImage(PngCodec.Encode(image));
                }
                catch (Exception ex)
                {
                    return ResourcePreview.AsText(
                        $"Image could not be decoded ({ex.Message}).\n\n{HexDump(data)}", "hex");
                }
            }

            if (ResourceTypeIds.IsTexture(type))
            {
                RasterTexture raster = RasterTexture.Read(new MemoryStream(data));
                string info =
                    "RasterTexture\n" +
                    $"  Size: {raster.Width} x {raster.Height}\n" +
                    $"  Format: {TextureFormats.Describe(raster.TextureFormat)} (0x{raster.TextureFormat:X8})\n" +
                    $"  Mipmaps: {raster.MipmapCount}\n" +
                    $"  Pixel width: {raster.PixelWidth}\n\n" +
                    "Use the CLI 'texture' command to convert this to DDS.";
                return ResourcePreview.AsText(info, "texture");
            }

            if (ResourceTypeIds.IsRenderWare4(type))
            {
                using RenderWare4File rw4 = RenderWare4File.Read(new MemoryStream(data));

                // RW4 texture resources: decode the embedded raster and show it as an image.
                if (rw4.RawType == (uint)Rw4ContainerType.Texture)
                {
                    byte[]? png = TryDecodeRw4Texture(rw4);
                    if (png is not null)
                    {
                        return ResourcePreview.AsImage(png);
                    }
                }

                var builder = new StringBuilder();
                builder.Append("RenderWare 4 resource\n");
                builder.Append($"  Container: {Rw4SectionTypes.DescribeContainer(rw4.RawType)}\n");
                builder.Append($"  Sections: {rw4.SectionCount}\n");

                AppendRw4Summary(rw4, builder);

                builder.Append('\n');
                int shown = Math.Min(rw4.Sections.Count, 200);
                for (int i = 0; i < shown; i++)
                {
                    Rw4SectionInfo s = rw4.Sections[i];
                    builder.Append($"  [{i}] {Rw4SectionTypes.Describe(s.TypeCode)} @ 0x{s.DataOffset:X} size={s.Size} align={s.Alignment}\n");
                }

                if (rw4.Sections.Count > shown)
                {
                    builder.Append($"  … {rw4.Sections.Count - shown} more section(s)\n");
                }

                // For models, also decode the geometry so the GUI can show a 3D preview.
                PreviewModel? model = TryBuildModel(rw4);
                if (model is not null && model.Meshes.Count > 0)
                {
                    builder.Append($"\n  3D: {model.Meshes.Count} mesh(es), {model.TotalTriangles} triangles.\n");
                    return ResourcePreview.AsModel(model, builder.ToString());
                }

                // A model with meshes but no static vertex buffers is procedural/animated (geometry is
                // generated at runtime), so there is nothing static to display.
                IReadOnlyList<Rw4Mesh> meshes = rw4.ReadMeshes();
                if (meshes.Count > 0)
                {
                    builder.Append("\n  (No static geometry: this model is procedural/animated — it has meshes but no vertex buffers.)\n");
                }

                return ResourcePreview.AsText(builder.ToString(), "rw4");
            }

            if (ResourceTypeIds.IsAnimation(type))
            {
                SpAnimation anim = SpAnimation.Read(new MemoryStream(data));
                var builder = new StringBuilder();
                builder.Append("Spore animation\n");
                builder.Append($"  Version: {anim.Version}\n");
                builder.Append($"  Id:      0x{anim.Id:X8}\n");
                builder.Append($"  Length:  {anim.Length:0.###} s\n");
                builder.Append($"  Channels: {anim.Channels.Count}\n");
                builder.Append($"  Events:   {anim.Events.Count}\n");

                int totalComponents = anim.Channels.Sum(c => c.Components.Count);
                int totalKeyframes = anim.Channels.Sum(c => c.Components.Sum(comp => comp.Keyframes.Count));
                builder.Append($"  Components: {totalComponents}\n");
                builder.Append($"  Keyframes:  {totalKeyframes}\n");

                int shown = Math.Min(anim.Channels.Count, 100);
                if (shown > 0)
                {
                    builder.Append('\n');
                    for (int i = 0; i < shown; i++)
                    {
                        SpAnimationChannel channel = anim.Channels[i];
                        builder.Append($"  [{i}] {channel.Name}  keys={channel.KeyframeCount} comps={channel.ComponentCount}\n");
                    }

                    if (anim.Channels.Count > shown)
                    {
                        builder.Append($"  … {anim.Channels.Count - shown} more channel(s)\n");
                    }
                }

                return ResourcePreview.AsText(builder.ToString(), "anim");
            }

            // Triggers are stored as plain text (e.g. Config/*.txt) even though their type name is not ".txt".
            if (ResourceTypeIds.IsTrigger(type) && LooksLikeText(data))
            {
                string triggerText = TextCodec.Decode(data);
                if (triggerText.Length > MaxTextChars)
                {
                    triggerText = triggerText[..MaxTextChars] + "\n… (truncated)";
                }

                return ResourcePreview.AsText(triggerText, "text");
            }

            // Some resources are already standard images (e.g. the "png" type stores raw PNG data).
            if (IsPng(data))
            {
                return ResourcePreview.AsImage(data);
            }

            if (LooksLikeText(data))
            {
                string text = TextCodec.Decode(data);
                if (text.Length > MaxTextChars)
                {
                    text = text[..MaxTextChars] + "\n… (truncated)";
                }

                return ResourcePreview.AsText(text, "text");
            }

            return ResourcePreview.AsText(HexDump(data), "hex");
        }
        catch (Exception ex)
        {
            return ResourcePreview.AsText($"Preview unavailable: {ex.Message}\n\n{HexDump(data)}", "error");
        }
    }

    /// <summary>Decodes triangle geometry into a UI-free <see cref="PreviewModel"/> (null on failure/empty).</summary>
    private static PreviewModel? TryBuildModel(RenderWare4File rw4)
    {
        try
        {
            IReadOnlyList<Rw4MeshGeometry> geometry = rw4.ReadGeometry();
            var model = new PreviewModel();
            int triangles = 0;
            foreach (Rw4MeshGeometry mesh in geometry)
            {
                if (mesh.VertexCount == 0 || mesh.TriangleCount == 0)
                {
                    continue;
                }

                if (triangles + mesh.TriangleCount > MaxPreviewTriangles)
                {
                    break;
                }

                model.Meshes.Add(new PreviewMesh
                {
                    MeshSectionIndex = mesh.MeshSectionIndex,
                    Positions = mesh.Positions,
                    Normals = mesh.Normals,
                    TexCoords = mesh.TexCoords,
                    Indices = mesh.Indices,
                });

                triangles += mesh.TriangleCount;
            }

            if (model.Meshes.Count > 0)
            {
                AttachTextures(rw4, model);
                AttachSkeleton(rw4, model);
                return model;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Attaches rest-pose bone positions and parent links from the first skin, when present.</summary>
    private static void AttachSkeleton(RenderWare4File rw4, PreviewModel model)
    {
        try
        {
            foreach (Rw4SkinLinkage skin in rw4.ReadSkins())
            {
                Rw4Skeleton? skeleton = skin.Skeleton;
                Rw4SkinMatrixBuffer? matrices = skin.MatrixBuffer;
                if (skeleton is null || skeleton.Bones.Count == 0)
                {
                    continue;
                }

                int count = skeleton.Bones.Count;

                // Bone origins are in the bind-pose matrices; when those look local (all near the
                // origin) the hierarchy is composed so the skeleton is positioned sensibly.
                var local = new float[count][];
                for (int i = 0; i < count; i++)
                {
                    local[i] = matrices is not null && i < matrices.Matrices.Count
                        ? matrices.Matrices[i].Values
                        : new float[12];
                }

                var world = new float[count][];
                for (int i = 0; i < count; i++)
                {
                    int parent = skeleton.Bones[i].ParentIndex;
                    world[i] = parent >= 0 && parent < count ? Multiply(local[i], world[parent]) : (float[])local[i].Clone();
                    var joint = new[] { world[i][3], world[i][7], world[i][11] };
                    model.SkeletonJoints.Add(joint);
                    model.SkeletonBindJoints.Add((float[])joint.Clone());
                    model.SkeletonBones.Add((i, parent));
                }

                model.Skeleton = skeleton;
                IReadOnlyList<Rw4KeyframeAnim> animations = rw4.ReadKeyframeAnimations();
                if (animations.Count > 0)
                {
                    model.SkeletonAnimation = animations[0];
                    model.AnimationLength = animations[0].Length;
                }

                return;
            }
        }
        catch
        {
            // A broken skeleton must not break the model preview.
        }
    }

    /// <summary>Multiplies two 3x4 (row-major) matrices: result = a * b.</summary>
    private static float[] Multiply(float[] a, float[] b)
    {
        var r = new float[12];
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                // Rotation part * b + translation of a (for col == 3 use a's translation).
                r[(row * 4) + col] = (a[(row * 4) + 0] * b[(0 * 4) + col])
                    + (a[(row * 4) + 1] * b[(1 * 4) + col])
                    + (a[(row * 4) + 2] * b[(2 * 4) + col])
                    + (col == 3 ? a[(row * 4) + 3] : 0);
            }
        }

        return r;
    }

    /// <summary>
    /// Assigns each mesh its own diffuse texture (via its material's sampler-0 slot) and falls back to
    /// a model-wide texture when a mesh has no material/texture.
    /// </summary>
    private static void AttachTextures(RenderWare4File rw4, PreviewModel model)
    {
        try
        {
            IReadOnlyList<Rw4Raster> rasters = rw4.ReadRasters();
            IReadOnlyList<Rw4Material> materials = rw4.ReadMaterials();

            var meshToMaterial = new Dictionary<int, int>();
            foreach (Rw4MaterialLink link in rw4.ReadMaterialLinks())
            {
                if (link.MaterialSectionIndices.Count > 0)
                {
                    meshToMaterial[link.MeshSectionIndex] = link.MaterialSectionIndices[0];
                }
            }

            byte[]? fallback = null;
            foreach (Rw4Raster raster in rasters)
            {
                byte[]? png = DecodeRasterToPng(rw4, raster);
                if (png is not null)
                {
                    fallback = png;
                    break;
                }
            }

            model.TexturePng = fallback;

            // Give each mesh its own material's diffuse texture when it has one.
            foreach (PreviewMesh mesh in model.Meshes)
            {
                if (!meshToMaterial.TryGetValue(mesh.MeshSectionIndex, out int materialSection))
                {
                    mesh.TexturePng ??= fallback;
                    continue;
                }

                Rw4Material? material = materials.FirstOrDefault(m => m.SectionIndex == materialSection);
                if (material is not null && material.TextureSlots.Count > 0)
                {
                    int rasterSection = PickDiffuseSlot(material);
                    Rw4Raster? raster = rasters.FirstOrDefault(r => r.SectionIndex == rasterSection);
                    if (raster is not null)
                    {
                        byte[]? png = DecodeRasterToPng(rw4, raster);
                        if (png is not null)
                        {
                            mesh.TexturePng = png;
                        }
                    }
                }

                mesh.TexturePng ??= fallback;
            }
        }
        catch
        {
            // Textures are optional; never fail the model preview because of them.
        }
    }

    /// <summary>Picks the raster reference of the material's lowest sampler (diffuse is usually sampler 0).</summary>
    private static int PickDiffuseSlot(Rw4Material material)
    {
        int best = -1;
        int bestSampler = int.MaxValue;
        foreach (Rw4TextureSlotMaterial slot in material.TextureSlots)
        {
            if (slot.RasterSectionIndex < 0)
            {
                continue;
            }

            if (slot.SamplerIndex < bestSampler)
            {
                bestSampler = slot.SamplerIndex;
                best = slot.RasterSectionIndex;
            }
        }

        return best;
    }

    private static byte[]? DecodeRasterToPng(RenderWare4File rw4, Rw4Raster raster)
    {
        byte[]? pixels = rw4.GetRasterData(raster);
        if (pixels is null || pixels.Length == 0)
        {
            return null;
        }

        byte[]? rgba = raster.DecodeRgba(pixels);
        return rgba is null ? null : PngCodec.Encode(new RgbaImage(raster.Width, raster.Height, rgba));
    }

    /// <summary>Decodes the first usable raster section of an RW4 texture resource to PNG, or null.</summary>
    private static byte[]? TryDecodeRw4Texture(RenderWare4File rw4)
    {
        try
        {
            foreach (Rw4Raster raster in rw4.ReadRasters())
            {
                byte[]? png = DecodeRasterToPng(rw4, raster);
                if (png is not null)
                {
                    return png;
                }
            }
        }
        catch
        {
            // fall through to the text summary
        }

        return null;
    }

    private static void AppendRw4Summary(RenderWare4File rw4, StringBuilder builder)
    {
        try
        {
            IReadOnlyList<Rw4Mesh> meshes = rw4.ReadMeshes();
            if (meshes.Count > 0)
            {
                long triangles = meshes.Sum(m => (long)m.TriangleCount);
                long vertices = meshes.Sum(m => (long)m.VertexCount);
                builder.Append($"\n  Meshes: {meshes.Count} ({triangles} triangles, {vertices} vertices)\n");
            }

            IReadOnlyList<Rw4Skeleton> skeletons = rw4.ReadSkeletons();
            if (skeletons.Count > 0)
            {
                builder.Append($"  Skeletons: {skeletons.Count} ({skeletons.Sum(s => s.Bones.Count)} bones)\n");
            }

            IReadOnlyList<Rw4SkinLinkage> skins = rw4.ReadSkins();
            if (skins.Count > 0)
            {
                builder.Append($"  Skins: {skins.Count}\n");
            }

            IReadOnlyList<Rw4KeyframeAnim> animations = rw4.ReadKeyframeAnimations();
            if (animations.Count > 0)
            {
                int channels = animations.Sum(a => a.Channels.Count);
                int keyframes = animations.Sum(a => a.Channels.Sum(c => c.Keyframes.Count));
                builder.Append($"  Keyframe animations: {animations.Count} ({channels} channels, {keyframes} keyframes)\n");
            }

            IReadOnlyList<Rw4Material> materials = rw4.ReadMaterials();
            if (materials.Count > 0)
            {
                builder.Append($"  Materials: {materials.Count}\n");
                foreach (Rw4Material material in materials.Take(8))
                {
                    builder.Append(
                        $"    shader 0x{material.RendererId:X8}, " +
                        $"{Direct3DStates.FormatPrimitiveType(material.PrimitiveType)}, " +
                        $"states {material.RenderGroups.Sum(g => g.States.Count)}, " +
                        $"textures {material.TextureSlots.Count}" +
                        $"{(material.Decompiled ? string.Empty : " (undecoded)")}\n");
                }
            }

            IReadOnlyList<Rw4MorphHandle> morphs = rw4.ReadMorphHandles();
            if (morphs.Count > 0)
            {
                builder.Append($"  Morph handles: {morphs.Count}\n");
            }

            IReadOnlyList<Rw4BlendShape> shapes = rw4.ReadBlendShapes();
            if (shapes.Count > 0)
            {
                builder.Append($"  Blend shapes: {shapes.Count}\n");
            }

            IReadOnlyList<Rw4Raster> rasters = rw4.ReadRasters();
            if (rasters.Count > 0)
            {
                builder.Append($"  Textures: {rasters.Count}\n");
                foreach (Rw4Raster raster in rasters.Take(6))
                {
                    builder.Append($"    {raster.Width}x{raster.Height} {raster.FormatName}, {raster.MipmapLevels} mip(s)\n");
                }
            }
        }
        catch (Exception ex)
        {
            builder.Append($"\n  (RW4 summary unavailable: {ex.Message})\n");
        }
    }

    private static bool IsPng(byte[] data) =>
        data.Length >= 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47;

    private static bool LooksLikeText(byte[] data)
    {
        int limit = Math.Min(data.Length, 1024);
        if (limit == 0)
        {
            return true;
        }

        int printable = 0;
        for (int i = 0; i < limit; i++)
        {
            byte b = data[i];
            if (b == 0)
            {
                return false;
            }

            if (b is 9 or 10 or 13 || (b >= 32 && b < 127))
            {
                printable++;
            }
        }

        return printable >= limit * 0.9;
    }

    private static string HexDump(byte[] data)
    {
        int count = Math.Min(data.Length, MaxHexBytes);
        var builder = new StringBuilder();
        builder.Append($"Binary data ({data.Length} bytes)").Append('\n').Append('\n');

        for (int offset = 0; offset < count; offset += 16)
        {
            builder.Append(offset.ToString("X8")).Append("  ");

            var ascii = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                int index = offset + i;
                if (index < count)
                {
                    byte b = data[index];
                    builder.Append(b.ToString("X2")).Append(' ');
                    ascii.Append(b is >= 32 and < 127 ? (char)b : '.');
                }
                else
                {
                    builder.Append("   ");
                }
            }

            builder.Append(' ').Append(ascii).Append('\n');
        }

        if (data.Length > count)
        {
            builder.Append("\n… (truncated)");
        }

        return builder.ToString();
    }
}

/// <summary>UTF-8 decoding that falls back to Latin1 when the bytes are not valid UTF-8.</summary>
internal static class TextCodec
{
    public static string Decode(byte[] data)
    {
        try
        {
            return new UTF8Encoding(false, true).GetString(data);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(data);
        }
    }
}
