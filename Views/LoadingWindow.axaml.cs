using Avalonia.Controls;

namespace DialogueHelper.Views;

public partial class LoadingWindow : Window
{
    public LoadingWindow() => InitializeComponent();

    public TextBlock Text => __InnerText;
    public ProgressBar ProgressBar => __InnerProgressBar;
}