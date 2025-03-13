extends RefCounted
class_name IGlyph

@warning_ignore("shadowed_global_identifier")
var char := ""
var rect := Rect2i(0, 0, 0, 0)
var shift := 0
var offset := 0

func _init(_json: Variant = null) -> void:
	if _json is Dictionary:
		var _jd: Dictionary = _json
		if _jd.has(&"Char"):
			char = str(_jd.Char)
		if _jd.has(&"X"):
			rect.position.x = _jd.X as int
		if _jd.has(&"Y"):
			rect.position.y = _jd.Y as int
		if _jd.has(&"Width"):
			rect.size.x = _jd.Width as int
		if _jd.has(&"Height"):
			rect.size.y = _jd.Height as int
		if _jd.has(&"Shift"):
			shift = _jd.Shift as int
		if _jd.has(&"Offset"):
			offset = _jd.Offset as int
