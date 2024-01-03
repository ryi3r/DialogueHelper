extends Window;

@onready var enable_git: CheckBox = $EnableGit;
@onready var git_url: LineEdit = $EnableGit/RepoLabel/LineEdit;
@onready var url_valid: Label = $EnableGit/RepoLabel/ValidLabel;
@onready var repo_label: Label = $EnableGit/RepoLabel;
@onready var git_branch: LineEdit = $EnableGit/RepoLabel/BranchLabel/LineEdit;

func _ready():
	enable_git.button_pressed = FileAccess.file_exists("user://enable_git.bool");
	repo_label.visible = enable_git.button_pressed;
	if FileAccess.file_exists("user://git_url.txt"):
		git_url.text = FileAccess.get_file_as_string("user://git_url.txt");
	if FileAccess.file_exists("user://git_branch.txt"):
		git_branch.text = FileAccess.get_file_as_string("user://git_branch.txt");

func _process(_delta):
	repo_label.visible = enable_git.button_pressed;
	if enable_git.button_pressed:
		if check_git_url():
			url_valid.add_theme_color_override("font_color", Color.LIME);
			url_valid.text = "Url is valid.";
		else:
			url_valid.add_theme_color_override("font_color", Color.RED);
			url_valid.text = "Url is NOT valid.";

func check_git_url() -> bool:
	var url = git_url.text; # https://github.com/Banana-Dorada-UTY/translation.git
	if !url.begins_with("https://") && !url.begins_with("http://") \
		&& !url.begins_with("git://") && !url.begins_with("ssh://"):
		return false;
	if !url.ends_with(".git"):
		return false;
	return true;

func _on_ok_button_pressed():
	if enable_git.button_pressed:
		FileAccess.open("user://enable_git.bool", FileAccess.WRITE).close();
		var f = FileAccess.open("user://git_url.txt", FileAccess.WRITE);
		f.store_string(git_url.text);
		f.flush();
		f.close();
		f = FileAccess.open("user://git_branch.txt", FileAccess.WRITE);
		f.store_string(git_branch.text);
		f.flush();
		f.close();
		get_parent().git.url = git_url.text;
		get_parent().git.branch = git_branch.text;
		if !DirAccess.open("user://").dir_exists("user://repo/"):
			var r = get_parent().git.clone();
			get_parent().call_deferred_thread_group("handle_git_output", r);
			if !r.success:
				return;
		else:
			var r = get_parent().git.set_url();
			get_parent().call_deferred_thread_group("handle_git_output", r);
			if !r.success:
				return;
			r = get_parent().git.pull();
			get_parent().call_deferred_thread_group("handle_git_output", r);
			if !r.success:
				return;
	else:
		if FileAccess.file_exists("user://enable_git.bool"):
			DirAccess.open("user://").remove("user://enable_git.bool");
	_on_close_requested();

func _on_cancel_button_pressed():
	_on_close_requested();

func _on_close_requested():
	var t = create_tween();
	t.tween_interval(1.0 / 60.0);
	t.tween_callback(func():
		get_parent().remove_child(self);
	);
	t.play();
