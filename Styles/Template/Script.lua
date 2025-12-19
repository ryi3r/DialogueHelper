CustomProperty = {}
CustomTool = {}
InfoWindow = {}

function Init(wh)
    print("A")
    -- This is called when the script has been initialized and is ready to go
end

function RegisterCustomSettings()
    return {
        CustomProperty.create("D", "bool", "boolean"),
        CustomProperty.create("E", "int", "integer"),
        CustomProperty.create("F", "string", "string"),
    }
end

function RegisterCustomProperties()
    -- Store these somewhere if you're going to use them
    return {
        CustomProperty.create("A", "bool", "boolean"),
        CustomProperty.create("B", "int", "integer"),
        CustomProperty.create("C", "string", "string"),
    }
end

function RegisterCustomTools()
    return {
        CustomTool.create("Custom Tool", ImportFromDataWin),
        CustomTool.create("File Picker Example", ExportToDataWin),
    }
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

function ImportFromDataWin(wh)
    local w = InfoWindow.create("This is a tool test message.")
    wh:show_dialog(w)
end

function PrepareDraw(wh, data)
    -- This is always called befores tarting to draw glyphs, draw anything you want here!
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
