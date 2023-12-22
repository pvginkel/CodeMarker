using CodeMarker.Themes;
using CommandLine;

namespace CodeMarker;

internal partial class App
{
    internal static Options Options { get; private set; } = new();

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        Parser.Default.ParseArguments<Options>(e.Args).WithParsed(p => Options = p);

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
