using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace SignBridge.Core.Recognition.Landmarks;

/// <summary>
/// MediaPipe hand landmark model (OpenCV Zoo handpose_estimation_mediapipe_2023feb.onnx, Apache-2.0).
/// Port of OpenCV Zoo mp_handpose.py: crops around the palm, rotates the hand upright,
/// predicts 21 points and maps them back to image pixels.
/// </summary>
public sealed class HandLandmarker(InferenceSession session, float confidenceThreshold = 0.8f)
{
    public const int InputSize = 224;

    private const int PalmBaseIndex = 0;
    private const int MiddleFingerBaseIndex = 2;

    /// <returns>63 floats (x,y,z for 21 points) in image pixels, or null if the hand is not confirmed.</returns>
    public float[]? Detect(SKBitmap image, Palm palm)
    {
        // 1. Crop a generous square around the palm (padded to its diagonal so rotation keeps corners).
        var first = CropAndPad(image, palm.Box, shiftY: 0f, enlarge: 4f, padToDiagonal: true);
        if (first is null)
        {
            return null;
        }

        using var padded = first.Value.Image;
        var bias = first.Value.Bias;
        var box = first.Value.Box;
        box.Offset(-bias.X, -bias.Y);
        var keypoints = palm.Keypoints.Select(p => new SKPoint(p.X - bias.X, p.Y - bias.Y)).ToArray();

        // 2. Rotate so the wrist -> middle finger direction points up.
        var p1 = keypoints[PalmBaseIndex];
        var p2 = keypoints[MiddleFingerBaseIndex];
        var radians = Math.PI / 2 - Math.Atan2(-(p2.Y - p1.Y), p2.X - p1.X);
        radians -= 2 * Math.PI * Math.Floor((radians + Math.PI) / (2 * Math.PI));
        var angle = radians * 180 / Math.PI;
        var rotation = RotationMatrix(box.MidX, box.MidY, angle);

        using var rotated = new SKBitmap(new SKImageInfo(padded.Width, padded.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(rotated))
        {
            canvas.Clear(SKColors.Black);
            canvas.SetMatrix(rotation);
            using var source = SKImage.FromBitmap(padded);
            canvas.DrawImage(source, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
        }

        var rotatedKeypoints = keypoints.Select(rotation.MapPoint).ToArray();
        var rotatedBox = new SKRect(
            rotatedKeypoints.Min(p => p.X), rotatedKeypoints.Min(p => p.Y),
            rotatedKeypoints.Max(p => p.X), rotatedKeypoints.Max(p => p.Y));

        // 3. Crop the upright hand, resize to the model input and run it.
        var second = CropAndPad(rotated, rotatedBox, shiftY: -0.4f, enlarge: 3f, padToDiagonal: false);
        if (second is null)
        {
            return null;
        }

        using var crop = second.Value.Image;
        var handBox = second.Value.Box;
        using var input = new SKBitmap(new SKImageInfo(InputSize, InputSize, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(input))
        {
            using var source = SKImage.FromBitmap(crop);
            canvas.DrawImage(source, SKRect.Create(InputSize, InputSize), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        }

        var tensor = new DenseTensor<float>(TensorConverter.ToNhwc(input), [1, InputSize, InputSize, 3]);
        using var outputs = session.Run([NamedOnnxValue.CreateFromTensor("input_1", tensor)]);
        var confidence = outputs.First(o => o.Name == "Identity_1").AsEnumerable<float>().First();
        if (confidence < confidenceThreshold)
        {
            return null;
        }

        var landmarks = outputs.First(o => o.Name == "Identity").AsEnumerable<float>().ToArray();
        return ToImageCoordinates(landmarks, handBox, angle, rotation, bias);
    }

    private static float[] ToImageCoordinates(float[] landmarks, SKRect handBox, double angle, SKMatrix rotation, SKPoint bias)
    {
        var scale = Math.Max(handBox.Width, handBox.Height) / InputSize;
        var a = Math.Cos(angle * Math.PI / 180);
        var b = Math.Sin(angle * Math.PI / 180);

        // Undo the rotation of the box center (inverse of the affine rotation matrix).
        double tx = rotation.TransX, ty = rotation.TransY;
        double invTx = -(a * tx - b * ty), invTy = -(b * tx + a * ty);
        double cx = handBox.MidX, cy = handBox.MidY;
        var originalX = a * cx - b * cy + invTx;
        var originalY = b * cx + a * cy + invTy;

        var result = new float[63];
        for (var i = 0; i < 21; i++)
        {
            var x = (landmarks[i * 3] - InputSize / 2f) * scale;
            var y = (landmarks[i * 3 + 1] - InputSize / 2f) * scale;
            result[i * 3] = (float)(x * a - y * b + originalX + bias.X);
            result[i * 3 + 1] = (float)(x * b + y * a + originalY + bias.Y);
            result[i * 3 + 2] = landmarks[i * 3 + 2] * scale;
        }

        return result;
    }

    /// <summary>Same as OpenCV getRotationMatrix2D(center, angleDegrees, 1).</summary>
    private static SKMatrix RotationMatrix(float cx, float cy, double angleDegrees)
    {
        var a = (float)Math.Cos(angleDegrees * Math.PI / 180);
        var b = (float)Math.Sin(angleDegrees * Math.PI / 180);
        return new SKMatrix(a, b, (1 - a) * cx - b * cy, -b, a, b * cx + (1 - a) * cy, 0, 0, 1);
    }

    /// <summary>Shift and enlarge the box, crop it (clipped to the image) and pad the crop to a square.</summary>
    private static (SKBitmap Image, SKRect Box, SKPoint Bias)? CropAndPad(SKBitmap image, SKRect box, float shiftY, float enlarge, bool padToDiagonal)
    {
        box.Offset(0, shiftY * box.Height);
        float halfW = box.Width * enlarge / 2, halfH = box.Height * enlarge / 2;
        int x1 = Math.Clamp((int)(box.MidX - halfW), 0, image.Width);
        int y1 = Math.Clamp((int)(box.MidY - halfH), 0, image.Height);
        int x2 = Math.Clamp((int)(box.MidX + halfW), 0, image.Width);
        int y2 = Math.Clamp((int)(box.MidY + halfH), 0, image.Height);
        int cropW = x2 - x1, cropH = y2 - y1;
        if (cropW <= 1 || cropH <= 1)
        {
            return null;
        }

        var side = padToDiagonal ? (int)Math.Sqrt(cropW * (double)cropW + cropH * (double)cropH) : Math.Max(cropW, cropH);
        int left = (side - cropW) / 2, top = (side - cropH) / 2;

        var result = new SKBitmap(new SKImageInfo(side, side, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(result))
        {
            canvas.Clear(SKColors.Black);
            using var source = SKImage.FromBitmap(image);
            canvas.DrawImage(source, SKRect.Create(x1, y1, cropW, cropH), SKRect.Create(left, top, cropW, cropH), new SKSamplingOptions(SKFilterMode.Nearest));
        }

        return (result, new SKRect(x1, y1, x2, y2), new SKPoint(x1 - left, y1 - top));
    }
}
