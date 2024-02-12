extends Window;

var is_quitting = false;

func _on_close_requested():
	var t = create_tween();
	t.tween_interval(1.0 / 60.0);
	t.tween_callback(func():
		get_parent().remove_child(self);
	);
	t.play();

func _on_ok_button_pressed():
	_on_close_requested();
	if is_quitting:
		get_tree().quit();
	else:
		get_parent().clear_data();

func _on_cancel_button_pressed():
	_on_close_requested();
