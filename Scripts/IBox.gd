extends RefCounted
class_name IBox

var name := ""
var texture: Texture2D = null

var scale := 1.0
var supports_portrait := false

var dialogue_offset := Vector2.ZERO
var portrait_offset := Vector2.ZERO

func _init(_json: Variant = null) -> void:
	if _json is Dictionary:
		if "Name" in _json:
			name = str(_json["Name"])
		if "Texture" in _json:
			var _path := Handle.style_get_path("Boxes/%s" % _json["Texture"])
			if FileAccess.file_exists(_path):
				texture = load(_path) if OS.has_feature("editor") else ImageTexture.create_from_image(Image.load_from_file(_path))
		if "Scale" in _json:
			scale = _json["Scale"] as float
		if "SupportsPortrait" in _json:
			supports_portrait = _json["SupportsPortrait"] as bool
		if "DialogueOffset" in _json:
			var _arr: Array[int] = []
			_arr.assign(_json["DialogueOffset"] as Array)
			dialogue_offset = Vector2i(_arr[0], _arr[1])
		if "PortraitOffset" in _json:
			var _arr: Array[int] = []
			_arr.assign(_json["PortraitOffset"] as Array)
			portrait_offset = Vector2i(_arr[0], _arr[1])
