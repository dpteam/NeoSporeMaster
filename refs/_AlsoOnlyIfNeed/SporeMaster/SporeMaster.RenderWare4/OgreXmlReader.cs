using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SporeMaster.RenderWare4;

internal class OgreXmlReader
{
	public List<Vertex> vertices = new List<Vertex>();

	public List<Triangle> triangles = new List<Triangle>();

	private void DecodeVertexBuffers(XElement geom)
	{
		if (geom == null)
		{
			return;
		}
		Vertex[] vertices = new Vertex[int.Parse(geom.Attribute("vertexcount").Value)];
		int v;
		for (v = 0; v < vertices.Length; v++)
		{
			vertices[v].packed_bone_indices = 0u;
			vertices[v].packed_bone_weights = 255u;
		}
		IEnumerable<XElement> vertex_elements = geom.Elements("vertexbuffer").Elements("vertex");
		v = 0;
		foreach (XElement p in vertex_elements.Elements("position"))
		{
			vertices[v].x = float.Parse(p.Attribute("x").Value);
			vertices[v].y = float.Parse(p.Attribute("y").Value);
			vertices[v].z = float.Parse(p.Attribute("z").Value);
			v++;
		}
		if (v != vertices.Length)
		{
			throw new ModelFormatException(null, "Missing vertex position data.", v);
		}
		v = 0;
		foreach (XElement p2 in vertex_elements.Elements("normal"))
		{
			vertices[v].normal = Vertex.PackNormal(float.Parse(p2.Attribute("x").Value), float.Parse(p2.Attribute("y").Value), float.Parse(p2.Attribute("z").Value));
			v++;
		}
		if (v != vertices.Length)
		{
			throw new ModelFormatException(null, "Missing vertex normal data.", v);
		}
		v = 0;
		foreach (XElement p3 in vertex_elements.Elements("tangent"))
		{
			vertices[v].tangent = Vertex.PackNormal(float.Parse(p3.Attribute("x").Value), float.Parse(p3.Attribute("y").Value), float.Parse(p3.Attribute("z").Value));
			v++;
		}
		if (v != vertices.Length)
		{
			throw new ModelFormatException(null, "Missing vertex tangent data.", v);
		}
		v = 0;
		foreach (XElement p4 in vertex_elements.Select((XElement vx) => vx.Element("texcoord")))
		{
			if (v < vertices.Length)
			{
				vertices[v].u = float.Parse(p4.Attribute("u").Value);
				vertices[v].v = float.Parse(p4.Attribute("v").Value);
				v++;
				continue;
			}
			break;
		}
		if (v != vertices.Length)
		{
			throw new ModelFormatException(null, "Missing vertex texture coordinates.", v);
		}
		this.vertices.AddRange(vertices);
	}

	public OgreXmlReader(string uri)
	{
		XElement mesh = XElement.Load(uri);
		DecodeVertexBuffers(mesh.Element("sharedgeometry"));
		foreach (XElement submesh in mesh.Element("submeshes").Elements("submesh"))
		{
			XAttribute optype = submesh.Attribute("operationtype");
			if (optype != null && optype.Value != "triangle_list")
			{
				throw new ModelFormatException(null, "Unsupported face type", submesh.Attribute("operationtype").Value);
			}
			uint first_vertex = 0u;
			XAttribute shared = submesh.Attribute("sharedgeometry");
			if (shared == null || shared.Value != "true")
			{
				first_vertex = (uint)vertices.Count;
			}
			IEnumerable<Triangle> triangles = from face in submesh.Element("faces").Elements("face")
				select new Triangle
				{
					i = first_vertex + uint.Parse(face.Attribute("v1").Value),
					j = first_vertex + uint.Parse(face.Attribute("v2").Value),
					k = first_vertex + uint.Parse(face.Attribute("v3").Value)
				};
			this.triangles.AddRange(triangles);
			DecodeVertexBuffers(submesh.Element("geometry"));
		}
	}
}
