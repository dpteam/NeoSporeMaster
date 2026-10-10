namespace SporeMaster.Core.Presentation;

/// <summary>
/// Picks a small icon (an emoji glyph) for a resource, the way Windows Explorer shows a different
/// icon per file type. Kept in Core and UI-free so it can be unit-tested; the GUI simply binds to it.
/// </summary>
public static class ResourceGlyph
{
    /// <summary>A glyph for a resource, based on its resolved path/extension and its type id.</summary>
    public static string For(string path, uint typeId)
    {
        string extension = ExtensionOf(path);

        if (extension.Length > 0)
        {
            switch (extension)
            {
                case ".prop":
                case ".prop_t":
                    return "\U0001F4C4"; // 📄 page
                case ".trigger":
                    return "\U0001F4C4"; // 📄 plain-text trigger
                case ".rw4":
                    return "\U0001F9CA"; // 🧊 3D model
                case ".animation":
                case ".anim":
                    return "\U0001F39E"; // 🎞 animation
                case ".raster":
                case ".png":
                case ".bitImage":
                case ".8bitImage":
                case ".16bitImage":
                case ".32bitImage":
                case ".48bitImage":
                    return "\U0001F5BC"; // 🖼 picture
                case ".locale":
                    return "\U0001F310"; // 🌐 locale/language
                case ".cnv":
                case ".gait":
                case ".tlsa":
                case ".pctp":
                case ".loottable":
                case ".look_table":
                case ".xml":
                case ".crt":
                    return "\U0001F4D1"; // 📑 structured data
                case ".txt":
                case ".arth":
                case ".tim":
                    return "\U0001F4C4"; // 📄 text
                case ".css":
                case ".html":
                case ".js":
                    return "\U0001F310"; // 🌐 web/UI content
                case ".ttf":
                    return "\U0001F524"; // 🔤 font
                case ".avi":
                case ".vp6":
                    return "\U0001F3AC"; // 🎬 video
                case ".snr":
                case ".sns":
                    return "\U0001F50A"; // 🔊 audio
            }
        }

        // Fall back to the type id when the extension is unknown.
        if (Dbpf.ResourceTypeIds.IsProperty(typeId))
        {
            return "\U0001F4C4";
        }

        if (Dbpf.ResourceTypeIds.IsTexture(typeId))
        {
            return "\U0001F5BC";
        }

        if (Dbpf.ResourceTypeIds.IsBitmap(typeId))
        {
            return "\U0001F5BC";
        }

        if (Dbpf.ResourceTypeIds.IsRenderWare4(typeId))
        {
            return "\U0001F9CA";
        }

        if (Dbpf.ResourceTypeIds.IsAnimation(typeId))
        {
            return "\U0001F39E";
        }

        if (Dbpf.ResourceTypeIds.IsTrigger(typeId))
        {
            return "\U0001F4C4";
        }

        if (typeId == Dbpf.ResourceTypeIds.Locale)
        {
            return "\U0001F310";
        }

        return "\U0001F4E6"; // 📦 generic resource
    }

    private static string ExtensionOf(string path)
    {
        int slash = path.LastIndexOf('/');
        string fileName = slash >= 0 ? path[(slash + 1)..] : path;
        int dot = fileName.LastIndexOf('.');
        return dot > 0 ? fileName[dot..] : string.Empty;
    }
}
