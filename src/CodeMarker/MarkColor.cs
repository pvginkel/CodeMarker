namespace CodeMarker;

internal class MarkColor
{
    public static readonly MarkColor Red = new MarkColor(Color.FromRgb(255, 200, 200), "RED", 3);
    public static readonly MarkColor Yellow = new MarkColor(
        Color.FromRgb(255, 233, 127),
        "YELLOW",
        2
    );
    public static readonly MarkColor Green = new MarkColor(
        Color.FromRgb(200, 255, 200),
        "GREEN",
        1
    );

    public Color Color { get; private set; }
    public string Name { get; private set; }
    public int Priority { get; private set; }

    private MarkColor(Color color, string name, int priority)
    {
        Color = color;
        Name = name;
        Priority = priority;
    }

    public static MarkColor GetColor(string? marking)
    {
        switch (marking)
        {
            case "GREEN":
                return Green;
            case "YELLOW":
                return Yellow;
            default:
                if (marking == null)
                    return Red;
                throw new ArgumentOutOfRangeException(nameof(marking));
        }
    }

    public override string ToString()
    {
        return Name;
    }
}
