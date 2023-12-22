namespace CodeMarker;

internal partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        _editor.SyntaxHighlighting =
            HL.Manager.ThemedHighlightingManager.Instance.GetDefinitionByExtension(".cs");
    }
}
