using Microsoft.Extensions.Logging;

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

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
