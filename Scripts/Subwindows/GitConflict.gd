extends Window;

var conflicts = null;
var current_entry = 0;

@onready var done_button: Button = $DoneButton;
@onready var selected_entry: LineEdit = $LabelEntry/LineEdit;
@onready var label_conflict: Label = $LabelConflict/Label;
@onready var entry_conflict: SpinBox = $LabelConflict/SpinBox;
@onready var current_string: TextEdit = $LabelCurrent/TextEdit;
@onready var git_string: TextEdit = $LabelGit/TextEdit;
@onready var current_label: Label = $LabelCurrent;
@onready var git_label: Label = $LabelGit;

func _ready():
	label_conflict.text = "out of {0}.".format([conflicts.size()]);
	entry_conflict.max_value = conflicts.size();
	change_current_entry(current_entry + 1);

func change_current_entry(entry: int):
	entry -= 1;
	if entry >= 0 && entry < conflicts.size():
		current_entry = entry;
		var entry_data: Type.GitConflict = conflicts[entry];
		current_string.text = entry_data.current_string;
		git_string.text = entry_data.git_string;
		update_selected_string();

func update_selected_string():
	var entry_data: Type.GitConflict = conflicts[current_entry];
	current_label.text = "Current String:";
	git_label.text = "Git String:";
	match entry_data.keep:
		Type.GitConflictKeep.KeepOriginal:
			current_label.text = "Current String [SELECTED]:";
		Type.GitConflictKeep.KeepGit:
			git_label.text = "Git String [SELECTED]:";

func _on_close_requested():
	var has_finished = true;
	for conflict: Type.GitConflict in conflicts:
		if conflict.keep == Type.GitConflictKeep.NoChoice:
			has_finished = false;
			break;
	if has_finished:
		resolved_merge_conflicts();
	else:
		Handle.gcu_window = Handle.gcu_scene.instantiate();
		add_child(Handle.gcu_window);

func resolved_merge_conflicts():
	var t = create_tween();
	t.tween_interval(1.0 / 60.0);
	t.tween_callback(func():
		get_parent().remove_child(self);
	);
	t.play();

func _on_spin_box_value_changed(value):
	change_current_entry(int(value));

func _on_button_git_pressed():
	(conflicts[current_entry] as Type.GitConflict).keep = Type.GitConflictKeep.KeepGit;
	update_selected_string();

func _on_button_og_pressed():
	(conflicts[current_entry] as Type.GitConflict).keep = Type.GitConflictKeep.KeepOriginal;
	update_selected_string();

func _on_done_button_pressed():
	_on_close_requested();
