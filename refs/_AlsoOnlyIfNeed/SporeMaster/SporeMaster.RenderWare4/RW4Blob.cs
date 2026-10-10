using System.IO;

namespace SporeMaster.RenderWare4;

internal class RW4Blob : RW4Object
{
	public byte[] blob;

	public long required_position = -1L;

	protected virtual bool Relocatable()
	{
		return false;
	}

	public override void Read(RW4Model m, RW4Section s, Stream r)
	{
		if (!Relocatable())
		{
			required_position = r.Position;
		}
		blob = r.ReadBytes((int)s.size);
	}

	public override void Write(RW4Model m, RW4Section s, Stream w)
	{
		if (required_position >= 0 && w.Position != required_position)
		{
			throw new ModelFormatException(w, "Unable to move unknown section", null);
		}
		w.Write(blob, 0, blob.Length);
	}

	public override int ComputeSize()
	{
		return blob.Length;
	}
}
