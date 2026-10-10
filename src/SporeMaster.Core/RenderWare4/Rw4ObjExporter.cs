using System.Globalization;
using System.Text;

namespace SporeMaster.Core.RenderWare4;

/// <summary>Writes extracted <see cref="Rw4MeshGeometry"/> to the Wavefront OBJ text format.</summary>
public static class Rw4ObjExporter
{
    /// <summary>Serialises geometry to OBJ text (one <c>o</c> group per mesh).</summary>
    public static string Export(IEnumerable<Rw4MeshGeometry> meshes)
    {
        var builder = new StringBuilder();
        builder.Append("# Exported by SporeMaster 3\n");

        int vertexBase = 1;
        int texCoordBase = 1;
        int normalBase = 1;
        int meshIndex = 0;

        foreach (Rw4MeshGeometry mesh in meshes)
        {
            builder.Append("o mesh_").Append(meshIndex).Append('\n');

            for (int v = 0; v < mesh.VertexCount; v++)
            {
                builder.Append("v ")
                    .Append(Format(mesh.Positions[(v * 3) + 0])).Append(' ')
                    .Append(Format(mesh.Positions[(v * 3) + 1])).Append(' ')
                    .Append(Format(mesh.Positions[(v * 3) + 2])).Append('\n');
            }

            if (mesh.HasTexCoords)
            {
                for (int v = 0; v < mesh.VertexCount; v++)
                {
                    builder.Append("vt ")
                        .Append(Format(mesh.TexCoords[(v * 2) + 0])).Append(' ')
                        .Append(Format(mesh.TexCoords[(v * 2) + 1])).Append('\n');
                }
            }

            if (mesh.HasNormals)
            {
                for (int v = 0; v < mesh.VertexCount; v++)
                {
                    builder.Append("vn ")
                        .Append(Format(mesh.Normals[(v * 3) + 0])).Append(' ')
                        .Append(Format(mesh.Normals[(v * 3) + 1])).Append(' ')
                        .Append(Format(mesh.Normals[(v * 3) + 2])).Append('\n');
                }
            }

            for (int t = 0; t < mesh.TriangleCount; t++)
            {
                builder.Append("f ");
                for (int k = 0; k < 3; k++)
                {
                    int index = mesh.Indices[(t * 3) + k];
                    if (k > 0)
                    {
                        builder.Append(' ');
                    }

                    builder.Append(index + vertexBase);
                    if (mesh.HasTexCoords || mesh.HasNormals)
                    {
                        builder.Append('/');
                        if (mesh.HasTexCoords)
                        {
                            builder.Append(index + texCoordBase);
                        }

                        if (mesh.HasNormals)
                        {
                            builder.Append('/').Append(index + normalBase);
                        }
                    }
                }

                builder.Append('\n');
            }

            vertexBase += mesh.VertexCount;
            texCoordBase += mesh.HasTexCoords ? mesh.VertexCount : 0;
            normalBase += mesh.HasNormals ? mesh.VertexCount : 0;
            meshIndex++;
        }

        return builder.ToString();
    }

    public static void Export(IEnumerable<Rw4MeshGeometry> meshes, string path) =>
        File.WriteAllText(path, Export(meshes), new UTF8Encoding(false));

    private static string Format(float value) =>
        value.ToString("0.######", CultureInfo.InvariantCulture);
}
