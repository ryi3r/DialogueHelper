extends ScrollContainer
class_name WBox

@export var current_box := 0
var portrait_enabled := false
@onready var spr: Sprite2D = $Handler/BoxSprite
@onready var handle: Control = $Handler

var supports_portrait := false
var dialogue_offset := Vector2i(20, 20)
var portrait_offset := Vector2i(145, 20)

func _process(_delta: float) -> void:
	supports_portrait = false
	dialogue_offset = Vector2i.ZERO
	portrait_offset = Vector2i.ZERO
	scale = Vector2.ZERO
	if !Handle.box_data.is_empty() && current_box < Handle.box_data.size():
		var box: IBox = Handle.box_data[current_box]
		if spr.texture != box.texture:
			spr.texture = box.texture
		supports_portrait = box.supports_portrait
		dialogue_offset = box.dialogue_offset
		portrait_offset = box.portrait_offset
		var _scale := Vector2(box.scale, box.scale)
		if _scale != scale:
			scale = _scale
