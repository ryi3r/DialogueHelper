extends Window
class_name WGitConflict

var conflicts: Array[IGitConflict] = []
var entry := 0

@onready var done_button: Button = $DoneButton
@onready var label_conflict: Label = $LabelConflict/Label
@onready var entry_conflict: SpinBox = $LabelConflict/SpinBox
@onready var current_string: TextEdit = $LabelCurrent/TextEdit
@onready var git_string: TextEdit = $LabelGit/TextEdit
@onready var current_label: Label = $LabelCurrent
@onready var git_label: Label = $LabelGit

func _ready() -> void:
	label_conflict.text = "out of {0}.".format([conflicts.size()])
	entry_conflict.max_value = conflicts.size()
	change_current_entry(entry + 1)

func change_current_entry(_entry: int) -> void:
	_entry -= 1
	if _entry >= 0 && _entry < conflicts.size():
		entry = _entry
		var entry_data := conflicts[entry]
		current_string.text = entry_data.current_string
		git_string.text = entry_data.git_string
		update_selected_string()

func update_selected_string() -> void:
	current_label.text = "Current String:"
	git_label.text = "Git String:"
	match conflicts[entry].keep:
		IGitConflict.IKeep.KeepOriginal:
			current_label.text = "Current String [SELECTED]:"
		IGitConflict.IKeep.KeepGit:
			git_label.text = "Git String [SELECTED]:"

func _on_close_requested() -> void:
	var has_finished := true
	for conflict: IGitConflict in conflicts:
		if conflict.keep == IGitConflict.IKeep.NoChoice:
			has_finished = false
			break
	if has_finished:
		queue_free()
	else:
		Handle.gcu_window = Handle.gcu_scene.instantiate()
		add_child(Handle.gcu_window)

func resolved_merge_conflicts() -> void:
	queue_free()

func _on_spin_box_value_changed(_value: int) -> void:
	change_current_entry(_value)

func _on_button_git_pressed() -> void:
	conflicts[entry].keep = IGitConflict.IKeep.KeepGit
	update_selected_string()

func _on_button_og_pressed() -> void:
	conflicts[entry].keep = IGitConflict.IKeep.KeepOriginal
	update_selected_string()

func _on_done_button_pressed() -> void:
	_on_close_requested()
