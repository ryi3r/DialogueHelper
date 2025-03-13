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
		var _jd: Dictionary = _json
		if _jd.has(&"Name"):
			name = str(_jd.Name)
		if _jd.has(&"Texture"):
			var _path := Handle.style_get_path("Boxes/%s" % _jd.Texture)
			if FileAccess.file_exists(_path):
				texture = load(_path) if OS.has_feature(&"editor") else ImageTexture.create_from_image(Image.load_from_file(_path))
		if _jd.has(&"Scale"):
			scale = _jd.Scale as float
		if _jd.has(&"SupportsPortrait"):
			supports_portrait = _jd.SupportsPortrait as bool
		if _jd.has(&"DialogueOffset"):
			var _arr: Array[int] = []
			_arr.assign(_jd.DialogueOffset as Array)
			dialogue_offset = Vector2i(_arr[0], _arr[1])
		if _jd.has(&"PortraitOffset"):
			var _arr: Array[int] = []
			_arr.assign(_jd.PortraitOffset as Array)
			portrait_offset = Vector2i(_arr[0], _arr[1])
