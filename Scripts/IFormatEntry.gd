extends RefCounted
class_name IFormatEntry

var kind := -1
var data := {}
var disable_uri := []

func _to_string() -> String:
	var _str := ""
	for key: Variant in data.keys():
		var value: Variant = data[key]
		if !_str.is_empty():
			_str += ";"
		_str += "%s:%s" % [key, str(value).uri_encode() if !disable_uri.has(key) else str(value)]
	return "%s;%s" % [kind, _str]
