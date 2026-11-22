namespace SignBridge.Core.Learning;

/// <summary>ASL fingerspelling alphabet with short descriptions of each handshape.</summary>
public static class AslAlphabet
{
    private static readonly Dictionary<string, string> Hints = new()
    {
        ["A"] = "Make a fist with the thumb resting against the side of the index finger.",
        ["B"] = "Flat hand, fingers together pointing up, thumb folded across the palm.",
        ["C"] = "Curve the fingers and thumb into the shape of a C.",
        ["D"] = "Index finger up; the other fingers curve to touch the thumb.",
        ["E"] = "Bend all fingertips down over the thumb, which is tucked across the palm.",
        ["F"] = "Touch the index tip to the thumb; the other three fingers stay up and apart.",
        ["G"] = "Index finger and thumb point sideways, parallel, like a small gap.",
        ["H"] = "Index and middle fingers together, pointing sideways.",
        ["I"] = "Little finger up, the other fingers in a fist.",
        ["J"] = "Little finger up, then draw a J in the air.",
        ["K"] = "Index and middle fingers up in a V, thumb touching the middle finger.",
        ["L"] = "Index finger up and thumb out, forming an L.",
        ["M"] = "Tuck the thumb under the first three fingers.",
        ["N"] = "Tuck the thumb under the first two fingers.",
        ["O"] = "Curve all fingers to meet the thumb, forming an O.",
        ["P"] = "Like K, but with the hand pointing down.",
        ["Q"] = "Like G, but with the hand pointing down.",
        ["R"] = "Cross the middle finger over the index finger.",
        ["S"] = "Make a fist with the thumb across the front of the fingers.",
        ["T"] = "Tuck the thumb between the index and middle fingers.",
        ["U"] = "Index and middle fingers up, together.",
        ["V"] = "Index and middle fingers up, spread apart in a V.",
        ["W"] = "Index, middle and ring fingers up and spread.",
        ["X"] = "Index finger bent like a hook, other fingers in a fist.",
        ["Y"] = "Thumb and little finger out, the other fingers folded.",
        ["Z"] = "Point with the index finger and draw a Z in the air.",
    };

    public static IReadOnlyList<string> Letters { get; } = Hints.Keys.Order().ToArray();

    /// <summary>J and Z need motion, which a single-frame classifier cannot judge reliably.</summary>
    public static IReadOnlyList<string> PracticeLetters { get; } = Letters.Where(l => l is not ("J" or "Z")).ToArray();

    public static string HintFor(string letter) => Hints.GetValueOrDefault(letter, string.Empty);
}
