using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DialogueHelper.Views;

public partial class QuestionWindow : Window
{
    public StackPanel Panel => __InnerPanel;
    public TextBlock Text => __InnerText;

    public Button YesButton => __InnerYesButton;
    public Button NoButton => __InnerNoButton;

    /// <summary>
    /// input arg will be true if clicked OK otherwise clicked the NO button
    /// </summary>
    public Func<bool, bool>? ValidityCheck = null;

    public QuestionWindow()
    {
        InitializeComponent();
        Closing += (_, args) =>
        {
            if (!args.IsProgrammatic)
                args.Cancel = true;
        };
    }

    public QuestionWindow(string question) : this() => Text.Text = question;

    void YesButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ValidityCheck is null || ValidityCheck(true))
            Close(true);
    }

    void NoButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ValidityCheck is null || ValidityCheck(false))
            Close(false);
    }
}
