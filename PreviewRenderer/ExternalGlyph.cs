using Lua;
using SixLabors.ImageSharp;

namespace DialogueHelper.PreviewRenderer;

[LuaObject]
public partial class ExternalGlyph
{
    [LuaMember("scale")]
    public LuaDoubleTuple Scale = new(1.0, 1.0);
    [LuaMember("color")]
    public LuaColor Color = LuaColor.White;
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    // ReSharper disable once ConvertToConstant.Global
    [LuaMember("alpha")]
    public double Alpha = 1.0;
    [LuaMember("position")]
    public LuaDoubleTuple Position = new(0.0f, 0.0f);
    [LuaMember("size")]
    public LuaDoubleTuple Size = new(0.0, 0.0);
}
