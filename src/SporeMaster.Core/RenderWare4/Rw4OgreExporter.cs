using System.Globalization;
using System.Text;

namespace SporeMaster.Core.RenderWare4;

/// <summary>
/// Exports extracted <see cref="Rw4MeshGeometry"/> to the Ogre XML mesh format (<c>.mesh.xml</c>),
/// the interchange format used by the Ogre engine and many modelling tools. Each mesh becomes one
/// submesh.
/// </summary>
/// <remarks>
/// Only the static mesh is written (positions, normals, texture coordinates and triangle faces);
/// skeletal animation is not part of the Ogre <c>.mesh.xml</c> and is not exported here.
/// </remarks>
public static class Rw4OgreExporter
{
    /// <summary>Serialises meshes to an Ogre <c>.mesh.xml</c> document.</summary>
    public static string ExportMesh(
        string meshName,
        IEnumerable<Rw4MeshGeometry> meshes,
        Func<Rw4MeshGeometry, string?>? materialName = null)
    {
        var builder = new StringBuilder();
        builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        builder.Append("<mesh>\n");
        builder.Append("  <submeshes>\n");

        int index = 0;
        var names = new List<(string Name, int Index)>();
        foreach (Rw4MeshGeometry mesh in meshes)
        {
            string submeshName = $"mesh_{index}";
            string material = materialName?.Invoke(mesh) ?? "BaseWhite";
            names.Add((submeshName, index));

            builder.Append($"    <submesh material=\"{Escape(material)}\" usesharedvertices=\"false\" use32bitindexes=\"true\" operationtype=\"triangle_list\">\n");

            builder.Append($"      <faces count=\"{mesh.TriangleCount}\">\n");
            for (int t = 0; t < mesh.TriangleCount; t++)
            {
                int a = mesh.Indices[(t * 3) + 0];
                int b = mesh.Indices[(t * 3) + 1];
                int c = mesh.Indices[(t * 3) + 2];
                builder.Append($"        <face v1=\"{a}\" v2=\"{b}\" v3=\"{c}\"/>\n");
            }

            builder.Append("      </faces>\n");
            builder.Append($"      <geometry vertexcount=\"{mesh.VertexCount}\">\n");
            for (int v = 0; v < mesh.VertexCount; v++)
            {
                builder.Append("        <vertex>\n");
                builder.Append($"          <position x=\"{F(mesh.Positions[(v * 3) + 0])}\" y=\"{F(mesh.Positions[(v * 3) + 1])}\" z=\"{F(mesh.Positions[(v * 3) + 2])}\"/>\n");
                if (mesh.HasNormals)
                {
                    builder.Append($"          <normal x=\"{F(mesh.Normals[(v * 3) + 0])}\" y=\"{F(mesh.Normals[(v * 3) + 1])}\" z=\"{F(mesh.Normals[(v * 3) + 2])}\"/>\n");
                }

                if (mesh.HasTexCoords)
                {
                    builder.Append($"          <texcoord u=\"{F(mesh.TexCoords[(v * 2) + 0])}\" v=\"{F(mesh.TexCoords[(v * 2) + 1])}\"/>\n");
                }

                builder.Append("        </vertex>\n");
            }

            builder.Append("      </geometry>\n");
            builder.Append("    </submesh>\n");
            index++;
        }

        builder.Append("  </submeshes>\n");
        builder.Append("  <submeshnames>\n");
        foreach ((string name, int i) in names)
        {
            builder.Append($"    <submeshname name=\"{Escape(name)}\" index=\"{i}\"/>\n");
        }

        builder.Append("  </submeshnames>\n");
        builder.Append("</mesh>\n");
        return builder.ToString();
    }

    /// <summary>Serialises an Ogre material script referencing each material's diffuse texture.</summary>
    public static string ExportMaterial(IEnumerable<(string Name, string? DiffuseTexture)> materials)
    {
        var builder = new StringBuilder();
        builder.Append("// Ogre material script exported by SporeMaster 3\n");
        foreach ((string name, string? texture) in materials)
        {
            builder.Append($"material {name}\n{{\n");
            builder.Append("  technique\n  {\n    pass\n    {\n");
            builder.Append("      ambient 0.5 0.5 0.5\n");
            builder.Append("      diffuse 1 1 1\n");
            if (!string.IsNullOrEmpty(texture))
            {
                builder.Append("      texture_unit\n      {\n");
                builder.Append($"        texture {texture}\n");
                builder.Append("      }\n");
            }

            builder.Append("    }\n  }\n}\n\n");
        }

        return builder.ToString();
    }

    /// <summary>Writes a <c>.mesh.xml</c> and a matching <c>.material</c> script.</summary>
    public static void Export(
        string meshPath,
        IEnumerable<Rw4MeshGeometry> meshes,
        IReadOnlyList<Rw4MeshGeometry> materialMeshes,
        Func<Rw4MeshGeometry, string?>? materialName,
        IReadOnlyList<(string Name, string? DiffuseTexture)> materials)
    {
        string baseName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(meshPath));
        File.WriteAllText(meshPath, ExportMesh(baseName, meshes, materialName), new UTF8Encoding(false));

        if (materials.Count > 0)
        {
            string materialPath = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(meshPath)) ?? ".",
                baseName + ".material");
            File.WriteAllText(materialPath, ExportMaterial(materials), new UTF8Encoding(false));
        }
    }

    private static string F(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    private static string Escape(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
