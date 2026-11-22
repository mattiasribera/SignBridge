using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using SignBridge.Services;
using SignBridge.ViewModels;

namespace SignBridge.Views;

/// <summary>
/// Base page for screens with a live camera: asks for permission, picks a camera, runs the
/// recognition loop while the page is visible and keeps the hand skeleton view up to date.
/// </summary>
public abstract class CameraPage : ContentPage
{
	private readonly RecognizerProvider _recognizers;
	private readonly RecognitionLoop _loop = new();
	private readonly HandSkeletonDrawable _skeleton = new();
	private IReadOnlyList<CameraInfo> _cameras = [];

	protected CameraPage(RecognizerProvider recognizers, RecognitionViewModel viewModel)
	{
		_recognizers = recognizers;
		ViewModel = viewModel;
		BindingContext = viewModel;
	}

	protected RecognitionViewModel ViewModel { get; }

	protected abstract CameraView Camera { get; }

	protected abstract GraphicsView SkeletonView { get; }

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		SkeletonView.Drawable = _skeleton;
		ViewModel.PropertyChanged += OnViewModelPropertyChanged;

		try
		{
			ViewModel.Status = "Loading models…";
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
			ViewModel.Status = "Show your hand to the camera";
			_loop.Start(Camera, recognizer, ViewModel);
		}
		catch (Exception ex)
		{
			ViewModel.Status = $"Could not start: {ex.Message}";
		}
	}

	protected override void OnDisappearing()
	{
		ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
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

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(RecognitionViewModel.Landmarks))
		{
			_skeleton.Landmarks = ViewModel.Landmarks;
			SkeletonView.Invalidate();
		}
	}

	private async Task SelectCameraAsync(CameraInfo camera)
	{
		Camera.SelectedCamera = camera;

		// Smallest resolution that still has detail: the hand detector works on 192x192.
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
}
