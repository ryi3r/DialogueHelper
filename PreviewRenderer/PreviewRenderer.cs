using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.SpellChecker;
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
            foreach (var ttfFont in style.FontMetadata) {
                if (!ttfFont.IsTtf) {
                    continue;
                }
                foreach (var c in str.Distinct()) {
                    _ = ttfFont.GlyphDictionary[c];
                }
            }
            // get every misspelled word
            var misspelled = MainWindow.SpellCheckerConfig is null
                ? [] : new SpellChecker(MainWindow.SpellCheckerConfig).CheckSpellingFullText(str);
            var charRects = new Dictionary<int, (double X0, double X1, double Y)>();
            // get the bottom of the 'g' character from the current font
            var showSpellcheck = MainWindow.ShowSpellcheckUnderline.IsChecked == true;
            // this will break if the current style doesn't also have the size multiplied by 2 in it's DrawGlyph()
            var baselineGlyph = font.GlyphDictionary.TryGetValue('g', out var g)
                ? g : font.Glyphs.OrderByDescending(x => x.Size.Y).First();
            var baselineHeight = baselineGlyph.Size.Y * 2;
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
                        if (misspelled.Any(m => i >= m.Start && i < m.Start + m.Length))
                        {
                            // not too much since we are already pretty low by using 'g' as a baseline
                            const double underlineGap = 2;
			                            
                            var x0 = eData.Glyph.Position.X * eData.Glyph.Scale.X;
                            var x1 = x0 + eData.Glyph.Size.X * eData.Glyph.Scale.X;
                            var y = eData.Glyph.Position.Y * eData.Glyph.Scale.Y
                                    + baselineHeight * eData.Glyph.Scale.Y
                                    + underlineGap * eData.Glyph.Scale.Y;
                            charRects[i] = (x0, x1, y);
                        }
                    }
                    finally
                    {
                        if (eData.QueueRedraw)
                            MainWindow.RedrawRenderer = true;
                    }
                }
                // only show if checkbox is checked
                if (showSpellcheck)
                {
                    foreach (var entry in misspelled)
                    {
                        if (!charRects.TryGetValue(entry.Start, out var first))
                            continue;
                        if (!charRects.TryGetValue(entry.Start + entry.Length - 1, out var last))
                            continue;
			
                        canvas.Children.Add(new Line
                        {
                            StartPoint = new Point(first.X0, first.Y),
                            EndPoint = new Point(last.X1, first.Y),
                            Stroke = Brushes.Red,
                            StrokeThickness = 1.5,
                            StrokeDashArray = new AvaloniaList<double>(4.0, 2.0),
                            StrokeLineCap = PenLineCap.Round,
                            //Opacity = 0.9
                        });
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
