extends RefCounted
class_name IGit

var url := ""
var branch := ""

func global_path(_path: String) -> String:
	return ProjectSettings.globalize_path("user://" + _path)

func _run_command(_args: PackedStringArray, _show_console: bool = false) -> IGitResponse:
	var _out := []
	OS.execute("cmd.exe", _args, _out, false, _show_console)
	var _r := IGitResponse.new()
	print("Output:")
	for _line: String in _out:
		print(_line)
		if (_line.begins_with("error:") || _line.begins_with("fatal:")):
			_r.success = false
	print("=======")
	_r.output = _out
	return _r

func clone() -> IGitResponse:
	return _run_command(["/c", "git -C \"{3}\" clone --branch \"{2}\" \"{0}\" \"{1}\"".format([
		url,
		global_path("repo/"),
		branch,
		global_path(""),
	])], true)

func pull() -> IGitResponse:
	return _run_command(["/c", "git -C \"{0}\" restore . & git -C \"{0}\" checkout \"{1}\" & git -C \"{0}\" pull -f".format([
		global_path("repo/"),
		branch,
	])])

func commit(_message: String) -> IGitResponse:
	return _run_command(["/c", "git -C \"{0}\" add . & git -C \"{0}\" commit -m \"{2}\" & git -C \"{0}\" push origin \"{1}\"".format([
		global_path("repo/"),
		branch,
		_message,
	])])

func set_url() -> IGitResponse:
	return _run_command(["/c", "git -C \"%s\" remote set-url origin \"%s\"".format([
		global_path("repo/"),
		url,
	])])
