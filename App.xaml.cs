using Microsoft.Extensions.DependencyInjection;
using KiirLink.Services;

namespace KiirLink;

public partial class App : Application
{
    public App()
    {
        LocalizationManager.Instance.Initialize();
        InitializeComponent();
        ThemeService.Initialize();
    }

    protected override Window CreateWindow( IActivationState? activationState )
    {
        var shell = IPlatformApplication.Current!.Services.GetRequiredService<AppShell>();
        return new Window( shell );
    }
}
