using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DockApp.Models;

namespace DockApp.Services;

public sealed class ShortcutLoader
{
    private readonly record struct IconKey(string Path, string? Image, DateTime SourceTime, DateTime ImageTime);
    private Dictionary<IconKey, ImageSource> _icons = [];

    public Task<List<DockItem>> LoadAsync(DockSettings settings)
    {
        var completion = new TaskCompletionSource<List<DockItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var worker = new Thread(() =>
        {
            try { completion.SetResult(LoadItems(settings)); }
            catch (Exception error) { completion.SetException(error); }
        }) { IsBackground = true, Name = "DockApp - ícones do Shell" };
        worker.SetApartmentState(ApartmentState.STA);
        worker.Start();
        return completion.Task;
    }

    private List<DockItem> LoadItems(DockSettings settings)
    {
        var nextIcons = new Dictionary<IconKey, ImageSource>();
        ImageSource? GetIcon(string path, string? image = null)
        {
            var key = new IconKey(path, image, LastWrite(path), LastWrite(image));
            if (!_icons.TryGetValue(key, out var icon))
                icon = ReadImage(image) ?? ReadShellIcon(path);
            if (icon != null)
                nextIcons[key] = icon;
            return icon;
        }

        var items = new List<DockItem>();
        var pinnedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pin in settings.PinnedItems ?? [])
        {
            if (string.IsNullOrWhiteSpace(pin?.Path))
                continue;

            pinnedPaths.Add(pin.Path);
            items.Add(new DockItem
            {
                Name = string.IsNullOrWhiteSpace(pin.Name) ? pin.Path : pin.Name,
                SourcePath = pin.Path,
                Icon = GetIcon(pin.Path, pin.IconPath)
            });
        }

        try
        {
            if (Directory.Exists(settings.FolderPath))
            {
                foreach (var file in Directory.EnumerateFiles(settings.FolderPath)
                             .Order(StringComparer.CurrentCultureIgnoreCase))
                {
                    var extension = Path.GetExtension(file);
                    if (pinnedPaths.Contains(file) ||
                        !(extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase) ||
                          extension.Equals(".url", StringComparison.OrdinalIgnoreCase)))
                        continue;

                    items.Add(new DockItem
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        SourcePath = file,
                        Icon = GetIcon(file)
                    });
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine(ex.Message);
        }
        _icons = nextIcons;
        return items;
    }

    private static DateTime LastWrite(string? path)
    {
        if (string.IsNullOrEmpty(path) || !Path.IsPathFullyQualified(path))
            return default;
        try { return File.GetLastWriteTimeUtc(path); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return default; }
    }

    private static ImageSource? ReadImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            using var stream = File.OpenRead(path);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = 192;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return null;
        }
    }

    private static ImageSource? ReadShellIcon(string path)
    {
        if (!Path.IsPathFullyQualified(path))
            return null;

        // A lista do Shell guarda o ícone base e as sobreposições separadamente.
        const uint systemIconIndex = 0x4000;
        var list = SHGetFileInfo(path, 0, out var info, (uint)Marshal.SizeOf<ShellFileInfo>(), systemIconIndex);
        if (list == IntPtr.Zero)
            return null;

        const uint transparent = 1;
        var handle = ImageList_GetIcon(list, info.IconIndex, transparent);
        if (handle == IntPtr.Zero)
            return null;
        try
        {
            var icon = Imaging.CreateBitmapSourceFromHIcon(handle, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            icon.Freeze();
            return icon;
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShellFileInfo
    {
        public IntPtr Icon;
        public int IconIndex;
        public uint Attributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string DisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string TypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(string path, uint attributes,
        out ShellFileInfo info, uint size, uint flags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr icon);

    [DllImport("comctl32.dll")]
    private static extern IntPtr ImageList_GetIcon(IntPtr list, int index, uint flags);
}
