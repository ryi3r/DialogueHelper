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
    };
    public static CustomProperty<bool> EnablePortrait = new("Enable Portrait", "enablePortrait", false);
    public static CustomProperty<bool> DarkZone = new("Dark Zone (JP Only)", "darkZone", false);

    static ExternalData _data = null!;
    public static bool EnvLastNewline
    {
        get => _data.Env["LastNewline"];
        set => _data.Env["LastNewline"] = value;
    }
    public static bool EnvStartedAsterisk
    {
        get => _data.Env["StartedAsterisk"];
        set => _data.Env["StartedAsterisk"] = value;
    }
    public static double EnvX
    {
        get => _data.Env["X"];
        set => _data.Env["X"] = value;
    }
    public static double EnvY
    {
        get => _data.Env["Y"];
        set => _data.Env["Y"] = value;
    }
    public static int EnvSkip
    {
        get => _data.Env["Skip"];
        set => _data.Env["Skip"] = value;
    }
    public static bool EnvFirstDrawnChar
    {
        get => _data.Env["FirstDrawnChar"];
        set => _data.Env["FirstDrawnChar"] = value;
    }
    public static int EnvCheckedIndex
    {
        get => _data.Env["CheckedIndex"];
        set => _data.Env["CheckedIndex"] = value;
    }

    public static (double X, double Y) EnvPortraitOffset
    {
        get => _data.Env["PortraitOffset"];
        set => _data.Env["PortraitOffset"] = value;
    }

    public static int EnvStrLen
    {
        get => _data.Env["StrLen"];
        set => _data.Env["StrLen"] = value;
    }

    public override async Task Init(CustomToolWindowHandler wh)
    {
        // This is called when the script is loaded and ready to go.
        //await wh.ShowDialog(new InfoWindow("Hello world!"));
    }

    public override async Task<IEnumerable<ICustomProperty>> RegisterCustomProperties()
    {
        // Store these somewhere if you're going to use them
        return [
            EnablePortrait,
            DarkZone,
        ];
    }

    public override async Task<IEnumerable<CustomTool>> RegisterCustomTools()
    {
        return [
            new("Import from data.win", ImportFromDataWin),
            new("Export to data.win", ExportToDataWin),
            new("Import from json file", ImportFromJson),
            new("Export to json file", ExportToJson),
        ];
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
            Title = "Select a file",
            FileTypeFilter = [new("GameMaker's Data Files") { Patterns = ["data.win"] }, new("All files") { Patterns = ["*"] }],
        });
        if (f.Count <= 0)
            return;
        File.Delete($"{wh.GetStylePath()}/output.json");
        using var proc = new Process()
        {
            StartInfo =
            {
                UseShellExecute = false,
                FileName = filePath,
                Arguments = $"load \"{Uri.UnescapeDataString(f[0].Path.AbsolutePath).Replace("\"", "\\\"")}\" -s ../ExportStringsJson.csx",
                WorkingDirectory = $"{wh.GetStylePath()}/UndertaleModCli",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            },
        };
        proc.Start();
        var lw = new LoadingWindow();
        lw.ProgressBarView.IsIndeterminate = true;
        lw.ProgressBarView.ShowProgressText = false;
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
            await LoadJson(output, wh);
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
            Title = "Select a file",
            FileTypeFilter = [new("GameMaker's Data Files") { Patterns = ["data.win"] }, new("All files") { Patterns = ["*"] }],
        });
        if (f.Count <= 0)
            return;
        var lw = new LoadingWindow();
        lw.ProgressBarView.IsIndeterminate = true;
        lw.ProgressBarView.ShowProgressText = false;
        wh.Show(lw);
        try
        {
            var stream = new MemoryStream(await File.ReadAllBytesAsync(Path.Combine(f[0].Path.AbsolutePath, "../lang/lang_ja.json")));
            var baseJson = (await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream))!;
            await stream.DisposeAsync();
            await File.WriteAllTextAsync($"{wh.GetStylePath()}/input.json", await ExportJson(baseJson, wh));
            var targetPath = $"\"{Uri.UnescapeDataString(f[0].Path.AbsolutePath).Replace("\"", "\\\"")}\"";
            using var proc = new Process()
            {
                StartInfo =
                {
                    UseShellExecute = false,
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
                stdout.Append(line);
                stdout.Append('\n');
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
        var lw = new LoadingWindow();
        lw.ProgressBarView.IsIndeterminate = true;
        lw.ProgressBarView.ShowProgressText = false;
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
        if (wh.GetLoadedFileData() is null)
        {
            await wh.ShowDialog(new InfoWindow("Please ensure that you have a file open."));
            return;
        }
        var fOpen = await wh.GetStorageProvider().OpenFilePickerAsync(new()
        {
            Title = "Select a file (Japanese/English for a exporting base)",
            FileTypeFilter = [new("JSON Files") { Patterns = ["*.json"] }, new("All Files") { Patterns = ["*"] }],
        });
        if (fOpen.Count <= 0)
            return;
        var fSave = await wh.GetStorageProvider().SaveFilePickerAsync(new()
        {
            Title = "Select a file",
            FileTypeChoices = [new("JSON Files") { Patterns = ["*.json"] }],
        });
        if (fSave is null)
            return;
        var lw = new LoadingWindow();
        lw.ProgressBarView.IsIndeterminate = true;
        lw.ProgressBarView.ShowProgressText = false;
        wh.Show(lw);
        try
        {
            var readStream = await fOpen[0].OpenReadAsync();
            var baseJson = (await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(readStream))!;
            await readStream.DisposeAsync();
            var stream = new StreamWriter(await fSave.OpenWriteAsync());
            stream.Write(await ExportJson(baseJson, wh));
            await stream.DisposeAsync();
        }
        finally
        {
            lw.Close();
        }
        await wh.ShowDialog(new InfoWindow($"Exported data successfully."));
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

    public async Task LoadJson(Dictionary<string, string> json, CustomToolWindowHandler wh)
    {
        var sb = new StringBuilder();
        foreach (var (header, entries) in CalculateHeaders(json.Keys))
        {
            sb.Append($"0;ID:{FormatEntry.GetSimpleUri(header)}\n");
            foreach (var key in entries)
                sb.Append($"1;OriginalContent:{FormatEntry.GetSimpleUri(json[key])}\n");
        }
        sb.Append("8;");
        wh.LoadString(sb.ToString());
    }

    public async Task<string> ExportJson(Dictionary<string, string> baseJson, CustomToolWindowHandler wh)
    {
        var data = new Dictionary<string, string> { ["date"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() };
        var file = wh.GetLoadedFileData()!;
        foreach (var (key, entries) in CalculateHeaders(baseJson.Keys))
        {
            //Console.WriteLine($"key={key}");
            if (file.Strings.TryGetValue(key, out var s))
            {
                // sometimes, deltarune will add unused strings to lang_ja, we need to handle this properly
                var arr = file.Strings[key].OrderBy(x => x.Key).ToArray();
                foreach (var (i, entry) in entries.Index())
                {
                    //Console.WriteLine($"i={i} entry={entry}");
                    if (i >= arr.Length)
                        continue; // in japanese there seem to be things larger than in english?
                    var sc = arr[i].Value;
                    data[entry] = sc.Text ?? sc.OriginalText;
                }
            }
        }
        return JsonSerializer.Serialize(data, new JsonSerializerOptions() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    }

    public override async Task<bool> PrepareDraw(CustomToolWindowHandler wh, ExternalData data)
    {
        _data = data;
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
        if (EnablePortrait.Value && !EnablePortrait.ReadOnly)
            data.Char.StartPosition = EnvPortraitOffset;
        EnvStrLen = data.Char.String.TrimEnd().Length;
        return await base.PrepareDraw(wh, data);
    }

    public bool IsNewlineSafe(int idx, string str, ExternalData data) => data.Style.Metadata.NewLines.Contains(str[idx]) && str[int.Max(0, idx - 1)] != '`';

    public override async Task<bool> DrawGlyph(CustomToolWindowHandler wh, ExternalData data)
    {
        _data = data;
        if (data.Char.IsIgnore || (!data.Font.GlyphDictionary.ContainsKey(data.Char.Char) && !data.Char.IsNewline))
            return false;
        if (EnvSkip > 0)
        {
            EnvSkip--;
            return false;
        }

        if (EnvStrLen - data.Char.Index >= 3)
        {
            if (data.Char.String.Substring(data.Char.Index, 3) == "/%%")
            {
                EnvSkip = 2;
                return false;
            }
        }
        if (EnvStrLen - data.Char.Index >= 2)
        {
            if (data.Char.String.Substring(data.Char.Index, 2) == "/%" ||
                data.Char.Char == '^' && int.TryParse(data.Char.String.Substring(data.Char.Index + 1, 1), out _) ||
                data.Char.String.Substring(data.Char.Index, 2) == "%%")
            {
                EnvSkip = 1;
                return false;
            }
        }
        if ((EnvStrLen - data.Char.Index == 1 || data.Char.Index == 0) && data.Char.Char == '/')
            return false;
        if (EnvStrLen - data.Char.Index == 1 && data.Char.Char == '%')
            return false;
        if (data.Style.Metadata.Ignore.Contains(data.Char.Char))
            return false;
        if (data.Char.Char == '\\')
        {
            EnvSkip = 2;
            if (EnvStrLen - (data.Char.Index + 1) >= 2)
            {
                var slice = data.Char.String[(data.Char.Index + 1)..(data.Char.Index + 3)];
                switch (slice[0])
                {
                    case 'E':
                        if (!"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".Contains(slice[1]))
                            EnvSkip = 0;
                        else if (!EnablePortrait.ReadOnly && !EnablePortrait.Value)
                        {
                            EnablePortrait.Value = true;
                            EnablePortrait.UpdateUiValue();
                            return true;
                        }
                        break;
                    case 'F':
                        if (!"0SRNTLsAaBbruUKQCyi".Contains(slice[1]))
                            EnvSkip = 0;
                        else if (!EnablePortrait.ReadOnly && !EnablePortrait.Value)
                        {
                            EnablePortrait.Value = true;
                            EnablePortrait.UpdateUiValue();
                            return true;
                        }
                        break;
                    case 'T':
                        var f = slice[1] switch
                        {
                            '0' or '1' or 'A' or 'a' or 'N' or 'n' or 'B' or 'S' or 'X' or 'r' or 'T' or 'U' or 'C' => Fonts.Main,
                            'R' or 'L' or 'J' or 'K' or 'q' or 'Q' or 'p' or 'k' or 'j' or 'g' or 'v' => Fonts.Main, //"fnt_mainbig",
                            's' => Fonts.ComicSans,
                            _ => (Fonts?)null,
                        };
                        if (f is not null)
                            data.Font = data.Style.FontMetadata[(int)f];
                        else
                            EnvSkip = 0;
                        break;
                    case 'c':
                        var col = slice[1] switch
                        {
                            'R' => new Color() { Rgba = 0xff0000ff },
                            'B' => new Color() { Rgba = 0x0000ffff },
                            'Y' => new Color() { Rgba = 0xffff00ff },
                            'G' => new Color() { Rgba = 0x00ff00ff },
                            'W' => new Color() { Rgba = 0xffffffff },
                            'X' => new Color() { Rgba = 0x000000ff },
                            'P' => new Color() { Rgba = 0x800080ff },
                            'M' => new Color() { Rgba = 0x800000ff },
                            'O' => new Color() { Rgba = 0xffa500ff },
                            'A' => new Color() { Rgba = 0x00aeffff },
                            'S' => new Color() { Rgba = 0xff80ffff },
                            'V' => new Color() { Rgba = 0x80ff80ff },
                            'I' => new Color() { Rgba = 0x549ff9ff },
                            '0' => new Color() { Rgba = 0xff69b4 },
                            _ => null,
                        };
                        if (col is not null)
                            data.Glyph.Color = col;
                        else
                            EnvSkip = 0;
                        break;
                    // f is small dialogue, the number after it is the index of the array
                    case 'S' or 's' or 'C' or 'M' or 'I' or 'm' or 'f' or 'O':
                        {
                            var invalid = false;
                            if (!int.TryParse(slice[1].ToString(), out var v))
                                invalid = true;
                            else
                            {
                                switch (slice[0])
                                {
                                    // s == skippable on value "v"
                                    case 's' when v < 0 || v > 1: invalid = true; break;
                                    // C == choicer with "v" amount of choices
                                    case 'C' when v < 1 || v > 4: invalid = true; break;
                                    // M == flag 20 on value "v"
                                    // S == play sound on array index "v"
                                    // I == draw sprite on array index "v"
                                    // m == draw miniface (sprite) on array index "v"
                                    // O == create an object on array index "v"
                                    case 'M' or 'S' or 'I' or 'm' or 'O' when v < 0 || v > 9: invalid = true; break;
                                }
                            }
                            if (invalid)
                                EnvSkip = 0;
                        }
                        break;
                }
            }
            if (EnvSkip != 0)
                return false;
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
            while (data.Char.Index + i < EnvStrLen && (data.Char.String[data.Char.Index + i] == ' ' || IsNewlineSafe(data.Char.Index + i, data.Char.String, data)))
            {
                fs += data.Char.String[data.Char.Index + i];
                i++;
            }
            while (data.Char.Index + i < EnvStrLen && data.Char.String[data.Char.Index + i] != ' ' && !IsNewlineSafe(data.Char.Index + i, data.Char.String, data))
            {
                var c = data.Char.String[data.Char.Index + i];
                switch (c)
                {
                    case '`':
                        i++;
                        continue;
                    case '\\':
                        i += 3;
                        continue;
                    case '^':
                        i += 2;
                        continue;
                    case '/':
                        if (data.Char.Index + i >= EnvStrLen - 1 || data.Char.Index + i == 0)
                        {
                            i++;
                            continue;
                        }
                        if ((data.Char.Index + i == EnvStrLen - 2 && data.Char.String[data.Char.Index + i + 1] == '%') ||
                            (data.Char.Index + i == EnvStrLen - 3 && data.Char.String[data.Char.Index + i + 1] == '%' && data.Char.String[data.Char.Index + i + 2] == '%'))
                        {
                            i += 2;
                            continue;
                        }
                        break;
                    case '%':
                        if ((data.Char.Index + i == EnvStrLen - 2 && data.Char.String[data.Char.Index + i + 1] == '%') || data.Char.Index + i == EnvStrLen - 1)
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
            if (charPos + 32 > (data.Style.ImageAssets[data.Box.Images[0].Path].Size.Width * data.GetCurrentBoxScale() / data.GetCurrentFontScale()) - (EnablePortrait.Value && !EnablePortrait.ReadOnly ? EnvPortraitOffset.X : 0) && !EnvFirstDrawnChar)
            {
                actAsNewline = true;
                if (EnvStartedAsterisk)
                    addSpaceXNewline = 2;
            }
        }

        if (actAsNewline)
        {
            EnvLastNewline = true;
            if (addSpaceXNewline <= 0 && EnvStartedAsterisk && EnvStrLen - data.Char.Index >= 2 && data.Char.String[data.Char.Index + 1] != '*')
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
