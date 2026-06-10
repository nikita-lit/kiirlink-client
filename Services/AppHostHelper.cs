using KiirLink.Configuration;

namespace KiirLink.Services;

public static class AppHostHelper
{
    private static ApiOptions? _options;

    public static void SetOptions( ApiOptions options )
    {
        _options = options;
    }

    public static string BaseUrl => _options?.BaseUrl ?? string.Empty;
}
