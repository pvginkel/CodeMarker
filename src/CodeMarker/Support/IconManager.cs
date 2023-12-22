using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Path = System.IO.Path;

namespace CodeMarker.Support;

internal class IconManager
{
    private readonly Dictionary<(string Extension, ImageSource Overlay), ImageSource> _cache =
        new();
    private readonly Dictionary<string, ImageSource?> _systemIconCache = new();

    private readonly int _resolution;
    private readonly int _dpi;
    private readonly ImageSource _genericFolderIcon;
    private readonly ImageSource _genericFileIcon;

    public IconManager(int resolution, int dpi)
    {
        _resolution = resolution;
        _dpi = dpi;

        _genericFolderIcon = GetSystemIcon(
            Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar),
            FILE_ATTRIBUTE_DIRECTORY,
            0
        )!;
        _genericFileIcon = GetSystemIcon(Path.GetTempFileName(), 0, 0)!;
    }

    public ImageSource GetFolderIcon(IEnumerable<MarkColor> color)
    {
        var key = (
            Extension: "(folder)",
            Overlay: MarkColor.GetOverlay(color) ?? MarkColor.GreenOverlay
        );

        if (!_cache.TryGetValue(key, out var image))
        {
            image = ImageFactory.RenderWithOverlays(
                _resolution,
                96,
                _genericFolderIcon,
                key.Overlay
            );
            _cache.Add(key, image);
        }

        return image;
    }

    public ImageSource GetIcon(string extension, IEnumerable<MarkColor> color)
    {
        var key = (
            Extension: extension.ToLowerInvariant(),
            Overlay: MarkColor.GetOverlay(color) ?? MarkColor.GreenOverlay
        );

        if (!_cache.TryGetValue(key, out var image))
        {
            var icon =
                GetSystemIcon($"dummy{key.Extension}", 0, SHGFI_USEFILEATTRIBUTES)
                ?? _genericFileIcon;

            image = ImageFactory.RenderWithOverlays(_resolution, 96, icon, key.Overlay);
            _cache.Add(key, image);
        }

        return image;
    }

    private ImageSource? GetSystemIcon(string fileName, uint attributes, uint flags)
    {
        if (!_systemIconCache.TryGetValue(fileName, out var icon))
        {
            var info = new SHFILEINFO();

            var result = SHGetFileInfo(
                fileName,
                attributes,
                ref info,
                (uint)Marshal.SizeOf(info),
                flags | SHGFI_ICON | (_dpi > 96 ? SHGFI_LARGEICON : SHGFI_SMALLICON)
            );

            if (result != 0 && info.hIcon != 0)
            {
                icon = Imaging.CreateBitmapSourceFromHIcon(
                    info.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                );
            }

            _systemIconCache.Add(fileName, icon);
        }

        return icon;
    }

    [DllImport("shell32")]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags
    );

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    private const uint SHGFI_LARGEICON = 0x0;
    private const uint SHGFI_SMALLICON = 0x1;
    private const uint SHGFI_OPENICON = 0x2;
    private const uint SHGFI_SHELLICONSIZE = 0x4;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
    private const uint SHGFI_ADDOVERLAYS = 0x20;
    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_SYSICONINDEX = 0x4000;

    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
}
