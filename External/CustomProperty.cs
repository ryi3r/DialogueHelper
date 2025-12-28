using System;
using System.Globalization;
using Avalonia.Controls;

namespace DialogueHelper.External;

public class CustomProperty(string visualName, string name, Type type, dynamic? defaultValue = null, bool readOnly = false)
{
    public readonly string Name = name;
    public readonly string VisualName = visualName;
    public readonly Type ValueType = type;
    public dynamic? Value = defaultValue;
    public readonly dynamic? DefaultValue = defaultValue;
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
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
            not null when ValueType == typeof(bool) => value == "true",
            not null when ValueType == typeof(byte) => byte.Parse(value),
            not null when ValueType == typeof(short) => short.Parse(value),
            not null when ValueType == typeof(int) => int.Parse(value),
            not null when ValueType == typeof(long) => long.Parse(value),
            not null when ValueType == typeof(sbyte) => sbyte.Parse(value),
            not null when ValueType == typeof(ushort) => ushort.Parse(value),
            not null when ValueType == typeof(uint) => uint.Parse(value),
            not null when ValueType == typeof(ulong) => ulong.Parse(value),
            not null when ValueType == typeof(float) => float.Parse(value),
            not null when ValueType == typeof(double) => double.Parse(value),
            not null when ValueType == typeof(string) => value,
            _ => throw new NotSupportedException($"{ValueType}")
        };
    }

    public string ValueToString()
    {
        if (Value == null)
            return "";

        return ValueType switch
        {
            not null when ValueType == typeof(bool) => (bool)Value ? "true" : "false",
            _ => Value.ToString()!,
        };
    }
}
