using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using DialogueHelper.FileFormat;
using DialogueHelper.StylesParser;
using SkiaSharp;

namespace DialogueHelper.PreviewRenderer;

public class ExternalData(MainWindow mainWindow, StringContainer stringContainer, Canvas canvas)
{
    public required StyleParser Style;
    public required FontMetadata Font;
    public required BoxMetadata Box;
    public required ExternalChar Char;
    public required ExternalGlyph Glyph;
    StringContainer _stringContainer = stringContainer;
    public required List<TintedImage> BoxImages = [];
    public required PreviewRenderer.UpdateKind UpdateKind;

    public bool QueueRedraw;

    public SKBitmap LoadTexture(string path)
    {
        if (mainWindow.LoadedImages.TryGetValue(path, out var value))
            return value;
        var bitmap = SKBitmap.Decode($"{Style.Folder}/{path}");
        mainWindow.LoadedImages.Add(path, bitmap);
        return bitmap;
    }

    public void FreeTexture(SKBitmap bitmap)
    {
        var key = mainWindow.LoadedImages.FirstOrDefault(i => i.Value == bitmap).Key;
        if (key is null)
            return;
        mainWindow.LoadedImages.Remove(key);
        bitmap.Dispose();
    }

    public TintedImage? DrawGlyph() {
        if (Font.IsTtf) {
            return DrawTtfGlyph();
        }

        var fontGlyph = Font.GlyphDictionary[Char.Char];
        if (!Style.ImageAssets.TryGetValue(Font.ImagePath!, out var targetTexture)) {
            return null;
        }

        return PlaceGlyphImage(targetTexture, new((int)fontGlyph.Position.X, (int)fontGlyph.Position.Y, (int)fontGlyph.Size.X, (int)fontGlyph.Size.Y));
    }

    private TintedImage? DrawTtfGlyph() {
        if (Font.Typeface is null)
            return null;

        var key = (Font, Char.Char);
        if (!mainWindow.RasterizedGlyphs.TryGetValue(key, out var bitmap)) {
            bitmap = RasterizeTtfGlyph(Font, Char.Char);
            mainWindow.RasterizedGlyphs.Add(key, bitmap);
        }

        return PlaceGlyphImage(bitmap, null);
    }

    private TintedImage PlaceGlyphImage(SKBitmap texture, PixelRect? sourceRect) {
        var glyphImg = new TintedImage {
            Source = texture,
            SourceRect = sourceRect,
            TintEnabled = true,
            Tint = Glyph.Color,
            Width = Glyph.Size.X * Glyph.Scale.X,
            Height = Glyph.Size.Y * Glyph.Scale.Y,
        };

        var x = Glyph.Position.X * Glyph.Scale.X;
        var y = Glyph.Position.Y * Glyph.Scale.Y;
        glyphImg.SetValue(Canvas.LeftProperty, x);
        glyphImg.SetValue(Canvas.TopProperty, y);
        glyphImg.RenderTransformOrigin = Avalonia.RelativePoint.TopLeft;
        canvas.Children.Add(glyphImg);

        if (x + glyphImg.Width > canvas.Width)
            canvas.Width = x + glyphImg.Width;
        if (y + glyphImg.Height > canvas.Height)
            canvas.Height = y + glyphImg.Height;

        return glyphImg;
    }
    
    private static SKBitmap RasterizeTtfGlyph(FontMetadata font, char c) {
        var glyphMeta = font.GlyphDictionary[c];
        var width = System.Math.Max(1, (int)System.Math.Ceiling(glyphMeta.Size.X));
        var height = System.Math.Max(1, (int)System.Math.Ceiling(glyphMeta.Size.Y));
			
        var bitmap = new SKBitmap(width, height);
        using var skCanvas = new SKCanvas(bitmap);
        using var skFont = new SKFont(font.Typeface, (float)font.Size);
        using var paint = new SKPaint { IsAntialias = true, Color = SKColors.White };
        skFont.GetFontMetrics(out var metrics);
        skCanvas.DrawText(c.ToString(), 0, -metrics.Ascent, skFont, paint);
        return bitmap;
    }

    public TintedImage DrawTexture(SKBitmap img, (double X, double Y) position, Color? color = null)
    {
        var node = new TintedImage
        {
            Source = img,
            TintEnabled = true,
            Tint = color ?? new() { R = 255, G = 255, B = 255 },
            Width = img.Width * GetCurrentPreviewScale(),
            Height = img.Height * GetCurrentPreviewScale(),
        };

        position = (position.X * GetCurrentPreviewScale(), position.Y * GetCurrentPreviewScale());
        node.SetValue(Canvas.LeftProperty, position.X);
        node.SetValue(Canvas.TopProperty, position.Y);
        canvas.Children.Add(node);

        if (position.X + node.Width > canvas.Width)
            canvas.Width = position.X + node.Width;
        if (position.Y + node.Height > canvas.Height)
            canvas.Height = position.Y + node.Height;

        return node;
    }

    public TintedImage DrawTextureScaled(SKBitmap img, (double X, double Y) position, (double X, double Y) scale, Color? color = null)
    {
        var node = new TintedImage
        {
            Source = img,
            TintEnabled = true,
            Tint = color ?? new() { R = 255, G = 255, B = 255 },
            Width = img.Width * GetCurrentPreviewScale() * scale.X,
            Height = img.Height * GetCurrentPreviewScale() * scale.Y,
        };

        position = (position.X * GetCurrentPreviewScale(), position.Y * GetCurrentPreviewScale());
        node.SetValue(Canvas.LeftProperty, position.X);
        node.SetValue(Canvas.TopProperty, position.Y);
        canvas.Children.Add(node);

        if (position.X + node.Width > canvas.Width)
            canvas.Width = position.X + node.Width;
        if (position.Y + node.Height > canvas.Height)
            canvas.Height = position.Y + node.Height;

        return node;
    }

    public TintedImage DrawTextureSized(SKBitmap img, (double X, double Y) position, (double X, double Y) size, Color? color = null)
    {
        var node = new TintedImage
        {
            Source = img,
            TintEnabled = true,
            Tint = color ?? new() { R = 255, G = 255, B = 255 },
            Width = size.X * GetCurrentPreviewScale(),
            Height = size.Y * GetCurrentPreviewScale(),
        };

        position = (position.X * GetCurrentPreviewScale(), position.Y * GetCurrentPreviewScale());
        node.SetValue(Canvas.LeftProperty, position.X);
        node.SetValue(Canvas.TopProperty, position.Y);
        canvas.Children.Add(node);

        if (position.X + node.Width > canvas.Width)
            canvas.Width = position.X + node.Width;
        if (position.Y + node.Height > canvas.Height)
            canvas.Height = position.Y + node.Height;

        return node;
    }

    public TintedImage DrawTextureCropped(SKBitmap img, (double X, double Y) position, (double X, double Y) size, (int X, int Y) srcPosition, (int X, int Y) srcSize, Color? color = null)
    {
        var node = new TintedImage
        {
            Source = img,
            TintEnabled = true,
            Tint = color ?? new() { R = 255, G = 255, B = 255 },
            SourceRect = new(srcPosition.X, srcPosition.Y, srcSize.X, srcSize.Y),
            Width = size.X * GetCurrentPreviewScale(),
            Height = size.Y * GetCurrentPreviewScale(),
        };

        position = (position.X * GetCurrentPreviewScale(), position.Y * GetCurrentPreviewScale());
        node.SetValue(Canvas.LeftProperty, position.X);
        node.SetValue(Canvas.TopProperty, position.Y);
        canvas.Children.Add(node);

        if (position.X + node.Width > canvas.Width)
            canvas.Width = position.X + node.Width;
        if (position.Y + node.Height > canvas.Height)
            canvas.Height = position.Y + node.Height;

        return node;
    }

    public void SetCurrentBox(int boxId)
    {
        _stringContainer.BoxStyle = boxId;
        mainWindow.BoxComboBox.SelectedIndex = boxId;
    }

    public void SetCurrentFont(int fontId)
    {
        _stringContainer.FontStyle = fontId;
        mainWindow.FontComboBox.SelectedIndex = fontId;
    }

    public void SetCurrentPreviewScale(float scale)
    {
        _stringContainer.PreviewScale = scale;
        mainWindow.PreviewScale.Value = (decimal)scale;
    }

    public void SetCurrentBoxScale(float scale)
    {
        _stringContainer.BoxScale = scale;
        mainWindow.BoxScale.Value = (decimal)scale;
    }

    public void SetCurrentFontScale(float scale)
    {
        _stringContainer.FontScale = scale;
        mainWindow.FontScale.Value = (decimal)scale;
    }

    public int GetCurrentBox() => _stringContainer.BoxStyle;

    public int GetCurrentFont() => _stringContainer.FontStyle;

    public float GetCurrentPreviewScale() => _stringContainer.PreviewScale;

    public float GetCurrentBoxScale() => _stringContainer.BoxScale;

    public float GetCurrentFontScale() => _stringContainer.FontScale;

    public void SetChangeableBox(bool changeable) => mainWindow.BoxComboBox.IsEnabled = changeable;

    public void SetChangeableFont(bool changeable) => mainWindow.FontComboBox.IsEnabled = changeable;

    public StringContainer GetStringContainer() => new(_stringContainer.OutputFormatEntry());
}
