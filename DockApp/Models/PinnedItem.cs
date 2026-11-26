namespace DockApp.Models;

public sealed record PinnedItem
{
    public string Path { get; init; } = "";
    public string Name { get; set; } = "";
    public string? IconPath { get; set; }
}
