namespace SignBridge.Views;

public partial class HomePage : ContentPage
{
	public HomePage()
	{
		InitializeComponent();
	}

	private async void OnPracticeTapped(object? sender, TappedEventArgs e) =>
		await Shell.Current.GoToAsync(AppShell.PracticeRoute);

	private async void OnTranslateTapped(object? sender, TappedEventArgs e) =>
		await Shell.Current.GoToAsync(AppShell.TranslateRoute);
}
