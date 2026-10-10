namespace SporeMaster.RenderWare4;

internal class VertexFormat : RW4Blob
{
	public const int type_code = 131076;

	protected override bool Relocatable()
	{
		return true;
	}
}
