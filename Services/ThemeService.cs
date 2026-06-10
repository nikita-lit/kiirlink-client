namespace KiirLink.Services;

public static class ThemeService
{
    private const string ThemePreferenceKey = "theme_is_dark";
    private static bool _initialized;

    public static event EventHandler? ThemeChanged;

    public static bool HasStoredPreference => Preferences.Default.ContainsKey(ThemePreferenceKey);

    public static bool IsDark => HasStoredPreference
        ? Preferences.Default.Get(ThemePreferenceKey, false)
        : false;

    public static void Initialize()
    {
        var app = Application.Current;
        if (app is null || _initialized)
            return;

        if (HasStoredPreference)
        {
            ApplyTheme(Preferences.Default.Get(ThemePreferenceKey, false));
            app.UserAppTheme = Preferences.Default.Get(ThemePreferenceKey, false) ? AppTheme.Dark : AppTheme.Light;
        }
        else
        {
            // Default to a light theme on first launch so the app does not inherit
            // a system dark mode background unless the user explicitly enables it.
            ApplyTheme(false);
            app.UserAppTheme = AppTheme.Light;
        }

        _initialized = true;
    }

    public static void SetDark(bool isDark)
    {
        Preferences.Default.Set(ThemePreferenceKey, isDark);

        var app = Application.Current;
        if (app is not null)
            app.UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;

        ApplyTheme(isDark);
    }

    private static void ApplyTheme(bool dark)
    {
        var resources = Application.Current?.Resources;
        if (resources is null)
            return;

        var background = Color.FromArgb(dark ? "#121212" : "#FFFFFF");
        var surface = Color.FromArgb(dark ? "#1C1C1C" : "#FFFFFF");
        var text = Color.FromArgb(dark ? "#F2F2F2" : "#343434");
        var mutedText = Color.FromArgb(dark ? "#BDBDBD" : "#666666");
        var stroke = Color.FromArgb(dark ? "#3A3A3A" : "#E6E6E6");
        var divider = Color.FromArgb(dark ? "#2B2B2B" : "#ECECEC");
        var track = Color.FromArgb(dark ? "#242424" : "#F1F1F1");

        resources["AppBackground"] = background;
        resources["AppSurface"] = surface;
        resources["AppText"] = text;
        resources["AppMutedText"] = mutedText;
        resources["AppStroke"] = stroke;
        resources["AppDivider"] = divider;
        resources["AppTrack"] = track;
        resources["AppShadow"] = Color.FromArgb(dark ? "#66000000" : "#22000000");
        resources["AppAccentSurface"] = Color.FromArgb(dark ? "#2A1A14" : "#FFF0EC");
        resources["AppAccentText"] = Color.FromArgb(dark ? "#FFB299" : "#C83B1D");
        resources["AppAccentStroke"] = Color.FromArgb(dark ? "#FF8A66" : "#D94322");
        resources["AppDangerSurface"] = Color.FromArgb(dark ? "#331313" : "#FFE7E7");
        resources["AppDangerText"] = Color.FromArgb(dark ? "#FFB0B0" : "#A52D2D");
        resources["AppDangerStroke"] = Color.FromArgb(dark ? "#CC6A6A" : "#A52D2D");
        resources["AppCategorySurface"] = Color.FromArgb(dark ? "#18351B" : "#DDF2DC");
        resources["AppCategoryText"] = Color.FromArgb(dark ? "#A7E4AA" : "#245E2A");
        resources["AppGoldSurface"] = Color.FromArgb(dark ? "#E2A900" : "#785600");
        resources["AppGoldText"] = Color.FromArgb(dark ? "#181200" : "#FFFFFF");

        SystemBarTheme.Apply(dark, background);
        ThemeChanged?.Invoke(null, EventArgs.Empty);

        var app = Application.Current;
        if (app is null)
            return;

        foreach (var window in app.Windows)
        {
            if (window.Page is not null)
                window.Page.BackgroundColor = background;
        }

        if (Shell.Current is not null)
        {
            Shell.Current.BackgroundColor = background;

            if (Shell.Current.CurrentPage is not null)
                Shell.Current.CurrentPage.BackgroundColor = background;
        }
    }
}
