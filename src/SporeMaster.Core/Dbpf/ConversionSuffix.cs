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
    public static bool IsConvertedPng(string relativePath) => IsConvertedSuffix(relativePath, ".png");

    /// <summary>
    /// True when a path ends in <c>.xml</c> as a *converted* property file (e.g. <c>name.prop.xml</c>).
    /// When the part before <c>.xml</c> has no further extension the file is a *native* xml resource
    /// (e.g. <c>SporeUIFonts.xml</c>) and the <c>.xml</c> is its resource type.
    /// </summary>
    public static bool IsConvertedPropXml(string relativePath) => IsConvertedSuffix(relativePath, ".xml");

    /// <summary>
    /// True when <paramref name="relativePath"/> ends in <paramref name="suffix"/> and the part before
    /// it still carries another extension (a conversion added the suffix to <c>name.ext</c>).
    /// </summary>
    public static bool IsConvertedSuffix(string relativePath, string suffix)
    {
        if (!relativePath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string withoutSuffix = relativePath[..^suffix.Length];
        int lastSlash = withoutSuffix.LastIndexOfAny(new[] { '\\', '/' });
        return withoutSuffix.LastIndexOf('.') > lastSlash;
    }
}
