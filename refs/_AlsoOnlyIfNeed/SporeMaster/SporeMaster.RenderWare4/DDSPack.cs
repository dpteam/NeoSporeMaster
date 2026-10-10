using System;
using System.IO;
using System.Linq;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

public class DDSPack
{
	public DDSPack(string inputFileName, Stream output)
	{
		using FileStream src = File.OpenRead(inputFileName);
		RW4Model model = new RW4Model();
		model.New();
		pack(src, model);
		model.Write(output);
	}

	private void pack(Stream src, RW4Model model)
	{
		src.expect(542327876u, "DDS000");
		int headerSize = src.ReadS32();
		if (headerSize < 124)
		{
			throw new ModelFormatException(src, "DDS001", headerSize);
		}
		src.ReadU32();
		int height = src.ReadS32();
		int width = src.ReadS32();
		src.ReadU32();
		src.expect(0u, "DDS002");
		int mipmaps = src.ReadS32();
		src.Seek(src.Position + 44, SeekOrigin.Begin);
		int pfsize = src.ReadS32();
		if (pfsize < 32)
		{
			throw new ModelFormatException(src, "DDS011", pfsize);
		}
		uint pf_flags = src.ReadU32();
		if ((pf_flags & 4) == 0)
		{
			throw new ModelFormatException(src, "DDS012", pf_flags);
		}
		uint fourcc = src.ReadU32();
		if (fourcc != 894720068)
		{
			throw new NotSupportedException("Texture packing currently only supports DXT5 compressed input textures.");
		}
		src.Seek(headerSize + 4, SeekOrigin.Begin);
		int[] sizes = (from num in Enumerable.Range(0, mipmaps)
			select Math.Max(width >> num, 4) * Math.Max(height >> num, 4)).ToArray();
		byte[] all_mipmaps = new byte[sizes.Sum()];
		int offset = 0;
		for (int i = 0; i < mipmaps; i++)
		{
			if (src.Read(all_mipmaps, offset, sizes[i]) != sizes[i])
			{
				throw new ModelFormatException(src, "Unexpected EOF reading .DDS file", null);
			}
			offset += sizes[i];
		}
		model.FileType = RW4Model.FileTypes.Texture;
		Texture texture = new Texture();
		texture.width = (ushort)width;
		texture.height = (ushort)height;
		texture.mipmapInfo = (uint)(256 * mipmaps + 8);
		texture.textureType = fourcc;
		texture.texData = new TextureBlob
		{
			blob = all_mipmaps
		};
		texture.unk1 = 0u;
		Texture texture2 = texture;
		model.AddObject(texture2.texData, 65584u);
		model.AddObject(texture2, 131075u);
	}
}
