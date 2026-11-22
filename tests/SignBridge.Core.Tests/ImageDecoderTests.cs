using SignBridge.Core.Recognition;

namespace SignBridge.Core.Tests;

public class ImageDecoderTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    [Fact]
    public void Applies_exif_rotation_from_phone_cameras()
    {
        // 40x20 image, blue strip on its left, EXIF orientation 6 (display rotated 90° clockwise).
        using var stream = File.OpenRead(Fixture("exif_rotated.jpg"));

        using var bitmap = ImageDecoder.Decode(stream);

        Assert.Equal(20, bitmap.Width);
        Assert.Equal(40, bitmap.Height);
        var top = bitmap.GetPixel(10, 2);
        var bottom = bitmap.GetPixel(10, 37);
        Assert.True(top.Blue > 200 && top.Red < 60, $"top should be blue, was {top}");
        Assert.True(bottom.Red > 200 && bottom.Blue < 60, $"bottom should be red, was {bottom}");
    }

    [Fact]
    public void Rejects_an_image_that_cannot_be_decoded()
    {
        using var garbage = new MemoryStream([1, 2, 3, 4]);

        Assert.Throws<InvalidDataException>(() => ImageDecoder.Decode(garbage));
    }
}
