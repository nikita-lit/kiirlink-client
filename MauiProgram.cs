using Microsoft.Extensions.Logging;
using KiirLink.Services;
using KiirLink.Pages;

namespace KiirLink;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("RadioCanadaBig-Regular.ttf", "RadioCanadaBigRegular");
				fonts.AddFont("RadioCanadaBig-Medium.ttf", "RadioCanadaBigMedium");
				fonts.AddFont("RadioCanadaBig-SemiBold.ttf", "RadioCanadaBigSemiBold");
				fonts.AddFont("RadioCanadaBig-Bold.ttf", "RadioCanadaBigBold");
			});

		// Services
		builder.Services.AddSingleton<ApiClient>();
		builder.Services.AddSingleton<AuthService>();
		builder.Services.AddSingleton<LinkService>();

		// Pages
		builder.Services.AddTransient<MainPage>();
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
}
