using Lua;

namespace DialogueHelper.PreviewRenderer;

[LuaObject]
public partial class ExternalChar
{
    [LuaMember("char")]
    public required string Char;
    [LuaMember("index")]
    public required int Index;
    [LuaMember("string")]
    public required string String;

    [LuaMember("start_position")]
    public LuaDoubleTuple StartPosition = new(0.0, 0.0);
    [LuaMember("position_offset")]
    public LuaDoubleTuple PositionOffset = new(0.0, 0.0);

    [LuaMember("is_ignore")]
    public required bool IsIgnore;
    [LuaMember("is_newline")]
    public required bool IsNewline;
}
