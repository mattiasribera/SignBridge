using SkiaSharp;

namespace SignBridge.Core.Recognition.Landmarks;

internal static class TensorConverter
{
    /// <summary>Interleaved RGB (NHWC) floats scaled to [0,1], as the MediaPipe models expect.</summary>
    public static float[] ToNhwc(SKBitmap bitmap)
    {
        var pixels = bitmap.Pixels;
        var data = new float[pixels.Length * 3];
        for (var i = 0; i < pixels.Length; i++)
        {
            data[i * 3] = pixels[i].Red / 255f;
            data[i * 3 + 1] = pixels[i].Green / 255f;
            data[i * 3 + 2] = pixels[i].Blue / 255f;
        }

        return data;
    }
}
