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
    public readonly Dictionary<string, string> Data = [];

    [LuaMember("get_data")]
    public string? GetData(string key) => Data.TryGetValue(key, out var v) ? v : null;

    [LuaMember("has_data")]
    public bool HasData(string key) => Data.ContainsKey(key);

    [LuaMember("size_data")]
    public int SizeData() => Data.Count;

    [LuaMember("keys_data")]
    public LuaTable KeysData()
    {
        var t = new LuaTable();
        foreach (var (i, v) in Data.Keys.Index())
            t.Insert(i + 1, v);
        return t;
    }

    [LuaMember("get_simple_uri")]
    public static string GetSimpleUri(string data) => new string[][] { ["%", "%25"], [";", "%3B"], [":", "%3A"], ["\n", "%0A"], ["\r", "%0D"], ["@", "%40"] }
            .Aggregate(data, (current, repl) => current.Replace(repl[0], repl[1]));

    [LuaMember("output_string")]
    public string OutputString()
    {
        var sb = new StringBuilder();
        sb.Append(Kind);
        if (Data.Count <= 0)
            sb.Append(';');
        foreach (var entry in Data)
        {
            sb.Append(';');
            sb.Append(entry.Key);
            sb.Append(':');
            sb.Append(GetSimpleUri(entry.Value));
        }
        return sb.ToString();
    }
}
