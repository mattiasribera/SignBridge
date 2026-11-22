using SkiaSharp;

namespace SignBridge.Core.Recognition;

/// <summary>Decodes camera images and applies their EXIF orientation so the hand is upright.</summary>
public static class ImageDecoder
{
    public static SKBitmap Decode(Stream encodedImage)
    {
        using var codecStream = new SKManagedStream(encodedImage);
        using var codec = SKCodec.Create(codecStream)
            ?? throw new InvalidDataException("The image could not be decoded.");
        var decoded = SKBitmap.Decode(codec)
            ?? throw new InvalidDataException("The image could not be decoded.");
        return ApplyOrientation(decoded, codec.EncodedOrigin);
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
