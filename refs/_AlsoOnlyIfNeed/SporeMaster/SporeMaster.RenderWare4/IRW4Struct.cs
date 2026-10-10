using System.IO;

namespace SporeMaster.RenderWare4;

internal interface IRW4Struct
{
	void Read(Stream r);

	void Write(Stream w);

	uint Size();
}
