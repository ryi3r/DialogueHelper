extends RefCounted
class_name ILastEdited

var timestamp := -1
var author := ""

func _init(_json: Variant = null, _timestamp: Variant = null, _author: Variant = null) -> void:
	if _json is Dictionary:
		var _jd: Dictionary = _json
		if _jd.has(&"Timestamp"):
			timestamp = _jd.Timestamp as int
		if _jd.has(&"Author"):
			author = str(_jd.Author)
	elif _json is String:
		var _data := str(_json).split(&",")
		author = _data[0].uri_decode()
		timestamp = int(_data[1])
	elif _timestamp is int && _author is String:
		timestamp = _timestamp
		author = _author

func _to_string() -> String:
	return "%s,%s" % [author.uri_encode(), str(int(timestamp))]
