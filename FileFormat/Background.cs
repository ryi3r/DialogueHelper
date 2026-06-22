using System.Collections.Generic;

public class BackgroundConfig
{
    public required string Name;
    public required string Author;
    public string BgndColor = "#000000";
    public List<BackgroundImage> Images = [];
}

public class BackgroundImage
{
    public required string Image;
    public float Opacity = 0.8f;

    public float X = 1; // pos 0 to 1
    public float Y = 1; // pos 0 to 1

    public float W = 1; // scale x
    public float H = 1; // scale y

    public float? ClientX; // pos 0 to 1
    public float? ClientY; // pos 0 to 1

    public float? ClientW; // scale x
    public float? ClientH; // scale y

    public string StretchDirection = "Both";
    public string StretchMode = "None";
    public string BlendMode = "SourceOver";
    public string Interpolation = "Default";
}
