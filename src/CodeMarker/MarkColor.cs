namespace CodeMarker;

internal class MarkColor
{
    public static readonly MarkColor Red = new MarkColor(
        Color.FromRgb(255, 200, 200),
        "RED",
        Images.MarkRed,
        3
    );
    public static readonly MarkColor Yellow = new MarkColor(
        Color.FromRgb(255, 233, 127),
        "YELLOW",
        Images.MarkYellow,
        2
    );
    public static readonly MarkColor Green = new MarkColor(
        Color.FromRgb(200, 255, 200),
        "GREEN",
        Images.MarkGreen,
        1
    );
    public static readonly ImageSource RedOverlay = Images.OverlayRed;
    public static readonly ImageSource YellowOverlay = Images.OverlayYellow;
    public static readonly ImageSource GreenOverlay = Images.OverlayGreen;
    public static readonly ImageSource RedYellowOverlay = Images.OverlayRedYellow;
    public static readonly ImageSource RedGreenOverlay = Images.OverlayRedGreen;
    public static readonly ImageSource YellowGreenOverlay = Images.OverlayYellowGreen;

    public static ImageSource? GetOverlay(IEnumerable<MarkColor> colors)
    {
        bool haveRed = false;
        bool haveYellow = false;
        bool haveGreen = false;

        foreach (var color in colors)
        {
            if (color == Red)
                haveRed = true;
            else if (color == Yellow)
                haveYellow = true;
            else if (color == Green)
                haveGreen = true;
        }

        if (haveRed)
        {
            if (haveYellow)
                return RedYellowOverlay;
            else if (haveGreen)
                return RedGreenOverlay;
            else
                return RedOverlay;
        }
        else if (haveYellow)
        {
            if (haveGreen)
                return YellowGreenOverlay;
            else
                return YellowOverlay;
        }
        else if (haveGreen)
        {
            return GreenOverlay;
        }
        else
        {
            return null;
        }
    }

    public Color Color { get; private set; }
    public string Name { get; private set; }
    public ImageSource Image { get; private set; }
    public int Priority { get; private set; }

    private MarkColor(Color color, string name, ImageSource image, int priority)
    {
        Color = color;
        Name = name;
        Image = image;
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
