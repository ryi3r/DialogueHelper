using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Lua;

namespace DialogueHelper.Views;

[LuaObject]
public partial class LoadingWindow : LuaWindow
{
    public LoadingWindow()
    {
        InitializeComponent();
    }

    [LuaMember("create")]
    public static LoadingWindow Create() => new();

    [LuaMember("set_progress_text")]
    public void SetProgressText(string text) => Text.Text = text;

    [LuaMember("show_progress_percentage")]
    public void ShowProgressPercentage(bool show) => ProgressBar.ShowProgressText = show;

    [LuaMember("set_progress_value")]
    public void SetProgressValue(double value) => ProgressBar.Value = value;

    [LuaMember("set_progress_max_value")]
    public void SetProgressMaxValue(double value) => ProgressBar.Maximum = value;

    [LuaMember("set_progress_min_value")]
    public void SetProgressMinValue(double value) => ProgressBar.Minimum = value;

    [LuaMember("set_progress_indeterminate")]
    public void SetProgressIndeterminate(bool indeterminate) => ProgressBar.IsIndeterminate = indeterminate;
}