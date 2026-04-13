//css_reference System.Diagnostics.Process
//css_reference System.Console
//css_reference System.IO.Pipelines
//css_reference Microsoft.CSharp

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using DialogueHelper.External;
using DialogueHelper.FileFormat;
using DialogueHelper.PreviewRenderer;
using DialogueHelper.Views;

public class Deltarune : StyleScript
{
    public enum Fonts
    {
        Main,
        MainBig,
        Small,
        ComicSans,
        Dotumche,
        TinyNoelle,
        JaMain,
        JaMainBig,
        JaSmall,
        JaComicSans,
        JaDotumche,
        JaKakugo,
        JaTinyNoelle,
    }

    public enum WarningKind
    {
        MissingKey,
        DuplicatedKey,
        UnusedKey,
    }

    public static CustomProperty<bool> EnablePortrait = new("Enable Portrait", "enablePortrait", false);
    public static CustomProperty<bool> DarkZone = new("Dark Zone (JP Only)", "darkZone", false);

    public static bool EnvLastNewline;
    public static bool EnvStartedAsterisk;
    public static double EnvX;
    public static double EnvY;
    public static int EnvSkip;
    public static bool EnvFirstDrawnChar;
    public static int EnvCheckedIndex;
    public ParsedString Parsed = new("");

    public static (double X, double Y) EnvPortraitOffset;


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
            EnablePortrait,
            DarkZone,
        ]);
    }

    public override Task<IEnumerable<CustomTool>> RegisterCustomTools()
    {
        return Task.FromResult<IEnumerable<CustomTool>>([
            new("Import from data.win", ImportFromDataWin, "Ctrl+Shift+I"),
            new("Export to data.win", ExportToDataWin, "Ctrl+Shift+E"),
            new("Import from json file", ImportFromJson, "Ctrl+Alt+I"),
            new("Export to json file", ExportToJson, "Ctrl+Alt+E"),
            new("Verify dialogues integrity", VerifyFileDialogueIntegrity, "Ctrl+Shift+P"),
        ]);
    }

    public async Task ImportFromDataWin(CustomToolWindowHandler wh)
    {
        if (wh.GetLoadedFileData() is not null)
        {
            var w = new InfoWindow("Please ensure that you do not have a file open.");
            await wh.ShowDialog(w);
            return;
        }
        var filePath = $"{wh.GetStylePath()}/UndertaleModCli/UndertaleModCli{(OperatingSystem.IsWindows() ? ".exe" : "")}";
        if (!File.Exists(filePath))
        {
            var w = new InfoWindow("Please download and add UndertaleModCli to this style's folder.");
            await wh.ShowDialog(w);
            return;
        }
        var f = await wh.GetStorageProvider().OpenFilePickerAsync(new()
        {
            Title = "Select the game data file",
            FileTypeFilter = [new("GameMaker's Data Files") { Patterns = ["data.win"] }, new("All files") { Patterns = ["*"] }],
        });
        if (f.Count <= 0)
            return;
        File.Delete($"{wh.GetStylePath()}/output.json");
        using var proc = new Process
        {
            StartInfo =
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = filePath,
                Arguments = $"load \"{f[0].Path.LocalPath.Replace("\"", "\\\"")}\" -s ../ExportStringsJson.csx",
                WorkingDirectory = $"{wh.GetStylePath()}/UndertaleModCli",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            },
        };
        proc.Start();
        var lw = new LoadingWindow { ProgressBarView = { IsIndeterminate = true, ShowProgressText = false } };
        wh.Show(lw);
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
                lw.TextView.Text = line.Trim();
            }
            var stderr = await proc.StandardError.ReadToEndAsync();

            Console.WriteLine($"stdout:\n{stdout}\nstderr:\n{stderr}");
            if (proc.ExitCode != 0 || !File.Exists($"{wh.GetStylePath()}/output.json"))
            {
                lw.Close();
                var w = new InfoWindow($"UndertaleModCli exited with code {proc.ExitCode}\n\nstdout:\n{stdout}\nstderr:\n{stderr}");
                await wh.ShowDialog(w);
                return;
            }
            var stream = new MemoryStream(await File.ReadAllBytesAsync($"{wh.GetStylePath()}/output.json"));
            var output = (await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream))!;
            await stream.DisposeAsync();
            LoadJson(output, wh);
        }
        finally
        {
            File.Delete($"{wh.GetStylePath()}/output.json");
            lw.Close();
        }
        await wh.ShowDialog(new InfoWindow($"Imported data successfully."));
    }

    public async Task ExportToDataWin(CustomToolWindowHandler wh)
    {
        if (wh.GetLoadedFileData() is null)
        {
            await wh.ShowDialog(new InfoWindow("Please ensure that you have a file open."));
            return;
        }
        var filePath = $"{wh.GetStylePath()}/UndertaleModCli/UndertaleModCli{(OperatingSystem.IsWindows() ? ".exe" : "")}";
        if (!File.Exists(filePath))
        {
            await wh.ShowDialog(new InfoWindow("Please download and add UndertaleModCli to this style's folder."));
            return;
        }
        var f = await wh.GetStorageProvider().OpenFilePickerAsync(new()
        {
            Title = "Select the game data file",
            FileTypeFilter = [new("GameMaker's Data Files") { Patterns = ["data.win"] }, new("All files") { Patterns = ["*"] }],
        });
        if (f.Count <= 0)
            return;
        var lw = new LoadingWindow { ProgressBarView = { IsIndeterminate = true, ShowProgressText = false } };
        wh.Show(lw);
        try
        {
            var stream = new MemoryStream(await File.ReadAllBytesAsync(Path.Combine(f[0].Path.LocalPath, "../lang/lang_ja.json")));
            var baseJson = (await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream))!;
            await stream.DisposeAsync();
            var file = File.Create($"{wh.GetStylePath()}/input.json");
            var (data, sc, warnedKeys) = ExportJson(baseJson, wh);
            await JsonSerializer.SerializeAsync(file, data, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            await file.DisposeAsync();
            var targetPath = $"\"{f[0].Path.LocalPath.Replace("\"", "\\\"")}\"";
            using var proc = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = filePath,
                    Arguments = $"load {targetPath} -s ../ImportStringsJson.csx -o {targetPath}",
                    WorkingDirectory = $"{wh.GetStylePath()}/UndertaleModCli",
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
                stdout.AppendLine(line);
                lw.TextView.Text = line.Trim();
            }
            var stderr = await proc.StandardError.ReadToEndAsync();

            Console.WriteLine($"stdout:\n{stdout}\nstderr:\n{stderr}");
            if (proc.ExitCode != 0 || stderr.Trim().Length > 0)
            {
                lw.Close();
                await wh.ShowDialog(new InfoWindow($"UndertaleModCli exited with code {proc.ExitCode}\n\nstdout:\n{stdout}\nstderr:\n{stderr}"));
                return;
            }
            if (warnedKeys.Count > 0)
            {
                lw.Hide();
                var sb = new StringBuilder();
                sb.AppendLine("Unable to be found entries to save (ignored in the final file):\n");
                foreach (var (key, cont, warn) in warnedKeys)
                {
                    if (warn == WarningKind.UnusedKey)
                    {
                        sb.AppendLine($"- Unused key: `{key}`\n  - Text: `{(cont?.Text ?? cont?.OriginalText ?? "").Replace("\r", "\\r").Replace("\n", "\\n")}`\n");
                        continue;
                    }
                    sb.AppendLine($"- {(warn == WarningKind.DuplicatedKey ? "Duplicate key found" : "Key not found")} `{key}` (ID `{cont!.Id}`)");
                    sb.AppendLine($"  - Original Text: `{cont.OriginalText}`");
                    if (cont.Text is not null)
                        sb.AppendLine($"  - Translated Text: `{cont.Text}`");
                    sb.AppendLine();
                }
                sb.AppendLine($"Found {warnedKeys.Count} warning(s).");
                await wh.ShowDialog<bool>(new TextBoxWindow($"There {(warnedKeys.Count == 1 ? "was" : "were")} {warnedKeys.Count} warned {(warnedKeys.Count == 1 ? "key" : "keys")} found.", sb.ToString(), [new("Markdown file") { Patterns = [new("*.md")] }]));
                lw.Show();
            }
        }
        finally
        {
            File.Delete($"{wh.GetStylePath()}/input.json");
            lw.Close();
        }
        await wh.ShowDialog(new InfoWindow($"Exported data successfully."));
    }

    public async Task ImportFromJson(CustomToolWindowHandler wh)
    {
        if (wh.GetLoadedFileData() is not null)
        {
            await wh.ShowDialog(new InfoWindow("Please ensure that you do not have a file open."));
            return;
        }
        var f = await wh.GetStorageProvider().OpenFilePickerAsync(new()
        {
            Title = "Select a file",
            FileTypeFilter = [new("JSON Files") { Patterns = ["*.json"] }, new("All Files") { Patterns = ["*"] }],
        });
        if (f.Count <= 0)
            return;
        var lw = new LoadingWindow { ProgressBarView = { IsIndeterminate = true, ShowProgressText = false } };
        wh.Show(lw);
        try
        {
            var stream = await f[0].OpenReadAsync();
            var output = (await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream))!;
            await stream.DisposeAsync();
            LoadJson(output, wh);
        }
        finally
        {
            lw.Close();
        }
        await wh.ShowDialog(new InfoWindow($"Imported data successfully."));
    }

    public async Task ExportToJson(CustomToolWindowHandler wh)
    {
        if (wh.GetLoadedFileData() is null)
        {
            await wh.ShowDialog(new InfoWindow("Please ensure that you have a file open."));
            return;
        }
        var filePath = $"{wh.GetStylePath()}/UndertaleModCli/UndertaleModCli{(OperatingSystem.IsWindows() ? ".exe" : "")}";
        if (!File.Exists(filePath))
        {
            var w = new InfoWindow("Please download and add UndertaleModCli to this style's folder.");
            await wh.ShowDialog(w);
            return;
        }
        var f = await wh.GetStorageProvider().OpenFilePickerAsync(new()
        {
            Title = "Select the game data file",
            FileTypeFilter = [new("GameMaker's Data Files") { Patterns = ["data.win"] }, new("All files") { Patterns = ["*"] }],
        });
        if (f.Count <= 0)
            return;
        var fSave = await wh.GetStorageProvider().SaveFilePickerAsync(new()
        {
            Title = "Select a file to save to",
            FileTypeChoices = [new("JSON Files") { Patterns = ["*.json"] }],
        });
        if (fSave is null)
            return;
        File.Delete($"{wh.GetStylePath()}/output.json");
        using var proc = new Process
        {
            StartInfo =
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = filePath,
                Arguments = $"load \"{f[0].Path.LocalPath.Replace("\"", "\\\"")}\" -s ../ExportStringsJson.csx",
                WorkingDirectory = $"{wh.GetStylePath()}/UndertaleModCli",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            },
        };
        proc.Start();
        var lw = new LoadingWindow { ProgressBarView = { IsIndeterminate = true, ShowProgressText = false } };
        wh.Show(lw);
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
                lw.TextView.Text = line.Trim();
            }
            var stderr = await proc.StandardError.ReadToEndAsync();

            Console.WriteLine($"stdout:\n{stdout}\nstderr:\n{stderr}");
            if (proc.ExitCode != 0 || !File.Exists($"{wh.GetStylePath()}/output.json"))
            {
                lw.Close();
                var w = new InfoWindow($"UndertaleModCli exited with code {proc.ExitCode}\n\nstdout:\n{stdout}\nstderr:\n{stderr}");
                await wh.ShowDialog(w);
                return;
            }
            var outputStream = File.OpenRead($"{wh.GetStylePath()}/output.json");
            var output = (await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(outputStream))!;
            await outputStream.DisposeAsync();
            var stream = await fSave.OpenWriteAsync();
            var (data, sc, warnedKeys) = ExportJson(output, wh);
            await JsonSerializer.SerializeAsync(stream, data, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            await stream.DisposeAsync();
            if (warnedKeys.Count > 0)
            {
                lw.Hide();
                var sb = new StringBuilder();
                sb.AppendLine("Unable to be found entries to save (ignored in the final file):\n");
                foreach (var (key, cont, warn) in warnedKeys)
                {
                    if (warn == WarningKind.UnusedKey)
                    {
                        sb.AppendLine($"- Unused key: `{key}`\n  - Text: `{(cont?.Text ?? cont?.OriginalText ?? "").Replace("\r", "\\r").Replace("\n", "\\n")}`\n");
                        continue;
                    }
                    sb.AppendLine($"- {(warn == WarningKind.DuplicatedKey ? "Duplicate key found" : "Key not found")} `{key}` (ID `{cont!.Id}`)");
                    sb.AppendLine($"  - Original Text: `{cont.OriginalText}`");
                    if (cont.Text is not null)
                        sb.AppendLine($"  - Translated Text: `{cont.Text}`");
                    sb.AppendLine();
                }
                sb.AppendLine($"Found {warnedKeys.Count} warning(s).");
                await wh.ShowDialog<bool>(new TextBoxWindow($"There {(warnedKeys.Count == 1 ? "was" : "were")} {warnedKeys.Count} warned {(warnedKeys.Count == 1 ? "key" : "keys")} found.", sb.ToString(), [new("Markdown file") { Patterns = [new("*.md")] }]));
                lw.Show();
            }
        }
        finally
        {
            File.Delete($"{wh.GetStylePath()}/output.json");
            lw.Close();
        }
        await wh.ShowDialog(new InfoWindow($"Exported data successfully."));
    }

    public async Task VerifyFileDialogueIntegrity(CustomToolWindowHandler wh)
    {
        if (wh.GetLoadedFileData() is null)
        {
            await wh.ShowDialog(new InfoWindow("Please ensure that you have a file open."));
            return;
        }
        var filePath = $"{wh.GetStylePath()}/UndertaleModCli/UndertaleModCli{(OperatingSystem.IsWindows() ? ".exe" : "")}";
        if (!File.Exists(filePath))
        {
            var w = new InfoWindow("Please download and add UndertaleModCli to this style's folder.");
            await wh.ShowDialog(w);
            return;
        }
        var f = await wh.GetStorageProvider().OpenFilePickerAsync(new()
        {
            Title = "Select the game data file",
            FileTypeFilter = [new("GameMaker's Data Files") { Patterns = ["data.win"] }, new("All files") { Patterns = ["*"] }],
        });
        if (f.Count <= 0)
            return;
        using var proc = new Process
        {
            StartInfo =
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = filePath,
                Arguments = $"load \"{f[0].Path.LocalPath.Replace("\"", "\\\"")}\" -s ../ExportStringsJson.csx",
                WorkingDirectory = $"{wh.GetStylePath()}/UndertaleModCli",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            },
        };
        proc.Start();
        var lw = new LoadingWindow { ProgressBarView = { IsIndeterminate = true, ShowProgressText = false } };
        wh.Show(lw);
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
                lw.TextView.Text = line.Trim();
            }
            var stderr = await proc.StandardError.ReadToEndAsync();

            Console.WriteLine($"stdout:\n{stdout}\nstderr:\n{stderr}");
            if (proc.ExitCode != 0 || !File.Exists($"{wh.GetStylePath()}/output.json"))
            {
                lw.Close();
                var w = new InfoWindow($"UndertaleModCli exited with code {proc.ExitCode}\n\nstdout:\n{stdout}\nstderr:\n{stderr}");
                await wh.ShowDialog(w);
                return;
            }
            var outputStream = File.OpenRead($"{wh.GetStylePath()}/output.json");
            var output = (await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(outputStream))!;
            await outputStream.DisposeAsync();
            var (data, sc, warnedKeys) = ExportJson(output, wh);
            var sb = new StringBuilder();
            sb.AppendLine("# Warnings\n");
            var warns = 0;
            foreach (var (key, value) in data)
            {
                if (!output.TryGetValue(key, out var og))
                    continue;
                var ogParsed = new ParsedString(og);
                var parsed = new ParsedString(value);
                if (ogParsed.ParseWarns.Count != parsed.ParseWarns.Count)
                {
                    sb.AppendLine($"Parse warnings for {key} (ID `{sc[key].Id}`) wasn't the same for the original ({ogParsed.ParseWarns.Count}) to the translated ({parsed.ParseWarns.Count}) text.\n");

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
                    sb.AppendLine($"Invalid events found for `{key}` (ID `{sc[key].Id}`) that weren't the same for the original ({ogParse.Length}) to the translated ({parse.Length}) text:\n");
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
                    if (warn == WarningKind.UnusedKey)
                    {
                        sb.AppendLine($"- Unused key: `{key}`\n  - Text: `{(cont?.Text ?? cont?.OriginalText ?? "").Replace("\r", "\\r").Replace("\n", "\\n")}`\n");
                        continue;
                    }
                    sb.AppendLine($"- {(warn == WarningKind.DuplicatedKey ? "Duplicate key found" : "Key not found")} `{key}` (ID `{cont!.Id}`)");
                    sb.AppendLine($"  - Original Text: `{cont.OriginalText}`");
                    if (cont.Text is not null)
                        sb.AppendLine($"  - Translated Text: `{cont.Text}`");
                    sb.AppendLine();
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
        finally
        {
            File.Delete($"{wh.GetStylePath()}/output.json");
            lw.Close();
        }
    }

    public Dictionary<string, List<string>> CalculateHeaders(IEnumerable<string> keys)
    {
        var headers = new Dictionary<string, List<string>>();
        foreach (var key in keys)
        {
            if (key == "date")
                continue;
            var pos = key.LastIndexOf("_gml_");
            if (pos == -1)
                pos = key.LastIndexOf('_');
            if (pos == -1)
                throw new InvalidDataException($"Unable to make a string group of {key}");
            var header = key[..pos];
            headers.TryAdd(header, []);
            headers[header].Add(key);
        }
        return headers;
    }

    public void LoadJson(Dictionary<string, string> json, CustomToolWindowHandler wh)
    {
        var sb = new StringBuilder();
        foreach (var (header, entries) in CalculateHeaders(json.Keys))
        {
            sb.Append($"0;ID:{FormatEntry.GetSimpleUri(header)}\n");
            foreach (var key in entries)
                sb.Append($"1;OriginalText:{FormatEntry.GetSimpleUri(json[key])};Key:{FormatEntry.GetSimpleUri(key)}\n");
        }
        sb.Append("8;");
        wh.LoadString(sb.ToString());
    }

    public (Dictionary<string, string> data, Dictionary<string, StringContainer> cont, List<(string key, StringContainer? cont, WarningKind warnKind)> warnedKeys) ExportJson(Dictionary<string, string> json, CustomToolWindowHandler wh)
    {
        var data = new Dictionary<string, string> { ["date"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() };
        var warnedKeys = new List<(string, StringContainer?, WarningKind)>();
        var sc = new Dictionary<string, StringContainer>();
        var file = wh.GetLoadedFileData()!;
        var headers = CalculateHeaders(json.Keys);
        foreach (var (key, conts) in file.Strings)
        {
            foreach (var (_, cont) in conts.OrderBy(x => x.Key))
            {
                string? targetKey = null;
                if (cont.Key is not null)
                    targetKey = cont.Key;
                else if (headers.TryGetValue(key, out var keys))
                    targetKey = keys.FirstOrDefault(x => x is not null && !data.ContainsKey(x) && json[x] == cont.OriginalText, null);
                if (targetKey is not null)
                {
                    if (!data.TryAdd(targetKey, cont.Text ?? cont.OriginalText))
                    {
                        warnedKeys.Add((key, cont, WarningKind.DuplicatedKey));
                        Console.WriteLine($"duplicate key found for {cont.Id} ({key}) with text \"{cont.OriginalText}\" (edited: \"{cont.Text}\")!");
                    }
                    else
                        sc.Add(targetKey, cont);
                }
                else
                {
                    warnedKeys.Add((key, cont, WarningKind.MissingKey));
                    Console.WriteLine($"no key found for {cont.Id} ({key}) with text \"{cont.OriginalText}\" (edited: \"{cont.Text}\")!");
                }
            }
        }
        foreach (var (key, value) in json)
        {
            if (!data.ContainsKey(key))
                warnedKeys.Add((key, null, WarningKind.UnusedKey));
        }
        return (data, sc, warnedKeys);
    }

    public override async Task<bool> PrepareDraw(CustomToolWindowHandler wh, ExternalData data)
    {
        EnvLastNewline = false;
        EnvStartedAsterisk = false;
        EnvX = -1;
        EnvY = -1;
        EnvSkip = 0;
        EnvFirstDrawnChar = true;
        EnvCheckedIndex = -1;
        var lro = EnablePortrait.ReadOnly;
        EnablePortrait.ReadOnly = !data.Box.CustomProperties.TryGetValue("SupportsPortrait", out var val) || ((JsonElement)val).ToString().ToLowerInvariant() != "true";
        if (lro != EnablePortrait.ReadOnly)
            EnablePortrait.UpdateUiValue();
        var (x, y) = (0d, 0d);
        if (data.Box.CustomProperties.TryGetValue("PortraitOffset", out var bpo) && bpo is JsonElement po)
        {
            po[0].TryGetDouble(out x);
            po[1].TryGetDouble(out y);
        }
        EnvPortraitOffset = (x, y);
        if (EnablePortrait is { Value: true, ReadOnly: false })
            data.Char.StartPosition = EnvPortraitOffset;
        if (Parsed.Text != data.Char.String)
            Parsed = new(data.Char.String);
        return await base.PrepareDraw(wh, data);
    }

    public bool IsNewlineSafe(int idx, string str, ExternalData data) => data.Style.Metadata.NewLines.Contains(str[idx]) && str[int.Max(0, idx - 1)] != '`';

    public override async Task<bool> DrawGlyph(CustomToolWindowHandler wh, ExternalData data)
    {
        if (data.Char.IsIgnore || (!data.Font.GlyphDictionary.ContainsKey(data.Char.Char) && !data.Char.IsNewline))
            return false;
        if (EnvSkip > 0)
        {
            EnvSkip--;
            return false;
        }

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
                        if (EnablePortrait is { ReadOnly: false, Value: false })
                        {
                            EnablePortrait.Value = true;
                            EnablePortrait.UpdateUiValue();
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
                    if (!Parsed.ParseWarns.ContainsKey(data.Char.Index) && col.Color is not null)
                    {
                        data.Glyph.Color = col.Color.Value;
                        EnvSkip = ev.Argument.Length - 1;
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

        switch ((Fonts)data.Style.FontMetadata.Index().First(x => x.Item == data.Font).Index)
        {
            case Fonts.Main:
                EnvX = 8;
                EnvY = 18;
                break;
            case Fonts.MainBig:
                EnvX = 16;
                EnvY = 36;
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
            default:
                EnvX = data.Font.GlyphDictionary[data.Char.Char].Shift;
                break;
        }
        if ((Enum.GetName((Fonts)data.GetCurrentFont()) ?? "").Contains("jp", StringComparison.InvariantCultureIgnoreCase) && data.Char.Char <= 0xff || (data.Char.Char >= 65377 && data.Char.Char <= 65439))
        {
            EnvX /= 2;
            EnvX += DarkZone.Value ? 16 : 8;
        }
        else if ((Fonts)data.GetCurrentFont() == Fonts.ComicSans)
        {
            switch (data.Char.Char)
            {
                case 'w': EnvX += 2; break;
                case 'm': EnvX += 3; break;
                case 'i' or 'l': EnvX -= 2; break;
                case 's' or 'j': EnvX--; break;
            }
        }
        var actAsNewline = IsNewlineSafe(data.Char.Index, data.Char.String, data);
        var addSpaceXNewline = 0;
        if (data.Char.Index > EnvCheckedIndex)
        {
            if (EnvCheckedIndex == -1)
                EnvCheckedIndex = 0;
            var i = 0;
            var fs = "";
            while (data.Char.Index + i < Parsed.TextTrimLength && (data.Char.String[data.Char.Index + i] == ' ' || IsNewlineSafe(data.Char.Index + i, data.Char.String, data)))
            {
                fs += data.Char.String[data.Char.Index + i];
                i++;
            }
            while (data.Char.Index + i < Parsed.TextTrimLength && data.Char.String[data.Char.Index + i] != ' ' && !IsNewlineSafe(data.Char.Index + i, data.Char.String, data))
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
            if (charPos + 16 >= (data.Style.ImageAssets[data.Box.Images[0].Path].Size.Width * data.GetCurrentBoxScale() / data.GetCurrentFontScale()) - (EnablePortrait.Value && !EnablePortrait.ReadOnly ? EnvPortraitOffset.X : 0) && !EnvFirstDrawnChar)
            {
                actAsNewline = true;
                if (EnvStartedAsterisk)
                    addSpaceXNewline = 2;
            }
        }

        if (actAsNewline)
        {
            EnvLastNewline = true;
            if (addSpaceXNewline <= 0 && EnvStartedAsterisk && Parsed.TextTrimLength - data.Char.Index >= 2 && data.Char.String[data.Char.Index + 1] != '*')
                addSpaceXNewline = 1;
            data.Char.PositionOffset.X = 0;
            if (addSpaceXNewline > 0)
            {
                var glyph = data.Font.GlyphDictionary[' '];
                data.Char.PositionOffset.X = ((EnvX != -1 ? EnvX : glyph.Shift) + glyph.Offset) * 2 * addSpaceXNewline;
            }

            if (EnvY == -1)
            {
                var size = data.Font.GlyphDictionary['A'].Size.Y;
                data.Char.PositionOffset.Y += (size + (size % 2) + (data.Font.Size % 2)) * 2;
            }
            else
                data.Char.PositionOffset.Y += EnvY * 2;
        }
        if (!IsNewlineSafe(data.Char.Index, data.Char.String, data) && !data.Char.IsIgnore)
        {
            var glyph = data.Font.GlyphDictionary[data.Char.Char];
            data.Glyph.Position.X = data.Char.StartPosition.X + data.Char.PositionOffset.X + (glyph.Offset * 2);
            data.Glyph.Position.Y = data.Char.StartPosition.Y + data.Char.PositionOffset.Y;
            data.Glyph.Size.X = glyph.Size.X * 2;
            data.Glyph.Size.Y = glyph.Size.Y * 2;
            data.DrawGlyph();
            if ((EnvLastNewline || EnvFirstDrawnChar) && data.Char.Char == '*')
                EnvStartedAsterisk = true;
            data.Char.PositionOffset.X += (int)Math.Ceiling(((EnvX != -1 ? EnvX : glyph.Shift) + glyph.Offset) * 2);
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
public class DialogueMiniface(string argument) : ParsedEvent(argument) { }
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

    public ParsedString(string text)
    {
        Text = text;
        var trimLen = text.TrimEnd().Length;
        TextTrimLength = trimLen;
        var sb = new StringBuilder();
        for (var i = 0; i < trimLen; i++)
        {
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
            var @char = text[i];
            if (((trimLen - i == 1 || i == 0) && @char == '/') || (trimLen - i == 1 && @char == '%'))
            {
                Events.Add(i, new DialogueEnd(@char.ToString()));
                continue;
            }
            if (@char is '`')
            {
                Events.Add(i, new DialogueIgnore(@char.ToString()));
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
                            if (!"0SRNTLsAaBbruUKQCyi".Contains(slice[1]))
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
                                    'I' => new() { Rgba = 0x549ff9ff },
                                    '0' => new() { Rgba = 0xff69b4ff },
                                    _ => null,
                                };
                                if (col is null)
                                    ParseWarns.Add(i, new ParseWarn(fullSlice, $"invalid choice {slice[1]} for setting color: {fullSlice}"));
                                Events.Add(i, new DialogueColor(fullSlice) { Color = col });
                            }
                            break;
                        case 'S' or 's' or 'C' or 'M' or 'I' or 'm' or 'f' or 'O':
                            {
                                if (!int.TryParse(slice[1].ToString(), out var v))
                                    valid = false;
                                else
                                {
                                    switch (slice[0])
                                    {
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
                                            Events.Add(i, new DialogueFlag20(fullSlice));
                                            break;
                                        case 'I':
                                            Events.Add(i, new DialogueSprite(fullSlice));
                                            break;
                                        case 'm':
                                            Events.Add(i, new DialogueMiniface(fullSlice));
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
            sb.Append(@char);
        }
        ParsedText = sb.ToString();
    }
}
