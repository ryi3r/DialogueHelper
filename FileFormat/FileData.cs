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
    public readonly Dictionary<string, Dictionary<int, StringContainer>> Strings = [];
    public readonly Dictionary<int, StringContainer> StringIds = [];
    [LuaMember("last_string_id")]
    public int LastStringId;
    public readonly List<string> AuthorList = [];

    [LuaMember("get_strings")]
    public StringContainer? GetStrings(string keyA, int keyB) => Strings.TryGetValue(keyA, out var a) && a.TryGetValue(keyB - 1, out var b) ? b : null;
    
    [LuaMember("size_strings")]
    public int SizeStrings() => Strings.Count;

    [LuaMember("keys_strings")]
    public LuaTable KeysStrings()
    {
        var t = new LuaTable();
        foreach (var (i, k) in Strings.Keys.Index())
            t.Insert(i + 1, k);
        return t;
    }

    [LuaMember("has_strings")]
    public bool HasStrings(string key) => Strings.ContainsKey(key);

    [LuaMember("size_strings_sub")]
    public LuaValue SizeStringsSub(string key) => Strings.TryGetValue(key, out var v) ? v.Count : LuaValue.Nil;

    [LuaMember("keys_strings_sub")]
    public LuaTable? KeysStringsSub(string key)
    {
        if (Strings.TryGetValue(key, out var v))
        {
            var t = new LuaTable();
            foreach (var (i, k) in v.Keys.Index())
                t.Insert(i + 1, k);
            return t;
        }
        return null;
    }

    [LuaMember("has_strings_sub")]
    public bool HasStringsSub(string keyA, int keyB) => Strings.TryGetValue(keyA, out var v) && v.ContainsKey(keyB);

    [LuaMember("get_string_ids")]
    public StringContainer? GetStringIds(int key) => StringIds.TryGetValue(key - 1, out var v) ? v : null;

    [LuaMember("size_string_ids")]
    public int SizeStringIds() => StringIds.Count;

    [LuaMember("keys_string_ids")]
    public LuaTable KeysStringIds()
    {
        var t = new LuaTable();
        foreach (var (i, k) in StringIds.Keys.Index())
            t.Insert(i + 1, k);
        return t;
    }

    [LuaMember("has_string_ids")]
    public bool HasStringIds(int key) => StringIds.ContainsKey(key);

    [LuaMember("get_author_list")]
    public string? GetAuthorList(int key) => key - 1 < AuthorList.Count && key > 0 ? AuthorList[key - 1] : null;
    
    [LuaMember("size_author_list")]
    public int SizeAuthorList() => AuthorList.Count;

    [LuaMember("load_string")]
    public static FileData LoadString(List<FormatEntry> fileEntries, LoadingWindow? lWin = null)
    {
        var fData = new FileData();
        if (lWin != null)
            lWin.ProgressBar.Maximum = fileEntries.Count;

        var loaded = 0;
        var currentEntry = "";
        var entries = new Dictionary<int, StringContainer>();
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
                        fData.ThemeName = themeName;
                    if (entry.Data.TryGetValue("Version", out var ver))
                        fData.FileVersion = ver;
                    break;
                case 0: // New entry
                    currentEntry = entry.Data["ID"];
                    isEntry = true;
                    break;
                case 1: // Add a string to the current entry
                    {
                        var id = fData.LastStringId++;
                        entry.Data["ID"] = id.ToString();
                        var sCont = new StringContainer(entry);
                        entries.Add(id, sCont);
                        fData.StringIds.Add(id, sCont);
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
            fel.AddRange(entry.Value.Select(str => str.Value.OutputFormatEntry()));
        }

        fel.AddRange(AuthorList.Select(author => new FormatEntry() { Kind = 2, Data = { [""] = author, } }));
        fel.Add(new()
        {
            Kind = 8,
        });
        return string.Join('\n', fel.Select(f => f.OutputString()));
    }
}
