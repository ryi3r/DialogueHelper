using System;
using System.Globalization;
using Avalonia.Controls;
using Lua;

namespace DialogueHelper.External;

[LuaObject]
public partial class CustomProperty(string visualName, string name, string type, dynamic? defaultValue = null, bool readOnly = false)
{
    [LuaMember("name")]
    public readonly string Name = name;
    [LuaMember("visual_name")]
    public readonly string VisualName = visualName;
    [LuaMember("value_type")]
    public readonly string ValueType = type;
    [LuaMember("value")]
    public dynamic? Value = defaultValue;
    [LuaMember("default_value")]
    public readonly dynamic? DefaultValue = defaultValue;
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    [LuaMember("readonly")]
    public bool ReadOnly = readOnly;
    public Control? Node;

    [LuaMember("create")]
    public static CustomProperty Create(string visualName, string name, string type, dynamic? defaultValue = null, bool readOnly = false) => new(visualName, name, type, defaultValue, readOnly);

    [LuaMember("update_ui_value")]
    public void UpdateUiValue()
    {
        if (Node == null)
            return;
        switch (Node)
        {
            case CheckBox cb:
                cb.IsEnabled = !ReadOnly;
                cb.IsChecked = (bool)(Value ?? false);
                break;
            case NumericUpDown nud:
                nud.IsReadOnly = ReadOnly;
                nud.Value = (decimal)(Value ?? 0);
                break;
            case TextBox tb:
                tb.IsReadOnly = ReadOnly;
                tb.Text = (string)(Value ?? "");
                break;
        }
    }

    [LuaMember("string_to_value")]
    public void StringToValue(string value)
    {
        if (value.Length <= 0)
        {
            Value = null;
            return;
        }

        Value = ValueType switch
        {
            "boolean" => bool.Parse(value),
            "number" => double.Parse(value),
            "integer" => long.Parse(value),
            "string" => value,
            "nil" => null,
            _ => throw new NotSupportedException($"{ValueType}")
        };
    }

    [LuaMember("value_to_string")]
    public string ValueToString() => Value is null ? "" : Value.ToString(CultureInfo.InvariantCulture);
}
