using System;
using System.Collections.Generic;
using System.Linq;
using DialogueHelper.Views;
using Lua;

namespace DialogueHelper.FileFormat;

[LuaObject]
public partial class FileData
{
    [LuaMember("theme_name")]
    public string? ThemeName;
    [LuaMember("file_version")]
    public string? FileVersion;
    /// <summary>
    /// <string, LuaTable<int, StringContainer>>
    /// </summary>
    [LuaMember("strings")]
    public readonly LuaTable Strings = [];
    /// <summary>
    /// <int, StringContainer>
    /// </summary>
    [LuaMember("string_ids")]
    public readonly LuaTable StringIds = [];
    [LuaMember("last_string_id")]
    public int LastStringId;
    /// <summary>
    /// string
    /// </summary>
    [LuaMember("author_list")]
    public readonly LuaList AuthorList = [];

    [LuaMember("load_string")]
    public static FileData LoadString(List<FormatEntry> fileEntries, LoadingWindow? lWin = null)
    {
        var fData = new FileData();
        if (lWin != null)
            lWin.ProgressBar.Maximum = fileEntries.Count;

        var loaded = 0;
        var currentEntry = "";
        // <int, StringContainer>
        var entries = new LuaTable();
        var isEntry = false;

        foreach (var entry in fileEntries)
        {
            // 8 == File end
            if (entry.Kind is 0 or 8 && isEntry)
            {
                fData.Strings[currentEntry] = entries;
                entries = [];
            }

            switch (entry.Kind)
            {
                case 9: // Settings
                    if (entry.Data.TryGetValue("Style", out var themeName))
                        fData.ThemeName = themeName.Read<string>();
                    if (entry.Data.TryGetValue("Version", out var ver))
                        fData.FileVersion = ver.Read<string>();
                    break;
                case 0: // New entry
                    currentEntry = entry.Data["ID"].Read<string>();
                    isEntry = true;
                    break;
                case 1: // Add a string to the current entry
                    {
                        var id = fData.LastStringId++;
                        entry.Data["ID"] = id.ToString();
                        var sCont = new StringContainer(entry);
                        entries[id] = sCont;
                        fData.StringIds[id] = sCont;
                        if (sCont.LastEdited.LegacyAuthorName != null)
                        {
                            if (!fData.AuthorList.Contains(sCont.LastEdited.LegacyAuthorName))
                                fData.AuthorList.Add(sCont.LastEdited.LegacyAuthorName);
                            sCont.LastEdited.AuthorId = fData.AuthorList.IndexOf(sCont.LastEdited.LegacyAuthorName);
                            sCont.LastEdited.LegacyAuthorName = null;
                        }
                    }
                    break;
                case 2: // Add author
                    fData.AuthorList.Add(entry.Data[""]);
                    break;
            }
            if (lWin != null)
                lWin.ProgressBar.Value = loaded++;
        }

        return fData;
    }

    [LuaMember("output_string")]
    public string OutputString()
    {
        var fel = new List<FormatEntry>();
        var fe = new FormatEntry()
        {
            Kind = 9,
            Data =
            {
                ["Version"] = "1",
            },
        };
        if (ThemeName != null)
            fe.Data["Style"] = ThemeName;
        fel.Add(fe);
        foreach (var entry in Strings)
        {
            fel.Add(new()
            {
                Kind = 0,
                Data =
                {
                    ["ID"] = entry.Key,
                },
            });
            fel.AddRange(entry.Value.Read<LuaTable>().Select(str => str.Value.Read<StringContainer>().OutputFormatEntry()));
        }

        fel.AddRange(AuthorList.Select(author => new FormatEntry() { Kind = 2, Data = { [""] = author.Read<string>(), } }));
        fel.Add(new()
        {
            Kind = 8,
        });
        return string.Join('\n', fel.Select(f => f.OutputString()));
    }
}
