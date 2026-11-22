using SignBridge.Core.Recognition;

namespace SignBridge.Services;

/// <summary>
/// Loads the ONNX model from the app package once and shares it between pages.
/// This is the only place that knows which concrete <see cref="ISignRecognizer"/> is used.
/// </summary>
public sealed class RecognizerProvider : IDisposable
{
	public const string ModelFile = "asl_efficientnet_b0.onnx";
	public const string LabelsFile = "asl_labels.json";

	private readonly Lazy<Task<ISignRecognizer>> _recognizer = new(LoadAsync);

	public Task<ISignRecognizer> GetAsync() => _recognizer.Value;

	private static async Task<ISignRecognizer> LoadAsync()
	{
		byte[] model;
		await using (var modelStream = await FileSystem.OpenAppPackageFileAsync(ModelFile))
		using (var buffer = new MemoryStream())
		{
			await modelStream.CopyToAsync(buffer);
			model = buffer.ToArray();
		}

		string labels;
		await using (var labelsStream = await FileSystem.OpenAppPackageFileAsync(LabelsFile))
		using (var reader = new StreamReader(labelsStream))
		{
			labels = await reader.ReadToEndAsync();
		}

		return await Task.Run(() => OnnxSignRecognizer.Create(model, labels));
	}

	public void Dispose()
	{
		if (_recognizer.IsValueCreated && _recognizer.Value.IsCompletedSuccessfully)
		{
			_recognizer.Value.Result.Dispose();
		}
	}
}
