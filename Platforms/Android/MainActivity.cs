using Android.App;
using Android.Content.PM;
using Android.OS;
using KiirLink.Services;

namespace KiirLink;

[Activity( Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density )]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnResume()
    {
        base.OnResume();

        var background = (Color)Microsoft.Maui.Controls.Application.Current!.Resources["AppBackground"];
        SystemBarTheme.Apply(ThemeService.IsDark, background);
    }
}
