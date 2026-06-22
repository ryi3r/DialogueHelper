using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DialogueHelper.FileFormat;
using DialogueHelper.StylesParser;
using DialogueHelper.Views;

namespace DialogueHelper.External;

public class CustomToolWindowHandler(MainWindow mainWindow, StyleParser style)
{
    readonly MainWindow _mainWindow = mainWindow;
    readonly StyleParser _style = style;

    public IStorageProvider GetStorageProvider() => _mainWindow.StorageProvider;

    public void Show(Window window) => window.Show(_mainWindow);

    public async Task ShowDialog(Window window) => await window.ShowDialog(_mainWindow);

    public async Task<T> ShowDialog<T>(Window window) => await window.ShowDialog<T>(_mainWindow);

    public string GetStylePath() => _style.Folder;

    public FileData? GetLoadedFileData() => _mainWindow.FileData;

    public void LoadFile(string? path = null, IStorageFile? file = null, bool markAsModified = false) => _mainWindow.LoadFileToProject(path, file, markAsModified);

    public void LoadString(string str, LoadingWindow? lWin = null, bool markAsModified = true) => _mainWindow.LoadStringToProject(str, lWin, markAsModified);

    public Task<bool> EnsureFileOpen(bool showMessage = true) => _mainWindow.EnsureFileOpen(showMessage);

    public async Task<bool> EnsureFileClosed(bool showMessage = true) => !await _mainWindow.EnsureFileOpen(false, showMessage);
}
