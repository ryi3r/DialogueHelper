extends Window;

@onready var it: ItemList = $ItemList;

func _on_item_selected(index):
	var parent = get_parent();
	var item = (it.get_item_text(index) + ":1").split(":");
	if parent.data.has(item[0]):
		parent.change_to(item[0], int(item[1]) - 1);
		parent.string_selector.clear();
		for stri in parent.data[item[0]]:
			parent.string_selector.add_item(stri.layer_strings[0]);
		parent.string_selector.select(int(item[1]) - 1);
		parent.string_selector.ensure_current_is_visible();
		var i = 0;
		while i < parent.dialogue_selector.get_item_count():
			if parent.dialogue_selector.get_item_text(i) == item[0]:
				parent.dialogue_selector.select(i);
				parent.dialogue_selector.ensure_current_is_visible();
				break;
			i += 1;

func _on_close_requested():
	var t = create_tween();
	t.tween_interval(1.0 / 60.0);
	t.tween_callback(func():
		get_parent().remove_child(self);
	);
	t.play();
