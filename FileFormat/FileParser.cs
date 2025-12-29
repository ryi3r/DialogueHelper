using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace DialogueHelper.FileFormat;

public static class FileParser
{
    public static List<FormatEntry> ParseString(string data)
    {
        return [.. data.Replace("\r", "").Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).Select(ParseLine)];
    }

    public static FormatEntry ParseLine(string line)
    {
        var index = 0;
        var lastEntry = 0;
        var gotName = false;
        var name = "";
        var fEntry = new FormatEntry();
        
        foreach (var chr in line)
        {
            if (chr == ':' && !gotName) // New entry
            {
                name = line[lastEntry..index];
                gotName = true;
                lastEntry = index + 1;
            }
            if (chr == ';' || index == line.Length - 1) // Update marker
            {
                if (!gotName)
                    fEntry.Kind = int.Parse(line[lastEntry..index]);
                else
                {
                    if (index == line.Length - 1 && chr != ';')
                        index++;
                    fEntry.Data.Add(name, WebUtility.UrlDecode(line[lastEntry..index]));
                    gotName = false;
                }
                lastEntry = index + 1;
            }
            index++;
        }

        return fEntry;
    }
}
