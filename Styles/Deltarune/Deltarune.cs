//css_reference System.Diagnostics.Process
//css_reference System.Console
//css_reference System.IO.Pipelines
//css_reference Microsoft.CSharp
//css_reference System.IO.Compression.ZipFile

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
using DialogueHelper.External;
using DialogueHelper.FileFormat;
using DialogueHelper.PreviewRenderer;
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
    public enum Boxes
    {
        Overworld,
        BattleIdle,
        BattleItemInfo,
        BattleDialogue,
        CMenuDescription, // no linewrap
        CMenuCharacterDescription, // no linewrap
        OverworldPink,
        PlatformerDiaglogue,
        ShopTalk,
        ShopTalkIdle, // wrong linewrap (33 in the code but should be much lower)
        ShopItemDescription, // no linewrap
        RecruitDescription,
        TennaBoard,
    }
    public enum DialogueKinds
    {
        Default,
        BattleIdle,
        Platformer,
        PlatformerAct, // no linewrap
        Pink,
        ShopSellEquippable,
        TennaBoard,
        DynamicWordwarp,
        None,
    }
    public enum PortraitKinds
    {
        None,
        ForceNone,
        Basic,
        SmallFlowers,
        SmallOther,
    }

    public enum WarningKind
    {
        NotFoundKey,
        DuplicatedKey,
        MissingKey,
    }

    public static CustomPropertyList<string> PortraitKind = new("Portrait Kind", "Portrait stype, sets the text offset as each specify", "portraitKind", ["None", "Force None", "Basic", "Small (Flowers)", "Small (Other)"], [], null, 0);
    public static CustomProperty<bool> DarkZone = new("Dark Zone (JP Only)", null, "darkZone", false);
    public static CustomPropertyList<string> DialogueType = new("Dialogue Type", "Line-wrapping style", "dialogueType", ["Default", "Battle Idle", "Platformer", "Platformer ACT", "Pink", "Shop Sell Equippable", "Tenna's Board", "Dynamic Wordwrap", "None"], [null, null, null, null, null, "This one doesn't actually change the line-wrapping style, but it'll change the box (if it's set correctly) to let you see the limit of that", null, "This is vastly the legacy line-wrapping system, this might be useful for certain custom boxes."], null, 0);

    public static bool EnvLastNewline;
    public static bool EnvStartedAsterisk;
    public static double EnvX;
    public static double EnvY;
    public static double EnvDynLimitWrap;
    public static int EnvSkip;
    public static bool EnvFirstDrawnChar;
    public static int EnvCheckedIndex;
    public static Stopwatch Stopwatch = Stopwatch.StartNew();
    public static bool IsRainbow;
    public static bool IsLastNewline;
    public ParsedString Parsed = new("", Boxes.Overworld);

    public static (double X, double Y) EnvPortraitOffset;
    public static (double X, double Y) EnvSmallFlowerPortraitOffset;
    public static (double X, double Y) EnvSmallOtherPortraitOffset;

    public static char[] Newlines = ['\n', '&', '#'];
    
    protected static string UtmtPath = $"{AppContext.BaseDirectory}/UndertaleModCli/";
			
    protected virtual byte AsteriskManualNewLineCompensation => 2;


    public override Task Init(CustomToolWindowHandler wh)
    {
        // This is called when the script is loaded and ready to go.
        //await wh.ShowDialog(new InfoWindow("Hello world!"));
        return Task.CompletedTask;
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
        if (!File.Exists(filePath) && await wh.ShowDialog<bool>(new QuestionWindow("Do you wish to automatically download UndertaleModTool's Cli on the folder?")))
        {
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
        var output = await DataWinToJsonPrompt(wh, lw);
        if (output is null)
            return;
        try
        {
            var (data, _, warnedKeys) = ExportJson(output, wh, lw);
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
            var ogParsed = new ParsedString(og, Boxes.Overworld);
            var parsed = new ParsedString(value, Boxes.Overworld);
            if (ogParsed.ParseWarns.Count != parsed.ParseWarns.Count)
            {
                sb.AppendLine($"Parse warnings for {key} (ID `{sc[key].Cont.Id}`) wasn't the same for the original ({ogParsed.ParseWarns.Count}) to the translated ({parsed.ParseWarns.Count}) text.\n");

                sb.AppendLine("|Old|New|\n|---|---|");
                var ogParseWarns = ogParsed.ParseWarns.Select(x => (x.Key, x.Value)).ToArray();
                var parseWarns = parsed.ParseWarns.Select(x => (x.Key, x.Value)).ToArray();
                for (var i = 0; i < int.Max(parseWarns.Length, ogParseWarns.Length); i++)
                {
                    sb.Append("|");
                    if (i < ogParseWarns.Length)
                        sb.Append($"{ogParseWarns[i].Key + 1}: {ogParseWarns[i].Value.Message}");
                    sb.Append("|");
                    if (i < parseWarns.Length)
                    {
                        var bold = i >= ogParseWarns.Length || parseWarns[i].Value.Argument != ogParseWarns[i].Value.Argument;
                        sb.Append($"{parseWarns[i].Key + 1}: {(bold ? "**" : "")}{parseWarns[i].Value.Message}{(bold ? "**" : "")}");
                    }
                    sb.AppendLine("|");
                }
                sb.AppendLine();
                warns++;
            }
            var ogParse = ogParsed.Events.Where(x => x.Value is not DialogueTimeSkip and not DialogueIgnore).Select(x => (x.Key, x.Value)).ToArray();
            var parse = parsed.Events.Where(x => x.Value is not DialogueTimeSkip and not DialogueIgnore).Select(x => (x.Key, x.Value)).ToArray();
            var valid = true;
            if (parse.Length != ogParse.Length)
                valid = false;
            else
            {
                for (var i = 0; i < parse.Length; i++)
                {
                    if (parse[i].Value.Argument != ogParse[i].Value.Argument)
                        valid = false;
                }
            }
            if (!valid)
            {
                sb.AppendLine($"Invalid events found for `{key}` (ID `{sc[key].Cont.Id}`) that weren't the same for the original ({ogParse.Length}) to the translated ({parse.Length}) text:\n");
                sb.AppendLine("|Old|New|\n|---|---|");
                for (var i = 0; i < int.Max(parse.Length, ogParse.Length); i++)
                {
                    sb.Append("|");
                    if (i < ogParse.Length)
                        sb.Append($"{ogParse[i].Key + 1}: {ogParse[i].Value.Argument}");
                    sb.Append("|");
                    if (i < parse.Length)
                    {
                        var bold = i >= ogParse.Length || parse[i].Value.Argument != ogParse[i].Value.Argument;
                        sb.Append($"{parse[i].Key + 1}: {(bold ? "**" : "")}{parse[i].Value.Argument}{(bold ? "**" : "")}");
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
        foreach (var (header, entries) in CalculateHeaders(output.Keys))
        {
            var s = fd.GetString(header);
            if (s is not null)
            {
                var unusedKeys = new List<(string Text, string Key)>();
                foreach (var key in entries)
                    unusedKeys.Add((output[key], key));
                foreach (var sc in s)
                {
                    if ((sc.Key ?? "").Length > 0)
                    {
                        var kf = unusedKeys.Index().FirstOrDefault(x => x.Item.Key == sc.Key, (Index: -1, Item: (null!, null!)));
                        if (kf.Index != -1)
                            unusedKeys.RemoveAt(kf.Index);
                    }
                    else if ((sc.Key ?? "").Length == 0)
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
            }
        }
        await wh.LoadRaw(fd);
        await wh.ShowDialog(new InfoWindow($"Updated data successfully.\n({entriesUpdated}/{entryCount} keys updated.)"));
    }

    public async Task UpdateDialogues(CustomToolWindowHandler wh)
    {
        if (!await wh.EnsureFileOpen())
            return;
        var oldOutput = await DataWinToJsonPrompt(wh, title: "Select the old game data file");
        if (oldOutput is null)
            return;
        var newOutput = await DataWinToJsonPrompt(wh, title: "Select the new game data file");
        if (newOutput is null)
            return;
        var resetEdited = await wh.ShowDialog<bool>(new QuestionWindow("Do you want to reset the texts that were changed between the 2 game versions to rectify the translation?"));
        var fd = wh.GetLoadedFileData()!;
        var lw = new LoadingWindow { ProgressBar = { IsIndeterminate = true, ShowProgressText = false } };
        wh.Show(lw);
        try
        {
            var oldHeaders = CalculateHeaders(oldOutput.Keys);
            var oldEj = ExportJson(oldOutput, wh, lw, fd, oldHeaders);
            var newHeaders = CalculateHeaders(oldOutput.Keys);
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
                    });
                }
                else if (oldGot && !newGot)
                {
                    // todo finish this thing idk
                }
            }
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
        }
        if (conts.Count > 0)
            fdFinal.InsertString(0, "__NO_KEY_FOUND__", conts);
        return fdFinal;
    }

    public async Task<OrderedDictionary<string, string>?> DataWinToJsonPrompt(CustomToolWindowHandler wh, LoadingWindow? lw = null, string? path = null, bool importToDataWin = false, string? title = "")
    {
        if (!await DownloadUndertaleModToolCli(wh))
            return null;
        if (path is null)
        {
            var f = await wh.GetStorageProvider().OpenFilePickerAsync(new()
            {
                Title = title ?? "Select the game data file",
                FileTypeFilter = [new("GameMaker's Data Files") { Patterns = ["*.win"] }, new("All files") { Patterns = ["*"] }],
            });
            if (f.Count <= 0)
                return null;
            path = f[0].Path.LocalPath.Replace("\"", "\\\"");
        }
        var args = $"load \"{path}\" -s ../";
        if (!importToDataWin)
        {
            File.Delete($"{wh.GetStylePath()}/output.json");
            args += "ExportStringsJson.csx";
        }
        else
            args += $"ImportStringsJson.csx -o \"{path}\"";
        using var proc = new Process
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
        var autohide = lw is null;
        if (lw is null)
        {
            lw = new LoadingWindow { ProgressBar = { IsIndeterminate = true, ShowProgressText = false } };
            wh.Show(lw);
        }
        try
        {
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
            if (importToDataWin)
                return [];
            else
            {
                var stream = new MemoryStream(await File.ReadAllBytesAsync($"{wh.GetStylePath()}/output.json"));
                var output = (await JsonSerializer.DeserializeAsync<OrderedDictionary<string, string>>(stream))!;
                await stream.DisposeAsync();

                return output;
            }
        }
        finally
        {
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
                // throw new InvalidDataException($"Unable to make a string group of {key}");
                pos = 0;
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
        if (data.UpdateKind is PreviewRenderer.UpdateKind.SelectedBoxChanged)
        {
            DialogueKinds? targetBox = null;
            switch ((Boxes)data.GetCurrentBox())
            {
                case Boxes.BattleIdle:
                    targetBox = DialogueKinds.BattleIdle;
                    break;
                case Boxes.PlatformerDiaglogue:
                    targetBox = DialogueKinds.Platformer;
                    break;
                case Boxes.OverworldPink:
                    targetBox = DialogueKinds.Pink;
                    break;
                case Boxes.RecruitDescription:
                    targetBox = DialogueKinds.DynamicWordwarp;
                    break;
                case Boxes.TennaBoard:
                    targetBox = DialogueKinds.TennaBoard;
                    break;
            }
            if (targetBox is not null && DialogueType.SelectedIndex != (int)targetBox)
            {
                DialogueType.SelectedIndex = (int)targetBox;
                DialogueType.UpdateUiValue();
                return true;
            }
        }
        switch ((Boxes)data.GetCurrentBox())
        {
            case Boxes.PlatformerDiaglogue:
                data.BoxImages[0].IsVisible = false;
                data.BoxImages[1].IsVisible = true;
                if (PortraitKind.SelectedIndex == (int)PortraitKinds.Basic)
                {
                    data.BoxImages[0].IsVisible = true;
                    data.BoxImages[1].IsVisible = false;
                }
                break;
            case Boxes.ShopItemDescription:
                data.BoxImages[0].IsVisible = true;
                data.BoxImages[1].IsVisible = false;
                if (DialogueType.SelectedIndex == (int)DialogueKinds.ShopSellEquippable)
                {
                    data.BoxImages[0].IsVisible = false;
                    data.BoxImages[1].IsVisible = true;
                }
                break;
        }
        EnvLastNewline = false;
        EnvStartedAsterisk = false;
        EnvX = -1;
        EnvY = -1;
        EnvSkip = 0;
        EnvFirstDrawnChar = true;
        EnvCheckedIndex = -1;
        IsRainbow = false;
        IsLastNewline = true;
        EnvDynLimitWrap = data.Style.ImageAssets[data.Box.Images[0].Path].Width * data.GetCurrentBoxScale();
        {
            var (x, y) = (0d, 0d);
            if (data.Box.CustomProperties.TryGetValue("PortraitOffset", out var bpo) && bpo is JsonElement po)
            {
                po[0].TryGetDouble(out x);
                po[1].TryGetDouble(out y);
            }
            EnvPortraitOffset = (x, y);
        }
        {
            var (x, y) = (0d, 0d);
            if (data.Box.CustomProperties.TryGetValue("SmallFlowerPortraitOffset", out var bpo) && bpo is JsonElement po)
            {
                po[0].TryGetDouble(out x);
                po[1].TryGetDouble(out y);
            }
            EnvSmallFlowerPortraitOffset = (x, y);
        }
        {
            var (x, y) = (0d, 0d);
            if (data.Box.CustomProperties.TryGetValue("SmallOtherPortraitOffset", out var bpo) && bpo is JsonElement po)
            {
                po[0].TryGetDouble(out x);
                po[1].TryGetDouble(out y);
            }
            EnvSmallOtherPortraitOffset = (x, y);
        }
        {
            if (data.Box.CustomProperties.TryGetValue("DynamicLimitWrap", out var bpo) && bpo is JsonElement po)
                po.TryGetDouble(out EnvDynLimitWrap);
        }
        var pk = (PortraitKinds)PortraitKind.SelectedIndex;
        if (pk > PortraitKinds.ForceNone)
        {
            data.Char.StartPosition = pk switch
            {
                PortraitKinds.Basic => EnvPortraitOffset,
                PortraitKinds.SmallFlowers => EnvSmallFlowerPortraitOffset,
                PortraitKinds.SmallOther => EnvSmallOtherPortraitOffset,
                _ => (0, 0),
            };
        }
        Parsed = new(data.Char.String, (Boxes)data.GetCurrentBox());
        return await base.PrepareDraw(wh, data);
    }

    public static bool IsNewlineSafe(int idx, string str) => Newlines.Contains(str[idx]) && str[int.Max(0, idx - 1)] != '`';

    public override async Task<bool> DrawGlyph(CustomToolWindowHandler wh, ExternalData data)
    {
        if (Newlines.Contains(data.Char.Char))
            data.Char.IsNewline = true;
        if (data.Char.IsIgnore || (!data.Font.GlyphDictionary.ContainsKey(data.Char.Char) && !data.Char.IsNewline))
            return false;
        if (EnvSkip > 0)
        {
            EnvSkip--;
            return false;
        }

        var font = (Fonts)data.GetCurrentFont();
        var box = (Boxes)data.GetCurrentBox();

        if (Parsed.Events.TryGetValue(data.Char.Index, out var ev))
        {
            switch (ev)
            {
                case DialogueEnd end:
                    EnvSkip = end.Argument.Length - 1;
                    return false;
                case DialogueIgnore:
                    return false;
                case DialogueIndexPortrait or DialogueCharPortrait:
                    if (!Parsed.ParseWarns.ContainsKey(data.Char.Index))
                    {
                        if (PortraitKind.SelectedIndex == (int)PortraitKinds.None)
                        {
                            PortraitKind.SelectedIndex = (int)PortraitKinds.Basic;
                            PortraitKind.UpdateUiValue();
                            return true;
                        }
                        EnvSkip = ev.Argument.Length - 1;
                        return false;
                    }
                    break;
                case DialogueFonts fnt:
                    if (!Parsed.ParseWarns.ContainsKey(data.Char.Index) && fnt.Font is not null)
                    {
                        data.Font = data.Style.FontMetadata[(int)fnt.Font.Value];
                        EnvSkip = ev.Argument.Length - 1;
                        return false;
                    }
                    break;
                case DialogueColor col:
                    IsRainbow = false;
                    if (!Parsed.ParseWarns.ContainsKey(data.Char.Index))
                    {
                        if (col.Color is not null)
                            data.Glyph.Color = col.Color.Value;
                        else if (col.Rainbow)
                            IsRainbow = true;
                        EnvSkip = ev.Argument.Length - 1;
                        if (col.Color is not null || col.Rainbow)
                            return false;
                    }
                    break;
                default:
                    if (!Parsed.ParseWarns.ContainsKey(data.Char.Index))
                    {
                        EnvSkip = ev.Argument.Length - 1;
                        return false;
                    }
                    break;
            }
        }

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
                switch (box)
                {
                    case Boxes.BattleIdle:
                        EnvY = 14;
                        break;
                    case Boxes.BattleItemInfo:
                        EnvX = 7;
                        EnvY = 16;
                        break;
                    case Boxes.CMenuDescription or Boxes.CMenuCharacterDescription or Boxes.ShopItemDescription:
                        EnvX = -1; // drawn by gamemaker
                        EnvY = -1;
                        break;
                }
                if ((DialogueKinds)DialogueType.SelectedIndex is DialogueKinds.PlatformerAct)
                {
                    EnvX = -1; // drawn by gamemaker
                    EnvY = -1;
                }
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
        var actAsNewline = IsNewlineSafe(data.Char.Index, data.Char.String) || Parsed.ActAsNewline.Contains(data.Char.Index);
        var addSpaceXNewline = 0;
        if ((DialogueKinds)DialogueType.SelectedIndex is DialogueKinds.DynamicWordwarp && data.Char.Index > EnvCheckedIndex)
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
            {
                actAsNewline = true;
                if (EnvStartedAsterisk)
                    addSpaceXNewline = 2;
            }
        }

        IsLastNewline = actAsNewline;
        if (actAsNewline)
        {
            EnvLastNewline = true;
            if (addSpaceXNewline <= 0 && EnvStartedAsterisk && (Parsed.TextTrimLength - data.Char.Index < 2 || (Parsed.TextTrimLength - data.Char.Index >= 2 && data.Char.String[data.Char.Index + 1] != '*')))
                addSpaceXNewline = AsteriskManualNewLineCompensation;
            data.Char.PositionOffset.X = 0;
            if (addSpaceXNewline > 0)
            {
                var glyph = data.Font.GlyphDictionary['|'];
                data.Char.PositionOffset.X = ((EnvX != -1 ? EnvX : glyph.Shift) + glyph.Offset) * 2 * addSpaceXNewline;
            }

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
            var glyph = data.Font.GlyphDictionary[data.Char.Char];
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
            data.DrawGlyph();
            if ((EnvLastNewline || EnvFirstDrawnChar) && data.Char.Char == '*')
                EnvStartedAsterisk = true;
            data.Char.PositionOffset.X += (int)double.Ceiling(EnvX != -1 ? EnvX : (glyph.Shift + glyph.Offset)) * 2;
            EnvLastNewline = false;
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
    public Dictionary<int, ParsedEvent> Events = [];
    public Dictionary<int, ParseWarn> ParseWarns = [];
    public HashSet<int> ActAsNewline = [];

    public ParsedString(string text, Deltarune.Boxes box)
    {
        Text = text;
        var trimLen = text.TrimEnd().Length;
        TextTrimLength = trimLen;
        var sb = new StringBuilder();
        var tSpace = -1;
        var cPos = 0;
        var asteriskCheck = false;
        var lastIsNewLine = true;
        for (var i = 0; i < trimLen; i++)
        {
            var @char = text[i];
            if (@char is '`')
            {
                Events.Add(i, new DialogueIgnore(@char.ToString()));
                i++;
                continue;
            }
            if (trimLen - i >= 3)
            {
                var sStr = text[i..(i + 3)];
                if (sStr == "/%%")
                {
                    Events.Add(i, new DialogueEnd(sStr));
                    i += 2;
                    continue;
                }
            }
            if (trimLen - i >= 2)
            {
                var sStr = text[i..(i + 2)];
                if (sStr == "/%" || sStr == "%%" || (sStr[0] == '^' && char.IsNumber(sStr[1])))
                {
                    Events.Add(i, sStr[0] == '^' ? new DialogueTimeSkip(sStr) : new DialogueEnd(sStr));
                    i++;
                    continue;
                }
            }
            if (((trimLen - i == 1 || i == 0) && @char == '/') || (trimLen - i == 1 && @char == '%'))
            {
                Events.Add(i, new DialogueEnd(@char.ToString()));
                continue;
            }
            if (@char == '\\')
            {
                var valid = true;
                if (trimLen - i >= 3)
                {
                    var fullSlice = text[i..(i + 3)];
                    var slice = text[(i + 1)..(i + 3)];
                    switch (slice[0])
                    {
                        case 'E':
                            if (!"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".Contains(slice[1]))
                                ParseWarns.Add(i, new ParseWarn(fullSlice, $"invalid choice {slice[1]} for setting index portrait: {fullSlice}"));
                            Events.Add(i, new DialogueIndexPortrait(fullSlice));
                            break;
                        case 'F':
                            if (!"0SRNTLsUAaBubrUKQC◘yi".Contains(slice[1]))
                                ParseWarns.Add(i, new ParseWarn(fullSlice, $"invalid choice {slice[1]} for setting character portrait: {fullSlice}"));
                            Events.Add(i, new DialogueCharPortrait(fullSlice));
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
                                    ParseWarns.Add(i, new ParseWarn(fullSlice, $"invalid choice {slice[1]} for setting font: {fullSlice}"));
                                Events.Add(i, new DialogueFonts(fullSlice) { Font = f });
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
                                    ParseWarns.Add(i, new ParseWarn(fullSlice, $"invalid choice {slice[1]} for setting color: {fullSlice}"));
                                Events.Add(i, new DialogueColor(fullSlice) { Color = col, Rainbow = rnb });
                            }
                            break;
                        case 'V':
                            {
                                if (!"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZzabcdefghijklmnopqrstuvwxy".Contains(slice[1]))
                                    ParseWarns.Add(i, new ParseWarn(fullSlice, $"invalid voice-clip selected: {slice[1]}"));
                                Events.Add(i, new DialogueFloweryVoiceClip(fullSlice));
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
                                                ParseWarns.Add(i, new ParseOutOfRange(fullSlice, 0..2));
                                            Events.Add(i, new DialogueVoiceClipMode(fullSlice));
                                            break;
                                        case 's':
                                            if (v < 0 || v > 1)
                                                ParseWarns.Add(i, new ParseOutOfRange(fullSlice, 0..1));
                                            Events.Add(i, new DialogueSkippable(fullSlice));
                                            break;
                                        case 'C':
                                            if (v < 1 || v > 4)
                                                ParseWarns.Add(i, new ParseOutOfRange(fullSlice, 1..4));
                                            Events.Add(i, new DialogueChoicer(fullSlice));
                                            break;
                                        case 'M':
                                            Events.Add(i, new DialogueFlag20(fullSlice));
                                            break;
                                        case 'S':
                                            Events.Add(i, new DialogueSound(fullSlice));
                                            break;
                                        case 'I':
                                            Events.Add(i, new DialogueSprite(fullSlice));
                                            break;
                                        case 'm':
                                            Events.Add(i, new DialogueMiniFace(fullSlice));
                                            break;
                                        case 'O':
                                            Events.Add(i, new DialogueObject(fullSlice));
                                            break;
                                        case 'f':
                                            Events.Add(i, new DialogueInstanceUpdate(fullSlice));
                                            break;
                                        default:
                                            ParseWarns.Add(i, new ParseWarn(fullSlice, $"unknown parameter: {fullSlice}"));
                                            break;
                                    }
                                }
                            }
                            break;
                    }
                }
                if (valid)
                {
                    i += 2;
                    continue;
                }
            }
            // todo: parse events like ~1, ~2, ..., ~N and a way to test it out (presets/editor?)
            var dk = (Deltarune.DialogueKinds)Deltarune.DialogueType.SelectedIndex;
            if (box is not Deltarune.Boxes.CMenuDescription and not Deltarune.Boxes.CMenuCharacterDescription and not Deltarune.Boxes.ShopItemDescription && dk is not Deltarune.DialogueKinds.PlatformerAct and not Deltarune.DialogueKinds.DynamicWordwarp and not Deltarune.DialogueKinds.None)
            {
                if (lastIsNewLine && text[i] == '*')
                    asteriskCheck = true;
                lastIsNewLine = false;
                if (@char == ' ')
                    tSpace = i + 1;
                else if (Deltarune.IsNewlineSafe(i, text))
                {
                    tSpace = -1;
                    cPos = 0;
                    lastIsNewLine = true;
                }
                var pt = (Deltarune.PortraitKinds)Deltarune.PortraitKind.SelectedIndex;
                var limit = dk switch
                {
                    Deltarune.DialogueKinds.BattleIdle => (pt > Deltarune.PortraitKinds.ForceNone) ? 29 : 37,
                    Deltarune.DialogueKinds.Platformer => pt == Deltarune.PortraitKinds.SmallFlowers ? 33 : 29,
                    Deltarune.DialogueKinds.Pink => 23,
                    Deltarune.DialogueKinds.TennaBoard => 22,
                    _ => pt switch
                    {
                        Deltarune.PortraitKinds.Basic => 26,
                        Deltarune.PortraitKinds.SmallFlowers or Deltarune.PortraitKinds.SmallOther => 29,
                        _ => 33,
                    }
                };
                //Console.WriteLine($"cPos:{cPos},i:{i}->{text[i]}");
                while (cPos++ >= limit)
                {
                    //Console.WriteLine($"cPos:{cPos},tSpace:{tSpace},i:{i}->{text[i]},a:{asteriskCheck}");
                    ActAsNewline.Add(tSpace > 2 ? tSpace : i);
                    cPos = tSpace > 2 ? i - tSpace : 0;
                    if (asteriskCheck)
                        cPos += 2;
                    tSpace = -1;
                    //Console.WriteLine($"new cPos:{cPos}");
                }
            }
            sb.Append(@char);
        }
        ParsedText = sb.ToString();
    }
}
