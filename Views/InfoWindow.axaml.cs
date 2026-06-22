using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DialogueHelper.Views;

public partial class InfoWindow : Window
{
    public TextBlock TitleBlock => __InnerTitle;
    public TextBlock TextBlock => __InnerText;

    public StackPanel Panel => __InnerPanel;

    public InfoWindow() => InitializeComponent();

    public InfoWindow(string text) : this() => TextBlock.Text = text;

    void OkButton_OnClick(object? sender, RoutedEventArgs e) => Close();
}
