using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace CodeMarker;

internal class CodeMarkingsColorizer(CodeMarkings markings) : DocumentColorizingTransformer
{
    protected override void ColorizeLine(DocumentLine line)
    {
        var lineNumber = line.LineNumber - 1;
        if (lineNumber < markings.LineColors.Length)
        {
            var color = markings.LineColors[lineNumber];
            if (color != null)
            {
                ChangeLinePart(
                    line.Offset,
                    line.EndOffset,
                    p =>
                        p.TextRunProperties.SetBackgroundBrush(
                            new SolidColorBrush(
                                Color.FromArgb(0x40, color.Color.R, color.Color.G, color.Color.B)
                            )
                        )
                );
            }
        }
    }
}
