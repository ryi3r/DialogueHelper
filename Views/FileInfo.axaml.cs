using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DialogueHelper.Views;

public partial class FileInfo : Window
{
    public FileInfo()
    {
        InitializeComponent();
    }

    void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
