using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Lua;

namespace DialogueHelper.StylesParser;

[LuaObject]
public partial class FontMetadata
{
    [LuaMember("name")]
    public readonly string Name;
    [LuaMember("image_path")]
    public readonly string ImagePath;
    [LuaMember("scale")]
    public readonly double Scale;
    [LuaMember("size")]
    public readonly double Size;
    [LuaMember("ascender")]
    public readonly double Ascender;
    [LuaMember("ascender_offset")]
    public readonly double AscenderOffset;
    public readonly List<GlyphMetadata> Glyphs;
    public readonly Dictionary<string, GlyphMetadata> GlyphDictionary;

    [LuaMember("get_glyphs")]
    public GlyphMetadata? GetGlyphs(int idx) => idx - 1 < Glyphs.Count ? Glyphs[idx - 1] : null;

    [LuaMember("size_glyphs")]
    public int SizeGlyphs() => Glyphs.Count;

    [LuaMember("get_glyph_dictionary")]
    public GlyphMetadata? GetGlyphDictionary(string key) => GlyphDictionary.TryGetValue(key, out var v) ? v : null;

    [LuaMember("has_glyph_dictionary")]
    public bool HasGlyphDictionary(string key) => GlyphDictionary.ContainsKey(key);

    [LuaMember("keys_glyph_dictionary")]
    public LuaTable KeysGlyphDictionary()
    {
        var t = new LuaTable();
        foreach (var (i, v) in GlyphDictionary.Keys.Index())
            t.Insert(i + 1, v);
        return t;
    }
    
    public FontMetadata(FontMetadataJson font)
    {
        Name = font.Name;
        ImagePath = font.ImagePath;
        Scale = font.Scale;
        Size = font.Size;
        Ascender = font.Ascender;
        AscenderOffset = font.AscenderOffset;
        Glyphs = [];
        if (font.Glyphs != null)
        {
            foreach (var glyph in font.Glyphs)
            {
                Glyphs.Add(new()
                {
                    Char = glyph.Char,
                    Kerning = glyph.Kerning,
                    Offset = glyph.Offset,
                    Position = new(glyph.Position[0], glyph.Position[1]),
                    Shift = glyph.Shift,
                    Size = new(glyph.Size[0], glyph.Size[1]),
                });
            }
        }
        GlyphDictionary = [];
        foreach (var glyph in Glyphs)
            GlyphDictionary.Add(glyph.Char, glyph);
    }
}

[LuaObject]
public partial class GlyphMetadata
{
    [LuaMember("char")]
    public required string Char;
    [LuaMember("position")]
    public required LuaDoubleTuple Position;
    [LuaMember("size")]
    public required LuaDoubleTuple Size;
    [LuaMember("shift")]
    public required double Shift;
    [LuaMember("offset")]
    public required double Offset;
    public List<KerningMetadata> Kerning = [];

    [LuaMember("add_kerning")]
    public void AddKerning(KerningMetadata kern) => Kerning.Add(kern);

    [LuaMember("get_kerning")]
    public KerningMetadata? GetKerning(int idx) => idx - 1 < Kerning.Count ? Kerning[idx - 1] : null;

    [LuaMember("size_kerning")]
    public int SizeKerning() => Kerning.Count;

    [LuaMember("remove_kerning")]
    public void RemoveKerning(int idx)
    {
        if (idx - 1 < Kerning.Count)
            Kerning.RemoveAt(idx);
    }
}

[LuaObject]
public partial class KerningMetadata
{
    [LuaMember("preceding_char")]
    public required char PrecedingChar;
    [LuaMember("shift_modifier")]
    public required double ShiftModifier;
}

[UsedImplicitly]
public class FontMetadataJson
{
    [UsedImplicitly]
    public required string Name;
    [UsedImplicitly]
    public required string ImagePath;
    public double Scale = 1.0;
    public double Size = 10.0;
    public double Ascender = 10.0;
    public double AscenderOffset = 0.0;
    [UsedImplicitly]
    public required List<GlyphMetadataJson> Glyphs;
}

[UsedImplicitly]
public class GlyphMetadataJson
{
    [UsedImplicitly] public required string Char;
    [UsedImplicitly] public required double[] Position;
    [UsedImplicitly] public required double[] Size;
    [UsedImplicitly] public required double Shift;
    [UsedImplicitly] public required double Offset;
    [UsedImplicitly] public List<KerningMetadata> Kerning = [];
}
