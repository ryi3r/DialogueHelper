using System.Collections.Generic;
using System.Text.Json;

namespace DialogueHelper.StylesParser;

public class BoxMetadata
{
    public required string Name;
    public required List<ImageMetadata> Images;
    public float Scale = 1;
    public Dictionary<string, JsonElement> CustomProperties = [];
    public float[] TextOffset = [0, 0];
    public float Index = float.MaxValue;
}

public class ImageMetadata
{
    public required string Path;
    public string Name = "";
    public float[] Position = [0, 0];
    public float[] Scale = [1, 1];
}
