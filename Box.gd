extends Node2D;

@export var x_offset: int = 25;
@export var box_num: int = 0;
@onready var spr = $BoxSprite;

var supports_portrait = false;
var dialogue_offset = Vector2(20, 20);
var portrait_offset = Vector2(145, 20);

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta):
	supports_portrait = false;
	match box_num:
		0:
			supports_portrait = true;
			dialogue_offset = Vector2(20, 20);
			portrait_offset = Vector2(145, 20);
			spr.texture = preload("res://Boxes/Box1.png");
		1:
			dialogue_offset = Vector2(20, 20);
			spr.texture = preload("res://Boxes/Box2.png");
		2:
			dialogue_offset = Vector2(35, 9);
			spr.texture = preload("res://Boxes/Box3.png");
		3:
			dialogue_offset = Vector2(20, 9);
			spr.texture = preload("res://Boxes/Box4.png");
		4:
			dialogue_offset = Vector2(10, 9);
			spr.texture = preload("res://Boxes/Box5.png");
		5:
			dialogue_offset = Vector2(15, 9);
			spr.texture = preload("res://Boxes/Box6.png");
		6:
			dialogue_offset = Vector2(10, 9);
			spr.texture = preload("res://Boxes/Box7.png");
		7:
			dialogue_offset = Vector2(9, 4);
			spr.texture = preload("res://Boxes/Box8.png");
		8:
			dialogue_offset = Vector2(9, 10);
			spr.texture = preload("res://Boxes/Box9.png");
		9:
			dialogue_offset = Vector2(15, 7);
			spr.texture = preload("res://Boxes/Box10.png");
		10:
			dialogue_offset = Vector2(10, 9);
			spr.texture = preload("res://Boxes/Box11.png");
		11:
			dialogue_offset = Vector2(10, 20);
			spr.texture = preload("res://Boxes/Box12.png");
		12:
			dialogue_offset = Vector2(10, 20);
			spr.texture = preload("res://Boxes/Box13.png");
		13:
			dialogue_offset = Vector2(20, 15);
			spr.texture = preload("res://Boxes/Box14.png");
		14:
			dialogue_offset = Vector2(9, 15);
			spr.texture = preload("res://Boxes/Box15.png");
		15:
			dialogue_offset = Vector2(39, 15);
			spr.texture = preload("res://Boxes/Box16.png");
		16:
			dialogue_offset = Vector2(15, 15);
			spr.texture = preload("res://Boxes/Box17.png");
		17:
			dialogue_offset = Vector2(10, 9);
			spr.texture = preload("res://Boxes/Box18.png");
		18:
			dialogue_offset = Vector2(20, 9);
			spr.texture = preload("res://Boxes/Box19.png");
		19:
			dialogue_offset = Vector2(21, 9);
			spr.texture = preload("res://Boxes/Box20.png");
		20:
			dialogue_offset = Vector2(10, 9);
			spr.texture = preload("res://Boxes/Box21.png");
		21:
			dialogue_offset = Vector2(21, 9);
			spr.texture = preload("res://Boxes/Box22.png");
		22:
			dialogue_offset = Vector2(10, 9);
			spr.texture = preload("res://Boxes/Box23.png");
		23:
			dialogue_offset = Vector2(10, 9);
			spr.texture = preload("res://Boxes/Box24.png");
		24:
			dialogue_offset = Vector2(10, 9);
			spr.texture = preload("res://Boxes/Box25.png");
		25:
			dialogue_offset = Vector2(40, 20);
			spr.texture = preload("res://Boxes/Box26.png");
		26:
			dialogue_offset = Vector2(56, 20);
			spr.texture = preload("res://Boxes/Box27.png");
		27:
			dialogue_offset = Vector2(26, 28);
			spr.texture = preload("res://Boxes/Box28.png");
		28:
			supports_portrait = true;
			dialogue_offset = Vector2(60, 20);
			portrait_offset = Vector2(176, 20);
			spr.texture = preload("res://Boxes/Box29.png");
