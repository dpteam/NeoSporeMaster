using System.Windows.Markup;
using SporeMaster.Core.Localization;

namespace SporeMaster.Gui;

/// <summary>
/// A XAML markup extension that resolves a localized string from <see cref="Localizer.Default"/> at
/// load time, e.g. <c>Header="{loc:Loc gui.menu.file}"</c>. Recreate the window to change language.
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class LocExtension : MarkupExtension
{
    public LocExtension()
    {
    }

    public LocExtension(string key)
    {
        Key = key;
    }

    /// <summary>The localization key (see <c>lang/*.lang</c>).</summary>
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider) => Localizer.Default[Key];
}
