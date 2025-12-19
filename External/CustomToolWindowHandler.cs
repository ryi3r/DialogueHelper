using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using DialogueHelper.FileFormat;
using DialogueHelper.StylesParser;
using Lua;

namespace DialogueHelper.External;

[LuaObject]
public partial class CustomToolWindowHandler(MainWindow mainWindow, StyleParser style)
{
    readonly MainWindow _mainWindow = mainWindow;
    [LuaMember("style")]
    public readonly StyleParser Style = style;

    [LuaMember("open_file_picker")]
    public async Task<LuaTable> OpenFilePicker(string? title, bool AllowMultiple, LuaValue filters)
    {
        IEnumerable<LuaStorageFile> files = (await _mainWindow.StorageProvider.OpenFilePickerAsync(new()
        {
            Title = title,
            AllowMultiple = AllowMultiple,
            FileTypeFilter = [.. filters.Read<LuaTable>().Select(x =>
            {
                var s = x.Value.Read<string>().Split('|');
                return new FilePickerFileType(s[0])
                {
                    Patterns = s[1].Split(';'),
                };
            })],
        })).Select(x => new LuaStorageFile(x));
        var t = new LuaTable();
        foreach (var (i, v) in files.Index())
            t.Insert(i + 1, v);
        return t;
    }

    [LuaMember("open_folder_picker")]
    public async Task<LuaTable> OpenFolderPicker(string? title, bool AllowMultiple)
    {
        IEnumerable<LuaStorageFolder> folders = (await _mainWindow.StorageProvider.OpenFolderPickerAsync(new()
        {
            Title = title,
            AllowMultiple = AllowMultiple,
        })).Select(x => new LuaStorageFolder(x));
        var t = new LuaTable();
        foreach (var (i, v) in folders.Index())
            t.Insert(i + 1, v);
        return t;
    }

    [LuaMember("save_file_picker")]
    public async Task<LuaStorageFile?> SaveFilePicker(string? title, string? suggestedFileName, int suggestedFilterIndex, LuaValue filtersLua)
    {
        var filters = filtersLua.Read<LuaTable>();
        var convFilters = filters.Select(x =>
            {
                var s = x.Value.Read<string>().Split('|');
                return new FilePickerFileType(s[0])
                {
                    Patterns = s[1].Split(';'),
                };
            }).ToArray();
        var res = await _mainWindow.StorageProvider.SaveFilePickerAsync(new()
        {
            Title = title,
            FileTypeChoices = convFilters,
            SuggestedFileType = suggestedFilterIndex - 1 < filters.ArrayLength && suggestedFilterIndex > 0 ? convFilters[suggestedFilterIndex - 1] : null,
            SuggestedFileName = suggestedFileName,
        });
        if (res == null)
            return null;
        return new LuaStorageFile(res);
    }

    [LuaMember("show")]
    public void Show(LuaWindow window) => window.Show(_mainWindow);

    [LuaMember("show_dialog")]
    public async Task ShowDialog(LuaWindow window) => await window.ShowDialog(_mainWindow);

    [LuaMember("show_dialog_get_value")]
    public async Task<LuaValue> ShowDialogGetLuaValue(LuaWindow window) => await window.ShowDialog<LuaValue>(_mainWindow);

    public async Task<T> ShowDialog<T>(LuaWindow window) => await window.ShowDialog<T>(_mainWindow);

    [LuaMember("get_style_path")]
    public string GetStylePath() => Style.Folder;

    [LuaMember("get_loaded_file_data")]
    public FileData? GetLoadedFileData() => _mainWindow.FileData;

    [LuaMember("load_file_to_project")]
    public void LoadFileToProject(string file)
    {
        _mainWindow.LoadFileToProject(file);
    }

    [LuaMember("load_string_to_project")]
    public void LoadStringToProject(string str)
    {
        _mainWindow.LoadStringToProject(str);
    }
}
