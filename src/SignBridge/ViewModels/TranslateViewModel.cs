using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SignBridge.Core.Learning;
using SignBridge.Core.Recognition;
using SignBridge.Services;

namespace SignBridge.ViewModels;

public partial class TranslateViewModel(SpeechService speech) : RecognitionViewModel
{
	private readonly WordBuilder _builder = new(confidenceThreshold: 0.6f, stableFrames: 3);

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SpeakCommand))]
	public partial string Text { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Candidate { get; set; } = string.Empty;

	[ObservableProperty]
	public partial double CandidateProgress { get; set; }

	protected override void Handle(RecognitionResult result)
	{
		var top = result.Top;
		if (_builder.Push(top))
		{
			Vibrate();
			Status = $"Agregué la \"{top.Label}\". Baja la mano para repetir una letra.";
		}
		else if (!result.HandFound)
		{
			Status = "Muestra tu mano a la cámara";
		}
		else if (_builder.Candidate is null)
		{
			Status = "Mantén la letra quieta";
		}

		Sync();
	}

	[RelayCommand]
	private void Space()
	{
		_builder.AddSpace();
		Sync();
	}

	[RelayCommand]
	private void Backspace()
	{
		_builder.Backspace();
		Sync();
	}

	[RelayCommand]
	private void Clear()
	{
		_builder.Clear();
		Sync();
	}

	[RelayCommand(CanExecute = nameof(CanSpeak))]
	private async Task SpeakAsync()
	{
		try
		{
			await speech.SpeakAsync(Text);
		}
		catch (Exception ex)
		{
			Status = $"No se pudo leer en voz alta: {ex.Message}";
		}
	}

	private bool CanSpeak() => !string.IsNullOrWhiteSpace(Text);

	private void Sync()
	{
		Text = _builder.Text;
		Candidate = _builder.Candidate ?? string.Empty;
		CandidateProgress = _builder.CandidateProgress;
	}
}
