using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using DialogueHelper.External;
using DialogueHelper.FileFormat;
using DialogueHelper.StylesParser;
using DialogueHelper.Views;

namespace DialogueHelper.PreviewRenderer;

public class PreviewRenderer(MainWindow mainWindow)
{
    public readonly MainWindow MainWindow = mainWindow;
    public List<TintedImage> BoxImageNodes = [];

    public async void CreateRender(StyleParser style, StringContainer sc, int selectedBox, int selectedFont, float boxScale, float fontScale, float previewScale, Color textColor, Color boxColor, string str, Canvas canvas)
    {
        canvas.Children.Clear();
        BoxImageNodes.Clear();
        // ensure always that selected box/font is in-bounds
        if (style.FontMetadata.Count == 0 || style.BoxMetadata.Count == 0)
            return;
        if (selectedBox < 0)
            selectedBox = 0;
        if (selectedFont < 0)
            selectedFont = 0;
        if (selectedBox >= style.BoxMetadata.Count)
            selectedBox = style.BoxMetadata.Count - 1;
        if (selectedFont >= style.FontMetadata.Count)
            selectedFont = style.FontMetadata.Count - 1;
        var box = style.BoxMetadata[selectedBox];
        var font = style.FontMetadata[selectedFont];

        canvas.Width = 0;
        canvas.Height = 0;

        foreach (var bImg in box.Images)
        {
            var imgAsset = style.ImageAssets[bImg.Path];
            var scale = boxScale * previewScale;
            var avImg = new TintedImage
            {
                Source = imgAsset,
                Tint = boxColor,
                TintEnabled = true,
                Width = imgAsset.Width * scale,
                Height = imgAsset.Height * scale,
            };
            var x = bImg.Position.ElementAtOrDefault(0);
            var y = bImg.Position.ElementAtOrDefault(1);
            avImg.SetValue(Canvas.LeftProperty, x);
            avImg.SetValue(Canvas.TopProperty, y);
            BoxImageNodes.Add(avImg);
            canvas.Children.Add(avImg);

            if (x + (imgAsset.Width * scale) > canvas.Width)
                canvas.Width = x + (imgAsset.Width * scale);
            if (y + (imgAsset.Height * scale) > canvas.Height)
                canvas.Height = y + (imgAsset.Height * scale);
        }
        var eData = new ExternalData(MainWindow, canvas)
        {
            StringContainer = sc,
            Box = box,
            BoxImages = BoxImageNodes,
            Char = new() { Char = '\0', Index = 0, String = str, IsNewline = false, IsIgnore = false, StartPosition = ((int)box.TextOffset.ElementAtOrDefault(0), (int)box.TextOffset.ElementAtOrDefault(1)) },
            Font = font,
            Glyph = new() { Scale = (font.Scale * fontScale * previewScale, font.Scale * fontScale * previewScale), Color = textColor },
            Style = style,
        };
        if (!style.InitializedScript)
            return;
        try
        {
            var wh = new CustomToolWindowHandler(MainWindow, style);
            eData.Char.String = str;
            if (!await (style.Script?.PrepareDraw(wh, eData) ?? Task.FromResult(false)))
            {
                foreach (var (i, chr) in str.Index())
                {
                    eData.Char.Char = chr;
                    eData.Char.Index = i;
                    eData.Char.IsNewline = style.Metadata.NewLines.Contains(chr);
                    eData.Char.IsIgnore = style.Metadata.Ignore.Contains(chr);
                    eData.Glyph.Scale = (font.Scale * fontScale * previewScale, font.Scale * fontScale * previewScale);
                    eData.QueueRedraw = false;

                    try
                    {
                        if (await (style.Script?.DrawGlyph(wh, eData) ?? Task.FromResult(false)))
                            break;
                    }
                    finally
                    {
                        if (eData.QueueRedraw)
                            MainWindow.RedrawRenderer = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var se = new StyleError
            {
                MessageBlock = { Text = "The custom tool has encountered an error" },
                ErrorBox = { Text = ex.ToString() }
            };
            await se.ShowDialog(MainWindow);
        }
    }
}
