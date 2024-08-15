extends RefCounted
class_name IStringTable

var name := ""
var entry := ""
var index := -1
var data: IStringContainer = null

func _init(_name := "", _entry := "", _index := -1, _data: IStringContainer = null) -> void:
	name = _name
	entry = _entry
	index = _index
	data = _data

func to_json() -> Dictionary:
	return {
		"Name": name,
		"Entry": entry,
		"Index": index,
	}

func _to_string() -> String:
	return JSON.stringify(to_json(), "", false)
