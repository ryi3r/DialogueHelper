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
        QueuedRedraw,
        SpellcheckChanged,
        ReloadSettings,
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
                // get every misspelled word
                var misspelled = MainWindow.SpellCheckerConfig is null
                    ? [] : new SpellChecker(MainWindow.SpellCheckerConfig).CheckSpellingFullText(eData.Char.String);
                var charRects = new Dictionary<int, (double X0, double X1, double Y)>();
                // get the bottom of the 'g' character from the current font
                var showSpellcheck = MainWindow.ShowSpellcheckUnderline.IsChecked ?? false;

                //Console.WriteLine("rendering start");
                foreach (var (i, chr) in eData.Char.String.Index())
                {
                    eData.Char.Char = chr;
                    eData.Char.Index = i;
                    eData.Char.IsNewline = style.Metadata.NewLines.Contains(chr);
                    eData.Char.IsIgnore = style.Metadata.Ignore.Contains(chr);
                    eData.Glyph.Scale = (font.Scale * sc.FontScale * sc.PreviewScale, font.Scale * sc.FontScale * sc.PreviewScale);
                    eData.QueueRedraw = false;
                    eData.MaxCharYRender = -1;

                    try
                    {
                        if (await (style.Script?.DrawGlyph(wh, eData) ?? Task.FromResult(false)))
                            break;
                        if (eData.MaxCharYRender != -1 && misspelled.Any(m => i >= m.Start && i < m.Start + m.Length))
                        {
                            const double underlineGap = 2;

                            var x0 = eData.Glyph.Position.X * eData.Glyph.Scale.X;
                            var x1 = x0 + eData.Glyph.Size.X * eData.Glyph.Scale.X;
                            var y = eData.MaxCharYRender + (underlineGap * eData.Glyph.Scale.Y);
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
                        var last = first;
                        for (var i = entry.Start; i < entry.Start + entry.Length; i++)
                        {
                            if (!charRects.TryGetValue(i, out var e))
                                continue;
                            last.Y = double.Max(last.Y, e.Y);
                            first.Y = double.Max(first.Y, e.Y);

                            last.X0 = double.Max(last.X0, e.X0);
                            last.X1 = double.Max(last.X1, e.X1);
                        }
                        if (last.X0 == first.X1 || last.X1 == first.X1)
                            continue;

                        canvas.Children.Add(new Line
                        {
                            StartPoint = new Point(first.X0, first.Y),
                            EndPoint = new Point(last.X1, first.Y),
                            Stroke = Brushes.Red,
                            StrokeThickness = 1.5,
                            StrokeDashArray = new AvaloniaList<double>(4.0, 2.0),
                            StrokeLineCap = PenLineCap.Round,
                            //Opacity = 0.9,
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
