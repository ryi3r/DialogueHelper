extends RefCounted
# Template: GameMaker fonts

# This function will be called for each glyph (character)
# in the string, this allows you full control of how
# it gets drawn.
static func draw_glyph(data: IUserData) -> void:
	if data.char.is_ignore || (!data.font.glyphs.has(data.char.char) && !data.char.is_newline):
		return
	var scale := data.glyph.vscale * data.font.scale
	if data.char.is_newline:
		data.char.position_offset.x = 0
		var size: int = data.font.glyphs["A"].rect.size.y
		data.char.position_offset.y += (size + (size % 2) + (data.font.size % 2)) * (data.glyph.vscale * data.font.scale)
	else:
		var glyph: IGlyph = data.font.glyphs[data.char.char]
		data.char.glyph.position.x = data.char.start_position.x + data.char.position_offset.x + (glyph.offset * (data.glyph.vscale * data.font.scale))
		data.char.glyph.position.y = data.char.start_position.y + data.char.position_offset.y
		data.char.glyph.size.x = glyph.rect.size.x * (data.glyph.vscale * data.font.scale)
		data.char.glyph.size.y = glyph.rect.size.y * (data.glyph.vscale * data.font.scale)
		data.draw_glyph()
		if (glyph.shift + (glyph.shift % 2)) / max(glyph.rect.size.x, 1) >= 6:
			data.char.position_offset.x += ((glyph.shift - data.font.size) + glyph.offset) * (data.glyph.vscale * data.font.scale)
		else:
			data.char.position_offset.x += (glyph.shift + glyph.offset) * (data.glyph.vscale * data.font.scale)
