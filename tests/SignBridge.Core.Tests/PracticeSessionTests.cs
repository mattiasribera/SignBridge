using SignBridge.Core.Learning;
using SignBridge.Core.Recognition;

namespace SignBridge.Core.Tests;

public class PracticeSessionTests
{
    private static readonly string[] Letters = ["A", "B", "C"];

    private static PracticeSession NewSession(int requiredHits = 2) =>
        new(Letters, new Random(42), confidenceThreshold: 0.6f, requiredHits: requiredHits);

    [Fact]
    public void Starts_with_a_target_from_the_given_letters_and_zero_score()
    {
        var session = NewSession();

        Assert.Contains(session.Target, Letters);
        Assert.Equal(0, session.Score);
        Assert.Equal(0, session.Streak);
    }

    [Fact]
    public void Matching_the_target_for_the_required_frames_scores_and_advances()
    {
        var session = NewSession();
        var target = session.Target;

        Assert.Equal(PracticeOutcome.InProgress, session.Submit(new SignPrediction(target, 0.9f)));
        Assert.Equal(PracticeOutcome.Matched, session.Submit(new SignPrediction(target, 0.9f)));

        Assert.Equal(10, session.Score);
        Assert.Equal(1, session.Streak);
        Assert.NotEqual(target, session.Target);
    }

    [Fact]
    public void Streak_adds_a_bonus_to_each_consecutive_match()
    {
        var session = NewSession(requiredHits: 1);

        session.Submit(new SignPrediction(session.Target, 0.9f));
        session.Submit(new SignPrediction(session.Target, 0.9f));
        session.Submit(new SignPrediction(session.Target, 0.9f));

        Assert.Equal(10 + 12 + 14, session.Score);
        Assert.Equal(3, session.Streak);
        Assert.Equal(3, session.BestStreak);
    }

    [Fact]
    public void Wrong_or_unsure_frames_do_not_count_and_reset_the_hold()
    {
        var session = NewSession();
        var target = session.Target;
        var other = Letters.First(l => l != target);

        Assert.Equal(PracticeOutcome.InProgress, session.Submit(new SignPrediction(target, 0.9f)));
        Assert.Equal(PracticeOutcome.NoMatch, session.Submit(new SignPrediction(other, 0.9f)));
        Assert.Equal(PracticeOutcome.InProgress, session.Submit(new SignPrediction(target, 0.9f)));
        Assert.Equal(PracticeOutcome.NoMatch, session.Submit(new SignPrediction(target, 0.3f)));

        Assert.Equal(0, session.Score);
        Assert.Equal(target, session.Target);
    }

    [Fact]
    public void Skip_resets_the_streak_and_changes_the_target()
    {
        var session = NewSession(requiredHits: 1);
        session.Submit(new SignPrediction(session.Target, 0.9f));
        var target = session.Target;

        session.Skip();

        Assert.Equal(0, session.Streak);
        Assert.Equal(1, session.BestStreak);
        Assert.NotEqual(target, session.Target);
    }
}
