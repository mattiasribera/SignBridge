using SignBridge.Core.Learning;

namespace SignBridge.Core.Tests;

public class AslAlphabetTests
{
    [Fact]
    public void Practice_letters_exclude_the_motion_letters_J_and_Z()
    {
        Assert.Equal(24, AslAlphabet.PracticeLetters.Count);
        Assert.DoesNotContain("J", AslAlphabet.PracticeLetters);
        Assert.DoesNotContain("Z", AslAlphabet.PracticeLetters);
    }

    [Fact]
    public void Every_letter_has_a_hint()
    {
        foreach (var letter in AslAlphabet.Letters)
        {
            Assert.False(string.IsNullOrWhiteSpace(AslAlphabet.HintFor(letter)), letter);
        }

        Assert.Equal(26, AslAlphabet.Letters.Count);
        Assert.Equal(string.Empty, AslAlphabet.HintFor("?"));
    }
}
