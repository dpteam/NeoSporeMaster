using System.IO;
using System.Linq;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

public class ModelPack
{
	public ModelPack(string inputFileName, Stream output)
	{
		OgreXmlReader src_model = new OgreXmlReader(inputFileName);
		RW4Model model = new RW4Model();
		model.New();
		pack(src_model, model);
		model.Write(output);
	}

	private void pack(OgreXmlReader src_model, RW4Model model)
	{
		model.FileType = RW4Model.FileTypes.Model;
		RW4Skeleton skel = new RW4Skeleton();
		skel.unk1 = 9268640u;
		skel.mat3 = new Matrices4x3
		{
			items = new Mat4x3[1]
			{
				new Mat4x3
				{
					m = new float[12]
					{
						1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f,
						1f, 0f
					}
				}
			}
		};
		skel.mat4 = new Matrices4x4
		{
			items = new Mat4x4[1]
			{
				new Mat4x4
				{
					m = new float[16]
					{
						0f, 1f, 0f, 0f, -1f, 0f, 0f, 0f, 0f, 0f,
						1f, 0f, 0f, 0f, 0f, 0f
					}
				}
			}
		};
		skel.jointInfo = new RW4HierarchyInfo
		{
			id = "skeleton1".FNV(),
			items = new RW4HierarchyInfo.Item[1]
			{
				new RW4HierarchyInfo.Item
				{
					index = 0,
					name_fnv = "joint1".FNV(),
					flags = 1u,
					parent = null
				}
			}
		};
		Anim anim = new Anim();
		anim.skeleton_id = skel.jointInfo.id;
		anim.flags = 3u;
		anim.length = 1.25f;
		anim.channel_names = skel.jointInfo.items.Select((RW4HierarchyInfo.Item j) => j.name_fnv).ToArray();
		Anim anim2 = anim;
		anim2.channel_frame_pose = new JointPose[1, 1] { 
		{
			new JointPose
			{
				qx = 0f,
				qy = 0f,
				qz = 0f,
				qs = 1f,
				tx = 0f,
				ty = 0f,
				tz = 0f,
				sx = 1f,
				sy = 1f,
				sz = 1f,
				time = 1.25f
			}
		} };
		RW4Mesh rW4Mesh = new RW4Mesh();
		rW4Mesh.vertices = new RW4VertexArray();
		rW4Mesh.triangles = new RW4TriangleArray();
		RW4Mesh mesh = rW4Mesh;
		mesh.vertices.unk2 = 0u;
		mesh.vertices.format = new VertexFormat
		{
			blob = RW4Garbage.vertex_format
		};
		mesh.vertices.vertices = new Buffer<Vertex>(0);
		mesh.vertices.vertices.Assign(src_model.vertices);
		mesh.triangles.unk1 = 0u;
		mesh.triangles.triangles = new Buffer<Triangle>(0);
		mesh.triangles.triangles.Assign(src_model.triangles);
		Texture texture = new Texture();
		texture.width = 64;
		texture.height = 64;
		texture.mipmapInfo = 1800u;
		texture.textureType = 894720068u;
		texture.unk1 = 0u;
		texture.texData = new TextureBlob
		{
			blob = new byte[5488]
		};
		Texture texture2 = texture;
		RW4TexMetadata rW4TexMetadata = new RW4TexMetadata();
		rW4TexMetadata.unk_data_1 = RW4Garbage.texture_format_1;
		rW4TexMetadata.unk_data_2 = RW4Garbage.texture_format_2;
		rW4TexMetadata.texture = texture2;
		RW4TexMetadata texture_format = rW4TexMetadata;
		RWMeshMaterialAssignment rWMeshMaterialAssignment = new RWMeshMaterialAssignment();
		rWMeshMaterialAssignment.mesh = mesh;
		rWMeshMaterialAssignment.mat = new RW4TexMetadata[1] { texture_format };
		RWMeshMaterialAssignment meshMaterial = rWMeshMaterialAssignment;
		model.AddObject(anim2, 458753u);
		model.AddObject(mesh.vertices.format, 131076u);
		model.AddObject(skel.mat4, 458755u);
		model.AddObject(skel.mat3, 458767u);
		skel.mat3.section.fixup_offsets.Add(16u);
		model.AddObject(skel, 458764u);
		model.AddObject(mesh.triangles.triangles, 65584u);
		model.AddObject(mesh.triangles, 131079u);
		model.AddObject(mesh, 131081u);
		model.AddObject(texture_format, 131083u);
		model.AddObject(meshMaterial, 131098u);
		model.AddObject(skel.jointInfo, 458754u);
		model.AddObject(mesh.vertices.vertices, 65584u);
		model.AddObject(mesh.vertices, 131077u);
		model.AddObject(texture2.texData, 65584u);
		model.AddObject(texture2, 131075u);
	}
}
