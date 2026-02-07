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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class Deltarune
{
    public static string[] Fonts = [
    "fnt_main",
    "fnt_mainbig",
    "fnt_small",
    "fnt_comicsans",
    "fnt_dotumche",
    "fnt_tinynoelle",
    "fnt_ja_main",
    "fnt_ja_mainbig",
    "fnt_ja_small",
    "fnt_ja_comicsans",
    "fnt_ja_dotumche",
    "fnt_ja_kakugo",
    "fnt_ja_tinynoelle",
];
    public static CustomProperty EnablePortrait = new("Enable Portrait", "enablePortrait", typeof(bool), false);

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

    public static async Task Init(CustomToolWindowHandler _)
    {
        // This is called when the script is loaded and ready to go.
        //await wH.ShowDialog(new InfoWindow("Hello world!"));
    }

    public static async Task<IEnumerable<CustomProperty>> RegisterCustomProperties()
    {
        // Store these somewhere if you're going to use them
        return [
            EnablePortrait,
        ];
    }

    public static async Task<IEnumerable<CustomTool>> RegisterCustomTools()
    {
        return [
            new("Import from data.win", ImportFromDataWin),
            new("Export to data.win", ExportToDataWin),
            new("Import from json file", ImportFromJson),
            new("Export to json file", ExportToJson),
        ];
    }

    public static async Task ImportFromDataWin(CustomToolWindowHandler wH)
    {
        if (wH.GetLoadedFileData() is not null)
        {
            var w = new InfoWindow("Please ensure that you do not have a file open.");
            await wH.ShowDialog(w);
            return;
        }
        var filePath = $"{wH.GetStylePath()}/UndertaleModCli/UndertaleModCli{(OperatingSystem.IsWindows() ? ".exe" : "")}";
        if (!File.Exists(filePath))
        {
            var w = new InfoWindow("Please download and add UndertaleModCli to this style's folder.");
            await wH.ShowDialog(w);
            return;
        }
        var f = await wH.GetStorageProvider().OpenFilePickerAsync(new()
        {
            Title = "Select a file",
            FileTypeFilter = [new("GameMaker's Data Files")
            {
                Patterns = ["data.win"],
            }, new("All files")
            {
                Patterns = ["*"],
            }]
        });
        if (f.Count <= 0)
            return;
        File.Delete($"{wH.GetStylePath()}/output.json");
        using var proc = new Process()
        {
            StartInfo =
            {
                UseShellExecute = false,
                FileName = filePath,
                Arguments = $"load \"{f[0].Path.AbsolutePath}\" -s ../ExportStringsJson.csx",
                WorkingDirectory = $"{wH.GetStylePath()}/UndertaleModCli",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            },
        };
        proc.Start();
        var lw = new LoadingWindow();
        lw.ProgressBarView.IsIndeterminate = true;
        lw.ProgressBarView.ShowProgressText = false;
        wH.Show(lw);
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
            if (proc.ExitCode != 0 || !File.Exists($"{wH.GetStylePath()}/output.json"))
            {
                lw.Close();
                var w = new InfoWindow($"UndertaleModCli exited with code {proc.ExitCode}\n\nstdout:\n{stdout}\nstderr:\n{stderr}");
                await wH.ShowDialog(w);
                return;
            }
            var stream = new MemoryStream(await File.ReadAllBytesAsync($"{wH.GetStylePath()}/output.json"));
            var output = (await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream))!;
            await stream.DisposeAsync();
            await LoadJson(output, wH);
        }
        finally
        {
            File.Delete($"{wH.GetStylePath()}/output.json");
            lw.Close();
        }
        await wH.ShowDialog(new InfoWindow($"Imported data successfully."));
    }

    public static async Task ExportToDataWin(CustomToolWindowHandler wH)
    {
        if (wH.GetLoadedFileData() is null)
        {
            var w = new InfoWindow("Please ensure that you have a file open.");
            await wH.ShowDialog(w);
            return;
        }
        var filePath = $"{wH.GetStylePath()}/UndertaleModCli/UndertaleModCli{(OperatingSystem.IsWindows() ? ".exe" : "")}";
        if (!File.Exists(filePath))
        {
            var w = new InfoWindow("Please download and add UndertaleModCli to this style's folder.");
            await wH.ShowDialog(w);
            return;
        }
        var f = await wH.GetStorageProvider().OpenFilePickerAsync(new()
        {
            Title = "Select a file",
            FileTypeFilter = [new("GameMaker's Data Files")
            {
                Patterns = ["data.win"],
            }, new("All files")
            {
                Patterns = ["*"],
            }]
        });
        if (f.Count <= 0)
            return;
        var lw = new LoadingWindow();
        lw.ProgressBarView.IsIndeterminate = true;
        lw.ProgressBarView.ShowProgressText = false;
        wH.Show(lw);
        try
        {
            var stream = new MemoryStream(await File.ReadAllBytesAsync(Path.Combine(f[0].Path.AbsolutePath, "../lang/lang_ja.json")));
            var baseJson = (await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream))!;
            await stream.DisposeAsync();
            await File.WriteAllTextAsync($"{wH.GetStylePath()}/input.json", await ExportJson(baseJson, wH));
            var targetPath = $"\"{f[0].Path.AbsolutePath.Replace("\"", "\\\"")}\"";
            using var proc = new Process()
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    FileName = filePath,
                    Arguments = $"load {targetPath} -s ../ImportStringsJson.csx -o {targetPath}",
                    WorkingDirectory = $"{wH.GetStylePath()}/UndertaleModCli",
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
                var w = new InfoWindow($"UndertaleModCli exited with code {proc.ExitCode}\n\nstdout:\n{stdout}\nstderr:\n{stderr}");
                await wH.ShowDialog(w);
                return;
            }
        }
        finally
        {
            File.Delete($"{wH.GetStylePath()}/input.json");
            lw.Close();
        }
        await wH.ShowDialog(new InfoWindow($"Exported data successfully."));
    }

    public static async Task ImportFromJson(CustomToolWindowHandler wH)
    {
        if (wH.GetLoadedFileData() is not null)
        {
            var w = new InfoWindow("Please ensure that you do not have a file open.");
            await wH.ShowDialog(w);
            return;
        }
        var f = await wH.GetStorageProvider().OpenFilePickerAsync(new()
        {
            Title = "Select a file",
            FileTypeFilter = [new("JSON Files")
            {
                Patterns = ["*.json"],
            }, new("All Files")
            {
                Patterns = ["*"],
            }]
        });
        if (f.Count <= 0)
            return;
        var lw = new LoadingWindow();
        lw.ProgressBarView.IsIndeterminate = true;
        lw.ProgressBarView.ShowProgressText = false;
        wH.Show(lw);
        try
        {
            var stream = await f[0].OpenReadAsync();
            var output = (await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream))!;
            await stream.DisposeAsync();
            await LoadJson(output, wH);
        }
        finally
        {
            //File.Delete($"{wH.GetStylePath()}/output.json");
            lw.Close();
        }
        await wH.ShowDialog(new InfoWindow($"Imported data successfully."));
    }

    public static async Task ExportToJson(CustomToolWindowHandler wH)
    {
        if (wH.GetLoadedFileData() is null)
        {
            var w = new InfoWindow("Please ensure that you have a file open.");
            await wH.ShowDialog(w);
            return;
        }
        var fOpen = await wH.GetStorageProvider().OpenFilePickerAsync(new()
        {
            Title = "Select a file (Japanese/English for a exporting base)",
            FileTypeFilter = [new("JSON Files")
            {
                Patterns = ["*.json"],
            }, new("All Files")
            {
                Patterns = ["*"],
            }]
        });
        if (fOpen.Count <= 0)
            return;
        var fSave = await wH.GetStorageProvider().SaveFilePickerAsync(new()
        {
            Title = "Select a file",
            FileTypeChoices = [new("JSON Files") { Patterns = ["*.json"] }],

        });
        if (fSave is null)
            return;
        var lw = new LoadingWindow();
        lw.ProgressBarView.IsIndeterminate = true;
        lw.ProgressBarView.ShowProgressText = false;
        wH.Show(lw);
        try
        {
            var readStream = await fOpen[0].OpenReadAsync();
            var baseJson = (await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(readStream))!;
            await readStream.DisposeAsync();
            var stream = new StreamWriter(await fSave.OpenWriteAsync());
            stream.Write(await ExportJson(baseJson, wH));
            await stream.DisposeAsync();
        }
        finally
        {
            lw.Close();
        }
        await wH.ShowDialog(new InfoWindow($"Imported data successfully."));
    }


    public static Dictionary<string, List<string>> CalculateHeaders(IEnumerable<string> keys)
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

    public static async Task LoadJson(Dictionary<string, string> json, CustomToolWindowHandler wH)
    {
        var sb = new StringBuilder();
        foreach (var (header, entries) in CalculateHeaders(json.Keys))
        {
            sb.Append($"0;ID:{FormatEntry.GetSimpleUri(header)}\n");
            foreach (var key in entries)
                sb.Append($"1;OriginalContent:{FormatEntry.GetSimpleUri(json[key])}\n");
        }
        sb.Append("8;");
        wH.LoadString(sb.ToString());
    }

    public static async Task<string> ExportJson(Dictionary<string, string> baseJson, CustomToolWindowHandler wH)
    {
        var data = new Dictionary<string, string>()
        {
            ["date"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
        };
        var file = wH.GetLoadedFileData()!;
        foreach (var (key, entries) in CalculateHeaders(baseJson.Keys))
        {
            //Console.WriteLine($"key={key}");
            foreach (var (i, entry) in entries.Index())
            {
                //Console.WriteLine($"i={i} entry={entry}");
                var sc = file.Strings[key].OrderBy(x => x.Key).ElementAt(i).Value;
                data[entry] = sc.Text ?? sc.OriginalText;
            }
        }
        return JsonSerializer.Serialize(data, new JsonSerializerOptions() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    }

    public static async Task PrepareDraw(CustomToolWindowHandler _, ExternalData data)
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
        if ((EnablePortrait.Value ?? false) && !EnablePortrait.ReadOnly)
        {
            var po = (JsonElement)data.Box.CustomProperties["PortraitOffset"];
            if (po.GetArrayLength() == 2)
            {
                po[0].TryGetDouble(out data.Char.StartPosition.X);
                po[1].TryGetDouble(out data.Char.StartPosition.Y);
            }
        }
    }

    public static async Task DrawGlyph(CustomToolWindowHandler __, ExternalData data)
    {
        _data = data;
        if (data.Char.IsIgnore || (!data.Font.GlyphDictionary.ContainsKey(data.Char.Char) && !data.Char.IsNewline))
            return;
        if (EnvSkip > 0)
        {
            EnvSkip--;
            return;
        }

        if (data.Char.String.Length - data.Char.Index >= 3)
        {
            if (data.Char.String.Substring(data.Char.Index, 3) == "/%%")
            {
                EnvSkip = 2;
                return;
            }
        }
        if (data.Char.String.Length - data.Char.Index >= 2)
        {
            if (data.Char.String.Substring(data.Char.Index, 2) == "/%" ||
                data.Char.Char == '^' && int.TryParse(data.Char.String.Substring(data.Char.Index + 1, 1), out _) ||
                data.Char.String.Substring(data.Char.Index, 2) == "%%")
            {
                EnvSkip = 1;
                return;
            }
        }
        if ((data.Char.String.Length - data.Char.Index == 1 || data.Char.Index == 0) && data.Char.Char == '/')
            return;
        if (data.Char.String.Length - data.Char.Index == 1 && data.Char.Char == '%')
            return;
        if (data.Char.Char == '\\')
        {
            EnvSkip = 2;
            if (data.Char.String.Length - (data.Char.Index + 1) >= 2)
            {
                var slice = data.Char.String[(data.Char.Index + 1)..(data.Char.Index + 3)];
                switch (slice[0])
                {
                    case 'E':
                        EnablePortrait.Value = true;
                        if (!"0123456789ABCDE".Contains(slice[1]))
                            EnvSkip = 0;
                        break;
                    case 'F':
                        EnablePortrait.Value = true;
                        if (!"0SRNTLsUAaBruKQ".Contains(slice[1]))
                            EnvSkip = 0;
                        break;
                    case 'T':
                        var f = slice[1] switch
                        {
                            '0' or '1' or 'A' or 'a' or 'N' or 'n' or 'B' or 'S' or 'X' or 'r' or 'T' or 'U' => "fnt_main",
                            'R' or 'L' or 'J' or 'K' or 'q' or 'Q' or 'q' => "fnt_mainbig",
                            's' => "fnt_comicsans",
                            _ => "",
                        };
                        if (f.Length > 0)
                            data.Font = data.Style.FontMetadata[Array.IndexOf(Fonts, f)];
                        else
                            EnvSkip = 0;
                        break;
                    case 'c':
                        var col = slice[1] switch
                        {
                            'R' => Color.Red,
                            'B' => Color.Blue,
                            'Y' => Color.Yellow,
                            'G' => Color.Lime,
                            'W' => Color.White,
                            'X' => Color.Black,
                            'P' => Color.Purple,
                            'M' => Color.DarkRed,
                            'S' => new Color(new Rgba32(0xff, 0x80, 0xff, 0xff)),
                            'V' => new Color(new Rgba32(0x80, 0xff, 0x80, 0xff)),
                            '0' => Color.HotPink,
                            _ => (Color?)null,
                        };
                        if (col is not null)
                            data.Glyph.Color = col.Value;
                        else
                            EnvSkip = 0;
                        break;
                    case 'S' or 's' or 'C' or 'M' or 'I' or 'm':
                        if (!int.TryParse(slice[1].ToString(), out _))
                            EnvSkip = 0;
                        break;
                }
            }
            if (EnvSkip != 0)
                return;
        }

        switch (Fonts[data.Style.FontMetadata.Index().First(x => x.Item == data.Font).Index])
        {
            case "fnt_main":
                EnvX = 8;
                EnvY = 18;
                break;
            case "fnt_mainbig":
                EnvX = 16;
                EnvY = 36;
                break;
            case "fnt_comicsans":
                EnvX = 8;
                EnvY = 18;
                break;
            case "fnt_tinynoelle":
                EnvX = 6;
                EnvY = 18;
                break;
            case "fnt_dotumche":
                EnvX = 9;
                EnvY = 20;
                break;
        }

        var actAsNewline = data.Char.IsNewline;
        var addSpaceXNewline = 0;
        if (data.Char.Index > EnvCheckedIndex)
        {
            if (EnvCheckedIndex == -1)
                EnvCheckedIndex = 0;
            var i = 0;
            var fs = "";
            while (data.Char.Index + i < data.Char.String.Length && (data.Char.String[data.Char.Index + i] == ' ' ||
                                                                        data.Style.Metadata.NewLines.Contains(data.Char.String[data.Char.Index + i])))
            {
                fs += data.Char.String[data.Char.Index + i];
                i++;
            }
            while (data.Char.Index + i < data.Char.String.Length && data.Char.String[data.Char.Index + i] != ' ' &&
                                                                        !data.Style.Metadata.NewLines.Contains(data.Char.String[data.Char.Index + i]))
            {
                var c = data.Char.String[data.Char.Index + i];
                switch (c)
                {
                    case '\\':
                        i += 3;
                        continue;
                    case '^':
                        i += 2;
                        continue;
                    case '/':
                        if (data.Char.Index + i >= data.Char.String.Length - 1 || data.Char.Index + i == 0)
                        {
                            i += 1;
                            continue;
                        }
                        if ((data.Char.Index + i == data.Char.String.Length - 2 && data.Char.String[data.Char.Index + i + 1] == '%') ||
                            (data.Char.Index + i == data.Char.String.Length - 3 && data.Char.String[data.Char.Index + i + 1] == '%' && data.Char.String[data.Char.Index + i + 2] == '%'))
                        {
                            i += 2;
                            continue;
                        }
                        break;
                    case '%':
                        if ((data.Char.Index + i == data.Char.String.Length - 2 && data.Char.String[data.Char.Index + i + 1] == '%') ||
                            data.Char.Index + i == data.Char.String.Length - 1)
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
                charPos += ((EnvX != -1.0 ? EnvX : glyph.Shift) + glyph.Offset) * 2d;
            }
            if (charPos + 40 > (data.Style.ImageAssets[data.Box.Images[0].Path].Size.Width * data.GetCurrentBoxScale() / data.GetCurrentFontScale()) - ((EnablePortrait.Value ?? false) && !EnablePortrait.ReadOnly ? double.Parse(
                    (((JsonElement)data.Box.CustomProperties["PortraitOffset"]).GetString() ?? "0,0").Split(',')[0]) : 0.0) && !EnvFirstDrawnChar)
            {
                actAsNewline = true;
                if (EnvStartedAsterisk)
                    addSpaceXNewline = 2;
            }
        }

        if (actAsNewline)
        {
            EnvLastNewline = true;
            if (addSpaceXNewline <= 0 && EnvStartedAsterisk && data.Char.String.Length - data.Char.Index >= 2 &&
                data.Char.String[data.Char.Index + 1] != '*')
                addSpaceXNewline = 1;
            data.Char.PositionOffset.X = 0.0;
            if (addSpaceXNewline > 0)
            {
                var glyph = data.Font.GlyphDictionary[' '];
                data.Char.PositionOffset.X =
                    ((EnvX != -1.0 ? EnvX : glyph.Shift) + glyph.Offset) * 2d * (double)addSpaceXNewline;
            }

            if (EnvY == -1.0)
            {
                var size = data.Font.GlyphDictionary['A'].Size.Y;
                data.Char.PositionOffset.Y += (size + (size % 2.0) + (data.Font.Size % 2.0)) * 2d;
            }
            else
                data.Char.PositionOffset.Y += EnvY * 2d;
        }
        if (!data.Char.IsNewline)
        {
            var glyph = data.Font.GlyphDictionary[data.Char.Char];
            data.Glyph.Position.X =
                data.Char.StartPosition.X + data.Char.PositionOffset.X + (glyph.Offset * 2d);
            data.Glyph.Position.Y = data.Char.StartPosition.Y + data.Char.PositionOffset.Y;
            data.Glyph.Size.X = glyph.Size.X * 2d;
            data.Glyph.Size.Y = glyph.Size.Y * 2d;
            data.DrawGlyph();
            if ((EnvLastNewline || EnvFirstDrawnChar) && data.Char.Char == '*')
                EnvStartedAsterisk = true;
            data.Char.PositionOffset.X += (int)Math.Ceiling(((EnvX != -1.0 ? EnvX : glyph.Shift) + glyph.Offset) * 2d);
            EnvLastNewline = false;
            EnvFirstDrawnChar = false;
        }
    }
}
