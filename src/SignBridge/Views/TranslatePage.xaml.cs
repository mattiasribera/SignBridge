using CommunityToolkit.Maui.Views;
using SignBridge.Services;
using SignBridge.ViewModels;

namespace SignBridge.Views;

public partial class TranslatePage : CameraPage
{
	public TranslatePage(RecognizerProvider recognizers, TranslateViewModel viewModel)
		: base(recognizers, viewModel)
	{
		InitializeComponent();
	}

	protected override CameraView Camera => CameraPreview;

	protected override GraphicsView SkeletonView => Skeleton;

	protected override GraphicsView OverlayView => Overlay;

	private TranslateViewModel Translator => (TranslateViewModel)ViewModel;

	/// <summary>Space adds a space, Backspace deletes a letter, Delete (Supr) clears the text.</summary>
	protected override bool HandleShortcut(string key)
	{
		var command = key switch
		{
			"Space" => Translator.SpaceCommand,
			"Back" => Translator.BackspaceCommand,
			"Delete" => Translator.ClearCommand,
			_ => null,
		};
		if (command is null)
		{
			return base.HandleShortcut(key);
		}

		command.Execute(null);
		return true;
	}
}
