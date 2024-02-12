extends Control;

var font = GMFont.GFont.new();

var search_kind = 0;

var author = "DefaultUsername";

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

var current_layer = 0;
var current_font_id = 0;
var current_box = 0;

var file_handler = preload("res://Resources/FileHandler.tscn").instantiate();

@onready var file_popup: PopupMenu = $Panel/MenuBar/Container/File.get_popup();
@onready var about_popup: PopupMenu = $Panel/MenuBar/Container/About.get_popup();

var last_thread = null;
var last_sthread = null;

func _ready():
	add_child(file_handler);
	file_popup.id_pressed.connect(file_menu_selected);
	about_popup.id_pressed.connect(about_menu_selected);
	
	if FileAccess.file_exists("user://username.txt"):
		author = FileAccess.get_file_as_string("user://username.txt");
	if FileAccess.file_exists("user://enable_git.bool"):
		var branch = "";
		if FileAccess.file_exists("user://git_branch.txt"):
			branch = FileAccess.get_file_as_string("user://git_branch.txt");
		Handle.git.url = FileAccess.get_file_as_string("user://git_url.txt");
		Handle.git.branch = branch;
	
	current_layer_node.max_value = Handle.layers;
	current_font_node.max_value = 8;
	current_box_node.max_value = 29;
	
	if FileAccess.file_exists("user://last_style.txt"):
		Handle.load_style(FileAccess.get_file_as_string("user://last_style.txt"));
	else:
		Handle.load_style();
	font = GMFont.get_font(Handle.current_font);
	update_box(current_box);
	update_font(current_font_id);

func _process(_delta):
	current_font_node.max_value = Handle.font_data.size();
	current_box_node.max_value = Handle.box_data.size();
	enable_portrait.disabled = !box.supports_portrait;
	if current_layer_node.value - 1 != current_layer:
		current_layer = current_layer_node.value - 1;
		current_color_node.color = Handle.layer_colors[current_layer];
		dialogue_edit.text = Handle.layer_strings[current_layer];
	var item;
	if current_font_node.value - 1 != current_font_id:
		update_font(int(current_font_node.value - 1));
		if dialogue_selector.get_selected_items().size() > 0:
			item = dialogue_selector.get_item_text(dialogue_selector.get_selected_items()[0]);
			if Handle.strings.has(item):
				Handle.strings[item][string_selector.get_selected_items()[0]].font_style = current_font_id;
	if current_box_node.value - 1 != current_box:
		update_box(int(current_box_node.value - 1));
		if dialogue_selector.get_selected_items().size() > 0:
			item = dialogue_selector.get_item_text(dialogue_selector.get_selected_items()[0]);
			if Handle.strings.has(item):
				Handle.strings[item][string_selector.get_selected_items()[0]].box_style = current_box;
	if current_scale_node.value != Handle.visual_scale:
		Handle.is_modified = true;
		Handle.visual_scale = current_scale_node.value;
		if dialogue_selector.get_selected_items().size() > 0:
			item = dialogue_selector.get_item_text(dialogue_selector.get_selected_items()[0]);
			if Handle.strings.has(item):
				Handle.strings[item][string_selector.get_selected_items()[0]].font_scale = current_scale_node.value;
	while Handle.layer_strings.size() < Handle.layers:
		Handle.layer_strings.append("");
	while Handle.layer_colors.size() < Handle.layers:
		Handle.layer_colors.append(Color.WHITE);
	Handle.layer_strings[current_layer] = dialogue_edit.text;
	if Handle.layer_colors[current_layer] != current_color_node.color:
		Handle.is_modified = true;
		Handle.layer_colors[current_layer] = current_color_node.color;
	if dialogue_selector.get_selected_items().size() > 0:
		item = dialogue_selector.get_item_text(dialogue_selector.get_selected_items()[0]);
		if Handle.strings.has(item):
			var s = string_selector.get_selected_items();
			var i = s[0] if s.size() > 0 else 0;
			if Handle.strings[item].size() > i:
				var citem = Handle.strings[item][i];
				citem.content = Handle.layer_strings[0];
				Handle.string_ids[int(citem.id)] = Handle.layer_strings[0]; # We need to update properly the String ID Dictionary
	if last_thread is Thread:
		if !last_thread.is_alive():
			last_thread.wait_to_finish();
			last_thread = null;

func update_box(i: int):
	if i >= Handle.box_data.size():
		return;
	current_box = i;
	current_box_label.text = Handle.box_data[i].name;
	box.current_box = i;
	Handle.is_modified = true;

func update_font(i: int):
	if i >= Handle.font_data.size():
		return;
	current_font_id = i;
	Handle.current_font = i;
	current_font_label.text = Handle.font_data[i].name;
	font = GMFont.get_font(Handle.current_font);
	Handle.is_modified = true;

func _on_item_list_item_selected(index):
	var item = dialogue_selector.get_item_text(index);
	change_to(item);
	if Handle.strings.has(item):
		var it = Handle.strings[item];
		for stri in it:
			string_selector.add_item(stri.content);
		string_selector.select(string_selector.get_item_at_position(Vector2(0, 0)));

func _on_item_list_item_selected_str(index):
	var item = dialogue_selector.get_item_text(dialogue_selector.get_selected_items()[0]);
	if Handle.strings.has(item):
		current_layer = 0;
		current_layer_node.value = 1;
		similar_entries.clear();
		var stri = Handle.strings[item][index];
		Handle.layer_strings = stri.layer_strings;
		Handle.layer_colors = stri.layer_colors;
		var t = create_tween();
		t.tween_interval(1.0 / 60.0);
		t.tween_callback(func():
			current_font_node.set_value(stri.font_style + 1);
			current_box_node.set_value(stri.box_style + 1);
			current_scale_node.set_value(stri.font_scale);
		);
		t.play();
		current_color_node.color = Handle.layer_colors[current_layer];
		dialogue_edit.text = Handle.layer_strings[current_layer];
		if last_sthread is Thread:
			last_sthread.wait_to_finish();
		last_sthread = Thread.new();
		last_sthread.start(func():
			similar_entries.call_deferred_thread_group("clear");
			for sstri in stri.equal_strings:
				if Handle.string_ids[int(sstri)] != item:
					var r = Handle.string_table[int(sstri)];
					similar_entries.call_deferred_thread_group("add_item", r.name + ":" + str(r.index + 1));
		);

func change_to(item, index: int = 0):
	if Handle.strings.has(item):
		current_layer = 0;
		current_layer_node.value = 1;
		string_selector.clear();
		var it = Handle.strings[item];
		if it.size() > index:
			var stri = it[index];
			Handle.layer_strings = stri.layer_strings;
			Handle.layer_colors = stri.layer_colors;
			var t = create_tween();
			t.tween_interval(1.0 / 60.0);
			t.tween_callback(func():
				current_font_node.set_value(stri.font_style + 1);
				current_box_node.set_value(stri.box_style + 1);
				current_scale_node.set_value(stri.font_scale);
			);
			t.play();
			current_color_node.color = Handle.layer_colors[current_layer];
			dialogue_edit.text = Handle.layer_strings[current_layer];
			if last_sthread is Thread:
				last_sthread.wait_to_finish();
			last_sthread = Thread.new();
			last_sthread.start(func():
				similar_entries.call_deferred_thread_group("clear");
				for sstri in stri.equal_strings: # String ID
					if int(sstri) != int(stri.id):
						var r = Handle.string_table[int(sstri)];
						similar_entries.call_deferred_thread_group("add_item", r.name + ":" + str(r.index + 1));
			);

func _on_item_list_item_selected_similar(index):
	var item = similar_entries.get_item_text(index).split(":");
	if Handle.strings.has(item[0]):
		change_to(item[0], int(item[1]) - 1);
		similar_entries.deselect_all();
		similar_entries.get_v_scroll_bar().value = 0;
		if last_sthread is Thread:
			last_sthread.wait_to_finish();
		last_sthread = Thread.new();
		last_sthread.start(func():
			similar_entries.call_deferred_thread_group("clear");
			for sstri in Handle.strings[item[0]][int(item[1]) - 1].equal_strings:
				if int(sstri) != int(Handle.strings[item[0]][int(item[1]) - 1].id):
					var r = Handle.string_table[int(sstri)];
					similar_entries.call_deferred_thread_group("add_item", r.name + ":" + str(r.index + 1));
		);
		string_selector.clear();
		for stri in Handle.strings[item[0]]:
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
	if sel.size() != 0 && sel1.size() != 0:
		var c = dialogue_selector.get_item_text(sel[0]);
		if current_layer == 0:
			var e = Handle.strings[c][sel1[0]].equal_strings.duplicate() if replace_similar.button_pressed else [];
			e.append(Handle.strings[c][sel1[0]].id);
			var entry_table = {};
			var i = 0;
			for g in Handle.strings[c]:
				entry_table[int(g.id)] = i;
				i += 1;
			Handle.is_modified = true;
			for f in e: # Update all strings first (before making the entry table), then change the visualization.
				f = Handle.string_table[int(f)] as Type.StringTable;
				var entr: Type.StringContainer = Handle.strings[f.name][f.index];
				entr.last_edited.author = author;
				entr.last_edited.timestamp = Time.get_unix_time_from_system();
				entr.content = dialogue_edit.text;
				entr.layer_strings[0] = entr.content;
				Handle.string_ids[int(entr.id)] = entr.content; # We need to update properly the String ID Dictionary
				if int(entr.id) in entry_table:
					if entry_table[int(entr.id)] == int(f.index):
						string_selector.set_item_text(f.index, dialogue_edit.text);
		else:
			Handle.is_modified = true;
			var f = Handle.string_table[int(Handle.strings[c][sel1[0]].id)] as Type.StringTable;
			var entr: Type.StringContainer = Handle.strings[f.name][f.index];
			entr.last_edited.author = author;
			entr.last_edited.timestamp = Time.get_unix_time_from_system();
			entr.layer_strings[current_layer] = dialogue_edit.text;

func file_menu_selected(id):
	match id:
		0: # Open JSON
			var lambda = func(path):
				var t = create_tween();
				t.tween_interval(1.0 / 60.0);
				t.tween_callback(func():
					if Handle.fd_window != null:
						Handle.fd_window.free();
						Handle.fd_window = null;
					Handle.loading_window = Handle.loading_scene.instantiate();
					add_child(Handle.loading_window);
					last_thread = file_handler.load_file(path);
				);
				t.play();
			if FileAccess.file_exists("user://enable_git.bool"):
				lambda.call("user://repo/Strings.txt");
			else:
				Handle.fd_window = Handle.fd_scene.instantiate();
				add_child(Handle.fd_window);
				Handle.fd_window.file_selected.connect(lambda);
				Handle.fd_window.close_requested.connect(func():
					Handle.fd_window.queue_free();
				);
				Handle.fd_window.show();
		1: # Save JSON
			var lambda = func(path):
				var t = create_tween();
				t.tween_interval(1.0 / 60.0);
				t.tween_callback(func():
					if Handle.fds_window != null:
						Handle.fds_window.free();
						Handle.fds_window = null;
					Handle.saving_window = Handle.saving_scene.instantiate();
					add_child(Handle.saving_window);
					last_thread = file_handler.save_file(path);
				);
				t.play();
			if FileAccess.file_exists("user://enable_git.bool"):
				lambda.call("user://repo/Strings.txt");
			else:
				Handle.fds_window = Handle.fds_scene.instantiate();
				add_child(Handle.fds_window);
				Handle.fds_window.file_selected.connect(lambda);
				Handle.fds_window.close_requested.connect(func():
					Handle.fds_window.queue_free();
				);
				Handle.fds_window.show();
		3: # Settings
			Handle.settings_window = Handle.settings_scene.instantiate();
			add_child(Handle.settings_window);
		5: # Quit
			if Handle.is_modified:
				Handle.uc_window = Handle.uc_scene.instantiate();
				Handle.uc_window.is_quitting = true;
				add_child(Handle.uc_window);
			else:
				get_tree().quit();
		6, 7: # New File, Close File
			# Same functionality same code
			if Handle.is_modified:
				Handle.uc_window = Handle.uc_scene.instantiate();
				add_child(Handle.uc_window);
			else:
				clear_data();

func clear_data():
	dialogue_selector.clear();
	string_selector.clear();
	similar_entries.clear();
	Handle.entry_names.clear();
	Handle.strings.clear();
	Handle.string_ids.clear();
	Handle.string_table.clear();

func about_menu_selected(id):
	match id:
		0: # Show String Info
			Handle.show_info_window = Handle.show_info_scene.instantiate() as Window;
			add_child(Handle.show_info_window);
			Handle.show_info_window.close_requested.connect(func():
				Handle.show_info_window.queue_free();
			);
			var n: Label = Handle.show_info_window.get_node("Label");
			var n2: Label = Handle.show_info_window.get_node("Label2");
			var l: LineEdit = Handle.show_info_window.get_node("LineEdit");
			var l2: LineEdit = Handle.show_info_window.get_node("LineEdit2");
			if dialogue_selector.get_selected_items().size() > 0 && string_selector.get_selected_items().size() > 0:
				var ename = dialogue_selector.get_item_text(dialogue_selector.get_selected_items()[0]);
				var eindex = string_selector.get_selected_items()[0];
				var laste = (Handle.strings[ename][eindex] as Type.StringContainer).last_edited;
				var td = Time.get_datetime_dict_from_unix_time(laste.timestamp + (Time.get_time_zone_from_system().bias * 60.0)); # Local timezone?
				if laste.author == "" || laste.timestamp == -1:
					n2.text = "No last edit was made.";
				else:
					n2.text = n2.text \
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
				l.text = ename + ":" + str(eindex + 1);
				l2.text = (Handle.strings[ename][eindex] as Type.StringContainer).original_content;
			else:
				n.text = "But Nobody Came." if randi() % 50 == 0 else "But there was nothing to see.";
				n2.hide();
				l.hide();
				l2.hide();
		1: # Set Author Details
			Handle.author_window = Handle.author_scene.instantiate();
			add_child(Handle.author_window);
			Handle.author_window.get_node("Label/LineEdit").text = author;
		3: # About DH...
			Handle.adh_window = Handle.adh_scene.instantiate();
			add_child(Handle.adh_window);

func open_search_menu():
	Handle.search_window = Handle.search_scene.instantiate();
	add_child(Handle.search_window);

func open_go_to_menu():
	Handle.goto_window = Handle.goto_scene.instantiate();
	add_child(Handle.goto_window);
	Handle.goto_window.get_node("GoTo/GoButton").pressed.connect(func():
		var t = Handle.goto_window.get_node("GoTo/Str").text;
		if t.length() > 0:
			var item = (t + ":1").split(":");
			if Handle.strings.has(item[0]):
				change_to(item[0], int(item[1]) - 1);
				string_selector.clear();
				for stri in Handle.strings[item[0]]:
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
				remove_child(Handle.goto_window);
			);
			t.play();
	);

func entry_search_text_changed(t: String):
	dialogue_selector.clear();
	if t.length() == 0:
		for e in Handle.entry_names:
			dialogue_selector.add_item(e);
	else:
		for e in Handle.entry_names:
			if str(e).to_lower().contains(t.to_lower()):
				dialogue_selector.add_item(e);

func _on_reload_style_pressed():
	Handle.load_style();
