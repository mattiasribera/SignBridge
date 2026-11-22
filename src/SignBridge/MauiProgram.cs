using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SignBridge.Services;
using SignBridge.ViewModels;
using SignBridge.Views;

namespace SignBridge;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseMauiCommunityToolkitCamera()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("AtkinsonHyperlegible-Regular.ttf", "AppRegular");
				fonts.AddFont("AtkinsonHyperlegible-Bold.ttf", "AppBold");
				fonts.AddFont("MaterialSymbols.ttf", "Icons");
			});

		builder.Services.AddSingleton<RecognizerProvider>();
		builder.Services.AddSingleton<SpeechService>();

		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<PracticePage>();
		builder.Services.AddTransient<PracticeViewModel>();
		builder.Services.AddTransient<TranslatePage>();
		builder.Services.AddTransient<TranslateViewModel>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
