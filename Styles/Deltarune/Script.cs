using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
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
        return [new("Import from data.win", ImportFromDataWin), new("Export to data.win", ExportToDataWin)];
    }

    public static async Task ImportFromDataWin(CustomToolWindowHandler wH)
    {
        if (wH.GetLoadedFileData() != null)
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
            FileTypeFilter = [new("GameMaker's Data File")
        {
            Patterns = ["data.win"],
        }, new("All files")
        {
            Patterns = ["*"],
        }]
        });
        if (f.Count <= 0)
            return;
        if (File.Exists($"{wH.GetStylePath()}/output.json"))
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
        var stdout = new StringBuilder();
        while (!proc.HasExited)
        {
            var line = await proc.StandardOutput.ReadLineAsync();
            if (line is null)
                continue;
            stdout.Append(line);
            lw.TextView.Text = line.Trim();
        }
        lw.Close();
        var stderr = await proc.StandardError.ReadToEndAsync();

        Console.WriteLine($"stdout:\n{stdout}\nstderr:\n{stderr}");
        if (proc.ExitCode != 0 || !File.Exists($"{wH.GetStylePath()}/output.json"))
        {
            var w = new InfoWindow($"UndertaleModCli exited with code {proc.ExitCode}\n\nstdout:\n{stdout}\nstderr:\n{stderr}");
            await wH.ShowDialog(w);
            return;
        }
        using var stream = new MemoryStream((await File.ReadAllBytesAsync($"{wH.GetStylePath()}/output.json")));
        var output = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream);
        File.Delete($"{wH.GetStylePath()}/output.json");
        // todo: do this shit
        //FileParser.ParseString()
        await wH.ShowDialog(new InfoWindow($"Imported data successfully."));
    }

    public static async Task ExportToDataWin(CustomToolWindowHandler wH)
    {

    }

    public static async Task PrepareDraw(CustomToolWindowHandler _, ExternalData data)
    {
        data.Env["LastNewline"] = false;
        data.Env["StartedAsterisk"] = false;
        data.Env["X"] = -1;
        data.Env["Y"] = -1;
        data.Env["Skip"] = 0;
        data.Env["FirstDrawnChar"] = true;
        data.Env["CheckedIndex"] = -1;
        var lro = EnablePortrait.ReadOnly;
        EnablePortrait.ReadOnly = !data.Box.CustomProperties.TryGetValue("SupportsPortrait", out var val) || ((JsonElement)val).ToString() != "true";
        if (lro != EnablePortrait.ReadOnly)
            EnablePortrait.UpdateUiValue();
        if ((EnablePortrait.Value ?? false) && !EnablePortrait.ReadOnly)
        {
            var pos = (((JsonElement)data.Box.CustomProperties["PortraitOffset"]).GetString() ?? "0,0").Split(',');
            data.Char.StartPosition.X = double.Parse(pos[0]);
            data.Char.StartPosition.Y = double.Parse(pos[1]);
        }
    }

    public static async Task DrawGlyph(CustomToolWindowHandler __, ExternalData data)
    {
        if (data.Char.IsIgnore || (!data.Font.GlyphDictionary.ContainsKey(data.Char.Char) && !data.Char.IsNewline))
            return;
        if (data.Env["Skip"] > 0)
        {
            data.Env["Skip"]--;
            return;
        }

        if (data.Char.String.Length - data.Char.Index >= 3)
        {
            if (data.Char.String.Substring(data.Char.Index, 3) == "/%%")
            {
                data.Env["Skip"] = 2;
                return;
            }
        }
        if (data.Char.String.Length - data.Char.Index >= 2)
        {
            if (data.Char.String.Substring(data.Char.Index, 2) == "/%" ||
                data.Char.Char == '^' && int.TryParse(data.Char.String.Substring(data.Char.Index + 1, 1), out _) ||
                data.Char.String.Substring(data.Char.Index, 2) == "%%")
            {
                data.Env["Skip"] = 1;
                return;
            }
        }
        if ((data.Char.String.Length - data.Char.Index == 1 || data.Char.Index == 0) && data.Char.Char == '/')
            return;
        if (data.Char.String.Length - data.Char.Index == 1 && data.Char.Char == '%')
            return;
        if (data.Char.Char == '\\')
        {
            data.Env["Skip"] = 2;
            if (data.Char.String.Length - (data.Char.Index + 1) >= 2)
            {
                var slice = data.Char.String[(data.Char.Index + 1)..(data.Char.Index + 3)];
                switch (slice[0])
                {
                    case 'E':
                        EnablePortrait.Value = true;
                        if (!"0123456789ABCDE".Contains(slice[1]))
                            data.Env["Skip"] = 0;
                        break;
                    case 'F':
                        EnablePortrait.Value = true;
                        if (!"0SRNTLsUAaBruKQ".Contains(slice[1]))
                            data.Env["Skip"] = 0;
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
                            data.Env["Skip"] = 0;
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
                            data.Env["Skip"] = 0;
                        break;
                    case 'S' or 's' or 'C' or 'M' or 'I' or 'm':
                        if (!int.TryParse(slice[1].ToString(), out _))
                            data.Env["Skip"] = 0;
                        break;
                }
            }
            if (data.Env["Skip"] != 0)
                return;
        }

        switch (Fonts[data.Style.FontMetadata.Index().First(x => x.Item == data.Font).Index])
        {
            case "fnt_main":
                data.Env["X"] = 8;
                data.Env["Y"] = 18;
                break;
            case "fnt_mainbig":
                data.Env["X"] = 16;
                data.Env["Y"] = 36;
                break;
            case "fnt_comicsans":
                data.Env["X"] = 8;
                data.Env["Y"] = 18;
                break;
            case "fnt_tinynoelle":
                data.Env["X"] = 6;
                data.Env["Y"] = 18;
                break;
            case "fnt_dotumche":
                data.Env["X"] = 9;
                data.Env["Y"] = 20;
                break;
        }

        var actAsNewline = data.Char.IsNewline;
        var addSpaceXNewline = 0;
        if (data.Char.Index > data.Env["CheckedIndex"])
        {
            if (data.Env["CheckedIndex"] == -1)
                data.Env["CheckedIndex"] = 0;
            var i = 0;
            var fs = "";
            while (data.Char.Index + i < data.Char.String.Length && (data.Char.String[data.Char.Index + i] == ' ' ||
                                                                        data.Char.String[data.Char.Index + i] == '&'))
            {
                fs += data.Char.String[data.Char.Index + i];
                i++;
            }
            while (data.Char.Index + i < data.Char.String.Length && (data.Char.String[data.Char.Index + i] != ' ' &&
                                                                        data.Char.String[data.Char.Index + i] != '&'))
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

            data.Env["CheckedIndex"] = data.Char.Index + i;
            var charPos = data.Char.PositionOffset.X;
            foreach (var chr in fs)
            {
                var glyph = data.Font.Glyphs.FirstOrDefault(g => g.Char == chr);
                if (glyph is null)
                    continue;
                charPos += ((data.Env["X"] != -1.0 ? data.Env["X"] : glyph.Shift) + glyph.Offset) * 2d;
            }
            if (charPos + 40 > (data.Style.ImageAssets[data.Box.Images[0].Path].Size.Width * data.GetCurrentBoxScale() / data.GetCurrentFontScale()) - ((EnablePortrait.Value ?? false) && !EnablePortrait.ReadOnly ? double.Parse(
                    (((JsonElement)data.Box.CustomProperties["PortraitOffset"]).GetString() ?? "0,0").Split(',')[0]) : 0.0) && !data.Env["FirstDrawnChar"])
            {
                actAsNewline = true;
                if (data.Env["StartedAsterisk"])
                    addSpaceXNewline = 2;
            }
        }

        if (actAsNewline)
        {
            data.Env["LastNewline"] = true;
            if (addSpaceXNewline <= 0 && data.Env["StartedAsterisk"] && data.Char.String.Length - data.Char.Index >= 2 &&
                data.Char.String[data.Char.Index + 1] != '*')
                addSpaceXNewline = 1;
            data.Char.PositionOffset.X = 0.0;
            if (addSpaceXNewline > 0)
            {
                var glyph = data.Font.GlyphDictionary[' '];
                data.Char.PositionOffset.X =
                    ((data.Env["X"] != -1.0 ? data.Env["X"] : glyph.Shift) + glyph.Offset) * 2d * (double)addSpaceXNewline;
            }

            if (data.Env["Y"] == -1.0)
            {
                var size = data.Font.GlyphDictionary['A'].Size.Y;
                data.Char.PositionOffset.Y += (size + (size % 2.0) + (data.Font.Size % 2.0)) * 2d;
            }
            else
                data.Char.PositionOffset.Y += data.Env["Y"] * 2d;
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
            if ((data.Env["LastNewline"] || data.Env["FirstDrawnChar"]) && data.Char.Char == '*')
                data.Env["StartedAsterisk"] = true;
            data.Char.PositionOffset.X += (int)Math.Ceiling(((data.Env["X"] != -1.0 ? data.Env["X"] : glyph.Shift) + glyph.Offset) * 2d);
            data.Env["LastNewline"] = false;
            data.Env["FirstDrawnChar"] = false;
        }
    }
}
