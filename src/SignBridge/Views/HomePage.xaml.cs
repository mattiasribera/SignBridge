namespace SignBridge.Views;

public partial class HomePage : ContentPage
{
	// Real landmarks of a hand signing "Y" (from the test fixtures), used as the app's emblem.
	private static readonly float[] EmblemHand =
	[
		224, 388, 0, 256, 347, -1, 273, 294, -5, 285, 257, -10, 307, 235, -14,
		221, 272, 3, 223, 243, -2, 233, 275, -3, 238, 296, -4,
		194, 283, -2, 195, 256, -11, 212, 292, -16, 220, 311, -18,
		170, 299, -7, 172, 274, -14, 192, 306, -16, 203, 326, -16,
		147, 315, -14, 127, 278, -18, 119, 256, -20, 111, 230, -22,
	];

	public HomePage()
	{
		InitializeComponent();
		Hero.Drawable = new HandSkeletonDrawable { Landmarks = EmblemHand };
	}

	private async void OnPracticeTapped(object? sender, TappedEventArgs e) => await OpenPracticeAsync();

	private async void OnPracticeClicked(object? sender, EventArgs e) => await OpenPracticeAsync();

	private async void OnTranslateTapped(object? sender, TappedEventArgs e) => await OpenTranslateAsync();

	private async void OnTranslateClicked(object? sender, EventArgs e) => await OpenTranslateAsync();

	private static Task OpenPracticeAsync() => Shell.Current.GoToAsync(AppShell.PracticeRoute);

	private static Task OpenTranslateAsync() => Shell.Current.GoToAsync(AppShell.TranslateRoute);
}
