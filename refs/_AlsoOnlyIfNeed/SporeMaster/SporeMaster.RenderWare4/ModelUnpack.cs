using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

public class ModelUnpack
{
	private string type;

	public string Type => type;

	public ModelUnpack(Stream input, string outputPath)
	{
		RW4Model model = new RW4Model();
		model.Read(input);
		if (model.FileType == RW4Model.FileTypes.Model)
		{
			unpackModel(model, outputPath + "\\model.mesh.xml");
		}
		else
		{
			unpackTexture(model, outputPath + "\\texture.dds");
		}
	}

	private void unpackModel(RW4Model model, string outputFileName)
	{
		IList<RW4Object> meshes = model.GetObjects(131081u);
		if (meshes.Count != 1)
		{
			throw new NotSupportedException("Only exactly one mesh supported.");
		}
		RW4Mesh mesh = meshes[0] as RW4Mesh;
		if (mesh.vertices == null)
		{
			throw new NotSupportedException("Only models with a vertex buffer can be unpacked.");
		}
		new OgreXmlWriter(mesh.vertices.vertices.ToArray(), mesh.triangles.triangles.ToArray(), outputFileName);
		type = "mesh";
	}

	private void unpackTexture(RW4Model model, string outputFileName)
	{
		IList<RW4Object> textures = model.GetObjects(131075u);
		if (textures.Count != 1)
		{
			throw new NotSupportedException("Only exactly one texture supported in a texture rw4.");
		}
		Texture texture = textures[0] as Texture;
		using (FileStream stream = File.Create(outputFileName))
		{
			stream.WriteU32(542327876u);
			stream.WriteU32(124u);
			stream.WriteU32(659463u);
			stream.WriteU32(texture.height);
			stream.WriteU32(texture.width);
			stream.WriteU32((uint)(texture.height * texture.width));
			stream.WriteU32(0u);
			stream.WriteU32(texture.mipmapInfo / 256);
			for (int i = 0; i < 11; i++)
			{
				stream.WriteU32(0u);
			}
			stream.WriteU32(32u);
			stream.WriteU32(4u);
			stream.WriteU32(texture.textureType);
			stream.WriteU32(32u);
			stream.WriteU32(16711680u);
			stream.WriteU32(65280u);
			stream.WriteU32(255u);
			stream.WriteU32(4278190080u);
			stream.WriteU32(0u);
			for (int j = 0; j < 4; j++)
			{
				stream.WriteU32(0u);
			}
			stream.Write(texture.texData.blob, 0, texture.texData.blob.Length);
		}
		type = "texture";
	}
}
