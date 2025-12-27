using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DialogueHelper.FileFormat;
using DialogueHelper.StylesParser;

namespace DialogueHelper.External;

public partial class CustomToolWindowHandler(MainWindow mainWindow, StyleParser style)
{
    readonly MainWindow _mainWindow = mainWindow;
    public readonly StyleParser Style = style;

    public IStorageProvider GetStorageProvider() => _mainWindow.StorageProvider;

    public void Show(Window window) => window.Show(_mainWindow);

    public async Task ShowDialog(Window window) => await window.ShowDialog(_mainWindow);

    public async Task<T> ShowDialog<T>(Window window) => await window.ShowDialog<T>(_mainWindow);

    public string GetStylePath() => Style.Folder;

    public FileData? GetLoadedFileData() => _mainWindow.FileData;

    public void LoadFileToProject(string file) => _mainWindow.LoadFileToProject(file);

    public void LoadStringToProject(string str) => _mainWindow.LoadStringToProject(str);
}
