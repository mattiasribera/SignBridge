namespace SignBridge.Core.Learning;

/// <summary>ASL fingerspelling alphabet with a short Spanish description of each handshape (shown in the UI).</summary>
public static class AslAlphabet
{
    private static readonly Dictionary<string, string> Hints = new()
    {
        ["A"] = "Puño cerrado con el pulgar apoyado al costado del índice.",
        ["B"] = "Mano plana, dedos juntos hacia arriba y el pulgar doblado sobre la palma.",
        ["C"] = "Curva los dedos y el pulgar formando una C.",
        ["D"] = "Índice hacia arriba; los demás dedos se curvan hasta tocar el pulgar.",
        ["E"] = "Dobla las puntas de los dedos sobre el pulgar, que queda cruzado sobre la palma.",
        ["F"] = "La punta del índice toca el pulgar; los otros tres dedos quedan arriba y separados.",
        ["G"] = "Índice y pulgar apuntan hacia el costado, paralelos, dejando un pequeño espacio.",
        ["H"] = "Índice y medio juntos, apuntando hacia el costado.",
        ["I"] = "Meñique hacia arriba y los demás dedos en puño.",
        ["J"] = "Meñique hacia arriba y dibuja una J en el aire.",
        ["K"] = "Índice y medio arriba en V, con el pulgar tocando el dedo medio.",
        ["L"] = "Índice hacia arriba y pulgar hacia afuera, formando una L.",
        ["M"] = "Esconde el pulgar debajo de los tres primeros dedos.",
        ["N"] = "Esconde el pulgar debajo de los dos primeros dedos.",
        ["O"] = "Curva todos los dedos hasta juntarlos con el pulgar, formando una O.",
        ["P"] = "Como la K, pero con la mano apuntando hacia abajo.",
        ["Q"] = "Como la G, pero con la mano apuntando hacia abajo.",
        ["R"] = "Cruza el dedo medio sobre el índice.",
        ["S"] = "Puño cerrado con el pulgar cruzado por delante de los dedos.",
        ["T"] = "Mete el pulgar entre el índice y el medio.",
        ["U"] = "Índice y medio hacia arriba, juntos.",
        ["V"] = "Índice y medio hacia arriba, separados en V.",
        ["W"] = "Índice, medio y anular hacia arriba y separados.",
        ["X"] = "Índice doblado como un gancho y los demás dedos en puño.",
        ["Y"] = "Pulgar y meñique hacia afuera, los demás dedos doblados.",
        ["Z"] = "Apunta con el índice y dibuja una Z en el aire.",
    };

    public static IReadOnlyList<string> Letters { get; } = Hints.Keys.Order().ToArray();

    /// <summary>J and Z need motion, which a single-frame classifier cannot judge reliably.</summary>
    public static IReadOnlyList<string> PracticeLetters { get; } = Letters.Where(l => l is not ("J" or "Z")).ToArray();

    public static string HintFor(string letter) => Hints.GetValueOrDefault(letter, string.Empty);
}
