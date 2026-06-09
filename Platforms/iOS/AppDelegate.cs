using Foundation;
using UIKit;

namespace KiirLink;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
	{
		var route = url.Host?.ToLowerInvariant() switch
		{
			"home" => "Home",
			"links" => "Links",
			"favourites" => "Favourites",
			"profile" => "Profile",
			"analytics" => "Analytics",
			_ => null
		};

		if (route is null)
		{
			return false;
		}

		MainThread.BeginInvokeOnMainThread(async () => await Shell.Current.GoToAsync($"//{route}"));
		return true;
	}
}
