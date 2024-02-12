extends Control;

@onready var box = get_parent();

func _process(_delta):
	queue_redraw();

func _draw():
	custom_minimum_size = Vector2();
	if Handle.font_data.is_empty():
		return;
	if Handle.user_script is GDScript:
		var env = {};
		var ls = Handle.layer_strings.duplicate();
		var lc = Handle.layer_colors.duplicate();
		for layer in range(Handle.layer_strings.size()):
			var data = Type.UserData.new();
			data.__parent = self;
			data.env = env;
			data.glyph.layer_strings = ls;
			data.glyph.layer_colors = lc;
			data.glyph.current_layer = layer;
			data.glyph.vscale = Handle.visual_scale;
			data.char.start_position.x = box.portrait_offset.x if box.supports_portrait && box.portrait_enabled else box.dialogue_offset.x;
			data.char.start_position.y = box.portrait_offset.y if box.supports_portrait && box.portrait_enabled else box.dialogue_offset.y;
			data.font = Handle.font_data[Handle.current_font];
			for layer_char in Handle.layer_strings[layer]:
				data.char.char = layer_char;
				data.char.glyph = Rect2();
				data.glyph.color = Handle.layer_colors[layer];
				if Handle.style_metadata.has("NewLines"):
					data.char.is_newline = layer_char in Handle.style_metadata["NewLines"];
				if Handle.style_metadata.has("Ignore"):
					data.char.is_ignore = layer_char in Handle.style_metadata["Ignore"];
				if Handle.user_script.has_method("draw_glyph"):
					Handle.user_script.draw_glyph(data);
					
