using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DialogueHelper.Views;

namespace DialogueHelper.FileFormat;

public class FileData
{
    public class StringData(string key, List<StringContainer> data)
    {
        public readonly string Key = key;
        // no setter in the api so readonly
        public readonly List<StringContainer> Data = data;
    }

    public string? ThemeName;
    public string? FileVersion;
    public int LastStringId;
    public readonly List<string> AuthorList = [];

    readonly List<StringData> Strings = [];
    readonly Dictionary<string, int> StringIndexes = [];

    /// <summary>
    /// returns array index, note that if the key already exists it overwrites it
    /// </summary>
    public int AddString(string key, List<StringContainer> value)
    {
        if (!StringIndexes.TryGetValue(key, out var idx))
            idx = Strings.Count;
        InsertString(idx, key, value);
        return idx;
    }

    public void InsertString(int index, string key, List<StringContainer> value)
    {
        index = int.Min(index, Strings.Count);
        var sd = new StringData(key, value);
        if (StringIndexes.TryGetValue(key, out var idx))
        {
            RemoveAtString(idx);
            Debug.Assert(!StringIndexes.ContainsKey(key));
        }
        if (index == Strings.Count)
            Strings.Add(sd);
        else
            Strings.Insert(index, sd);
        StringIndexes.Add(key, index);
    }

    /// <summary>
    /// returns true if it removed the entry
    /// </summary>
    public bool RemoveString(string key) => StringIndexes.TryGetValue(key, out var idx) && RemoveAtString(idx);

    /// <summary>
    /// returns null if the requested string is not there
    /// </summary>
    public List<StringContainer>? GetString(string key) => StringIndexes.TryGetValue(key, out var idx) ? GetAtString(idx) : null;

    public string? GetAtStringKey(int index)
    {
        if (index < 0 || index >= Strings.Count)
            return null;
        return Strings[index].Key;
    }

    public List<StringContainer>? GetAtString(int index)
    {
        if (index < 0 || index >= Strings.Count)
            return null;
        return Strings[index].Data;
    }

    public (string Key, List<StringContainer> Value)? GetAtStringKeyValue(int index)
    {
        if (index < 0 || index >= Strings.Count)
            return null;
        var e = Strings[index];
        return (e.Key, e.Data);
    }

    public IEnumerable<string> GetStringKeys()
    {
        foreach (var s in Strings)
            yield return s.Key;
    }

    public IEnumerable<List<StringContainer>> GetStringValues()
    {
        foreach (var s in Strings)
            yield return s.Data;
    }

    public IEnumerable<KeyValuePair<string, List<StringContainer>>> GetStringKeysValues()
    {
        foreach (var s in Strings)
            yield return new(s.Key, s.Data);
    }

    public int GetCountString() => Strings.Count;
    public int GetAllCountString()
    {
        var i = 0;
        foreach (var s in Strings)
            i += s.Data.Count;
        return i;
    }

    public bool RemoveAtString(int index)
    {
        if (index < 0 || index >= Strings.Count)
            return false;
        StringIndexes.Remove(Strings[index].Key);
        Strings.RemoveAt(index);
        // we need to update all indexes from this point on
        Debug.Assert(Strings.Count == StringIndexes.Count);
        while (index < Strings.Count)
        {
            StringIndexes[Strings[index].Key] = index;
            index++;
        }
        return true;
    }

    public int GetIndexString(string key) => StringIndexes.TryGetValue(key, out var idx) ? idx : -1;

    public bool ContainsKeyString(string key) => StringIndexes.ContainsKey(key);

    public bool TryGetStringValue(string key, [NotNullWhen(true)] out List<StringContainer>? val)
    {
        var r = GetString(key);
        val = r;
        return r is not null;
    }

    public IEnumerable<StringContainer> IterAllStrings()
    {
        foreach (var s in Strings)
        {
            foreach (var v in s.Data)
                yield return v;
        }
    }

    public static FileData LoadString(List<FormatEntry> fileEntries, LoadingWindow? lWin = null)
    {
        var data = new FileData();
        lWin?.ProgressBar.Maximum = fileEntries.Count;

        var loaded = 0;
        var currentEntry = "";
        var entries = new List<StringContainer>();
        var isEntry = false;

        foreach (var entry in fileEntries)
        {
            // 8 == File end
            if (entry.Kind is 0 or 8 && isEntry)
            {
                data.AddString(currentEntry, entries);
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
                        entries.Add(sCont);
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
            fel.AddRange(entry.Data.Select(str => str.OutputFormatEntry()));
        }

        fel.AddRange(AuthorList.Select(author => new FormatEntry { Kind = 2, Data = { [""] = author } }));
        fel.Add(new() { Kind = 8 });
        return string.Join('\n', fel.Select(x => x.OutputString()));
    }
}
