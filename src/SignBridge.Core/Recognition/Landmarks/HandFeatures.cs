namespace SignBridge.Core.Recognition.Landmarks;

/// <summary>
/// Turns 21 hand points into the 63 features the landmark classifier was trained on:
/// relative to the wrist and divided by the wrist-to-middle-finger-base length, so the
/// position and size of the hand in the frame do not matter.
/// </summary>
public static class HandFeatures
{
    private const int MiddleFingerBase = 9;

    public static float[] FromLandmarks(IReadOnlyList<float> landmarks)
    {
        if (landmarks.Count != 63)
        {
            throw new ArgumentException("Expected 21 points with x, y, z.", nameof(landmarks));
        }

        float wx = landmarks[0], wy = landmarks[1], wz = landmarks[2];
        var dx = landmarks[MiddleFingerBase * 3] - wx;
        var dy = landmarks[MiddleFingerBase * 3 + 1] - wy;
        var scale = MathF.Max(MathF.Sqrt(dx * dx + dy * dy), 1e-6f);

        var features = new float[63];
        for (var i = 0; i < 21; i++)
        {
            features[i * 3] = (landmarks[i * 3] - wx) / scale;
            features[i * 3 + 1] = (landmarks[i * 3 + 1] - wy) / scale;
            features[i * 3 + 2] = (landmarks[i * 3 + 2] - wz) / scale;
        }

        return features;
    }
}
