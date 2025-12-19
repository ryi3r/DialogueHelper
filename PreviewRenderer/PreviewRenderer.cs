using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DialogueHelper.External;
using DialogueHelper.StylesParser;
using DialogueHelper.Views;
using Lua;

namespace DialogueHelper.PreviewRenderer;

public class PreviewRenderer(MainWindow mainWindow)
{
    public readonly MainWindow MainWindow = mainWindow;

    public async void CreateRender(StyleParser style, int selectedBox, int selectedFont, float boxScale, float fontScale, float previewScale, string str, Canvas canvas)
    {
        canvas.Children.Clear();
        // ensure always that selected box/font is in-bounds
        if (style.FontMetadata.Count == 0 || style.BoxMetadata.Count == 0)
            return;
        if (selectedBox < 0)
            selectedBox = 0;
        if (selectedFont < 0)
            selectedFont = 0;
        if (selectedBox >= style.BoxMetadata.Count)
            selectedBox = style.BoxMetadata.Count - 1;
        if (selectedBox >= style.FontMetadata.Count)
            selectedFont = style.FontMetadata.Count - 1;
        var box = style.BoxMetadata[selectedBox];
        var font = style.FontMetadata[selectedFont];

        canvas.Width = 0.0;
        canvas.Height = 0.0;

        foreach (var bImg in box.Images)
        {
            var imgAsset = style.ImageAssets[bImg.Path];
            var avImg = new Image()
            {
                Source = imgAsset,
                Width = imgAsset.Size.Width * boxScale * previewScale,
                Height = imgAsset.Size.Height * boxScale * previewScale,
            };
            RenderOptions.SetBitmapInterpolationMode(avImg, BitmapInterpolationMode.None);
            avImg.SetValue(Canvas.LeftProperty, bImg.Position[0]);
            avImg.SetValue(Canvas.TopProperty, bImg.Position[1]);
            canvas.Children.Add(avImg);

            if (bImg.Position[0] + imgAsset.Size.Width > canvas.Width)
                canvas.Width = bImg.Position[0] + imgAsset.Size.Width;
            if (bImg.Position[1] + imgAsset.Size.Height > canvas.Height)
                canvas.Height = bImg.Position[1] + imgAsset.Size.Height;
        }
        var eData = new ExternalData(MainWindow, canvas, style.ImageAssets[font.ImagePath])
        {
            GlobalEnv = style.GlobalEnv,
            Box = box,
            Char = new()
            {
                Char = "A",
                Index = 0,
                String = str,

                IsNewline = false,
                IsIgnore = false,

                StartPosition = new((int)box.TextOffset[0], (int)box.TextOffset[1]),
            },
            Env = [],
            Font = font,
            Glyph = new()
            {
                Scale = new(fontScale * previewScale, fontScale * previewScale),
            },
            Style = style,
        };
        try
        {
            var wh = new CustomToolWindowHandler(MainWindow, style);
            {
                if (style.LuaState.Environment.TryGetValue("PrepareDraw", out var f))
                    await style.LuaState.CallAsync(f, [wh, eData]);
            }
            eData.Char.String = str;
            var i = 0;
            foreach (var chr in str)
            {
                eData.Char.Char = chr.ToString();
                eData.Char.Index = i++;
                eData.Char.IsNewline = style.Metadata.NewLines.Contains(chr);
                eData.Char.IsIgnore = style.Metadata.Ignore.Contains(chr);
                eData.Glyph = new()
                {
                    Scale = new(fontScale * previewScale, fontScale * previewScale),
                };

                {
                    if (style.LuaState.Environment.TryGetValue("DrawGlyph", out var f))
                        await style.LuaState.CallAsync(f, [wh, eData]);
                }
            }
        }
        catch (Exception ex)
        {
            var se = new StyleError()
            {
                MessageBlock =
                {
                    Text = "The custom tool has encountered an error",
                },
                ErrorBox =
                {
                    Text = ex.ToString(),
                }
            };
            await se.ShowDialog(MainWindow);
        }
    }
}
