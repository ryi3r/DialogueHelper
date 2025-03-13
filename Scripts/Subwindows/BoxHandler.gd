extends Control
class_name BoxHandler

@onready var box: WBox = get_parent()

var last_layer_strings := []
var last_layer_colors := []
var last_pos := Vector2.ZERO
var queue_update_secs := -1.0
var force_update := false

var global_env := {}

func _process(_delta: float) -> void:
	var _update := false
	if Handle.layer_strings != last_layer_strings:
		last_layer_strings = Handle.layer_strings.duplicate()
		_update = true
	if Handle.layer_colors != last_layer_colors:
		last_layer_colors = Handle.layer_colors.duplicate()
		_update = true
	var _posx := (box.portrait_offset.x if box.supports_portrait && box.portrait_enabled else box.dialogue_offset.x) * Handle.visual_scale
	var _posy := (box.portrait_offset.y if box.supports_portrait && box.portrait_enabled else box.dialogue_offset.y) * Handle.visual_scale
	if Vector2(_posx, _posy) != last_pos:
		last_pos = Vector2(_posx, _posy)
		_update = true
	if queue_update_secs > 0.0:
		queue_update_secs -= _delta
		if queue_update_secs <= 0.0:
			_update = true
	if force_update:
		force_update = false
		_update = true
	if _update:
		queue_redraw()

func _draw() -> void:
	custom_minimum_size = Vector2.ZERO
	if Handle.font_data.is_empty():
		return
	if Handle.user_script is GDScript:
		if Handle.user_script_obj == null:
			Handle.user_script_obj = Handle.user_script.new()
		var _env := {}
		var _ls := Handle.layer_strings.duplicate()
		var _lc := Handle.layer_colors.duplicate()
		
		var _data := IUserData.new()
		_data.__parent = self
		_data.env = _env
		_data.global_env = global_env
		_data.glyph.layer_strings = _ls
		_data.glyph.layer_colors = _lc
		_data.glyph.vscale = Handle.visual_scale
		_data.char.start_position.x = last_pos.x
		_data.char.start_position.y = last_pos.y
		if Handle.user_script_obj.has_method("prepare_draw"):
			@warning_ignore("unsafe_method_access")
			Handle.user_script_obj.prepare_draw(_data)
		for _layer in range(Handle.layer_strings.size()):
			_data.glyph.current_layer = _layer
			_data.char.position_offset = Vector2.ZERO
			var _index := 0
			var _current_string: String = Handle.layer_strings[_layer]
			_data.glyph.color = Handle.layer_colors[_layer]
			for _layer_char: String in _current_string:
				# do this so that if the font was changed it changes in real time
				_data.font = Handle.font_data[Handle.current_font]
				_data.box = Handle.box_data[box.current_box]
				#
				_data.char.char = _layer_char
				_data.char.glyph = Rect2()
				_data.char.index = _index
				_data.char.string = _current_string
				if Handle.style_metadata.has("NewLines"):
					_data.char.is_newline = (Handle.style_metadata.NewLines as Array).has(_layer_char)
				if Handle.style_metadata.has("Ignore"):
					_data.char.is_ignore = (Handle.style_metadata.Ignore as Array).has(_layer_char)
				if Handle.user_script_obj.has_method("draw_glyph"):
					@warning_ignore("unsafe_method_access")
					Handle.user_script_obj.draw_glyph(_data)
				_index += 1
		if Handle.user_script_obj.has_method("draw_portrait") && Handle.main_node.box.portrait_enabled && Handle.main_node.box.supports_portrait:
			_data.char = null
			@warning_ignore("unsafe_method_access")
			Handle.user_script_obj.draw_portrait(_data)
		queue_update_secs = _data.queue_update_secs
		for _node in box.handle.get_children():
			if _node is Sprite2D:
				var _spr: Sprite2D = _node
				if _spr.texture != null:
					if _spr.position.x + (_spr.texture.get_width() * _spr.scale.x) > custom_minimum_size.x:
						custom_minimum_size.x = _spr.position.x + (_spr.texture.get_width() * _spr.scale.x)
					if _spr.position.y + (_spr.texture.get_height() * _spr.scale.y) > custom_minimum_size.y:
						custom_minimum_size.y = _spr.position.y + (_spr.texture.get_height() * _spr.scale.y)
