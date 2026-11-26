using System.IO;
using DockApp.Models;

namespace DockApp.Services;

internal static class DockOrder
{
    private static string Key(string path) => Path.IsPathFullyQualified(path) ? path.ToUpperInvariant() : path;

    public static List<T> Sort<T>(IEnumerable<T> items, IEnumerable<string>? order, Func<T, string> path)
    {
        var ranks = new Dictionary<string, int>();
        foreach (var entry in order ?? [])
            if (!string.IsNullOrEmpty(entry))
                ranks.TryAdd(Key(entry), ranks.Count);
        return items.OrderBy(item => ranks.GetValueOrDefault(Key(path(item)), int.MaxValue)).ToList();
    }

    public static List<DockItem> Arrange(List<DockItem> items, DockSettings settings)
    {
        var separators = new List<DockSeparator>();
        var seen = new HashSet<Guid>();
        foreach (var key in settings.ItemOrder ?? [])
            if (DockSeparator.TryReadId(key, out var id) && seen.Add(id))
                separators.Add(DockSeparator.Create(id));
        var sorted = Sort(items.Concat(separators), settings.ItemOrder, item => item.SourcePath);
        // A grade de configurações controla a ordem relativa dos fixados,
        // inclusive quando há atalhos da pasta intercalados entre eles.
        var pins = (settings.PinnedItems ?? []).Where(pin => pin != null && !string.IsNullOrEmpty(pin.Path)).ToList();
        var keys = pins.Select(pin => Key(pin.Path)).ToHashSet();
        var pinnedSlots = sorted.Where(item => keys.Contains(Key(item.SourcePath)));
        var queue = new Queue<DockItem>(Sort(pinnedSlots, pins.Select(pin => pin.Path), item => item.SourcePath));
        return sorted.Select(item => keys.Contains(Key(item.SourcePath)) ? queue.Dequeue() : item).ToList();
    }
}
