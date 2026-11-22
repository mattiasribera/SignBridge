using SignBridge.Core.Recognition;
using SkiaSharp;

namespace SignBridge.Core.Tests;

public class OnnxSignRecognizerTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    private static OnnxSignRecognizer CreateRecognizer() =>
        OnnxSignRecognizer.Create(
            File.ReadAllBytes(Fixture("asl_efficientnet_b0.onnx")),
            File.ReadAllText(Fixture("asl_labels.json")));

    [Theory]
    [InlineData("A")]
    [InlineData("B")]
    [InlineData("L")]
    [InlineData("Y")]
    public void Recognizes_dataset_sample_letters(string letter)
    {
        using var recognizer = CreateRecognizer();
        using var image = File.OpenRead(Fixture($"letter_{letter}.jpg"));

        var result = recognizer.Recognize(image, FrameOptions.Default);

        Assert.Equal(letter, result.Top.Label);
        Assert.InRange(result.Top.Confidence, 0.5f, 1f);
        Assert.Equal(26, result.Ranked.Count);
        Assert.True(result.Ranked.SequenceEqual(result.Ranked.OrderByDescending(p => p.Confidence)));
        Assert.Equal(1f, result.Ranked.Sum(p => p.Confidence), 3);
    }

    [Fact]
    public void Rejects_an_image_that_cannot_be_decoded()
    {
        using var recognizer = CreateRecognizer();
        using var garbage = new MemoryStream([1, 2, 3, 4]);

        Assert.Throws<InvalidDataException>(() => recognizer.Recognize(garbage, FrameOptions.Default));
    }
}

public class ImagePreprocessorTests
{
    [Fact]
    public void Produces_a_normalized_planar_tensor()
    {
        using var bitmap = new SKBitmap(10, 10);
        bitmap.Erase(new SKColor(255, 0, 0));

        var tensor = ImagePreprocessor.ToTensor(bitmap, FrameOptions.Default, size: 4);

        Assert.Equal(3 * 4 * 4, tensor.Length);
        Assert.Equal((1f - 0.485f) / 0.47853944f, tensor[0], 3);
        Assert.Equal((0f - 0.456f) / 0.4732864f, tensor[16], 3);
        Assert.Equal((0f - 0.406f) / 0.47434163f, tensor[32], 3);
    }

    [Fact]
    public void Crops_the_center_square_and_can_mirror()
    {
        // Left half black, right half white, 20x10: the center 10x10 crop is half and half.
        using var bitmap = new SKBitmap(20, 10);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Black);
            using var white = new SKPaint { Color = SKColors.White };
            canvas.DrawRect(10, 0, 10, 10, white);
        }

        var plain = ImagePreprocessor.ToTensor(bitmap, FrameOptions.Default, size: 2);
        var mirrored = ImagePreprocessor.ToTensor(bitmap, FrameOptions.Default with { Mirror = true }, size: 2);

        Assert.True(plain[0] < 0 && plain[1] > 0, "left pixel dark, right pixel bright");
        Assert.True(mirrored[0] > 0 && mirrored[1] < 0, "mirrored swaps sides");
    }
}
