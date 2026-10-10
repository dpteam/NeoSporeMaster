namespace SporeMaster.RenderWare4;

internal class TextureBlob : RW4Blob
{
	public const uint type_code = 65584u;

	protected override bool Relocatable()
	{
		return true;
	}
}
