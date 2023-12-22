using CodeMarker.Themes;

namespace CodeMarker;

internal partial class App
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        SetTheme();
    }

    private void SetTheme()
    {
        ThemesController.SetTheme(
            TextEditorUtils.IsLightTheme ? ThemeType.LightTheme : ThemeType.SoftDark
        );

        if (!TextEditorUtils.IsLightTheme)
            HL.Manager.ThemedHighlightingManager.Instance.SetCurrentTheme("VS2019_Dark");
    }
}
