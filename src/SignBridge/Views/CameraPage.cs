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
	private readonly HandSkeletonDrawable _overlay = new() { IsOverlay = true };
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

	/// <summary>Transparent view stacked exactly on top of the camera preview.</summary>
	protected abstract GraphicsView OverlayView { get; }

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		SkeletonView.Drawable = _skeleton;
		OverlayView.Drawable = _overlay;
		AttachKeyboardShortcuts();
		ViewModel.PropertyChanged += OnViewModelPropertyChanged;

		try
		{
			ViewModel.Status = "Cargando los modelos…";
			var recognizer = await _recognizers.GetAsync();

			if (await Permissions.RequestAsync<Permissions.Camera>() != PermissionStatus.Granted)
			{
				ViewModel.Status = "Se necesita permiso para usar la cámara.";
				return;
			}

			_cameras = await Camera.GetAvailableCameras(CancellationToken.None);
			var preferred = _cameras.FirstOrDefault(c => c.Position == CameraPosition.Front) ?? _cameras.FirstOrDefault();
			if (preferred is null)
			{
				ViewModel.Status = "No se encontró ninguna cámara.";
				return;
			}

			await SelectCameraAsync(preferred);
			ViewModel.Status = "Muestra tu mano a la cámara";
			_loop.Start(Camera, recognizer, ViewModel);
		}
		catch (Exception ex)
		{
			ViewModel.Status = $"No se pudo iniciar: {ex.Message}";
		}
	}

	protected override void OnDisappearing()
	{
		ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
		DetachKeyboardShortcuts();
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
			ViewModel.Status = "Solo hay una cámara disponible.";
			return;
		}

		var index = _cameras.ToList().FindIndex(c => c.DeviceId == Camera.SelectedCamera?.DeviceId);
		await SelectCameraAsync(_cameras[(index + 1) % _cameras.Count]);
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(RecognitionViewModel.Landmarks) or nameof(RecognitionViewModel.ShowOverlay))
		{
			_skeleton.Landmarks = ViewModel.Landmarks;
			_overlay.Landmarks = ViewModel.ShowOverlay ? ViewModel.Landmarks : null;
			_overlay.FrameSize = ViewModel.FrameSize;
			_overlay.Mirror = PreviewIsMirrored;
			SkeletonView.Invalidate();
			OverlayView.Invalidate();
		}
	}

	/// <summary>
	/// Handles a keyboard shortcut (Windows). Key names follow Windows.System.VirtualKey:
	/// "E", "Space", "Back", "Delete". Returns true when the key was used.
	/// </summary>
	protected virtual bool HandleShortcut(string key)
	{
		if (key == RecognitionViewModel.OverlayShortcutKey)
		{
			ViewModel.ToggleOverlayCommand.Execute(null);
			return true;
		}

		return false;
	}

#if WINDOWS
	private Microsoft.UI.Xaml.UIElement? _keyboardRoot;
	private Microsoft.UI.Xaml.Input.KeyEventHandler? _keyDown;
	private Microsoft.UI.Xaml.Input.KeyEventHandler? _keyUp;
	private Windows.System.VirtualKey? _consumedKey;

	private void AttachKeyboardShortcuts()
	{
		// Shell may raise OnAppearing twice; a second set of handlers would undo every toggle.
		DetachKeyboardShortcuts();

		// While a Shell page appears it is not attached to its window yet, so use the app's window.
		var window = Window ?? Application.Current?.Windows.FirstOrDefault();
		if (window?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content: { } root })
		{
			return;
		}

		// Preview (tunneling) events reach the page before the focused control, so a shortcut
		// such as Space never also "presses" the button that happens to have the focus.
		_keyDown = (_, e) =>
		{
			if (HandleShortcut(e.Key.ToString()))
			{
				_consumedKey = e.Key;
				e.Handled = true;
			}
		};
		_keyUp = (_, e) =>
		{
			if (_consumedKey == e.Key)
			{
				_consumedKey = null;
				e.Handled = true;
			}
		};
		root.AddHandler(Microsoft.UI.Xaml.UIElement.PreviewKeyDownEvent, _keyDown, handledEventsToo: true);
		root.AddHandler(Microsoft.UI.Xaml.UIElement.PreviewKeyUpEvent, _keyUp, handledEventsToo: true);
		_keyboardRoot = root;

		// Windows delivers keys only when some element has the focus; after navigating, none has.
		root.DispatcherQueue.TryEnqueue(() =>
		{
			if (root.XamlRoot is { } xamlRoot && Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(xamlRoot) is null)
			{
				Microsoft.UI.Xaml.Input.FocusManager.TryMoveFocus(
					Microsoft.UI.Xaml.Input.FocusNavigationDirection.Next,
					new Microsoft.UI.Xaml.Input.FindNextElementOptions { SearchRoot = xamlRoot.Content });
			}
		});
	}

	private void DetachKeyboardShortcuts()
	{
		if (_keyboardRoot is not null)
		{
			_keyboardRoot.RemoveHandler(Microsoft.UI.Xaml.UIElement.PreviewKeyDownEvent, _keyDown);
			_keyboardRoot.RemoveHandler(Microsoft.UI.Xaml.UIElement.PreviewKeyUpEvent, _keyUp);
		}

		_keyboardRoot = null;
		_keyDown = null;
		_keyUp = null;
	}
#else
	private void AttachKeyboardShortcuts()
	{
	}

	private void DetachKeyboardShortcuts()
	{
	}
#endif

	/// <summary>Android shows the front camera mirrored (like a selfie); the captured frame is not.</summary>
	private bool PreviewIsMirrored =>
		DeviceInfo.Platform == DevicePlatform.Android && Camera.SelectedCamera?.Position == CameraPosition.Front;

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
