using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Views;

public partial class QuestionNumberInputWindow : Window
{
    public TextBlock TextBlock => __InnerText;

    public TextBlock NumberText => __InnerNumberText;
    public NumericUpDown NumberSel => __InnerNumberSel;

    public StackPanel Panel => __InnerPanel;

    public Func<bool>? ValidityCheck = null;

    public QuestionNumberInputWindow() => InitializeComponent();

    public QuestionNumberInputWindow(string questionText, string numText) : this()
    {
        TextBlock.Text = questionText;
        NumberSel.Text = numText;
    }

    void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ValidityCheck is null || ValidityCheck())
            Close(true);
    }
}