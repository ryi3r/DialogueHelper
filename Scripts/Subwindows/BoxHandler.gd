extends Control

@onready var box: WBox = get_parent()

var last_layer_strings := []
var last_layer_colors := []
var last_pos := Vector2.ZERO

func _process(_delta: float) -> void:
	var _update := false
	if Handle.layer_strings != last_layer_strings:
		last_layer_strings = Handle.layer_strings.duplicate()
		_update = true
	if Handle.layer_colors != last_layer_colors:
		last_layer_colors = Handle.layer_colors.duplicate()
		_update = true
	var _posx := box.portrait_offset.x if box.supports_portrait && box.portrait_enabled else box.dialogue_offset.x
	var _posy := box.portrait_offset.y if box.supports_portrait && box.portrait_enabled else box.dialogue_offset.y
	if Vector2(_posx, _posy) != last_pos:
		last_pos = Vector2(_posx, _posy)
		_update = true
	if _update:
		queue_redraw()

func _draw() -> void:
	custom_minimum_size = Vector2.ZERO
	if Handle.font_data.is_empty():
		return
	if Handle.user_script is GDScript:
		var _env := {}
		var _ls := Handle.layer_strings.duplicate()
		var _lc := Handle.layer_colors.duplicate()
		
		var _data := IUserData.new()
		_data.__parent = self
		_data.env = _env
		_data.glyph.layer_strings = _ls
		_data.glyph.layer_colors = _lc
		_data.glyph.vscale = Handle.visual_scale
		_data.char.start_position.x = box.portrait_offset.x if box.supports_portrait && box.portrait_enabled else box.dialogue_offset.x
		_data.char.start_position.y = box.portrait_offset.y if box.supports_portrait && box.portrait_enabled else box.dialogue_offset.y
		_data.font = Handle.font_data[Handle.current_font]
		for _layer in range(Handle.layer_strings.size()):
			_data.glyph.current_layer = _layer
			_data.char.position_offset = Vector2.ZERO
			for _layer_char: String in Handle.layer_strings[_layer]:
				_data.char.char = _layer_char
				_data.char.glyph = Rect2()
				_data.glyph.color = Handle.layer_colors[_layer]
				if Handle.style_metadata.has("NewLines"):
					_data.char.is_newline = (Handle.style_metadata["NewLines"] as Array).has(_layer_char)
				if Handle.style_metadata.has("Ignore"):
					_data.char.is_ignore = (Handle.style_metadata["Ignore"] as Array).has(_layer_char)
				if Handle.user_script.has_method("draw_glyph"):
					@warning_ignore("unsafe_method_access")
					Handle.user_script.draw_glyph(_data)
