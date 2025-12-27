using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
    public readonly Dictionary<string, GlyphMetadata> GlyphDictionary;

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

public partial class GlyphMetadata
{
    public required string Char;
    public required (double X, double Y) Position;
    public required (double X, double Y) Size;
    public required double Shift;
    public required double Offset;
    public List<KerningMetadata> Kerning = [];
}

public partial class KerningMetadata
{
    public required char PrecedingChar;
    public required double ShiftModifier;
}

public class FontMetadataJson
{
    public required string Name;
    public required string ImagePath;
    public double Scale = 1.0;
    public double Size = 10.0;
    public double Ascender = 10.0;
    public double AscenderOffset = 0.0;
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
