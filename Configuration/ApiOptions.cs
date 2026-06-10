namespace KiirLink.Configuration;

public sealed class ApiOptions
{
    public const string SectionName = "Api";

    public string BaseUrl { get; set; } = "http://localhost:5129";
    public int TimeoutSeconds { get; set; } = 20;
    public int RetryCount { get; set; } = 3;
}
