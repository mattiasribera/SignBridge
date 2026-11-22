using CommunityToolkit.Mvvm.ComponentModel;
using SignBridge.Core.Recognition;
using SignBridge.Services;

namespace SignBridge.ViewModels;

/// <summary>Shared state for screens that show what the camera currently recognizes.</summary>
public abstract partial class RecognitionViewModel : ObservableObject, IRecognitionConsumer
{
	/// <summary>Part of the shorter image side analyzed; matches the guide frame drawn on screen.</summary>
	public const float GuideFraction = 0.8f;

	[ObservableProperty]
	public partial string DetectedLabel { get; set; } = "–";

	[ObservableProperty]
	public partial double DetectedConfidence { get; set; }

	[ObservableProperty]
	public partial string Status { get; set; } = "Starting camera…";

	[ObservableProperty]
	public partial bool Mirror { get; set; }

	public FrameOptions FrameOptions => new(GuideFraction, Mirror);

	public void OnRecognized(RecognitionResult result)
	{
		DetectedLabel = result.Top.Label;
		DetectedConfidence = result.Top.Confidence;
		Handle(result.Top);
	}

	public void OnStatus(string message) => Status = message;

	protected abstract void Handle(SignPrediction top);

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
