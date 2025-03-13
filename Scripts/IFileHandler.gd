extends Node

class ILoadFile extends RefCounted:
	var strings := {}
	var string_table := {}
	var string_ids := {}
	var entry_names := []
	var last_string_id := 0

func save_file(_path: String) -> Thread:
	Handle.gc_window = Handle.gc_scene.instantiate()
	var _thr := Thread.new()
	_thr.start(func() -> void:
		if FileAccess.file_exists("user://enable_git.bool"):
			Handle.saving_window.label.set_text.call_deferred("Fetching the git repo...")
			var _r := Handle.git.pull()
			Handle.handle_git_output.call_deferred(_r)
			if !_r.success:
				return
			var _old_data: Dictionary = load_file_data(_path, false).strings
			Handle.saving_window.label.set_text.call_deferred("Checking up differences between commits...")
			var _conflicts: Array[IGitConflict] = []
			for _entry_name: String in Handle.strings.keys():
				if Handle.strings.has(_entry_name) && _old_data.has(_entry_name):
					var _old_entry: Array = _old_data[_entry_name]
					var _new_entry: Array = Handle.strings[_entry_name]
					var _i := 0
					for _entry: IStringContainer in _new_entry:
						if _i < _old_entry.size():
							var _oentry: IStringContainer = _old_entry[_i]
							# Disabled until I figure out a better way to do this...
							#if _oentry.last_edited.timestamp > _entry.last_edited.timestamp:
							if _oentry.content != _entry.content:
								var _conflict := IGitConflict.new()
								_conflict.string_id = _entry.id
								_conflict.current_string = _entry.content
								_conflict.git_string = _oentry.content
								_conflicts.append(_conflict)
							_i += 1
						else:
							break
			if !_conflicts.is_empty():
				Handle.gc_window.conflicts = _conflicts
				add_child.call_deferred(Handle.gc_window)
				await Handle.gc_window.tree_exited
				for _conflict in _conflicts:
					var _strt: IStringTable = Handle.string_table[_conflict.string_id]
					match _conflict.keep:
						IGitConflict.IKeep.KeepGit:
							_strt.data.content = _conflict.git_string
						IGitConflict.IKeep.KeepOriginal:
							_strt.data.content = _conflict.current_string
		Handle.saving_window.label.set_text.call_deferred("Saving...")
		var _data := []
		var _size := 0 #Handle.string_sstr_arr.size()
		for _e: Array in Handle.strings.values():
			_size += _e.size()
		var _v := 0
		var _fe := IFormatEntry.new()
		_fe.kind = 9
		_fe.data.Style = Handle.style
		_data.append(_fe)
		for key: String in Handle.strings.keys():
			_fe = IFormatEntry.new()
			_fe.kind = 0
			_fe.data.ID = key
			_data.append(str(_fe))
			for _entry: IStringContainer in Handle.strings[key] as Array:
				_data.append(str(_entry))
				Handle.saving_window.progress_bar.set_value.call_deferred(_v)
				_v += 1
		_fe = IFormatEntry.new()
		_fe.kind = 8
		_data.append(str(_fe))
		Handle.saving_window.label.set_text.call_deferred("Saving the file...")
		var _f := FileAccess.open(_path, FileAccess.WRITE)
		_f.store_string("\n".join(PackedStringArray(_data)))
		_f.flush()
		_f.close()
		Handle.is_modified = false
		if FileAccess.file_exists("user://enable_git.bool"):
			Handle.saving_window.label.set_text.call_deferred("Commiting on the git repo...")
			var _r := Handle.git.commit("[DH] Update Strings")
			Handle.handle_git_output.call_deferred(_r)
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
			Handle.loading_window.label.set_text.call_deferred("Fetching the git repo...")
			var _r := Handle.git.pull()
			Handle.handle_git_output.call_deferred(_r)
			if !_r.success:
				return
		Handle.loading_window.label.set_text.call_deferred("Loading...")
		Handle.main_node.dialogue_selector.clear.call_deferred()
		Handle.main_node.string_selector.clear.call_deferred()
		Handle.main_node.similar_entries.clear.call_deferred()
		
		Handle.entry_names.clear()
		Handle.og_str = ""
		for _i in range(Handle.layer_strings.size()):
			Handle.layer_strings[_i] = ""
		for _i in range(Handle.layer_colors.size()):
			Handle.layer_colors[_i] = Color.WHITE
		Handle.strings.clear()
		var _output := load_file_data(_path, true, true)
		Handle.strings = _output.strings
		Handle.string_table = _output.string_table
		Handle.string_ids = _output.string_ids
		Handle.entry_names = _output.entry_names
		Handle.last_string_id = _output.last_string_id
		
		Handle.string_sstr.clear()
		Handle.string_sstr_arr.clear()
		var _eqstr := {}
		var _eqstrarr := []
		var _v := 0
		Handle.loading_window.label.set_text.call_deferred("Caching similar entries... [This may take a long time.]")
		Handle.loading_window.progress_bar.set_value.call_deferred(0)
		Handle.loading_window.progress_bar.set_max.call_deferred(Handle.string_table.size() * 2)
		for _entry: int in Handle.string_table.keys():
			var _strg: IStringContainer = Handle.string_table[_entry].data
			if !_eqstr.has(_strg.original_content):
				_eqstr[_strg.original_content] = _eqstrarr.size()
				var _arr: Array[int] = [_strg.id]
				_strg.equal_strings = _arr
				_eqstrarr.append(_arr)
			else:
				var _arr: Array[int] = _eqstrarr[_eqstr[_strg.original_content]]
				_arr.append(_strg.id)
				_strg.equal_strings = _arr
			_v += 1
			Handle.loading_window.progress_bar.set_value.call_deferred(_v)
		for _key: String in _eqstr.keys():
			var _entries: Array = _eqstrarr[_eqstr[_key]]
			if _entries.size() > 1:
				var _index := Handle.string_sstr_arr.size()
				Handle.string_sstr[_key] = _index
				Handle.string_sstr_arr.append(_entries)
				for _entry: int in _entries:
					(Handle.string_table[_entry] as IStringTable).data.equal_strings_index = _index
			_v += 1
			Handle.loading_window.progress_bar.set_value.call_deferred(_v)
		Handle.is_modified = false
		Handle.loading_window.queue_free.call_deferred()
	)
	return _cthr

func load_file_data(_path: String, _apply_settings := true, _is_thread := false) -> ILoadFile:
	var _strings := {}
	var _string_table := {}
	var _string_ids := {}
	var _entry_names := []
	var _last_string_id := 0
	
	var _data := FileAccess.get_file_as_string(_path)
	var _arr := FileFormat.parse_file(_data)
	
	if _is_thread:
		Handle.loading_window.progress_bar.set_max.call_deferred(_arr.size())
	
	var _totl := 0
	var _current_entry := ""
	var _entries := []
	var _is_entry := false
	
	for _line: IFormatEntry in _arr:
		# 8 = File End
		if (_line.kind == 0 || _line.kind == 8) && _is_entry:
			_strings[_current_entry] = _entries
			_entries = []
		match _line.kind:
			9: # Settings
				if _apply_settings:
					if _is_thread:
						Handle.load_style.call_deferred(_line.data["Style"])
					else:
						Handle.load_style(_line.data["Style"])
			0: # New Entry
				_current_entry = _line.data.ID
				_entry_names.append(_current_entry)
				_is_entry = true
			1: # Add String to the current Entry
				_line.data.ID = _last_string_id
				_last_string_id += 1
				var _cont := IStringContainer.new(_line)
				if _is_thread:
					_string_table[_cont.id] = IStringTable.new(_current_entry, _cont.content, _entries.size(), _cont)
				_string_ids[_cont.id] = _cont.content
				_entries.append(_cont)
		if _is_thread:
			_totl += 1
			Handle.loading_window.progress_bar.set_value.call_deferred(_totl)
	if _is_thread:
		(
			func() -> void:
				for _en: String in _entry_names:
					Handle.main_node.dialogue_selector.add_item(_en)
		).call_deferred()
	var ilf := ILoadFile.new()
	ilf.strings = _strings
	ilf.string_table = _string_table
	ilf.string_ids = _string_ids
	ilf.entry_names = _entry_names
	ilf.last_string_id = _last_string_id
	return ilf
