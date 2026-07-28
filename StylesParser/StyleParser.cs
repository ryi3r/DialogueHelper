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
        Metadata = JsonSerializer.Deserialize<StyleMetadata>(File.ReadAllText(Path.Combine(path, "Metadata.jsonc")), jsonSeri)!;
        BoxMetadata = [];
        foreach (var f in Directory.GetFiles(Path.Combine(path, "Boxes")))
        {
            if (!f.EndsWith(".jsonc"))
                continue;
            BoxMetadata.Add(JsonSerializer.Deserialize<BoxMetadata>(File.ReadAllText(f), jsonSeri)!);
            foreach (var img in BoxMetadata[^1].Images)
            {
                if (ImageAssets.ContainsKey(img.Path))
                    continue;
                ImageAssets.Add(img.Path, SKBitmap.Decode($"{path}/{img.Path}"));
            }
        }
        BoxMetadata.Sort((a, b) => a.Index.CompareTo(b.Index));
        FontMetadata = [];
        foreach (var f in Directory.GetFiles(Path.Combine(path, "Fonts")))
        {
            if (!f.EndsWith(".jsonc"))
                continue;
            FontMetadata.Add(new(JsonSerializer.Deserialize<FontMetadataJson>(File.ReadAllText(f), jsonSeri)!));
            if (FontMetadata[^1].IsTtf)
            {
                FontMetadata[^1].Typeface = SKTypeface.FromFile($"{path}/{FontMetadata[^1].TtfPath}");
                continue;
            }
            var imgp = FontMetadata[^1].ImagePath;
            if (imgp is null || ImageAssets.ContainsKey(imgp))
                continue;
            ImageAssets.Add(imgp, SKBitmap.Decode($"{path}/{imgp}"));
        }
        FontMetadata.Sort((a, b) => a.Index.CompareTo(b.Index));
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
