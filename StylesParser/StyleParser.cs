using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using DialogueHelper.External;
using DialogueHelper.FileFormat;
using DialogueHelper.PreviewRenderer;
using DialogueHelper.Views;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using SixLabors.ImageSharp;

namespace DialogueHelper.StylesParser;

public partial class StyleParser : IDisposable
{
    public StyleMetadata Metadata = null!;
    public List<BoxMetadata> BoxMetadata = [];
    public List<FontMetadata> FontMetadata = [];
    public string Folder = null!;
    public string? ScriptData;
    public Dictionary<string, Bitmap> ImageAssets = [];
    public V8ScriptEngine Engine = new(V8ScriptEngineFlags.EnableTaskPromiseConversion);
    public Dictionary<string, dynamic> GlobalEnv = [];
    private bool disposedValue;

    public static async Task<StyleParser> Create(string path)
    {
        var @this = new StyleParser
        {
            Folder = path,
        };
        var jsonSeri = new JsonSerializerOptions()
        {
            IncludeFields = true,
            AllowTrailingCommas = true,
        };
        @this.Metadata = JsonSerializer.Deserialize<StyleMetadata>(File.ReadAllText($"{path}/Metadata.json"), jsonSeri)!;
        @this.BoxMetadata = JsonSerializer.Deserialize<List<BoxMetadata>>(File.ReadAllText($"{path}/Boxes.json"), jsonSeri)!;
        var list = JsonSerializer.Deserialize<List<FontMetadataJson>>(File.ReadAllText($"{path}/Fonts.json"), jsonSeri)!;
        @this.FontMetadata = new(list.Capacity);
        foreach (var font in list)
            @this.FontMetadata.Add(new FontMetadata(font));
        if (@this.Metadata.ScriptPath != null)
            @this.ScriptData = File.ReadAllText($"{path}/{@this.Metadata.ScriptPath}");
        @this.Engine.AddHostObject("host", new HostFunctions());
        @this.Engine.AddHostObject("lib", new HostTypeCollection("mscorlib", "System.Core"));
        @this.Engine.AddHostTypes([
            typeof(Console),
            typeof(CustomProperty),
            typeof(CustomTool),
            typeof(CustomToolWindowHandler),
            typeof(FileData),
            typeof(FormatEntry),
            typeof(LastEdited),
            typeof(StringContainer),
            typeof(ExternalChar),
            typeof(ExternalData),
            typeof(ExternalGlyph),
            typeof(Bitmap),
            typeof(Image),
            typeof(Color),
            typeof(BoxMetadata),
            typeof(ImageMetadata),
            typeof(FontMetadata),
            typeof(GlyphMetadata),
            typeof(KerningMetadata),
            typeof(StyleParser),
            typeof(InfoWindow),
            typeof(QuestionWindow),
            typeof(CustomTool),
            typeof(FilePickerOpenOptions),
            typeof(FilePickerSaveOptions),
            typeof(FolderPickerOpenOptions),
            typeof(FilePickerFileType),
            typeof(FilePickerFileTypes),
        ]);
        @this.Engine.Execute(@this.ScriptData);
        Console.WriteLine(@this.Engine.Script.Test);
        foreach (var box in @this.BoxMetadata)
        {
            foreach (var img in box.Images)
            {
                if (!@this.ImageAssets.ContainsKey(img.Path))
                {
                    var baseImage = Image.Load($"{path}/{img.Path}");
                    using var stream = new MemoryStream();
                    baseImage.SaveAsWebp(stream);
                    baseImage.Dispose();
                    stream.Seek(0, SeekOrigin.Begin);
                    @this.ImageAssets.Add(img.Path, new Bitmap(stream));
                }
            }
        }
        foreach (var font in @this.FontMetadata)
        {
            if (!@this.ImageAssets.ContainsKey(font.ImagePath))
            {
                var baseImage = Image.Load($"{path}/{font.ImagePath}");
                using var stream = new MemoryStream();
                baseImage.SaveAsWebp(stream);
                baseImage.Dispose();
                stream.Seek(0, SeekOrigin.Begin);
                @this.ImageAssets.Add(font.ImagePath, new Bitmap(stream));
            }
        }
        return @this;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
                Engine.Dispose();
            disposedValue = true;
        }
    }
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
