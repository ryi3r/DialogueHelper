using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using DialogueHelper.External;
using DialogueHelper.FileFormat;
using DialogueHelper.PreviewRenderer;
using DialogueHelper.Views;
using SixLabors.ImageSharp;

public static class Script
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
    
    public static async Task Init(CustomToolWindowHandler wH)
    {
        // This is called when the script is loaded and ready to go.
        //await wH.ShowDialog(new InfoWindow("Hello world!"));
    }
    
    public static async Task<List<CustomProperty>> RegisterCustomProperties()
    {
        // Store these somewhere if you're going to use them
        return [
            EnablePortrait,
        ];
    }

    public static async Task<List<CustomTool>> RegisterCustomTools()
    {
        return [/*new("Import from data.win", ImportFromDataWin), new("Export to data.win", CustomToolFileTest)*/];
    }
    
    public static async Task ImportFromDataWin(CustomToolWindowHandler wH)
    {
        if (wH.GetLoadedFileData() != null)
        {
            var w = new InfoWindow($"Please ensure that you do not have a file open.");
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
        using var proc = new Process()
        {
            StartInfo =
            {
                UseShellExecute = false,
                FileName = $"{wH.GetStylePath()}/UndertaleModCli/UndertaleModCli{(OperatingSystem.IsWindows() ? ".exe" : "")}",
                Arguments = $"load \"{Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(f[0].Path.AbsolutePath, false)}\" -s ../ExportStringsJson.csx",
            },
        };
        if (File.Exists($"{wH.GetStylePath()}/output.json"))
            File.Delete($"{wH.GetStylePath()}/output.json");
        proc.Start();
        await proc.WaitForExitAsync();
        using var stream = new MemoryStream((await File.ReadAllBytesAsync($"{wH.GetStylePath()}/output.json")));
        var output = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream);
        File.Delete($"{wH.GetStylePath()}/output.json");
        // todo: do this shit
        //FileParser.ParseString()
        await wH.ShowDialog(new InfoWindow($"Imported data successfully."));
    }

    public static async Task PrepareDraw(CustomToolWindowHandler wH, ExternalData data)
    {
        data.Env["LastNewline"] = false;
        data.Env["StartedAsterisk"] = false;
        data.Env["_e"] = 0;
        data.Env["_c"] = 0;
        data.Env["_f"] = 0;
        data.Env["_sx"] = -1;
        data.Env["_sy"] = -1;
        data.Env["Skip"] = 0;
        data.Env["FirstDrawnChar"] = true;
        data.Env["CheckedIndex"] = -1;
        var lro = EnablePortrait.ReadOnly;
        EnablePortrait.ReadOnly = data.Box.CustomProperties.TryGetValue("SupportsPortrait", out var val)
            ? ((JsonElement)val).ToString() != "true"
            : true;
        if (lro != EnablePortrait.ReadOnly)
            EnablePortrait.UpdateUiValue();
        if ((EnablePortrait.Value ?? false) && !EnablePortrait.ReadOnly)
        {
            var pos = (((JsonElement)data.Box.CustomProperties["PortraitOffset"]).GetString() ?? "0,0").Split(',');
            data.Char.StartPosition.Item1 = double.Parse(pos[0]);
            data.Char.StartPosition.Item2 = double.Parse(pos[1]);
        }
    }

    public static async Task DrawGlyph(CustomToolWindowHandler wH, ExternalData data)
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

        if (data.Char.Char == '\\')
        {
            data.Env["Skip"] = 2;
            // todo: portrait conditionals
            return;
        }

        switch (Fonts[data.GetCurrentFont()])
        {
            case "fnt_main":
                data.Env["_sx"] = 8;
                data.Env["_sy"] = 18;
                break;
            case "fnt_mainbig":
                data.Env["_sx"] = 16;
                data.Env["_sy"] = 36;
                break;
            case "fnt_comicsans":
                data.Env["_sx"] = 8;
                data.Env["_sy"] = 18;
                break;
            case "fnt_tinynoelle":
                data.Env["_sx"] = 6;
                data.Env["_sy"] = 18;
                break;
            case "fnt_dotumche":
                data.Env["_sx"] = 9;
                data.Env["_sy"] = 20;
                break;
        }

        var scale = data.Glyph.Scale.Item1 * 2.0;
        var actAsNewline = data.Char.IsNewline;
        var addSpaceXNewline = false;
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
                        if (data.Char.Index + i == data.Char.String.Length - 1 || data.Char.Index + i == 0)
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
            var charPos = data.Char.PositionOffset.Item1;
            foreach (var chr in fs)
            {
                var glyph = data.Font.Glyphs.FirstOrDefault(g => g.Char == chr);
                if (glyph == null)
                    continue;
                charPos += ((data.Env["_sx"] != -1.0 ? data.Env["_sx"] : glyph.Shift) + glyph.Offset) * scale;
            }
            
            if (charPos + 40 > data.Style.ImageAssets[data.Box.Images[0].Path].Size.Width - ((EnablePortrait.Value ?? false) && !EnablePortrait.ReadOnly ? double.Parse(
                    (((JsonElement)data.Box.CustomProperties["PortraitOffset"]).GetString() ?? "0,0").Split(',')[0]) : 0.0) && !data.Env["FirstDrawnChar"])
            {
                actAsNewline = true;
                if (data.Env["StartedAsterisk"])
                    addSpaceXNewline = true;
            }
        }
        
        if (actAsNewline)
        {
            data.Env["LastNewline"] = true;
            if (data.Env["StartedAsterisk"] && data.Char.String.Length - data.Char.Index >= 2 &&
                data.Char.String[data.Char.Index + 1] != '*')
                addSpaceXNewline = true;
            if (addSpaceXNewline)
            {
                var glyph = data.Font.GlyphDictionary[' '];
                data.Char.PositionOffset.Item1 =
                    ((data.Env["_sx"] != -1.0 ? data.Env["_sx"] : glyph.Shift) + glyph.Offset) * scale;
            }
            else
                data.Char.PositionOffset.Item1 = 0.0;

            if (data.Env["_sy"] == -1.0)
            {
                var size = data.Font.GlyphDictionary['A'].Size.Item2;
                data.Char.PositionOffset.Item2 += (size + (size % 2.0) + (data.Font.Size % 2.0)) * scale;
            }
            else
                data.Char.PositionOffset.Item2 += data.Env["_sy"] * scale;
        }
        if (!data.Char.IsNewline)
        {
            var glyph = data.Font.GlyphDictionary[data.Char.Char];
            data.Glyph.Position.Item1 =
                (int)(data.Char.StartPosition.Item1 + data.Char.PositionOffset.Item1 + (glyph.Offset * scale));
            data.Glyph.Position.Item2 = data.Char.StartPosition.Item2 + data.Char.PositionOffset.Item2;
            data.Glyph.Size.Item1 = glyph.Size.Item1 * scale;
            data.Glyph.Size.Item2 = glyph.Size.Item2 * scale;
            data.DrawGlyph();
            if ((data.Env["LastNewline"] || data.Env["FirstDrawnChar"]) && data.Char.Char == '*')
                data.Env["StartedAsterisk"] = true;
            data.Char.PositionOffset.Item1 += (int)Math.Ceiling(((data.Env["_sx"] != -1.0 ? data.Env["_sx"] : glyph.Shift) + glyph.Offset) * scale);
            data.Env["LastNewline"] = false;
            data.Env["FirstDrawnChar"] = false;
        }
    }
}
