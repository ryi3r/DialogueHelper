using Avalonia.Controls;
using Avalonia.Interactivity;
using Lua;

namespace DialogueHelper.Views;

[LuaObject]
public partial class QuestionWindow : LuaWindow
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

    void YesButton_OnClick(object? sender, RoutedEventArgs e) => Close(true);

    void NoButton_OnClick(object? sender, RoutedEventArgs e) => Close(false);

    [LuaMember("create")]
    public static QuestionWindow Create() => new();

    [LuaMember("set_question_text")]
    public void SetQuestionText(string text) => InnerText.Text = text;

    [LuaMember("set_yes_button_text")]
    public void SetYesButtonText(string text) => YesButton.Content = text;

    [LuaMember("set_no_button_text")]
    public void SetNoButtonText(string text) => NoButton.Content = text;
}
