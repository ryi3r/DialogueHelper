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
    print("1")
    print(data.env)
    data.env.set("last_new_line", false)
    print("a")
    data.env.set("started_asterisk", false)
    print("b")
    data.env.set("e", 0)
    print("c")
    data.env.set("c", 0)
    print("d")
    data.env.set("f", 0)
    print("e")
    data.env.set("sx", -1)
    print("f")
    data.env.set("sy", -1)
    print("g")
    data.env.set("skip", 0)
    print("h")
    data.env.set("first_drawn_char", true)
    print("i")
    data.env.set("checked_index", -1)
    print("2")
    local lro = EnablePortrait.read_only
    print("3")
    EnablePortrait.read_only = true
    print("4")
    if data.box:has_custom_properties("SupportsPortrait") then
        print("5")
        EnablePortrait.read_only = data.box:get_custom_properties("SupportsPortrait") ~= true
        print("6")
    end
    print("7")
    if lro ~= EnablePortrait.read_only then
        print("8")
        EnablePortrait:update_ui_value()
        print("9")
    end
    print("10")
    if EnablePortrait.value == true and not EnablePortrait.read_only then
        print("11")
        local pos = data.box.get_custom_properties("PortraitOffset")
        print("12")
    end
    print("13")
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
