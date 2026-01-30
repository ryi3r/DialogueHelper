using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DialogueHelper.StylesParser;
using Lua;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace DialogueHelper.PreviewRenderer;

[LuaObject]
public partial class ExternalData(MainWindow mainWindow, Canvas canvas, Bitmap fontTexture)
{
    [LuaMember("env")]
    public LuaTable Env = [];
    [LuaMember("global_env")]
    public LuaTable GlobalEnv = [];
    [LuaMember("style")]
    public required StyleParser Style;
    [LuaMember("font")]
    public required FontMetadata Font;
    [LuaMember("box")]
    public required BoxMetadata Box;
    [LuaMember("char")]
    public required ExternalChar Char;
    [LuaMember("glyph")]
    public required ExternalGlyph Glyph;

    [LuaMember("load_texture")]
    public LuaBitmap LoadTexture(string path)
    {
        if (mainWindow.LoadedImages.TryGetValue(path, out var value))
            return new(value);
        var baseImage = Image.Load($"{Style.Folder}/{path}");
        using var stream = new MemoryStream();
        baseImage.SaveAsBmp(stream);
        baseImage.Dispose();
        stream.Seek(0, SeekOrigin.Begin);
        var bitmap = new Bitmap(stream);
        mainWindow.LoadedImages.Add(path, bitmap);
        return new(bitmap);
    }

    [LuaMember("free_texture")]
    public void FreeTexture(LuaBitmap bitmap)
    {
        mainWindow.LoadedImages.Remove(mainWindow.LoadedImages.FirstOrDefault(i => i.Value == bitmap.Bitmap).Key);
        bitmap.Bitmap.Dispose();
    }

    [LuaMember("modify_image")]
    public LuaBitmap ModifyImage(LuaBitmap bitmap, LuaColor? color, double alpha = 1.0)
    {
        using var stream1 = new MemoryStream();
        bitmap.Bitmap.Save(stream1);
        stream1.Seek(0, SeekOrigin.Begin);
        var tex = Image.Load<Rgba32>(stream1);
        var col = (color ?? Glyph.Color).Color.ToPixel<Rgba32>();
        tex.ProcessPixelRows(accessor =>
        {
            for (var i = 0; i < accessor.Height; i++)
            {
                foreach (ref var px in accessor.GetRowSpan(i))
                {
                    px.R = (byte)(px.R / 255.0f * (col.R / 255.0f) * 255.0f);
                    px.G = (byte)(px.G / 255.0f * (col.G / 255.0f) * 255.0f);
                    px.B = (byte)(px.B / 255.0f * (col.B / 255.0f) * 255.0f);
                    px.A = (byte)(px.A / 255.0f * Math.Clamp(alpha, 0.0, 1.0) * 255.0f);
                }
            }
        });
        {
            using var stream2 = new MemoryStream();
            tex.SaveAsWebp(stream2);
            stream2.Seek(0, SeekOrigin.Begin);
            return new(new(stream2));
        }
    }

    [LuaMember("modify_image_and_cache")]
    public LuaBitmap ModifyImageAndCache(LuaBitmap bitmap, LuaColor color, double alpha = 1.0)
    {
        var path = mainWindow.LoadedImages.FirstOrDefault(i => i.Value == bitmap.Bitmap).Key;
        if (mainWindow.LoadedImages.TryGetValue($"{Glyph.Color.ToHex()}+{alpha}@{path}", out var value))
            return new(value);
        else
        {
            var outBitmap = ModifyImage(bitmap, color, alpha);
            mainWindow.LoadedImages.Add($"{Glyph.Color.ToHex()}+{alpha}@{path}", outBitmap.Bitmap);
            return outBitmap;
        }
    }

    [LuaMember("draw_glyph")]
    public LuaImage DrawGlyph()
    {
        var fontGlyph = Font.GlyphDictionary[Char.Char].Read<GlyphMetadata>();
        var targetTexture = fontTexture;
        var alpha = Math.Round(Glyph.Alpha, 2);
        if (Glyph.Color != LuaColor.White || alpha < 1.0)
            targetTexture = ModifyImageAndCache(new(targetTexture), Glyph.Color, alpha).Bitmap;
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

        return new(glyphImg);
    }

    [LuaMember("draw_texture")]
    public LuaImage DrawTexture(Bitmap img, LuaDoubleTuple position)
    {
        var node = new Avalonia.Controls.Image()
        {
            Source = img,
            Width = img.Size.Width * GetCurrentScale(),
            Height = img.Size.Height * GetCurrentScale(),
        };

        RenderOptions.SetBitmapInterpolationMode(node, BitmapInterpolationMode.None);
        position = new(position.X * GetCurrentScale(), position.Y * GetCurrentScale());
        node.SetValue(Canvas.LeftProperty, position.X);
        node.SetValue(Canvas.TopProperty, position.Y);
        canvas.Children.Add(node);

        if (position.X + node.Width > canvas.Width)
            canvas.Width = position.X + node.Width;
        if (position.Y + node.Height > canvas.Height)
            canvas.Height = position.Y + node.Height;

        return new(node);
    }

    [LuaMember("draw_texture_scaled")]
    public LuaImage DrawTextureScaled(Bitmap img, LuaDoubleTuple position, LuaDoubleTuple scale)
    {
        var node = new Avalonia.Controls.Image()
        {
            Source = img,
            Width = img.Size.Width * GetCurrentScale() * scale.X,
            Height = img.Size.Height * GetCurrentScale() * scale.Y,
        };

        RenderOptions.SetBitmapInterpolationMode(node, BitmapInterpolationMode.None);
        position = new(position.X * GetCurrentScale(), position.Y * GetCurrentScale());
        node.SetValue(Canvas.LeftProperty, position.X);
        node.SetValue(Canvas.TopProperty, position.Y);
        canvas.Children.Add(node);

        if (position.X + node.Width > canvas.Width)
            canvas.Width = position.X + node.Width;
        if (position.Y + node.Height > canvas.Height)
            canvas.Height = position.Y + node.Height;

        return new(node);
    }

    [LuaMember("draw_texture_sized")]
    public LuaImage DrawTextureSized(Bitmap img, LuaDoubleTuple position, LuaDoubleTuple size)
    {
        var node = new Avalonia.Controls.Image()
        {
            Source = img,
            Width = size.X * GetCurrentScale(),
            Height = size.Y * GetCurrentScale(),
        };

        RenderOptions.SetBitmapInterpolationMode(node, BitmapInterpolationMode.None);
        position = new(position.X * GetCurrentScale(), position.Y * GetCurrentScale());
        node.SetValue(Canvas.LeftProperty, position.X);
        node.SetValue(Canvas.TopProperty, position.Y);
        canvas.Children.Add(node);

        if (position.X + node.Width > canvas.Width)
            canvas.Width = position.X + node.Width;
        if (position.Y + node.Height > canvas.Height)
            canvas.Height = position.Y + node.Height;

        return new(node);
    }

    [LuaMember("draw_texture_cropped")]
    public LuaImage DrawTextureCropped(Bitmap img, LuaDoubleTuple position, LuaDoubleTuple size, LuaIntTuple srcPosition, LuaIntTuple srcSize)
    {
        var node = new Avalonia.Controls.Image()
        {
            Source = new CroppedBitmap(img, new(srcPosition.X, srcPosition.Y, srcSize.X, srcSize.Y)),
            Width = size.X * GetCurrentScale(),
            Height = size.Y * GetCurrentScale(),
        };

        RenderOptions.SetBitmapInterpolationMode(node, BitmapInterpolationMode.None);
        position = new(position.X * GetCurrentScale(), position.Y * GetCurrentScale());
        node.SetValue(Canvas.LeftProperty, position.X);
        node.SetValue(Canvas.TopProperty, position.Y);
        canvas.Children.Add(node);

        if (position.X + node.Width > canvas.Width)
            canvas.Width = position.X + node.Width;
        if (position.Y + node.Height > canvas.Height)
            canvas.Height = position.Y + node.Height;

        return new(node);
    }

    [LuaMember("set_current_box")]
    public void SetCurrentBox(int boxId)
    {
        mainWindow.BoxComboBox.SelectedIndex = boxId;
    }

    [LuaMember("set_current_font")]
    public void SetCurrentFont(int fontId)
    {
        mainWindow.FontComboBox.SelectedIndex = fontId;
    }

    [LuaMember("set_current_scale")]
    public void SetCurrentScale(float scale)
    {
        mainWindow.PreviewScale.Value = (decimal)scale;
    }

    [LuaMember("set_current_box_scale")]
    public void SetCurrentBoxScale(float scale)
    {
        mainWindow.BoxScale.Value = (decimal)scale;
    }

    [LuaMember("set_current_font_scale")]
    public void SetCurrentFontScale(float scale)
    {
        mainWindow.FontScale.Value = (decimal)scale;
    }

    [LuaMember("get_current_box")]
    public int GetCurrentBox()
    {
        return mainWindow.BoxComboBox.SelectedIndex;
    }

    [LuaMember("get_current_font")]
    public int GetCurrentFont()
    {
        return mainWindow.FontComboBox.SelectedIndex;
    }

    [LuaMember("get_current_scale")]
    public float GetCurrentScale()
    {
        return (float?)mainWindow.PreviewScale.Value ?? 0f;
    }

    [LuaMember("get_current_box_scale")]
    public float GetCurrentBoxScale()
    {
        return (float?)mainWindow.BoxScale.Value ?? 0f;
    }

    [LuaMember("get_current_font_scale")]
    public float GetCurrentFontScale()
    {
        return (float?)mainWindow.FontScale.Value ?? 0f;
    }

    [LuaMember("set_changeable_box")]
    public void SetChangeableBox(bool changeable)
    {
        mainWindow.BoxComboBox.IsEnabled = changeable;
    }

    [LuaMember("set_changeable_font")]
    public void SetChangeableFont(bool changeable)
    {
        mainWindow.FontComboBox.IsEnabled = changeable;
    }
}
