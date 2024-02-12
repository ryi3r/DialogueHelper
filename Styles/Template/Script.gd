# This is based on the template for GameMaker fonts (Undertale Yellow's script.)

# This is how the API looks like:
#
#

# This function will be called for each glyph (character)
# in the string, this allows you full control of how
# it gets drawn.
static func draw_glyph(data: Type.UserData):
    if data.ignore || (!data.font.glyphs.has(data.char) && !data.new_line):
        return;
    var scale = data.visual_scale * data.font.scale;
    if data.new_line:
        data.offset.x = 0;
        var a_glyph = data.font.glyphs["A"];
        var size = a_glyph.rect.size.y;
        data.offset.y += (size + (size % 2) + (data.font.size % 2)) * (data.visual_scale * data.font.scale);
    else:
        var glyph: GMFont.Glyph = data.font.glyphs[data.char];
        data.draw_glyph = true;
        data.glyph_rect.position.x = data.position.x + data.offset.x + (glyph.offset * (data.visual_scale * data.font.scale));
        data.glyph_rect.position.y = data.position.y + data.offset.y;
        data.glyph_rect.size.x = glyph.rect.size.x * (data.visual_scale * data.font.scale);
        data.glyph_rect.size.y = glyph.rect.size.y * (data.visual_scale * data.font.scale);
        if (glyph.shift + (glyph.shift % 2)) / max(glyph.rect.size.x, 1) >= 6:
            data.offset.x += ((glyph.shift - data.font.size) + glyph.offset) * (data.visual_scale * data.font.scale);
        else:
            data.offset.x += glyph.shift * (data.visual_scale * data.font.scale);