using Reticle.Core;
using Reticle.Localization;

namespace Reticle.Themes;

/// <summary>Swaps the palette (<c>Themes/Palettes/*.xaml</c>); brushes are referenced with <c>DynamicResource</c>.</summary>
internal static class ThemeManager
{
    private const string Folder = "Themes/Palettes/";

    public static void Apply(AppTheme theme) =>
        ResourceSwitcher.Replace(Folder, new Uri($"{Folder}{theme}.xaml", UriKind.Relative));
}
