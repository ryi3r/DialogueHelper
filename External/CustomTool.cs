using System;
using System.Threading.Tasks;
using Lua;

namespace DialogueHelper.External;

[LuaObject]
public partial class CustomTool(string name, LuaValue luaFunc)
{
    [LuaMember("name")]
    public string Name = name;
    [LuaMember("func")]
    public LuaValue LuaFunc = luaFunc;

    [LuaMember("create")]
    public static CustomTool Create(string name, LuaValue func) => new(name, func);
}
