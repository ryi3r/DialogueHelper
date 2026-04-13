using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace DialogueHelper.FileFormat;

public class StringContainer
{
    public int Id = -1;
    public string? Text;
    public string OriginalText = "";
    public string? Key
    {
        get;
        set => field = value is not null && value.Length == 0 ? null : value;
    }
    public readonly LastEdited LastEdited = new();
    public int BoxStyle;
    public int FontStyle;
    public readonly Dictionary<string, string> CustomProperties = [];
    public readonly List<string> AuthorGroups = [];
    public bool MarkAsModified;

    public List<int> EqStrings = [];

    public StringContainer() { }

    public StringContainer(FormatEntry fEntry)
    {
        if (fEntry.Data.TryGetValue("ID", out var id))
            Id = int.Parse(id);
        if (fEntry.Data.TryGetValue("Key", out var key))
            Key = key;
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

    public FormatEntry OutputFormatEntry()
    {
        var fe = new FormatEntry() { Kind = 1 };
        if (Text is not null && Text != OriginalText && Text.Length > 0)
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
                    $"{FormatEntry.GetSimpleUri(p.Key)}@@{FormatEntry.GetSimpleUri(p.Value)}"
                )
            );
        if (AuthorGroups.Count > 0)
            fe.Data["AuthorGroups"] = string.Join('@', AuthorGroups.Select(WebUtility.UrlEncode));
        if (MarkAsModified)
            fe.Data["MarkAsModified"] = "";
        if (Key is not null)
            fe.Data["Key"] = Key;
        return fe;
    }
}
