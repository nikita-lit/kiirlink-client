using CommunityToolkit.Maui;
using KiirLink.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using KiirLink.Services;
using KiirLink.Pages;
using KiirLink.ViewModels;

namespace KiirLink;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        var environment = Environment.GetEnvironmentVariable("KIIRLINK_ENVIRONMENT")
#if DEBUG
                          ?? "Development";
#else
                          ?? "Production";
#endif

        AddEmbeddedConfiguration(builder.Configuration, "appsettings.json");
        AddEmbeddedConfiguration(builder.Configuration, $"appsettings.{environment}.json", optional: true);

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts( fonts =>
            {
                fonts.AddFont( "RadioCanadaBig-Regular.ttf", "RadioCanadaBigRegular" );
                fonts.AddFont( "RadioCanadaBig-Medium.ttf", "RadioCanadaBigMedium" );
                fonts.AddFont( "RadioCanadaBig-SemiBold.ttf", "RadioCanadaBigSemiBold" );
                fonts.AddFont( "RadioCanadaBig-Bold.ttf", "RadioCanadaBigBold" );
            } );

        builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.SectionName));

        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services
            .AddHttpClient<IApiClient, ApiClient>((services, client) =>
            {
                var options = services.GetRequiredService<IOptions<ApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddStandardResilienceHandler(options =>
            {
                var apiOptions = builder.Configuration
                    .GetSection(ApiOptions.SectionName)
                    .Get<ApiOptions>() ?? new ApiOptions();

                var retryCount = Math.Max(0, apiOptions.RetryCount);
                var attemptTimeout = TimeSpan.FromSeconds(Math.Max(1, apiOptions.TimeoutSeconds));

                options.Retry.MaxRetryAttempts = retryCount;
                options.AttemptTimeout.Timeout = attemptTimeout;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromTicks(attemptTimeout.Ticks * 2);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromTicks(
                    attemptTimeout.Ticks * (retryCount + 1) + TimeSpan.FromSeconds(5).Ticks);
            });

        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<AuthService>(services =>
            (AuthService)services.GetRequiredService<IAuthService>());
        builder.Services.AddSingleton<ILinkService, LinkService>();
        builder.Services.AddSingleton<LinkService>(services =>
            (LinkService)services.GetRequiredService<ILinkService>());

        builder.Services.AddTransient<SignInViewModel>();
        builder.Services.AddTransient<CreateAccountViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<LinksViewModel>();
        builder.Services.AddTransient<FavouritesViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();

        // Pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<SignInPage>();
        builder.Services.AddTransient<CreateAccountPage>();
        builder.Services.AddTransient<LinksPage>();
        builder.Services.AddTransient<FavouritesPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<AnalyticsPage>();
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void AddEmbeddedConfiguration(IConfigurationBuilder configuration, string fileName,
        bool optional = false)
    {
        var resourceName = $"{typeof(MauiProgram).Assembly.GetName().Name}.{fileName}";
        var stream = typeof(MauiProgram).Assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            if (optional)
                return;

            throw new InvalidOperationException($"Embedded configuration '{fileName}' was not found.");
        }

        configuration.AddJsonStream(stream);
    }
}
