extends RefCounted
class_name IFont

var name := ""
var glyphs := {}
var texture: Texture2D = null

var size := 0
var ascender := 0
var ascender_offset := 0
var scale := 1.0

func _init(json: Variant = null) -> void:
	if json is Dictionary:
		if "Name" in json:
			name = str(json["Name"])
		if "Texture" in json:
			var path := str(Handle.style_get_path("Fonts/%s" % json["Texture"]))
			if FileAccess.file_exists(path):
				texture = load(path) if OS.has_feature("editor") else ImageTexture.create_from_image(Image.load_from_file(path))
		if "Size" in json:
			size = json["Size"] as int
		if "Scale" in json:
			scale = json["Scale"] as int
		if "Ascender" in json:
			ascender = json["Ascender"] as int
		if "AscenderOffset" in json:
			ascender_offset = json["AscenderOffset"] as int
		if "Glyphs" in json:
			for glyph: Dictionary in json["Glyphs"]:
				if glyph.has("Char"):
					glyphs[str(glyph["Char"])] = IGlyph.new(glyph)

static func get_font(_index: int) -> IFont:
	if _index >= Handle.font_data.size() || _index < 0:
		return null
	return Handle.font_data[_index]
