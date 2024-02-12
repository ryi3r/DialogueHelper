extends Control;

@onready var box = get_parent();

func _process(_delta):
	queue_redraw();

func _draw():
	custom_minimum_size = Vector2();
	if Handle.font_data.is_empty():
		return;
	if Handle.user_script is GDScript:
		for layer in range(Handle.layer_strings.size()):
			var glyph = Type.DrawGlyph.new();
			glyph.position.x = box.portrait_offset.x if box.supports_portrait && box.portrait_enabled else box.dialogue_offset.x;
			glyph.position.y = box.portrait_offset.y if box.supports_portrait && box.portrait_enabled else box.dialogue_offset.x;
			glyph.layer = layer;
			glyph.layer_color = Handle.layer_colors[layer] if Handle.layer_colors.size() > layer else Color.WHITE;
			glyph.visual_scale = Handle.visual_scale;
			glyph.font = Handle.font_data[Handle.current_font];
			for layer_char in Handle.layer_strings[layer]:
				glyph.draw_glyph = false;
				glyph.char = layer_char;
				glyph.color = glyph.layer_color;
				if Handle.style_metadata.has("NewLines"):
					glyph.new_line = layer_char in Handle.style_metadata["NewLines"];
				if Handle.style_metadata.has("Ignore"):
					glyph.ignore = layer_char in Handle.style_metadata["Ignore"];
				glyph.glyph_rect = Rect2();
				if Handle.user_script.has_method("draw_glyph"):
					Handle.user_script.draw_glyph(glyph);
					if glyph.draw_glyph:
						draw_texture_rect_region(glyph.font.texture, glyph.glyph_rect, glyph.font.glyphs[layer_char].rect, glyph.color);
						if glyph.glyph_rect.position.x + glyph.glyph_rect.size.x + 20 > custom_minimum_size.x:
							custom_minimum_size.x = glyph.glyph_rect.position.x + glyph.glyph_rect.size.x + 20;
						if glyph.glyph_rect.position.y + glyph.glyph_rect.size.y + 20 > custom_minimum_size.y:
							custom_minimum_size.y = glyph.glyph_rect.position.y + glyph.glyph_rect.size.y + 20;
