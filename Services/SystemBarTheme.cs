namespace KiirLink.Services;

public static class SystemBarTheme
{
    public static void Apply(bool dark, Color background)
    {
#if ANDROID
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        var window = activity?.Window;
        if (window is null)
            return;

        activity!.RunOnUiThread(() =>
        {
            var platformColor = Android.Graphics.Color.Argb(
                (int)(background.Alpha * 255),
                (int)(background.Red * 255),
                (int)(background.Green * 255),
                (int)(background.Blue * 255));
            window.SetStatusBarColor(platformColor);
            window.SetNavigationBarColor(platformColor);

            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var controller = window.InsetsController;
                if (controller is null)
                    return;

                const Android.Views.WindowInsetsControllerAppearance modernLightBars =
                    Android.Views.WindowInsetsControllerAppearance.LightStatusBars |
                    Android.Views.WindowInsetsControllerAppearance.LightNavigationBars;

                controller.SetSystemBarsAppearance(
                    dark ? 0 : (int)modernLightBars,
                    (int)modernLightBars);
                return;
            }

            var visibility = (int)window.DecorView.SystemUiVisibility;
            const int legacyLightBars =
                (int)Android.Views.SystemUiFlags.LightStatusBar |
                (int)Android.Views.SystemUiFlags.LightNavigationBar;

            window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)(dark
                ? visibility & ~legacyLightBars
                : visibility | legacyLightBars);
        });
#endif
    }
}
