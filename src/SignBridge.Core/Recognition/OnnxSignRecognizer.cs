using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace SignBridge.Core.Recognition;

/// <summary>
/// Runs the Hugging Face ASL alphabet classifier (EfficientNet-B0 exported to ONNX) on-device.
/// </summary>
public sealed class OnnxSignRecognizer : ISignRecognizer
{
    private const string InputName = "pixel_values";
    private readonly InferenceSession _session;
    private readonly IReadOnlyList<string> _labels;
    private readonly Lock _gate = new();

    private OnnxSignRecognizer(InferenceSession session, IReadOnlyList<string> labels)
    {
        _session = session;
        _labels = labels;
    }

    /// <param name="model">Bytes of asl_efficientnet_b0.onnx.</param>
    /// <param name="labelsJson">JSON array mapping class index to label (asl_labels.json).</param>
    public static OnnxSignRecognizer Create(byte[] model, string labelsJson)
    {
        var labels = JsonSerializer.Deserialize<string[]>(labelsJson)
            ?? throw new InvalidDataException("Labels file is empty.");
        var options = new SessionOptions { GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL };
        return new OnnxSignRecognizer(new InferenceSession(model, options), labels);
    }

    public RecognitionResult Recognize(Stream encodedImage, FrameOptions options)
    {
        using var bitmap = ImagePreprocessor.Decode(encodedImage);
        var pixels = ImagePreprocessor.ToTensor(bitmap, options);
        var size = ImagePreprocessor.ModelInputSize;
        var input = new DenseTensor<float>(pixels, [1, 3, size, size]);

        float[] logits;
        lock (_gate)
        {
            using var outputs = _session.Run([NamedOnnxValue.CreateFromTensor(InputName, input)]);
            logits = outputs[0].AsEnumerable<float>().ToArray();
        }

        return new RecognitionResult(Rank(Softmax(logits)));
    }

    private IReadOnlyList<SignPrediction> Rank(float[] probabilities) =>
        probabilities
            .Select((p, i) => new SignPrediction(_labels[i], p))
            .OrderByDescending(p => p.Confidence)
            .ToArray();

    private static float[] Softmax(float[] logits)
    {
        var max = logits.Max();
        var exp = logits.Select(l => MathF.Exp(l - max)).ToArray();
        var sum = exp.Sum();
        return exp.Select(e => e / sum).ToArray();
    }

    public void Dispose() => _session.Dispose();
}
