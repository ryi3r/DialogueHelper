extends Window;

func _on_close_requested():
	var t = create_tween();
	t.tween_interval(1.0 / 60.0);
	t.tween_callback(func():
		get_parent().remove_child(self);
	);
	t.play();

func _on_cancel_button_pressed():
	_on_close_requested();

func _on_ok_button_pressed():
	get_parent().author = $Label/LineEdit.text;
	var f = FileAccess.open("user://username.txt", FileAccess.WRITE);
	f.store_string(get_parent().author);
	f.flush();
	f.close();
	_on_close_requested();
