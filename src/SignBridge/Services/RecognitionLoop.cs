using System.Diagnostics;
using CommunityToolkit.Maui.Views;
using SignBridge.Core.Recognition;

namespace SignBridge.Services;

/// <summary>What a screen needs to consume live recognition results.</summary>
public interface IRecognitionConsumer
{
	void OnRecognized(RecognitionResult result);

	void OnStatus(string message);
}

/// <summary>
/// Captures a camera frame every <see cref="Interval"/>, runs the recognizer off the UI thread
/// and hands the result back on the UI thread. One frame is in flight at a time.
/// </summary>
public sealed class RecognitionLoop
{
	public static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(350);

	private CancellationTokenSource? _cts;

	public void Start(CameraView camera, ISignRecognizer recognizer, IRecognitionConsumer consumer)
	{
		Stop();
		_cts = new CancellationTokenSource();
		_ = RunAsync(camera, recognizer, consumer, _cts.Token);
	}

	public void Stop()
	{
		_cts?.Cancel();
		_cts?.Dispose();
		_cts = null;
	}

	private static async Task RunAsync(CameraView camera, ISignRecognizer recognizer, IRecognitionConsumer consumer, CancellationToken token)
	{
		while (!token.IsCancellationRequested)
		{
			var started = Stopwatch.GetTimestamp();
			try
			{
				using var frame = new MemoryStream();
				await using (var captured = await camera.CaptureImage(token))
				{
					await captured.CopyToAsync(frame, token);
				}

				frame.Position = 0;
				var result = await Task.Run(() => recognizer.Recognize(frame), token);
				consumer.OnRecognized(result);
			}
			catch (OperationCanceledException) when (token.IsCancellationRequested)
			{
				return;
			}
			catch (Exception ex)
			{
				consumer.OnStatus($"Se saltó un cuadro de la cámara: {ex.Message}");
				if (!await DelayAsync(TimeSpan.FromSeconds(1), token))
				{
					return;
				}
			}

			var remaining = Interval - Stopwatch.GetElapsedTime(started);
			if (remaining > TimeSpan.Zero && !await DelayAsync(remaining, token))
			{
				return;
			}
		}
	}

	private static async Task<bool> DelayAsync(TimeSpan delay, CancellationToken token)
	{
		try
		{
			await Task.Delay(delay, token);
			return true;
		}
		catch (OperationCanceledException)
		{
			return false;
		}
	}
}
