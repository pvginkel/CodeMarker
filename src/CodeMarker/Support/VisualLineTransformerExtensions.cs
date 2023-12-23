using ICSharpCode.AvalonEdit.Rendering;

namespace CodeMarker.Support;

internal static class VisualLineTransformerExtensions
{
    public static void RemoveByType<T>(this IList<IVisualLineTransformer> self)
        where T : IVisualLineTransformer
    {
        foreach (var item in self.OfType<T>().ToList())
        {
            self.Remove(item);
        }
    }
}
