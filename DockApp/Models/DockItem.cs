using System.Diagnostics;
using System.IO;
using System.Windows.Media;

namespace DockApp.Models;

public sealed class DockItem
{
    public required string Name { get; init; }
    public required string SourcePath { get; init; }
    public ImageSource? Icon { get; init; }

    public void Open()
    {
        // O Shell interpreta o .lnk original, inclusive argumentos e "Iniciar em".
        var launch = new ProcessStartInfo(SourcePath) { UseShellExecute = true };
        if (Path.IsPathFullyQualified(SourcePath))
            launch.WorkingDirectory = Path.GetDirectoryName(SourcePath) ?? SourcePath;

        using var process = Process.Start(launch);
    }
}
