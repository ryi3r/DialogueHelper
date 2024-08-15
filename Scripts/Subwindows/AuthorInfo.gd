extends Window

func _on_close_requested() -> void:
	queue_free()

func _on_cancel_button_pressed() -> void:
	queue_free()

func _on_ok_button_pressed() -> void:
	var _author := ($Label/LineEdit as LineEdit).text
	(get_parent() as WDialogueHelper).author = _author
	var _f := FileAccess.open("user://username.txt", FileAccess.WRITE)
	_f.store_string(_author)
	_f.flush()
	_f.close()
	queue_free()
