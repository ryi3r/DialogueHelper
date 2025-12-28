using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Avalonia.Media.Imaging;
using CSScriptLib;
using DialogueHelper.External;
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
    public Type? Script;
    public Assembly? ScriptAsm;
    public bool InitializedScript;
    public readonly Dictionary<string, dynamic?> GlobalEnv = [];

    public StyleParser(string path)
    {
        Folder = path;
        var jsonSeri = new JsonSerializerOptions()
        {
            IncludeFields = true,
            AllowTrailingCommas = true,
        };
        Metadata = JsonSerializer.Deserialize<StyleMetadata>(File.ReadAllText($"{path}/Metadata.json"), jsonSeri)!;
        BoxMetadata = JsonSerializer.Deserialize<List<BoxMetadata>>(File.ReadAllText($"{path}/Boxes.json"), jsonSeri)!;
        var list = JsonSerializer.Deserialize<List<FontMetadataJson>>(File.ReadAllText($"{path}/Fonts.json"),
            jsonSeri)!;
        FontMetadata = new(list.Capacity);
        foreach (var font in list)
            FontMetadata.Add(new FontMetadata(font));
        if (Metadata.ScriptPath != null)
            ScriptData = File.ReadAllText($"{path}/{Metadata.ScriptPath}");
        foreach (var box in BoxMetadata)
        {
            foreach (var img in box.Images)
            {
                if (!ImageAssets.ContainsKey(img.Path))
                {
                    var baseImage = Image.Load($"{path}/{img.Path}");
                    using var stream = new MemoryStream();
                    baseImage.SaveAsWebp(stream);
                    baseImage.Dispose();
                    stream.Seek(0, SeekOrigin.Begin);
                    ImageAssets.Add(img.Path, new Bitmap(stream));
                }
            }
        }
        foreach (var font in FontMetadata)
        {
            if (!ImageAssets.ContainsKey(font.ImagePath))
            {
                var baseImage = Image.Load($"{path}/{font.ImagePath}");
                using var stream = new MemoryStream();
                baseImage.SaveAsWebp(stream);
                baseImage.Dispose();
                stream.Seek(0, SeekOrigin.Begin);
                ImageAssets.Add(font.ImagePath, new Bitmap(stream));
            }
        }
    }

    public void CompileCode()
    {
        if (!Directory.Exists("RuntimeComp"))
            Directory.CreateDirectory("RuntimeComp");
        if (ScriptAsm == null)
        {
            ScriptAsm = CSScript.Evaluator
                .CompileCode(ScriptData, new()
                {
                    RootClass = "Script",
                    AssemblyFile = $"RuntimeComp/IL-{Path.GetFileName(Folder)}.dll",
                });
            Script = ScriptAsm.GetType("Script");
        }
    }
}
