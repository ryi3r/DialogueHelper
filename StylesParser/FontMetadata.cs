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
    /// <summary>
    /// GlyphMetadata
    /// </summary>
    [LuaMember("glyphs")]
    public readonly LuaList Glyphs;
    /// <summary>
    /// <string, GlyphMetadata>
    /// </summary>
    [LuaMember("glyph_dictionary")]
    public readonly LuaDictionary GlyphDictionary;

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
                Glyphs.Add(new GlyphMetadata()
                {
                    Char = glyph.Char,
                    Kerning = [.. glyph.Kerning],
                    Offset = glyph.Offset,
                    Position = new(glyph.Position[0], glyph.Position[1]),
                    Shift = glyph.Shift,
                    Size = new(glyph.Size[0], glyph.Size[1]),
                });
            }
        }
        GlyphDictionary = [];
        foreach (var glyph in Glyphs)
            GlyphDictionary.Add(glyph.Read<GlyphMetadata>().Char, glyph.Read<GlyphMetadata>());
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
    /// <summary>
    /// KerningMetadata
    /// </summary>
    [LuaMember("kerning")]
    public LuaList Kerning = [];
}

[LuaObject]
public partial class KerningMetadata
{
    [LuaMember("preceding_char")]
    public required string PrecedingChar;
    [LuaMember("shift_modifier")]
    public required double ShiftModifier;
}

public class FontMetadataJson
{
    public required string Name;
    public required string ImagePath;
    public double Scale = 1;
    public double Size = 10;
    public double Ascender = 10;
    public double AscenderOffset = 0;
    public required List<GlyphMetadataJson> Glyphs;
}

public class GlyphMetadataJson
{
    public required string Char;
    public required double[] Position;
    public required double[] Size;
    public required double Shift;
    public required double Offset;
    public List<KerningMetadata> Kerning = [];
}
