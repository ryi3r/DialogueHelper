using System.Collections.Generic;
using Lua;

namespace DialogueHelper.StylesParser;

[LuaObject]
public partial class BoxMetadata
{
    [LuaMember("name")]
    public required string Name;
    /// <summary>
    /// ImageMetadata
    /// </summary>
    [LuaMember("images")]
    public required LuaList Images;
    [LuaMember("scale")]
    public float Scale = 1.0f;
    [LuaMember("custom_properties")]
    public LuaTable CustomProperties = [];
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    [LuaMember("text_offset_x")]
    public float TextOffsetX => TextOffset[0];
    [LuaMember("text_offset_y")]
    public float TextOffsetY => TextOffset[1];
    public float[] TextOffset = [0.0f, 0.0f];
}

[LuaObject]
public partial class ImageMetadata
{
    // ReSharper disable once UnassignedField.Global
    [LuaMember("path")]
    public required string Path;
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    [LuaMember("position_x")]
    public float PositionX => Position[0];
    [LuaMember("position_y")]
    public float PositionY => Position[1];
    public float[] Position = [0.0f, 0.0f];
    [LuaMember("scale_x")]
    public float ScaleX => Scale[0];
    [LuaMember("scale_y")]
    public float ScaleY => Scale[1];
    public float[] Scale = [1.0f, 1.0f];
}

public partial class BoxMetadataJson
{
    public required string Name;
    public required List<ImageMetadata> Images;
    public float Scale = 1.0f;
    public Dictionary<string, object> CustomProperties = [];
    public float[] TextOffset = [0.0f, 0.0f];
}
