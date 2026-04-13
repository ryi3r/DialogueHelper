using System.Collections.Generic;
using System.Linq;
using DialogueHelper.Views;

namespace DialogueHelper.FileFormat;

public class FileData
{
    public string? ThemeName;
    public string? FileVersion;
    public readonly Dictionary<string, Dictionary<int, StringContainer>> Strings = [];
    public readonly Dictionary<int, StringContainer> StringIds = [];
    public int LastStringId;
    public readonly List<string> AuthorList = [];

    public static FileData LoadString(List<FormatEntry> fileEntries, LoadingWindow? lWin = null)
    {
        var data = new FileData();
        lWin?.ProgressBar.Maximum = fileEntries.Count;

        var loaded = 0;
        var currentEntry = "";
        var entries = new Dictionary<int, StringContainer>();
        var isEntry = false;

        foreach (var entry in fileEntries)
        {
            // 8 == File end
            if (entry.Kind is 0 or 8 && isEntry)
            {
                data.Strings[currentEntry] = entries;
                entries = [];
            }

            switch (entry.Kind)
            {
                case 9: // Settings
                    if (entry.Data.TryGetValue("Style", out var themeName))
                        data.ThemeName = themeName;
                    if (entry.Data.TryGetValue("Version", out var ver))
                        data.FileVersion = ver;
                    break;
                case 0: // New entry
                    currentEntry = entry.Data["ID"];
                    isEntry = true;
                    break;
                case 1: // Add a string to the current entry
                    {
                        var id = data.LastStringId++;
                        entry.Data["ID"] = id.ToString();
                        var sCont = new StringContainer(entry);
                        entries.Add(id, sCont);
                        data.StringIds.Add(id, sCont);
                        if (sCont.LastEdited.LegacyAuthorName is not null)
                        {
                            if (!data.AuthorList.Contains(sCont.LastEdited.LegacyAuthorName))
                                data.AuthorList.Add(sCont.LastEdited.LegacyAuthorName);
                            sCont.LastEdited.AuthorId = data.AuthorList.IndexOf(sCont.LastEdited.LegacyAuthorName);
                            sCont.LastEdited.LegacyAuthorName = null;
                        }
                    }
                    break;
                case 2: // Add author
                    data.AuthorList.Add(entry.Data[""]);
                    break;
            }
            lWin?.ProgressBar.Value = loaded++;
        }

        return data;
    }

    public string OutputString()
    {
        var fel = new List<FormatEntry>();
        var fe = new FormatEntry { Kind = 9, Data = { ["Version"] = "1" } };
        if (ThemeName is not null)
            fe.Data["Style"] = ThemeName;
        fel.Add(fe);
        foreach (var entry in Strings)
        {
            fel.Add(new() { Kind = 0, Data = { ["ID"] = entry.Key } });
            fel.AddRange(entry.Value.Select(str => str.Value.OutputFormatEntry()));
        }

        fel.AddRange(AuthorList.Select(author => new FormatEntry { Kind = 2, Data = { [""] = author } }));
        fel.Add(new() { Kind = 8 });
        return string.Join('\n', fel.Select(x => x.OutputString()));
    }
}
