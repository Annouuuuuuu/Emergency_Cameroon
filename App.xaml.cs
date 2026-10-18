namespace Emergency_cameroun;

public partial class App : Application
{
	private readonly AppShell _appShell;

	public App(AppShell appShell)
	{
		_appShell = appShell;
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(_appShell);
#if WINDOWS
		window.Width = 1180;
		window.Height = 900;
		window.MinimumWidth = 360;
		window.MinimumHeight = 640;
#endif
		return window;
	}
}