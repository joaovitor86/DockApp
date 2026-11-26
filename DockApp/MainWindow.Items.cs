using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DockApp.Models;
using DockApp.Services;
using DragEventArgs = System.Windows.DragEventArgs;
using DragDropEffects = System.Windows.DragDropEffects;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace DockApp;

public partial class MainWindow
{
    private readonly DispatcherTimer _runningTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private bool _checkingApps;
    private FrameworkElement? _pressedIcon;
    private Point _pressPosition;
    private DockItem? _activeDrag;
    private const string MoveFormat = "DockApp.MoveItem";
    private string? _menuItemPath;

    private void DockMenu_Opening(object sender, ContextMenuEventArgs e)
    {
        var item = (e.OriginalSource as FrameworkElement)?.DataContext as DockItem;
        _menuItemPath = item?.SourcePath;
        AddSeparatorMenu.IsEnabled = IconsPanel.ItemsSource is List<DockItem>;
        RemoveSeparatorMenu.Visibility = item is DockSeparator ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AddSeparator_Click(object sender, RoutedEventArgs e)
    {
        if (IconsPanel.ItemsSource is not List<DockItem> items) return;
        var updated = items.ToList();
        int anchor = updated.FindIndex(item => item.SourcePath == _menuItemPath);
        updated.Insert(anchor < 0 ? updated.Count : anchor + 1, DockSeparator.Create(Guid.NewGuid()));
        SaveDockOrder(updated);
    }

    private void RemoveSeparator_Click(object sender, RoutedEventArgs e)
    {
        if (IconsPanel.ItemsSource is not List<DockItem> items) return;
        var updated = items.ToList();
        if (updated.RemoveAll(item => item is DockSeparator && item.SourcePath == _menuItemPath) > 0)
            SaveDockOrder(updated);
    }

    private async void RefreshRunningApps()
    {
        if (_checkingApps || _closed || !IsVisible ||
            IconsPanel.ItemsSource is not List<DockItem> items || items.Count == 0)
            return;
        _checkingApps = true;
        try
        {
            var running = await Task.Run(() => RunningApps.Find(items));
            if (!_closed && ReferenceEquals(items, IconsPanel.ItemsSource))
                foreach (var item in items)
                    item.IsRunning = running.Contains(item);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Falha ao consultar aplicativos em execução: {ex.Message}");
        }
        finally { _checkingApps = false; }
    }

    private void Icon_Press(object sender, MouseButtonEventArgs e)
    {
        var icon = (FrameworkElement)sender;
        _pressPosition = e.GetPosition(this);
        _pressedIcon = icon.CaptureMouse() ? icon : null;
        e.Handled = true;
    }

    private void Icon_DragMove(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: DockItem item } icon ||
            !ReferenceEquals(icon, _pressedIcon) || !icon.IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed)
            return;
        var delta = e.GetPosition(this) - _pressPosition;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        _pressedIcon = null;
        icon.ReleaseMouseCapture();
        CancelPendingNameTooltip();
        CloseNameTooltip();
        _activeDrag = item;
        try
        {
            var data = new System.Windows.DataObject(MoveFormat, item.SourcePath);
            System.Windows.DragDrop.DoDragDrop(icon, data, DragDropEffects.Move);
        }
        finally { _activeDrag = null; }
    }

    private bool CanMove(DragEventArgs e) => _activeDrag != null && e.Data.GetDataPresent(MoveFormat, false);

    private static void ShowOutline(FrameworkElement icon, bool visible)
    {
        if (icon.FindName("DropOutline") is Border outline)
            outline.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Icon_DragOver(object sender, DragEventArgs e)
    {
        var icon = (FrameworkElement)sender;
        bool allowed = CanMove(e);
        e.Effects = allowed ? DragDropEffects.Move : DragDropEffects.None;
        ShowOutline(icon, allowed && !ReferenceEquals(icon.Tag, _activeDrag));
        e.Handled = true;
    }

    private void Icon_DragLeave(object sender, DragEventArgs e) => ShowOutline((FrameworkElement)sender, false);

    private void Icon_Drop(object sender, DragEventArgs e)
    {
        var icon = (FrameworkElement)sender;
        ShowOutline(icon, false);
        e.Handled = true;
        e.Effects = DragDropEffects.None;
        if (!CanMove(e) || icon.Tag is not DockItem target || IconsPanel.ItemsSource is not List<DockItem> items)
            return;
        var reordered = items.ToList();
        int from = reordered.FindIndex(item => item.SourcePath == _activeDrag!.SourcePath);
        int to = reordered.IndexOf(target);
        if (from < 0 || to < 0 || from == to) return;
        var moved = reordered[from];
        reordered.RemoveAt(from);
        reordered.Insert(to, moved);

        if (SaveDockOrder(reordered))
            e.Effects = DragDropEffects.Move;
    }

    private bool SaveDockOrder(List<DockItem> reordered)
    {
        try
        {
            var saved = ConfigurationService.Load();
            saved.ItemOrder = reordered.Select(item => item.SourcePath).ToList();
            saved.PinnedItems = DockOrder.Sort(saved.PinnedItems ?? [], saved.ItemOrder, item => item.Path);
            ConfigurationService.Save(saved);
            _settings.ItemOrder = saved.ItemOrder;
            _settings.PinnedItems = saved.PinnedItems;
            IconsPanel.ItemsSource = reordered;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            System.Windows.MessageBox.Show(this, "Não foi possível salvar a organização da dock. " + ex.Message, "DockApp");
            return false;
        }
    }
}
