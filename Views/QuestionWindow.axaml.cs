using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DialogueHelper.Views;

public partial class QuestionWindow : Window
{
    public QuestionWindow()
    {
        InitializeComponent();
        Closing += (_, args) =>
        {
            if (!args.IsProgrammatic)
                args.Cancel = true;
        };
    }

    void YesButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    void NoButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
