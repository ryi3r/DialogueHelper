extends RefCounted
class_name IGitConflict

enum IKeep {
	NoChoice,
	KeepGit,
	KeepOriginal,
}

var keep := IKeep.NoChoice
var string_id := -1
var current_string := ""
var git_string := ""
