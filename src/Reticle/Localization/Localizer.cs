using System.Globalization;
using System.Windows;
using Reticle.Core;

namespace Reticle.Localization;

/// <summary>
/// Swaps the string dictionary (<c>Localization/*.xaml</c>) merged into the application resources. XAML reads
/// strings with <c>DynamicResource</c>; code reads them with <see cref="Get"/> and refreshes on <see cref="Changed"/>.
/// </summary>
internal static class Localizer
{
    private const string Folder = "Localization/";

    public static CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("en-GB");

    public static event EventHandler? Changed;

    public static void Apply(AppLanguage language)
    {
        (string file, string culture) = language switch
        {
            AppLanguage.PortuguesePortugal => ("pt-PT.xaml", "pt-PT"),
            _ => ("en.xaml", "en-GB"),
        };
        Culture = CultureInfo.GetCultureInfo(culture);
        ResourceSwitcher.Replace(Folder, new Uri(Folder + file, UriKind.Relative));
        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static string Get(string key) =>
        Application.Current?.TryFindResource(key) as string ?? key;

    public static string Format(string key, params object[] args) =>
        string.Format(Culture, Get(key), args);
}

/// <summary>
/// Replaces one merged dictionary of the application resources, identified by its source folder. BAML may store
/// the source as a lower-cased pack URI, hence the case-insensitive substring match.
/// </summary>
internal static class ResourceSwitcher
{
    public static void Replace(string folder, Uri source)
    {
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var replacement = new ResourceDictionary { Source = source };
        for (int i = 0; i < dictionaries.Count; i++)
        {
            if (dictionaries[i].Source?.OriginalString.Contains(folder, StringComparison.OrdinalIgnoreCase) == true)
            {
                dictionaries[i] = replacement;
                return;
            }
        }
        dictionaries.Add(replacement);
    }
}
