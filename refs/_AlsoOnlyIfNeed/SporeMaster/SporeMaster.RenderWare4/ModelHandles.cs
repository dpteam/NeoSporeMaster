namespace SporeMaster.RenderWare4;

internal class ModelHandles : RW4Blob
{
	public const int type_code = 16711680;

	protected override bool Relocatable()
	{
		return true;
	}
}
