namespace SporeMaster.Core.Dbpf;

/// <summary>
/// Helpers for the converted-file suffixes used by the named unpack/pack layout (for example
/// <c>name.prop.xml</c> and <c>name.32bitImage.png</c>).
/// </summary>
public static class ConversionSuffix
{
    /// <summary>
    /// True when a path ends in <c>.png</c> as a *converted* bitmap image (that is, the part before
    /// the <c>.png</c> still carries another extension, e.g. <c>name.32bitImage.png</c>). When the
    /// part before <c>.png</c> has no further extension the file is a *native* png resource and the
    /// <c>.png</c> is its resource type, so it must not be stripped.
    /// </summary>
    public static bool IsConvertedPng(string relativePath)
    {
        if (!relativePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string withoutPng = relativePath[..^4];
        int lastSlash = withoutPng.LastIndexOfAny(new[] { '\\', '/' });
        return withoutPng.LastIndexOf('.') > lastSlash;
    }
}
