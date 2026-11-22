using CommunityToolkit.Maui.Views;
using SignBridge.Services;
using SignBridge.ViewModels;

namespace SignBridge.Views;

public partial class PracticePage : CameraPage
{
	public PracticePage(RecognizerProvider recognizers, PracticeViewModel viewModel)
		: base(recognizers, viewModel)
	{
		InitializeComponent();
	}

	protected override CameraView Camera => CameraPreview;

	protected override GraphicsView SkeletonView => Skeleton;
}
