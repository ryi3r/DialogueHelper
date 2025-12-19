using Lua;

namespace DialogueHelper.FileFormat;

[LuaObject]
public partial class LastEdited
{
    [LuaMember("timestamp")]
    public long Timestamp = -1;
    [LuaMember("author_id")]
    public int AuthorId = -1;
    [LuaMember("legacy_author_name")]
    public string? LegacyAuthorName;

    public LastEdited() { }

    public LastEdited(string data)
    {
        var sData = data.Split(",");
        if (int.TryParse(sData[0], out var num))
            AuthorId = num;
        else
            LegacyAuthorName = sData[0];
        Timestamp = long.Parse(sData[1]);
    }

    [LuaMember("create_empty")]
    public static LastEdited CreateEmpty() => new();

    [LuaMember("create")]
    public static LastEdited Create(string data) => new(data);

    [LuaMember("output_string")]
    public string OutputString() => $"{AuthorId},{Timestamp}";
}
