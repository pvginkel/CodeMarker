using System.IO;
using CodeMarker.Support;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;

namespace CodeMarker;

internal partial class MainWindow
{
    private Project? _project;
    private FileNode? _file;
    private IconManager? _iconManager;

    public MainWindow()
    {
        InitializeComponent();

        _editor.TextArea.TextView.Options.EnableEmailHyperlinks = false;
        _editor.TextArea.TextView.Options.EnableHyperlinks = false;

        _openProjectImage.Source = Images.OpenFolder;
        _closeProjectImage.Source = Images.DismissSquare;

        _markRedImage.Source = Images.MarkRed;
        _markYellowImage.Source = Images.MarkYellow;
        _markGreenImage.Source = Images.MarkGreen;

        UpdateEnabled();
    }

    private void BaseWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var dpi = VisualTreeHelper.GetDpi(this);

        _iconManager = new IconManager((int)(16 * dpi.DpiScaleX), (int)dpi.PixelsPerInchX);
        _iconManager.GetIcon(".cs", [MarkColor.Red]);
        if (App.Options.ProjectFileName != null)
            OpenProject(App.Options.ProjectFileName);
    }

    private void UpdateEnabled()
    {
        _closeProject.IsEnabled = _project != null;

        _markGreen.IsEnabled = _file != null;
        _markYellow.IsEnabled = _file != null;
        _markRed.IsEnabled = _file != null;
    }

    private void _openProject_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog();

        dialog.DefaultExt = ".codemarker";
        dialog.Filter = "Code Marker Project (*.codemarker)|*.codemarker|All Files (*.*)|*.*";

        if (dialog.ShowDialog() != true)
            return;

        var projectFileName = dialog.FileName;

        OpenProject(projectFileName);
    }

    private void OpenProject(string projectFileName)
    {
        CloseProject();

        _project = new Project(projectFileName);

        LoadTreeView();

        UpdateEnabled();
    }

    private void LoadTreeView()
    {
        if (_project == null)
            return;

        _files.ItemsSource = LoadFiles(Path.GetDirectoryName(_project.FileName)!, "");

        List<Node> LoadFiles(string basePath, string currentPath)
        {
            var fullPath = Path.Combine(basePath, currentPath);
            var nodes = new List<Node>();

            foreach (var path in Directory.GetDirectories(fullPath))
            {
                if (File.GetAttributes(path).HasFlag(FileAttributes.Hidden))
                    continue;

                var name = Path.GetFileName(path);

                var children = LoadFiles(basePath, Path.Combine(currentPath, name))
                    .ToImmutableArray();
                if (children.Length == 0)
                    continue;

                var folderNode = new FolderNode(name, children);

                foreach (var node in folderNode.Children)
                {
                    node.Parent = folderNode;
                }

                nodes.Add(folderNode);

                var colors = folderNode.Children.SelectMany(p => p.MarkColors).ToHashSet();

                folderNode.Icon = _iconManager!.GetFolderIcon(colors);
                folderNode.MarkColors = ImmutableArray.Create(colors.ToArray());
            }

            foreach (var path in Directory.GetFiles(fullPath))
            {
                if (File.GetAttributes(path).HasFlag(FileAttributes.Hidden))
                    continue;

                var name = Path.GetFileName(path);

                var codeMarkings = CodeMarkings.FromProjectItem(path);
                if (codeMarkings == null)
                    continue;

                var fileNode = new FileNode(name, codeMarkings)
                {
                    Icon = _iconManager!.GetIcon(Path.GetExtension(name), codeMarkings.MarkColors),
                    MarkColors = codeMarkings.MarkColors.ToImmutableArray()
                };

                nodes.Add(fileNode);
            }

            return nodes;
        }
    }

    private void CloseProject()
    {
        if (_project == null)
            return;

        _project = null;

        _files.Items.Clear();
        _file = null;

        UpdateEnabled();
    }

    private void _closeProject_Click(object sender, RoutedEventArgs e) => CloseProject();

    private void _files_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        _file = e.NewValue as FileNode;

        _editor.TextArea.TextView.LineTransformers.Clear();
        _editor.Text = null;

        if (_file != null)
        {
            _editor.SyntaxHighlighting =
                HL.Manager.ThemedHighlightingManager.Instance.GetDefinitionByExtension(
                    Path.GetExtension(_file.Markings.FileName)
                );

            _editor.Text = _file.Markings.Text;
            _editor.TextArea.TextView.LineTransformers.Add(
                new CodeMarkingsColorizer(_file.Markings)
            );
        }

        UpdateEnabled();
    }

    private void _editor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.D1 when Keyboard.Modifiers.HasFlag(ModifierKeys.Control):
                Mark(MarkColor.Green);
                e.Handled = true;
                break;
            case Key.D2 when Keyboard.Modifiers.HasFlag(ModifierKeys.Control):
                Mark(MarkColor.Yellow);
                e.Handled = true;
                break;
            case Key.D3 when Keyboard.Modifiers.HasFlag(ModifierKeys.Control):
                Mark(MarkColor.Red);
                e.Handled = true;
                break;
        }
    }

    private void Mark(MarkColor color)
    {
        if (_file == null)
            return;

        var selection = _editor.TextArea.Selection;
        int startLine;
        int endLine;

        if (selection.IsEmpty)
        {
            startLine = endLine = _editor.TextArea.Caret.Line - 1;
        }
        else
        {
            startLine = selection.StartPosition.Line - 1;
            endLine = selection.EndPosition.Line - 1;
        }

        if (startLine > endLine)
            (startLine, endLine) = (endLine, startLine);

        for (var line = startLine; line <= endLine; line++)
        {
            if (line >= _file.Markings.LineColors.Length || _file.Markings.LineColors[line] == null)
                continue;

            _file.Markings.LineColors[line] = color;
        }

        _file.Markings.Save();

        _file.MarkColors = _file.Markings.UpdatePrevalentColor();
        _file.Icon = _iconManager!.GetIcon(Path.GetExtension(_file.Name), _file.MarkColors);

        for (var node = _file.Parent; node != null; node = node.Parent)
        {
            var colors = node.Children.SelectMany(p => p.MarkColors).Distinct().ToImmutableArray();

            node.MarkColors = colors;
            node.Icon = _iconManager!.GetFolderIcon(node.MarkColors);
        }

        _editor.TextArea.TextView.Redraw();
    }

    private void _markGreen_Click(object sender, RoutedEventArgs e) => Mark(MarkColor.Green);

    private void _markYellow_Click(object sender, RoutedEventArgs e) => Mark(MarkColor.Yellow);

    private void _markRed_Click(object sender, RoutedEventArgs e) => Mark(MarkColor.Red);
}
