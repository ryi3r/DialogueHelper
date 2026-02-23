using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DialogueHelper.Views;

public partial class EntryInfo : Window
{
    public EntryInfo()
    {
        InitializeComponent();
    }
    
    void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
