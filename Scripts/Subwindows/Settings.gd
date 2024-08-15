extends Window

@onready var enable_git: CheckBox = $EnableGit
@onready var git_url: LineEdit = $EnableGit/RepoLabel/LineEdit
@onready var url_valid: Label = $EnableGit/RepoLabel/ValidLabel
@onready var repo_label: Label = $EnableGit/RepoLabel
@onready var git_branch: LineEdit = $EnableGit/RepoLabel/BranchLabel/LineEdit
@onready var author: LineEdit = $FontStyle/Author/LineEdit
@onready var style_select: OptionButton = $FontStyle/OptionButton

func _ready() -> void:
	style_select.clear()
	var _i := 0
	for _dir in DirAccess.get_directories_at(ProjectSettings.globalize_path("res://Styles/")):
		style_select.add_item(_dir, _i)
		if _dir == Handle.style:
			style_select.select(_i)
		_i += 1
	enable_git.button_pressed = FileAccess.file_exists("user://enable_git.bool")
	repo_label.visible = enable_git.button_pressed
	if FileAccess.file_exists("user://git_url.txt"):
		git_url.text = FileAccess.get_file_as_string("user://git_url.txt")
	if FileAccess.file_exists("user://git_branch.txt"):
		git_branch.text = FileAccess.get_file_as_string("user://git_branch.txt")

func _process(_delta: float) -> void:
	author.text = "-----"
	if !Handle.style_metadata.is_empty():
		if "Author" in Handle.style_metadata:
			author.text = Handle.style_metadata["Author"]
	repo_label.visible = enable_git.button_pressed
	if enable_git.button_pressed:
		if check_git_url():
			url_valid.add_theme_color_override("font_color", Color.LIME)
			url_valid.text = "Url is valid."
		else:
			url_valid.add_theme_color_override("font_color", Color.RED)
			url_valid.text = "Url is NOT valid."

func check_git_url() -> bool:
	var _url := git_url.text
	var _starts_with := false
	for _begin: String in ["https://", "http://", "git://", "ssh://"]:
		if _url.begins_with(_begin):
			_starts_with = true
	if _starts_with && _url.ends_with(".git"):
		return true
	return false

func _on_ok_button_pressed() -> void:
	var _f := FileAccess.open("user://last_style.txt", FileAccess.WRITE)
	_f.store_string(style_select.get_item_text(style_select.selected))
	_f.flush()
	_f.close()
	if enable_git.button_pressed:
		FileAccess.open("user://enable_git.bool", FileAccess.WRITE).close()
		_f = FileAccess.open("user://git_url.txt", FileAccess.WRITE)
		_f.store_string(git_url.text)
		_f.flush()
		_f.close()
		_f = FileAccess.open("user://git_branch.txt", FileAccess.WRITE)
		_f.store_string(git_branch.text)
		_f.flush()
		_f.close()
		Handle.git.url = git_url.text
		Handle.git.branch = git_branch.text
		if !DirAccess.open("user://").dir_exists("user://repo/"):
			var _r := Handle.git.clone()
			Handle.call_deferred("handle_git_output", _r)
			if !_r.success:
				return
		else:
			var _r := Handle.git.set_url()
			Handle.call_deferred("handle_git_output", _r)
			if !_r.success:
				return
			_r = Handle.git.pull()
			Handle.call_deferred("handle_git_output", _r)
			if !_r.success:
				return
	else:
		if FileAccess.file_exists("user://enable_git.bool"):
			DirAccess.open("user://").remove("user://enable_git.bool")
	queue_free()

func _on_cancel_button_pressed() -> void:
	queue_free()

func _on_close_requested() -> void:
	queue_free()

func _on_option_button_item_selected(_index: int) -> void:
	Handle.load_style(style_select.get_item_text(_index))
