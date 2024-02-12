extends Node;

class GBox:
	var name = "";
	var texture = null;
	
	var scale = 1;
	var supports_portrait = false;
	
	var dialogue_offset = Vector2();
	var portrait_offset = Vector2();
	
	func _init(json = null):
		if json is Dictionary:
			if "Name" in json:
				name = json["Name"];
			if "Texture" in json:
				var path = Handle.style_get_path("Boxes/{Box}".format({
					"Box": json["Texture"],
				}));
				if FileAccess.file_exists(path):
					texture = load(path) if OS.has_feature("editor") else ImageTexture.create_from_image(Image.load_from_file(path));
			if "Scale" in json:
				scale = json["Scale"];
			if "SupportsPortrait" in json:
				supports_portrait = json["SupportsPortrait"];
			if "DialogueOffset" in json:
				var arr = json["DialogueOffset"];
				dialogue_offset = Vector2i(int(arr[0]), int(arr[1]));
			if "PortraitOffset" in json:
				var arr = json["PortraitOffset"];
				portrait_offset = Vector2i(int(arr[0]), int(arr[1]));
