using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DialogueHelper.StylesParser;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;

namespace DialogueHelper.PreviewRenderer;

public class ExternalData(MainWindow mainWindow, Canvas canvas)
{
    public Dictionary<string, dynamic?> Env = [];
    public Dictionary<string, dynamic?> GlobalEnv = [];
    public required StyleParser Style;
    public required FontMetadata Font;
    public required BoxMetadata Box;
    public required ExternalChar Char;
    public required ExternalGlyph Glyph;

    public Bitmap LoadTexture(string path)
    {
        if (mainWindow.LoadedImages.TryGetValue(path, out var value))
            return value;
        var baseImage = Image.Load($"{Style.Folder}/{path}");
        using var stream = new MemoryStream();
        baseImage.SaveAsBmp(stream);
        baseImage.Dispose();
        stream.Seek(0, SeekOrigin.Begin);
        var bitmap = new Bitmap(stream);
        mainWindow.LoadedImages.Add(path, bitmap);
        return bitmap;
    }

    public void FreeTexture(Bitmap bitmap)
    {
        mainWindow.LoadedImages.Remove(mainWindow.LoadedImages.FirstOrDefault(i => i.Value == bitmap).Key);
        bitmap.Dispose();
    }

    public Bitmap ModifyImage(Bitmap bitmap, Color? color = null, double alpha = 1.0)
    {
        using var stream1 = new MemoryStream();
        bitmap.Save(stream1);
        stream1.Seek(0, SeekOrigin.Begin);
        var tex = Image.Load<Rgba32>(stream1);
        var col = (color ?? Glyph.Color).ToPixel<Rgba32>();
        tex.ProcessPixelRows(accessor =>
        {
            for (var i = 0; i < accessor.Height; i++)
            {
                foreach (ref var px in accessor.GetRowSpan(i))
                {
                    px.R = (byte)(((px.R / 255f) * (col.R / 255f)) * 255f);
                    px.G = (byte)(((px.G / 255f) * (col.G / 255f)) * 255f);
                    px.B = (byte)(((px.B / 255f) * (col.B / 255f)) * 255f);
                    px.A = (byte)(((px.A / 255f) * double.Clamp(alpha, 0d, 1d)) * 255f);
                }
            }
        });
        {
            using var stream2 = new MemoryStream();
            tex.SaveAsWebp(stream2);
            stream2.Seek(0, SeekOrigin.Begin);
            return new Bitmap(stream2);
        }
    }

    public Bitmap ModifyImageAndCache(Bitmap bitmap, Color color, double alpha = 1.0)
    {
        var path = mainWindow.LoadedImages.FirstOrDefault(i => i.Value == bitmap).Key;
        if (mainWindow.LoadedImages.TryGetValue($"{Glyph.Color.ToHex()}+{alpha}@{path}", out var value))
            return value;
        else
        {
            var outBitmap = ModifyImage(bitmap, color, alpha);
            mainWindow.LoadedImages.Add($"{Glyph.Color.ToHex()}+{alpha}@{path}", outBitmap);
            return outBitmap;
        }
    }

    public Avalonia.Controls.Image? DrawGlyph()
    {
        var fontGlyph = Font.GlyphDictionary[Char.Char];
        if (!Style.ImageAssets.TryGetValue(Font.ImagePath, out var targetTexture))
            return null;
        var alpha = Math.Round(Glyph.Alpha, 2);
        if (Glyph.Color != Color.White || alpha < 1.0)
            targetTexture = ModifyImageAndCache(targetTexture, Glyph.Color, alpha);
        var glyphImg = new Avalonia.Controls.Image()
        {
            Source = new CroppedBitmap(targetTexture, new((int)fontGlyph.Position.X, (int)fontGlyph.Position.Y, (int)fontGlyph.Size.X, (int)fontGlyph.Size.Y)),
            Width = Glyph.Size.X,
            Height = Glyph.Size.Y,
            RenderTransform = new ScaleTransform(Glyph.Scale.X, Glyph.Scale.Y),
        };
        RenderOptions.SetBitmapInterpolationMode(glyphImg, BitmapInterpolationMode.None);
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

    public Avalonia.Controls.Image DrawTexture(Bitmap img, (double X, double Y) position)
    {
        var node = new Avalonia.Controls.Image()
        {
            Source = img,
            Width = img.Size.Width * GetCurrentScale(),
            Height = img.Size.Height * GetCurrentScale(),
        };

        RenderOptions.SetBitmapInterpolationMode(node, BitmapInterpolationMode.None);
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

    public Avalonia.Controls.Image DrawTextureScaled(Bitmap img, (double X, double Y) position, (double X, double Y) scale)
    {
        var node = new Avalonia.Controls.Image()
        {
            Source = img,
            Width = img.Size.Width * GetCurrentScale() * scale.X,
            Height = img.Size.Height * GetCurrentScale() * scale.Y,
        };

        RenderOptions.SetBitmapInterpolationMode(node, BitmapInterpolationMode.None);
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

    public Avalonia.Controls.Image DrawTextureSized(Bitmap img, (double X, double Y) position, (double X, double Y) size)
    {
        var node = new Avalonia.Controls.Image()
        {
            Source = img,
            Width = size.X * GetCurrentScale(),
            Height = size.Y * GetCurrentScale(),
        };

        RenderOptions.SetBitmapInterpolationMode(node, BitmapInterpolationMode.None);
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

    public Avalonia.Controls.Image DrawTextureCropped(Bitmap img, (double X, double Y) position, (double X, double Y) size, (int X, int Y) srcPosition, (int X, int Y) srcSize)
    {
        var node = new Avalonia.Controls.Image()
        {
            Source = new CroppedBitmap(img, new(srcPosition.X, srcPosition.Y, srcSize.X, srcSize.Y)),
            Width = size.X * GetCurrentScale(),
            Height = size.Y * GetCurrentScale(),
        };

        RenderOptions.SetBitmapInterpolationMode(node, BitmapInterpolationMode.None);
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

    public void SetCurrentBox(int boxId)
    {
        mainWindow.BoxComboBox.SelectedIndex = boxId;
    }

    public void SetCurrentFont(int fontId)
    {
        mainWindow.FontComboBox.SelectedIndex = fontId;
    }

    public void SetCurrentScale(float scale)
    {
        mainWindow.PreviewScale.Value = (decimal)scale;
    }

    public void SetCurrentBoxScale(float scale)
    {
        mainWindow.BoxScale.Value = (decimal)scale;
    }

    public void SetCurrentFontScale(float scale)
    {
        mainWindow.FontScale.Value = (decimal)scale;
    }

    public int GetCurrentBox()
    {
        return mainWindow.BoxComboBox.SelectedIndex;
    }

    public int GetCurrentFont()
    {
        return mainWindow.FontComboBox.SelectedIndex;
    }

    public float GetCurrentScale()
    {
        return (float?)mainWindow.PreviewScale.Value ?? 0f;
    }

    public float GetCurrentBoxScale()
    {
        return (float?)mainWindow.BoxScale.Value ?? 0f;
    }

    public float GetCurrentFontScale()
    {
        return (float?)mainWindow.FontScale.Value ?? 0f;
    }

    public void SetChangeableBox(bool changeable)
    {
        mainWindow.BoxComboBox.IsEnabled = changeable;
    }

    public void SetChangeableFont(bool changeable)
    {
        mainWindow.FontComboBox.IsEnabled = changeable;
    }
}
