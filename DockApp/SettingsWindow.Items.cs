using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using DockApp.Models;
using Microsoft.Win32;

namespace DockApp;

public partial class SettingsWindow
{
    private readonly ObservableCollection<PinnedItem> _items = [];

    private void LoadItems()
    {
        _items.Clear();
        foreach (var item in _settings.PinnedItems ?? [])
            if (item != null)
                _items.Add(item with { });
        ItemsGrid.ItemsSource = _items;
        AlwaysOnTop.IsChecked = _settings.AlwaysOnTop;
        ShowNames.IsChecked = _settings.ShowNames;
        EdgeDistance.Value = _settings.EdgeDistance;
    }

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Selecione programas, jogos, atalhos ou documentos",
            Multiselect = true,
            DereferenceLinks = false,
            Filter = "Todos os arquivos|*.*"
        };
        if (dialog.ShowDialog(this) == true)
            foreach (var path in dialog.FileNames)
                Add(path);
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog(this) == true)
            Add(dialog.FolderName);
    }

    private void Grid_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
            ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void Grid_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] paths)
            foreach (var path in paths)
                Add(path);
        e.Handled = true;
    }

    private void AddLink_Click(object sender, RoutedEventArgs e)
    {
        var path = LinkInput.Text.Trim();
        if (!Uri.TryCreate(path, UriKind.Absolute, out var uri) || uri.IsFile)
        {
            System.Windows.MessageBox.Show(this, "Informe um endereço completo, como https:// ou steam://.", Title);
            return;
        }
        Add(path);
        LinkInput.Clear();
    }

    private void Add(string path)
    {
        bool isFilePath = Path.IsPathFullyQualified(path);
        var comparison = isFilePath ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var item = _items.FirstOrDefault(existing => string.Equals(existing.Path, path, comparison));
        if (item == null)
        {
            var name = isFilePath ? Path.GetFileNameWithoutExtension(path) : path;
            item = new PinnedItem { Path = path, Name = string.IsNullOrEmpty(name) ? path : name };
            _items.Add(item);
        }
        ItemsGrid.SelectedItem = item;
        ItemsGrid.ScrollIntoView(item);
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e) => Move(-1);
    private void MoveDown_Click(object sender, RoutedEventArgs e) => Move(1);

    private void Move(int delta)
    {
        var index = ItemsGrid.SelectedIndex;
        var next = index + delta;
        if (index >= 0 && next >= 0 && next < _items.Count)
            _items.Move(index, next);
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsGrid.SelectedItem is PinnedItem item)
            _items.Remove(item);
    }

    private void SetIcon_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsGrid.SelectedItem is not PinnedItem item)
            return;
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Imagens|*.png;*.ico;*.jpg;*.jpeg;*.bmp" };
        if (dialog.ShowDialog(this) == true)
            item.IconPath = dialog.FileName;
    }

    private void ClearIcon_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsGrid.SelectedItem is PinnedItem item)
            item.IconPath = null;
    }

    private void ApplyItems()
    {
        ItemsGrid.CommitEdit();
        _settings.PinnedItems = [.. _items];
        _settings.EdgeDistance = EdgeDistance.Value;
        _settings.AlwaysOnTop = AlwaysOnTop.IsChecked == true;
        _settings.ShowNames = ShowNames.IsChecked == true;

    }
}
