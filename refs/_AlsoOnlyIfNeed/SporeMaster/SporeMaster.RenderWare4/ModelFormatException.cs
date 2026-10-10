using System;
using System.IO;

namespace SporeMaster.RenderWare4;

public class ModelFormatException : FormatException
{
	public string exception_type;

	public ModelFormatException(Stream r, string description, object argument)
		: base(string.Format("{0} {2} @0x{1:x}", description, r?.Position ?? 0, argument))
	{
		exception_type = description;
	}
}
