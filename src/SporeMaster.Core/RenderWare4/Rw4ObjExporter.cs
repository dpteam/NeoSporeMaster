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
        int meshIndex = 0;
        foreach (Rw4MeshGeometry mesh in meshes)
        {
            builder.Append("o mesh_").Append(meshIndex).Append('\n');

            for (int v = 0; v < mesh.VertexCount; v++)
            {
                float x = mesh.Positions[(v * 3) + 0];
                float y = mesh.Positions[(v * 3) + 1];
                float z = mesh.Positions[(v * 3) + 2];
                builder.Append("v ")
                    .Append(Format(x)).Append(' ')
                    .Append(Format(y)).Append(' ')
                    .Append(Format(z)).Append('\n');
            }

            for (int t = 0; t < mesh.TriangleCount; t++)
            {
                int i = mesh.Indices[(t * 3) + 0] + vertexBase;
                int j = mesh.Indices[(t * 3) + 1] + vertexBase;
                int k = mesh.Indices[(t * 3) + 2] + vertexBase;
                builder.Append("f ").Append(i).Append(' ').Append(j).Append(' ').Append(k).Append('\n');
            }

            vertexBase += mesh.VertexCount;
            meshIndex++;
        }

        return builder.ToString();
    }

    public static void Export(IEnumerable<Rw4MeshGeometry> meshes, string path) =>
        File.WriteAllText(path, Export(meshes), new UTF8Encoding(false));

    private static string Format(float value) =>
        value.ToString("0.######", CultureInfo.InvariantCulture);
}
