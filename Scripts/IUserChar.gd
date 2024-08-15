extends RefCounted
class_name IUserChar

@warning_ignore("shadowed_global_identifier")
var char := "" # 1-letter String "layer_string[layer][index]" [READ ONLY]
var index := -1 # Char index contained in the current layer string "layer_string[layer]" [READ ONLY]

var start_position := Vector2.ZERO # Glyph starter position
var position_offset := Vector2.ZERO # Glyph local offset
var glyph := Rect2() # Glyph draw Rect

var is_ignore := false # Char is classified as "ignore" [READ ONLY]
var is_newline := false # Char is classified as "new line" [READ ONLY]
