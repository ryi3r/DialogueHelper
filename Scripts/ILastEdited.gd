extends RefCounted
class_name ILastEdited

var timestamp := -1
var author := ""

func _init(_json: Variant = null, _timestamp: Variant = null, _author: Variant = null) -> void:
	if _json is Dictionary:
		if "Timestamp" in _json:
			timestamp = _json["Timestamp"] as int
		if "Author" in _json:
			author = str(_json["Author"])
	elif _json is String:
		var _data := str(_json).split(",")
		author = _data[0].uri_decode()
		timestamp = int(_data[1])
	elif _timestamp is int && _author is String:
		timestamp = _timestamp
		author = _author

func _to_string() -> String:
	return "%s,%s" % [author.uri_encode(), str(int(timestamp))]
