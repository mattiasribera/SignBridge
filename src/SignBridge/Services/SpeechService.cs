namespace SignBridge.Services;

/// <summary>Offline text-to-speech using the device engine, preferring a Latin American Spanish voice.</summary>
public sealed class SpeechService
{
	// Bolivia first, then Latin America, Mexico and the US; any other Spanish voice after that.
	private static readonly string[] PreferredCountries = ["BO", "419", "MX", "US"];

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
			var spanish = locales.Where(l => l.Language.StartsWith("es", StringComparison.OrdinalIgnoreCase)).ToList();
			_locale = PreferredCountries
				.Select(country => spanish.FirstOrDefault(l => string.Equals(l.Country, country, StringComparison.OrdinalIgnoreCase)
					|| l.Language.EndsWith(country, StringComparison.OrdinalIgnoreCase)))
				.FirstOrDefault(l => l is not null)
				?? spanish.FirstOrDefault();
			_localeResolved = true;
		}

		// Lower case so "HOLA" is read as a word instead of being spelled out as an acronym.
		var options = new SpeechOptions { Locale = _locale, Volume = 1f, Pitch = 1f };
		await TextToSpeech.Default.SpeakAsync(text.Trim().ToLowerInvariant(), options, cancellationToken);
	}
}
