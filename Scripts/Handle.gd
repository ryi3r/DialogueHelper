extends Node;

var loading_scene = preload("res://Subwindows/Progress bars/Loading.tscn");
var saving_scene = preload("res://Subwindows/Progress bars/Saving.tscn");
var fd_scene = preload("res://Subwindows/File action/LoadFile.tscn");
var show_info_scene = preload("res://Subwindows/ShowInfo.tscn");
var author_scene = preload("res://Subwindows/AuthorInfo.tscn");
var settings_scene = preload("res://Subwindows/Settings.tscn");
var fds_scene = preload("res://Subwindows/File action/SaveFile.tscn");
var goto_scene = preload("res://Subwindows/GoTo.tscn");
var search_scene = preload("res://Subwindows/Search.tscn");
var gc_scene = preload("res://Subwindows/GitConflict.tscn");
var gcu_scene = preload("res://Subwindows/GitConflictUnreplaced.tscn");
var se_scene = preload("res://Subwindows/StyleError.tscn");
var ls_scene = preload("res://Subwindows/Progress bars/LoadingStyle.tscn");
var uc_scene = preload("res://Subwindows/UnsavedChanges.tscn");
var adh_scene = preload("res://Subwindows/AboutDh.tscn");
var ae_scene = preload("res://Subwindows/AddEntry.tscn");
var as_scene = preload("res://Subwindows/AddString.tscn");

var loading_window = null;
var saving_window = null;
var fd_window = null;
var show_info_window = null;
var author_window = null;
var settings_window = null;
var fds_window = null;
var goto_window = null;
var search_window = null;
var gc_window = null;
var gcu_window = null;
var se_window = null;
var ls_window = null;
var uc_window = null;
var adh_window = null;
var ae_window = null;
var as_window = null;

var style = "Template";

var font_metadata = {};
var box_metadata = {};
var style_metadata = {};
var font_data = [];
var box_data = [];
var user_script = null;

var visual_scale = 1;
var current_font = 0;
var layers = 5;
var layer_strings = [];
var layer_colors = [];

var strings = {};
var string_table = {};
var string_ids = {};
var entry_names = [];
var last_string_id = 0;
var string_size = 0;
var is_modified = false;

var git = Type.Git.new();
var main_node = null;

func load_style(_style = null):
	if _style is String:
		style = _style;
	font_metadata.clear();
	box_metadata.clear();
	style_metadata.clear();
	font_data.clear();
	box_data.clear();
	var logs = [];
	ls_window = ls_scene.instantiate();
	add_child(ls_window);
	var progress_bar: ProgressBar = ls_window.get_node("ProgressBar");
	if FileAccess.file_exists(style_get_path("Metadata.json")):
		style_metadata = JSON.parse_string(FileAccess.get_file_as_string(style_get_path("Metadata.json")));
		if style_metadata == null:
			logs.append("{RelativePath} had a JSON parsing error.".format({
				"RelativePath": style_get_relative_path("Metadata.json"),
			}));
			style_metadata = {};
		elif style_metadata.has("Script"):
				user_script = GDScript.new();
				user_script.source_code = FileAccess.get_file_as_string(style_get_path(style_metadata["Script"]));
				var error = user_script.reload();
				if error != OK:
					logs.append("Script failed to compile with error {Error}.".format({
						"Error": str(error),
					}));
		if FileAccess.file_exists(style_get_path("Fonts/Metadata.json")):
			font_metadata = JSON.parse_string(FileAccess.get_file_as_string(style_get_path("Fonts/Metadata.json")));
			if font_metadata == null:
				logs.append("{RelativePath} had a JSON parsing error.".format({
					"RelativePath": style_get_relative_path("Metadata.json"),
				}));
			elif font_metadata.has("Fonts"):
				progress_bar.max_value += font_metadata["Fonts"].size();
				for font in font_metadata["Fonts"]:
					if FileAccess.file_exists(style_get_path("Fonts/{Font}.json".format({
						"Font": font,
					}))):
						var font_json = JSON.parse_string(FileAccess.get_file_as_string(style_get_path("Fonts/{Font}.json".format({
							"Font": font,
						}))));
						if font_json == null:
							logs.append("{RelativePath} had a JSON parsing error.".format({
								"RelativePath": style_get_relative_path("Fonts/{Font}.json".format({
										"Font": font,
									}))
							}));
						else:
							font_data.append(GMFont.GFont.new(font_json));
						progress_bar.value += 1;
					else:
						logs.append("{RelativePath} does not exist.".format({
							"RelativePath": style_get_relative_path("Fonts/{Font}.json".format({
									"Font": font,
								}))
						}));
		else:
			logs.append("{RelativePath} does not exist.".format({
				"RelativePath": style_get_relative_path("Fonts/Metadata.json"),
			}))
		if FileAccess.file_exists(style_get_path("Boxes/Metadata.json")):
			box_metadata = JSON.parse_string(FileAccess.get_file_as_string(style_get_path("Boxes/Metadata.json")));
			if box_metadata == null:
				logs.append("{RelativePath} had a JSON parsing error.".format({
					"RelativePath": style_get_relative_path("Metadata.json"),
				}));
			elif box_metadata.has("Boxes"):
				progress_bar.max_value += box_metadata["Boxes"].size();
				for box in box_metadata["Boxes"]:
					if FileAccess.file_exists(style_get_path("Boxes/{Box}.json".format({
						"Box": box,
					}))):
						var box_json = JSON.parse_string(FileAccess.get_file_as_string(style_get_path("Boxes/{Box}.json".format({
							"Box": box,
						}))));
						if box_json == null:
							logs.append("{RelativePath} had a JSON parsing error.".format({
								"RelativePath": style_get_relative_path("Boxes/{Box}.json".format({
										"Box": box,
									}))
							}));
						else:
							box_data.append(GMBox.GBox.new(box_json));
					else:
						logs.append("{RelativePath} does not exist.".format({
							"RelativePath": style_get_relative_path("Boxes/{Box}.json".format({
									"Box": box,
								}))
						}));
					progress_bar.value += 1;
		else:
			logs.append("{RelativePath} does not exist.".format({
				"RelativePath": style_get_relative_path("Boxes/Metadata.json"),
			}))
		progress_bar.value = progress_bar.max_value;
	else:
		logs.append("Style Path \"{RelativePath}\" was not found.".format({
			"RelativePath": style_get_relative_path(""),
		}));
	remove_child(ls_window);
	if !logs.is_empty(): # An error ocurred.
		se_window = se_scene.instantiate();
		(se_window.get_node("TextEdit") as TextEdit).text = "\n".join(PackedStringArray(logs));
		add_child(se_window);

func style_get_path(path: String) -> String:
	return "res://Styles/{Style}/{Path}".format({
		"Style": style,
		"Path": path,
	});

func style_get_relative_path(path: String) -> String:
	return "/{Style}/{Path}".format({
		"Style": style,
		"Path": path,
	});

func handle_git_output(r: Type.GitResponse):
	if !r.success:
		var w = preload("res://Subwindows/GitError.tscn").instantiate();
		add_child(w);
		w.get_node("TextEdit").text = "".join(PackedStringArray(r.output));
