using SignBridge.Views;

namespace SignBridge;

public partial class AppShell : Shell
{
	public const string PracticeRoute = "practice";
	public const string TranslateRoute = "translate";

	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(PracticeRoute, typeof(PracticePage));
		Routing.RegisterRoute(TranslateRoute, typeof(TranslatePage));
	}
}
