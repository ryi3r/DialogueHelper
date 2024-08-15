extends Window
class_name WUnsavedChanges

var is_quitting := false

func _on_close_requested() -> void:
	queue_free()

func _on_ok_button_pressed() -> void:
	if is_quitting:
		get_tree().quit()
	else:
		(get_parent() as WDialogueHelper).clear_data()
	queue_free()

func _on_cancel_button_pressed() -> void:
	queue_free()
