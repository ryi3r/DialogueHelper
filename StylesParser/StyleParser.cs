using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CSScriptLib;
using SkiaSharp;

namespace DialogueHelper.StylesParser;

public class StyleParser
{
    public readonly StyleMetadata Metadata;
    public readonly List<BoxMetadata> BoxMetadata;
    public readonly List<FontMetadata> FontMetadata;
    public readonly string Folder;
    public readonly string? ScriptData;
    public readonly Dictionary<string, SKBitmap> ImageAssets = [];
    public StyleScript? Script;
    public bool InitializedScript;

    public StyleParser(string path)
    {
        Folder = path;
        var jsonSeri = new JsonSerializerOptions
        {
            IncludeFields = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };
        Metadata = JsonSerializer.Deserialize<StyleMetadata>(File.ReadAllText($"{path}/Metadata.jsonc"), jsonSeri)!;
        BoxMetadata = JsonSerializer.Deserialize<List<BoxMetadata>>(File.ReadAllText($"{path}/Boxes.jsonc"), jsonSeri)!;
        var list = JsonSerializer.Deserialize<List<FontMetadataJson>>(File.ReadAllText($"{path}/Fonts.jsonc"), jsonSeri)!;
        FontMetadata = new(list.Capacity);
        foreach (var font in list)
            FontMetadata.Add(new FontMetadata(font));
        foreach (var box in BoxMetadata)
        {
            foreach (var img in box.Images)
            {
                if (ImageAssets.ContainsKey(img.Path))
                    continue;
                ImageAssets.Add(img.Path, SKBitmap.Decode($"{path}/{img.Path}"));
            }
        }
        foreach (var font in FontMetadata)
        {
            if (ImageAssets.ContainsKey(font.ImagePath))
                continue;
            ImageAssets.Add(font.ImagePath, SKBitmap.Decode($"{path}/{font.ImagePath}"));
        }
    }

    public async Task CompileCode()
    {
        if (Script is null && Metadata.ScriptPath is not null)
        {
            // required to show error lines
            CSScript.EvaluatorConfig.DebugBuild = true;
            Script = (StyleScript)CSScript.Evaluator.LoadFile(Path.Join(Folder, Metadata.ScriptPath));
        }
    }
}
