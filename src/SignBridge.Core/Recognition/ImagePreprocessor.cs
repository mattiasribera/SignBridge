using SkiaSharp;

namespace SignBridge.Core.Recognition;

/// <summary>
/// Converts an image into the planar RGB tensor expected by the EfficientNet model:
/// centered square crop, resize, scale to [0,1], normalize with the model's mean/std.
/// Mirrors the Python preprocessing validated in tools/export_onnx.py.
/// </summary>
public static class ImagePreprocessor
{
    public const int ModelInputSize = 224;

    // Values from the model's preprocessor_config.json on Hugging Face.
    private static readonly float[] Mean = [0.485f, 0.456f, 0.406f];
    private static readonly float[] Std = [0.47853944f, 0.4732864f, 0.47434163f];

    public static SKBitmap Decode(Stream encodedImage)
    {
        using var codecStream = new SKManagedStream(encodedImage);
        using var codec = SKCodec.Create(codecStream)
            ?? throw new InvalidDataException("The image could not be decoded.");
        var decoded = SKBitmap.Decode(codec)
            ?? throw new InvalidDataException("The image could not be decoded.");
        return ApplyOrientation(decoded, codec.EncodedOrigin);
    }

    public static float[] ToTensor(SKBitmap source, FrameOptions options, int size = ModelInputSize)
    {
        var side = Math.Max(1, (int)(Math.Min(source.Width, source.Height) * Math.Clamp(options.CropFraction, 0.1f, 1f)));
        var crop = SKRectI.Create((source.Width - side) / 2, (source.Height - side) / 2, side, side);

        using var resized = new SKBitmap(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(resized))
        {
            if (options.Mirror)
            {
                canvas.Scale(-1, 1, size / 2f, 0);
            }

            using var image = SKImage.FromBitmap(source);
            canvas.DrawImage(image, crop, SKRect.Create(size, size), new SKSamplingOptions(SKFilterMode.Linear));
        }

        var plane = size * size;
        var tensor = new float[3 * plane];
        var pixels = resized.Pixels;
        for (var i = 0; i < plane; i++)
        {
            var c = pixels[i];
            tensor[i] = (c.Red / 255f - Mean[0]) / Std[0];
            tensor[plane + i] = (c.Green / 255f - Mean[1]) / Std[1];
            tensor[2 * plane + i] = (c.Blue / 255f - Mean[2]) / Std[2];
        }

        return tensor;
    }

    // Phone cameras store rotation as EXIF metadata; apply it so the hand is upright.
    private static SKBitmap ApplyOrientation(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft)
        {
            return bitmap;
        }

        var rotated = origin is SKEncodedOrigin.RightTop or SKEncodedOrigin.LeftBottom;
        var result = new SKBitmap(rotated ? bitmap.Height : bitmap.Width, rotated ? bitmap.Width : bitmap.Height);
        using (var canvas = new SKCanvas(result))
        {
            switch (origin)
            {
                case SKEncodedOrigin.BottomRight:
                    canvas.RotateDegrees(180, bitmap.Width / 2f, bitmap.Height / 2f);
                    break;
                case SKEncodedOrigin.RightTop:
                    canvas.Translate(result.Width, 0);
                    canvas.RotateDegrees(90);
                    break;
                case SKEncodedOrigin.LeftBottom:
                    canvas.Translate(0, result.Height);
                    canvas.RotateDegrees(270);
                    break;
                case SKEncodedOrigin.TopRight:
                    canvas.Scale(-1, 1, bitmap.Width / 2f, 0);
                    break;
                case SKEncodedOrigin.BottomLeft:
                    canvas.Scale(1, -1, 0, bitmap.Height / 2f);
                    break;
            }

            using var image = SKImage.FromBitmap(bitmap);
            canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
        }

        bitmap.Dispose();
        return result;
    }
}
