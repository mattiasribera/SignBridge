using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace SignBridge.Core.Recognition.Landmarks;

/// <summary>A detected palm: box corners and 7 palm keypoints in image pixels.</summary>
public sealed record Palm(SKRect Box, SKPoint[] Keypoints, float Score);

/// <summary>
/// MediaPipe palm detector (OpenCV Zoo palm_detection_mediapipe_2023feb.onnx, Apache-2.0).
/// Port of OpenCV Zoo mp_palmdet.py; keeps only the highest-scoring palm.
/// </summary>
public sealed class PalmDetector(InferenceSession session, float scoreThreshold = 0.6f)
{
    public const int InputSize = 192;

    private static readonly SKPoint[] Anchors = GenerateAnchors();

    /// <summary>MediaPipe SSD anchors for a 192x192 input: strides 8 (2 per cell) and 16 (6 per cell).</summary>
    public static SKPoint[] GenerateAnchors()
    {
        var anchors = new List<SKPoint>(2016);
        foreach (var (stride, perCell) in new[] { (8, 2), (16, 6) })
        {
            var cells = InputSize / stride;
            for (var y = 0; y < cells; y++)
            {
                for (var x = 0; x < cells; x++)
                {
                    for (var i = 0; i < perCell; i++)
                    {
                        anchors.Add(new SKPoint((x + 0.5f) / cells, (y + 0.5f) / cells));
                    }
                }
            }
        }

        return [.. anchors];
    }

    public Palm? Detect(SKBitmap image)
    {
        int w = image.Width, h = image.Height;
        var ratio = Math.Min((float)InputSize / h, (float)InputSize / w);
        int newW = (int)(w * ratio), newH = (int)(h * ratio);
        int left = (InputSize - newW) / 2, top = (InputSize - newH) / 2;
        var padBias = new SKPoint((int)(left / ratio), (int)(top / ratio));

        using var input = new SKBitmap(new SKImageInfo(InputSize, InputSize, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(input))
        {
            canvas.Clear(SKColors.Black);
            using var source = SKImage.FromBitmap(image);
            // Plain bilinear (no mipmaps) to match OpenCV INTER_LINEAR used by the reference pipeline.
            canvas.DrawImage(source, SKRect.Create(left, top, newW, newH), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
        }

        var tensor = new DenseTensor<float>(TensorConverter.ToNhwc(input), [1, InputSize, InputSize, 3]);
        using var outputs = session.Run([NamedOnnxValue.CreateFromTensor("input_1", tensor)]);
        var regressors = outputs.First(o => o.Name == "Identity").AsEnumerable<float>().ToArray();
        var logits = outputs.First(o => o.Name == "Identity_1").AsEnumerable<float>().ToArray();

        var best = 0;
        for (var i = 1; i < logits.Length; i++)
        {
            if (logits[i] > logits[best])
            {
                best = i;
            }
        }

        var score = 1f / (1f + MathF.Exp(-logits[best]));
        if (score < scoreThreshold)
        {
            return null;
        }

        var scale = (float)Math.Max(w, h);
        var anchor = Anchors[best];
        var d = regressors.AsSpan(best * 18, 18);
        float cx = d[0] / InputSize + anchor.X, cy = d[1] / InputSize + anchor.Y;
        float bw = d[2] / InputSize, bh = d[3] / InputSize;
        var box = new SKRect(
            (cx - bw / 2) * scale - padBias.X,
            (cy - bh / 2) * scale - padBias.Y,
            (cx + bw / 2) * scale - padBias.X,
            (cy + bh / 2) * scale - padBias.Y);

        var keypoints = new SKPoint[7];
        for (var k = 0; k < 7; k++)
        {
            keypoints[k] = new SKPoint(
                (d[4 + k * 2] / InputSize + anchor.X) * scale - padBias.X,
                (d[5 + k * 2] / InputSize + anchor.Y) * scale - padBias.Y);
        }

        return new Palm(box, keypoints, score);
    }
}
