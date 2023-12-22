using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CodeMarker;

internal record Node(string Name)
{
    private ImageSource? _icon;

    public ImageSource? Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            OnPropertyChanged();
        }
    }

    public ImmutableArray<MarkColor> MarkColors { get; set; } = ImmutableArray<MarkColor>.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal record FolderNode(string Name, List<Node> Children) : Node(Name);

internal record FileNode(string Name, CodeMarkings Markings) : Node(Name);
