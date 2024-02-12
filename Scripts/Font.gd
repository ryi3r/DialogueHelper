extends Node;

class GFont:
	var name = "";
	var glyphs = {};
	var texture = null;
	
	var size = 0;
	var ascender = 0;
	var ascender_offset = 0;
	var scale = 1;
	
	func _init(json = null):
		if json is Dictionary:
			if "Name" in json:
				name = json["Name"];
			if "Texture" in json:
				var path = Handle.style_get_path("Fonts/{Texture}".format({
					"Texture": json["Texture"]
				}));
				if FileAccess.file_exists(path):
					texture = load(path) if OS.has_feature("editor") else ImageTexture.create_from_image(Image.load_from_file(path));
			if "Size" in json:
				size = int(json["Size"]);
			if "Scale" in json:
				scale = json["Scale"];
			if "Ascender" in json:
				ascender = int(json["Ascender"]);
			if "AscenderOffset" in json:
				ascender_offset = int(json["AscenderOffset"]);
			if "Glyphs" in json:
				for glyph: Dictionary in json["Glyphs"]:
					if glyph.has("Char"):
						glyphs[str(glyph["Char"])] = Glyph.new(glyph);

class Glyph:
	@warning_ignore("shadowed_global_identifier")
	var char = "";
	var rect = Rect2i(0, 0, 0, 0);
	var shift = 0;
	var offset = 0;
	
	func _init(json = null):
		if json is Dictionary:
			if "Char" in json:
				char = json["Char"];
			if "X" in json:
				rect.position.x = int(json["X"]);
			if "Y" in json:
				rect.position.y = int(json["Y"]);
			if "Width" in json:
				rect.size.x = int(json["Width"]);
			if "Height" in json:
				rect.size.y = int(json["Height"]);
			if "Shift" in json:
				shift = int(json["Shift"]);
			if "Offset" in json:
				offset = int(json["Offset"]);

func get_font(index: int) -> GFont:
	if index >= Handle.font_data.size() || index < 0:
		return GFont.new();
	return Handle.font_data[index];
