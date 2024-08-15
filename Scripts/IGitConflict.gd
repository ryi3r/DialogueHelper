extends RefCounted
class_name IGitConflict

enum IKeep {
	NoChoice,
	KeepGit,
	KeepOriginal,
}

var keep := IKeep.NoChoice
var current_string := ""
var git_string := ""
