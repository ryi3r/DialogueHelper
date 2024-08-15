extends Window
class_name WUnsavedChanges

signal callback()

func _on_close_requested() -> void:
	queue_free()

func _on_ok_button_pressed() -> void:
	if !callback.is_null():
		callback.emit()
	queue_free()

func _on_cancel_button_pressed() -> void:
	queue_free()
