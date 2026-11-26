namespace DockApp.Models;

public sealed record DockProfile(string Name, string Start, string End, string Border, string Accent,
    HoverEffect Hover, LaunchEffect? Launch, double IdleOpacity, bool Gradient = true)
{
    public static IReadOnlyList<DockProfile> All { get; } =
    [
        new("Grafite", "#22262C", "#16181B", "#51565E", "#CFD6E0", HoverEffect.Zoom, LaunchEffect.Bounce, 1, false),
        new("Aurora", "#142F46", "#332153", "#596995", "#68E4ED", HoverEffect.ZoomAndGlow, LaunchEffect.Pulse, 0.85),
        new("Âmbar", "#42291E", "#211D22", "#886044", "#FFB85C", HoverEffect.Wave, LaunchEffect.Tilt, 0.90),
        new("Oceano", "#10374C", "#09202D", "#38798F", "#71E5FF", HoverEffect.Wave, LaunchEffect.Pulse, 0.85),
        new("Floresta", "#173C30", "#192B26", "#486F59", "#ACE7AD", HoverEffect.Glow, LaunchEffect.Pulse, 0.80),
        new("Cerejeira", "#542E46", "#322239", "#94617D", "#FFB2D3", HoverEffect.Glow, LaunchEffect.Tilt, 0.90),
        new("Neon", "#271739", "#122F41", "#8655CA", "#52FFF1", HoverEffect.ZoomAndGlow, LaunchEffect.Bounce, 1),
        new("Cobre", "#4F3026", "#2C2022", "#9A6553", "#FFAE89", HoverEffect.Zoom, LaunchEffect.Tilt, 0.95),
        new("Glacial", "#364957", "#253341", "#8094A8", "#D4F1FF", HoverEffect.Glow, null, 0.85),
        new("Vinho", "#491F34", "#251725", "#83516A", "#F19AB6", HoverEffect.Zoom, LaunchEffect.Pulse, 0.90),
        new("Eclipse", "#14151A", "#14151A", "#343740", "#B4B9C7", HoverEffect.None, null, 0.65, false),
        new("Solar", "#59371A", "#3C2336", "#AD8048", "#FFD676", HoverEffect.Wave, LaunchEffect.Bounce, 1)
    ];

    public string SwatchEnd => Gradient ? End : Start;

    public string MotionDescription
    {
        get
        {
            var hover = Hover switch
            {
                HoverEffect.Zoom => "Ampliação",
                HoverEffect.Wave => "Onda nos vizinhos",
                HoverEffect.Glow => "Brilho",
                HoverEffect.ZoomAndGlow => "Ampliação e brilho",
                _ => "Sem efeito de mouse"
            };
            var launch = Launch switch
            {
                LaunchEffect.Bounce => "salto",
                LaunchEffect.Pulse => "pulso",
                LaunchEffect.Tilt => "inclinação",
                _ => "sem animação ao abrir"
            };
            return $"{hover} · {launch}";
        }
    }
}
