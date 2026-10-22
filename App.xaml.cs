using Microsoft.Extensions.DependencyInjection;

namespace Emergency_cameroun;

public partial class App : Application
{
	private readonly IServiceProvider _services;

	public App(IServiceProvider services)
	{
		_services = services;
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var appShell = _services.GetRequiredService<AppShell>();
		var window = new Window(appShell);
#if WINDOWS
		window.Width = GetWindowDimension("PhoneWindowWidth", 433);
		window.Height = GetWindowDimension("PhoneWindowHeight", 892);
		window.MinimumWidth = 360;
		window.MinimumHeight = 640;
#endif
		return window;
	}

	private static double GetWindowDimension(string key, double fallback)
	{
		return Current?.Resources.TryGetValue(key, out var value) == true && value is double dimension
			? dimension
			: fallback;
	}
}