using SignBridge.Core.Recognition;

namespace SignBridge.Core.Learning;

/// <summary>
/// Turns a noisy stream of per-frame predictions into text: a letter is committed once it
/// stays on top with enough confidence for <c>stableFrames</c> consecutive frames.
/// Holding a sign does not repeat it; to write a double letter, drop the hand between signs.
/// </summary>
public sealed class WordBuilder(float confidenceThreshold = 0.7f, int stableFrames = 3)
{
    private readonly System.Text.StringBuilder _text = new();
    private int _count;

    public string Text => _text.ToString();

    /// <summary>The letter currently being held, or null when nothing confident is visible.</summary>
    public string? Candidate { get; private set; }

    /// <summary>How close the candidate is to being committed (0..1).</summary>
    public float CandidateProgress => Candidate is null ? 0f : Math.Min(1f, (float)_count / stableFrames);

    /// <returns>True when this frame committed a new letter.</returns>
    public bool Push(SignPrediction prediction)
    {
        if (prediction.Confidence < confidenceThreshold)
        {
            Candidate = null;
            _count = 0;
            return false;
        }

        if (prediction.Label == Candidate)
        {
            _count++;
        }
        else
        {
            Candidate = prediction.Label;
            _count = 1;
        }

        if (_count != stableFrames)
        {
            return false;
        }

        _text.Append(prediction.Label);
        return true;
    }

    public void AddSpace()
    {
        if (_text.Length > 0 && _text[^1] != ' ')
        {
            _text.Append(' ');
        }
    }

    public void Backspace()
    {
        if (_text.Length > 0)
        {
            _text.Length--;
        }
    }

    public void Clear()
    {
        _text.Clear();
        Candidate = null;
        _count = 0;
    }
}
