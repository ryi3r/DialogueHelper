extends RefCounted
class_name IStringContainer

var id := -1
var content := ""
var original_content := ""
var last_edited := ILastEdited.new()
var equal_strings: Array[int] = []
var layer_strings: Array[String] = []
var layer_colors: Array[Color] = []
var box_style := 0
var font_style := 0
var font_scale := 1.0
var enable_portrait := false

var equal_strings_index := -1

func _init(json: IFormatEntry = null) -> void:
	var _json := json.data
	if "ID" in _json:
		id = _json["ID"] as int
	if "OriginalContent" in _json:
		content = str(_json["OriginalContent"])
		original_content = str(_json["OriginalContent"])
	if "Content" in _json:
		content = str(_json["Content"])
	if "LastEdited" in _json:
		last_edited = ILastEdited.new(_json["LastEdited"])
	if "LayerStrings" in _json:
		var _ls := str(_json["LayerStrings"]).split(",")
		var _sls := [content]
		for _e in _ls:
			_sls.append(_e.uri_decode())
		layer_strings.clear()
		layer_strings.assign(_sls)
	else:
		layer_strings = [content]
	while layer_strings.size() < Handle.layers:
		layer_strings.append("")
	if "LayerColors" in _json:
		var _lc := str(_json["LayerColors"]).split(",")
		var _clc := []
		for _e in _lc:
			_clc.append(Color.hex(_e.hex_to_int()))
		layer_colors.clear()
		layer_colors.assign(_clc)
	else:
		layer_colors = []
	while layer_colors.size() < Handle.layers:
		layer_colors.append(Color.WHITE)
	if "BoxStyle" in _json:
		box_style = _json["BoxStyle"] as int
	if "FontStyle" in _json:
		font_style = _json["FontStyle"] as int
	if "FontScale" in _json:
		font_scale = _json["FontScale"] as int
	if "EnablePortrait" in _json:
		enable_portrait = _json["EnablePortrait"] as bool
	if "EqualStringsIndex" in _json:
		equal_strings_index = _json["EqualStringsIndex"] as int

func _to_string() -> String:
	var _entry := IFormatEntry.new()
	_entry.disable_uri = ["LayerStrings", "LayerColors", "LastEdited"]
	_entry.kind = 1
	#_entry.data["ID"] = id
	if content != original_content:
		_entry.data["Content"] = content
	_entry.data["OriginalContent"] = original_content
	if last_edited.author != "" && last_edited.timestamp != -1:
		_entry.data["LastEdited"] = str(last_edited)
	if !"".join(PackedStringArray(layer_strings.slice(1))).is_empty():
		var _ls: Array[String] = layer_strings.slice(1)
		var _cls: Array[String] = []
		var _last := 0
		for _i in range(_ls.size()):
			if !_ls[_i].is_empty():
				_last = _i + 1
			_cls.append(_ls[_i].uri_encode())
		_entry.data["LayerStrings"] = ",".join(PackedStringArray(_ls.slice(0, _last)))
	var _lc := []
	var _lastc := 0
	for _color in layer_colors:
		for _i in range(layer_colors.size()):
			if layer_colors[_i] != Color.WHITE:
				_lastc = _i + 1
			_lc.append(String.num_uint64(layer_colors[_i].to_rgba32(), 16))
	if !"".join(PackedStringArray(_lc)).replace("f", "").is_empty():
		_entry.data["LayerColors"] = ",".join(PackedStringArray(_lc.slice(0, _lastc)))
	if box_style != 0:
		_entry.data["BoxStyle"] = box_style
	if font_style != 0:
		_entry.data["FontStyle"] = font_style
	if font_scale != 1:
		_entry.data["FontScale"] = font_scale
	if enable_portrait:
		_entry.data["EnablePortrait"] = enable_portrait
	if equal_strings_index != -1:
		_entry.data["EqualStringsIndex"] = equal_strings_index
	return str(_entry)

func update() -> void:
	if layer_strings.is_empty():
		layer_strings.append(content)
		while layer_strings.size() < Handle.layers:
			layer_strings.append("")
	if layer_colors.is_empty():
		while layer_colors.size() < Handle.layers:
			layer_colors.append(Color.WHITE)
	if id is not int:
		push_warning("ID is not an Integer!")
	elif id < 0:
		push_warning("ID is not initialized (ID < 0)!")
