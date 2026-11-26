using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;

namespace DockApp.Models;

public class DockItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public string? ExecutablePath { get; init; }
    public uint? SteamAppId { get; init; }
    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (_isRunning == value) return;
            _isRunning = value;
            PropertyChanged?.Invoke(this, new(nameof(IsRunning)));
        }
    }
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

public sealed class DockSeparator : DockItem
{
    private const string Prefix = ":separator:";

    internal static DockSeparator Create(Guid id) =>
        new() { Name = "Separador", SourcePath = Prefix + id.ToString("N") };

    internal static bool TryReadId(string? key, out Guid id)
    {
        id = default;
        return key != null && key.StartsWith(Prefix, StringComparison.Ordinal) &&
               Guid.TryParseExact(key.AsSpan(Prefix.Length), "N", out id);
    }
}
