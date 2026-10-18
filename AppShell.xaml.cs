namespace Emergency_cameroun;

public partial class AppShell : Shell
{
	public AppShell(MainPage mainPage)
	{
		InitializeComponent();
		Items.Add(new ShellContent
		{
			Title = "Emergency Cameroon",
			Route = "MainPage",
			Content = mainPage
		});
	}
}
