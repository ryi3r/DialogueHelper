extends Window
class_name WAddString

var entry := ""

func _on_close_requested() -> void:
	queue_free()

func _on_cancel_button_pressed() -> void:
	queue_free()

func _on_ok_button_pressed() -> void:
	var _sc := IStringContainer.new()
	_sc.id = Handle.last_string_id
	Handle.last_string_id += 1
	_sc.original_content = ($Label/TextEdit as TextEdit).text
	_sc.content = _sc.original_content
	_sc.layer_strings = [_sc.content]
	_sc.layer_colors = [Color.WHITE]
	_sc.equal_strings = []
	Handle.string_table[_sc.id] = IStringTable.new(entry, _sc.content, (Handle.strings[entry] as Array).size(), _sc)
	Handle.string_ids[_sc.id] = _sc.content
	(Handle.strings[entry] as Array).append(_sc)
	if Handle.string_sstr.has(_sc.original_content):
		_sc.equal_strings_index = Handle.string_sstr[_sc.original_content]
		_sc.equal_strings = Handle.string_sstr_arr[_sc.equal_strings_index]
		_sc.equal_strings.append(_sc.id)
	else:
		var _eqstr: Array[int] = []
		for _key: int in Handle.string_ids.keys():
			if Handle.string_ids[_key] == _sc.original_content:
				_eqstr.append(_key)
		if _eqstr.size() > 1: # This ignores if there's a key and it is the same entry
			var _index := Handle.string_sstr_arr.size()
			Handle.string_sstr[_sc.original_content] = _index
			Handle.string_sstr_arr.append(_eqstr)
			_sc.equal_strings = _eqstr
			_sc.equal_strings_index = _index
			for _key in _eqstr:
				var _strg: IStringContainer = Handle.string_table[_key].data
				_strg.equal_strings = _eqstr
				_strg.equal_strings_index = _index
	var _parent: WDialogueHelper = get_parent()
	var _index_strs := _parent.string_selector.add_item(_sc.content)
	_parent.string_selector.select(_index_strs)
	_parent._on_item_list_item_selected_str(_index_strs)
	queue_free()
