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
}
