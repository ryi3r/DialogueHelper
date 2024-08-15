extends Window

func _on_close_requested() -> void:
	queue_free()

func _on_ok_button_pressed() -> void:
	queue_free()
	(get_parent() as WGitConflict).resolved_merge_conflicts()

func _on_cancel_button_pressed() -> void:
	queue_free()
