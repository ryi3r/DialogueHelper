using Avalonia.Controls;

namespace DialogueHelper.Views;

public partial class LoadingWindow : Window
{
    public LoadingWindow() => InitializeComponent();

    public TextBlock TextView => Text;
    public ProgressBar ProgressBarView => ProgressBar;
}