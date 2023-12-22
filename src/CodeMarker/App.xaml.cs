using CodeMarker.Themes;
using Microsoft.Win32;

namespace CodeMarker;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        SetTheme();
    }

    private void SetTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"
        );

        var isLight = (key?.GetValue("AppsUseLightTheme") as int?) != 0;

        ThemesController.SetTheme(isLight ? ThemeType.LightTheme : ThemeType.SoftDark);
    }
}
