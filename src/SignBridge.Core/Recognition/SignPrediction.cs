namespace SignBridge.Core.Recognition;

/// <summary>One candidate sign and the model's probability for it (0..1).</summary>
public sealed record SignPrediction(string Label, float Confidence);

/// <summary>All candidates for one frame, ordered from most to least likely.</summary>
public sealed record RecognitionResult(IReadOnlyList<SignPrediction> Ranked)
{
    public SignPrediction Top => Ranked[0];
}

/// <summary>How a camera frame is turned into model input.</summary>
/// <param name="CropFraction">Side of the centered square crop, relative to the shorter image side.</param>
/// <param name="Mirror">Flip horizontally (front cameras deliver mirrored selfies on some platforms).</param>
public sealed record FrameOptions(float CropFraction = 1f, bool Mirror = false)
{
    public static FrameOptions Default { get; } = new();
}

/// <summary>
/// Port for any sign recognizer. The app depends only on this, so the current
/// ASL alphabet classifier can be swapped for a landmark- or LSB-based model.
/// </summary>
public interface ISignRecognizer : IDisposable
{
    RecognitionResult Recognize(Stream encodedImage, FrameOptions options);
}
