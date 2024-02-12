extends Node;

func save_file(path) -> Thread:
	Handle.gc_window = (Handle.gc_scene as PackedScene).instantiate();
	var thr = Thread.new();
	thr.start(func():
		var f;
		var parent = get_parent();
		if FileAccess.file_exists("user://enable_git.bool"):
			(Handle.saving_window.label as Label).call_deferred_thread_group("set_text", "Fetching the git repo...");
			var old_data: Dictionary = load_file_data(path, false)[0];
			var r = Handle.git.pull();
			Handle.call_deferred_thread_group("handle_git_output", r);
			if !r.success:
				return;
			var new_data: Dictionary = load_file_data(path, false)[0];
			(Handle.saving_window.label as Label).call_deferred_thread_group("set_text", "Checking up differences between commits...");
			var conflicts = [];
			for entry_name in new_data.keys():
				if Handle.strings.has(entry_name) && old_data.has(entry_name):
					var i = 0;
					var old_entry: Array = old_data[entry_name];
					var data_entry: Array = new_data[entry_name];
					for entry in new_data[entry_name]:
						if entry.content != data_entry[i].content:
							if old_entry[i].content == data_entry[i].content:
								# The entry has been changed between the commits,
								# And it hasn't been modified by the author.
								pass;
							else:
								# The entry has been changed between the commits,
								# And it has been modified by the author.
								var conflict = Type.GitConflict.new();
								conflict.current_string = data_entry[i].content;
								conflict.git_string = old_entry[i].content;
								conflicts.append(conflict);
						elif old_entry[i].content != data_entry[i].content:
							# The old entry was different, but the new entry
							# contains the most recent string.
							var conflict = Type.GitConflict.new();
							conflict.current_string = data_entry[i].content;
							conflict.git_string = entry.content;
							conflicts.append(conflict);
						#else:
							# The entry has not been changed.
							# We do nothing.
							#pass;
						i += 1;
			if !conflicts.is_empty():
				call_deferred_thread_group("add_child", Handle.gc_window);
				Handle.gc_window.conflicts = conflicts;
				await Handle.gc_window.tree_exited;
		(Handle.saving_window.label as Label).call_deferred_thread_group("set_text", "Saving...");
		var data = [];
		var size = 0;
		for e in Handle.strings.values():
			size += e.size();
		var v = 0;
		var fe = FileFormat.FormatEntry.new();
		fe.kind = 9;
		fe.data["Style"] = Handle.style;
		data.append(fe);
		Handle.saving_window.progress_bar.call_deferred_thread_group("set_max", size);
		var eq_strings_map = {};
		for key in Handle.strings.keys():
			fe = FileFormat.FormatEntry.new();
			fe.kind = 0;
			fe.data["ID"] = key;
			data.append(str(fe));
			for entry: Type.StringContainer in Handle.strings[key] as Array:
				if !entry.equal_strings.is_empty():
					var cstr = ",".join(PackedStringArray(entry.equal_strings));
					if !eq_strings_map.has(cstr):
						eq_strings_map[cstr] = eq_strings_map.size();
						fe = FileFormat.FormatEntry.new();
						fe.disable_uri = ["Data"];
						fe.kind = 2;
						fe.data["Data"] = cstr;
						data.append(str(fe));
					entry.equal_strings_i = eq_strings_map[cstr];
				data.append(str(entry));
				Handle.saving_window.progress_bar.call_deferred_thread_group("set_value", v);
				v += 1;
		fe = FileFormat.FormatEntry.new();
		fe.kind = 8;
		data.append(str(fe));
		(Handle.saving_window.label as Label).call_deferred_thread_group("set_text", "Saving the file...");
		f = FileAccess.open(path, FileAccess.WRITE);
		f.store_string("\n".join(PackedStringArray(data)));
		f.flush();
		f.close();
		Handle.is_modified = false;
		if FileAccess.file_exists("user://enable_git.bool"):
			(Handle.saving_window.label as Label).call_deferred_thread_group("set_text", "Commiting on the git repo...");
			var r = Handle.git.commit("Update Strings (Using DH)");
			Handle.call_deferred_thread_group("handle_git_output", r);
			if !r.success:
				return;
		parent.call_deferred_thread_group("remove_child", Handle.saving_window);
	);
	return thr;

func load_file(path) -> Thread:
	Handle.main_node.clear_data();
	var cthr = Thread.new();
	cthr.start(func():
		var parent = get_parent();
		if FileAccess.file_exists("user://enable_git.bool"):
			(Handle.loading_window.label as Label).call_deferred_thread_group("set_text", "Fetching the git repo...");
			var r = Handle.git.pull();
			Handle.call_deferred_thread_group("handle_git_output", r);
			if !r.success:
				return;
		(Handle.loading_window.label as Label).call_deferred_thread_group("set_text", "Loading...");
		parent.dialogue_selector.call_deferred_thread_group("clear");
		parent.string_selector.call_deferred_thread_group("clear");
		parent.similar_entries.call_deferred_thread_group("clear");
		
		Handle.entry_names.clear();
		for i in range(Handle.layer_strings.size()):
			Handle.layer_strings[i] = "";
		for i in range(Handle.layer_colors.size()):
			Handle.layer_colors[i] = Color.WHITE;
		Handle.strings.clear();
		var output = load_file_data(path, true, true);
		Handle.strings = output[0];
		Handle.string_table = output[1];
		Handle.string_ids = output[2];
		Handle.entry_names = output[3];
		Handle.last_string_id = output[4];
		Handle.loading_window.label.call_deferred_thread_group("set_text", "Caching similar entries... [This may take a long time.]");
		
		var m = Mutex.new();
		var n = Mutex.new();
		var v = [Handle.loading_window.progress_bar.value];

		var threads = [];
		var running_threads = [];
		var strgs_equal = {};
		var limit = max(OS.get_processor_count() - 4, 1);
		for i in range(limit):
			threads.append([]);
			running_threads.append(Thread.new());
		var current_thread = 0;
		for entry in Handle.strings.keys():
			threads[current_thread % limit].append(entry);
			current_thread += 1;

		current_thread = 0;
		for thr in running_threads:
			thr.start(func():
				var ei = 0;
				var arr = threads[current_thread];
				while ei < arr.size():
					ei += 1;
					for strg in Handle.strings[arr[ei - 1]]:
						if strg.equal_strings == null:
							n.lock();
							strg.equal_strings = [int(strg.id)];
							var has = strgs_equal.has(strg.original_content);
							n.unlock();
							if has:
								strg.equal_strings = strgs_equal[strg.original_content];
							else:
								var strgid = int(strg.id);
								for key in Handle.string_ids.keys():
									var ky = int(key);
									if Handle.string_ids[ky] == strg.original_content && ky != strgid:
										strg.equal_strings.append(ky);
								n.lock();
								strgs_equal[strg.original_content] = strg.equal_strings;
								n.unlock();
					m.lock();
					v[0] += 1;
					m.unlock();
			);
			current_thread += 1;
		
		while true:
			m.lock();
			Handle.loading_window.progress_bar.call_deferred_thread_group("set_value", v[0]);
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
			var is_not_finished = true;
			for thr in running_threads:
				if thr == null:
					continue;
				if thr.is_alive():
					is_not_finished = false;
					break;
			if is_not_finished:
				parent.call_deferred_thread_group("remove_child", Handle.loading_window);
				return;
			OS.delay_msec(1000 / max(Engine.max_fps, 1)); # Update each "frame"
	);
	return cthr;

# [Dictionary (Strings), Dictionary (StrTable), Dictionary (StrIDs), Array (EntryNames), Int (LastStringID)]
func load_file_data(path, apply_settings = true, is_thread = false) -> Array:
	var parent = get_parent();
	var strings = {};
	var string_table = {};
	var string_ids = {};
	var entry_names = [];
	var last_string_id = 0;
	
	var datas = FileAccess.get_file_as_string(path);
	var datat = JSON.parse_string(datas);
	if datat is Dictionary:
		print("Loading from Old [JSON] format.");
		strings = datat;
		var extr = 0;
		var size = 0;
		entry_names.append_array(strings.keys());
		for e in strings.values():
			for sube in e:
				size += 1;
				if sube["EqualStrings"] == null:
					extr += 1;
		if is_thread:
			Handle.string_size = size;
			Handle.loading_window.progress_bar.call_deferred_thread_group("set_max", size + extr);
		var new_max = size + extr;
		var totl = 0;
		for entry_name in strings.keys():
			if is_thread:
				parent.dialogue_selector.call_deferred_thread_group("add_item", entry_name);
			var entry = strings[entry_name];
			var i = 0;
			while i < entry.size():
				string_table[int(entry[i]["ID"])] = Type.StringTable.new(entry_name, entry[i]["Content"], i);
				string_ids[int(entry[i]["ID"])] = entry[i]["Content"];
				if int(entry[i]["ID"]) > last_string_id:
					last_string_id = int(entry[i]["ID"]) + 1;
				var json = Dictionary(entry[i]).duplicate(true);
				entry[i] = Type.StringContainer.new(json);
				i += 1;
				if is_thread:
					if entry[i].equal_strings == null:
						new_max += 1;
						Handle.loading_window.progress_bar.call_deferred_thread_group("set_max", new_max);
					totl += 1;
					Handle.loading_window.progress_bar.call_deferred_thread_group("set_value", totl);
	else:
		print("Loading from new [TXT] format.");
		var arr = FileFormat.parse_file(datas);
		if is_thread:
			Handle.loading_window.progress_bar.call_deferred_thread_group("set_max", arr.size());
		var totl = 0;
		var current_entry = "";
		var entries = [];
		var is_entry = false;
		var new_max = arr.size();
		var eqs_map = {};
		for line: FileFormat.FormatEntry in arr:
			if (line.kind == 0 || line.kind == 8) && is_entry:
				strings[current_entry] = entries;
				entries = [];
			match line.kind:
				8: # File End
					pass;
				9: # Settings
					if apply_settings:
						if is_thread:
							Handle.call_deferred_thread_group("load_style", line.data["Style"]);
						else:
							Handle.load_style(line.data["Style"]);
				0: # New Entry
					current_entry = line.data["ID"];
					entry_names.append(current_entry);
					if is_thread:
						parent.dialogue_selector.call_deferred_thread_group("add_item", current_entry);
					is_entry = true;
				1: # Add String to the current Entry
					var data = Type.StringContainer.new(line);
					if line.data.has("EqualStringsI"):
						#print(line.data["EqualStringsI"]);
						data.equal_strings = eqs_map[line.data["EqualStringsI"]];
					if is_thread && data.equal_strings == null:
						new_max += 1;
						Handle.loading_window.progress_bar.call_deferred_thread_group("set_max", new_max);
					line.data["ID"] = current_entry;
					string_table[int(data.id)] = Type.StringTable.new(current_entry, data.content, entries.size());
					string_ids[int(data.id)] = data.content;
					if int(data.id) > last_string_id:
						last_string_id = int(data.id) + 1;
					entries.append(data);
				2: # Assign Equal Strings Array to Index
					var nea = [];
					for etr in line.data["Data"].split(","):
						nea.append(int(etr));
					eqs_map[str(eqs_map.size())] = nea;
			if is_thread:
				totl += 1;
				Handle.loading_window.progress_bar.call_deferred_thread_group("set_value", totl);
	return [strings, string_table, string_ids, entry_names, last_string_id];
