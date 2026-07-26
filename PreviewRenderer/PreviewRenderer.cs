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
    public enum UpdateKind
    {
        None,
        EditableTextChanged,
        OriginalTextChanged,
        SelectedBoxChanged,
        SelectedFontChanged,
        ReloadedStyle,
        InitializedStyle,
        CustomPropertyChanged,
        ScaleChanged,
        ColorChanged,
        ChangedEntry,
    }

    public readonly MainWindow MainWindow = mainWindow;
    public List<TintedImage> BoxImageNodes = [];

    public async void CreateRender(UpdateKind updateKind, StyleParser style, StringContainer sc, string str, Canvas canvas)
    {
        canvas.Children.Clear();
        BoxImageNodes.Clear();
        // ensure always that selected box/font is in-bounds
        if (style.FontMetadata.Count == 0 || style.BoxMetadata.Count == 0)
        {
            //Console.WriteLine("early return");
            return;
        }
        if (sc.BoxStyle < 0)
            sc.BoxStyle = 0;
        if (sc.FontStyle < 0)
            sc.FontStyle = 0;
        if (sc.BoxStyle >= style.BoxMetadata.Count)
            sc.BoxStyle = style.BoxMetadata.Count - 1;
        if (sc.FontStyle >= style.FontMetadata.Count)
            sc.FontStyle = style.FontMetadata.Count - 1;
        var box = style.BoxMetadata[sc.BoxStyle];
        var font = style.FontMetadata[sc.FontStyle];

        canvas.Width = 0;
        canvas.Height = 0;

        foreach (var bImg in box.Images)
        {
            var imgAsset = style.ImageAssets[bImg.Path];
            var scale = sc.BoxScale * sc.PreviewScale;
            var avImg = new TintedImage
            {
                Source = imgAsset,
                Tint = sc.BoxColor,
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
        var eData = new ExternalData(MainWindow, sc, canvas)
        {
            Box = box,
            BoxImages = BoxImageNodes,
            Char = new() { Char = '\0', Index = 0, String = str, IsNewline = false, IsIgnore = false, StartPosition = ((int)box.TextOffset.ElementAtOrDefault(0), (int)box.TextOffset.ElementAtOrDefault(1)) },
            Font = font,
            Glyph = new() { Scale = (font.Scale * sc.FontScale * sc.PreviewScale, font.Scale * sc.FontScale * sc.PreviewScale), Color = sc.TextColor },
            Style = style,
            UpdateKind = updateKind,
        };
        if (!style.InitializedScript)
        {
            //Console.WriteLine("script not initialized");
            return;
        }
        try
        {
            var wh = new CustomToolWindowHandler(MainWindow, style);
            eData.Char.String = str;
            //Console.WriteLine("rendering prep");
            if (!await (style.Script?.PrepareDraw(wh, eData) ?? Task.FromResult(false)))
            {
                //Console.WriteLine("rendering start");
                foreach (var (i, chr) in str.Index())
                {
                    eData.Char.Char = chr;
                    eData.Char.Index = i;
                    eData.Char.IsNewline = style.Metadata.NewLines.Contains(chr);
                    eData.Char.IsIgnore = style.Metadata.Ignore.Contains(chr);
                    eData.Glyph.Scale = (font.Scale * sc.FontScale * sc.PreviewScale, font.Scale * sc.FontScale * sc.PreviewScale);
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
            //Console.WriteLine("rendering OK");
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
