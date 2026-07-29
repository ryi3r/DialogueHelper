using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SkiaSharp;

namespace DialogueHelper.StylesParser;

public class GlyphLookup(FontMetadata font)
{
	readonly Dictionary<char, GlyphMetadata> _cache = [];

	public GlyphMetadata this[char c] {
		get {
			if (_cache.TryGetValue(c, out var existing))
				return existing;

			if (!font.IsTtf)
				throw new KeyNotFoundException(
					$"No glyph defined for character '{c}' (0x{(int)c:X4}) in font '{font.Name}'.");

			return Rasterize(c);
		}
	}

	public bool ContainsKey(char c)
	{
		if (_cache.ContainsKey(c))
			return true;
		return font.IsTtf && font.Typeface is not null && font.Typeface.GetGlyph(c) != 0;
	}

	public void Add(char c, GlyphMetadata glyph) => _cache[c] = glyph;

	GlyphMetadata Rasterize(char c)
	{
		using var skFont = new SKFont(font.Typeface, (float)font.Size);
		var advance = skFont.MeasureText(c.ToString());
		skFont.GetFontMetrics(out var metrics);

		var glyph = new GlyphMetadata {
			Char = c,
			Position = (0, 0),
			Size = (advance, metrics.Descent - metrics.Ascent),
			Shift = advance,
			Offset = 0,
		};
		_cache[c] = glyph;
		font.Glyphs.Add(glyph); // needed so code that scans Font.Glyphs directly (not through this indexer) sees TTF characters too
		return glyph;
	}
	
	public bool TryGetValue(char c, [NotNullWhen(true)] out GlyphMetadata? glyph)
	{
		if (_cache.TryGetValue(c, out var existing))
		{
			glyph = existing;
			return true;
		}
		if (!font.IsTtf)
		{
			glyph = null;
			return false;
		}
		glyph = Rasterize(c);
		return true;
	}
}
