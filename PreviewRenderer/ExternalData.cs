using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DialogueHelper.StylesParser;
using SkiaSharp;

namespace DialogueHelper.PreviewRenderer;

public class ExternalData(MainWindow mainWindow, Canvas canvas)
{
    public required StyleParser Style;
    public required FontMetadata Font;
    public required BoxMetadata Box;
    public required ExternalChar Char;
    public required ExternalGlyph Glyph;

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

    public TintedImage? DrawGlyph()
    {
        var fontGlyph = Font.GlyphDictionary[Char.Char];
        if (!Style.ImageAssets.TryGetValue(Font.ImagePath, out var targetTexture))
            return null;
        var glyphImg = new TintedImage
        {
            Source = targetTexture,
            SourceRect = new((int)fontGlyph.Position.X, (int)fontGlyph.Position.Y, (int)fontGlyph.Size.X, (int)fontGlyph.Size.Y),
            TintEnabled = true,
            Tint = Glyph.Color,
            Width = Glyph.Size.X,
            Height = Glyph.Size.Y,
            RenderTransform = new ScaleTransform(Glyph.Scale.X, Glyph.Scale.Y),
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

    public TintedImage DrawTexture(SKBitmap img, (double X, double Y) position, Color? color = null)
    {
        var node = new TintedImage
        {
            Source = img,
            TintEnabled = true,
            Tint = color ?? new() { R = 255, G = 255, B = 255 },
            Width = img.Width * GetCurrentScale(),
            Height = img.Height * GetCurrentScale(),
        };

        position = (position.X * GetCurrentScale(), position.Y * GetCurrentScale());
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
            Width = img.Width * GetCurrentScale() * scale.X,
            Height = img.Height * GetCurrentScale() * scale.Y,
        };

        position = (position.X * GetCurrentScale(), position.Y * GetCurrentScale());
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
            Width = size.X * GetCurrentScale(),
            Height = size.Y * GetCurrentScale(),
        };

        position = (position.X * GetCurrentScale(), position.Y * GetCurrentScale());
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
            Width = size.X * GetCurrentScale(),
            Height = size.Y * GetCurrentScale(),
        };

        position = (position.X * GetCurrentScale(), position.Y * GetCurrentScale());
        node.SetValue(Canvas.LeftProperty, position.X);
        node.SetValue(Canvas.TopProperty, position.Y);
        canvas.Children.Add(node);

        if (position.X + node.Width > canvas.Width)
            canvas.Width = position.X + node.Width;
        if (position.Y + node.Height > canvas.Height)
            canvas.Height = position.Y + node.Height;

        return node;
    }

    public void SetCurrentBox(int boxId) => mainWindow.BoxComboBox.SelectedIndex = boxId;

    public void SetCurrentFont(int fontId) => mainWindow.FontComboBox.SelectedIndex = fontId;

    public void SetCurrentScale(float scale) => mainWindow.PreviewScale.Value = (decimal)scale;

    public void SetCurrentBoxScale(float scale) => mainWindow.BoxScale.Value = (decimal)scale;

    public void SetCurrentFontScale(float scale) => mainWindow.FontScale.Value = (decimal)scale;

    public int GetCurrentBox() => mainWindow.BoxComboBox.SelectedIndex;

    public int GetCurrentFont() => mainWindow.FontComboBox.SelectedIndex;

    public float GetCurrentScale() => (float?)mainWindow.PreviewScale.Value ?? 0;

    public float GetCurrentBoxScale() => (float?)mainWindow.BoxScale.Value ?? 0;

    public float GetCurrentFontScale() => (float?)mainWindow.FontScale.Value ?? 0;

    public void SetChangeableBox(bool changeable) => mainWindow.BoxComboBox.IsEnabled = changeable;

    public void SetChangeableFont(bool changeable) => mainWindow.FontComboBox.IsEnabled = changeable;
}
