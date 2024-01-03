extends Window;

@onready var search_type: OptionButton = $SearchTypeLabel/OptionButton;
@onready var search_for: TextEdit = $SearchForLabel/TextEdit;
@onready var case_sensitive: CheckBox = $CaseSensitive;

var searching_scene = preload("res://Subwindows/Progress bars/Searching.tscn");
var searching_window = null;
var search_results_scene = preload("res://Subwindows/SearchResults.tscn");
var search_results_window = null;

var last_thread = null;

func _ready():
	search_type.select(get_parent().search_kind);

func _process(_delta):
	if last_thread is Thread:
		if !last_thread.is_alive():
			last_thread.wait_to_finish();
			last_thread = null;
			_on_close_requested();

func _on_close_requested():
	var t = create_tween();
	t.tween_interval(1.0 / 60.0);
	t.tween_callback(func():
		get_parent().remove_child(self);
	);
	t.play();

func _on_search_button_pressed():
	if search_type.get_selected_id() == -1:
		return;
	var do_casing = case_sensitive.button_pressed;
	var search = search_for.text;
	var kind = search_type.get_item_text(search_type.get_selected_id());
	if !do_casing:
		search = search.to_lower();
	get_parent().search_kind = search_type.get_selected_id();
	var data = get_parent().data as Dictionary;
	searching_window = searching_scene.instantiate();
	add_child(searching_window);
	searching_window.progress_bar.set_max(get_parent().data_size);
	last_thread = Thread.new();
	search_results_window = search_results_scene.instantiate();
	get_parent().add_child(search_results_window);
	search_results_window.title = "Search results for: " + str(search_for.text);
	var il: ItemList = search_results_window.get_node("ItemList");
	last_thread.start(func():
		var v = 0;
		for entry_name in data.keys():
			var index = 0;
			for strg in data[entry_name]:
				var s = str(strg.content) if kind == "String" else str(entry_name);
				if !do_casing:
					s = s.to_lower();
				if s.contains(search):
					if kind == "Entry":
						if index == 0:
							il.call_deferred_thread_group("add_item", entry_name);
					else:
						il.call_deferred_thread_group("add_item", entry_name + ":" + str(index + 1));
				v += 1;
				index += 1;
				searching_window.progress_bar.call_deferred_thread_group("set_value", v);
		call_deferred_thread_group("set_visible", false);
		search_results_window.call_deferred_thread_group("grab_focus");
	);
