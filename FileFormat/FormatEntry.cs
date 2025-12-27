using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DialogueHelper.FileFormat;

public partial class FormatEntry
{
    public int Kind = -1;
    public readonly Dictionary<string, string> Data = [];

    public static string GetSimpleUri(string data) => new string[][] { ["%", "%25"], [";", "%3B"], [":", "%3A"], ["\n", "%0A"], ["\r", "%0D"], ["@", "%40"] }
            .Aggregate(data, (current, repl) => current.Replace(repl[0], repl[1]));

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
