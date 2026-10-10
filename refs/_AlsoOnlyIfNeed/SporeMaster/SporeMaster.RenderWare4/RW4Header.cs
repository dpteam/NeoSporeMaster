using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

internal class RW4Header
{
	private readonly byte[] RW4_Magic = new byte[28]
	{
		137, 82, 87, 52, 119, 51, 50, 0, 13, 10,
		26, 10, 0, 32, 4, 0, 52, 53, 52, 0,
		48, 48, 48, 0, 0, 0, 0, 0
	};

	private readonly uint[] fixed_section_types = new uint[5] { 0u, 65584u, 65585u, 65586u, 65552u };

	public RW4Model.FileTypes file_type;

	public uint unknown_bits_030;

	public uint section_index_begin;

	public uint section_index_padding;

	public RW4Section[] sections = new RW4Section[0];

	public uint section_index_end => (uint)(section_index_begin + 24 * sections.Length + 8 * getFixupCount() + section_index_padding);

	private uint getFixupCount()
	{
		uint total = 0u;
		RW4Section[] array = sections;
		foreach (RW4Section s in array)
		{
			total += (uint)s.fixup_offsets.Count;
		}
		return total;
	}

	public void Read(Stream r)
	{
		byte[] b = r.ReadBytes(RW4_Magic.Length);
		for (int i = 0; i < b.Length; i++)
		{
			if (b[i] != RW4_Magic[i])
			{
				throw new ModelFormatException(r, "Not a RW4 file", b);
			}
		}
		uint file_type_code = r.ReadU32();
		switch (file_type_code)
		{
		case 1u:
			file_type = RW4Model.FileTypes.Model;
			break;
		case 67108864u:
			file_type = RW4Model.FileTypes.Texture;
			break;
		default:
			throw new ModelFormatException(r, "Unknown file type", file_type_code);
		}
		uint ft_const1 = ((file_type == RW4Model.FileTypes.Model) ? 16u : 4u);
		uint section_count = r.ReadU32();
		r.expect(section_count, "H001 Section count not repeated");
		r.expect(ft_const1, "H002");
		r.expect(0u, "H003");
		section_index_begin = r.ReadU32();
		uint first_header_section_begin = r.ReadU32();
		uint[] values = new uint[3];
		r.expect(values, "H010");
		uint section_index_end1 = r.ReadU32();
		r.expect(ft_const1, "H011");
		uint file_size = r.ReadU32() + section_index_end1;
		if (r.Length != file_size)
		{
			throw new ModelFormatException(r, "H012", file_size);
		}
		r.expect(new uint[5] { 4u, 0u, 1u, 0u, 1u }, "H020");
		unknown_bits_030 = r.ReadU32();
		r.expect(new uint[5] { 4u, 0u, 1u, 0u, 1u }, "H040");
		r.expect(new uint[7] { 0u, 1u, 0u, 0u, 0u, 0u, 0u }, "H041");
		if (r.Position != 152)
		{
			throw new ModelFormatException(r, "H099", r.Position);
		}
		r.Seek(first_header_section_begin, SeekOrigin.Begin);
		r.expect(65540u, "H140");
		uint[] offsets = new uint[6];
		for (int j = 0; j < offsets.Length; j++)
		{
			offsets[j] = r.ReadU32() + first_header_section_begin;
		}
		if (offsets[2] != r.Position)
		{
			throw new ModelFormatException(r, "H145", r.Position);
		}
		r.expect(65541u, "H150");
		uint count = r.ReadU32();
		r.expect(12u, "H151");
		uint[] section_types = new uint[count];
		for (int k = 0; k < count; k++)
		{
			section_types[k] = r.ReadU32();
		}
		if (offsets[3] != r.Position)
		{
			throw new ModelFormatException(r, "H146", r.Position);
		}
		r.expect(65542u, "H160");
		uint[] array = new uint[8] { 3u, 24u, 0u, 4289724416u, 0u, 0u, 0u, 0u };
		array[2] = file_type_code;
		array[4] = file_type_code;
		r.expect(array, "H161");
		if (offsets[4] != r.Position)
		{
			throw new ModelFormatException(r, "H147", r.Position);
		}
		r.expect(65543u, "H170");
		uint fixup_count = r.ReadU32();
		r.expect(0u, "H171");
		r.expect(0u, "H172");
		r.expect(section_index_begin + section_count * 24 + fixup_count * 8, "H173");
		r.expect(section_index_begin + section_count * 24, "H174");
		r.expect(fixup_count, "H176");
		if (offsets[5] != r.Position)
		{
			throw new ModelFormatException(r, "H148", r.Position);
		}
		r.expect(65544u, "H180");
		uint[] values2 = new uint[2];
		r.expect(values2, "H181");
		r.Seek(section_index_begin, SeekOrigin.Begin);
		sections = new RW4Section[section_count];
		for (uint i2 = 0u; i2 < section_count; i2++)
		{
			sections[i2] = new RW4Section();
			sections[i2].LoadHeader(r, i2, section_index_end1);
		}
		for (int l = 0; l < fixup_count; l++)
		{
			uint sind = r.ReadU32();
			uint offset = r.ReadU32();
			sections[sind].fixup_offsets.Add(offset);
		}
		section_index_padding = section_index_end1 - (uint)(int)r.Position;
		r.ReadPadding(section_index_padding);
		bool[] used = new bool[section_types.Length];
		RW4Section[] array2 = sections;
		foreach (RW4Section s in array2)
		{
			used[s.type_code_indirect] = true;
			if (section_types[s.type_code_indirect] != s.type_code)
			{
				throw new ModelFormatException(r, "H300", s.type_code_indirect);
			}
		}
		for (int n = 0; n < fixed_section_types.Length; n++)
		{
			if (section_types[n] != fixed_section_types[n])
			{
				throw new ModelFormatException(r, "H301", n);
			}
		}
		for (int num = fixed_section_types.Length; num < section_types.Length; num++)
		{
			if (!used[num])
			{
				throw new ModelFormatException(r, "H302", section_types[num]);
			}
		}
	}

	private uint[] getSectionTypes()
	{
		Dictionary<uint, uint> stype = new Dictionary<uint, uint>();
		for (uint i = 0u; i < fixed_section_types.Length; i++)
		{
			stype[fixed_section_types[i]] = i;
		}
		for (int j = 0; j < sections.Length; j++)
		{
			if (!stype.TryGetValue(sections[j].type_code, out sections[j].type_code_indirect))
			{
				stype.Add(sections[j].type_code, sections[j].type_code_indirect = (uint)stype.Count);
			}
		}
		return (from keyValuePair in stype
			orderby keyValuePair.Value
			select keyValuePair.Key).ToArray();
	}

	public void Write(Stream w)
	{
		uint[] section_types = getSectionTypes();
		uint file_type_code = ((file_type == RW4Model.FileTypes.Model) ? 1u : 67108864u);
		w.Write(RW4_Magic, 0, RW4_Magic.Length);
		w.WriteU32(file_type_code);
		w.WriteU32((uint)sections.Length);
		w.WriteU32((uint)sections.Length);
		uint ft_const1 = ((file_type == RW4Model.FileTypes.Model) ? 16u : 4u);
		w.WriteU32(ft_const1);
		w.WriteU32(0u);
		w.WriteU32(section_index_begin);
		w.WriteU32(152u);
		uint[] values = new uint[3];
		w.WriteU32s(values);
		w.WriteU32(section_index_end);
		w.WriteU32(ft_const1);
		uint file_end = section_index_end;
		RW4Section[] array = sections;
		foreach (RW4Section s in array)
		{
			if (s.pos + s.size > file_end)
			{
				file_end = s.pos + s.size;
			}
		}
		w.WriteU32(file_end - section_index_end);
		w.WriteU32s(new uint[5] { 4u, 0u, 1u, 0u, 1u });
		w.WriteU32(unknown_bits_030);
		w.WriteU32s(new uint[5] { 4u, 0u, 1u, 0u, 1u });
		w.WriteU32s(new uint[7] { 0u, 1u, 0u, 0u, 0u, 0u, 0u });
		if (w.Position != 152)
		{
			throw new ModelFormatException(w, "WH099", w.Position);
		}
		w.WriteU32(65540u);
		uint[] array2 = new uint[6] { 4u, 8u, 16u, 0u, 36u, 28u };
		array2[3] = (uint)(12 + 4 * section_types.Length);
		uint[] hs_sizes = array2;
		for (int j = 1; j < hs_sizes.Length; j++)
		{
			hs_sizes[j] += hs_sizes[j - 1];
		}
		w.WriteU32s(hs_sizes);
		w.WriteU32(65541u);
		w.WriteU32((uint)section_types.Length);
		w.WriteU32(12u);
		w.WriteU32s(section_types);
		w.WriteU32(65542u);
		uint[] array3 = new uint[8] { 3u, 24u, 0u, 4289724416u, 0u, 0u, 0u, 0u };
		array3[2] = file_type_code;
		array3[4] = file_type_code;
		w.WriteU32s(array3);
		w.WriteU32(65543u);
		int fixup_count = 0;
		RW4Section[] array4 = sections;
		foreach (RW4Section s2 in array4)
		{
			fixup_count += s2.fixup_offsets.Count;
		}
		w.WriteU32((uint)fixup_count);
		w.WriteU32(0u);
		w.WriteU32(0u);
		w.WriteU32((uint)(section_index_begin + sections.Length * 24 + fixup_count * 8));
		w.WriteU32((uint)(section_index_begin + sections.Length * 24));
		w.WriteU32((uint)fixup_count);
		w.WriteU32(65544u);
		uint[] values2 = new uint[2];
		w.WriteU32s(values2);
	}

	public void WriteIndex(Stream w)
	{
		if (w.Position != section_index_begin)
		{
			throw new ModelFormatException(w, "WH200", null);
		}
		RW4Section[] array = sections;
		foreach (RW4Section s in array)
		{
			s.WriteHeader(w, section_index_end);
		}
		RW4Section[] array2 = sections;
		foreach (RW4Section s2 in array2)
		{
			foreach (uint f in s2.fixup_offsets)
			{
				w.WriteU32(s2.number);
				w.WriteU32(f);
			}
		}
		w.WritePadding(section_index_end - w.Position);
		if (w.Position != section_index_end)
		{
			throw new ModelFormatException(w, "WH299", w.Position);
		}
	}

	public uint GetHeaderEnd()
	{
		return (uint)(256 + 4 * getSectionTypes().Length + 12);
	}
}
