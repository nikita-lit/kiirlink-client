using System.Globalization;
using System.Resources;

namespace KiirLink.Resources.Strings;

public static class AppResources
{
    private static readonly ResourceManager ResourceManager =
        new("KiirLink.Resources.Strings.AppResources", typeof(AppResources).Assembly);

    public static string Get(string key, CultureInfo? culture = null) =>
        ResourceManager.GetString(key, culture ?? CultureInfo.CurrentUICulture) ?? key;
}
