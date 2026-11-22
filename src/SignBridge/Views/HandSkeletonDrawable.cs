namespace SignBridge.Views;

/// <summary>
/// Draws the 21 detected hand points and their bones, fitted to the view. It shows what the
/// model actually "sees": the shape of the hand, without background or skin.
/// </summary>
public sealed class HandSkeletonDrawable : IDrawable
{
	// MediaPipe hand topology: thumb, index, middle, ring, little finger and palm.
	private static readonly (int From, int To)[] Bones =
	[
		(0, 1), (1, 2), (2, 3), (3, 4),
		(0, 5), (5, 6), (6, 7), (7, 8),
		(9, 10), (10, 11), (11, 12),
		(13, 14), (14, 15), (15, 16),
		(0, 17), (17, 18), (18, 19), (19, 20),
		(5, 9), (9, 13), (13, 17),
	];

	public float[]? Landmarks { get; set; }

	public Color BoneColor { get; set; } = Color.FromArgb("#5EEAD4");

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		var points = Landmarks;
		if (points is null || points.Length < 63)
		{
			canvas.FontColor = Colors.White;
			canvas.FontSize = 13;
			canvas.DrawString("No hand", dirtyRect, HorizontalAlignment.Center, VerticalAlignment.Center);
			return;
		}

		float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
		for (var i = 0; i < 21; i++)
		{
			minX = Math.Min(minX, points[i * 3]);
			maxX = Math.Max(maxX, points[i * 3]);
			minY = Math.Min(minY, points[i * 3 + 1]);
			maxY = Math.Max(maxY, points[i * 3 + 1]);
		}

		const float padding = 10f;
		var scale = Math.Min(
			(dirtyRect.Width - 2 * padding) / Math.Max(maxX - minX, 1f),
			(dirtyRect.Height - 2 * padding) / Math.Max(maxY - minY, 1f));
		var offsetX = dirtyRect.X + (dirtyRect.Width - (maxX - minX) * scale) / 2;
		var offsetY = dirtyRect.Y + (dirtyRect.Height - (maxY - minY) * scale) / 2;
		PointF Map(int i) => new(offsetX + (points[i * 3] - minX) * scale, offsetY + (points[i * 3 + 1] - minY) * scale);

		canvas.StrokeColor = BoneColor;
		canvas.StrokeSize = 3;
		canvas.StrokeLineCap = LineCap.Round;
		foreach (var (from, to) in Bones)
		{
			canvas.DrawLine(Map(from), Map(to));
		}

		canvas.FillColor = Colors.White;
		for (var i = 0; i < 21; i++)
		{
			var p = Map(i);
			canvas.FillCircle(p, i is 4 or 8 or 12 or 16 or 20 ? 4.5f : 3f);
		}
	}
}
