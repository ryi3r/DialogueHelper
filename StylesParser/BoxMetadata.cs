using System.Collections.Generic;
using System.Numerics;

namespace DialogueHelper.StylesParser;


public partial class BoxMetadata
{
    public required string Name;
    public required List<ImageMetadata> Images;
    public float Scale = 1;
    public Dictionary<string, object?> CustomProperties = [];
    public float[] TextOffset = [1, 1];
}

public partial class ImageMetadata
{
    public required string Path;
    public float[] Position = [0, 0];
    public float[] Scale = [1, 1];
}
