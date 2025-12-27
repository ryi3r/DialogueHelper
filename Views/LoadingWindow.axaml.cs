using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DialogueHelper.Views;

public partial class LoadingWindow : Window
{
    public LoadingWindow()
    {
        InitializeComponent();
    }

    public void SetProgressText(string text) => Text.Text = text;

    public void ShowProgressPercentage(bool show) => ProgressBar.ShowProgressText = show;

    public void SetProgressValue(double value) => ProgressBar.Value = value;

    public void SetProgressMaxValue(double value) => ProgressBar.Maximum = value;

    public void SetProgressMinValue(double value) => ProgressBar.Minimum = value;

    public void SetProgressIndeterminate(bool indeterminate) => ProgressBar.IsIndeterminate = indeterminate;
}