extends Window;

func _on_close_requested():
	var t = create_tween();
	t.tween_interval(1.0 / 60.0);
	t.tween_callback(func():
		get_parent().remove_child(self);
	);
	t.play();

func _on_button_pressed():
	_on_close_requested();

func _on_button_2_pressed():
	OS.shell_open("https://github.com/ZorroMundo/dialogue-helper/");
