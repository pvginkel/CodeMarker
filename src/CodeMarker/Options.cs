using CommandLine;

namespace CodeMarker;

internal class Options
{
    [Option("project")]
    public string? ProjectFileName { get; set; }
}
