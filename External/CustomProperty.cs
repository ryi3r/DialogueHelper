using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;

namespace DialogueHelper.External;

public interface ICustomProperty
{
    public string Name { get; }
    public string VisualName { get; }
    public string? Tooltip { get; }
    public bool ReadOnly { get; set; }
    public object? Value { get; set; }
    public object? DefaultValue { get; }
    public Type Type { get; }

    public Control? Node { get; set; }

    public void UpdateUiValue();
    public void StringToValue(string value);
    public string ValueToString();
}

public class CustomProperty<T>(string visualName, string? tooltip, string name, T? defaultValue = default, bool readOnly = false) : ICustomProperty
    where T : IParsable<T>
{
    public string Name { get; } = name;
    public string VisualName { get; set; } = visualName;
    public string? Tooltip { get; set; } = tooltip;
    public bool ReadOnly { get; set; } = readOnly;
    object? ICustomProperty.Value
    {
        get => Value;
        set => Value = (T?)value;
    }
    object? ICustomProperty.DefaultValue => DefaultValue;
    public Type Type { get; } = typeof(T);
    public Control? Node { get; set; }
    public T? Value = defaultValue;
    public readonly T? DefaultValue = defaultValue;

    public virtual void UpdateUiValue()
    {
        if (Node is null)
            return;
        switch (Node)
        {
            case CheckBox cb:
                cb.IsEnabled = !ReadOnly;
                cb.IsChecked = ((bool?)(object?)Value) ?? false;
                break;
            case NumericUpDown nud:
                nud.IsReadOnly = ReadOnly;
                nud.Value = ((decimal?)(object?)Value) ?? 0;
                break;
            case TextBox tb:
                tb.IsReadOnly = ReadOnly;
                tb.Text = ((string?)(object?)Value) ?? "";
                break;
            case ColorPicker col:
                col.IsEnabled = !ReadOnly;
                if (Value is Color c)
                {
                    col.Color = new(c.R, c.G, c.B, c.A);
                    col.IsAlphaEnabled = c.IsAlphaEnabled;
                    col.IsAlphaVisible = c.IsAlphaVisible;
                }
                else
                    col.Color = new(0, 0, 0, 0xff);
                break;
        }
    }

    public virtual void StringToValue(string value)
    {
        if (value.Length <= 0)
        {
            Value = default;
            return;
        }

        Value = T.Parse(value, CultureInfo.InvariantCulture);
        //throw new NotSupportedException($"unsupported str2val type: {typeof(T)}");
    }

    public virtual string ValueToString()
    {
        if (Value is null)
            return "";
        if (Value is Color c)
            return c.Rgba.ToString();
        return Value.ToString() ?? "";
    }
}

public interface ICustomPropertyList : ICustomProperty
{
    IList? Data { get; }
    List<string?>? ItemTooltips { get; }
    int SelectedIndex { get; set; }
    int DefaultIndex { get; }
}

public class CustomPropertyList<T>(string visualName, string? tooltip, string name, List<T> data, List<string?>? itemTooltips, T? defaultValue = default, int defaultIndexValue = -1, bool readOnly = false) : CustomProperty<int>(visualName, tooltip, name, -1, readOnly), ICustomPropertyList
    where T : IParsable<T>
{
    IList? ICustomPropertyList.Data => Data;
    List<string?>? ICustomPropertyList.ItemTooltips => itemTooltips;
    int ICustomPropertyList.DefaultIndex => DefaultValue is null ? -1 : Data.IndexOf(DefaultValue);

    public List<T> Data = data;
    // todo: custom enabled list (enable/disable certain fields)
    public new T? DefaultValue { get; } = defaultIndexValue >= 0 && defaultIndexValue < data.Count ? data[defaultIndexValue] : defaultValue;
    public new T? Value
    {
        get => SelectedIndex < 0 || SelectedIndex >= Data.Count ? default : Data[SelectedIndex];
        set => SelectedIndex = value is null ? -1 : Data.IndexOf(value);
    }
    public int SelectedIndex
    {
        get;
        set
        {
            field = value;
            base.Value = field;
        }
    } = defaultIndexValue;
    public new Type Type => typeof(ICustomPropertyList);

    public override void UpdateUiValue()
    {
        if (Node is ComboBox cb)
        {
            cb.IsEnabled = !ReadOnly;
            cb.ItemsSource = Data;
            cb.SelectedItem = Value;
        }
        else
            base.UpdateUiValue();
    }

    public override void StringToValue(string value)
    {
        if (value.Length <= 0)
        {
            SelectedIndex = -1;
            return;
        }
        SelectedIndex = int.Parse(value, CultureInfo.InvariantCulture);
    }

    public override string ValueToString()
    {
        return SelectedIndex.ToString(CultureInfo.InvariantCulture);
    }
}
