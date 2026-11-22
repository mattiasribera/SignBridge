using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using SignBridge.Services;
using SignBridge.ViewModels;

namespace SignBridge.Views;

/// <summary>
/// Base page for screens with a live camera: asks for permission, picks a camera,
/// sizes the guide frame and runs the recognition loop while the page is visible.
/// </summary>
public abstract class CameraPage : ContentPage
{
	private readonly RecognizerProvider _recognizers;
	private readonly RecognitionLoop _loop = new();
	private IReadOnlyList<CameraInfo> _cameras = [];

	protected CameraPage(RecognizerProvider recognizers, RecognitionViewModel viewModel)
	{
		_recognizers = recognizers;
		ViewModel = viewModel;
		BindingContext = viewModel;
	}

	protected RecognitionViewModel ViewModel { get; }

	protected abstract CameraView Camera { get; }

	protected abstract View GuideFrame { get; }

	protected abstract View CameraArea { get; }

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		CameraArea.SizeChanged += OnCameraAreaSizeChanged;

		try
		{
			ViewModel.Status = "Loading model…";
			var recognizer = await _recognizers.GetAsync();

			if (await Permissions.RequestAsync<Permissions.Camera>() != PermissionStatus.Granted)
			{
				ViewModel.Status = "Camera permission is required.";
				return;
			}

			_cameras = await Camera.GetAvailableCameras(CancellationToken.None);
			var preferred = _cameras.FirstOrDefault(c => c.Position == CameraPosition.Front) ?? _cameras.FirstOrDefault();
			if (preferred is null)
			{
				ViewModel.Status = "No camera found.";
				return;
			}

			await SelectCameraAsync(preferred);
			ViewModel.Status = "Show a sign inside the frame";
			_loop.Start(Camera, recognizer, ViewModel);
		}
		catch (Exception ex)
		{
			ViewModel.Status = $"Could not start: {ex.Message}";
		}
	}

	protected override void OnDisappearing()
	{
		CameraArea.SizeChanged -= OnCameraAreaSizeChanged;
		_loop.Stop();
		try
		{
			Camera.StopCameraPreview();
		}
		catch (Exception)
		{
			// The preview may already be stopped.
		}

		base.OnDisappearing();
	}

	protected async void OnSwitchCameraClicked(object? sender, EventArgs e)
	{
		if (_cameras.Count < 2)
		{
			ViewModel.Status = "Only one camera available.";
			return;
		}

		var index = _cameras.ToList().FindIndex(c => c.DeviceId == Camera.SelectedCamera?.DeviceId);
		await SelectCameraAsync(_cameras[(index + 1) % _cameras.Count]);
	}

	private async Task SelectCameraAsync(CameraInfo camera)
	{
		Camera.SelectedCamera = camera;

		// Smallest resolution that still has detail: the model only needs 224x224.
		var resolution = camera.SupportedResolutions
			.Where(s => Math.Min(s.Width, s.Height) >= 480)
			.OrderBy(s => s.Width * s.Height)
			.FirstOrDefault();
		if (resolution != Size.Zero)
		{
			Camera.ImageCaptureResolution = resolution;
		}

		try
		{
			await Camera.StartCameraPreview(CancellationToken.None);
		}
		catch (Exception)
		{
			// Some platforms start the preview automatically once a camera is selected.
		}
	}

	private void OnCameraAreaSizeChanged(object? sender, EventArgs e)
	{
		var side = Math.Min(CameraArea.Width, CameraArea.Height) * RecognitionViewModel.GuideFraction;
		if (side > 0)
		{
			GuideFrame.WidthRequest = side;
			GuideFrame.HeightRequest = side;
		}
	}
}
