extends RefCounted
class_name IFormatEntry

var kind := -1
var data := {}
var disable_uri := []

func _to_string() -> String:
	var _str := ""
	for key: Variant in data.keys():
		var value := str(data[key])
		if !_str.is_empty():
			_str += ";"
		if !disable_uri.has(key):
			for ad: Array in [
				[&"%", &"%25"],
				[&";", &"%3B"],
				[&":", &"%3A"],
				[&"\n", &"%0A"],
				[&"\r", &"%0D"],
			]:
				value = value.replace(str(ad[0]), str(ad[1]))
		_str += "%s:%s" % [key, value]
	return "%s;%s" % [kind, _str]
