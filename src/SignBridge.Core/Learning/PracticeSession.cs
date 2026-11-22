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
/// Each consecutive match adds a streak bonus. Targets are dealt from a shuffled deck so every
/// letter is practiced before any repeats.
/// </summary>
public sealed class PracticeSession
{
    public const int PointsPerMatch = 10;
    public const int StreakBonus = 2;

    private readonly IReadOnlyList<string> _letters;
    private readonly Random _random;
    private readonly float _threshold;
    private readonly int _requiredHits;
    private readonly Queue<string> _deck = new();
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
        Target = DrawFromDeck(previous: null);
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
        Target = DrawFromDeck(Target);
    }

    /// <summary>
    /// Deals letters like a shuffled deck: every letter comes up once before any repeats, and a
    /// new deck never starts with the letter that was just asked.
    /// </summary>
    private string DrawFromDeck(string? previous)
    {
        if (_deck.Count == 0)
        {
            var shuffled = _letters.ToArray();
            _random.Shuffle(shuffled);
            if (shuffled[0] == previous)
            {
                (shuffled[0], shuffled[^1]) = (shuffled[^1], shuffled[0]);
            }

            foreach (var letter in shuffled)
            {
                _deck.Enqueue(letter);
            }
        }

        return _deck.Dequeue();
    }
}
