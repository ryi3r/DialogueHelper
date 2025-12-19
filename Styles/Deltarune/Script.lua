local json = require("lunajson")

CustomProperty = {}
CustomTool = {}
InfoWindow = {}

function Init(wh)
    -- This is called when the script has been initialized and is ready to go
    Fonts = {
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
    }
    EnablePortrait = CustomProperty.create("Enable Portrait", "enablePortrait", "boolean", false)
end

function RegisterCustomProperties()
    -- Store these somewhere if you're going to use them
    return {
        EnablePortrait,
    }
end

function RegisterCustomTools()
    return {
        CustomTool.create("Import from data.win", ImportFromDataWin),
        CustomTool.create("Export to data.win", ExportToDataWin),
    }
end

function ImportFromDataWin(wh)
    if wh:get_loaded_file_data() ~= nil then
        local w = InfoWindow.create("Please close the currently open file to import from the data.win.")
        wh:show_dialog(w)
        return
    end
    local f = wh:open_file_picker("Select a file", { "GameMaker Data File|data.win", "All files|*.*;*" })
    if #f == 0 then
        return
    end
    local of = io.open(string.format("%s/output.json", wh:get_style_path()))
    if of ~= nil then
        of:close()
        os.remove(string.format("%s/output.json", wh:get_style_path()))
    end
    print(f[0].path)
    local exitStatus = os.execute(string.format("%s/UndertaleModCli/UndertaleModCli load %s -s ../ExportStringsJson.csx",
        wh:get_style_path(), f[0].path))
    print(exitStatus)
    of = io.open(string.format("%s/output.json", wh:get_style_path()), "r")
    if of ~= nil then
        local jsonStr = of:read("a")
        of:close()
        local out = json.decode(jsonStr)
        -- todo
    end
end

function ExportToDataWin(wh)
    local f = wh:open_file_picker("Example", true, { "Json file|*.json", "All files|*.*;*" })
    local w = InfoWindow.create(string.format("File(s) chosen: %s", table.concat((function()
        local t = {}
        for i, p in ipairs(f) do
            t[i] = p.name
        end
        return t
    end)(), ", ")))
    wh:show_dialog(w)
end

function PrepareDraw(wh, data)
    -- This is always called befores tarting to draw glyphs, draw anything you want here!
    data.env.last_new_line = false
    data.env.started_asterisk = false
    data.env.e = 0
    data.env.c = 0
    data.env.f = 0
    data.env.sx = -1
    data.env.sy = -1
    data.env.skip = 0
    data.env.first_drawn_char = true
    data.env.checked_index = -1
    local lro = EnablePortrait.read_only
    EnablePortrait.read_only = true
    if data.box:has_custom_properties("SupportsPortrait") then
        EnablePortrait.read_only = data.box:get_custom_properties("SupportsPortrait") ~= true
    end
    if lro ~= EnablePortrait.read_only then
        EnablePortrait:update_ui_value()
    end
    if EnablePortrait.value == true and not EnablePortrait.read_only then
        local pos = data.box.get_custom_properties("PortraitOffset")
    end
end

local function round(n)
    return math.floor((math.floor(n * 2) + 1) / 2)
end

function DrawGlyph(wh, data)
    if data.char.is_ignore or (not data.font:has_glyph_dictionary(data.char.char) and not data.char.is_newline) then
        return
    end
    if data.char.is_newline then
        data.char.position_offset.first = 0
        local size = data.font:get_glyph_dictionary("A").size.last;
        data.char.position_offset.last = data.char.position_offset.last
            + round((size + (size % 2) + (data.font.size % 2)) * data.glyph.scale.last)
    else
        local glyph = data.font:get_glyph_dictionary(data.char.char)
        data.glyph.position.first = round(data.char.start_position.first + data.char.position_offset.first +
            (glyph.offset * data.glyph.scale.first))
        data.glyph.position.last = data.char.start_position.last + data.char.position_offset.last;
        data.glyph.size = glyph.size
        data:draw_glyph()
        data.char.position_offset.first = data.char.position_offset.first +
            math.ceil((glyph.shift + glyph.offset) * data.glyph.scale.first)
    end
end
