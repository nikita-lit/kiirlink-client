using Microsoft.Extensions.DependencyInjection;

namespace KiirLink;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var shell = IPlatformApplication.Current!.Services.GetRequiredService<AppShell>();
		return new Window(shell);
	}
}