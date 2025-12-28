using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using DialogueHelper.External;
using DialogueHelper.PreviewRenderer;
using DialogueHelper.Views;
using SixLabors.ImageSharp;

public static async Task Init(CustomToolWindowHandler wH)
{
    // This is called when the script is loaded and ready to go.
    //await wH.ShowDialog(new InfoWindow("Hello world!"));
}

public static async Task<IEnumerable<CustomProperty>> RegisterCustomSettings()
{
    // This may be called before Init, so be careful!
    // Store these somewhere if you're going to use them
    return [
        new("D" , "bool", typeof(bool)),
        new("E", "int", typeof(int)),
        new("F", "string", typeof(string)),
    ];
}

public static async Task<IEnumerable<CustomProperty>> RegisterCustomProperties()
{
    // Store these somewhere if you're going to use them
    return [
        new("A" , "bool", typeof(bool)),
        new("B", "int", typeof(int)),
        new("C", "string", typeof(string)),
    ];
}

public static async Task<IEnumerable<CustomTool>> RegisterCustomTools()
{
    return [new("Custom Tool", CustomToolTest), new("File Picker Example", CustomToolFileTest)];
}

public static async Task CustomToolFileTest(CustomToolWindowHandler wH)
{
    var f = await wH.GetStorageProvider().OpenFilePickerAsync(new()
    {
        Title = "Example",
        AllowMultiple = true,
        FileTypeFilter = [new("Json file")
        {
            Patterns = ["*.json"],
        }, new("All files")
        {
            Patterns = ["*"],
        }]
    });
    var w = new InfoWindow($"File(s) chosen: {string.Join(", ", f.Select(f => f.Name))}");
    w.Title = "Example";
    await wH.ShowDialog(w);
}

public static async Task CustomToolTest(CustomToolWindowHandler wH)
{
    var w = new InfoWindow("This is a tool test message.");
    await wH.ShowDialog(w);
}

public static async Task PrepareDraw(CustomToolWindowHandler wH, ExternalData data)
{
    // This is always called before starting to draw glyphs, draw anything you want here!
}

public static async Task DrawGlyph(CustomToolWindowHandler wH, ExternalData data)
{
    if (data.Char.IsIgnore || (!data.Font.GlyphDictionary.ContainsKey(data.Char.Char) && !data.Char.IsNewline))
        return;
    if (data.Char.IsNewline)
    {
        data.Char.PositionOffset.Item1 = 0;
        var size = data.Font.GlyphDictionary['A'].Size.Item2;
        data.Char.PositionOffset.Item2 += (int)((size + (size % 2.0f) + (data.Font.Size % 2.0f)) * data.Glyph.Scale.Item2);
    }
    else
    {
        var glyph = data.Font.GlyphDictionary[data.Char.Char];
        data.Glyph.Position.Item1 =
            (int)(data.Char.StartPosition.Item1 + data.Char.PositionOffset.Item1 + (glyph.Offset * data.Glyph.Scale.Item1));
        data.Glyph.Position.Item2 = data.Char.StartPosition.Item2 + data.Char.PositionOffset.Item2;
        data.Glyph.Size = glyph.Size;
        data.DrawGlyph();
        data.Char.PositionOffset.Item1 += (int)Math.Ceiling((glyph.Shift + glyph.Offset) * data.Glyph.Scale.Item1);
    }
}
