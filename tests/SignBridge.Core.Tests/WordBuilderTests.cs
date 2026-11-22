using SignBridge.Core.Learning;
using SignBridge.Core.Recognition;

namespace SignBridge.Core.Tests;

public class WordBuilderTests
{
    private static SignPrediction P(string label, float confidence = 0.95f) => new(label, confidence);

    [Fact]
    public void Commits_a_letter_after_it_is_stable_for_the_required_frames()
    {
        var builder = new WordBuilder(confidenceThreshold: 0.7f, stableFrames: 3);

        Assert.False(builder.Push(P("H")));
        Assert.False(builder.Push(P("H")));
        Assert.True(builder.Push(P("H")));

        Assert.Equal("H", builder.Text);
    }

    [Fact]
    public void Holding_the_same_letter_does_not_repeat_it()
    {
        var builder = new WordBuilder(confidenceThreshold: 0.7f, stableFrames: 2);

        for (var i = 0; i < 10; i++)
        {
            builder.Push(P("A"));
        }

        Assert.Equal("A", builder.Text);
    }

    [Fact]
    public void A_double_letter_needs_the_hand_to_drop_between_signs()
    {
        var builder = new WordBuilder(confidenceThreshold: 0.7f, stableFrames: 2);

        builder.Push(P("L"));
        builder.Push(P("L"));
        builder.Push(P("L", 0.2f));
        builder.Push(P("L"));
        builder.Push(P("L"));

        Assert.Equal("LL", builder.Text);
    }

    [Fact]
    public void Low_confidence_or_changing_frames_reset_the_progress()
    {
        var builder = new WordBuilder(confidenceThreshold: 0.7f, stableFrames: 3);

        builder.Push(P("O"));
        builder.Push(P("O"));
        builder.Push(P("O", 0.3f));
        builder.Push(P("O"));
        builder.Push(P("C"));

        Assert.Equal(string.Empty, builder.Text);
        Assert.Equal("C", builder.Candidate);
        Assert.Equal(1f / 3f, builder.CandidateProgress, 3);
    }

    [Fact]
    public void Space_backspace_and_clear_edit_the_text()
    {
        var builder = new WordBuilder(confidenceThreshold: 0.7f, stableFrames: 1);

        builder.AddSpace();
        builder.Push(P("H"));
        builder.Push(P("I", 0.1f));
        builder.Push(P("I"));
        builder.AddSpace();
        builder.AddSpace();

        Assert.Equal("HI ", builder.Text);

        builder.Backspace();
        builder.Backspace();
        Assert.Equal("H", builder.Text);

        builder.Clear();
        Assert.Equal(string.Empty, builder.Text);
        builder.Backspace();
        Assert.Equal(string.Empty, builder.Text);
    }
}
