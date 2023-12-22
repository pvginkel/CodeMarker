namespace CodeMarker;

internal partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        _openProjectImage.Source = Images.OpenFolder;
        _closeProjectImage.Source = Images.DismissSquare;

        _markRedImage.Source = Images.MarkRed;
        _markYellowImage.Source = Images.MarkYellow;
        _markGreenImage.Source = Images.MarkGreen;

        _editor.SyntaxHighlighting =
            HL.Manager.ThemedHighlightingManager.Instance.GetDefinitionByExtension(".cs");
    }
}
