extends Object
class_name FileFormat

static func parse_line(_line: String) -> IFormatEntry:
	var _index := 0
	var _last_entry := 0
	var _got_name := false
	var _name := ""
	var _fe := IFormatEntry.new()
	
	for _char: String in _line:
		if _char == ":" && !_got_name: # Entry Start
			_name = _line.substr(_last_entry, _index - _last_entry)
			_got_name = true
			_last_entry = _index + 1
		elif _char == ";" || _index == _line.length() - 1: # Entry End
			if !_got_name:
				_fe.kind = int(_line.substr(_last_entry, _index - _last_entry).uri_decode())
				_last_entry = _index + 1
			else:
				if _index == _line.length() - 1 && _char != ";":
					_index += 1
				_fe.data[_name] = _line.substr(_last_entry, _index - _last_entry).uri_decode()
				_last_entry = _index + 1
				_got_name = false
		_index += 1
	return _fe

static func parse_file(_data: String) -> Array:
	var _arr := []
	for _line in _data.replace("\r", "").split("\n"):
		_arr.append(parse_line(_line))
	return _arr
