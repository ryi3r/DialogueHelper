using System;
using System.Collections.Generic;
using SkiaSharp;

namespace DialogueHelper.StylesParser;

public class FontMetadata
{
    public readonly string Name;
    public readonly string? ImagePath;
    public readonly string? TtfPath;
    public readonly double Scale;
    public readonly double Size;
    public readonly double Ascender;
    public readonly double AscenderOffset;
    public readonly List<GlyphMetadata> Glyphs;
    public readonly GlyphLookup GlyphDictionary;
    public readonly float Index = float.MaxValue;
    
    public SKTypeface? Typeface;
	
    public bool IsTtf => TtfPath is not null;


    public FontMetadata(FontMetadataJson font)
    {
        Name = font.Name;
        ImagePath = font.ImagePath;
        TtfPath = font.TtfPath;
        if (ImagePath is null && TtfPath is null) {
            throw new KeyNotFoundException($"The font \"{Name}\" doesn't have a ImagePath or a TtfPath defined, so the style couldn't be loaded");
        }
        Scale = font.Scale;
        Size = font.Size;
        Ascender = font.Ascender;
        AscenderOffset = font.AscenderOffset;
        Index = font.Index;
        Glyphs = [];
        foreach (var glyph in font.Glyphs ?? [])
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
        GlyphDictionary = new(this);
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
    public string? ImagePath;
    public string? TtfPath;
    public double Scale = 1;
    public double Size = 10;
    public double Ascender = 10;
    public double AscenderOffset = 0;
    public List<GlyphMetadataJson>? Glyphs;
    public float Index = float.MaxValue;
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
