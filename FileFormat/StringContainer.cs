using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace DialogueHelper.FileFormat;

public class StringContainer()
{
    public int Id = -1;
    public string? Text;
    public string OriginalText = "";
    public string? Comment;
    public string? Key
    {
        get;
        set => field = value is not null && value.Length == 0 ? null : value;
    }
    public readonly LastEdited LastEdited = new();

    public int BoxStyle;
    public float BoxScale = 1;
    public int FontStyle;
    public float FontScale = 1;
    public float PreviewScale = 1;
    public Color BoxColor = new() { Rgba = 0xffffffff };
    public Color TextColor = new() { Rgba = 0xffffffff };

    public readonly Dictionary<string, string> CustomProperties = [];
    public readonly List<string> AuthorGroups = [];

    public bool MarkAsModified;
    public List<StringContainer> EqStrings = [];

    public StringContainer(FormatEntry fEntry) : this()
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
        if (fEntry.Data.TryGetValue("BoxScale", out var boxScale))
            BoxScale = float.Parse(boxScale);
        if (fEntry.Data.TryGetValue("Font", out var fontStyle))
            FontStyle = int.Parse(fontStyle);
        if (fEntry.Data.TryGetValue("FontScale", out var fontScale))
            FontScale = float.Parse(fontScale);
        if (fEntry.Data.TryGetValue("PreviewScale", out var previewScale))
            FontScale = float.Parse(previewScale);
        if (fEntry.Data.TryGetValue("BoxColor", out var boxColor))
            BoxColor = new() { Rgba = uint.Parse(boxColor) };
        if (fEntry.Data.TryGetValue("TextColor", out var textColor))
            TextColor = new() { Rgba = uint.Parse(textColor) };
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

        if (fEntry.Data.TryGetValue("Comment", out var comment))
            Comment = comment;
    }

    public FormatEntry OutputFormatEntry()
    {
        var fe = new FormatEntry { Kind = 1 };
        if (Text is not null && Text != OriginalText && Text.Length > 0)
            fe.Data["Text"] = Text;
        fe.Data["OriginalText"] = OriginalText;
        if (LastEdited.AuthorId != -1 || LastEdited.Timestamp != -1)
            fe.Data["LastEdited"] = LastEdited.OutputString();
        if (BoxStyle != 0)
            fe.Data["Box"] = BoxStyle.ToString();
        if (BoxScale != 1)
            fe.Data["BoxScale"] = BoxScale.ToString();
        if (FontStyle != 0)
            fe.Data["Font"] = FontStyle.ToString();
        if (FontScale != 1)
            fe.Data["FontScale"] = FontScale.ToString();
        if (PreviewScale != 1)
            fe.Data["PreviewScale"] = PreviewScale.ToString();
        if (BoxColor.Rgba != 0xffffffff)
            fe.Data["BoxColor"] = BoxColor.Rgba.ToString();
        if (TextColor.Rgba != 0xffffffff)
            fe.Data["TextColor"] = TextColor.Rgba.ToString();
        if (CustomProperties.Count > 0)
        {
            fe.Data["CustomProperties"] = string.Join(
                "@@@",
                CustomProperties.Select(p =>
                    $"{FormatEntry.GetSimpleUri(p.Key)}@@{FormatEntry.GetSimpleUri(p.Value)}"
                )
            );
        }
        if (AuthorGroups.Count > 0)
            fe.Data["AuthorGroups"] = string.Join('@', AuthorGroups.Select(WebUtility.UrlEncode));
        if (MarkAsModified)
            fe.Data["MarkAsModified"] = "";
        if (Comment is not null && !string.IsNullOrWhiteSpace(Comment))
            fe.Data["Comment"] = Comment;
        if (Key is not null)
            fe.Data["Key"] = Key;
        return fe;
    }
}
