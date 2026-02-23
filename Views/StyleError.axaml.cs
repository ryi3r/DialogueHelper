using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace DialogueHelper.Views;

public partial class StyleError : Window
{
    public StyleError()
    {
        InitializeComponent();
        Closing += (sender, args) =>
        {
            if (!args.IsProgrammatic)
                args.Cancel = true;
        };
    }

    void Ok_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
