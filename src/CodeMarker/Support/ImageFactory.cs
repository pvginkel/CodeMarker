using System.IO;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using Brush = System.Windows.Media.Brush;

namespace CodeMarker.Support;

public static class ImageFactory
{
    public static BitmapImage CreateBitmapImage(Stream stream)
    {
        var memoryStream = default(MemoryStream);

        // BitmapImage requires that the stream can be seekable in order
        // to be able to freeze the bitmap.

        if (!stream.CanSeek)
        {
            memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;
        }

        var bitmapImage = new BitmapImage();

        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = memoryStream ?? stream;
        bitmapImage.EndInit();

        bitmapImage.Freeze();

        memoryStream?.Dispose();

        return bitmapImage;
    }

    public static DrawingImage CreateSvgImage(
        Stream stream,
        Brush? fill = null,
        Brush? stroke = null
    )
    {
        var settings = new WpfDrawingSettings
        {
            IncludeRuntime = true,
            TextAsGeometry = false,
            OptimizePath = true
        };

        using var reader = new FileSvgReader(settings);

        var drawingGroup = reader.Read(stream);
        if (drawingGroup != null)
        {
            if (fill != null || stroke != null)
                OverrideBrushes(drawingGroup, fill, stroke);

            var drawing = new DrawingImage(drawingGroup);

            drawing.Freeze();

            return drawing;
        }

        throw new InvalidOperationException("Could not convert SVG image");
    }

    private static void OverrideBrushes(DrawingGroup group, Brush? fill, Brush? stroke)
    {
        foreach (var child in group.Children)
        {
            switch (child)
            {
                case DrawingGroup drawingGroup:
                    OverrideBrushes(drawingGroup, fill, stroke);
                    break;

                case GeometryDrawing geometryDrawing:
                    if (fill != null && geometryDrawing.Brush != null)
                        geometryDrawing.Brush = fill;
                    if (stroke != null && geometryDrawing.Pen != null)
                        geometryDrawing.Pen.Brush = stroke;
                    break;
            }
        }
    }

    public static ImageSource FromEmbeddedResource(Type relativeTo, string name)
    {
        var resourceName = $"{relativeTo.Namespace}.{name}";
        using var stream = relativeTo.Assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
            throw new ArgumentException($"Cannot find resource '{resourceName}'");

        if (name.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            return CreateSvgImage(stream);
        return CreateBitmapImage(stream);
    }
}
