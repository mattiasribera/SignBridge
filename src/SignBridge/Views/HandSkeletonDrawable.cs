namespace SignBridge.Views;

/// <summary>
/// Draws the 21 detected hand points and their bones. It shows what the model actually "sees":
/// the shape of the hand, without background or skin. As a thumbnail it fits the hand to the
/// view; as an overlay it places each point where it is in the camera preview.
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

	/// <summary>Draw on top of the camera preview instead of as a fitted thumbnail.</summary>
	public bool IsOverlay { get; set; }

	/// <summary>Size of the analyzed frame, needed to map points onto the preview.</summary>
	public SizeF FrameSize { get; set; }

	/// <summary>Flip horizontally, for previews shown mirrored (Android front camera).</summary>
	public bool Mirror { get; set; }

	public Color BoneColor { get; set; } = Color.FromArgb("#FFB81C");

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		var points = Landmarks;
		if (points is null || points.Length < 63)
		{
			if (!IsOverlay)
			{
				canvas.FontColor = Colors.White;
				canvas.FontSize = 13;
				canvas.DrawString("Sin mano", dirtyRect, HorizontalAlignment.Center, VerticalAlignment.Center);
			}

			return;
		}

		var map = IsOverlay ? OverlayMap(points, dirtyRect) : ThumbnailMap(points, dirtyRect);
		if (map is null)
		{
			return;
		}

		canvas.StrokeColor = BoneColor;
		canvas.StrokeSize = IsOverlay ? 5 : 3;
		canvas.StrokeLineCap = LineCap.Round;
		foreach (var (from, to) in Bones)
		{
			canvas.DrawLine(map(from), map(to));
		}

		canvas.FillColor = Colors.White;
		var tip = IsOverlay ? 7f : 4.5f;
		var joint = IsOverlay ? 4.5f : 3f;
		for (var i = 0; i < 21; i++)
		{
			canvas.FillCircle(map(i), i is 4 or 8 or 12 or 16 or 20 ? tip : joint);
		}
	}

	private static Func<int, PointF> ThumbnailMap(float[] points, RectF dirtyRect)
	{
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
		return i => new(offsetX + (points[i * 3] - minX) * scale, offsetY + (points[i * 3 + 1] - minY) * scale);
	}

	/// <summary>
	/// Maps frame pixels to the view the way the preview shows the frame: scaled to fit, centered
	/// (both the Windows and the Android camera previews use aspect-fit).
	/// </summary>
	private Func<int, PointF>? OverlayMap(float[] points, RectF dirtyRect)
	{
		if (FrameSize.Width <= 0 || FrameSize.Height <= 0)
		{
			return null;
		}

		var scale = Math.Min(dirtyRect.Width / FrameSize.Width, dirtyRect.Height / FrameSize.Height);
		var offsetX = dirtyRect.X + (dirtyRect.Width - FrameSize.Width * scale) / 2;
		var offsetY = dirtyRect.Y + (dirtyRect.Height - FrameSize.Height * scale) / 2;
		var mirror = Mirror;
		var width = FrameSize.Width;
		return i =>
		{
			var x = mirror ? width - points[i * 3] : points[i * 3];
			return new PointF(offsetX + x * scale, offsetY + points[i * 3 + 1] * scale);
		};
	}
}
