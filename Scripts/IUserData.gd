extends RefCounted
class_name IUserData

var env: Dictionary = {}
var font := IFont.new()
var glyph := IUserGlyph.new()
@warning_ignore("shadowed_global_identifier")
var char := IUserChar.new()

var __parent: Control = null

func draw_glyph() -> void:
	__parent.draw_texture_rect_region(font.texture, char.glyph, (font.glyphs[char.char] as IGlyph).rect, glyph.color)
	if char.glyph.position.x + char.glyph.size.x + 20 > __parent.custom_minimum_size.x:
		__parent.custom_minimum_size.x = char.glyph.position.x + char.glyph.size.x + 20
	if char.glyph.position.y + char.glyph.size.y + 20 > __parent.custom_minimum_size.y:
		__parent.custom_minimum_size.y = char.glyph.position.y + char.glyph.size.y + 20

func set_current_box(_box_id: int) -> void:
	Handle.main_node.update_box(_box_id)

func set_current_font(_font_id: int) -> void:
	Handle.main_node.update_font(_font_id)
	
func set_box_button_enabled(_enabled: bool) -> void:
	Handle.main_node.current_box_node.editable = _enabled

func set_font_button_enabled(_enabled: bool) -> void:
	Handle.main_node.current_font_node.editable = _enabled

func get_box_button_enabled() -> bool:
	return Handle.main_node.current_box_node.editable

func get_font_button_enabled() -> bool:
	return Handle.main_node.current_font_node.editable
