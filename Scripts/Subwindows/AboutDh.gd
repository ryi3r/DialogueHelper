extends Window

func _on_close_requested() -> void:
	queue_free()

func _on_button_pressed() -> void:
	queue_free()

func _on_button_2_pressed() -> void:
	OS.shell_open("https://github.com/ryi3r/DialogueHelper/")
