extends Node

var loading_scene := preload("res://Subwindows/ProgressBars/Loading.tscn")
var saving_scene := preload("res://Subwindows/ProgressBars/Saving.tscn")
var fd_scene := preload("res://Subwindows/FileAction/LoadFile.tscn")
var show_info_scene := preload("res://Subwindows/ShowInfo.tscn")
var author_scene := preload("res://Subwindows/AuthorInfo.tscn")
var settings_scene := preload("res://Subwindows/Settings.tscn")
var fds_scene := preload("res://Subwindows/FileAction/SaveFile.tscn")
var goto_scene := preload("res://Subwindows/GoTo.tscn")
var search_scene := preload("res://Subwindows/Search.tscn")
var gc_scene := preload("res://Subwindows/GitConflict.tscn")
var gcu_scene := preload("res://Subwindows/GitConflictUnreplaced.tscn")
var se_scene := preload("res://Subwindows/StyleError.tscn")
var ls_scene := preload("res://Subwindows/ProgressBars/LoadingStyle.tscn")
var uc_scene := preload("res://Subwindows/UnsavedChanges.tscn")
var adh_scene := preload("res://Subwindows/AboutDh.tscn")
var ae_scene := preload("res://Subwindows/AddEntry.tscn")
var as_scene := preload("res://Subwindows/AddString.tscn")

var loading_window: WLoading = null
var saving_window: WLoading = null
var fd_window: FileDialog = null
var show_info_window: Window = null
var author_window: Node = null
var settings_window: Node = null
var fds_window: FileDialog = null
var goto_window: Node = null
var search_window: Node = null
var gc_window: WGitConflict = null
var gcu_window: Node = null
var se_window: Node = null
var ls_window: Node = null
var uc_window: WUnsavedChanges = null
var adh_window: Node = null
var ae_window: Node = null
var as_window: WAddString = null

var style := "Template"

var font_metadata := {}
var box_metadata := {}
var style_metadata := {}
var font_data := []
var box_data := []
var user_script := GDScript.new()

var visual_scale := 1.0
var current_font := 0
var layers := 5
var layer_strings := []
var layer_colors := []

var strings := {}
var string_table := {}
var string_ids := {}
var string_sstr := {}

var string_sstr_arr: Array[Array] = []
var entry_names := []
var last_string_id := 0
var string_size := 0
var is_modified := false

var git := IGit.new()
var main_node: WDialogueHelper = null

func load_style(_style: Variant = null) -> void:
	if _style is String:
		style = _style
	font_metadata.clear()
	box_metadata.clear()
	style_metadata.clear()
	font_data.clear()
	box_data.clear()
	var logs := PackedStringArray()
	ls_window = ls_scene.instantiate()
	add_child(ls_window)
	var progress_bar: ProgressBar = ls_window.get_node("ProgressBar")
	if FileAccess.file_exists(style_get_path("Metadata.json")):
		style_metadata = JSON.parse_string(FileAccess.get_file_as_string(style_get_path("Metadata.json")))
		if style_metadata == null:
			logs.append("%s had a JSON parsing error." % style_get_relative_path("Metadata.json"))
			style_metadata = {}
		elif style_metadata.has("Script"):
				user_script.source_code = FileAccess.get_file_as_string(style_get_path(str(style_metadata["Script"])))
				var error := user_script.reload()
				if error != OK:
					logs.append("Script failed to compile with error %s." % error)
		if FileAccess.file_exists(style_get_path("Fonts/Metadata.json")):
			font_metadata = JSON.parse_string(FileAccess.get_file_as_string(style_get_path("Fonts/Metadata.json")))
			if font_metadata == null:
				logs.append("%s had a JSON parsing error." % style_get_relative_path("Metadata.json"))
			elif font_metadata.has("Fonts"):
				progress_bar.max_value += (font_metadata["Fonts"] as Array).size()
				for font: String in font_metadata["Fonts"] as Array:
					if FileAccess.file_exists(style_get_path("Fonts/%s.json" % font)):
						var font_json: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(style_get_path("Fonts/%s.json" % font)))
						if font_json == null:
							logs.append("%s had a JSON parsing error." % style_get_relative_path("Fonts/%s.json" % font))
						else:
							font_data.append(IFont.new(font_json))
						progress_bar.value += 1
					else:
						logs.append("%s does not exist." % style_get_relative_path("Fonts/%s.json" % font))
		else:
			logs.append("%s does not exist." % style_get_relative_path("Fonts/Metadata.json"))
		if FileAccess.file_exists(style_get_path("Boxes/Metadata.json")):
			box_metadata = JSON.parse_string(FileAccess.get_file_as_string(style_get_path("Boxes/Metadata.json")))
			if box_metadata == null:
				logs.append("%s had a JSON parsing error." % style_get_relative_path("Metadata.json"))
			elif box_metadata.has("Boxes"):
				progress_bar.max_value += (box_metadata["Boxes"] as Array).size()
				for box: String in box_metadata["Boxes"] as Array:
					if FileAccess.file_exists(style_get_path("Boxes/%s.json" % box)):
						var box_json: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(style_get_path("Boxes/%s.json" % box)))
						if box_json == null:
							logs.append("%s had a JSON parsing error." % style_get_relative_path("Boxes/%s.json" % box))
						else:
							box_data.append(IBox.new(box_json))
					else:
						logs.append("%s does not exist." % style_get_relative_path("Boxes/%s.json" % box))
					progress_bar.value += 1
		else:
			logs.append("%s does not exist." % style_get_relative_path("Boxes/Metadata.json"))
		progress_bar.value = progress_bar.max_value
	else:
		logs.append("Style Path \"%s\" was not found." % style_get_relative_path(""))
	ls_window.queue_free()
	if !logs.is_empty(): # An error ocurred.
		se_window = se_scene.instantiate()
		(se_window.get_node("TextEdit") as TextEdit).text = "\n".join(PackedStringArray(logs))
		add_child(se_window)

func style_get_path(_path: String) -> String:
	return "res://Styles/%s/%s" % [style, _path]

func style_get_relative_path(_path: String) -> String:
	return "/%s/%s" % [style, _path]

func handle_git_output(r: IGitResponse) -> void:
	if !r.success:
		var w := preload("res://Subwindows/GitError.tscn").instantiate()
		add_child(w)
		(w.get_node("TextEdit") as TextEdit).text = "".join(PackedStringArray(r.output))
