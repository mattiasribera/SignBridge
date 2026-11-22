using System.Text.Json;
using SignBridge.Core.Recognition;
using SignBridge.Core.Recognition.Landmarks;
using SkiaSharp;

namespace SignBridge.Core.Tests;

/// <summary>
/// Parity with the Python reference pipeline (tools/landmark_reference.py, OpenCV Zoo MediaPipe
/// models + landmark MLP). Fixture photos come from EitanG98/asl_letters (Unlicense).
/// </summary>
public class LandmarkSignRecognizerTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    private static readonly Lazy<Reference> Expected = new(() =>
        JsonSerializer.Deserialize<Reference>(File.ReadAllText(Fixture("landmark_reference.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!);

    private static LandmarkSignRecognizer CreateRecognizer() =>
        LandmarkSignRecognizer.Create(
            File.ReadAllBytes(Fixture("palm_detection.onnx")),
            File.ReadAllBytes(Fixture("hand_landmarks.onnx")),
            File.ReadAllBytes(Fixture("asl_landmark_mlp.onnx")),
            File.ReadAllText(Fixture("asl_landmark_labels.json")));

    [Fact]
    public void Generated_anchors_match_mediapipe()
    {
        var anchors = PalmDetector.GenerateAnchors();
        var expected = Expected.Value.Anchors;

        Assert.Equal(expected.Length, anchors.Length);
        for (var i = 0; i < anchors.Length; i++)
        {
            Assert.Equal(expected[i][0], anchors[i].X, 5);
            Assert.Equal(expected[i][1], anchors[i].Y, 5);
        }
    }

    [Fact]
    public void Features_are_wrist_relative_and_scale_invariant()
    {
        var sample = Expected.Value.Images["hand_A.jpg"];
        var landmarks = sample.Landmarks!.SelectMany(p => p).Select(v => (float)v).ToArray();

        var features = HandFeatures.FromLandmarks(landmarks);

        Assert.Equal(63, features.Length);
        for (var i = 0; i < 63; i++)
        {
            Assert.Equal(sample.Features![i], features[i], 4);
        }

        var doubled = HandFeatures.FromLandmarks(landmarks.Select(v => v * 2 + 10).ToArray());
        Assert.Equal(features[30], doubled[30], 4);
    }

    [Fact]
    public void Classifier_matches_reference_probabilities()
    {
        using var recognizer = CreateRecognizer();
        var sample = Expected.Value.Images["hand_L.jpg"];

        var result = recognizer.Classify(sample.Features!.Select(v => (float)v).ToArray());

        Assert.Equal("L", result.Top.Label);
        Assert.Equal((float)sample.Probabilities!.Max(), result.Top.Confidence, 4);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("B")]
    [InlineData("L")]
    [InlineData("O")]
    [InlineData("V")]
    [InlineData("Y")]
    public void Recognizes_photos_end_to_end_like_the_python_pipeline(string letter)
    {
        using var recognizer = CreateRecognizer();
        using var image = File.OpenRead(Fixture($"hand_{letter}.jpg"));
        var expected = Expected.Value.Images[$"hand_{letter}.jpg"];

        var result = recognizer.Recognize(image);

        Assert.True(result.HandFound);
        Assert.Equal(letter, result.Top.Label);
        Assert.NotNull(result.Landmarks);

        // Landmarks within 6% of the hand size of the reference (resampling differs slightly).
        var reference = expected.Landmarks!;
        var handSize = Distance(reference[0][0], reference[0][1], reference[9][0], reference[9][1]);
        var meanError = Enumerable.Range(0, 21)
            .Average(i => Distance(reference[i][0], reference[i][1], result.Landmarks![i * 3], result.Landmarks[i * 3 + 1]));
        Assert.True(meanError < handSize * 0.06, $"mean landmark error {meanError:F1}px vs hand size {handSize:F1}px");
    }

    [Theory]
    [InlineData("A")]
    [InlineData("B")]
    [InlineData("L")]
    [InlineData("O")]
    [InlineData("V")]
    [InlineData("Y")]
    public void Recognizes_the_same_sign_made_with_the_left_hand(string letter)
    {
        using var recognizer = CreateRecognizer();
        using var leftHand = Mirrored(Fixture($"hand_{letter}.jpg"));

        var result = recognizer.Recognize(leftHand);

        Assert.True(result.HandFound);
        Assert.Equal(letter, result.Top.Label);
    }

    [Fact]
    public void Reports_the_frame_size_the_landmarks_refer_to()
    {
        using var recognizer = CreateRecognizer();
        using var image = File.OpenRead(Fixture("hand_A.jpg"));
        using var decoded = SKBitmap.Decode(Fixture("hand_A.jpg"));

        var result = recognizer.Recognize(image);

        Assert.Equal(decoded.Width, result.FrameWidth);
        Assert.Equal(decoded.Height, result.FrameHeight);
    }

    [Fact]
    public void Reports_no_hand_on_an_empty_scene()
    {
        using var recognizer = CreateRecognizer();
        using var image = File.OpenRead(Fixture("no_hand.jpg"));

        var result = recognizer.Recognize(image);

        Assert.False(result.HandFound);
        Assert.Equal(string.Empty, result.Top.Label);
        Assert.Equal(0f, result.Top.Confidence);
        Assert.Empty(result.Ranked);
    }

    private static double Distance(double x1, double y1, double x2, double y2) => Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));

    private sealed record Reference(double[][] Anchors, Dictionary<string, ReferenceImage> Images);

    private sealed record ReferenceImage(
        [property: System.Text.Json.Serialization.JsonPropertyName("palm_score")] double? PalmScore,
        double[][]? Landmarks,
        double[]? Features,
        double[]? Probabilities);

    private static MemoryStream Mirrored(string path)
    {
        using var source = SKBitmap.Decode(path);
        using var mirrored = new SKBitmap(source.Width, source.Height);
        using (var canvas = new SKCanvas(mirrored))
        {
            canvas.Scale(-1, 1, source.Width / 2f, 0);
            canvas.DrawBitmap(source, 0, 0);
        }

        var stream = new MemoryStream();
        mirrored.Encode(stream, SKEncodedImageFormat.Jpeg, 95);
        stream.Position = 0;
        return stream;
    }
}
