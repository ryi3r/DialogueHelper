using System.Collections.Generic;
using System.Linq;
using System.Net;
using Lua;

namespace DialogueHelper.FileFormat;

[LuaObject]
public partial class StringContainer
{
    [LuaMember("id")]
    public int Id = -1;
    [LuaMember("text")]
    public string? Text;
    [LuaMember("original_text")]
    public string OriginalText = "";
    [LuaMember("last_edited")]
    public readonly LastEdited LastEdited = new();
    [LuaMember("box_style")]
    public int BoxStyle;
    [LuaMember("font_style")]
    public int FontStyle;
    public readonly Dictionary<string, string> CustomProperties = [];
    public readonly List<string> AuthorGroups = [];
    [LuaMember("mark_as_modified")]
    public bool MarkAsModified;

    public List<int> EqStrings = [];

    [LuaMember("set_custom_properties")]
    public void SetCustomProperties(string idx, string val) => CustomProperties[idx] = val;

    [LuaMember("get_custom_properties")]
    public string? GetCustomProperties(int idx) => idx - 1 > AuthorGroups.Count && idx > 0 ? AuthorGroups[idx - 1] : null;

    [LuaMember("size_custom_properties")]
    public int SizeCustomProperties() => CustomProperties.Count;

    [LuaMember("remove_custom_properties")]
    public void RemoveCustomProperties(string key) => CustomProperties.Remove(key);

    [LuaMember("keys_custom_properties")]
    public LuaTable KeysCustomProperties()
    {
        var t = new LuaTable();
        foreach (var (i, v) in CustomProperties.Keys.Index())
            t.Insert(i + 1, v);
        return t;
    }

    [LuaMember("has_custom_properties")]
    public void HasCustomProperties(string key) => CustomProperties.ContainsKey(key);

    [LuaMember("add_author_groups")]
    public void AddAuthorGroups(string val) => AuthorGroups.Add(val);

    [LuaMember("set_author_groups")]
    public void SetAuthorGroups(int idx, string val)
    {
        if (idx - 1 > AuthorGroups.Count && idx > 0)
            AuthorGroups[idx - 1] = val;
    }

    [LuaMember("get_author_groups")]
    public string? GetAuthorGroups(int idx) => idx - 1 > AuthorGroups.Count && idx > 0 ? AuthorGroups[idx - 1] : null;

    [LuaMember("size_author_groups")]
    public int SizeAuthorGroups() => AuthorGroups.Count;

    [LuaMember("remove_author_groups")]
    public void RemoveAuthorGroups(int idx)
    {
        if (idx - 1 > AuthorGroups.Count && idx > 0)
            AuthorGroups.RemoveAt(idx - 1);
    }

    [LuaMember("remove_eq_strings")]
    public void RemoveEqStrings(int idx)
    {
        if (idx - 1 > EqStrings.Count && idx > 0)
            EqStrings.RemoveAt(idx - 1);
    }

    [LuaMember("add_eq_strings")]
    public void AddEqStrings(int val) => EqStrings.Add(val);

    [LuaMember("set_eq_strings")]
    public void SetEqStrings(int idx, int val)
    {
        if (idx - 1 > EqStrings.Count && idx > 0)
            EqStrings[idx - 1] = val;
    }

    [LuaMember("get_eq_strings")]
    public LuaValue GetEqStrings(int idx) => idx - 1 > EqStrings.Count && idx > 0 ? EqStrings[idx - 1] : LuaValue.Nil;

    [LuaMember("size_eq_strings")]
    public int GetEqStringsEntry() => EqStrings.Count;

    [LuaMember("create_empty")]
    public static StringContainer CreateEmpty() => new();

    [LuaMember("create")]
    public static StringContainer Create(FormatEntry fEntry) => new(fEntry);

    public StringContainer() { }

    public StringContainer(FormatEntry fEntry)
    {
        if (fEntry.Data.TryGetValue("ID", out var id))
            Id = int.Parse(id);
        if (fEntry.Data.TryGetValue("OriginalText", out var originalText))
            OriginalText = originalText;
        else if (fEntry.Data.TryGetValue("OriginalContent", out originalText))
            OriginalText = originalText;
        if (fEntry.Data.TryGetValue("Text", out var text))
            Text = text;
        else if (fEntry.Data.TryGetValue("Content", out text))
            Text = text;
        if (fEntry.Data.TryGetValue("LastEdited", out var lastEdited))
            LastEdited = new(lastEdited);
        if (fEntry.Data.TryGetValue("Box", out var boxStyle))
            BoxStyle = int.Parse(boxStyle);
        if (fEntry.Data.TryGetValue("Font", out var fontStyle))
            FontStyle = int.Parse(fontStyle);
        if (fEntry.Data.TryGetValue("CustomProperties", out var prop))
        {
            foreach (var item in prop.Split("@@@"))
            {
                var entry = item.Split("@@");
                CustomProperties[WebUtility.UrlDecode(entry[0])] = WebUtility.UrlDecode(entry[1]);
            }
        }
        if (fEntry.Data.TryGetValue("AuthorGroups", out var authorGroups))
            AuthorGroups = [.. authorGroups.Split('@').Select(x => WebUtility.UrlDecode(x)!)];
        if (fEntry.Data.ContainsKey("MarkAsModified"))
            MarkAsModified = true;
    }

    [LuaMember("output_format_entry")]
    public FormatEntry OutputFormatEntry()
    {
        var fe = new FormatEntry()
        {
            Kind = 1,
        };
        if (Text != null && Text != OriginalText && Text.Length > 0)
            fe.Data["Text"] = Text;
        fe.Data["OriginalText"] = OriginalText;
        if (LastEdited.AuthorId != -1 || LastEdited.Timestamp != -1)
            fe.Data["LastEdited"] = LastEdited.OutputString();
        if (BoxStyle != 0)
            fe.Data["Box"] = BoxStyle.ToString();
        if (FontStyle != 0)
            fe.Data["Font"] = FontStyle.ToString();
        if (CustomProperties.Count > 0)
        {
            fe.Data["CustomProperties"] = string.Join("@@@", CustomProperties.Select(p =>
                $"{FormatEntry.GetSimpleUri(p.Key)}@@{FormatEntry.GetSimpleUri(p.Value)}"));
        }
        if (AuthorGroups.Count > 0)
            fe.Data["AuthorGroups"] = string.Join('@', AuthorGroups.Select(WebUtility.UrlEncode));
        if (MarkAsModified)
            fe.Data["MarkAsModified"] = "";
        return fe;
    }
}
