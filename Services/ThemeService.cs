namespace KiirLink.Services;

public static class ThemeService
{
    private const string ThemePreferenceKey = "theme_is_dark";
    private static readonly IReadOnlyDictionary<string, (string Light, string Dark)> Colors =
        new Dictionary<string, (string, string)>
        {
            ["AppBackground"] = ("#FFFFFF", "#121212"),
            ["AppSurface"] = ("#FFFFFF", "#1C1C1C"),
            ["AppText"] = ("#343434", "#F2F2F2"),
            ["AppMutedText"] = ("#666666", "#BDBDBD"),
            ["AppStroke"] = ("#E6E6E6", "#3A3A3A"),
            ["AppDivider"] = ("#ECECEC", "#2B2B2B"),
            ["AppTrack"] = ("#F1F1F1", "#242424"),
            ["AppShadow"] = ("#22000000", "#66000000"),
            ["AppAccentSurface"] = ("#FFF0EC", "#2A1A14"),
            ["AppAccentText"] = ("#C83B1D", "#FFB299"),
            ["AppAccentStroke"] = ("#D94322", "#FF8A66"),
            ["AppDangerSurface"] = ("#FFE7E7", "#331313"),
            ["AppDangerText"] = ("#A52D2D", "#FFB0B0"),
            ["AppDangerStroke"] = ("#A52D2D", "#CC6A6A"),
            ["AppCategorySurface"] = ("#DDF2DC", "#18351B"),
            ["AppCategoryText"] = ("#245E2A", "#A7E4AA"),
            ["AppGoldSurface"] = ("#785600", "#E2A900"),
            ["AppGoldText"] = ("#FFFFFF", "#181200")
        };
    private static bool _initialized;

    public static event EventHandler? ThemeChanged;

    public static bool HasStoredPreference => Preferences.Default.ContainsKey(ThemePreferenceKey);

    public static bool IsDark => Preferences.Default.Get(ThemePreferenceKey, false);

    public static void Initialize()
    {
        var app = Application.Current;
        if (app is null || _initialized)
            return;

        var dark = HasStoredPreference && IsDark;
        ApplyTheme(dark);
        app.UserAppTheme = dark ? AppTheme.Dark : AppTheme.Light;

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

        foreach (var (key, colors) in Colors)
            resources[key] = Color.FromArgb(dark ? colors.Dark : colors.Light);

        var background = (Color)resources["AppBackground"];
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
