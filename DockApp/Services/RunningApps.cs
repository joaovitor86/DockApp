using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using DockApp.Models;
using Microsoft.Win32;

namespace DockApp.Services;

internal static class RunningApps
{
    public static (string? Executable, uint? SteamId) Identify(string source)
    {
        try
        {
            string target = source;
            string arguments = "";
            if (Path.IsPathFullyQualified(source))
            {
                switch (Path.GetExtension(source).ToLowerInvariant())
                {
                    case ".lnk":
                        (target, arguments) = ReadShortcut(source);
                        break;
                    case ".url":
                        target = File.ReadLines(source)
                            .FirstOrDefault(line => line.StartsWith("URL=", StringComparison.OrdinalIgnoreCase))?[4..].Trim() ?? "";
                        break;
                    case ".exe": break;
                    default: return default;
                }
            }
            if (Uri.TryCreate(target, UriKind.Absolute, out var uri) && uri.Scheme == "steam" &&
                uri.Host is "run" or "rungameid" &&
                uint.TryParse(uri.AbsolutePath.Trim('/').Split('/')[0], out var game))
                return (null, game);

            if (!Path.IsPathFullyQualified(target) || !target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return default;
            if (Path.GetFileName(target).Equals("steam.exe", StringComparison.OrdinalIgnoreCase))
            {
                var parts = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                int index = Array.FindIndex(parts, part => part.Equals("-applaunch", StringComparison.OrdinalIgnoreCase));
                if (index >= 0 && index + 1 < parts.Length && uint.TryParse(parts[index + 1], out game))
                    return (null, game);
            }
            return (Path.GetFullPath(target), null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or COMException or ArgumentException
            or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
        {
            Debug.WriteLine(ex.Message);
            return default;
        }
    }

    private static (string, string) ReadShortcut(string source)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null) return ("", "");
        dynamic shell = Activator.CreateInstance(shellType)!;
        try
        {
            dynamic shortcut = shell.CreateShortcut(source);
            try { return ((string)shortcut.TargetPath, (string)shortcut.Arguments); }
            finally { Marshal.ReleaseComObject(shortcut); }
        }
        finally { Marshal.ReleaseComObject(shell); }
    }

    public static HashSet<DockItem> Find(IReadOnlyList<DockItem> items)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var names = items.Where(item => item.ExecutablePath != null)
            .Select(item => Path.GetFileNameWithoutExtension(item.ExecutablePath!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        using var current = Process.GetCurrentProcess();
        foreach (var process in names.Count > 0 ? Process.GetProcesses() : Array.Empty<Process>())
        {
            using (process)
            {
                try
                {
                    if (names.Contains(process.ProcessName) && process.SessionId == current.SessionId &&
                        process.MainModule?.FileName is string path)
                        paths.Add(path);
                }
                catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or NotSupportedException)
                {
                    // O processo pode ter terminado ou estar protegido.
                }
            }
        }

        var running = items.Where(item => item.ExecutablePath != null && paths.Contains(item.ExecutablePath)).ToHashSet();
        foreach (var group in items.Where(item => item.SteamAppId.HasValue).GroupBy(item => item.SteamAppId!.Value))
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam\Apps\" + group.Key.ToString(CultureInfo.InvariantCulture));
                if (key?.GetValue("Running") is 1)
                    running.UnionWith(group);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        return running;
    }
}
