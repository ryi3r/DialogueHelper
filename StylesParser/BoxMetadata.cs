using System.Collections.Generic;
using Lua;

namespace DialogueHelper.StylesParser;

[LuaObject]
public partial class BoxMetadata
{
    [LuaMember("name")]
    public required string Name;
    public required List<ImageMetadata> Images;
    [LuaMember("scale")]
    public float Scale = 1.0f;
    public Dictionary<string, dynamic> CustomProperties = [];
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    [LuaMember("text_offset_x")]
    public float TextOffsetX => TextOffset[0];
    [LuaMember("text_offset_y")]
    public float TextOffsetY => TextOffset[1];
    public float[] TextOffset = [0.0f, 0.0f];

    [LuaMember("add_custom_properties_entry")]
    public void AddCustomPropertiesEntry(string key, ImageMetadata img) => CustomProperties.Add(key, img);

    [LuaMember("get_custom_properties_entry")]
    public ImageMetadata? GetCustomPropertiesEntry(string key) => CustomProperties.TryGetValue(key, out var v) ? v : null;

    [LuaMember("remove_custom_properties_entry")]
    public void RemoveCustomPropertiesEntry(string key) => CustomProperties.Remove(key);

    [LuaMember("add_images_entry")]
    public void AddImagesEntry(ImageMetadata img) => Images.Add(img);

    [LuaMember("get_images_entry")]
    public ImageMetadata GetImagesEntry(int img) => Images[img - 1];

    [LuaMember("remove_images_entry")]
    public void RemoveImagesEntry(int img) => Images.RemoveAt(img - 1);
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
