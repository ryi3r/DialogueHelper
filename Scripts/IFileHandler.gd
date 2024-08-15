extends Node

func save_file(_path: String) -> Thread:
	Handle.gc_window = Handle.gc_scene.instantiate()
	var _thr := Thread.new()
	_thr.start(func() -> void:
		if FileAccess.file_exists("user://enable_git.bool"):
			Handle.saving_window.label.call_deferred("set_text", "Fetching the git repo...")
			var _old_data: Dictionary = load_file_data(_path, false)[0]
			var _r := Handle.git.pull()
			Handle.call_deferred("handle_git_output", _r)
			if !_r.success:
				return
			var _new_data: Dictionary = load_file_data(_path, false)[0]
			Handle.saving_window.label.call_deferred("set_text", "Checking up differences between commits...")
			var _conflicts := []
			for _entry_name: String in _new_data.keys():
				if Handle.strings.has(_entry_name) && _old_data.has(_entry_name):
					var _i := 0
					var _old_entry: Array = _old_data[_entry_name]
					var _data_entry: Array = _new_data[_entry_name]
					for _entry: IStringContainer in _new_data[_entry_name]:
						if _entry.content != _data_entry[_i].content:
							if _old_entry[_i].content != _data_entry[_i].content:
								var _conflict := IGitConflict.new()
								_conflict.current_string = _data_entry[_i].content
								_conflict.git_string = _old_entry[_i].content
								_conflicts.append(_conflict)
						elif _old_entry[_i].content != _data_entry[_i].content:
							# The old entry was different, but the new entry
							# contains the most recent string.
							var _conflict := IGitConflict.new()
							_conflict.current_string = _data_entry[_i].content
							_conflict.git_string = _entry.content
							_conflicts.append(_conflict)
						_i += 1
			if !_conflicts.is_empty():
				Handle.gc_window.conflicts = _conflicts
				call_deferred("add_child", Handle.gc_window)
				await Handle.gc_window.tree_exited
		Handle.saving_window.label.call_deferred("set_text", "Saving...")
		var _data := []
		var _size := 0
		for _e: Array in Handle.strings.values():
			_size += _e.size()
		var _v := 0
		var _fe := IFormatEntry.new()
		_fe.kind = 9
		_fe.data["Style"] = Handle.style
		_data.append(_fe)
		Handle.saving_window.progress_bar.call_deferred("set_max", _size * 2)
		var _eq_strings_map := {}
		for _i in range(Handle.string_sstr_arr.size()):
			_fe = IFormatEntry.new()
			_fe.disable_uri = ["Data"]
			_fe.kind = 2
			_fe.data["Data"] = ",".join(PackedStringArray(Handle.string_sstr_arr[_i]))
			_data.append(str(_fe))
			Handle.saving_window.progress_bar.call_deferred("set_value", _v)
			_v += 1
		for key: String in Handle.strings.keys():
			_fe = IFormatEntry.new()
			_fe.kind = 0
			_fe.data["ID"] = key
			_data.append(str(_fe))
			for _entry: IStringContainer in Handle.strings[key] as Array:
				_data.append(str(_entry))
				Handle.saving_window.progress_bar.call_deferred("set_value", _v)
				_v += 1
		_fe = IFormatEntry.new()
		_fe.kind = 8
		_data.append(str(_fe))
		(Handle.saving_window.label as Label).call_deferred("set_text", "Saving the file...")
		var _f := FileAccess.open(_path, FileAccess.WRITE)
		_f.store_string("\n".join(PackedStringArray(_data)))
		_f.flush()
		_f.close()
		Handle.is_modified = false
		if FileAccess.file_exists("user://enable_git.bool"):
			(Handle.saving_window.label as Label).call_deferred("set_text", "Commiting on the git repo...")
			var _r := Handle.git.commit("[DH] Update Strings")
			Handle.call_deferred("handle_git_output", _r)
			if !_r.success:
				return
		Handle.saving_window.call_deferred("queue_free")
	)
	return _thr

func load_file(_path: String) -> Thread:
	Handle.main_node.clear_data()
	var _cthr := Thread.new()
	_cthr.start(func() -> void:
		if FileAccess.file_exists("user://enable_git.bool"):
			Handle.loading_window.label.call_deferred("set_text", "Fetching the git repo...")
			var _r := Handle.git.pull()
			Handle.call_deferred("handle_git_output", _r)
			if !_r.success:
				return
		Handle.loading_window.label.call_deferred("set_text", "Loading...")
		Handle.main_node.dialogue_selector.call_deferred("clear")
		Handle.main_node.string_selector.call_deferred("clear")
		Handle.main_node.similar_entries.call_deferred("clear")
		
		Handle.entry_names.clear()
		for _i in range(Handle.layer_strings.size()):
			Handle.layer_strings[_i] = ""
		for _i in range(Handle.layer_colors.size()):
			Handle.layer_colors[_i] = Color.WHITE
		Handle.strings.clear()
		var _output := load_file_data(_path, true, true)
		Handle.strings = _output[0]
		Handle.string_table = _output[1]
		Handle.string_ids = _output[2]
		Handle.entry_names = _output[3]
		Handle.last_string_id = _output[4]
		Handle.loading_window.label.call_deferred("set_text", "Caching similar entries... [This may take a long time.]")

		var _v := 0
		for _entry: int in Handle.string_table.keys():
			var _strg: IStringContainer = Handle.string_table[_entry].data
			var _eqstr: Array[int] = []
			for _key: int in Handle.string_ids.keys():
				if Handle.string_ids[_key] == _strg.original_content:
					_eqstr.append(_key)
			if _eqstr.size() > 1: # This ignores if there's a key and it is the same entry
				var _index := Handle.string_sstr_arr.size()
				Handle.string_sstr[_strg.original_content] = _index
				Handle.string_sstr_arr.append(_eqstr)
				_strg.equal_strings = _eqstr
				_strg.equal_strings_index = _index
			_v += 1
			Handle.loading_window.progress_bar.call_deferred("set_value", _v)
		Handle.loading_window.call_deferred("queue_free")
	)
	return _cthr

## [Dictionary (Strings), Dictionary (StrTable), Dictionary (StrIDs), Array (EntryNames), Int (LastStringID)]
# TODO: Don't use an Array, use a custom class for this.
func load_file_data(_path: String, _apply_settings := true, _is_thread := false) -> Array:
	var _strings := {}
	var _string_table := {}
	var _string_ids := {}
	var _entry_names := []
	var _last_string_id := 0
	
	var _data := FileAccess.get_file_as_string(_path)
	var _arr := FileFormat.parse_file(_data)
	if _is_thread:
		Handle.loading_window.progress_bar.call_deferred("set_max", _arr.size())
	var _totl := 0
	var _current_entry := ""
	var _entries := []
	var _is_entry := false
	var _new_max := _arr.size()
	var _eqs_map := {}
	for _line: IFormatEntry in _arr:
		if (_line.kind == 0 || _line.kind == 8) && _is_entry:
			_strings[_current_entry] = _entries
			_entries = []
		match _line.kind:
			8: # File End
				pass
			9: # Settings
				if _apply_settings:
					if _is_thread:
						Handle.call_deferred("load_style", _line.data["Style"])
					else:
						Handle.load_style(_line.data["Style"])
			0: # New Entry
				_current_entry = _line.data["ID"]
				_entry_names.append(_current_entry)
				if _is_thread:
					Handle.main_node.dialogue_selector.call_deferred("add_item", _current_entry)
				_is_entry = true
			1: # Add String to the current Entry
				var _cont := IStringContainer.new(_line)
				if _is_thread && _cont.equal_strings == null:
					_new_max += 1
					Handle.loading_window.progress_bar.call_deferred("set_max", _new_max)
				_line.data["ID"] = _current_entry
				_string_table[_cont.id] = IStringTable.new(_current_entry, _cont.content, _entries.size(), _cont)
				_string_ids[_cont.id] = _cont.content
				if _cont.id > _last_string_id:
					_last_string_id = _cont.id + 1
				_entries.append(_cont)
		if _is_thread:
			_totl += 1
			Handle.loading_window.progress_bar.call_deferred("set_value", _totl)
	return [_strings, _string_table, _string_ids, _entry_names, _last_string_id]
