using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DialogueHelper.Views;

public partial class InfoWindow : Window
{
    public readonly TextBlock TitleBlock;
    public readonly TextBlock TextBlock;

    public InfoWindow()
    {
        InitializeComponent();
        TitleBlock = InnerTitle;
        TextBlock = InnerText;
    }

    public InfoWindow(string text)
    {
        InitializeComponent();
        TitleBlock = InnerTitle;
        TextBlock = InnerText;
        TextBlock.Text = text;
    }

    void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    public void SetTitleBlockText(string text) => TitleBlock.Text = text;

    public void SetTextBlockText(string text) => TextBlock.Text = text;
}
