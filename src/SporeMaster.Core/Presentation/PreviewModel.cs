namespace SporeMaster.Core.Presentation;

/// <summary>One mesh of a <see cref="PreviewModel"/>: triangle geometry for a 3D preview.</summary>
public sealed class PreviewMesh
{
    /// <summary>Vertex positions as a flat <c>xyz</c> array.</summary>
    public float[] Positions { get; init; } = Array.Empty<float>();

    /// <summary>Optional vertex normals as a flat <c>xyz</c> array.</summary>
    public float[] Normals { get; init; } = Array.Empty<float>();

    /// <summary>Triangle indices into the vertex arrays.</summary>
    public int[] Indices { get; init; } = Array.Empty<int>();

    public int VertexCount => Positions.Length / 3;

    public int TriangleCount => Indices.Length / 3;

    public bool HasNormals => Normals.Length >= VertexCount * 3 && VertexCount > 0;
}

/// <summary>
/// Geometry for a 3D model preview, kept UI-free so it can be built and tested headlessly and then
/// handed to a viewer (the GUI builds a WPF mesh from it).
/// </summary>
public sealed class PreviewModel
{
    public List<PreviewMesh> Meshes { get; } = new();

    public int TotalVertices => Meshes.Sum(m => m.VertexCount);

    public int TotalTriangles => Meshes.Sum(m => m.TriangleCount);

    /// <summary>Axis-aligned bounding box of every mesh: (min, max) in model space.</summary>
    public (float[] Min, float[] Max) Bounds()
    {
        var min = new[] { float.MaxValue, float.MaxValue, float.MaxValue };
        var max = new[] { float.MinValue, float.MinValue, float.MinValue };
        foreach (PreviewMesh mesh in Meshes)
        {
            for (int i = 0; i + 2 < mesh.Positions.Length; i += 3)
            {
                for (int axis = 0; axis < 3; axis++)
                {
                    float value = mesh.Positions[i + axis];
                    if (value < min[axis])
                    {
                        min[axis] = value;
                    }

                    if (value > max[axis])
                    {
                        max[axis] = value;
                    }
                }
            }
        }

        if (min[0] > max[0])
        {
            return (new float[3], new float[3]);
        }

        return (min, max);
    }
}
