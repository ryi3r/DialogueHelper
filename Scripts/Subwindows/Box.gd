extends ScrollContainer;

@export var current_box: int = 0;
var portrait_enabled = false;
@onready var spr = $Handler/BoxSprite;

var supports_portrait = false;
var dialogue_offset = Vector2i(20, 20);
var portrait_offset = Vector2i(145, 20);

func _process(_delta):
	supports_portrait = false;
	dialogue_offset = Vector2i(0, 0);
	portrait_offset = Vector2i(0, 0);
	scale = Vector2();
	if !Handle.box_data.is_empty() && current_box < Handle.box_data.size():
		var box: GMBox.GBox = Handle.box_data[current_box];
		spr.texture = box.texture;
		supports_portrait = box.supports_portrait;
		dialogue_offset = box.dialogue_offset;
		portrait_offset = box.portrait_offset;
		scale = Vector2(box.scale, box.scale);
