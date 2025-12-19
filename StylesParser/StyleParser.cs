using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DialogueHelper.External;
using DialogueHelper.FileFormat;
using DialogueHelper.PreviewRenderer;
using DialogueHelper.Views;
using Lua;
using Lua.Standard;
using SixLabors.ImageSharp;

namespace DialogueHelper.StylesParser;

[LuaObject]
public partial class StyleParser : IDisposable
{
    public StyleMetadata Metadata = null!;
    public List<BoxMetadata> BoxMetadata = [];
    public List<FontMetadata> FontMetadata = [];
    public string Folder = null!;
    public string? ScriptData;
    public Dictionary<string, Bitmap> ImageAssets = [];
    public LuaState LuaState = LuaState.Create();
    public Dictionary<string, dynamic> GlobalEnv = [];
    private bool disposedValue;

    public static async Task<StyleParser> Create(string path)
    {
        var @this = new StyleParser
        {
            Folder = path,
        };
        @this.LuaState.OpenStandardLibraries();
        var jsonSeri = new JsonSerializerOptions()
        {
            IncludeFields = true,
            AllowTrailingCommas = true,
        };
        @this.Metadata = JsonSerializer.Deserialize<StyleMetadata>(File.ReadAllText($"{path}/Metadata.json"), jsonSeri)!;
        @this.BoxMetadata = JsonSerializer.Deserialize<List<BoxMetadata>>(File.ReadAllText($"{path}/Boxes.json"), jsonSeri)!;
        var list = JsonSerializer.Deserialize<List<FontMetadataJson>>(File.ReadAllText($"{path}/Fonts.json"),
            jsonSeri)!;
        @this.FontMetadata = new(list.Capacity);
        foreach (var font in list)
            @this.FontMetadata.Add(new FontMetadata(font));
        if (@this.Metadata.ScriptPath != null)
            @this.ScriptData = File.ReadAllText($"{path}/{@this.Metadata.ScriptPath}");
        await @this.LuaState.DoStringAsync(@this.ScriptData ?? "");
        @this.LuaState.Environment["CustomProperty"] = new CustomProperty(null!, null!, null!);
        @this.LuaState.Environment["CustomTool"] = new CustomTool(null!, LuaValue.Nil);
        @this.LuaState.Environment["CustomToolWindowHandler"] = new CustomToolWindowHandler(null!, null!);
        @this.LuaState.Environment["FileData"] = new FileData();
        @this.LuaState.Environment["FormatEntry"] = new FormatEntry();
        @this.LuaState.Environment["LastEdited"] = new LastEdited();
        @this.LuaState.Environment["StringContainer"] = new StringContainer();
        @this.LuaState.Environment["ExternalChar"] = new ExternalChar()
        {
            Char = null!,
            Index = -1,
            String = null!,
            IsIgnore = false,
            IsNewline = false,
        };
        @this.LuaState.Environment["ExternalData"] = new ExternalData(null!, null!, null!)
        {
            Style = null!,
            Font = null!,
            Box = null!,
            Char = null!,
            Glyph = null!,
        };
        @this.LuaState.Environment["ExternalGlyph"] = new ExternalGlyph();
        @this.LuaState.Environment["Bitmap"] = new LuaBitmap(null!);
        @this.LuaState.Environment["Image"] = new LuaImage(null!);
        @this.LuaState.Environment["IntTuple"] = new LuaIntTuple(0, 0);
        @this.LuaState.Environment["FloatTuple"] = new LuaFloatTuple(0, 0);
        @this.LuaState.Environment["DoubleTuple"] = new LuaDoubleTuple(0, 0);
        @this.LuaState.Environment["Color"] = new LuaColor(Color.White);
        @this.LuaState.Environment["BoxMetadata"] = new BoxMetadata()
        {
            Name = null!,
            Images = null!,
        };
        @this.LuaState.Environment["ImageMetadata"] = new ImageMetadata()
        {
            Path = null!,
        };
        @this.LuaState.Environment["FontMetadata"] = new FontMetadata(new()
        {
            Name = null!,
            ImagePath = null!,
            Glyphs = null!,
        });
        @this.LuaState.Environment["GlyphMetadata"] = new GlyphMetadata()
        {
            Char = "",
            Position = new(0, 0),
            Size = new(0, 0),
            Shift = 0,
            Offset = 0,
        };
        @this.LuaState.Environment["KerningMetadata"] = new KerningMetadata()
        {
            PrecedingChar = '\0',
            ShiftModifier = 0,
        };
        @this.LuaState.Environment["StyleParser"] = new StyleParser();
        @this.LuaState.Environment["InfoWindow"] = new InfoWindow();
        @this.LuaState.Environment["QuestionWindw"] = new QuestionWindow();
        @this.LuaState.Environment["LoadingWindow"] = new LoadingWindow();
        //@this.LuaState.Environment[""] = new();
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
                LuaState.Dispose();
            disposedValue = true;
        }
    }
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
