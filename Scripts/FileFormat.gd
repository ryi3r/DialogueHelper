extends Node;

class FormatEntry:
	var kind = -1;
	var data = {};
	var disable_uri = [];
	
	func _to_string():
		var stri = "";
		for key in data.keys():
			var value = data[key];
			if stri.length() != 0:
				stri += ";";
			stri += key;
			stri += ":";
			stri += str(value).uri_encode() if !(key in disable_uri) else str(value);
		return str(kind) + ";" + stri;

func parse_line(line: String) -> FormatEntry:
	var index = 0;
	var last_entry = 0;
	var got_name = false;
	var _name = null;
	var fe = FormatEntry.new();
	for chr in line:
		if chr == ":" && !got_name: # Entry Start
			_name = line.substr(last_entry, index - last_entry);
			got_name = true;
			last_entry = index + 1;
		elif chr == ";" || index == line.length() - 1: # Entry End
			if !got_name:
				fe.kind = int(line.substr(last_entry, index - last_entry).uri_decode());
				last_entry = index + 1;
			else:
				if index == line.length() - 1 && chr != ";":
					index += 1;
				var value = line.substr(last_entry, index - last_entry).uri_decode();
				fe.data[_name] = value;
				last_entry = index + 1;
				got_name = false;
		index += 1;
	return fe;

func parse_file(data: String) -> Array:
	var arr = [];
	for line in data.replace("\r", "").split("\n"):
		arr.append(parse_line(line));
	return arr;
