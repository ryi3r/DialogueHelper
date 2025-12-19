using Avalonia.Controls;
using Avalonia.Interactivity;
using Lua;

namespace DialogueHelper.Views;

[LuaObject]
public partial class InfoWindow : LuaWindow
{
    public readonly TextBlock TitleBlock;
    public readonly TextBlock TextBlock;

    public InfoWindow()
    {
        InitializeComponent();
        TitleBlock = InnerTitle;
        TextBlock = InnerText;
    }

    public InfoWindow(string text)
    {
        InitializeComponent();
        TitleBlock = InnerTitle;
        TextBlock = InnerText;
        TextBlock.Text = text;
    }

    void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    [LuaMember("create_empty")]
    public static InfoWindow CreateEmpty() => new();

    [LuaMember("create")]
    public static InfoWindow Create(string text) => new(text);

    [LuaMember("set_title_block_text")]
    public void SetTitleBlockText(string text) => TitleBlock.Text = text;

    [LuaMember("set_text_block_text")]
    public void SetTextBlockText(string text) => TextBlock.Text = text;
}
