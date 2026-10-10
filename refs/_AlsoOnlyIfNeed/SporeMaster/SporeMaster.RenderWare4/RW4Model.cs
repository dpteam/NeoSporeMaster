using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SporeMaster.RenderWare4;

public class RW4Model
{
	public enum FileTypes
	{
		Model,
		Texture
	}

	private RW4Header header;

	public IList<RW4Section> Sections => header.sections;

	public FileTypes FileType
	{
		get
		{
			return header.file_type;
		}
		set
		{
			header.file_type = value;
		}
	}

	public IList<RW4Object> GetObjects(uint type_code)
	{
		return (from s in header.sections
			where s.type_code == type_code
			select s.obj).ToList();
	}

	public void AddObject(RW4Object obj, uint type_code)
	{
		obj.section = InsertSection(header.sections.Length);
		obj.section.type_code = type_code;
		obj.section.obj = obj;
		obj.section.alignment = 16u;
	}

	public void RemoveSection(RW4Section section)
	{
		if (header.sections[section.number] != section)
		{
			throw new ArgumentException("Attempt to remove invalid section.");
		}
		RW4Section[] new_sections = new RW4Section[header.sections.Length - 1];
		for (int i = 0; i < section.number; i++)
		{
			new_sections[i] = header.sections[i];
		}
		for (int j = (int)section.number; j < new_sections.Length; j++)
		{
			new_sections[j] = header.sections[j + 1];
			new_sections[j].number = (uint)j;
		}
		header.sections = new_sections;
		section.number = uint.MaxValue;
	}

	public RW4Section InsertSection(int index)
	{
		RW4Section[] new_sections = new RW4Section[header.sections.Length + 1];
		for (int i = 0; i < index; i++)
		{
			new_sections[i] = header.sections[i];
		}
		for (int j = index + 1; j < new_sections.Length; j++)
		{
			new_sections[j] = header.sections[j - 1];
			new_sections[j].number = (uint)j;
		}
		RW4Section s = (new_sections[index] = new RW4Section());
		s.number = (uint)index;
		header.sections = new_sections;
		return s;
	}

	private void Pack()
	{
		RW4Section[] sections = header.sections;
		foreach (RW4Section s in sections)
		{
			int cs = s.obj.ComputeSize();
			if (cs != -1)
			{
				s.size = (uint)cs;
			}
		}
		RW4Section[] sections2 = header.sections;
		uint p = header.GetHeaderEnd();
		for (int j = 0; j < sections2.Length; j++)
		{
			if (sections2[j].type_code != 65584)
			{
				uint np = (p + sections2[j].alignment - 1) & ~(sections2[j].alignment - 1);
				if (sections2[j].pos != np)
				{
					sections2[j].pos = np;
				}
				p = sections2[j].pos + sections2[j].size;
			}
		}
		header.section_index_begin = p;
		p = header.section_index_end;
		for (int k = 0; k < sections2.Length; k++)
		{
			if (sections2[k].type_code == 65584)
			{
				uint np2 = (p + sections2[k].alignment - 1) & ~(sections2[k].alignment - 1);
				if (sections2[k].pos != np2)
				{
					sections2[k].pos = np2;
				}
				p = sections2[k].pos + sections2[k].size;
			}
		}
	}

	public void New()
	{
		header = new RW4Header();
	}

	public void Read(Stream r)
	{
		header = new RW4Header();
		header.Read(r);
		RW4Section[] sections = header.sections;
		foreach (RW4Section section in sections)
		{
			switch (section.type_code)
			{
			case 131081u:
			{
				section.GetObject<RW4Mesh>(this, r, out var _);
				break;
			}
			case 458763u:
			{
				section.GetObject<RW4Material>(this, r, out var _);
				break;
			}
			case 458764u:
			{
				section.GetObject<RW4Skeleton>(this, r, out var _);
				break;
			}
			case 131075u:
			{
				section.GetObject<Texture>(this, r, out var _);
				break;
			}
			case 458754u:
			{
				section.GetObject<RW4HierarchyInfo>(this, r, out var _);
				break;
			}
			case 131098u:
			{
				section.GetObject<RWMeshMaterialAssignment>(this, r, out var _);
				break;
			}
			case 524293u:
			{
				section.GetObject<RW4BBox>(this, r, out var _);
				break;
			}
			case 16711680u:
			{
				section.GetObject<ModelHandles>(this, r, out var _);
				break;
			}
			case 458753u:
			{
				section.GetObject<Anim>(this, r, out var _);
				break;
			}
			case 16711681u:
			{
				section.GetObject<Animations>(this, r, out var _);
				break;
			}
			}
		}
		RW4Section[] sections2 = header.sections;
		foreach (RW4Section section2 in sections2)
		{
			if (section2.obj == null)
			{
				section2.LoadObject(this, new UnreferencedSection(), r);
			}
		}
	}

	public void Write(Stream w)
	{
		Pack();
		header.Write(w);
		RW4Section[] sections = header.sections.OrderBy((RW4Section s) => s.pos).ToArray();
		bool wrote_index = false;
		for (int i = 0; i < sections.Length; i++)
		{
			sections[i].Write(w, this);
			if (!wrote_index && (i + 1 == sections.Length || sections[i + 1].pos >= header.section_index_end))
			{
				w.WritePadding(header.section_index_begin - w.Position);
				header.WriteIndex(w);
				wrote_index = true;
			}
		}
	}

	public void Test()
	{
	}
}
