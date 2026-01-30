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
    /// <summary>
    /// <string, string>
    /// </summary>
    [LuaMember("custom_properties")]
    public readonly LuaDictionary CustomProperties = [];
    /// <summary>
    ///  string
    /// </summary>
    [LuaMember("author_groups")]
    public readonly LuaList AuthorGroups = [];
    [LuaMember("mark_as_modified")]
    public bool MarkAsModified;

    /// <summary>
    /// int
    /// </summary>
    [LuaMember("eq_strings")]
    public LuaList EqStrings = [];

    [LuaMember("create_empty")]
    public static StringContainer CreateEmpty() => new();

    [LuaMember("create")]
    public static StringContainer Create(FormatEntry fEntry) => new(fEntry);

    public StringContainer() { }

    public StringContainer(FormatEntry fEntry)
    {
        if (fEntry.Data.TryGetValue("ID", out var id))
            Id = int.Parse(id.Read<string>());
        if (fEntry.Data.TryGetValue("OriginalText", out var originalText))
            OriginalText = originalText.Read<string>();
        else if (fEntry.Data.TryGetValue("OriginalContent", out originalText))
            OriginalText = originalText.Read<string>();
        if (fEntry.Data.TryGetValue("Text", out var text))
            Text = text.Read<string>();
        else if (fEntry.Data.TryGetValue("Content", out text))
            Text = text.Read<string>();
        if (fEntry.Data.TryGetValue("LastEdited", out var lastEdited))
            LastEdited = new(lastEdited.Read<string>());
        if (fEntry.Data.TryGetValue("Box", out var boxStyle))
            BoxStyle = int.Parse(boxStyle.Read<string>());
        if (fEntry.Data.TryGetValue("Font", out var fontStyle))
            FontStyle = int.Parse(fontStyle.Read<string>());
        if (fEntry.Data.TryGetValue("CustomProperties", out var prop))
        {
            foreach (var item in prop.Read<string>().Split("@@@"))
            {
                var entry = item.Split("@@");
                CustomProperties[WebUtility.UrlDecode(entry[0])] = WebUtility.UrlDecode(entry[1]);
            }
        }
        if (fEntry.Data.TryGetValue("AuthorGroups", out var authorGroups))
            AuthorGroups = [.. authorGroups.Read<string>().Split('@').Select(x => WebUtility.UrlDecode(x)!)];
        if (fEntry.Data.ContainsKey("MarkAsModified"))
            MarkAsModified = true;
    }

    [LuaMember("output_format_entry")]
    public FormatEntry OutputFormatEntry()
    {
        var fe = new FormatEntry() { Kind = 1 };
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
            fe.Data["CustomProperties"] = string.Join(
                "@@@",
                CustomProperties.Select(p =>
                    $"{FormatEntry.GetSimpleUri(p.Key.Read<string>())}@@{FormatEntry.GetSimpleUri(p.Value.Read<string>())}"
                )
            );
        if (AuthorGroups.Count > 0)
            fe.Data["AuthorGroups"] = string.Join('@', AuthorGroups.Select(x => WebUtility.UrlEncode(x.Read<string>())));
        if (MarkAsModified)
            fe.Data["MarkAsModified"] = "";
        return fe;
    }
}
