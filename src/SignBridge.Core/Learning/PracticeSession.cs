using SignBridge.Core.Recognition;

namespace SignBridge.Core.Learning;

public enum PracticeOutcome
{
    /// <summary>The frame shows something other than the target, or the model is unsure.</summary>
    NoMatch,

    /// <summary>The target is visible; keep holding it.</summary>
    InProgress,

    /// <summary>The target was held long enough: points awarded, new target chosen.</summary>
    Matched,
}

/// <summary>
/// Practice game: show a target letter, the learner signs it, and points are awarded when the
/// recognizer sees it with enough confidence for <c>requiredHits</c> consecutive frames.
/// Each consecutive match adds a streak bonus.
/// </summary>
public sealed class PracticeSession
{
    public const int PointsPerMatch = 10;
    public const int StreakBonus = 2;

    private readonly IReadOnlyList<string> _letters;
    private readonly Random _random;
    private readonly float _threshold;
    private readonly int _requiredHits;
    private int _hits;

    public PracticeSession(IReadOnlyList<string> letters, Random random, float confidenceThreshold = 0.6f, int requiredHits = 2)
    {
        if (letters.Count < 2)
        {
            throw new ArgumentException("Practice needs at least two letters.", nameof(letters));
        }

        _letters = letters;
        _random = random;
        _threshold = confidenceThreshold;
        _requiredHits = requiredHits;
        Target = _letters[_random.Next(_letters.Count)];
    }

    public string Target { get; private set; }

    public int Score { get; private set; }

    public int Streak { get; private set; }

    public int BestStreak { get; private set; }

    /// <summary>How long the target has been held, relative to what is required (0..1).</summary>
    public float HoldProgress => (float)_hits / _requiredHits;

    public PracticeOutcome Submit(SignPrediction prediction) => Submit(new RecognitionResult([prediction]));

    /// <summary>
    /// Judges a frame by the probability the recognizer gives to the target letter, so a sign
    /// the model finds likely counts even when another letter is marginally ahead.
    /// </summary>
    public PracticeOutcome Submit(RecognitionResult result)
    {
        var targetProbability = result.Ranked.FirstOrDefault(p => p.Label == Target)?.Confidence ?? 0f;
        if (targetProbability < _threshold)
        {
            _hits = 0;
            return PracticeOutcome.NoMatch;
        }

        if (++_hits < _requiredHits)
        {
            return PracticeOutcome.InProgress;
        }

        Score += PointsPerMatch + StreakBonus * Streak;
        Streak++;
        BestStreak = Math.Max(BestStreak, Streak);
        NextTarget();
        return PracticeOutcome.Matched;
    }

    public void Skip()
    {
        Streak = 0;
        NextTarget();
    }

    private void NextTarget()
    {
        _hits = 0;
        var previous = Target;
        do
        {
            Target = _letters[_random.Next(_letters.Count)];
        }
        while (Target == previous);
    }
}
