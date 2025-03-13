extends Window
class_name WSearchResults

@onready var it: ItemList = $ItemList

func _on_item_selected(_index: int) -> void:
	var _item := (it.get_item_text(_index) + ":1").split(":")
	if Handle.strings.has(_item[0]):
		Handle.main_node.inner_selected = Handle.entry_names.find(_item[0])
		Handle.main_node.change_to(_item[0], int(_item[1]) - 1)
		Handle.main_node.string_selector.clear()
		for _stri: IStringContainer in Handle.strings[_item[0]]:
			Handle.main_node.string_selector.add_item(_stri.layer_strings[0])
		Handle.main_node.string_selector.select(int(_item[1]) - 1)
		Handle.main_node.string_selector.ensure_current_is_visible()
		Handle.main_node.dialogue_selector.deselect_all()
		for _i in range(Handle.main_node.dialogue_selector.get_item_count()):
			if Handle.main_node.dialogue_selector.get_item_text(_i) == _item[0]:
				Handle.main_node.dialogue_selector.select(_i)
				Handle.main_node.dialogue_selector.ensure_current_is_visible()
				break

func _on_close_requested() -> void:
	queue_free()
