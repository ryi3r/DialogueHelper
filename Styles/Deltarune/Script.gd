extends RefCounted
# This is the template for GameMaker fonts. (2022.2+ supported.)

const DELTARUNE_FONTS: Array[String] = [
	"fnt_main",
	"fnt_mainbig",
	"fnt_small",
	"fnt_comicsans",
	"fnt_dotumche",
	"fnt_tinynoelle",
	"fnt_ja_main",
	"fnt_ja_mainbig",
	"fnt_ja_small",
	"fnt_ja_comicsans",
	"fnt_ja_dotumche",
	"fnt_ja_kakugo",
	"fnt_ja_tinynoelle",
]

static func prepare_draw(data: IUserData) -> void:
	data.env.last_newline = false
	data.env.started_asterisk = false
	data.env.checked_index = -1
	data.env._e = 0
	data.env._c = 0
	data.env._f = 0
	data.env._sx = -1
	data.env._sy = -1
	data.env.skip = 0
	data.env.first_drawn_char = true
	data.global_env.portrait_target = ""
	if !data.global_env.has("face_texture"):
		data.global_env.face_texture = null
		data.global_env.face_path = ""
		data.global_env.toribody_texture = data.load_texture("Assets/spr_face_tbody.png")

static func draw_portrait(data: IUserData) -> void:
	var _target := ""
	var _x := 30
	var _y := 30
	#print(data.env._c)
	#print(data.env._e)
	match data.env._c:
		1: _target = "spr_face_s%s" % ("0123456789ABCD"[data.env._e])
		2:
			_target = "spr_face_r_nohat/spr_face_r_nohat_%d" % data.env._e
			_x -= 15
			_y -= 10
		3:
			_target = "spr_face_n_matome_cropped/spr_face_n_matome_cropped_%d" % data.env._e
			_x -= 10
			_y -= 10
		4:
			match data.env._e:
				0, 1, 2, 6, 7, 9:
					_target = "spr_face_t%d/spr_face_t%d_0" % [data.env._e, data.env._e]
				_:
					_target = "spr_face_t%d" % data.env._e
			_x += 10
			var _t: Texture2D = data.global_env.toribody_texture
			if _t != null:
				data.draw_texture_rect(_t, Rect2(_x - (7 * 2), _y + (29 * 2), _t.get_width() * 2, _t.get_height() * 2), false)
		5:
			_target = "spr_face_l0/spr_face_l0_%d" % data.env._e
			_x -= 10
			_y -= 10
		6:
			_target = "spr_face_sans%d" % data.env._e
			_x += 10
			_y += 5
		9:
			_target = "spr_face_undyne/spr_face_undyne_%d" % data.env._e
			_x -= 10
		10:
			match data.env._e:
				0, 1, 2, 3, 4, 5, 6:
					_target = "spr_face_asgore%d/spr_face_asgore%d_0" % [data.env._e, data.env._e]
				_:
					_target = "spr_face_asgore%d" % data.env._e
			_x -= 3
			_y -= 5
		11:
			_target = "spr_alphysface/spr_alphysface_%d" % data.env._e
			_x -= 5
		12:
			_target = "spr_face_berdly_dark/spr_face_berdly_dark_%d" % data.env._e
			_x -= 5
			_y += 5
		13:
			_target = "spr_face_c%d" % data.env._e
			_x -= 10
		14:
			_target = "spr_face_jock%d" % data.env._e
			_x -= 10
		15:
			_target = "spr_face_rudy/spr_face_rudy_%d" % data.env._e
			_x -= 7
			_y -= 17
		16:
			_target = "spr_face_catty/spr_face_catty_%d" % data.env._e
			_x -= 10
		17:
			_target = "spr_face_bratty/spr_face_bratty_%d" % data.env._e
			_x -= 5
			_y += 2
		18:
			_target = "spr_face_rurus/spr_face_rurus_%d" % data.env._e
			_x += 5
		19:
			_target = "spr_face_burgerpants/spr_face_burgerpants_%d" % data.env._e
			_x -= 5
			_y -= 5
		20:
			_target = "spr_face_king/spr_face_king_%d" % data.env._e
			_x += 5
			_y -= 5
		21:
			_target = "spr_face_queen/spr_face_queen_%d" % data.env._e
			_x += 5
			_y += 5
	#print(_target)
	if data.global_env.face_path != _target:
		data.global_env.face_path = _target
		data.global_env.face_texture = data.load_texture("Assets/%s.png" % _target)
	if data.global_env.face_texture != null:
		var _t: Texture2D = data.global_env.face_texture
		data.draw_texture_rect(_t, Rect2(_x, _y, _t.get_width() * 2, _t.get_height() * 2), false)

# This function will be called for each glyph (character)
# in the string, this allows you full control of how
# it gets drawn.
static func draw_glyph(data: IUserData) -> void:
	if data.char.is_ignore || (!data.font.glyphs.has(data.char.char) && !data.char.is_newline):
		return
	if data.env.skip > 0:
		data.env.skip -= 1
		return
	if data.char.string.length() - data.char.index >= 3:
		if data.char.string.substr(data.char.index, 3) == "/%%":
			data.env.skip = 2
			return
	if data.char.string.length() - data.char.index >= 2:
		if data.char.string.substr(data.char.index, 2) == "/%" || \
			(data.char.char == "^" && data.char.string.substr(data.char.index + 1, 1).is_valid_int()) || \
			data.char.string.substr(data.char.index, 2) == "%%":
			data.env.skip = 1
			return
		if data.char.char == "\\":
			data.env.skip = 2
			data.global_env.portrait_target = data.char.string.substr(data.char.index + 1, 2)
			if data.global_env.has("portrait_target") && str(data.global_env.portrait_target).length() >= 2:
				match str(data.global_env.portrait_target)[0]:
					"E":
						data.env._e = "0123456789ABCDE".find(str(data.global_env.portrait_target)[1])
						if data.env._e == -1:
							data.env.skip = 1
						else:
							data.set_box_portrait(true)
					"F":
						data.env._c = "0SRNTLs!!UAaB!!r!!u!KQ".find(str(data.global_env.portrait_target)[1])
						if data.env._c == -1:
							data.env.skip = 1
						else:
							data.set_box_portrait(true)
					"T":
						match str(data.global_env.portrait_target)[1]:
							"0": data.env._f = 5
							"1": data.env._f = 2
							"A": data.env._f = 18
							"a": data.env._f = 20
							"N": data.env._f = 12
							"n": data.env._f = 23
							"B": data.env._f = 13
							"S": data.env._f = 10
							"R": data.env._f = 31
							"L": data.env._f = 32
							"X": data.env._f = 40
							"r": data.env._f = 55
							"T": data.env._f = 7
							"J": data.env._f = 35
							"K": data.env._f = 33
							"q": data.env._f = 62
							"Q": data.env._f = 58
							"s": data.env._f = 14
							"U": data.env._f = 17
							"p": data.env._f = 67
							_: data.env.skip = 0
					"c":
						match str(data.global_env.portrait_target)[1]:
							"R": data.glyph.color = Color.RED
							"B": data.glyph.color = Color.BLUE
							"Y": data.glyph.color = Color.YELLOW
							"G": data.glyph.color = Color.LIME
							"W": data.glyph.color = Color.WHITE
							"X": data.glyph.color = Color.BLACK
							"P": data.glyph.color = Color.PURPLE
							"M": data.glyph.color = Color.DARK_RED
							"S": data.glyph.color = Color.hex(0xff80ffff)
							"V": data.glyph.color = Color.hex(0x80ff80ff)
							"0": data.glyph.color = Color.HOT_PINK
							_: data.env.skip = 0
					"S", "s", "C", "M", "I", "m":
						if !str(data.global_env.portrait_target)[1].is_valid_int():
							data.env.skip = 0
					_:
						data.env.skip = 0
			if data.env.skip != 0:
				return
	if (data.char.string.length() - data.char.index == 1 || data.char.index == 0) && data.char.char == "/":
		return
	if data.char.string.length() - data.char.index == 1 && data.char.char == "%":
		return
	var _act_as_newline := data.char.is_newline
	var scale := data.glyph.vscale * data.font.scale * 2.0
	var _add_space_x_newline := false
	match data.env._f:
		1, 2, 3, 5, 7, 8, 10, 11, 12, 13, 15, 17, 18, 19, 20, 21, 40, 41, 55, 60, 61, 63, 64, 666, 667, 999:
			data.set_current_font(DELTARUNE_FONTS.find("fnt_main"))
			data.env._sx = 8
			data.env._sy = 18
		4, 45, 46, 47, 48, 59, 77:
			data.set_current_font(DELTARUNE_FONTS.find("fnt_mainbig"))
			data.env._sx = 16
			data.env._sy = 28
		6, 30, 31, 32, 33, 34, 35, 36, 42, 51, 52, 56, 57, 58, 62, 65, 66, 67, 78:
			data.set_current_font(DELTARUNE_FONTS.find("fnt_mainbig"))
			data.env._sx = 16
			data.env._sy = 36
		37:
			data.set_current_font(DELTARUNE_FONTS.find("fnt_mainbig"))
			data.env._sx = 18
			data.env._sy = 36
		14:
			data.set_current_font(DELTARUNE_FONTS.find("fnt_comicsans"))
			data.env._sx = 8
			data.env._sy = 18
		22, 23:
			data.set_current_font(DELTARUNE_FONTS.find("fnt_tinynoelle"))
			data.env._sx = 6
			data.env._sy = 18
		50, 53, 54, 69, 70, 71, 72, 74, 75, 76:
			data.set_current_font(DELTARUNE_FONTS.find("fnt_dotumche"))
			data.env._sx = 9
			data.env._sy = 20
		_:
			match DELTARUNE_FONTS[data.get_current_font()]:
				"fnt_main":
					data.env._sx = 8
					data.env._sy = 18
				"fnt_mainbig":
					data.env._sx = 16
					data.env._sy = 36
				"fnt_comicsans":
					data.env._sx = 8
					data.env._sy = 18
				"fnt_tinynoelle":
					data.env._sx = 6
					data.env._sy = 18
				"fnt_dotumche":
					data.env._sx = 9
					data.env._sy = 20
	data.font = data.get_font(data.get_current_font())
	if data.char.index > data.env.checked_index:
		if data.env.checked_index == -1:
			data.env.checked_index = 0
		var _i := 0
		var _fs := ""
		while data.char.index + _i < data.char.string.length() && (data.char.string[data.char.index + _i] == " " || data.char.string[data.char.index + _i] == "&"):
			_fs += data.char.string[data.char.index + _i]
			_i += 1
		while data.char.index + _i < data.char.string.length() && (data.char.string[data.char.index + _i] != " " && data.char.string[data.char.index + _i] != "&"):
			var _c := data.char.string[data.char.index + _i]
			match _c:
				"\\":
					_i += 3
					continue
				"^":
					_i += 2
					continue
				"/":
					if data.char.index + _i == data.char.string.length() - 1 || data.char.index + _i == 0:
						_i += 1
						continue
					if (data.char.index + _i == data.char.string.length() - 2 && data.char.string[data.char.index + _i + 1] == "%") || \
						(data.char.index + _i == data.char.string.length() - 3 && data.char.string[data.char.index + _i + 1] == "%" && data.char.string[data.char.index + _i + 2] == "%"):
						_i += 2
						continue
				"%":
					if (data.char.index + _i == data.char.string.length() - 2 && data.char.string[data.char.index + _i + 1] == "%") || \
						data.char.index + _i == data.char.string.length() - 1:
						_i += 2
						continue
			_fs += _c
			_i += 1
		data.env.checked_index = data.char.index + _i
		var _cpos := data.char.position_offset.x
		#print(_fs)
		for _char in _fs:
			var glyph: IGlyph = data.font.glyphs[_char]
			_cpos += ((data.env._sx if data.env._sx != -1 else glyph.shift) + glyph.offset) * scale
		if _cpos + 40 > data.box.texture.get_width() - (data.box.portrait_offset.x if data.get_box_portrait() else 0.0) && !data.env.first_drawn_char:
			_act_as_newline = true
			if data.env.started_asterisk:
				_add_space_x_newline = true
	if _act_as_newline:
		#data.env.started_asterisk = false
		data.env.last_newline = true
		if data.env.started_asterisk && data.char.string.length() - data.char.index >= 2 && data.char.string[data.char.index + 1] != "*":
			_add_space_x_newline = true
		if _add_space_x_newline:
			var glyph: IGlyph = data.font.glyphs[" "]
			data.char.position_offset.x = ((data.env._sx if data.env._sx != -1 else glyph.shift) + glyph.offset) * scale * 2.0
		else:
			data.char.position_offset.x = 0
		if data.env._sy == -1:
			var size: int = data.font.glyphs["A"].rect.size.y
			data.char.position_offset.y += (size + (size % 2) + (data.font.size % 2)) * scale
		else:
			data.char.position_offset.y += data.env._sy * scale
	if !data.char.is_newline:
		var glyph: IGlyph = data.font.glyphs[data.char.char]
		data.char.glyph.position.x = data.char.start_position.x + data.char.position_offset.x + (glyph.offset * scale)
		data.char.glyph.position.y = data.char.start_position.y + data.char.position_offset.y
		data.char.glyph.size.x = glyph.rect.size.x * scale
		data.char.glyph.size.y = glyph.rect.size.y * scale
		data.draw_glyph()
		if (data.env.last_newline || data.env.first_drawn_char) && data.char.char == "*":
			data.env.started_asterisk = true
		data.char.position_offset.x += ((data.env._sx if data.env._sx != -1 else glyph.shift) + glyph.offset) * scale
		data.env.last_newline = false
		data.env.first_drawn_char = false
