using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DialogueHelper.Views;

public partial class UnsavedChanges : Window
{
    public UnsavedChanges()
    {
        InitializeComponent();
        Closing += (_, ev) =>
        {
            if (!ev.IsProgrammatic)
                ev.Cancel = true;
        };
    }

    void ReturnToApp_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    void IgnoreWarning_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
