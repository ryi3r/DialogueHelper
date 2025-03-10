extends RefCounted
class_name IUserData

var env: Dictionary = {}
var global_env: Dictionary = {}
var font := IFont.new()
var glyph := IUserGlyph.new()
@warning_ignore("shadowed_global_identifier")
var char := IUserChar.new()
var box := IBox.new()
var queue_update_secs := -1.0

var __parent: Control = null

func load_texture(_path: String) -> Texture2D:
	var _texture: Texture2D = null
	_path = Handle.style_get_path(_path)
	if FileAccess.file_exists(_path):
		_texture = load(_path) if OS.has_feature("editor") else ImageTexture.create_from_image(Image.load_from_file(_path))
	return _texture

func draw_glyph() -> void:
	__parent.draw_texture_rect_region(font.texture, char.glyph, (font.glyphs[char.char] as IGlyph).rect, glyph.color)
	if char.glyph.position.x + char.glyph.size.x + 20 > __parent.custom_minimum_size.x:
		__parent.custom_minimum_size.x = char.glyph.position.x + char.glyph.size.x + 20
	if char.glyph.position.y + char.glyph.size.y + 20 > __parent.custom_minimum_size.y:
		__parent.custom_minimum_size.y = char.glyph.position.y + char.glyph.size.y + 20

func draw_texture(_texture: Texture2D, _position: Vector2, _modulate: Color = Color.WHITE) -> void:
	__parent.draw_texture(_texture, _position, _modulate)
	if _position.x + _texture.get_width() + 20 > __parent.custom_minimum_size.x:
		__parent.custom_minimum_size.x = _position.x + _texture.get_width() + 20
	if _position.y + _texture.get_height() + 20 > __parent.custom_minimum_size.y:
		__parent.custom_minimum_size.y = _position.y + _texture.get_height() + 20

func draw_texture_rect(_texture: Texture2D, _rect: Rect2, _tile: bool, _modulate: Color = Color.WHITE, _transpose := false) -> void:
	__parent.draw_texture_rect(_texture, _rect, _tile, _modulate, _transpose)
	if _rect.position.x + _rect.size.x + 20 > __parent.custom_minimum_size.x:
		__parent.custom_minimum_size.x = _rect.position.x + _rect.size.x + 20
	if _rect.position.y + _rect.size.y + 20 > __parent.custom_minimum_size.y:
		__parent.custom_minimum_size.y = _rect.position.y + _rect.size.y + 20

func draw_texture_rect_region(_texture: Texture2D, _rect: Rect2, _src_rect: Rect2, _modulate: Color = Color.WHITE, _transpose := false, _clip_uv := true) -> void:
	__parent.draw_texture_rect_region(_texture, _rect, _src_rect, _modulate, _transpose, _clip_uv)
	if _rect.position.x + _rect.size.x + 20 > __parent.custom_minimum_size.x:
		__parent.custom_minimum_size.x = _rect.position.x + _rect.size.x + 20
	if _rect.position.y + _rect.size.y + 20 > __parent.custom_minimum_size.y:
		__parent.custom_minimum_size.y = _rect.position.y + _rect.size.y + 20

func set_current_box(_box_id: int) -> void:
	Handle.main_node.update_box(_box_id, false)

func set_current_font(_font_id: int) -> void:
	Handle.main_node.update_font(_font_id, false)

func set_viewing_scale(_scale: float) -> void:
	Handle.main_node.current_scale_node.value = _scale

func get_current_box() -> int:
	return Handle.main_node.current_box

func get_current_font() -> int:
	return Handle.main_node.current_font_id

func get_viewing_scale() -> float:
	return Handle.main_node.current_scale_node.value

func set_box_button_enabled(_enabled: bool) -> void:
	Handle.main_node.current_box_node.editable = _enabled

func set_font_button_enabled(_enabled: bool) -> void:
	Handle.main_node.current_font_node.editable = _enabled

func set_box_portrait(_enabled: bool) -> void:
	Handle.main_node.box.portrait_enabled = _enabled
	Handle.main_node.enable_portrait.button_pressed = _enabled

func get_box_portrait() -> bool:
	return Handle.main_node.box.portrait_enabled && Handle.main_node.box.supports_portrait

func get_box_supports_portrait() -> bool:
	return Handle.main_node.box.supports_portrait

func get_box_button_enabled() -> bool:
	return Handle.main_node.current_box_node.editable

func get_font_button_enabled() -> bool:
	return Handle.main_node.current_font_node.editable

func get_font(_font: int) -> IFont:
	return Handle.font_data[_font]

func get_box(_box: int) -> IBox:
	return Handle.box_data[_box]
