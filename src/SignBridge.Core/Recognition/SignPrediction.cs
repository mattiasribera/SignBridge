namespace SignBridge.Core.Recognition;

/// <summary>One candidate sign and the model's probability for it (0..1).</summary>
public sealed record SignPrediction(string Label, float Confidence);

/// <summary>All candidates for one frame, ordered from most to least likely.</summary>
/// <param name="Ranked">Candidates, most likely first. Empty when no hand was found.</param>
/// <param name="Landmarks">21 hand points as x,y,z triplets in image pixels, when the recognizer provides them.</param>
public sealed record RecognitionResult(IReadOnlyList<SignPrediction> Ranked, float[]? Landmarks = null)
{
    private static readonly SignPrediction Nothing = new(string.Empty, 0f);

    public static RecognitionResult NoHand { get; } = new([]);

    public bool HandFound => Ranked.Count > 0;

    public SignPrediction Top => Ranked.Count > 0 ? Ranked[0] : Nothing;

    /// <summary>Width in pixels of the analyzed frame (after EXIF rotation); <see cref="Landmarks"/> refer to it.</summary>
    public int FrameWidth { get; init; }

    /// <summary>Height in pixels of the analyzed frame (after EXIF rotation).</summary>
    public int FrameHeight { get; init; }
}

/// <summary>
/// Port for any sign recognizer. The app depends only on this, so the current ASL
/// alphabet recognizer can be swapped for an LSB or whole-word model without touching the UI.
/// </summary>
public interface ISignRecognizer : IDisposable
{
    RecognitionResult Recognize(Stream encodedImage);
}
