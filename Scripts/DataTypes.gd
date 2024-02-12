extends Node;

class StringContainer:
	var id = -1;
	var content = "";
	var original_content = "";
	var last_edited = LastEdited.new();
	var equal_strings = null;
	var layer_strings = null;
	var layer_colors = null;
	var box_style = 0;
	var font_style = 0;
	var font_scale = 1.0;
	var enable_portrait = false;
	
	var equal_strings_i = null;

	func _init(json = null):
		if json is Dictionary:
			if "ID" in json:
				id = json["ID"];
			if "Content" in json:
				content = json["Content"];
			if "OriginalContent" in json:
				original_content = json["OriginalContent"];
			if "LastEdited" in json:
				last_edited = LastEdited.new(json["LastEdited"]);
			if "EqualStrings" in json:
				equal_strings = json["EqualStrings"];
			if "LayerStrings" in json:
				layer_strings = json["LayerStrings"];
			if "LayerColors" in json:
				layer_colors = json["LayerColors"];
				for i in range(layer_colors.size()):
					layer_colors[i] = Color.hex(int(layer_colors[i]));
			if "BoxStyle" in json:
				box_style = json["BoxStyle"];
			if "FontStyle" in json:
				font_style = json["FontStyle"];
			if "FontScale" in json:
				font_scale = json["FontScale"];
			if "EnablePortrait" in json:
				enable_portrait = json["EnablePortrait"];
		elif json is FileFormat.FormatEntry:
			var djson: Dictionary = json.data;
			if "ID" in djson:
				id = djson["ID"];
			if "OriginalContent" in djson:
				content = djson["OriginalContent"];
				original_content = djson["OriginalContent"];
			if "Content" in djson:
				content = djson["Content"];
			if "LastEdited" in djson:
				last_edited = LastEdited.new(djson["LastEdited"]);
			if "EqualStrings" in djson:
				var es = djson["EqualStrings"].split(",");
				var esi = [];
				for i in range(es.size()):
					esi.append(int(es[i]));
				equal_strings = es;
			if "LayerStrings" in djson:
				var ls = djson["LayerStrings"].split(",");
				var sls = [content];
				for i in range(ls.size()):
					sls.append(ls[i].uri_decode());
				layer_strings = sls;
			else:
				layer_strings = [content];
			while layer_strings.size() < Handle.layers:
				layer_strings.append("");
			if "LayerColors" in djson:
				var lc = djson["LayerColors"].split(",");
				var clc = [];
				for i in range(lc.size()):
					clc.append(Color.hex(str(lc[i]).hex_to_int()));
				layer_colors = clc;
			else:
				layer_colors = [];
			while layer_colors.size() < Handle.layers:
				layer_colors.append(Color.WHITE);
			if "BoxStyle" in djson:
				box_style = int(djson["BoxStyle"]);
			if "FontStyle" in djson:
				font_style = int(djson["FontStyle"]);
			if "FontScale" in djson:
				font_scale = int(djson["FontScale"]);
			if "EnablePortrait" in djson:
				enable_portrait = bool(djson["EnablePortrait"]);

	func _to_string():
		var entry = FileFormat.FormatEntry.new();
		entry.disable_uri = ["LayerStrings", "LayerColors", "LastEdited", "EqualStrings"];
		entry.kind = 1;
		entry.data["ID"] = id;
		if content != original_content:
			entry.data["Content"] = content;
		entry.data["OriginalContent"] = original_content;
		if last_edited.author != "" && last_edited.timestamp != -1:
			entry.data["LastEdited"] = str(last_edited);
		#if equal_strings is Array:
			#entry.data["EqualStrings"] = ",".join(PackedStringArray(equal_strings));
		if equal_strings_i is int:
			entry.data["EqualStringsI"] = equal_strings_i;
		if !"".join(PackedStringArray(layer_strings.slice(1))).is_empty():
			var ls = layer_strings.slice(1);
			var cls = [];
			var last = 0;
			for i in range(ls.size()):
				if !ls[i].is_empty():
					last = i + 1;
				cls.append(ls[i].uri_encode());
			entry.data["LayerStrings"] = ",".join(PackedStringArray((ls as Array).slice(0, last)));
		var lc = [];
		var lastc = 0;
		for color: Color in layer_colors:
			for i in range(layer_colors.size()):
				if layer_colors[i] != Color.WHITE:
					lastc = i + 1;
				lc.append(String.num_uint64(layer_colors[i].to_rgba32(), 16));
		if !"".join(PackedStringArray(lc)).replace("f", "").is_empty():
			entry.data["LayerColors"] = ",".join(PackedStringArray(lc.slice(0, lastc)));
		if box_style != 0:
			entry.data["BoxStyle"] = box_style;
		if font_style != 0:
			entry.data["FontStyle"] = font_style;
		if font_scale != 1:
			entry.data["FontScale"] = font_scale;
		if enable_portrait:
			entry.data["EnablePortrait"] = enable_portrait;
		return str(entry);

	func update():
		if layer_strings == null:
			layer_strings = [content, "", "", "", ""];
		if layer_colors == null:
			layer_colors = [0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff];
		if !(id is int):
			push_warning("ID is not an Integer!");
		elif id < 0:
			push_warning("ID is not initialized (ID < 0)!");

class LastEdited:
	var timestamp = -1;
	var author = "";

	func _init(json = null, _timestamp = null, _author = null):
		if json is Dictionary:
			if "Timestamp" in json:
				timestamp = json["Timestamp"];
			if "Author" in json:
				author = json["Author"];
		elif json is String:
			var data = json.split(",");
			author = data[0].uri_decode();
			timestamp = int(data[1]);
		elif _timestamp is int && _author is String:
			timestamp = _timestamp;
			author = _author;

	func _to_string():
		return author.uri_encode() + "," + str(int(timestamp));

class StringTable:
	var name = "";
	var entry = null;
	var index = -1;

	func _init(_name = "", _entry = null, _index = -1):
		name = _name;
		entry = _entry;
		index = _index;

	func to_json() -> Dictionary:
		return {
			"Name": name,
			"Entry": entry,
			"Index": index,
		};

	func _to_string():
		return JSON.stringify(to_json(), "", false);

class GitResponse:
	var success = true;
	var output = [];

class Git:
	var url = "";
	var branch = "";
	
	func gpath(path):
		return ProjectSettings.globalize_path("user://" + path);
	
	func _run_command(args: Array, show_console: bool = false) -> GitResponse:
		var opt = [];
		OS.execute("cmd.exe", PackedStringArray(args), opt, false, show_console);
		var r = GitResponse.new();
		print("Output:");
		for line: String in opt:
			print(line);
			if (line.begins_with("error:") || line.begins_with("fatal:")):
				r.success = false;
		print("=======");
		r.output = opt;
		return r;
	
	func clone() -> GitResponse:
		return _run_command(["/c", "git -C \"{3}\" clone --branch \"{2}\" \"{0}\" \"{1}\"".format([
			url,
			gpath("repo/"),
			branch,
			gpath(""),
		])], true);
	
	func pull() -> GitResponse:
		return _run_command(["/c", "git -C \"{0}\" restore . & git -C \"{0}\" checkout \"{1}\" & git -C \"{0}\" pull -f".format([
			gpath("repo/"),
			branch,
		])]);
	
	func commit(message: String) -> GitResponse:
		return _run_command(["/c", "git -C \"{0}\" add . & git -C \"{0}\" commit -m \"{2}\" & git -C \"{0}\" push origin \"{1}\"".format([
			gpath("repo/"),
			branch,
			message,
		])]);
	
	func set_url() -> GitResponse:
		return _run_command(["/c", "git -C \"{0}\" remote set-url origin \"{1}\"".format([
			gpath("repo/"),
			url,
		])]);

class GitConflict:
	var entry = null;
	var keep = GitConflictKeep.NoChoice;
	var current_string = null;
	var git_string = null;

class DrawGlyph:
	@warning_ignore("shadowed_global_identifier")
	var char = "";
	var position = Vector2();
	var offset = Vector2();
	
	var ignore = false;
	var new_line = false;
	
	var color = Color.WHITE;
	var layer_color = Color.WHITE;
	
	var layer = 0;
	var current_char = 0;
	
	var font = GMFont.GFont.new();
	var visual_scale = 1;
	
	var draw_glyph = false;
	var glyph_rect = Rect2();

enum GitConflictKeep {
	NoChoice,
	KeepGit,
	KeepOriginal,
};
