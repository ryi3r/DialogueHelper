extends RefCounted
class_name IGlyph

@warning_ignore("shadowed_global_identifier")
var char := ""
var rect := Rect2i(0, 0, 0, 0)
var shift := 0
var offset := 0

func _init(json: Variant = null) -> void:
	if json is Dictionary:
		if "Char" in json:
			char = str(json["Char"])
		if "X" in json:
			rect.position.x = json["X"] as int
		if "Y" in json:
			rect.position.y = json["Y"] as int
		if "Width" in json:
			rect.size.x = json["Width"] as int
		if "Height" in json:
			rect.size.y = json["Height"] as int
		if "Shift" in json:
			shift = json["Shift"] as int
		if "Offset" in json:
			offset = json["Offset"] as int
