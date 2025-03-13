extends RefCounted
class_name IFont

var name := ""
var glyphs := {}
var texture: Texture2D = null

var size := 0
var ascender := 0
var ascender_offset := 0
var scale := 1.0

func _init(_json: Variant = null) -> void:
	if _json is Dictionary:
		var _jd: Dictionary = _json
		if _jd.has(&"Name"):
			name = str(_jd.Name)
		if _jd.has(&"Texture"):
			var path := str(Handle.style_get_path("Fonts/%s" % _jd.Texture))
			if FileAccess.file_exists(path):
				texture = load(path) if OS.has_feature("editor") else ImageTexture.create_from_image(Image.load_from_file(path))
		if _jd.has(&"Size"):
			size = _jd.Size as int
		if _jd.has(&"Scale"):
			scale = _jd.Scale as int
		if _jd.has(&"Ascender"):
			ascender = _jd.Ascender as int
		if _jd.has(&"AscenderOffset"):
			ascender_offset = _jd.AscenderOffset as int
		if _jd.has(&"Glyphs"):
			for glyph: Dictionary in _jd.Glyphs:
				if glyph.has(&"Char"):
					glyphs[str(glyph.Char)] = IGlyph.new(glyph)

static func get_font(_index: int) -> IFont:
	if _index >= Handle.font_data.size() || _index < 0:
		return null
	return Handle.font_data[_index]
