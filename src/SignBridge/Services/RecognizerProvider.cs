using SignBridge.Core.Recognition;

namespace SignBridge.Services;

/// <summary>
/// Loads the ONNX models from the app package once and shares the recognizer between pages.
/// This is the only place that knows which concrete <see cref="ISignRecognizer"/> is used.
/// </summary>
public sealed class RecognizerProvider : IDisposable
{
	public const string PalmModelFile = "palm_detection.onnx";
	public const string HandModelFile = "hand_landmarks.onnx";
	public const string ClassifierModelFile = "asl_landmark_mlp.onnx";
	public const string LabelsFile = "asl_landmark_labels.json";

	private readonly Lazy<Task<ISignRecognizer>> _recognizer = new(LoadAsync);

	public Task<ISignRecognizer> GetAsync() => _recognizer.Value;

	private static async Task<ISignRecognizer> LoadAsync()
	{
		var palm = await ReadBytesAsync(PalmModelFile);
		var hand = await ReadBytesAsync(HandModelFile);
		var classifier = await ReadBytesAsync(ClassifierModelFile);
		var labels = System.Text.Encoding.UTF8.GetString(await ReadBytesAsync(LabelsFile));

		return await Task.Run(() => LandmarkSignRecognizer.Create(palm, hand, classifier, labels));
	}

	private static async Task<byte[]> ReadBytesAsync(string file)
	{
		await using var stream = await FileSystem.OpenAppPackageFileAsync(file);
		using var buffer = new MemoryStream();
		await stream.CopyToAsync(buffer);
		return buffer.ToArray();
	}

	public void Dispose()
	{
		if (_recognizer.IsValueCreated && _recognizer.Value.IsCompletedSuccessfully)
		{
			_recognizer.Value.Result.Dispose();
		}
	}
}
