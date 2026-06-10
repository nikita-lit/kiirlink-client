using System.Globalization;

namespace KiirLink.Services;

public static class UriQueryBuilder
{
    public static string Build(string path, params (string Key, object? Value)[] parameters)
    {
        var query = parameters
            .Where(parameter => parameter.Value is not null)
            .Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(Format(parameter.Value!))}");

        var queryString = string.Join("&", query);
        return string.IsNullOrEmpty(queryString) ? path : $"{path}?{queryString}";
    }

    private static string Format(object value) => value switch
    {
        bool boolean => boolean ? "true" : "false",
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };
}
