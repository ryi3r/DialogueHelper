using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DialogueHelper.StylesParser;

public class FontMetadata
{
    public readonly string Name;
    public readonly string ImagePath;
    public readonly double Scale;
    public readonly double Size;
    public readonly double Ascender;
    public readonly double AscenderOffset;
    public readonly List<GlyphMetadata> Glyphs;
    public readonly Dictionary<char, GlyphMetadata> GlyphDictionary;

    public FontMetadata(FontMetadataJson font)
    {
        Name = font.Name;
        ImagePath = font.ImagePath;
        Scale = font.Scale;
        Size = font.Size;
        Ascender = font.Ascender;
        AscenderOffset = font.AscenderOffset;
        Glyphs = [];
        foreach (var glyph in font.Glyphs)
        {
            Glyphs.Add(new()
            {
                Char = glyph.Char,
                Kerning = glyph.Kerning,
                Offset = glyph.Offset,
                Position = (glyph.Position[0], glyph.Position[1]),
                Shift = glyph.Shift,
                Size = (glyph.Size[0], glyph.Size[1]),
            });
        }
        GlyphDictionary = [];
        foreach (var glyph in Glyphs)
            GlyphDictionary.Add(glyph.Char, glyph);
    }
}

public class GlyphMetadata
{
    public required char Char;
    public required (double X, double Y) Position;
    public required (double X, double Y) Size;
    public required double Shift;
    public required double Offset;
    public List<KerningMetadata> Kerning = [];
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
    public required char Char;
    public required double[] Position;
    public required double[] Size;
    public required double Shift;
    public required double Offset;
    public List<KerningMetadata> Kerning = [];
}

public class KerningMetadata
{
    public required char PrecedingChar;
    public required double ShiftModifier;
}
