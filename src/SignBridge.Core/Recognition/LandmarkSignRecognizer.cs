using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SignBridge.Core.Recognition.Landmarks;

namespace SignBridge.Core.Recognition;

/// <summary>
/// Recognizes ASL letters from the shape of the hand, not from pixels:
/// palm detector -> 21 hand landmarks -> normalized features -> small MLP classifier.
/// Background, clothing and skin color never reach the classifier, which is why it
/// generalizes to new people far better than whole-image classifiers.
/// </summary>
/// <remarks>The whole frame is analyzed: the palm detector finds the hand, and the classifier was
/// trained on left and right hands, so no cropping or mirroring is needed.</remarks>
public sealed class LandmarkSignRecognizer : ISignRecognizer
{
    private readonly InferenceSession[] _sessions;
    private readonly PalmDetector _palms;
    private readonly HandLandmarker _hands;
    private readonly InferenceSession _classifier;
    private readonly IReadOnlyList<string> _labels;
    private readonly Lock _gate = new();

    private LandmarkSignRecognizer(InferenceSession palm, InferenceSession hand, InferenceSession classifier, IReadOnlyList<string> labels)
    {
        _sessions = [palm, hand, classifier];
        _palms = new PalmDetector(palm);
        _hands = new HandLandmarker(hand);
        _classifier = classifier;
        _labels = labels;
    }

    public static LandmarkSignRecognizer Create(byte[] palmModel, byte[] handModel, byte[] classifierModel, string labelsJson)
    {
        var labels = JsonSerializer.Deserialize<string[]>(labelsJson)
            ?? throw new InvalidDataException("Labels file is empty.");
        var options = new SessionOptions { GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL };
        return new LandmarkSignRecognizer(
            new InferenceSession(palmModel, options),
            new InferenceSession(handModel, options),
            new InferenceSession(classifierModel, options),
            labels);
    }

    public RecognitionResult Recognize(Stream encodedImage)
    {
        using var bitmap = ImageDecoder.Decode(encodedImage);
        lock (_gate)
        {
            var palm = _palms.Detect(bitmap);
            var landmarks = palm is null ? null : _hands.Detect(bitmap, palm);
            if (landmarks is null)
            {
                return RecognitionResult.NoHand;
            }

            return Classify(HandFeatures.FromLandmarks(landmarks)) with { Landmarks = landmarks };
        }
    }

    public RecognitionResult Classify(float[] features)
    {
        var input = new DenseTensor<float>(features, [1, features.Length]);
        using var outputs = _classifier.Run([NamedOnnxValue.CreateFromTensor("X", input)], ["probabilities"]);
        var probabilities = outputs[0].AsEnumerable<float>().ToArray();
        var ranked = probabilities
            .Select((p, i) => new SignPrediction(_labels[i], p))
            .OrderByDescending(p => p.Confidence)
            .ToArray();
        return new RecognitionResult(ranked);
    }

    public void Dispose()
    {
        foreach (var session in _sessions)
        {
            session.Dispose();
        }
    }
}
