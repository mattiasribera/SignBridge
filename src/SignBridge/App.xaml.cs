using Microsoft.Extensions.DependencyInjection;

namespace SignBridge;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		// The 60-30-10 palette is designed for one light theme; keep it on dark-mode devices too.
		UserAppTheme = AppTheme.Light;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}