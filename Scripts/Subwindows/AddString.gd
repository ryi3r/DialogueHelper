extends Window;

var entry = null;

func _on_close_requested():
	var t = create_tween();
	t.tween_interval(1.0 / 60.0);
	t.tween_callback(func():
		get_parent().remove_child(self);
	);
	t.play();

func _on_cancel_button_pressed():
	_on_close_requested();

func _on_ok_button_pressed():
	if entry is String:
		var id = Handle.last_string_id;
		Handle.last_string_id += 1;
		var sc = Type.StringContainer.new();
		sc.id = id;
		sc.original_content = $Label/TextEdit.text;
		sc.content = sc.original_content;
		sc.layer_strings = [sc.content];
		sc.layer_colors = [Color.WHITE];
		sc.equal_strings = [];
		Handle.string_table[int(id)] = Type.StringTable.new(entry, sc.content, Handle.strings[entry].size());
		Handle.string_ids[int(id)] = sc.content;
		Handle.strings[entry].append(sc);
		var parent = get_parent();
		var index = parent.string_selector.add_item(sc.content);
		parent.string_selector.select(index);
		parent._on_item_list_item_selected_str(index);
	
	_on_close_requested();
