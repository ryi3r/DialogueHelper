
using System;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Lua;
using SixLabors.ImageSharp;

[LuaObject]
public partial class LuaStorageFile(IStorageFile file)
{
    public IStorageFile File = file;

    [LuaMember("name")]
    public LuaValue LuaName
    {
        get => File.Name;
        set { }
    }
    [LuaMember("path")]
    public LuaValue LuaPath
    {
        get => File.Path.AbsolutePath;
        set { }
    }
}

[LuaObject]
public partial class LuaStorageFolder(IStorageFolder folder)
{
    public IStorageFolder Folder = folder;

    [LuaMember("name")]
    public string Name
    {
        get => Folder.Name;
        set { }
    }
    [LuaMember("path")]
    public string Path
    {
        get => Folder.Path.AbsolutePath;
        set { }
    }
}

[LuaObject]
public partial class LuaBitmap(Bitmap bmp)
{
    public Bitmap Bitmap = bmp;

    [LuaMember("width")]
    public double Width
    {
        get => Bitmap.Size.Width;
        set { }
    }
    [LuaMember("height")]
    public double Height
    {
        get => Bitmap.Size.Height;
        set { }
    }

    [LuaMember("pixel_width")]
    public int PixelWidth
    {
        get => Bitmap.PixelSize.Width;
        set { }
    }
    [LuaMember("pixel_height")]
    public int PixelHeight
    {
        get => Bitmap.PixelSize.Height;
        set { }
    }

    public string AlphaFormat
    {
        get => Bitmap.AlphaFormat.ToString()!;
        set { }
    }

    [LuaMember("save")]
    public void Save(string path, int? quality = null) => Bitmap.Save(path, quality);
}

[LuaObject]
public partial class LuaImage(Avalonia.Controls.Image img)
{
    public Avalonia.Controls.Image Image = img;

    [LuaMember("height")]
    public double Height
    {
        get => Image.Height;
        set { }
    }
    [LuaMember("width")]
    public double Width
    {
        get => Image.Width;
        set { }
    }

    [LuaMember("x")]
    public double X
    {
        get => Image.GetValue(Canvas.LeftProperty);
        set => Image.SetValue(Canvas.LeftProperty, value);
    }
    [LuaMember("y")]
    public double Y
    {
        get => Image.GetValue(Canvas.TopProperty);
        set => Image.SetValue(Canvas.TopProperty, value);
    }

    [LuaMember("interpolation_mode")]
    public string InterpolationMode
    {
        get => RenderOptions.GetBitmapInterpolationMode(Image).ToString();
        set => RenderOptions.SetBitmapInterpolationMode(Image, Enum.Parse<BitmapInterpolationMode>(value));
    }

    [LuaMember("blending_mode")]
    public string BlendingMode
    {
        get => RenderOptions.GetBitmapBlendingMode(Image).ToString();
        set => RenderOptions.SetBitmapBlendingMode(Image, Enum.Parse<BitmapBlendingMode>(value));
    }
}

[LuaObject]
public partial class LuaIntTuple(int item1, int item2)
{
    [LuaMember("first")]
    public int Item1 = item1;
    [LuaMember("last")]
    public int Item2 = item2;

    [LuaMember("create")]
    public static LuaIntTuple Create(int first, int last) => new(first, last);
}

[LuaObject]
public partial class LuaFloatTuple(float item1, float item2)
{
    [LuaMember("first")]
    public float Item1 = item1;
    [LuaMember("last")]
    public float Item2 = item2;

    [LuaMember("create")]
    public static LuaFloatTuple Create(float first, float last) => new(first, last);
}

[LuaObject]
public partial class LuaDoubleTuple(double item1, double item2)
{
    [LuaMember("first")]
    public double Item1 = item1;
    [LuaMember("last")]
    public double Item2 = item2;

    [LuaMember("create")]
    public static LuaDoubleTuple Create(double first, double last) => new(first, last);
}

[LuaObject]
public partial class LuaWindow : Window
{
    [LuaMember("close")]
    public void LuaClose() => Close();

    [LuaMember("hide")]
    public void LuaHide() => Hide();

    [LuaMember("show")]
    public void LuaShow() => Show();
}

[LuaObject]
public partial class LuaColor(SixLabors.ImageSharp.Color color)
{
    public SixLabors.ImageSharp.Color Color = color;

    [LuaMember("create")]
    public static LuaColor Create() => new(default);

    [LuaMember("to_hex")]
    public string ToHex() => Color.ToHex();

    [LuaMember("from_rgba")]
    public static LuaColor FromRgba(float r, float g, float b, float a) => new(new(new Vector4(r, g, b, a)));

    [LuaMember("ALICE_BLUE")]
    public static readonly LuaColor AliceBlue = FromRgba(240, 248, 255, 255);

    [LuaMember("ANTIQUE_WHITE")]
    public static readonly LuaColor AntiqueWhite = FromRgba(250, 235, 215, 255);

    [LuaMember("AQUA")]
    public static readonly LuaColor Aqua = FromRgba(0, 255, 255, 255);

    [LuaMember("AQUAMARINE")]
    public static readonly LuaColor Aquamarine = FromRgba(127, 255, 212, 255);

    [LuaMember("AZURE")]
    public static readonly LuaColor Azure = FromRgba(240, 255, 255, 255);

    [LuaMember("BEIGE")]
    public static readonly LuaColor Beige = FromRgba(245, 245, 220, 255);

    [LuaMember("BISQUE")]
    public static readonly LuaColor Bisque = FromRgba(255, 228, 196, 255);

    [LuaMember("BLACK")]
    public static readonly LuaColor Black = FromRgba(0, 0, 0, 255);

    [LuaMember("BLANCHED_ALMOND")]
    public static readonly LuaColor BlanchedAlmond = FromRgba(255, 235, 205, 255);

    [LuaMember("BLUE")]
    public static readonly LuaColor Blue = FromRgba(0, 0, 255, 255);

    [LuaMember("BLUE_VIOLET")]
    public static readonly LuaColor BlueViolet = FromRgba(138, 43, 226, 255);

    [LuaMember("BROWN")]
    public static readonly LuaColor Brown = FromRgba(165, 42, 42, 255);

    [LuaMember("BURLY_WOOD")]
    public static readonly LuaColor BurlyWood = FromRgba(222, 184, 135, 255);

    [LuaMember("CADET_BLUE")]
    public static readonly LuaColor CadetBlue = FromRgba(95, 158, 160, 255);

    [LuaMember("CHARTREUSE")]
    public static readonly LuaColor Chartreuse = FromRgba(127, 255, 0, 255);

    [LuaMember("CHOCOLATE")]
    public static readonly LuaColor Chocolate = FromRgba(210, 105, 30, 255);

    [LuaMember("CORAL")]
    public static readonly LuaColor Coral = FromRgba(255, 127, 80, 255);

    [LuaMember("CORNFLOWER_BLUE")]
    public static readonly LuaColor CornflowerBlue = FromRgba(100, 149, 237, 255);

    [LuaMember("CORNSILK")]
    public static readonly LuaColor Cornsilk = FromRgba(255, 248, 220, 255);

    [LuaMember("CRIMSON")]
    public static readonly LuaColor Crimson = FromRgba(220, 20, 60, 255);

    [LuaMember("CYAN")]
    public static readonly LuaColor Cyan = Aqua;

    [LuaMember("DARK_BLUE")]
    public static readonly LuaColor DarkBlue = FromRgba(0, 0, 139, 255);

    [LuaMember("DARK_CYAN")]
    public static readonly LuaColor DarkCyan = FromRgba(0, 139, 139, 255);

    [LuaMember("DARK_GOLDENROD")]
    public static readonly LuaColor DarkGoldenrod = FromRgba(184, 134, 11, 255);

    [LuaMember("DARK_GRAY")]
    public static readonly LuaColor DarkGray = FromRgba(169, 169, 169, 255);

    [LuaMember("DARK_GREEN")]
    public static readonly LuaColor DarkGreen = FromRgba(0, 100, 0, 255);

    [LuaMember("DARK_GREY")]
    public static readonly LuaColor DarkGrey = DarkGray;

    [LuaMember("DARK_KHAKI")]
    public static readonly LuaColor DarkKhaki = FromRgba(189, 183, 107, 255);

    [LuaMember("DARK_MAGENTA")]
    public static readonly LuaColor DarkMagenta = FromRgba(139, 0, 139, 255);

    [LuaMember("DARK_OLIVE_GREEN")]
    public static readonly LuaColor DarkOliveGreen = FromRgba(85, 107, 47, 255);

    [LuaMember("DARK_ORANGE")]
    public static readonly LuaColor DarkOrange = FromRgba(255, 140, 0, 255);

    [LuaMember("DARK_ORCHID")]
    public static readonly LuaColor DarkOrchid = FromRgba(153, 50, 204, 255);

    [LuaMember("DARK_RED")]
    public static readonly LuaColor DarkRed = FromRgba(139, 0, 0, 255);

    [LuaMember("DARK_SALMON")]
    public static readonly LuaColor DarkSalmon = FromRgba(233, 150, 122, 255);

    [LuaMember("DARK_SEA_GREEN")]
    public static readonly LuaColor DarkSeaGreen = FromRgba(143, 188, 143, 255);

    [LuaMember("DARK_SLATE_BLUE")]
    public static readonly LuaColor DarkSlateBlue = FromRgba(72, 61, 139, 255);

    [LuaMember("DARK_SLATE_GRAY")]
    public static readonly LuaColor DarkSlateGray = FromRgba(47, 79, 79, 255);

    [LuaMember("DARK_SLATE_GREY")]
    public static readonly LuaColor DarkSlateGrey = DarkSlateGray;

    [LuaMember("DARK_TURQUOISE")]
    public static readonly LuaColor DarkTurquoise = FromRgba(0, 206, 209, 255);

    [LuaMember("DARK_VIOLET")]
    public static readonly LuaColor DarkViolet = FromRgba(148, 0, 211, 255);

    [LuaMember("DEEP_PINK")]
    public static readonly LuaColor DeepPink = FromRgba(255, 20, 147, 255);

    [LuaMember("DEEP_SKY_BLUE")]
    public static readonly LuaColor DeepSkyBlue = FromRgba(0, 191, 255, 255);

    [LuaMember("DIM_GRAY")]
    public static readonly LuaColor DimGray = FromRgba(105, 105, 105, 255);

    [LuaMember("DIM_GREY")]
    public static readonly LuaColor DimGrey = DimGray;

    [LuaMember("DODGER_BLUE")]
    public static readonly LuaColor DodgerBlue = FromRgba(30, 144, 255, 255);

    [LuaMember("FIREBRICK")]
    public static readonly LuaColor Firebrick = FromRgba(178, 34, 34, 255);

    [LuaMember("FLORAL_WHITE")]
    public static readonly LuaColor FloralWhite = FromRgba(255, 250, 240, 255);

    [LuaMember("FOREST_GREEN")]
    public static readonly LuaColor ForestGreen = FromRgba(34, 139, 34, 255);

    [LuaMember("FOCHSIA")]
    public static readonly LuaColor Fuchsia = FromRgba(255, 0, 255, 255);

    [LuaMember("GAINSBORO")]
    public static readonly LuaColor Gainsboro = FromRgba(220, 220, 220, 255);

    [LuaMember("GHOST_WHITE")]
    public static readonly LuaColor GhostWhite = FromRgba(248, 248, 255, 255);

    [LuaMember("GOLD")]
    public static readonly LuaColor Gold = FromRgba(255, 215, 0, 255);

    [LuaMember("GOLDENROD")]
    public static readonly LuaColor Goldenrod = FromRgba(218, 165, 32, 255);

    [LuaMember("GRAY")]
    public static readonly LuaColor Gray = FromRgba(128, 128, 128, 255);

    [LuaMember("GREEN")]
    public static readonly LuaColor Green = FromRgba(0, 128, 0, 255);

    [LuaMember("GREEN_YELLOW")]
    public static readonly LuaColor GreenYellow = FromRgba(173, 255, 47, 255);

    [LuaMember("GREY")]
    public static readonly LuaColor Grey = Gray;

    [LuaMember("HONEYDEW")]
    public static readonly LuaColor Honeydew = FromRgba(240, 255, 240, 255);

    [LuaMember("HOT_PINK")]
    public static readonly LuaColor HotPink = FromRgba(255, 105, 180, 255);

    [LuaMember("INDIAN_RED")]
    public static readonly LuaColor IndianRed = FromRgba(205, 92, 92, 255);

    [LuaMember("INDIGO")]
    public static readonly LuaColor Indigo = FromRgba(75, 0, 130, 255);

    [LuaMember("IVORY")]
    public static readonly LuaColor Ivory = FromRgba(255, 255, 240, 255);

    [LuaMember("KHAKI")]
    public static readonly LuaColor Khaki = FromRgba(240, 230, 140, 255);

    [LuaMember("LAVENDER")]
    public static readonly LuaColor Lavender = FromRgba(230, 230, 250, 255);

    [LuaMember("LAVENDER_BLUSH")]
    public static readonly LuaColor LavenderBlush = FromRgba(255, 240, 245, 255);

    [LuaMember("LAWN_GREEN")]
    public static readonly LuaColor LawnGreen = FromRgba(124, 252, 0, 255);

    [LuaMember("LEMON_CHIFFON")]
    public static readonly LuaColor LemonChiffon = FromRgba(255, 250, 205, 255);

    [LuaMember("LIGHT_BLUE")]
    public static readonly LuaColor LightBlue = FromRgba(173, 216, 230, 255);

    [LuaMember("LIGHT_CORAL")]
    public static readonly LuaColor LightCoral = FromRgba(240, 128, 128, 255);

    [LuaMember("LIGHT_CYAN")]
    public static readonly LuaColor LightCyan = FromRgba(224, 255, 255, 255);

    [LuaMember("LIGHT_GOLDENROD_YELLOW")]
    public static readonly LuaColor LightGoldenrodYellow = FromRgba(250, 250, 210, 255);

    [LuaMember("LIGHT_GRAY")]
    public static readonly LuaColor LightGray = FromRgba(211, 211, 211, 255);

    [LuaMember("LIGHT_GREEN")]
    public static readonly LuaColor LightGreen = FromRgba(144, 238, 144, 255);

    [LuaMember("LIGHT_GREY")]
    public static readonly LuaColor LightGrey = LightGray;

    [LuaMember("LIGHT_PINK")]
    public static readonly LuaColor LightPink = FromRgba(255, 182, 193, 255);

    [LuaMember("LIGHT_SALMON")]
    public static readonly LuaColor LightSalmon = FromRgba(255, 160, 122, 255);

    [LuaMember("LIGHT_SEA_GREEN")]
    public static readonly LuaColor LightSeaGreen = FromRgba(32, 178, 170, 255);

    [LuaMember("LIGHT_SKY_BLUE")]
    public static readonly LuaColor LightSkyBlue = FromRgba(135, 206, 250, 255);

    [LuaMember("LIGHT_SLATE_GRAY")]
    public static readonly LuaColor LightSlateGray = FromRgba(119, 136, 153, 255);

    [LuaMember("LIGHT_SLATE_GREY")]
    public static readonly LuaColor LightSlateGrey = LightSlateGray;

    [LuaMember("LIGHT_STEEL_BLUE")]
    public static readonly LuaColor LightSteelBlue = FromRgba(176, 196, 222, 255);

    [LuaMember("LIGHT_YELLOW")]
    public static readonly LuaColor LightYellow = FromRgba(255, 255, 224, 255);

    [LuaMember("LIME")]
    public static readonly LuaColor Lime = FromRgba(0, 255, 0, 255);

    [LuaMember("LIME_GREEN")]
    public static readonly LuaColor LimeGreen = FromRgba(50, 205, 50, 255);

    [LuaMember("LINEN")]
    public static readonly LuaColor Linen = FromRgba(250, 240, 230, 255);

    [LuaMember("MAGENTA")]
    public static readonly LuaColor Magenta = Fuchsia;

    [LuaMember("MAROON")]
    public static readonly LuaColor Maroon = FromRgba(128, 0, 0, 255);

    [LuaMember("MEDIUM_AQUAMARINE")]
    public static readonly LuaColor MediumAquamarine = FromRgba(102, 205, 170, 255);

    [LuaMember("MEDIUM_BLUE")]
    public static readonly LuaColor MediumBlue = FromRgba(0, 0, 205, 255);

    [LuaMember("MEDIUM_ORCHID")]
    public static readonly LuaColor MediumOrchid = FromRgba(186, 85, 211, 255);

    [LuaMember("MEDIUM_PURPLE")]
    public static readonly LuaColor MediumPurple = FromRgba(147, 112, 219, 255);

    [LuaMember("MEDIUM_SEA_GREEN")]
    public static readonly LuaColor MediumSeaGreen = FromRgba(60, 179, 113, 255);

    [LuaMember("MEDIUM_SLATE_BLUE")]
    public static readonly LuaColor MediumSlateBlue = FromRgba(123, 104, 238, 255);

    [LuaMember("MEDIUM_SPRING_GREEN")]
    public static readonly LuaColor MediumSpringGreen = FromRgba(0, 250, 154, 255);

    [LuaMember("MEDIUM_TURQUOISE")]
    public static readonly LuaColor MediumTurquoise = FromRgba(72, 209, 204, 255);

    [LuaMember("MEDIUM_VIOLET_RED")]
    public static readonly LuaColor MediumVioletRed = FromRgba(199, 21, 133, 255);

    [LuaMember("MIDNIGHT_BLUE")]
    public static readonly LuaColor MidnightBlue = FromRgba(25, 25, 112, 255);

    [LuaMember("MINT_CREAM")]
    public static readonly LuaColor MintCream = FromRgba(245, 255, 250, 255);

    [LuaMember("MISTY_ROSE")]
    public static readonly LuaColor MistyRose = FromRgba(255, 228, 225, 255);

    [LuaMember("MOCCASIN")]
    public static readonly LuaColor Moccasin = FromRgba(255, 228, 181, 255);

    [LuaMember("NAVAJO_WHITE")]
    public static readonly LuaColor NavajoWhite = FromRgba(255, 222, 173, 255);

    [LuaMember("NAVY")]
    public static readonly LuaColor Navy = FromRgba(0, 0, 128, 255);

    [LuaMember("OLD_LACE")]
    public static readonly LuaColor OldLace = FromRgba(253, 245, 230, 255);

    [LuaMember("OLIVE")]
    public static readonly LuaColor Olive = FromRgba(128, 128, 0, 255);

    [LuaMember("OLIVE_DRAB")]
    public static readonly LuaColor OliveDrab = FromRgba(107, 142, 35, 255);

    [LuaMember("ORANGE")]
    public static readonly LuaColor Orange = FromRgba(255, 165, 0, 255);

    [LuaMember("ORANGE_RED")]
    public static readonly LuaColor OrangeRed = FromRgba(255, 69, 0, 255);

    [LuaMember("ORCHID")]
    public static readonly LuaColor Orchid = FromRgba(218, 112, 214, 255);

    [LuaMember("PALE_GOLDENROD")]
    public static readonly LuaColor PaleGoldenrod = FromRgba(238, 232, 170, 255);

    [LuaMember("PALE_GREEN")]
    public static readonly LuaColor PaleGreen = FromRgba(152, 251, 152, 255);

    [LuaMember("PALE_TURQUOISE")]
    public static readonly LuaColor PaleTurquoise = FromRgba(175, 238, 238, 255);

    [LuaMember("PALE_VIOLET_RED")]
    public static readonly LuaColor PaleVioletRed = FromRgba(219, 112, 147, 255);

    [LuaMember("PAPAYA_WHIP")]
    public static readonly LuaColor PapayaWhip = FromRgba(255, 239, 213, 255);

    [LuaMember("PEACH_PUFF")]
    public static readonly LuaColor PeachPuff = FromRgba(255, 218, 185, 255);

    [LuaMember("PERU")]
    public static readonly LuaColor Peru = FromRgba(205, 133, 63, 255);

    [LuaMember("PINK")]
    public static readonly LuaColor Pink = FromRgba(255, 192, 203, 255);

    [LuaMember("PLUM")]
    public static readonly LuaColor Plum = FromRgba(221, 160, 221, 255);

    [LuaMember("POWDER_BLUE")]
    public static readonly LuaColor PowderBlue = FromRgba(176, 224, 230, 255);

    [LuaMember("PURPLE")]
    public static readonly LuaColor Purple = FromRgba(128, 0, 128, 255);

    [LuaMember("REBECCA_PURPLE")]
    public static readonly LuaColor RebeccaPurple = FromRgba(102, 51, 153, 255);

    [LuaMember("RED")]
    public static readonly LuaColor Red = FromRgba(255, 0, 0, 255);

    [LuaMember("ROSY_BROWN")]
    public static readonly LuaColor RosyBrown = FromRgba(188, 143, 143, 255);

    [LuaMember("ROYAL_BLUE")]
    public static readonly LuaColor RoyalBlue = FromRgba(65, 105, 225, 255);

    [LuaMember("SADDLE_BROWN")]
    public static readonly LuaColor SaddleBrown = FromRgba(139, 69, 19, 255);

    [LuaMember("SALMON")]
    public static readonly LuaColor Salmon = FromRgba(250, 128, 114, 255);

    [LuaMember("SANDY_BROWN")]
    public static readonly LuaColor SandyBrown = FromRgba(244, 164, 96, 255);

    [LuaMember("SEA_GREEN")]
    public static readonly LuaColor SeaGreen = FromRgba(46, 139, 87, 255);

    [LuaMember("SEA_SHELL")]
    public static readonly LuaColor SeaShell = FromRgba(255, 245, 238, 255);

    [LuaMember("SIENNA")]
    public static readonly LuaColor Sienna = FromRgba(160, 82, 45, 255);

    [LuaMember("SILVER")]
    public static readonly LuaColor Silver = FromRgba(192, 192, 192, 255);

    [LuaMember("SKY_BLUE")]
    public static readonly LuaColor SkyBlue = FromRgba(135, 206, 235, 255);

    [LuaMember("SLATE_BLUE")]
    public static readonly LuaColor SlateBlue = FromRgba(106, 90, 205, 255);

    [LuaMember("SLATE_GRAY")]
    public static readonly LuaColor SlateGray = FromRgba(112, 128, 144, 255);

    [LuaMember("SLATE_GREY")]
    public static readonly LuaColor SlateGrey = SlateGray;

    [LuaMember("SNOW")]
    public static readonly LuaColor Snow = FromRgba(255, 250, 250, 255);

    [LuaMember("SPRING_GREEN")]
    public static readonly LuaColor SpringGreen = FromRgba(0, 255, 127, 255);

    [LuaMember("STEEL_BLUE")]
    public static readonly LuaColor SteelBlue = FromRgba(70, 130, 180, 255);

    [LuaMember("TAN")]
    public static readonly LuaColor Tan = FromRgba(210, 180, 140, 255);

    [LuaMember("TEAL")]
    public static readonly LuaColor Teal = FromRgba(0, 128, 128, 255);

    [LuaMember("THISTLE")]
    public static readonly LuaColor Thistle = FromRgba(216, 191, 216, 255);

    [LuaMember("TOMATO")]
    public static readonly LuaColor Tomato = FromRgba(255, 99, 71, 255);

    [LuaMember("TRANSPARENT")]
    public static readonly LuaColor Transparent = FromRgba(0, 0, 0, 0);

    [LuaMember("TURQUOISE")]
    public static readonly LuaColor Turquoise = FromRgba(64, 224, 208, 255);

    [LuaMember("VIOLET")]
    public static readonly LuaColor Violet = FromRgba(238, 130, 238, 255);

    [LuaMember("WHEAT")]
    public static readonly LuaColor Wheat = FromRgba(245, 222, 179, 255);

    [LuaMember("WHITE")]
    public static readonly LuaColor White = FromRgba(255, 255, 255, 255);

    [LuaMember("WHITE_SMOKE")]
    public static readonly LuaColor WhiteSmoke = FromRgba(245, 245, 245, 255);

    [LuaMember("YELLOW")]
    public static readonly LuaColor Yellow = FromRgba(255, 255, 0, 255);

    [LuaMember("YELLOW_GREEN")]
    public static readonly LuaColor YellowGreen = FromRgba(154, 205, 50, 255);
}
