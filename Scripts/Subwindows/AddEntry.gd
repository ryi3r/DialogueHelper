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
	var txt = $Label/LineEdit.text;
	Handle.strings[txt] = [];
	var parent = get_parent();
	parent.dialogue_selector.select(parent.dialogue_selector.add_item(txt));
	_on_close_requested();
