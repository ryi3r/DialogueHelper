extends Window

func _on_close_requested() -> void:
	queue_free()

func _on_cancel_button_pressed() -> void:
	queue_free()

func _on_ok_button_pressed() -> void:
	var txt := ($Label/LineEdit as LineEdit).text
	Handle.strings[txt] = []
	var _item := Handle.main_node.dialogue_selector.add_item(txt)
	Handle.main_node.dialogue_selector.select(_item)
	Handle.main_node._on_item_list_item_selected(_item)
	queue_free()
