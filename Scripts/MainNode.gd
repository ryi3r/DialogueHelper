extends Control
class_name WDialogueHelper

var font := IFont.new()
var search_kind := 1
var author := "DefaultUsername"

@onready var similar_entries: ItemList = $SimilarEntries/ItemList
@onready var box: WBox = $Box
@onready var dialogue_edit: TextEdit = $DialogueEdit
@onready var original_dialogue: TextEdit = $DialogueEdit/OriginalDialogue
@onready var current_layer_node: SpinBox = $DisplaySettings/CurrentLayer/Num
@onready var current_box_node: SpinBox = $DisplaySettings/CurrentBox/Num
@onready var current_font_node: SpinBox = $DisplaySettings/CurrentFont/Num
@onready var current_color_node: ColorPickerButton = $DisplaySettings/CurrentColor/Picker
@onready var current_scale_node: SpinBox = $DisplaySettings/CurrentScale/Num

@onready var current_box_label: Label = $DisplaySettings/CurrentBox/Info
@onready var current_font_label: Label = $DisplaySettings/CurrentFont/Info
@onready var dialogue_selector: ItemList = $EntryList
@onready var string_selector: ItemList = $StringSelector/ItemList

@onready var replace_similar: CheckBox = $DialogueEdit/ReplaceSimilar
@onready var enable_portrait: CheckBox = $DialogueEdit/EnablePortrait
@onready var add_entry: Button = $DialogueEdit/AddEntry
@onready var add_string: Button = $DialogueEdit/AddString

@onready var tree := get_tree()
@onready var panel: Control = $Panel
@onready var display_settings: Control = $DisplaySettings
@onready var string_selector_parent: Control = $StringSelector
@onready var similar_entries_parent: Control = $SimilarEntries

var current_layer := 0
var current_font_id := 0
var current_box := 0

@onready var file_popup: PopupMenu = ($Panel/MenuBar/Container/File as MenuButton).get_popup()
@onready var about_popup: PopupMenu = ($Panel/MenuBar/Container/About as MenuButton).get_popup()

var last_thread: Thread = null
var last_sthread: Thread = null
var last_size := Vector2i.ZERO

func _ready() -> void:
	tree.root.min_size = Vector2(1100, 700)
	if OS.has_environment("USERNAME"):
		author = OS.get_environment("USERNAME")
	elif OS.has_environment("USER"):
		author = OS.get_environment("USER")
	
	file_popup.id_pressed.connect(file_menu_selected)
	about_popup.id_pressed.connect(about_menu_selected)
	Handle.main_node = self
	
	if FileAccess.file_exists("user://username.txt"):
		author = FileAccess.get_file_as_string("user://username.txt")
	if FileAccess.file_exists("user://enable_git.bool"):
		var _branch := ""
		if FileAccess.file_exists("user://git_branch.txt"):
			_branch = FileAccess.get_file_as_string("user://git_branch.txt")
		Handle.git.url = FileAccess.get_file_as_string("user://git_url.txt")
		Handle.git.branch = _branch
	if FileAccess.file_exists("user://scale.txt"):
		current_scale_node.value = int(FileAccess.get_file_as_string("user://scale.txt").strip_escapes())
	
	current_layer_node.max_value = Handle.layers
	current_font_node.max_value = 8
	current_box_node.max_value = 29
	
	if FileAccess.file_exists("user://last_style.txt"):
		Handle.load_style(FileAccess.get_file_as_string("user://last_style.txt"))
	else:
		Handle.load_style()
	font = IFont.get_font(Handle.current_font)
	update_box(current_box)
	update_font(current_font_id)
	Handle.is_modified = false

func _process(_delta: float) -> void:
	if last_size != tree.root.size:
		last_size = tree.root.size
		panel.size.x = tree.root.size.x
		display_settings.position.x = tree.root.size.x - (1100 - 876)
		
		string_selector_parent.position.x = tree.root.size.x - (1100 - 881)
		similar_entries_parent.position.x = tree.root.size.x - (1100 - 879)
		box.size.x = tree.root.size.x - (1100 - 580)
		box.size.y = tree.root.size.y - (700 - 400)
		dialogue_edit.position.y = tree.root.size.y - (700 - 530)
		dialogue_edit.size.x = tree.root.size.x - (1100 - 587)
		original_dialogue.size.x = tree.root.size.x - (1100 - 587)
		add_string.position.x = (dialogue_edit.size.x - 587) + 449
		replace_similar.position.x = (dialogue_edit.size.x - 587) + 353
		dialogue_selector.size.y = tree.root.size.y - (700 - 602)
		string_selector.size.y = tree.root.size.y - (700 - 157)
	
	current_font_node.max_value = Handle.font_data.size()
	current_box_node.max_value = Handle.box_data.size()
	add_string.disabled = dialogue_selector.get_selected_items().is_empty()
	enable_portrait.disabled = !box.supports_portrait
	box.portrait_enabled = enable_portrait.button_pressed
	if int(current_layer_node.value) - 1 != current_layer:
		current_layer = int(current_layer_node.value) - 1
		current_color_node.color = Handle.layer_colors[current_layer]
		dialogue_edit.text = Handle.layer_strings[current_layer]
		original_dialogue.text = Handle.layer_strings[current_layer]
	if current_font_node.value - 1 != current_font_id:
		update_font(int(current_font_node.value - 1))
		if dialogue_selector.get_selected_items().size() > 0:
			var _item := dialogue_selector.get_item_text(dialogue_selector.get_selected_items()[0])
			if Handle.strings.has(_item):
				Handle.strings[_item][string_selector.get_selected_items()[0]].font_style = current_font_id
	if current_box_node.value - 1 != current_box:
		update_box(int(current_box_node.value - 1))
		if dialogue_selector.get_selected_items().size() > 0:
			var _item := dialogue_selector.get_item_text(dialogue_selector.get_selected_items()[0])
			if Handle.strings.has(_item):
				Handle.strings[_item][string_selector.get_selected_items()[0]].box_style = current_box
	if current_scale_node.value != Handle.visual_scale:
		Handle.is_modified = true
		Handle.visual_scale = current_scale_node.value
		box.handle.queue_redraw()
		box.spr.scale = Vector2(Handle.visual_scale, Handle.visual_scale)
		var _f := FileAccess.open("user://scale.txt", FileAccess.WRITE)
		_f.store_string(str(Handle.visual_scale))
		_f.flush()
		_f.close()
	while Handle.layer_strings.size() < Handle.layers:
		Handle.layer_strings.append("")
	while Handle.layer_colors.size() < Handle.layers:
		Handle.layer_colors.append(Color.WHITE)
	Handle.layer_strings[current_layer] = dialogue_edit.text
	if Handle.layer_colors[current_layer] != current_color_node.color:
		Handle.is_modified = true
		Handle.layer_colors[current_layer] = current_color_node.color
	original_dialogue.text = Handle.og_str
	if dialogue_selector.get_selected_items().size() > 0:
		var _item := dialogue_selector.get_item_text(dialogue_selector.get_selected_items()[0])
		if Handle.strings.has(_item):
			var _s := string_selector.get_selected_items()
			var _i := _s[0] if _s.size() > 0 else 0
			if (Handle.strings[_item] as Array).size() > _i:
				var _citem: IStringContainer = Handle.strings[_item][_i]
				_citem.content = Handle.layer_strings[0]
				Handle.string_ids[_citem.id] = Handle.layer_strings[0] # We need to update properly the String ID Dictionary
	if last_thread is Thread:
		if !last_thread.is_alive():
			last_thread.wait_to_finish()
			last_thread = null

func update_box(_i: int) -> void:
	if _i >= Handle.box_data.size():
		return
	current_box = _i
	current_box_label.text = Handle.box_data[_i].name
	current_box_node.value = _i + 1
	box.current_box = _i
	Handle.is_modified = true

func update_font(_i: int) -> void:
	if _i >= Handle.font_data.size():
		return
	current_font_id = _i
	Handle.current_font = _i
	current_font_label.text = Handle.font_data[_i].name
	current_font_node.value = _i + 1
	font = IFont.get_font(Handle.current_font)
	Handle.is_modified = true

func _on_item_list_item_selected(_index: int) -> void:
	var _item := dialogue_selector.get_item_text(_index)
	change_to(_item)
	if Handle.strings.has(_item):
		var _it: Array = Handle.strings[_item]
		for _stri: IStringContainer in _it:
			string_selector.add_item(_stri.content)
		if !_it.is_empty():
			string_selector.select(string_selector.get_item_at_position(Vector2(0, 0)))

func _on_item_list_item_selected_str(_index: int) -> void:
	var _item := dialogue_selector.get_item_text(dialogue_selector.get_selected_items()[0])
	if Handle.strings.has(_item):
		current_layer = 0
		current_layer_node.value = 1
		similar_entries.clear()
		var _stri: IStringContainer = Handle.strings[_item][_index]
		Handle.layer_strings = _stri.layer_strings
		Handle.layer_colors = _stri.layer_colors
		Handle.og_str = _stri.original_content
		var _t := create_tween()
		_t.tween_callback(func() -> void:
			current_font_node.set_value(_stri.font_style + 1)
			current_box_node.set_value(_stri.box_style + 1)
		).set_delay(1.0 / 60.0)
		_t.play()
		current_color_node.color = Handle.layer_colors[current_layer]
		dialogue_edit.text = Handle.layer_strings[current_layer]
		if last_sthread is Thread:
			last_sthread.wait_to_finish()
		last_sthread = Thread.new()
		last_sthread.start(func() -> void:
			similar_entries.call_deferred("clear")
			for sstri in _stri.equal_strings:
				if sstri != _stri.id:
					var _r: IStringTable = Handle.string_table[sstri]
					similar_entries.call_deferred("add_item", "%s:%s" % [_r.name, _r.index + 1])
		)

func change_to(item: String, index: int = 0) -> void:
	if Handle.strings.has(item):
		current_layer = 0
		current_layer_node.value = 1
		string_selector.clear()
		var _it: Array = Handle.strings[item]
		if _it.size() > index:
			var _stri: IStringContainer = _it[index]
			Handle.layer_strings = _stri.layer_strings
			Handle.layer_colors = _stri.layer_colors
			Handle.og_str = _stri.original_content
			var _t := create_tween()
			_t.tween_callback(func() -> void:
				current_font_node.set_value(_stri.font_style + 1)
				current_box_node.set_value(_stri.box_style + 1)
			).set_delay(1.0 / 60.0)
			_t.play()
			current_color_node.color = Handle.layer_colors[current_layer]
			dialogue_edit.text = Handle.layer_strings[current_layer]
			if last_sthread is Thread:
				last_sthread.wait_to_finish()
			last_sthread = Thread.new()
			last_sthread.start(func() -> void:
				similar_entries.call_deferred("clear")
				for sstri in _stri.equal_strings: # String ID
					if sstri != _stri.id:
						var r: IStringTable = Handle.string_table[sstri]
						similar_entries.call_deferred("add_item", "%s:%s" % [r.name, r.index + 1])
			)
		else:
			if last_sthread is Thread:
				last_sthread.wait_to_finish()
			last_sthread = Thread.new()
			last_sthread.start(func() -> void:
				similar_entries.call_deferred("clear")
			)

func _on_item_list_item_selected_similar(_index: int) -> void:
	var _item := similar_entries.get_item_text(_index).split(":")
	if Handle.strings.has(_item[0]):
		change_to(_item[0], int(_item[1]) - 1)
		similar_entries.deselect_all()
		similar_entries.get_v_scroll_bar().value = 0
		if last_sthread is Thread:
			last_sthread.wait_to_finish()
		last_sthread = Thread.new()
		last_sthread.start(func() -> void:
			similar_entries.call_deferred("clear")
			for sstri: int in Handle.strings[_item[0]][_item[1] as int - 1].equal_strings:
				if sstri != int((Handle.strings[_item[0]][_item[1] as int - 1] as IStringContainer).id):
					var _r: IStringTable = Handle.string_table[sstri]
					similar_entries.call_deferred("add_item", "%s:%s" % [_r.name, _r.index + 1])
		)
		string_selector.clear()
		for _stri: IStringContainer in Handle.strings[_item[0]]:
			string_selector.add_item(_stri.layer_strings[0])
		string_selector.select(int(_item[1]) - 1)
		string_selector.ensure_current_is_visible()
		for _i in range(dialogue_selector.get_item_count()):
			if dialogue_selector.get_item_text(_i) == _item[0]:
				dialogue_selector.select(_i)
				dialogue_selector.ensure_current_is_visible()
				break

func _on_dialogue_edit_text_changed() -> void:
	var _sel := dialogue_selector.get_selected_items()
	var _sel1 := string_selector.get_selected_items()
	if _sel.size() != 0 && _sel1.size() != 0:
		var _c := dialogue_selector.get_item_text(_sel[0])
		if current_layer == 0:
			var _e: Array = (Handle.strings[_c][_sel1[0]].equal_strings as Array).duplicate() if replace_similar.button_pressed else []
			_e.append(Handle.strings[_c][_sel1[0]].id)
			var _entry_table := {}
			var _i := 0
			for _g: IStringContainer in Handle.strings[_c]:
				_entry_table[_g.id] = _i
				_i += 1
			Handle.is_modified = true
			for _f: int in _e: # Update all strings first (before making the entry table), then change the visualization.
				var _t: IStringTable = Handle.string_table[_f]
				var _entr: IStringContainer = Handle.strings[_t.name][_t.index]
				_entr.last_edited.author = author
				_entr.last_edited.timestamp = int(Time.get_unix_time_from_system())
				_entr.content = dialogue_edit.text
				_entr.layer_strings[0] = _entr.content
				Handle.string_ids[_entr.id] = _entr.content # We need to update properly the String ID Dictionary
				if _entry_table.has(_entr.id):
					if _entry_table[_entr.id] == _t.index:
						string_selector.set_item_text(_t.index, dialogue_edit.text)
		else:
			Handle.is_modified = true
			var _f: IStringTable = Handle.string_table[(Handle.strings[_c][_sel1[0]] as IStringContainer).id]
			var _entr: IStringContainer = Handle.strings[_f.name][_f.index]
			_entr.last_edited.author = author
			_entr.last_edited.timestamp = int(Time.get_unix_time_from_system())
			_entr.layer_strings[current_layer] = dialogue_edit.text

func file_menu_selected(_id: int) -> void:
	match _id:
		1: # Open File
			var _lambda := func(_path: String) -> void:
				var _t := create_tween()
				_t.tween_callback(func() -> void:
					if Handle.fd_window != null:
						Handle.fd_window.free()
						Handle.fd_window = null
					Handle.loading_window = Handle.loading_scene.instantiate()
					add_child(Handle.loading_window)
					last_thread = IFileHandler.load_file(_path)
				).set_delay(1.0 / 60.0)
				_t.play()
			if Handle.is_modified:
				Handle.uc_window = Handle.uc_scene.instantiate()
				Handle.uc_window.callback.connect(func() -> void:
					if FileAccess.file_exists("user://enable_git.bool"):
						_lambda.call("user://repo/Strings.txt")
					else:
						Handle.fd_window = Handle.fd_scene.instantiate()
						Handle.fd_window.file_selected.connect(_lambda)
						Handle.fd_window.close_requested.connect(Handle.fd_window.queue_free)
						Handle.fd_window.canceled.connect(Handle.fd_window.queue_free)
						add_child(Handle.fd_window)
						Handle.fd_window.show()
				)
				add_child(Handle.uc_window)
			elif FileAccess.file_exists("user://enable_git.bool"):
				_lambda.call("user://repo/Strings.txt")
			else:
				Handle.fd_window = Handle.fd_scene.instantiate()
				Handle.fd_window.file_selected.connect(_lambda)
				Handle.fd_window.close_requested.connect(Handle.fd_window.queue_free)
				Handle.fd_window.canceled.connect(Handle.fd_window.queue_free)
				add_child(Handle.fd_window)
				Handle.fd_window.show()
		2: # Save File
			var _lambda := func(_path: String) -> void:
				var _t := create_tween()
				_t.tween_callback(func() -> void:
					if Handle.fds_window != null:
						Handle.fds_window.free()
						Handle.fds_window = null
					Handle.saving_window = Handle.saving_scene.instantiate()
					add_child(Handle.saving_window)
					last_thread = IFileHandler.save_file(_path)
				).set_delay(1.0 / 60.0)
				_t.play()
			if FileAccess.file_exists("user://enable_git.bool"):
				_lambda.call("user://repo/Strings.txt")
			else:
				Handle.fds_window = Handle.fds_scene.instantiate()
				Handle.fds_window.file_selected.connect(_lambda)
				Handle.fds_window.close_requested.connect(Handle.fds_window.queue_free)
				Handle.fds_window.canceled.connect(Handle.fds_window.queue_free)
				add_child(Handle.fds_window)
				Handle.fds_window.show()
		3: # Settings
			Handle.settings_window = Handle.settings_scene.instantiate()
			add_child(Handle.settings_window)
		5: # Quit
			if Handle.is_modified:
				Handle.uc_window = Handle.uc_scene.instantiate()
				Handle.uc_window.callback.connect(tree.quit)
				add_child(Handle.uc_window)
			else:
				get_tree().quit()
		6, 7: # New File, Close File
			# Same functionality same code
			if Handle.is_modified:
				Handle.uc_window = Handle.uc_scene.instantiate()
				Handle.uc_window.callback.connect(clear_data)
				add_child(Handle.uc_window)
			else:
				clear_data()

func clear_data() -> void:
	dialogue_selector.clear()
	string_selector.clear()
	similar_entries.clear()
	Handle.entry_names.clear()
	Handle.strings.clear()
	Handle.string_ids.clear()
	Handle.string_table.clear()
	Handle.string_sstr.clear()
	Handle.string_sstr_arr.clear()
	
	for _i in range(Handle.layer_colors.size()):
		Handle.layer_colors[_i] = Color.WHITE
	for _i in range(Handle.layer_strings.size()):
		Handle.layer_strings[_i] = ""
	Handle.og_str = ""
	original_dialogue.text = ""
	dialogue_edit.text = ""
	current_color_node.color = Color.WHITE
	current_font_node.value = current_font_node.min_value
	current_box_node.value = current_box_node.min_value
	current_color_node.color = Color.WHITE
	Handle.last_string_id = 0
	Handle.is_modified = false

func about_menu_selected(_id: int) -> void:
	match _id:
		0: # Show String Info
			Handle.show_info_window = Handle.show_info_scene.instantiate()
			add_child(Handle.show_info_window)
			Handle.show_info_window.close_requested.connect(func() -> void:
				Handle.show_info_window.queue_free()
			)
			var _n: Label = Handle.show_info_window.get_node("Label")
			var _n2: Label = Handle.show_info_window.get_node("Label2")
			var _l: LineEdit = Handle.show_info_window.get_node("LineEdit")
			var _l2: TextEdit = Handle.show_info_window.get_node("LineEdit2")
			if dialogue_selector.get_selected_items().size() > 0 && string_selector.get_selected_items().size() > 0:
				var _ename := dialogue_selector.get_item_text(dialogue_selector.get_selected_items()[0])
				var _eindex := string_selector.get_selected_items()[0]
				var _laste := (Handle.strings[_ename][_eindex] as IStringContainer).last_edited
				var _td := Time.get_datetime_dict_from_unix_time(_laste.timestamp + int((Time.get_time_zone_from_system().bias as int) * 60)) # Local timezone?
				if _laste.author == "" || _laste.timestamp == -1:
					_n2.text = "\n\n\nNo last edit was made."
				else:
					_n2.text = _n2.text \
						.replace("AUTHOR_NAME", _laste.author) \
						.replace("TIMESTAMP", "{DAY}/{MONTH}/{YEAR} {HOUR}:{MINUTE}:{SECOND} {HFORMAT}".format({
								"DAY": str(_td["day"]).pad_zeros(2),
								"MONTH": str(_td["month"]).pad_zeros(2),
								"YEAR": str(_td["year"]).pad_zeros(4),
								"HOUR": str(12 if _td["hour"] == 0 else _td["hour"] if _td["hour"] < 13 else _td["hour"] - 12).pad_zeros(2),
								"MINUTE": str(_td["minute"]).pad_zeros(2),
								"SECOND": str(_td["second"]).pad_zeros(2),
								"HFORMAT": "a.m." if _td["hour"] < 12 else "p.m",
							}))
				_l.text = "%s:%s" % [_ename, _eindex + 1]
				_l2.text = (Handle.strings[_ename][_eindex] as IStringContainer).original_content
			else:
				_n.text = "But Nobody Came." if randi() % 50 == 0 else "But there was nothing to see."
				_n2.hide()
				_l.hide()
				_l2.hide()
		1: # Set Author Details
			Handle.author_window = Handle.author_scene.instantiate()
			add_child(Handle.author_window)
			(Handle.author_window.get_node("Label/LineEdit") as LineEdit).text = author
		3: # About DH...
			Handle.adh_window = Handle.adh_scene.instantiate()
			add_child(Handle.adh_window)

func open_search_menu() -> void:
	Handle.search_window = Handle.search_scene.instantiate()
	add_child(Handle.search_window)

func open_go_to_menu() -> void:
	Handle.goto_window = Handle.goto_scene.instantiate()
	add_child(Handle.goto_window)
	(Handle.goto_window.get_node("GoTo/GoButton") as Button).pressed.connect(func() -> void:
		var _te := (Handle.goto_window.get_node("GoTo/Str") as LineEdit).text
		if _te.length() > 0:
			var _item := (_te + ":1").split(":")
			if Handle.strings.has(_item[0]):
				change_to(_item[0], int(_item[1]) - 1)
				string_selector.clear()
				for _stri: IStringContainer in Handle.strings[_item[0]]:
					string_selector.add_item(str(_stri.layer_strings[0]))
				string_selector.select(int(_item[1]) - 1)
				string_selector.ensure_current_is_visible()
				for _i in range(dialogue_selector.get_item_count()):
					if dialogue_selector.get_item_text(_i) == _item[0]:
						dialogue_selector.select(_i)
						dialogue_selector.ensure_current_is_visible()
						break
		Handle.goto_window.queue_free()
	)

func entry_search_text_changed(_t: String) -> void:
	dialogue_selector.clear()
	if _t.length() == 0:
		for _e: String in Handle.entry_names:
			dialogue_selector.add_item(_e)
	else:
		for _e: String in Handle.entry_names:
			if _e.to_lower().contains(_t.to_lower()):
				dialogue_selector.add_item(_e)

func _on_reload_style_pressed() -> void:
	Handle.load_style()

func _on_add_entry_pressed() -> void:
	Handle.ae_window = Handle.ae_scene.instantiate()
	add_child(Handle.ae_window)

func _on_add_string_pressed() -> void:
	if !dialogue_selector.get_selected_items().is_empty():
		Handle.as_window = Handle.as_scene.instantiate()
		Handle.as_window.entry = dialogue_selector.get_item_text(dialogue_selector.get_selected_items()[0])
		add_child(Handle.as_window)
