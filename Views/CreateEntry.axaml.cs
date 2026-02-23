using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DialogueHelper.Views;

public partial class CreateEntry : Window
{
    public bool AcceptEmpty;
    
    public CreateEntry()
    {
        InitializeComponent();
    }

    void FinishButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if ((TextBox.Text ?? "").Length > 0 || AcceptEmpty)
            Close(true);
    }

    void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
