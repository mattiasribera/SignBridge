namespace SignBridge.Services;

/// <summary>Offline text-to-speech using the device engine, preferring a Spanish voice.</summary>
public sealed class SpeechService
{
	private Locale? _locale;
	private bool _localeResolved;

	public async Task SpeakAsync(string text, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		if (!_localeResolved)
		{
			var locales = await TextToSpeech.Default.GetLocalesAsync();
			_locale = locales.FirstOrDefault(l => l.Language.StartsWith("es", StringComparison.OrdinalIgnoreCase));
			_localeResolved = true;
		}

		// Lower case so "HOLA" is read as a word instead of being spelled out as an acronym.
		var options = new SpeechOptions { Locale = _locale, Volume = 1f, Pitch = 1f };
		await TextToSpeech.Default.SpeakAsync(text.Trim().ToLowerInvariant(), options, cancellationToken);
	}
}
