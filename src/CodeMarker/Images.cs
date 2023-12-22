using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Resources;
using CodeMarker.Support;

namespace CodeMarker;

internal static class Images
{
    private static readonly ConcurrentDictionary<
        (string ResourceName, Color? Color),
        DrawingImage
    > LoadedImages = new();

    public static DrawingImage GetImage(string resourceName, Color? color = null)
    {
        return LoadedImages.GetOrAdd(
            (resourceName, color),
            p => LoadImage(p.ResourceName, p.Color)
        );
    }

    private static DrawingImage LoadImage(string resourceName, Color? color)
    {
        using var stream = Application
            .GetResourceStream(
                new Uri(
                    $"pack://application:,,,/CodeMarker;component/Resources/{resourceName.Replace("+", " ")}"
                )
            )!
            .Stream;

        var fill = default(Brush);
        if (color.HasValue)
            fill = new SolidColorBrush(color.Value);

        return ImageFactory.CreateSvgImage(stream!, fill, null);
    }

    private static Color? GetDefaultImageColor() =>
        TextEditorUtils.IsLightTheme ? Colors.Black : Colors.White;

    public static readonly ImageSource DismissSquare = GetImage(
        "Dismiss Square.svg",
        GetDefaultImageColor()
    );
    public static readonly ImageSource OpenFolder = GetImage(
        "Open Folder.svg",
        GetDefaultImageColor()
    );
    public static readonly ImageSource MarkRed = GetImage("Mark Red.svg");
    public static readonly ImageSource MarkGreen = GetImage("Mark Green.svg");
    public static readonly ImageSource MarkYellow = GetImage("Mark Yellow.svg");
    public static readonly ImageSource OverlayRed = GetImage("Overlay Red.svg");
    public static readonly ImageSource OverlayRedGreen = GetImage("Overlay Red Green.svg");
    public static readonly ImageSource OverlayRedYellow = GetImage("Overlay Red Yellow.svg");
    public static readonly ImageSource OverlayYellow = GetImage("Overlay Yellow.svg");
    public static readonly ImageSource OverlayYellowGreen = GetImage("Overlay Yellow Green.svg");
    public static readonly ImageSource OverlayGreen = GetImage("Overlay Green.svg");
}
