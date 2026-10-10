using System.Globalization;
using SporeMaster.Core.Dbpf;

namespace SporeMaster.Core.Dbpf;

/// <summary>
/// The layout used by <c>unpack --raw</c>: resources are stored by their numeric key alone, with no
/// dependence on the name registry. The path is
/// <c>_raw/{type:X8}/{group:X8}/{instance:X8}.bin</c>, which <c>pack</c> parses back into an exact
/// key. This makes a raw unpack → pack round-trip lossless and independent of naming.
/// </summary>
public static class RawKeyPath
{
    /// <summary>The marker directory that identifies raw-key files.</summary>
    public const string Root = "_raw";

    /// <summary>Builds the raw relative path for a key (uses forward slashes).</summary>
    public static string Format(ResourceKey key) =>
        $"{Root}/{key.TypeId:X8}/{key.GroupId:X8}/{key.InstanceId:X8}.bin";

    /// <summary>
    /// Parses a raw relative path back into a resource key. Returns false when the path is not a raw
    /// key path (so callers can fall back to the normal <c>group/name.ext</c> resolution).
    /// </summary>
    public static bool TryParse(string relative, out ResourceKey key)
    {
        key = default;
        string normalized = relative.Replace('\\', '/').TrimStart('/');
        string[] parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || !parts[0].Equals(Root, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!parts[3].EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string instance = parts[3][..^4];
        if (TryHex(parts[1], out uint type) && TryHex(parts[2], out uint group) && TryHex(instance, out uint instanceId))
        {
            key = new ResourceKey(type, group, instanceId);
            return true;
        }

        return false;
    }

    private static bool TryHex(string token, out uint value) =>
        uint.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
}
