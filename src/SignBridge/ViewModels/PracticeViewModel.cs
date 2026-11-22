using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SignBridge.Core.Learning;
using SignBridge.Core.Recognition;

namespace SignBridge.ViewModels;

public partial class PracticeViewModel : RecognitionViewModel
{
	private readonly PracticeSession _session = new(AslAlphabet.PracticeLetters, Random.Shared, confidenceThreshold: 0.5f, requiredHits: 2);
	private CancellationTokenSource? _celebration;

	public PracticeViewModel() => SyncFromSession();

	[ObservableProperty]
	public partial string Target { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Hint { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string TargetImage { get; set; } = string.Empty;

	[ObservableProperty]
	public partial int Score { get; set; }

	[ObservableProperty]
	public partial int Streak { get; set; }

	[ObservableProperty]
	public partial int BestStreak { get; set; }

	[ObservableProperty]
	public partial double HoldProgress { get; set; }

	[ObservableProperty]
	public partial bool IsCelebrating { get; set; }

	protected override void Handle(RecognitionResult result)
	{
		var previousScore = _session.Score;
		var outcome = _session.Submit(result);

		Status = outcome switch
		{
			PracticeOutcome.Matched => $"¡Correcto! +{_session.Score - previousScore} puntos",
			PracticeOutcome.InProgress => "¡Bien, mantenla!…",
			_ when !result.HandFound => "Muestra tu mano a la cámara",
			_ => $"Todavía no: compara tu mano con el dibujo de la {_session.Target}",
		};

		if (outcome == PracticeOutcome.Matched)
		{
			Vibrate();
			_ = CelebrateAsync();
		}

		SyncFromSession();
	}

	[RelayCommand]
	private void Skip()
	{
		_session.Skip();
		Status = $"Saltada. Ahora haz la letra {_session.Target}";
		SyncFromSession();
	}

	private void SyncFromSession()
	{
		Target = _session.Target;
		Hint = AslAlphabet.HintFor(_session.Target);
		TargetImage = SignImage(_session.Target);
		Score = _session.Score;
		Streak = _session.Streak;
		BestStreak = _session.BestStreak;
		HoldProgress = _session.HoldProgress;
	}

	private async Task CelebrateAsync()
	{
		_celebration?.Cancel();
		_celebration = new CancellationTokenSource();
		IsCelebrating = true;
		try
		{
			await Task.Delay(800, _celebration.Token);
			IsCelebrating = false;
		}
		catch (OperationCanceledException)
		{
			// A newer celebration took over.
		}
	}
}
