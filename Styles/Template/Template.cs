using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DialogueHelper.External;
using DialogueHelper.PreviewRenderer;
using DialogueHelper.Views;
using SixLabors.ImageSharp;

public class Template : StyleScript
{
    public override async Task Init(CustomToolWindowHandler wh)
    {
        // This is called when the script is loaded and ready to go.
        //await wh.ShowDialog(new InfoWindow("Hello world!"));
    }

    public override async Task<IEnumerable<CustomProperty>> RegisterCustomSettings()
    {
        // This may be called before Init, so be careful!
        // Store these somewhere if you're going to use them
        return [
            new("D" , "bool", typeof(bool)),
            new("E", "int", typeof(int)),
            new("F", "string", typeof(string)),
        ];
    }

    public override async Task<IEnumerable<CustomProperty>> RegisterCustomProperties()
    {
        // Store these somewhere if you're going to use them
        return [
            new("A" , "bool", typeof(bool)),
            new("B", "int", typeof(int)),
            new("C", "string", typeof(string)),
        ];
    }

    public override async Task<IEnumerable<CustomTool>> RegisterCustomTools()
    {
        return [new("Custom Tool", CustomToolTest), new("File Picker Example", CustomToolFileTest)];
    }

    public async Task CustomToolFileTest(CustomToolWindowHandler wh)
    {
        var f = await wh.GetStorageProvider().OpenFilePickerAsync(new()
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
        var w = new InfoWindow($"File(s) chosen: {string.Join(", ", f.Select(f => f.Name))}")
        {
            Title = "Example"
        };
        await wh.ShowDialog(w);
    }

    public async Task CustomToolTest(CustomToolWindowHandler wh)
    {
        var w = new InfoWindow("This is a tool test message.");
        await wh.ShowDialog(w);
    }

    public override async Task PrepareDraw(CustomToolWindowHandler wh, ExternalData data)
    {
        // This is always called before starting to draw glyphs, draw anything you want here!
    }

    public override async Task DrawGlyph(CustomToolWindowHandler wh, ExternalData data)
    {
        if (data.Char.IsIgnore || (!data.Font.GlyphDictionary.ContainsKey(data.Char.Char) && !data.Char.IsNewline))
            return;
        if (data.Char.IsNewline)
        {
            data.Char.PositionOffset.X = 0;
            var size = data.Font.GlyphDictionary['A'].Size.Y;
            data.Char.PositionOffset.Y += (int)((size + (size % 2.0f) + (data.Font.Size % 2.0f)) * data.Glyph.Scale.Y);
        }
        else
        {
            var glyph = data.Font.GlyphDictionary[data.Char.Char];
            data.Glyph.Position.X =
                (int)(data.Char.StartPosition.X + data.Char.PositionOffset.X + (glyph.Offset * data.Glyph.Scale.X));
            data.Glyph.Position.Y = data.Char.StartPosition.Y + data.Char.PositionOffset.Y;
            data.Glyph.Size = glyph.Size;
            data.DrawGlyph();
            data.Char.PositionOffset.X += (int)double.Ceiling((glyph.Shift + glyph.Offset) * data.Glyph.Scale.X);
        }
    }
}
