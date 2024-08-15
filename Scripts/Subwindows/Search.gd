extends Window

@onready var search_type: OptionButton = $SearchTypeLabel/OptionButton
@onready var search_for: TextEdit = $SearchForLabel/TextEdit
@onready var case_sensitive: CheckBox = $CaseSensitive

var searching_scene := preload("res://Subwindows/Progress bars/Searching.tscn")
var search_results_scene := preload("res://Subwindows/SearchResults.tscn")
var searching_window: WLoading = null
var search_results_window: WSearchResults = null

var last_thread: Thread = null

func _ready() -> void:
	search_type.select((get_parent() as WDialogueHelper).search_kind)

func _process(_delta: float) -> void:
	if last_thread is Thread:
		if !last_thread.is_alive():
			last_thread.wait_to_finish()
			last_thread = null
			queue_free()

func _on_close_requested() -> void:
	(get_parent() as WDialogueHelper).search_kind = search_type.get_selected_id()
	queue_free()

func _on_search_button_pressed() -> void:
	if search_type.get_selected_id() == -1:
		return
	var _do_casing := case_sensitive.button_pressed
	var _search := search_for.text
	var _kind := search_type.get_item_text(search_type.get_selected_id())
	if !_do_casing:
		_search = _search.to_lower()
	(get_parent() as WDialogueHelper).search_kind = search_type.get_selected_id()
	var _data: Dictionary = Handle.strings
	searching_window = searching_scene.instantiate()
	add_child(searching_window)
	searching_window.progress_bar.set_max(Handle.string_size)
	last_thread = Thread.new()
	search_results_window = search_results_scene.instantiate()
	get_parent().add_child(search_results_window)
	search_results_window.title = "Search results for: " + str(search_for.text)
	var _il: ItemList = search_results_window.get_node("ItemList")
	last_thread.start(func() -> void:
		var _v := 0
		for _entry_name: String in _data.keys():
			var _index := 0
			for _strg: IStringContainer in _data[_entry_name]:
				var _s := str(_strg.content) if _kind == "String" else str(_entry_name)
				if !_do_casing:
					_s = _s.to_lower()
				if _s.contains(_search):
					if _kind == "Entry":
						if _index == 0:
							_il.call_deferred("add_item", _entry_name)
					else:
						_il.call_deferred("add_item", _entry_name + ":" + str(_index + 1))
				_v += 1
				_index += 1
				searching_window.progress_bar.call_deferred("set_value", _v)
		call_deferred("set_visible", false)
		search_results_window.call_deferred("grab_focus")
	)
