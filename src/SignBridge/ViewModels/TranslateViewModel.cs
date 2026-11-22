using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SignBridge.Core.Learning;
using SignBridge.Core.Recognition;
using SignBridge.Services;

namespace SignBridge.ViewModels;

public partial class TranslateViewModel(SpeechService speech) : RecognitionViewModel
{
	private readonly WordBuilder _builder = new(confidenceThreshold: 0.7f, stableFrames: 3);

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SpeakCommand))]
	public partial string Text { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Candidate { get; set; } = string.Empty;

	[ObservableProperty]
	public partial double CandidateProgress { get; set; }

	protected override void Handle(SignPrediction top)
	{
		if (_builder.Push(top))
		{
			Vibrate();
			Status = $"Added \"{top.Label}\". Lower your hand to repeat a letter.";
		}
		else if (_builder.Candidate is null)
		{
			Status = "Hold a letter steady inside the frame";
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
			Status = $"Text-to-speech failed: {ex.Message}";
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
