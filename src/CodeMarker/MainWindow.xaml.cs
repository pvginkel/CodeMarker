using System.Collections;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Windows.Forms;
using CodeMarker.Support;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;

namespace CodeMarker;

internal partial class MainWindow
{
    private Project? _project;
    private CodeMarkings? _file;
    private IconManager? _iconManager;

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

                var folderNode = new FolderNode(
                    name,
                    LoadFiles(basePath, Path.Combine(currentPath, name))
                );

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
}
