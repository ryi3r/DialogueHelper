//css_reference System.Diagnostics.Process
//css_reference System.Console
//css_reference System.IO.Pipelines
//css_reference Microsoft.CSharp
//css_reference System.IO.Compression.ZipFile

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using DialogueHelper.External;
using DialogueHelper.FileFormat;
using DialogueHelper.PreviewRenderer;
using DialogueHelper.StylesParser;
using DialogueHelper.Views;

public class Deltarune : StyleScript
{
    public enum Fonts
    {
        Main,
        Small,
        ComicSans,
        Dotumche,
        TinyNoelle,
        TennaBoard,
        JaMain,
        JaSmall,
        JaComicSans,
        JaDotumche,
        JaKakugo,
        JaTinyNoelle,
    }
    public enum DialogueKinds
    {
        Default,
        None,
        DynamicWordWrap,
    }
    public enum PortraitKinds
    {
        Default,
        None,
        Basic,
    }

    public enum WarningKind
    {
        NotFoundKey,
        DuplicatedKey,
        MissingKey,
    }

    public static CustomProperty<bool> AutoIndentAutoWrappedMiniFaceLines = new("Auto Indent Auto Wrapped Mini Face Lines", "Allows not using manual line breaks with mini face (\\m?) lines in case your mod has it implemented.", "autoIndentAutoWrappedMiniFaceLines");
    public static CustomPropertyList<string> PortraitKind = new("Portrait Kind", "Portrait type, sets the text offset as each specify", "portraitKind", ["Default", "None", "Basic"], [ "Auto-detect portrait type", "Force no portrait", "Force default portrait" ], null, 0);
    public static CustomProperty<bool> DarkZone = new("Dark Zone (JP Only)", null, "darkZone");
    public static CustomPropertyList<string> DialogueType = new("Dialogue Type", "Line-wrapping style", "dialogueType", ["Default", "None", "Dynamic"], [null, null, "This is vastly the legacy line-wrapping system, this might be useful for certain custom boxes."], null, 0);

    public static double EnvX;
    public static double EnvY;
    public static double EnvDynLimitWrap;
    public static int EnvSkip;
    public static bool EnvFirstDrawnChar;
    public static int EnvCheckedIndex;
    public static Stopwatch Stopwatch = Stopwatch.StartNew();
    public static bool IsRainbow;
    public static bool IsLastNewline;
    public ParsedString Parsed = new();

    public static (double X, double Y) EnvPortraitOffset;

    public static bool EnvDrawAsterisk;

    public static char[] Newlines = ['\n', '&', '#'];

    protected static string UtmtPath = null!;

    public override Task Init(CustomToolWindowHandler wh)
    {
        // This is called when the script is loaded and ready to go.
        //await wh.ShowDialog(new InfoWindow("Hello world!"));
        UtmtPath = $"{wh.GetStylePath()}/UndertaleModCli/";
        return Task.CompletedTask;
    }
    
    public override Task<IEnumerable<ICustomProperty>> RegisterCustomSettings()
    {
        return Task.FromResult<IEnumerable<ICustomProperty>>([
            AutoIndentAutoWrappedMiniFaceLines
        ]);
    }

    public override Task<IEnumerable<ICustomProperty>> RegisterCustomProperties()
    {
        // Store these somewhere if you're going to use them
        return Task.FromResult<IEnumerable<ICustomProperty>>([
            PortraitKind,
            DarkZone,
            DialogueType,
        ]);
    }

    public override Task<IEnumerable<CustomTool>> RegisterCustomTools()
    {
        return Task.FromResult<IEnumerable<CustomTool>>([
            new("Import from data.win", null, ImportFromDataWin, "Ctrl+Shift+I"),
            new("Export to data.win", null, ExportToDataWin, "Ctrl+Shift+E"),
            new("Import from json file", null, ImportFromJson, "Ctrl+Alt+I"),
            new("Export to json file", null, ExportToJson, "Ctrl+Alt+E"),
            new("Verify dialogues integrity", null, VerifyFileDialogueIntegrity, "Ctrl+Shift+P"),
            new("Update Dialogues", "Update your old .dh file to contain changes from newer game versions.", UpdateDialogues, "Ctrl+Shift+U"),
            new("Assign String Keys to all Entries", "Update your old .dh translation file to contain all keys.", AssignStringKeyToAll),
            new("Reorganize Dialogues", "Reorder your old .dh file dialogue order to ensure accuracy with the base json.", ReorderDialogues),
        ]);
    }

    public async Task<bool> DownloadUndertaleModToolCli(CustomToolWindowHandler wh)
    {
        var folderPath = UtmtPath;
        var filePath = folderPath + $"UndertaleModCli{(OperatingSystem.IsWindows() ? ".exe" : "")}";
        if (!File.Exists(filePath))
        {
            if (!await wh.ShowDialog<bool>(new QuestionWindow("Do you wish to automatically download UndertaleModTool's Cli on the folder?")))
                return false;
            var pg = new LoadingWindow { Text = { Text = "Downloading..." }, ProgressBar = { Value = 0 }, };
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.UserAgent.ParseAdd("DialogueHelper-DeltaruneStyle/1.0");
                var rel = await http.GetFromJsonAsync<JsonElement>("https://api.github.com/repos/UnderminersTeam/UndertaleModTool/releases/latest");
                foreach (var r in rel.GetProperty("assets").EnumerateArray().Where(x => (x.GetProperty("name").GetString() ?? "").StartsWith("UTMT_CLI_")))
                {
                    var n = r.GetProperty("name").GetString()!;
                    if (OperatingSystem.IsWindows() && !n.Contains("Windows"))
                        continue;
                    if (OperatingSystem.IsLinux() && !n.Contains("Ubuntu"))
                        continue;
                    if (OperatingSystem.IsMacOS() && !n.Contains("macOS"))
                        continue;
                    Console.WriteLine($"getting: {n}");
                    wh.Show(pg);
                    using var stream = await http.GetStreamAsync(r.GetProperty("browser_download_url").GetString()!);
                    using var za = new ZipArchive(stream, ZipArchiveMode.Read);
                    pg.ProgressBar.Maximum = za.Entries.Count;
                    pg.ProgressBar.Value = 0;
                    foreach (var e in za.Entries)
                    {
                        if (string.IsNullOrEmpty(e.Name))
                            continue;
                        pg.Text.Text = $"Extrating {e.Name}";
                        var path = Path.Combine(folderPath, e.FullName);
                        var fDirPath = Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(fDirPath))
                            Directory.CreateDirectory(fDirPath);
                        await e.ExtractToFileAsync(path, true);
                        pg.ProgressBar.Value++;
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"fetch exception: {e}");
                var w = new InfoWindow($"Unable to fetch latest UndertaleModCli, please manually download and extract UndertaleModCli to {UtmtPath}.");
                await wh.ShowDialog(w);
                return false;
            }
            finally
            {
                pg.Close();
            }
        }
        else
            return true;
        return false;
    }

    public async Task ImportFromDataWin(CustomToolWindowHandler wh)
    {
        if (!await wh.EnsureFileClosed())
            return;
        var output = await DataWinToJsonPrompt(wh);
        if (output is null)
            return;
        await LoadJson(output.ToDictionary(), wh);
        await wh.ShowDialog(new InfoWindow($"Imported data successfully."));
    }

    public async Task ExportToDataWin(CustomToolWindowHandler wh)
    {
        if (!await wh.EnsureFileOpen())
            return;
        if (!await DownloadUndertaleModToolCli(wh))
            return;
        var f = await wh.GetStorageProvider().OpenFilePickerAsync(new()
        {
            Title = "Select the game data file",
            FileTypeFilter = [new("GameMaker's Data Files") { Patterns = ["data.win"] }, new("All files") { Patterns = ["*"] }],
        });
        if (f.Count <= 0)
            return;
        var lw = new LoadingWindow { ProgressBar = { IsIndeterminate = true, ShowProgressText = false } };
        wh.Show(lw);
        try
        {
            var targetPath = $"\"{f[0].Path.LocalPath.Replace("\"", "\\\"")}\"";
            var baseJson = await DataWinToJsonPrompt(wh, lw, targetPath);
            if (baseJson is null)
                return;

            var file = File.Create($"{wh.GetStylePath()}/input.json");
            var (data, _, warnedKeys) = ExportJson(baseJson, wh, lw);
            await JsonSerializer.SerializeAsync(file, data, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            await file.DisposeAsync();
            if (await DataWinToJsonPrompt(wh, lw, targetPath, true) is null)
                return;
            if (warnedKeys.Count > 0)
            {
                lw.Hide();
                var sb = new StringBuilder();
                sb.AppendLine("Unable to be found entries to save (ignored in the final file):\n");
                foreach (var (key, cont, warn) in warnedKeys)
                {
                    sb.Append($"- {warn switch
                    {
                        WarningKind.NotFoundKey => "Target key not found",
                        WarningKind.DuplicatedKey => "Duplicate key found",
                        WarningKind.MissingKey => "Entry not found",
                        _ => throw new("unsupported warn kind"),
                    }} `{key}`");
                    if (cont is not null)
                        sb.Append($" (ID `{cont.Id}`)");
                    sb.AppendLine();
                    if (cont is not null)
                    {
                        sb.AppendLine($"  - Original Text: `{cont.OriginalText}`");
                        if (cont.Text is not null)
                            sb.AppendLine($"  - Translated Text: `{cont.Text}`");
                        sb.AppendLine();
                    }
                }
                sb.AppendLine($"Found {warnedKeys.Count} warning(s).");
                await wh.ShowDialog<bool>(new TextBoxWindow($"There {(warnedKeys.Count == 1 ? "was" : "were")} {warnedKeys.Count} warned {(warnedKeys.Count == 1 ? "key" : "keys")} found.", sb.ToString(), [new("Markdown file") { Patterns = ["*.md"] }]));
                lw.Show();
            }
            lw.Hide();
            await wh.ShowDialog(new InfoWindow($"Exported data successfully."));
        }
        finally
        {
            File.Delete($"{wh.GetStylePath()}/output.json");
            File.Delete($"{wh.GetStylePath()}/input.json");
            lw.Close();
        }
    }

    public async Task ImportFromJson(CustomToolWindowHandler wh)
    {
        if (!await wh.EnsureFileClosed())
            return;
        var f = await wh.GetStorageProvider().OpenFilePickerAsync(new()
        {
            Title = "Select a file",
            FileTypeFilter = [new("JSON Files") { Patterns = ["*.json"] }, new("All Files") { Patterns = ["*"] }],
        });
        if (f.Count <= 0)
            return;
        var lw = new LoadingWindow { ProgressBar = { IsIndeterminate = true, ShowProgressText = false } };
        wh.Show(lw);
        try
        {
            var stream = await f[0].OpenReadAsync();
            var output = (await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream))!;
            await stream.DisposeAsync();
            await LoadJson(output, wh);
        }
        finally
        {
            lw.Close();
        }
        await wh.ShowDialog(new InfoWindow($"Imported data successfully."));
    }

    public async Task ExportToJson(CustomToolWindowHandler wh)
    {
        if (!await wh.EnsureFileOpen())
            return;
        var fSave = await wh.GetStorageProvider().SaveFilePickerAsync(new()
        {
            Title = "Select a file to save to",
            FileTypeChoices = [new("JSON Files") { Patterns = ["*.json"] }],
        });
        if (fSave is null)
            return;
        var lw = new LoadingWindow { ProgressBar = { IsIndeterminate = true, ShowProgressText = false } };
        wh.Show(lw);
        var fd = wh.GetLoadedFileData()!;
        var allHaveKeys = true;
        foreach (var e in fd.IterAllStrings())
        {
            if (e.Key is not null && e.Key.Length != 0)
                continue;
            allHaveKeys = false;
        }
        OrderedDictionary<string, string>? output;
        if (allHaveKeys)
        {
            output = [];
            foreach (var e in fd.IterAllStrings())
            {
                if (e.Key is null)
                    throw new InvalidDataException($"unexpected null key on id {e.Id}");
                output.Add(e.Key, e.OriginalText);
            }
        }
        else
        {
            output = await DataWinToJsonPrompt(wh, lw);
            if (output is null)
                return;
        }
        try
        {
            var (data, _, warnedKeys) = ExportJson(output, wh, lw, fd);
            var stream = await fSave.OpenWriteAsync();
            await JsonSerializer.SerializeAsync(stream, data, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            await stream.DisposeAsync();
            if (warnedKeys.Count > 0)
            {
                lw.Hide();
                var sb = new StringBuilder();
                sb.AppendLine("Unable to be found entries to save (ignored in the final file):\n");
                foreach (var (key, cont, warn) in warnedKeys)
                {
                    sb.Append($"- {warn switch
                    {
                        WarningKind.NotFoundKey => "Target key not found",
                        WarningKind.DuplicatedKey => "Duplicate key found",
                        WarningKind.MissingKey => "Entry not found",
                        _ => throw new("unsupported warn kind"),
                    }} `{key}`");
                    if (cont is not null)
                        sb.Append($" (ID `{cont.Id}`)");
                    sb.AppendLine();
                    if (cont is not null)
                    {
                        sb.AppendLine($"  - Original Text: `{cont.OriginalText}`");
                        if (cont.Text is not null)
                            sb.AppendLine($"  - Translated Text: `{cont.Text}`");
                        sb.AppendLine();
                    }
                }
                sb.AppendLine($"Found {warnedKeys.Count} warning(s).");
                await wh.ShowDialog<bool>(new TextBoxWindow($"There {(warnedKeys.Count == 1 ? "was" : "were")} {warnedKeys.Count} warned {(warnedKeys.Count == 1 ? "key" : "keys")} found.", sb.ToString(), [new("Markdown file") { Patterns = [new("*.md")] }]));
                lw.Show();
            }
            lw.Hide();
            await wh.ShowDialog(new InfoWindow("Exported data successfully."));
        }
        finally
        {
            lw.Close();
        }
    }

    public async Task VerifyFileDialogueIntegrity(CustomToolWindowHandler wh)
    {
        if (!await wh.EnsureFileOpen())
            return;
        var lw = new LoadingWindow { ProgressBar = { IsIndeterminate = true, ShowProgressText = false } };
        wh.Show(lw);
        var output = await DataWinToJsonPrompt(wh);
        if (output is null)
        {
            lw.Close();
            return;
        }
        var (data, sc, warnedKeys) = ExportJson(output, wh, lw);
        var sb = new StringBuilder();
        sb.AppendLine("# Warnings\n");
        var warns = 0;
        foreach (var (key, value) in data)
        {
            if (!output.TryGetValue(key, out var og))
                continue;
            var ogParsed = new ParsedString(og);
            var parsed = new ParsedString(value);
            {
                var ogWarns = new List<(int Key, ParsedString.ParseWarn Value)>();
                var nWarns = new List<(int Key, ParsedString.ParseWarn Value)>();
                foreach (var (k, ev) in parsed.ParseWarns)
                {
                    foreach (var e in ev)
                        nWarns.Add((k, e));
                }
                foreach (var (k, ev) in ogParsed.ParseWarns)
                {
                    foreach (var e in ev)
                        ogWarns.Add((k, e));
                }
                if (ogWarns.Count != nWarns.Count)
                {
                    sb.AppendLine($"Parse warnings for {key} (ID `{sc[key].Cont.Id}`) wasn't the same for the original ({ogParsed.ParseWarns.Count}) to the translated ({parsed.ParseWarns.Count}) text.\n");

                    sb.AppendLine("|Old|New|\n|---|---|");
                    for (var i = 0; i < int.Max(nWarns.Count, ogWarns.Count); i++)
                    {
                        sb.Append("|");
                        if (i < ogWarns.Count)
                            sb.Append($"{ogWarns[i].Key + 1}: {ogWarns[i].Value.Message}");
                        sb.Append("|");
                        if (i < nWarns.Count)
                        {
                            var bold = i >= ogWarns.Count || nWarns[i].Value.Argument != ogWarns[i].Value.Argument;
                            sb.Append($"{nWarns[i].Key + 1}: {(bold ? "**" : "")}{nWarns[i].Value.Message}{(bold ? "**" : "")}");
                        }
                        sb.AppendLine("|");
                    }
                    sb.AppendLine();
                    warns++;
                }
            }
            var ogParse = ogParsed.Events.Select(x => (x.Key, Item: x.Value.Where(x => x is not DialogueTimeSkip and not DialogueIgnore).ToList())).ToArray();
            var parse = parsed.Events.Select(x => (x.Key, Item: x.Value.Where(x => x is not DialogueTimeSkip and not DialogueIgnore).ToList())).ToArray();
            var valid = true;
            if (parse.Length != ogParse.Length)
                valid = false;
            else
            {
                for (var i = 0; i < parse.Length; i++)
                {
                    var evs = parse[i];
                    var ogEvs = ogParse[i];
                    if (evs.Item.Count != ogEvs.Item.Count)
                    {
                        valid = false;
                        break;
                    }
                    foreach (var (e, o) in evs.Item.Zip(ogEvs.Item))
                    {
                        if (e.Argument == o.Argument)
                            continue;
                        valid = false;
                        break;
                    }
                    if (!valid)
                        break;
                }
            }
            if (!valid)
            {
                sb.AppendLine($"Invalid events found for `{key}` (ID `{sc[key].Cont.Id}`) that weren't the same for the original ({ogParse.Length}) to the translated ({parse.Length}) text:\n");
                sb.AppendLine("|Old|New|\n|---|---|");
                var evsParse = new List<(int Key, ParsedEvent Item)>();
                var ogEvsParse = new List<(int Key, ParsedEvent Item)>();
                foreach (var ev in parse)
                {
                    foreach (var e in ev.Item)
                        evsParse.Add((ev.Key, e));
                }
                foreach (var ev in ogParse)
                {
                    foreach (var e in ev.Item)
                        ogEvsParse.Add((ev.Key, e));
                }
                for (var i = 0; i < int.Max(evsParse.Count, ogEvsParse.Count); i++)
                {
                    sb.Append("|");
                    if (i < ogParse.Length)
                        sb.Append($"{ogParse[i].Key + 1}: {ogEvsParse[i].Item.Argument}");
                    sb.Append("|");
                    if (i < parse.Length)
                    {
                        var bold = i >= ogEvsParse.Count || evsParse[i].Item.Argument != ogEvsParse[i].Item.Argument;
                        sb.Append($"{evsParse[i].Key + 1}: {(bold ? "**" : "")}{evsParse[i].Item.Argument}{(bold ? "**" : "")}");
                    }
                    sb.AppendLine("|");
                }
                sb.AppendLine();
                warns++;
            }
        }
        if (warnedKeys.Count > 0)
        {
            sb.AppendLine("Unable to be found entries to save (ignored in the final file):\n");
            foreach (var (key, cont, warn) in warnedKeys)
            {
                sb.Append($"- {warn switch
                {
                    WarningKind.NotFoundKey => "Target key not found",
                    WarningKind.DuplicatedKey => "Duplicate key found",
                    WarningKind.MissingKey => "Entry not found",
                    _ => throw new("unsupported warn kind"),
                }} `{key}`");
                if (cont is not null)
                    sb.Append($" (ID `{cont.Id}`)");
                sb.AppendLine();
                if (cont is not null)
                {
                    sb.AppendLine($"  - Original Text: `{cont.OriginalText}`");
                    if (cont.Text is not null)
                        sb.AppendLine($"  - Translated Text: `{cont.Text}`");
                    sb.AppendLine();
                }
            }
            warns += warnedKeys.Count;
        }
        if (warns > 0)
        {
            sb.AppendLine($"Found {warns} warning(s).");
            lw.Hide();
            await wh.ShowDialog<bool>(new TextBoxWindow($"There {(warns == 1 ? "was" : "were")} {warns} warned {(warns == 1 ? "key" : "keys")} found.", sb.ToString(), [new("Markdown file") { Patterns = [new("*.md")] }]));
            lw.Show();
        }
        else
        {
            lw.Hide();
            await wh.ShowDialog(new InfoWindow("No warnings were found."));
        }
    }

    public async Task AssignStringKeyToAll(CustomToolWindowHandler wh)
    {
        if (!await wh.EnsureFileOpen())
            return;
        var output = await DataWinToJsonPrompt(wh);
        if (output is null)
            return;
        var fd = wh.GetLoadedFileData()!;
        var entryCount = 0;
        var entriesUpdated = 0;
        var unusedKeys = new List<(string Text, string Key)>();
        foreach (var (header, entries) in CalculateHeaders(output.Keys))
        {
            foreach (var key in entries)
                unusedKeys.Add((output[key], key));
        }
        foreach (var sc in fd.IterAllStrings())
        {
            var assign = (sc.Key ?? "").Length == 0;
            if ((sc.Key ?? "").Length > 0)
            {
                var kf = unusedKeys.Index().FirstOrDefault(x => x.Item.Key == sc.Key, (Index: -1, Item: (null!, null!)));
                if (kf.Index != -1)
                    unusedKeys.RemoveAt(kf.Index);
                else
                    assign = true;
            }
            if (assign)
            {
                var kf = unusedKeys.Index().FirstOrDefault(x => x.Item.Text == sc.OriginalText, (Index: -1, Item: (null!, null!)));
                if (kf.Index != -1)
                {
                    sc.Key = kf.Item.Key;
                    unusedKeys.RemoveAt(kf.Index);
                    entriesUpdated++;
                }
            }
            entryCount++;
        }
        await wh.LoadRaw(fd);
        await wh.ShowDialog(new InfoWindow($"Updated data successfully.\n({entriesUpdated}/{entryCount} keys updated.)"));
    }

    public async Task UpdateDialogues(CustomToolWindowHandler wh)
    {
        if (!await wh.EnsureFileOpen())
            return;
        var fd = wh.GetLoadedFileData()!;
        {
            var keys = new HashSet<string>();
            foreach (var e in fd.IterAllStrings())
            {
                if (e.Key is null || e.Key.Length == 0 || !keys.Add(e.Key))
                {
                    await wh.ShowDialog(new InfoWindow("This tool requires all entries to have a key (and be unique, too.)"));
                    return;
                }
            }
        }
        var oldOutput = await DataWinToJsonPrompt(wh, title: "Select the old game data file", acceptJsonAsReplacement: true);
        if (oldOutput is null)
            return;
        var newOutput = await DataWinToJsonPrompt(wh, title: "Select the new game data file", acceptJsonAsReplacement: true);
        if (newOutput is null)
            return;
        var resetEdited = await wh.ShowDialog<bool>(new QuestionWindow("Do you want to reset the texts that were changed between the 2 game versions to rectify the translation?"));
        var lw = new LoadingWindow { ProgressBar = { IsIndeterminate = true, ShowProgressText = false } };
        wh.Show(lw);
        try
        {
            var oldHeaders = CalculateHeaders(oldOutput.Keys);
            var oldEj = ExportJson(oldOutput, wh, lw, fd, oldHeaders);
            var newHeaders = CalculateHeaders(newOutput.Keys);
            var newEj = ExportJson(newOutput, wh, lw, fd, newHeaders);
            var allKeys = newOutput.Keys.ToList();
            foreach (var k in oldOutput.Keys)
            {
                if (allKeys.Contains(k))
                    continue;
                allKeys.Add(k);
            }
            foreach (var k in allKeys)
            {
                var oldGot = oldOutput.TryGetValue(k, out var oldValue);
                var newGot = newOutput.TryGetValue(k, out var newValue);
                if (newGot && !oldGot)
                {
                    var secKey = newHeaders.First(x => x.Value.Contains(k)).Key;
                    if (!fd.ContainsKeyString(secKey))
                        fd.AddString(secKey, []);
                    fd.GetString(secKey)!.Add(new()
                    {
                        Id = fd.LastStringId++,
                        OriginalText = newValue!,
                        Key = k,
                    });
                }
                else if (oldGot && !newGot)
                {
                    foreach (var (e, v) in fd.GetStringKeysValues())
                    {
                        var (idx, n) = v.Index().FirstOrDefault(x => x.Item.Key == k, (Index: -1, Item: null!));
                        if (n is not null)
                        {
                            v.RemoveAt(idx);
                            if (v.Count == 0)
                                fd.RemoveString(e);
                            break;
                        }
                    }
                }
                else if (oldValue != newValue)
                {
                    foreach (var e in fd.IterAllStrings())
                    {
                        if (e.Key != k)
                            continue;
                        e.OriginalText = newValue!;
                        if (resetEdited)
                        {
                            e.MarkAsModified = false;
                            e.Text = null;
                        }
                        break;
                    }
                }
            }
            await wh.LoadRaw(fd);
        }
        finally
        {
            lw.Close();
        }
    }

    public async Task ReorderDialogues(CustomToolWindowHandler wh)
    {
        if (!await wh.EnsureFileOpen())
            return;
        var output = await DataWinToJsonPrompt(wh);
        if (output is null)
            return;
        var lw = new LoadingWindow { ProgressBar = { IsIndeterminate = true, ShowProgressText = false } };
        wh.Show(lw);
        try
        {
            await wh.LoadRaw(ReorderFileData(wh, lw, output), lw);
        }
        finally
        {
            lw.Close();
        }
    }

    public FileData ReorderFileData(CustomToolWindowHandler wh, LoadingWindow lw, OrderedDictionary<string, string> json, FileData? fd = null, Dictionary<string, List<string>>? headers = null)
    {
        fd ??= wh.GetLoadedFileData()!;
        headers ??= CalculateHeaders(json.Keys);
        var ej = ExportJson(json, wh, lw, fd, headers);
        var conts = fd.IterAllStrings().ToList();
        var fdFinal = new FileData();
        foreach (var (entry, keys) in headers)
        {
            if (!fdFinal.ContainsKeyString(entry))
                fdFinal.AddString(entry, []);
            foreach (var key in keys)
            {
                if (ej.Container.TryGetValue(key, out var sc))
                {
                    conts.Remove(sc.Cont);
                    fdFinal.GetString(entry)!.Add(sc.Cont);
                }
            }
            if (fdFinal.ContainsKeyString(entry) && fdFinal.GetString(entry)!.Count == 0)
                fdFinal.RemoveString(entry);
        }
        if (conts.Count > 0)
            fdFinal.InsertString(0, "__NO_KEY_FOUND__", conts);
        return fdFinal;
    }

    public async Task<OrderedDictionary<string, string>?> DataWinToJsonPrompt(CustomToolWindowHandler wh, LoadingWindow? lw = null, string? path = null, bool importToDataWin = false, string? title = "", bool acceptJsonAsReplacement = false)
    {
        if (!await DownloadUndertaleModToolCli(wh))
            return null;
        var isJson = false;
        if (path is null)
        {
            List<FilePickerFileType> fpt = [new("GameMaker's Data Files") { Patterns = ["*.win"] }, new("All files") { Patterns = ["*"] }];
            if (acceptJsonAsReplacement)
                fpt.Insert(1, new("JSON Files") { Patterns = ["*.json"] });
            var f = await wh.GetStorageProvider().OpenFilePickerAsync(new()
            {
                Title = title ?? "Select the game data file",
                FileTypeFilter = fpt,
            });
            if (f.Count <= 0)
                return null;
            if (acceptJsonAsReplacement && f[0].Path.LocalPath.EndsWith("json", StringComparison.InvariantCultureIgnoreCase))
            {
                isJson = true;
                path = f[0].Path.LocalPath;
            }
            else
                path = f[0].Path.LocalPath.Replace("\"", "\\\"");
        }
        Process? proc = null;
        var autohide = lw is null;
        if (lw is null)
        {
            lw = new LoadingWindow { ProgressBar = { IsIndeterminate = true, ShowProgressText = false } };
            wh.Show(lw);
        }
        try
        {
            if (!isJson)
            {
                var args = $"load \"{path}\" -s \"{wh.GetStylePath()}/";
                if (!importToDataWin)
                {
                    File.Delete($"{wh.GetStylePath()}/output.json");
                    args += "ExportStringsJson.csx\"";
                }
                else
                    args += $"ImportStringsJson.csx\" -o \"{path}\"";
                proc = new Process
                {
                    StartInfo =
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = $"{UtmtPath}/UndertaleModCli{(OperatingSystem.IsWindows() ? ".exe" : "")}",
                    Arguments = args,
                    WorkingDirectory = UtmtPath,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                },
                };
                proc.Start();

                var stdout = new StringBuilder();
                while (!proc.HasExited)
                {
                    var line = await proc.StandardOutput.ReadLineAsync();
                    if (line is null)
                        continue;
                    stdout.Append(line);
                    stdout.Append('\n');
                    line = line.Trim();
                    if (line.EndsWith(')'))
                    {
                        var idx = line.LastIndexOf('(');
                        if (idx != -1)
                        {
                            var sep = line.IndexOf('/', idx + 1);
                            if (sep != -1 && sep < line.Length - 1)
                            {
                                lw.ProgressBar.IsIndeterminate = false;
                                lw.ProgressBar.Maximum = long.Parse(line[(sep + 1)..(line.Length - 1)]);
                                lw.ProgressBar.Value = long.Parse(line[(idx + 1)..sep]);
                            }
                        }
                    }
                    lw.Text.Text = line;
                }
                var stderr = await proc.StandardError.ReadToEndAsync();

                Console.WriteLine($"stdout:\n{stdout}\nstderr:\n{stderr}");
                if (proc.ExitCode != 0 || stderr.Trim().Length > 0 || (!importToDataWin && !File.Exists($"{wh.GetStylePath()}/output.json")))
                {
                    lw.Close();
                    await wh.ShowDialog(new TextBoxWindow($"UndertaleModCli exited with code {proc.ExitCode}", $"stdout:\n{stdout}\nstderr:\n{stderr}", [new("Text file") { Patterns = ["*.txt"] }]));
                    return null;
                }
            }
            if (importToDataWin)
                return [];
            else
            {
                var stream = new MemoryStream(await File.ReadAllBytesAsync(isJson ? path : $"{wh.GetStylePath()}/output.json"));
                var output = (await JsonSerializer.DeserializeAsync<OrderedDictionary<string, string>>(stream))!;
                await stream.DisposeAsync();

                return output;
            }
        }
        finally
        {
            proc?.Dispose();
            if (!isJson)
                File.Delete($"{wh.GetStylePath()}/{(!importToDataWin ? "output" : "input")}.json");
            if (autohide)
                lw.Close();
        }
    }

    public Dictionary<string, List<string>> CalculateHeaders(IEnumerable<string> keys)
    {
        var headers = new Dictionary<string, List<string>>();
        var lastHeader = "";
        var targetHeader = "";
        foreach (var key in keys)
        {
            if (key == "date")
                continue;
            var pos = key.LastIndexOf("_gml_");
            if (pos == -1)
                pos = key.LastIndexOf('_');
            if (pos == -1)
            {
                Console.WriteLine($"Unable to make a string group of {key}");
                pos = key.Length;
            }
            var header = key[..pos];
            if (header != lastHeader)
            {
                lastHeader = header;
                if (headers.ContainsKey(header))
                {
                    var i = 1;
                    while (headers.ContainsKey($"{header}+{i}"))
                        i++;
                    targetHeader = $"{header}+{i}";
                }
                else
                    targetHeader = header;
            }
            headers.TryAdd(targetHeader, []);
            headers[targetHeader].Add(key);
        }
        return headers;
    }

    public async Task LoadJson(Dictionary<string, string> json, CustomToolWindowHandler wh)
    {
        var fd = new FileData();
        foreach (var (header, entries) in CalculateHeaders(json.Keys))
        {
            var l = new List<StringContainer>();
            foreach (var key in entries)
            {
                l.Add(new()
                {
                    OriginalText = json[key]!,
                    Key = key,
                });
            }
            fd.AddString(header, l);
        }
        await wh.LoadRaw(fd);
    }

    public record ExportedJson(OrderedDictionary<string, string> Data, Dictionary<string, (string Entry, StringContainer Cont)> Container, List<(string key, StringContainer? cont, WarningKind warnKind)> WarnedKeys);

    public ExportedJson ExportJson(OrderedDictionary<string, string> json, CustomToolWindowHandler wh, LoadingWindow lWin, FileData? file = null, Dictionary<string, List<string>>? headers = null)
    {
        var data = new OrderedDictionary<string, string> { ["date"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() };
        var warnedKeys = new List<(string, StringContainer?, WarningKind)>();
        var scUns = new Dictionary<string, (string BaseKey, StringContainer Cont)>();
        file ??= wh.GetLoadedFileData()!;
        headers ??= CalculateHeaders(json.Keys);
        lWin.Text.Text = "Processing strings...";
        var count = 0;
        foreach (var c in file.GetStringValues())
        {
            foreach (var v in c)
                count++;
        }
        lWin.Text.Text = "Outputting data...";
        lWin.ProgressBar.Maximum = count;
        lWin.ProgressBar.Value = 0;
        lWin.ProgressBar.ShowProgressText = true;
        lWin.ProgressBar.IsIndeterminate = false;
        string? GetContKey(string key, StringContainer cont)
        {
            string? targetKey = null;
            if (cont.Key is not null)
                targetKey = cont.Key;
            else if (headers.TryGetValue(key, out var keys))
                targetKey = keys.FirstOrDefault(x => x is not null && !data.ContainsKey(x) && json[x] == cont.OriginalText, null);
            return targetKey;
        }
        var preorder = new Dictionary<string, string>();
        foreach (var (key, conts) in file.GetStringKeysValues())
        {
            foreach (var cont in conts)
            {
                var tk = GetContKey(key, cont);
                if (tk is not null)
                {
                    if (!preorder.TryAdd(tk, cont.Text ?? cont.OriginalText))
                    {
                        warnedKeys.Add((key, cont, WarningKind.DuplicatedKey));
                        Console.WriteLine($"duplicate key found for {cont.Id} ({key}) with text \"{cont.OriginalText}\" (edited: \"{cont.Text}\")!");
                    }
                    else
                        scUns.Add(tk, (key, cont));
                }
                else
                {
                    warnedKeys.Add((key, cont, WarningKind.NotFoundKey));
                    Console.WriteLine($"no key found for {cont.Id} ({key}) with text \"{cont.OriginalText}\" (edited: \"{cont.Text}\")!");
                }
                lWin.ProgressBar.Value++;
            }
        }
        foreach (var k in json.Keys.SkipWhile(x => x == "date"))
        {
            if (scUns.Keys.Contains(k))
                continue;
            warnedKeys.Add((k, null, WarningKind.MissingKey));
        }
        foreach (var k in json.Keys.SkipWhile(x => x == "date"))
        {
            if (preorder.TryGetValue(k, out var v))
                data.Add(k, v);
        }
        // ensure that nothing is missing
        foreach (var (k, v) in preorder)
        {
            if (!data.ContainsKey(k))
                data.Add(k, v);
        }
        return new(data, scUns, warnedKeys);
    }

    public override async Task<bool> PrepareDraw(CustomToolWindowHandler wh, ExternalData data)
    {
        int? characterLimit;
        double? textAreaEnd;
        {
            characterLimit = data.Box.CustomProperties.TryGetValue("CharacterLimit", out var l)
                             && l is JsonElement && ((JsonElement)l).TryGetInt32(out var eValue)
                ? eValue
                : null;
            textAreaEnd = data.Box.CustomProperties.TryGetValue("TextAreaEnd", out l)
                          && l is JsonElement && ((JsonElement)l).TryGetDouble(out var d)
                ? d
                : null;
        }
        if (data.UpdateKind is PreviewRenderer.UpdateKind.SelectedBoxChanged)
        {
            if (characterLimit is null)
            {
                DialogueType.SelectedIndex = (int)DialogueKinds.DynamicWordWrap;
                DialogueType.UpdateUiValue();
            }
            else if (DialogueType.SelectedIndex == (int)DialogueKinds.DynamicWordWrap)
            {
                DialogueType.SelectedIndex = (int)DialogueKinds.Default;
                DialogueType.UpdateUiValue();
            }
            return true;
        }
        Parsed = new(data.Char.String, data.Box);
        data.Char.String = Parsed.ParsedText;
        EnvX = -1;
        EnvY = -1;
        EnvSkip = 0;
        EnvFirstDrawnChar = true;
        EnvCheckedIndex = -1;
        IsRainbow = false;
        IsLastNewline = true;
        EnvDrawAsterisk = true;
        EnvDynLimitWrap = data.Style.ImageAssets[data.Box.Images[0].Path].Width * data.GetCurrentBoxScale();
        {
            double x, y;
            if (data.Box.CustomProperties.TryGetValue("PortraitOffset", out var bpo) && bpo is JsonElement po)
            {
                po[0].TryGetDouble(out x);
                po[1].TryGetDouble(out y);
            }
            else
            {
                /* so that boxes that don't have PortraitOffset
                 * don't default to the upper left corner
                 * if PortraitKinds.Basic is active */
                x = data.Box.TextOffset[0];
                y = data.Box.TextOffset[1];
            }
            EnvPortraitOffset = (x, y);
        }
        if (textAreaEnd.HasValue)
            EnvDynLimitWrap = textAreaEnd.Value;
        if ((PortraitKinds)PortraitKind.SelectedIndex is PortraitKinds.Basic) {
            data.Char.StartPosition = EnvPortraitOffset;
        }
        //Console.WriteLine(string.Join(",", Parsed.AsteriskPositions));
        return await base.PrepareDraw(wh, data);
    }

    public static bool IsNewlineSafe(int idx, string str) => Newlines.Contains(str[idx]) && str[int.Max(0, idx - 1)] != '`';

    public override async Task<bool> DrawGlyph(CustomToolWindowHandler wh, ExternalData data)
    {
        if (Newlines.Contains(data.Char.Char))
            data.Char.IsNewline = true;

        var font = (Fonts)data.GetCurrentFont();

        if (Parsed.Events.TryGetValue(data.Char.Index, out var evs))
        {
            foreach (var ev in evs)
            {
                switch (ev)
                {
                    case DialogueIgnore:
                        break;
                    case DialogueIndexPortrait or DialogueCharPortrait:
                        if (PortraitKind.SelectedIndex == (int)PortraitKinds.Default)
                        {
                            PortraitKind.SelectedIndex = (int)PortraitKinds.Basic;
                            PortraitKind.UpdateUiValue();
                            return true;
                        }
                        break;
                    case DialogueFonts fnt:
                        if (fnt.Font is not null)
                            data.Font = data.Style.FontMetadata[(int)fnt.Font.Value];
                        break;
                    case DialogueColor col:
                        IsRainbow = col.Rainbow;
                        if (col.Color is not null)
                            data.Glyph.Color = col.Color.Value;
                        break;
                    case DialogueMiniFace:
                        EnvDrawAsterisk = false;
                        break;
                }
            }
        }

        if (EnvSkip-- > 0)
            return false;

        if (IsRainbow)
        {
            data.QueueRedraw = true;
            var rgb = HsvColor.FromHsv(((Stopwatch.Elapsed.TotalSeconds * 60d) + (data.Char.Index * 30d)) % 360d, 0.6, 1).ToRgb();
            if (Stopwatch.Elapsed.TotalSeconds >= 360d * 2)
                Stopwatch.Restart();
            data.Glyph.Color = new() { R = rgb.R, B = rgb.B, G = rgb.G };
        }
        switch ((Fonts)data.GetCurrentFont())
        {
            case Fonts.Main:
                EnvX = 8;
                EnvY = 18;
                break;
            case Fonts.ComicSans:
                EnvX = 8;
                EnvY = 18;
                break;
            case Fonts.TinyNoelle:
                EnvX = 6;
                EnvY = 18;
                break;
            case Fonts.Dotumche:
                EnvX = 9;
                EnvY = 20;
                break;
            case Fonts.TennaBoard:
                EnvX = 16;
                EnvY = 20;
                break;
            default:
                { EnvX = data.Font.GlyphDictionary.TryGetValue(data.Char.Char, out var glyph) ? glyph.Shift : -1; }
                break;
        }
        {
            if (data.Box.CustomProperties.TryGetValue("CustomY", out var l)
                && l is JsonElement && ((JsonElement)l).TryGetDouble(out var eValue))
                EnvY = eValue;
            if (data.Box.CustomProperties.TryGetValue("CustomX", out l)
                && l is JsonElement && ((JsonElement)l).TryGetDouble(out eValue))
                EnvX = eValue;
        }
        var visualY = 0;
        if ((Enum.GetName(font) ?? "").Contains("jp", StringComparison.InvariantCultureIgnoreCase) && data.Char.Char <= 0xff || (data.Char.Char >= 65377 && data.Char.Char <= 65439))
        {
            EnvX /= 2;
            EnvX += DarkZone.Value ? 16 : 8;
        }
        else
        {
            switch (font)
            {
                case Fonts.ComicSans:
                    switch (data.Char.Char)
                    {
                        case 'w': EnvX += 2; break;
                        case 'm': EnvX += 3; break;
                        case 'i' or 'l': EnvX -= 2; break;
                        case 's' or 'j': EnvX--; break;
                    }
                    break;
                case Fonts.TennaBoard:
                    if (data.Char.Char == '$')
                        visualY -= 4;
                    break;
            }
        }
        var actAsNewline = IsNewlineSafe(data.Char.Index, data.Char.String);
        if ((DialogueKinds)DialogueType.SelectedIndex is DialogueKinds.DynamicWordWrap && data.Char.Index > EnvCheckedIndex)
        {
            if (EnvCheckedIndex == -1)
                EnvCheckedIndex = 0;
            var i = 0;
            var fs = "";
            while (data.Char.Index + i < Parsed.TextTrimLength && (data.Char.String[data.Char.Index + i] == ' ' || IsNewlineSafe(data.Char.Index + i, data.Char.String)))
            {
                fs += data.Char.String[data.Char.Index + i];
                i++;
            }
            while (data.Char.Index + i < Parsed.TextTrimLength && data.Char.String[data.Char.Index + i] != ' ' && !IsNewlineSafe(data.Char.Index + i, data.Char.String))
            {
                var c = data.Char.String[data.Char.Index + i];
                switch (c)
                {
                    case '`': i++; continue;
                    case '\\': i += 3; continue;
                    case '^': i += 2; continue;
                    case '/':
                        if (data.Char.Index + i >= Parsed.TextTrimLength - 1 || data.Char.Index + i == 0)
                        {
                            i++;
                            continue;
                        }
                        if ((data.Char.Index + i == Parsed.TextTrimLength - 2 && data.Char.String[data.Char.Index + i + 1] == '%') ||
                            (data.Char.Index + i == Parsed.TextTrimLength - 3 && data.Char.String[data.Char.Index + i + 1] == '%' && data.Char.String[data.Char.Index + i + 2] == '%'))
                        {
                            i += 2;
                            continue;
                        }
                        break;
                    case '%':
                        if ((data.Char.Index + i == Parsed.TextTrimLength - 2 && data.Char.String[data.Char.Index + i + 1] == '%') || data.Char.Index + i == Parsed.TextTrimLength - 1)
                        {
                            i += 2;
                            continue;
                        }
                        break;
                }
                fs += c;
                i++;
            }

            EnvCheckedIndex = data.Char.Index + i;
            var charPos = data.Char.PositionOffset.X;
            foreach (var chr in fs)
            {
                var glyph = data.Font.Glyphs.FirstOrDefault(g => g.Char == chr);
                if (glyph is null)
                    continue;
                charPos += ((EnvX != -1 ? EnvX : glyph.Shift) + glyph.Offset) * 2;
            }
            var wrapSize = data.Style.ImageAssets[data.Box.Images[0].Path].Width * data.GetCurrentBoxScale();
            if (!IsLastNewline && charPos + 18 >= (EnvDynLimitWrap / data.GetCurrentFontScale()) - data.Char.StartPosition.X && !EnvFirstDrawnChar)
                actAsNewline = true;
        }

        IsLastNewline = actAsNewline;
        if (actAsNewline)
        {
            data.Char.PositionOffset.X = 0;

            if (EnvY == -1)
            {
                var size = data.Font.GlyphDictionary['A'].Size.Y;
                data.Char.PositionOffset.Y += (size + (size % 11) + (data.Font.Size % 2)) * 2;
            }
            else
                data.Char.PositionOffset.Y += EnvY * 2;
        }
        if (!IsNewlineSafe(data.Char.Index, data.Char.String) && !data.Char.IsIgnore)
        {
            var glyph = data.Font.GlyphDictionary.GetValueOrDefault(data.Char.Char, new()
            {
                Char = '\0',
                Position = (0, 0),
                Size = (0, 0),
                Shift = 0,
                Offset = 0,
            })!;
            if (EnvX == -1 && data.Char.Index > 0)
            {
                var last = data.Char.String[data.Char.Index - 1];
                foreach (var k in glyph.Kerning)
                {
                    if (k.PrecedingChar != last)
                        continue;
                    data.Char.PositionOffset.X += k.ShiftModifier;
                    break;
                }
            }
            data.Glyph.Position.X = data.Char.StartPosition.X + data.Char.PositionOffset.X + (glyph.Offset * 2);
            data.Glyph.Position.Y = data.Char.StartPosition.Y + data.Char.PositionOffset.Y + visualY;
            data.Glyph.Size.X = glyph.Size.X * 2;
            data.Glyph.Size.Y = glyph.Size.Y * 2;

            if (!Parsed.UndrawPositions.Contains(data.Char.Index) && (EnvDrawAsterisk || (!EnvDrawAsterisk && !Parsed.AsteriskPositions.Contains(data.Char.Index))))
                data.DrawGlyph(glyph.Char);
            /*if (Parsed.AsteriskPositions.Contains(data.Char.Index))
                Console.WriteLine($"{EnvDrawAsterisk}->{data.Char.Index}->{data.Char.Char}->{glyph.Char}");*/
            //var last1 = data.Char.PositionOffset.X;
            data.Char.PositionOffset.X += (int)double.Ceiling(EnvX != -1 ? EnvX : (glyph.Shift + glyph.Offset)) * 2;
            //Console.WriteLine($"{glyph.Char}->{(int)glyph.Char}, {last1}->{data.Char.PositionOffset.X}");
            EnvFirstDrawnChar = false;
        }
        return await base.PrepareDraw(wh, data);
    }
}

public class ParsedEvent(string argument)
{
    public string Argument = argument;
}
/// <summary>
/// /%%, /%, %%, /, %
/// </summary>
public class DialogueEnd(string argument) : ParsedEvent(argument) { }
/// <summary>
/// ^_ (_ = int)
/// </summary>
public class DialogueTimeSkip(string argument) : ParsedEvent(argument) { }
/// <summary>
/// \c_ (_ = char)
/// </summary>
public class DialogueColor(string argument) : ParsedEvent(argument)
{
    public required Color? Color;
    public required bool Rainbow;
}
/// <summary>
/// \T_ (_ = char)
/// </summary>
public class DialogueFonts(string argument) : ParsedEvent(argument)
{
    public required Deltarune.Fonts? Font;
}
/// <summary>
/// \F_ (_ = char) (for character selection)
/// </summary>
public class DialogueCharPortrait(string argument) : ParsedEvent(argument) { }
/// <summary>
/// \E_ (_ = char) (portrait index)
/// </summary>
public class DialogueIndexPortrait(string argument) : ParsedEvent(argument) { }
/// <summary>
/// \s_ (_ = int 0..=1): skippable flag (0 == false, 1 == true)
/// </summary>
public class DialogueSkippable(string argument) : ParsedEvent(argument) { }
/// <summary>
/// \C_ (_ = int 1..=4)
/// </summary>
public class DialogueChoicer(string argument) : ParsedEvent(argument) { }
/// <summary>
/// \M_ (_ = int 1..=9): flag 20 on index int
/// </summary>
public class DialogueFlag20(string argument) : ParsedEvent(argument) { }
/// <summary>
/// \S_ (_ = int 1..=9): play sound on an array through index _
/// </summary>
public class DialogueSound(string argument) : ParsedEvent(argument) { }
/// <summary>
/// \I_ (_ = int 1..=9): show sprite on an array through index _
/// </summary>
public class DialogueSprite(string argument) : ParsedEvent(argument) { }
/// <summary>
/// \m_ (_ = int 1..=9): show small-face sprite on an array through index _
/// </summary>
public class DialogueMiniFace(string argument) : ParsedEvent(argument) { }
/// <summary>
/// \O_ (_ = int 1..=9): create an object instance on an array through index _
/// </summary>
/// <param name="argument"></param>
public class DialogueObject(string argument) : ParsedEvent(argument) { }
/// <summary>
/// \f_ (_ = int 1..=9): update an instance on an array through index _
/// </summary>
public class DialogueInstanceUpdate(string argument) : ParsedEvent(argument) { }
/// <summary>
/// `
/// </summary>
public class DialogueIgnore(string argument) : ParsedEvent(argument) { }
/// <summary>
/// \v_ (_ = int 1..=2): update an instance on an array through index _
/// </summary>
public class DialogueVoiceClipMode(string argument) : ParsedEvent(argument) { }
/// <summary>
/// \V_ (_ = char A-Z, z): update an instance on an array through index _
/// </summary>
public class DialogueFloweryVoiceClip(string argument) : ParsedEvent(argument) { }

public class ParsedString
{
    public class ParseWarn(string argument, string message)
    {
        public string Argument = argument;
        public string Message = message;
    }
    public class ParseOutOfRange(string argument, Range range) : ParseWarn(argument, $"Invalid range for \"{argument}\", valid range is only {range.Start} to {range.End}") { }

    public string Text;
    public string ParsedText;
    public int TextTrimLength;
    public Dictionary<int, List<ParsedEvent>> Events = [];
    public Dictionary<int, List<ParseWarn>> ParseWarns = [];
    public HashSet<int> AsteriskPositions = [];
    public HashSet<int> UndrawPositions = [];
    public bool AsteriskCheck;

    void AddEvent(int i, ParsedEvent ev)
    {
        Events.TryAdd(i, []);
        Events[i].Add(ev);
    }

    void AddParseWarn(int i, ParseWarn warn)
    {
        ParseWarns.TryAdd(i, []);
        ParseWarns[i].Add(warn);
    }
    
    public ParsedString(string text = "", BoxMetadata? box = null, bool autoAsterisk = true) {
        Text = text;
        var trimLen = text.TrimEnd().Length;
        TextTrimLength = trimLen;
        var sb = new StringBuilder();
        var tSpace = -1;
        var cPos = 0;
        var spaceSb = -1;
        var lastX = -1;
        var indentSize = 2;
        //Console.WriteLine($"============== {text}");
        int? limit = null;
        if (box is not null)
        {
            // I don't know how to format this to not by this ugly
            if (
                // portrait
                (Deltarune.PortraitKinds)Deltarune.PortraitKind.SelectedIndex == Deltarune.PortraitKinds.Basic
                    && box.CustomProperties.TryGetValue("PortraitCharacterLimit", out var l)
                    && l is JsonElement
                    && ((JsonElement)l).TryGetInt32(out var eValue)
                // normal
                ||  box.CustomProperties.TryGetValue("CharacterLimit", out l)
                    && l is JsonElement
                    && ((JsonElement)l).TryGetInt32(out eValue)
                )
                limit = eValue;
        }
        for (var i = 0; i < trimLen; i++)
        {
            var x = sb.Length;
            if (lastX != x)
            {
                lastX = x;
                Events.Remove(x);
                ParseWarns.Remove(x);
                AsteriskPositions.Remove(x);
            }
            char? @char = text[i];
            if (@char is '`')
            {
                AddEvent(x, new DialogueIgnore(@char.Value.ToString()));
                i++;
                continue;
            }
            if (trimLen - i >= 3)
            {
                var sStr = text[i..(i + 3)];
                if (sStr == "/%%")
                {
                    AddEvent(x, new DialogueEnd(sStr));
                    i += 2;
                    continue;
                }
            }
            if (trimLen - i >= 2)
            {
                var sStr = text[i..(i + 2)];
                if (sStr == "/%" || sStr == "%%" || (sStr[0] == '^' && char.IsNumber(sStr[1])))
                {
                    AddEvent(x, sStr[0] == '^' ? new DialogueTimeSkip(sStr) : new DialogueEnd(sStr));
                    i++;
                    continue;
                }
            }
            if (((trimLen - i == 1 || i == 0) && @char == '/') || (trimLen - i == 1 && @char == '%'))
            {
                AddEvent(x, new DialogueEnd(@char.Value.ToString()));
                continue;
            }
            switch (@char)
            {
                case '~':
                {
                    if (trimLen - i >= 1 && "0123456789".Contains(text[i + 1]))
                    {
                        var fullSlice = text[i..(i + 1)];
                        if (text.StartsWith("~1* "))    // best way to detect if it's a flower line
                        {
                            if (i == 0)
                            {
                                AddEvent(i, new DialogueMiniFace(fullSlice));
                                indentSize += 2;
                                for (var j = 0; j < 2; j++)
                                    UndrawPositions.Add(sb.Length + j);
                            }
                            else
                            {
                                // this is a little janky, but it works
                                text = text.Replace(fullSlice, "\n\t");
                                for (var j = 0; j < 6; j++)
                                    UndrawPositions.Add(sb.Length + j);
                            }
                        }
                    }
                }
                    break;
                case '\\':
                {
                    var valid = true;
                    if (trimLen - i >= 3)
                    {
                        var fullSlice = text[i..(i + 3)];
                        var slice = text[(i + 1)..(i + 3)];
                        switch (slice[0])
                        {
                            case 'E':
                            {
                                var oldFullSlice = fullSlice;
                                if (slice[1] is '~' && trimLen - i >= 4 && "0123456789".Contains(text[i + 3]))
                                {
                                    fullSlice = text[i..(i + 4)];
                                }
                                else if (!"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".Contains(slice[1]))
                                {
                                    valid = false;
                                    AddParseWarn(x, new ParseWarn(fullSlice, $"invalid choice {slice[1]} for setting index portrait: {fullSlice}"));
                                }
                                if (valid)
                                {
                                    AddEvent(x, new DialogueIndexPortrait(fullSlice));
                                    i += fullSlice.Length - oldFullSlice.Length;
                                }
                            }
                                break;
                            case 'F':
                            {
                                // todo: \F0 seems to reset line wrap
                                if (!"0SRNTLsUAaBubrUKQC◘yi".Contains(slice[1]))
                                {
                                    valid = false;
                                    AddParseWarn(x, new ParseWarn(fullSlice, $"invalid choice {slice[1]} for setting character portrait: {fullSlice}"));
                                }
                                else
                                    AddEvent(x, new DialogueCharPortrait(fullSlice));
                            }
                                break;
                            case 'T':
                            {
                                Deltarune.Fonts? f = slice[1] switch
                                {
                                    '0' or '1' or 'A' or 'a' or 'N' or 'n' or 'B' or 'S' or 'X' or 'r' or 'T' or 'U' or 'C' => Deltarune.Fonts.Main,
                                    'R' or 'L' or 'J' or 'K' or 'q' or 'Q' or 'p' or 'k' or 'j' or 'g' or 'v' => Deltarune.Fonts.Main, //"fnt_mainbig",
                                    's' => Deltarune.Fonts.ComicSans,
                                    _ => null,
                                };
                                if (f is null)
                                {
                                    valid = false;
                                    AddParseWarn(x, new ParseWarn(fullSlice, $"invalid choice {slice[1]} for setting font: {fullSlice}"));
                                }
                                else
                                    AddEvent(x, new DialogueFonts(fullSlice) { Font = f });
                            }
                                break;
                            case 'c':
                            {
                                var rnb = false;
                                Color? col = slice[1] switch
                                {
                                    'R' => new() { Rgba = 0xff0000ff },
                                    'B' => new() { Rgba = 0x0000ffff },
                                    'Y' => new() { Rgba = 0xffff00ff },
                                    'G' => new() { Rgba = 0x00ff00ff },
                                    'W' => new() { Rgba = 0xffffffff },
                                    'X' => new() { Rgba = 0x000000ff },
                                    'P' => new() { Rgba = 0x800080ff },
                                    'M' => new() { Rgba = 0x800000ff },
                                    'O' => new() { Rgba = 0xffa500ff },
                                    'A' => new() { Rgba = 0x00aeffff },
                                    'S' => new() { Rgba = 0xff80ffff },
                                    'V' => new() { Rgba = 0x80ff80ff },
                                    'Z' => null,
                                    'a' => new() { Rgba = 0x84f9ffff },
                                    'y' => new() { Rgba = 0xfff8a1ff },
                                    'g' => new() { Rgba = 0xaeffbcff },
                                    'o' => new() { Rgba = 0xffac87ff },
                                    's' => new() { Rgba = 0xe2a8fcff },
                                    'p' => new() { Rgba = 0xff8a90ff },
                                    'b' => new() { Rgba = 0x86a7ffff },
                                    'I' => new() { Rgba = 0x549ff9ff },
                                    '0' => new() { Rgba = 0xff69b4ff },
                                    _ => null,
                                };
                                if (slice[1] == 'Z')
                                    rnb = true;
                                if (col is null && !rnb)
                                {
                                    valid = false;
                                    AddParseWarn(x, new ParseWarn(fullSlice, $"invalid choice {slice[1]} for setting color: {fullSlice}"));
                                }
                                else
                                    AddEvent(x, new DialogueColor(fullSlice) { Color = col, Rainbow = rnb });
                            }
                                break;
                            case 'V':
                            {
                                if (!"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZzabcdefghijklmnopqrstuvwxy".Contains(slice[1]))
                                {
                                    valid = false;
                                    AddParseWarn(x, new ParseWarn(fullSlice, $"invalid voice-clip selected: {slice[1]}"));
                                }
                                else
                                    AddEvent(x, new DialogueFloweryVoiceClip(fullSlice));
                            }
                                break;
                            case 'S' or 's' or 'C' or 'M' or 'I' or 'm' or 'f' or 'O' or 'v':
                            {
                                if (!int.TryParse(slice[1].ToString(), out var v))
                                    valid = false;
                                else
                                {
                                    switch (slice[0])
                                    {
                                        case 'v':
                                            if (v < 0 || v > 2)
                                            {
                                                valid = false;
                                                AddParseWarn(x, new ParseOutOfRange(fullSlice, 0..2));
                                            }
                                            else
                                                AddEvent(x, new DialogueVoiceClipMode(fullSlice));
                                            break;
                                        case 's':
                                            if (v < 0 || v > 1)
                                            {
                                                valid = false;
                                                AddParseWarn(x, new ParseOutOfRange(fullSlice, 0..1));
                                            }
                                            else
                                                AddEvent(x, new DialogueSkippable(fullSlice));
                                            break;
                                        case 'C':
                                            if (v < 1 || v > 4)
                                            {
                                                valid = false;
                                                AddParseWarn(x, new ParseOutOfRange(fullSlice, 1..4));
                                            }
                                            else
                                                AddEvent(x, new DialogueChoicer(fullSlice));
                                            break;
                                        case 'M':
                                            AddEvent(x, new DialogueFlag20(fullSlice));
                                            break;
                                        case 'S':
                                            AddEvent(x, new DialogueSound(fullSlice));
                                            break;
                                        case 'I':
                                            AddEvent(x, new DialogueSprite(fullSlice));
                                            break;
                                        case 'm':
                                            AddEvent(x, new DialogueMiniFace(fullSlice));
                                            var indentCounter = 0;
                                            var ii = i + 3;
                                            while (trimLen - ii > 0 && text[ii] is '*' or '\t' or ' ')
                                            {
                                                indentCounter++;
                                                ii++;
                                            }
                                            if (indentCounter == 4)
                                            {
                                                Color? col = slice[1] switch
                                                {
                                                    '0' => new() { Rgba = 0x84f9ffff }, // Aqua
                                                    '1' => new() { Rgba = 0xe2a8fcff }, // Seth
                                                    '2' => new() { Rgba = 0xffac87ff }, // Orange
                                                    '3' => new() { Rgba = 0xaeffbcff }, // Green
                                                    '4' => new() { Rgba = 0xfff8a1ff }, // Yellow
                                                    '5' => new() { Rgba = 0x86a7ffff }, // Blue
                                                    _ => null
                                                };
                                                if (col is not null)
                                                    AddEvent(x, new DialogueColor(fullSlice) { Color = col, Rainbow = false });
                                            }
                                            if (Deltarune.AutoIndentAutoWrappedMiniFaceLines.Value) {
                                                indentSize = indentCounter;
                                            }
                                            break;
                                        case 'O':
                                            AddEvent(x, new DialogueObject(fullSlice));
                                            break;
                                        case 'f':
                                            AddEvent(x, new DialogueInstanceUpdate(fullSlice));
                                            break;
                                        default:
                                            valid = false;
                                            AddParseWarn(x, new ParseWarn(fullSlice, $"unknown parameter: {fullSlice}"));
                                            break;
                                    }
                                }
                            }
                                break;
                            default:
                                valid = false;
                                break;
                        }
                    }
                    if (valid)
                    {
                        i += 2;
                        continue;
                    }
                }
                    break;
                //Console.WriteLine($"{lastIsNewLine}:{i},{cPos}->{text[i]}->{text[i] == '*'}");
                case '*':
                    AsteriskCheck = true;
                    AsteriskPositions.Add(x);
                    break;
                case ' ':
                    tSpace = i + 1;
                    spaceSb = sb.Length;
                    break;
            }

            if (Deltarune.IsNewlineSafe(i, text))
            {
                tSpace = -1;
                cPos = 0;
                sb.Append(@char);
                @char = null;
                if (AsteriskCheck && autoAsterisk && (i + 1 >= text.Length || text[i + 1] != '*'))
                {
                    cPos += 2;
                    for (var j = 0; j < 2; j++)
                        UndrawPositions.Add(sb.Length + j);
                    sb.Append(new string('|', 2));
                }
            }

            // todo: parse events like ~1, ~2, ..., ~N and a way to test it out (presets/editor?)
            if (limit is not null && (Deltarune.DialogueKinds)Deltarune.DialogueType.SelectedIndex is Deltarune.DialogueKinds.Default)
            {
                //Console.WriteLine($"cPos:{cPos},i:{i}->{text[i]}->l:{limit}");
                if (cPos >= limit)
                {
                    //Console.WriteLine($"cPos:{cPos},tSpace:{tSpace},i:{i}->{text[i]},a:{AsteriskCheck}");
                    cPos = 0;
                    if (tSpace > 2)
                    {
                        i = tSpace - 1;
                        if (spaceSb >= 0)
                        {
                            //Console.WriteLine($"s:{sb.ToString()[..spaceSb]},i:{spaceSb}");
                            sb.Remove(spaceSb, sb.Length - spaceSb);
                        }
                        @char = null;
                        sb.Append('&');
                    }
                    else
                        sb.Append('&');
                    if (AsteriskCheck && autoAsterisk)
                    {
                        for (var j = 0; j < indentSize; j++)
                            UndrawPositions.Add(sb.Length + j);
                        sb.Append(new string('|', indentSize));
                    }
                    tSpace = -1;
                    spaceSb = -1;
                    //Console.WriteLine($"final cPos:{cPos}");
                }
                if (!Deltarune.IsNewlineSafe(i, text))
                {
                    cPos++;
                    //Console.WriteLine($"new cPos:{cPos},i:{i}->{text[i]},a:{AsteriskCheck}");
                }
            }
            if (@char is not null)
                sb.Append(@char.Value);
        }
        //Console.WriteLine($"================================{sb}");
        ParsedText = sb.ToString();
    }
}
