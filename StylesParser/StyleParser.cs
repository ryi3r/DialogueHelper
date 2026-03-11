using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CSScriptLib;
using SixLabors.ImageSharp;

namespace DialogueHelper.StylesParser;

public class StyleParser
{
    public readonly StyleMetadata Metadata;
    public readonly List<BoxMetadata> BoxMetadata;
    public readonly List<FontMetadata> FontMetadata;
    public readonly string Folder;
    public readonly string? ScriptData;
    public readonly Dictionary<string, Bitmap> ImageAssets = [];
    public StyleScript? Script;
    public bool InitializedScript;
    public readonly Dictionary<string, dynamic?> GlobalEnv = [];

    public StyleParser(string path)
    {
        Folder = path;
        var jsonSeri = new JsonSerializerOptions()
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
                var baseImage = Image.Load($"{path}/{img.Path}");
                var stream = new MemoryStream();
                baseImage.SaveAsWebp(stream);
                baseImage.Dispose();
                stream.Seek(0, SeekOrigin.Begin);
                ImageAssets.Add(img.Path, new Bitmap(stream));
                stream.Dispose();
            }
        }
        foreach (var font in FontMetadata)
        {
            if (ImageAssets.ContainsKey(font.ImagePath))
                continue;
            var baseImage = Image.Load($"{path}/{font.ImagePath}");
            var stream = new MemoryStream();
            baseImage.SaveAsWebp(stream);
            baseImage.Dispose();
            stream.Seek(0, SeekOrigin.Begin);
            ImageAssets.Add(font.ImagePath, new Bitmap(stream));
            stream.Dispose();
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
