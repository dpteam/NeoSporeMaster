using System.Globalization;

namespace SporeMaster.Core.Dbpf;

/// <summary>The 3-part identifier (type / group / instance) that names a resource in a package.</summary>
public readonly record struct ResourceKey(uint TypeId, uint GroupId, uint InstanceId)
{
    public override string ToString() =>
        $"0x{TypeId:X8}:0x{GroupId:X8}:0x{InstanceId:X8}";

    /// <summary>
    /// Parses a key written as <c>type:group:instance</c> where each part may be a
    /// <c>0x</c>-hex value, a <c>#</c>-prefixed hex value, or a plain decimal.
    /// </summary>
    public static ResourceKey Parse(string text)
    {
        if (!TryParse(text, out ResourceKey key))
        {
            throw new FormatException($"'{text}' is not a valid resource key (expected type:group:instance).");
        }

        return key;
    }

    public static bool TryParse(string? text, out ResourceKey key)
    {
        key = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string[] parts = text.Trim().Split(':');
        if (parts.Length != 3)
        {
            return false;
        }

        if (!TryParsePart(parts[0], out uint type) ||
            !TryParsePart(parts[1], out uint group) ||
            !TryParsePart(parts[2], out uint instance))
        {
            return false;
        }

        key = new ResourceKey(type, group, instance);
        return true;
    }

    private static bool TryParsePart(string part, out uint value)
    {
        part = part.Trim();
        if (part.StartsWith('#'))
        {
            return uint.TryParse(part.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        if (part.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(part.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        return uint.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
