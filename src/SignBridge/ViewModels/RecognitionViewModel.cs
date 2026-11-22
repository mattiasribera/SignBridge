using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SignBridge.Core.Recognition;
using SignBridge.Services;

namespace SignBridge.ViewModels;

/// <summary>Shared state for screens that show what the camera currently recognizes.</summary>
public abstract partial class RecognitionViewModel : ObservableObject, IRecognitionConsumer
{
	[ObservableProperty]
	public partial string DetectedLabel { get; set; } = "–";

	[ObservableProperty]
	public partial double DetectedConfidence { get; set; }

	/// <summary>Reference picture of the sign the model thinks it sees, to compare with your hand.</summary>
	[ObservableProperty]
	public partial string? DetectedImage { get; set; }

	/// <summary>21 hand points (x,y,z triplets) of the last frame, or null when no hand is visible.</summary>
	[ObservableProperty]
	public partial float[]? Landmarks { get; set; }

	[ObservableProperty]
	public partial bool HandVisible { get; set; }

	/// <summary>Size of the frame the <see cref="Landmarks"/> refer to.</summary>
	public Size FrameSize { get; private set; }

	/// <summary>Draw the skeleton on top of the hand in the preview instead of in the corner thumbnail.</summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ShowThumbnail), nameof(OverlayButtonText))]
	public partial bool ShowOverlay { get; set; } = Preferences.Default.Get(OverlayPreference, false);

	public bool ShowThumbnail => !ShowOverlay;

	public string OverlayButtonText => ShowOverlay ? "✋ Esqueleto en miniatura" : "✋ Esqueleto sobre mi mano";

	[ObservableProperty]
	public partial string Status { get; set; } = "Iniciando la cámara…";

	private const string OverlayPreference = "skeleton_overlay";

	public void OnRecognized(RecognitionResult result)
	{
		HandVisible = result.HandFound;
		FrameSize = new Size(result.FrameWidth, result.FrameHeight);
		Landmarks = result.Landmarks;
		DetectedLabel = result.HandFound ? result.Top.Label : "–";
		DetectedConfidence = result.Top.Confidence;
		DetectedImage = result.HandFound ? SignImage(result.Top.Label) : null;
		Handle(result);
	}

	public void OnStatus(string message) => Status = message;

	[RelayCommand]
	private void ToggleOverlay()
	{
		ShowOverlay = !ShowOverlay;
		Preferences.Default.Set(OverlayPreference, ShowOverlay);
	}

	/// <summary>Image resource for a letter (public-domain ASL drawings from Wikimedia Commons).</summary>
	public static string SignImage(string letter) => $"asl_{letter.ToLowerInvariant()}.png";

	protected abstract void Handle(RecognitionResult result);

	protected static void Vibrate()
	{
		try
		{
			HapticFeedback.Default.Perform(HapticFeedbackType.Click);
		}
		catch (FeatureNotSupportedException)
		{
			// Desktop: no haptics.
		}
	}
}
