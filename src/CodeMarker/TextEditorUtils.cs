using Microsoft.Win32;

namespace CodeMarker;

internal class TextEditorUtils
{
    public static readonly bool IsLightTheme = GetIsLightTheme();

    private static bool GetIsLightTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"
        );

        return (key?.GetValue("AppsUseLightTheme") as int?) != 0;
    }
}
