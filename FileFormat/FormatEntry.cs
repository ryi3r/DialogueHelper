using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lua;

namespace DialogueHelper.FileFormat;

[LuaObject]
public partial class FormatEntry
{
    [LuaMember("kind")]
    public int Kind = -1;
    /// <summary>
    /// <string, string>
    /// </summary>
    [LuaMember("data")]
    public readonly LuaTable Data = [];

    [LuaMember("get_simple_uri")]
    public static string GetSimpleUri(string data) => new string[][] { ["%", "%25"], [";", "%3B"], [":", "%3A"], ["\n", "%0A"], ["\r", "%0D"], ["@", "%40"] }
            .Aggregate(data, (current, repl) => current.Replace(repl[0], repl[1]));

    [LuaMember("output_string")]
    public string OutputString()
    {
        var sb = new StringBuilder();
        sb.Append(Kind);
        if (Data.ArrayLength <= 0)
            sb.Append(';');
        foreach (var entry in Data)
        {
            sb.Append(';');
            sb.Append(entry.Key);
            sb.Append(':');
            sb.Append(GetSimpleUri(entry.Value.Read<string>()));
        }
        return sb.ToString();
    }
}
