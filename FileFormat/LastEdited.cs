namespace DialogueHelper.FileFormat;

public class LastEdited()
{
    public long Timestamp = -1;
    public int AuthorId = -1;
    public string? LegacyAuthorName;

    public LastEdited(LastEdited toDuplicate) : this()
    {
        Timestamp = toDuplicate.Timestamp;
        AuthorId = toDuplicate.AuthorId;
        LegacyAuthorName = toDuplicate.LegacyAuthorName;
    }

    public LastEdited(string data) : this()
    {
        var sData = data.Split(",");
        if (int.TryParse(sData[0], out var num))
            AuthorId = num;
        else
            LegacyAuthorName = sData[0];
        Timestamp = long.Parse(sData[1]);
    }

    public string OutputString()
    {
        return $"{AuthorId},{Timestamp}";
    }
}
