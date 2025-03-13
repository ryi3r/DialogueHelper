extends Window

@onready var enable_git: CheckBox = $EnableGit
@onready var git_url: LineEdit = $EnableGit/RepoLabel/LineEdit
@onready var url_valid: Label = $EnableGit/RepoLabel/ValidLabel
@onready var repo_label: Label = $EnableGit/RepoLabel
@onready var git_branch: LineEdit = $EnableGit/RepoLabel/BranchLabel/LineEdit
@onready var author: LineEdit = $FontStyle/Author/LineEdit
@onready var style_select: OptionButton = $FontStyle/OptionButton

var folders: Dictionary[int, String] = {}
var changed_style := false

func _ready() -> void:
	style_select.clear()
	var _i := 0
	var logs := PackedStringArray()
	for _dir in DirAccess.get_directories_at(ProjectSettings.globalize_path(&"res://Styles/")):
		var _name := _dir
		if FileAccess.file_exists(Handle.style_get_path(&"Metadata.json", _dir)):
			var _style_metadata: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(Handle.style_get_path(&"Metadata.json", _dir)))
			if _style_metadata == null:
				logs.append("%s had a JSON parsing error." % Handle.style_get_relative_path(&"Metadata.json", _dir))
				continue
			elif _style_metadata.has(&"Name"):
				_name = str(_style_metadata.Name)
		style_select.add_item(_name, _i)
		folders[_i] = _dir
		if _dir == Handle.style:
			style_select.select(_i)
		_i += 1
	if !logs.is_empty(): # An error ocurred.
		Handle.se_window = Handle.se_scene.instantiate()
		(Handle.se_window.get_node(^"TextEdit") as TextEdit).text = "\n".join(PackedStringArray(logs))
		Handle.add_child(Handle.se_window)
	enable_git.button_pressed = FileAccess.file_exists(&"user://enable_git.bool")
	repo_label.visible = enable_git.button_pressed
	if FileAccess.file_exists(&"user://git_url.txt"):
		git_url.text = FileAccess.get_file_as_string(&"user://git_url.txt")
	if FileAccess.file_exists(&"user://git_branch.txt"):
		git_branch.text = FileAccess.get_file_as_string(&"user://git_branch.txt")

func _process(_delta: float) -> void:
	author.text = "-----"
	if !Handle.style_metadata.is_empty():
		if Handle.style_metadata.has(&"Author"):
			author.text = Handle.style_metadata.Author
	repo_label.visible = enable_git.button_pressed
	if enable_git.button_pressed:
		if check_git_url():
			url_valid.add_theme_color_override(&"font_color", Color.LIME)
			url_valid.text = "Url is valid."
		else:
			url_valid.add_theme_color_override(&"font_color", Color.RED)
			url_valid.text = "Url is NOT valid."

func check_git_url() -> bool:
	var _url := git_url.text
	var _starts_with := false
	for _begin: String in [&"https://", &"http://", &"git://", &"ssh://"]:
		if _url.begins_with(_begin):
			_starts_with = true
	if _starts_with && _url.ends_with(&".git"):
		return true
	return false

func _on_ok_button_pressed() -> void:
	var _f := FileAccess.open(&"user://last_style.txt", FileAccess.WRITE)
	_f.store_string(style_select.get_item_text(style_select.selected))
	_f.flush()
	_f.close()
	if enable_git.button_pressed:
		FileAccess.open(&"user://enable_git.bool", FileAccess.WRITE).close()
		_f = FileAccess.open(&"user://git_url.txt", FileAccess.WRITE)
		_f.store_string(git_url.text)
		_f.flush()
		_f.close()
		_f = FileAccess.open(&"user://git_branch.txt", FileAccess.WRITE)
		_f.store_string(git_branch.text)
		_f.flush()
		_f.close()
		Handle.git.url = git_url.text
		Handle.git.branch = git_branch.text
		if !DirAccess.open(&"user://").dir_exists(&"user://repo/"):
			var _r := Handle.git.clone()
			Handle.handle_git_output.call_deferred(_r)
			if !_r.success:
				return
		else:
			var _r := Handle.git.set_url()
			Handle.handle_git_output.call_deferred(_r)
			if !_r.success:
				return
			_r = Handle.git.pull()
			Handle.handle_git_output.call_deferred(_r)
			if !_r.success:
				return
	else:
		if FileAccess.file_exists(&"user://enable_git.bool"):
			DirAccess.open(&"user://").remove(&"user://enable_git.bool")
	if !changed_style && style_select.selected != -1:
		_on_option_button_item_selected(style_select.selected)
	queue_free()

func _on_cancel_button_pressed() -> void:
	queue_free()

func _on_close_requested() -> void:
	queue_free()

func _on_option_button_item_selected(_index: int) -> void:
	Handle.load_style(folders[_index])
	Handle.main_node.box.handle.force_update = true
	changed_style = true
