extends Node2D;

var layers = 5;
var layer_data = ["", "", "", "", ""];
var layer_color = [0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff];
var current_font = "fnt_main";
var font_scale = 2;

var search_kind = 0;

var author = "DefaultUsername";

var show_info_templ = "
Current String:

Original String:

Last Edit Details: EDIT_DETAILS
";

var show_info_last_edit_templ = "
>   Author: AUTHOR_NAME
>   Timestamp: TIMESTAMP
";

@onready var similar_entries = $SimilarEntries/ItemList;
@onready var box = $Box;
@onready var dialogue_edit = $DialogueEdit;
@onready var current_layer_node = $DisplaySettings/CurrentLayer/Num;
@onready var current_box_node = $DisplaySettings/CurrentBox/Num;
@onready var current_font_node = $DisplaySettings/CurrentFont/Num;
@onready var current_color_node = $DisplaySettings/CurrentColor/Picker;
@onready var current_scale_node = $DisplaySettings/CurrentScale/Num;

@onready var current_box_label = $DisplaySettings/CurrentBox/Info;
@onready var current_font_label = $DisplaySettings/CurrentFont/Info;
@onready var dialogue_selector = $EntryList;
@onready var string_selector = $StringSelector/ItemList;

@onready var replace_similar = $ReplaceSimilar;
@onready var enable_portrait = $EnablePortrait;

var dataure = load("res://Fonts/fnt_main.png");
var glyphs = {};
var ascender = 0;

var current_layer = 0;
var current_font_id = 0;
var current_box = 0;
var current_scale = 1.0;

var loading_scene = preload("res://Subwindows/Progress bars/Loading.tscn");
var loading_window = null;
var saving_scene = preload("res://Subwindows/Progress bars/Saving.tscn");
var saving_window = null;
var fd_scene = preload("res://Subwindows/File action/LoadJSON.tscn");
var fd_window = null;
var show_info_scene = preload("res://Subwindows/ShowInfo.tscn");
var show_info_window = null;
var author_scene = preload("res://Subwindows/AuthorInfo.tscn");
var author_window = null;
var settings_scene = preload("res://Subwindows/Settings.tscn");
var settings_window = null;
var fds_scene = preload("res://Subwindows/File action/SaveJSON.tscn");
var fds_window = null;
var goto_scene = preload("res://Subwindows/GoTo.tscn");
var goto_window = null;
var search_scene = preload("res://Subwindows/Search.tscn");
var search_window = null;

@onready var file_popup: PopupMenu = $Panel/MenuBar/Container/File.get_popup();
@onready var about_popup: PopupMenu = $Panel/MenuBar/Container/About.get_popup();

var data = {};
var data_table = {};
var string_ids = {};
var data_size = 0;
var entry_names = [];
var last_thread = null;

var force_update_box = 0;
var force_update_font = 0;

var git = Git.new();

class StringContainer:
	var id = -1;
	var content = "";
	var original_content = "";
	var last_edited = LastEdited.new();
	var equal_strings = [];
	var layer_strings = null;
	var layer_colors = null;
	var box_style = 0;
	var font_style = 0;
	var font_scale = 1.0;
	var enable_portrait = false;

	func to_json() -> Dictionary:
		return {
			"ID": id,
			"Content": content,
			"OriginalContent": original_content,
			"LastEdited": last_edited.to_json(),
			"EqualStrings": equal_strings,
			"LayerStrings": layer_strings,
			"LayerColors": layer_colors,
			"BoxStyle": box_style,
			"FontStyle": font_style,
			"FontScale": font_scale,
			"EnablePortrait": enable_portrait,
		};

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
			if "BoxStyle" in json:
				box_style = json["BoxStyle"];
			if "FontStyle" in json:
				font_style = json["FontStyle"];
			if "FontScale" in json:
				font_scale = json["FontScale"];
			if "EnablePortrait" in json:
				enable_portrait = json["EnablePortrait"];

	func _to_string():
		return JSON.stringify(to_json(), "", false);

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

	func to_json() -> Dictionary:
		return {
			"Timestamp": timestamp,
			"Author": author,
		};

	func _init(json = null, _timestamp = null, _author = null):
		if json is Dictionary:
			if "Timestamp" in json:
				timestamp = json["Timestamp"];
			if "Author" in json:
				author = json["Author"];
		elif _timestamp is int && _author is String:
			timestamp = _timestamp;
			author = _author;

	func _to_string():
		return JSON.stringify(to_json(), "", false);

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
		return _run_command(["/c", "git -C \"{0}\" checkout \"{1}\" & git -C \"{0}\" pull -f".format([
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

func _ready():
	file_popup.id_pressed.connect(file_menu_selected);
	about_popup.id_pressed.connect(about_menu_selected);
	
	if FileAccess.file_exists("user://username.txt"):
		author = FileAccess.get_file_as_string("user://username.txt");
	if FileAccess.file_exists("user://enable_git.bool"):
		var branch = "";
		if FileAccess.file_exists("user://git_branch.txt"):
			branch = FileAccess.get_file_as_string("user://git_branch.txt");
		git.url = FileAccess.get_file_as_string("user://git_url.txt");
		git.branch = branch;
	
	current_layer_node.max_value = layers;
	current_font_node.max_value = 8;
	current_box_node.max_value = 29;
	
	load_font();

func _process(_delta):
	enable_portrait.disabled = !box.supports_portrait;
	if current_layer_node.value - 1 != current_layer:
		current_layer = current_layer_node.value - 1;
		current_color_node.color = Color.hex(layer_color[current_layer]);
		dialogue_edit.text = layer_data[current_layer];
	var item;
	if current_font_node.value - 1 != current_font_id || force_update_font > 0:
		update_font(int(current_font_node.value - 1));
		if dialogue_selector.get_selected_items().size() > 0:
			item = dialogue_selector.get_item_text(dialogue_selector.get_selected_items()[0]);
			if data.has(item):
				data[item][string_selector.get_selected_items()[0]].font_style = current_font_id;
		if force_update_font > 0:
			force_update_font -= 1;
	if current_box_node.value - 1 != current_box || force_update_box > 0:
		update_box(int(current_box_node.value - 1));
		if dialogue_selector.get_selected_items().size() > 0:
			item = dialogue_selector.get_item_text(dialogue_selector.get_selected_items()[0]);
			if data.has(item):
				data[item][string_selector.get_selected_items()[0]].box_style = current_box;
		if force_update_box > 0:
			force_update_box -= 1;
	if current_scale_node.value != current_scale:
		current_scale = current_scale_node.value;
		if dialogue_selector.get_selected_items().size() > 0:
			item = dialogue_selector.get_item_text(dialogue_selector.get_selected_items()[0]);
			if data.has(item):
				data[item][string_selector.get_selected_items()[0]].font_scale = current_scale_node.value;
	layer_data[current_layer] = dialogue_edit.text;
	layer_color[current_layer] = current_color_node.color.to_rgba32();
	if dialogue_selector.get_selected_items().size() > 0:
		item = dialogue_selector.get_item_text(dialogue_selector.get_selected_items()[0]);
		if data.has(item):
			var s = string_selector.get_selected_items();
			var i = s[0] if s.size() > 0 else 0;
			if data[item].size() > i:
				data[item][i].content = layer_data[current_layer];
	queue_redraw();
	if last_thread is Thread:
		if !last_thread.is_alive():
			last_thread.wait_to_finish();
			last_thread = null;

func update_box(i: int):
	match i:
		0: # OW
			current_box_label.text = "OW";
		1: # Battle
			current_box_label.text = "Battle";
		25: # Shop Speech
			current_box_label.text = "Shop Speech";
		26: # Shop Select
			current_box_label.text = "Shop Select";
		27: # Shop Description
			current_box_label.text = "Shop Description";
		28: # Shop Talk
			current_box_label.text = "Shop Talk";
		_: # Battle Speech
			current_box_label.text = "Battle Speech #" + str(i - 1);
	current_box = i;
	box.box_num = i;

func update_font(i: int):
	match i:
		0: # Speech
			current_font_label.text = "Speech";
			current_font = "fnt_main";
			font_scale = 2;
		1: # Computer Screen
			current_font_label.text = "Computer Screen";
			current_font = "fnt_chem_computer_screen";
			font_scale = 1;
		2: # Menu
			current_font_label.text = "Menu";
			current_font = "fnt_mainb";
			font_scale = 1;
		3: # Battle Speech
			current_font_label.text = "Battle Speech";
			current_font = "fnt_dotumche";
			font_scale = 1;
		4: # Sans
			current_font_label.text = "Sans";
			current_font = "fnt_sans";
			font_scale = 1;
		5: # Stats
			current_font_label.text = "Stats";
			current_font = "fnt_stats";
			font_scale = 1;
		6: # Mars Needs Cunnilingus
			current_font_label.text = "Mars Needs Cunnilingus";
			current_font = "fnt_mars_needs_cunnilingus";
			font_scale = 1;
		7: # Battle the 2nd
			current_font_label.text = "Battle the 2nd";
			current_font = "fnt_battle";
			font_scale = 2;
	current_font_id = i;
	load_font();

func _draw():
	for i in range(layers):
		var t = layer_data[i];
		var pos = Vector2(box.position.x + (box.portrait_offset.x if box.supports_portrait && enable_portrait.button_pressed else box.dialogue_offset.x), box.position.y + (box.portrait_offset.y if box.supports_portrait && enable_portrait.button_pressed else box.dialogue_offset.y));
		var add = Vector2();
		var color = Color.hex(int(layer_color[i]));
		for chr in str(t):
			var curc = str(chr).unicode_at(0);
			if curc == "#".unicode_at(0):
				add.x = 0;
				add.y += (glyphs[str("A".unicode_at(0))]["source"].size.y * font_scale) + (ascender - 4);
				continue;
			if glyphs.has(str(curc)):
				var glyph = glyphs[str(curc)];
				draw_texture_rect_region(dataure, Rect2(pos.x + add.x + (glyph["offset"] * (font_scale * current_scale)), pos.y + add.y, (glyph["source"].size.x * (font_scale * current_scale)), (glyph["source"].size.y * (font_scale * current_scale))), glyph["source"], color);
				add.x += glyph["shift"] * (font_scale * current_scale);

func load_font():
	dataure = load("res://Fonts/{0}.png".format([current_font]));
	var s = FileAccess.get_file_as_string("res://Fonts/glyphs_{0}.txt".format([current_font])).split("\n");
	glyphs.clear();
	var i = 0;
	for e in s:
		if !e.is_empty():
			e = e.split(";");
			if i == 0:
				ascender = int(e[1]);
			else:
				glyphs[str(e[0])] = {
					"char": int(e[0]),
					"source": Rect2i(int(e[1]), int(e[2]), int(e[3]), int(e[4])),
					"shift": int(e[5]),
					"offset": int(e[6]),
				};
			i += 1;

func handle_git_output(r: GitResponse):
	if !r.success:
		var w = preload("res://Subwindows/GitError.tscn").instantiate();
		add_child(w);
		w.get_node("TextEdit").text = "".join(PackedStringArray(r.output));

func load_stuff(path):
	if FileAccess.file_exists("user://enable_git.bool"):
		(loading_window.label as Label).call_deferred_thread_group("set_text", "Fetching the git repo...");
		var r = git.pull();
		call_deferred_thread_group("handle_git_output", r);
		if !r.success:
			return;
	(loading_window.label as Label).call_deferred_thread_group("set_text", "Loading...");
	dialogue_selector.call_deferred_thread_group("clear");
	string_selector.call_deferred_thread_group("clear");
	similar_entries.call_deferred_thread_group("clear");
	entry_names.clear();
	layer_data = ["", "", "", "", ""];
	layer_color = [0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff];
	data.clear();
	var v = [0];
	var datat = JSON.parse_string(FileAccess.get_file_as_string(path));
	if datat.has("Objects") && datat.has("Rooms") && datat.has("Scripts"): # Old Format -> New Format
		print("Loading from Old Format, converting...");
		data_size = datat["Objects"].size() + datat["Rooms"].size() + datat["Scripts"].size();
		loading_window.progress_bar.call_deferred_thread_group("set_max", (datat["Objects"].size() + datat["Rooms"].size() + datat["Scripts"].size()) * 2);
		var id = 0;
		for en in ["Objects", "Rooms", "Scripts"]:
			for entry in datat[en]:
				var strg = [];
				for stri in entry["Strings"]:
					entry_names.append(entry["Name"]);
					data_table[int(id)] = StringTable.new(entry["Name"], strg, strg.size());
					var container = StringContainer.new();
					container.id = id;
					container.content = stri;
					container.original_content = stri;
					container.last_edited = LastEdited.new(null, -1, "");
					container.equal_strings = null;
					container.update();
					string_ids[int(id)] = stri;
					strg.append(container);
					v[0] = id;
					id += 1;
					loading_window.progress_bar.call_deferred_thread_group("set_value", id);
				dialogue_selector.call_deferred_thread_group("add_item", entry["Name"]);
				data[entry["Name"]] = strg;
	else: # New format, no need for conversion.
		print("Loading from New format.");
		data = Dictionary(datat);
		var extr = 0;
		var size = 0;
		entry_names.append_array(data.keys());
		for e in data.values():
			for sube in e:
				size += 1;
				if sube["EqualStrings"] == null:
					extr += 1;
		data_size = size;
		loading_window.progress_bar.call_deferred_thread_group("set_max", size + extr);
		var totl = 0;
		for entry_name in data.keys():
			dialogue_selector.call_deferred_thread_group("add_item", entry_name);
			var entry = data[entry_name];
			var i = 0;
			while i < entry.size():
				data_table[int(entry[i]["ID"])] = StringTable.new(entry_name, entry[i]["Content"], i);
				string_ids[int(entry[i]["ID"])] = entry[i]["Content"];
				var json = Dictionary(entry[i]).duplicate(true);
				entry[i] = StringContainer.new(json);
				v[0] = totl;
				totl += 1;
				loading_window.progress_bar.call_deferred_thread_group("set_value", totl);
				i += 1;
	
	var m = Mutex.new();
	var n = Mutex.new();

	var threads = [];
	var running_threads = [];
	var strgs_equal = {};
	var limit = max(OS.get_processor_count() - 4, 1);
	for i in range(limit):
		threads.append([]);
		running_threads.append(Thread.new());
	var current_thread = 0;
	for entry in data.keys():
		threads[current_thread % limit].append(entry);
		current_thread += 1;

	current_thread = 0;
	for thr in running_threads:
		thr.start(func():
			var ei = 0;
			var arr = threads[current_thread];
			while ei < arr.size():
				ei += 1;
				for strg in data[arr[ei - 1]]:
					if strg.equal_strings == null:
						strg.equal_strings = [];
						n.lock();
						var has = strgs_equal.has(strg.content);
						n.unlock();
						if has:
							strg.equal_strings = strgs_equal[strg.content].duplicate();
						else:
							for key in string_ids.keys():
								if string_ids[key] == strg.content:
									strg.equal_strings.append(int(key));
							n.lock();
							strgs_equal[strg.content] = strg.equal_strings.duplicate();
							n.unlock();
				m.lock();
				v[0] += 1;
				m.unlock();
		);
		current_thread += 1;
	
	while true:
		m.lock();
		loading_window.progress_bar.call_deferred_thread_group("set_value", v[0]);
		m.unlock();
		var r = 0;
		for thr in running_threads:
			if thr == null:
				r += 1;
				continue;
			if !thr.is_alive() && thr.is_started():
				thr.wait_to_finish();
				running_threads[r] = null;
			r += 1;
		var isalv = true;
		for thr in running_threads:
			if thr == null:
				continue;
			if thr.is_alive():
				isalv = false;
				break;
		if isalv:
			call_deferred_thread_group("remove_child", loading_window);
			return;
		OS.delay_msec(1000 / max(Engine.max_fps, 1)); # Update each "frame"

func _on_item_list_item_selected(index):
	var item = dialogue_selector.get_item_text(index);
	change_to(item);
	if data.has(item):
		var it = data[item];
		for stri in it:
			string_selector.add_item(stri.content);
		string_selector.select(string_selector.get_item_at_position(Vector2(0, 0)));

func _on_item_list_item_selected_str(index):
	var item = dialogue_selector.get_item_text(dialogue_selector.get_selected_items()[0]);
	if data.has(item):
		current_layer = 0;
		similar_entries.clear();
		var stri = data[item][index];
		layer_data = stri.layer_strings;
		layer_color = stri.layer_colors;
		var t = create_tween();
		t.tween_interval(1.0 / 60.0);
		t.tween_callback(func():
			current_font_node.set_value(stri.font_style + 1);
			current_box_node.set_value(stri.box_style + 1);
			current_scale_node.set_value(stri.font_scale);
		);
		t.play();
		current_color_node.color = Color.hex(layer_color[current_layer]);
		dialogue_edit.text = layer_data[current_layer];
		similar_entries.clear();
		for sstri in stri.equal_strings:
			if string_ids[int(sstri)] != item:
				var r = data_table[int(sstri)];
				similar_entries.add_item(r.name + ":" + str(r.index + 1));

func change_to(item, index: int = 0):
	if data.has(item):
		current_layer = 0;
		similar_entries.clear();
		string_selector.clear();
		var it = data[item];
		if it.size() > index:
			var stri = it[index];
			layer_data = stri.layer_strings;
			layer_color = stri.layer_colors;
			var t = create_tween();
			t.tween_interval(1.0 / 60.0);
			t.tween_callback(func():
				current_font_node.set_value(stri.font_style + 1);
				current_box_node.set_value(stri.box_style + 1);
				current_scale_node.set_value(stri.font_scale);
			);
			t.play();
			current_color_node.color = Color.hex(layer_color[current_layer]);
			dialogue_edit.text = layer_data[current_layer];
			for sstri in stri.equal_strings:
				if string_ids[int(sstri)] != item:
					var r = data_table[int(sstri)];
					similar_entries.add_item(r.name + ":" + str(r.index + 1));

func _on_item_list_item_selected_similar(index):
	var item = similar_entries.get_item_text(index).split(":");
	similar_entries.deselect_all();
	similar_entries.get_v_scroll_bar().value = 0;
	if data.has(item[0]):
		change_to(item[0], int(item[1]) - 1);
		string_selector.clear();
		for stri in data[item[0]]:
			string_selector.add_item(stri.layer_strings[0]);
		string_selector.select(int(item[1]) - 1);
		string_selector.ensure_current_is_visible();
		var i = 0;
		while i < dialogue_selector.get_item_count():
			if dialogue_selector.get_item_text(i) == item[0]:
				dialogue_selector.select(i);
				dialogue_selector.ensure_current_is_visible();
				break;
			i += 1;

func _on_dialogue_edit_text_changed():
	var sel = dialogue_selector.get_selected_items();
	var sel1 = string_selector.get_selected_items();
	if sel.size() != 0 && sel1.size() != 0 && current_layer == 0:
		var c = dialogue_selector.get_item_text(sel[0]);
		var e = [];
		if replace_similar.button_pressed:
			e = data[c][sel1[0]].equal_strings.duplicate();
		e.insert(0, data[c][sel1[0]].id);
		var entry_table = {};
		var i = 0;
		for g in data[c]:
			entry_table[int(g.id)] = i;
			i += 1;
		for f in e: # Update all strings first (before making the entry table), then change the visualization.
			f = data_table[int(f)] as StringTable;
			var entr: StringContainer = data[f.name][f.index];
			entr.last_edited.author = author;
			entr.last_edited.timestamp = Time.get_unix_time_from_system();
			entr.content = dialogue_edit.text;
			entr.layer_strings[0] = entr.content;
			string_ids[int(entr.id)] = entr.content; # We need to update properly the String ID Dictionary
			if int(entr.id) in entry_table:
				if entry_table[int(entr.id)] == int(f.index):
					string_selector.set_item_text(f.index, dialogue_edit.text);

func save_stuff(path):
	var f;
	if FileAccess.file_exists("user://enable_git.bool"):
		(saving_window.label as Label).call_deferred_thread_group("set_text", "Fetching the git repo...");
		var old_json = FileAccess.get_file_as_string(path);
		var r = git.pull();
		call_deferred_thread_group("handle_git_output", r);
		if !r.success:
			return;
		var new_json = FileAccess.get_file_as_string(path);
		if old_json != new_json: # A commit was made
			new_json = JSON.parse_string(new_json) as Dictionary;
			old_json = JSON.parse_string(old_json) as Dictionary;
			(saving_window.label as Label).call_deferred_thread_group("set_text", "Checking up differences between commits...");
			for entry_name in new_json.keys():
				if old_json.has(entry_name) && data.has(entry_name):
					var i = 0;
					var oentry = old_json[entry_name];
					var dentry = data[entry_name];
					for entry in new_json[entry_name]:
						if entry["Content"] != oentry[i]["Content"]:
							if entry["Content"] != (dentry[i] as StringContainer).content:
								if oentry[i]["Content"] == (dentry[i] as StringContainer).content:
									# The entry has been changed between the commits,
									# And it hasn't been modified by the author.
									# We can update the string safely.
									dentry[i] = StringContainer.new(entry);
								else:
									# The entry has been changed between the commits,
									# And it has been modified by the author.
									# The last edit matters more, so we'll be leaving
									# Our own string.
									pass;
							elif oentry[i]["Content"] == (dentry[i] as StringContainer).content:
								# The entry has not been changed.
								# We do nothing.
								pass;
							else:
								# The old entry was different.
								# We do nothing, since the most recent
								# version has our string.
								pass;
						i += 1;
	(saving_window.label as Label).call_deferred_thread_group("set_text", "Saving...");
	var json = {};
	var size = 0;
	for e in data.values():
		size += e.size();
	var v = 0;
	saving_window.progress_bar.call_deferred_thread_group("set_max", size);
	for key in data.keys():
		var entries = data[key] as Array;
		var arr = [];
		for entry in entries:
			arr.append((entry as StringContainer).to_json());
			saving_window.progress_bar.call_deferred_thread_group("set_value", v);
			v += 1;
		json[key] = arr;
	(saving_window.label as Label).call_deferred_thread_group("set_text", "Saving the file...");
	f = FileAccess.open(path, FileAccess.WRITE);
	f.store_string(JSON.stringify(json, "\t", false));
	f.flush();
	f.close();
	if FileAccess.file_exists("user://enable_git.bool"):
		(saving_window.label as Label).call_deferred_thread_group("set_text", "Commiting on the git repo...");
		var r = git.commit("Update Strings (Using DH)");
		call_deferred_thread_group("handle_git_output", r);
		if !r.success:
			return;
	call_deferred_thread_group("remove_child", saving_window);

func file_menu_selected(id):
	match id:
		0: # Open JSON
			var lambda = func(path):
				var t = create_tween();
				t.tween_interval(1.0 / 60.0);
				t.tween_callback(func():
					if fd_window != null:
						fd_window.free();
						fd_window = null;
					loading_window = loading_scene.instantiate();
					add_child(loading_window);
					var tre = Thread.new();
					tre.start(load_stuff.bind(path));
					last_thread = tre;
				);
				t.play();
			if FileAccess.file_exists("user://enable_git.bool"):
				lambda.call("user://repo/strings.json");
			else:
				fd_window = fd_scene.instantiate();
				add_child(fd_window);
				fd_window.file_selected.connect(lambda);
				fd_window.close_requested.connect(func():
					fd_window.queue_free();
				);
				fd_window.show();
		1: # Save JSON
			var lambda = func(path):
				var t = create_tween();
				t.tween_interval(1.0 / 60.0);
				t.tween_callback(func():
					if fds_window != null:
						fds_window.free();
						fds_window = null;
					saving_window = saving_scene.instantiate();
					add_child(saving_window);
					var tre = Thread.new();
					tre.start(save_stuff.bind(path));
					last_thread = tre;
				);
				t.play();
			if FileAccess.file_exists("user://enable_git.bool"):
				lambda.call("user://repo/strings.json");
			else:
				fds_window = fds_scene.instantiate();
				add_child(fds_window);
				fds_window.file_selected.connect(lambda);
				fds_window.close_requested.connect(func():
					fds_window.queue_free();
				);
				fds_window.show();
		3: # Settings
			settings_window = settings_scene.instantiate();
			add_child(settings_window);
		5: # Quit
			get_tree().quit();

func about_menu_selected(id):
	match id:
		0: # Show String Info
			show_info_window = show_info_scene.instantiate() as Window;
			add_child(show_info_window);
			show_info_window.close_requested.connect(func():
				show_info_window.queue_free();
			);
			var n: Label = show_info_window.get_node("Label");
			var l: LineEdit = show_info_window.get_node("LineEdit");
			var l2: LineEdit = show_info_window.get_node("LineEdit2");
			if dialogue_selector.get_selected_items().size() > 0 && string_selector.get_selected_items().size() > 0:
				var ename = dialogue_selector.get_item_text(dialogue_selector.get_selected_items()[0]);
				var eindex = string_selector.get_selected_items()[0];
				var laste = (data[ename][eindex] as StringContainer).last_edited;
				var td = Time.get_datetime_dict_from_unix_time(laste.timestamp + (Time.get_time_zone_from_system().bias * 60.0)); # Local timezone?
				n.text = show_info_templ.substr(1, show_info_templ.length() - 2) \
					.replace("EDIT_DETAILS", "\n>   No Last Edit was made." if laste.author == "" || laste.timestamp == -1 else (
						show_info_last_edit_templ.substr(0, show_info_last_edit_templ.length() - 1) \
							.replace("AUTHOR_NAME", laste.author) \
							.replace("TIMESTAMP", "{DAY}/{MONTH}/{YEAR} {HOUR}:{MINUTE}:{SECOND} {HFORMAT}".format({
									"DAY": str(td["day"]).pad_zeros(2),
									"MONTH": str(td["month"]).pad_zeros(2),
									"YEAR": str(td["year"]).pad_zeros(4),
									"HOUR": str(12 if td["hour"] == 0 else td["hour"] if td["hour"] < 13 else td["hour"] - 12).pad_zeros(2),
									"MINUTE": str(td["minute"]).pad_zeros(2),
									"SECOND": str(td["second"]).pad_zeros(2),
									"HFORMAT": "a.m." if td["hour"] < 12 else "p.m",
								}))
					));
				l.text = ename + ":" + str(eindex + 1);
				l2.text = (data[ename][eindex] as StringContainer).original_content;
			else:
				n.text = "But Nobody Came." if randi() % 50 == 0 else "But there was nothing to see.";
				l.hide();
				l2.hide();
		1: # Set Author Details
			author_window = author_scene.instantiate();
			add_child(author_window);
			author_window.get_node("Label/LineEdit").text = author;

func open_search_menu():
	search_window = search_scene.instantiate();
	add_child(search_window);

func open_go_to_menu():
	goto_window = goto_scene.instantiate();
	add_child(goto_window);
	goto_window.get_node("GoTo/GoButton").pressed.connect(func():
		var t = goto_window.get_node("GoTo/Str").text;
		if t.length() > 0:
			var item = (t + ":1").split(":");
			if data.has(item[0]):
				change_to(item[0], int(item[1]) - 1);
				string_selector.clear();
				for stri in data[item[0]]:
					string_selector.add_item(stri.layer_strings[0]);
				string_selector.select(int(item[1]) - 1);
				string_selector.ensure_current_is_visible();
				var i = 0;
				while i < dialogue_selector.get_item_count():
					if dialogue_selector.get_item_text(i) == item[0]:
						dialogue_selector.select(i);
						dialogue_selector.ensure_current_is_visible();
						break;
					i += 1;
			t = create_tween();
			t.tween_interval(1.0 / 60.0);
			t.tween_callback(func():
				remove_child(goto_window);
			);
			t.play();
	);

func entry_search_text_changed(t: String):
	dialogue_selector.clear();
	if t.length() == 0:
		for e in entry_names:
			dialogue_selector.add_item(e);
	else:
		for e in entry_names:
			if str(e).to_lower().contains(t.to_lower()):
				dialogue_selector.add_item(e);
