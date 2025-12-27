using System;
using System.Globalization;
using Avalonia.Controls;

namespace DialogueHelper.External;

public class CustomProperty(string visualName, string name, string type, dynamic? defaultValue = null, bool readOnly = false)
{
    public readonly string Name = name;
    public readonly string VisualName = visualName;
    public readonly string ValueType = type;
    public dynamic? Value = defaultValue;
    public readonly dynamic? DefaultValue = defaultValue;
    public bool ReadOnly = readOnly;
    public Control? Node;

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

    public string ValueToString() => Value is null ? "" : Value.ToString(CultureInfo.InvariantCulture);
}
