using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DialogueHelper.Views;

public partial class LoadingWindow : Window
{
    public LoadingWindow()
    {
        InitializeComponent();
    }

    public TextBlock TextView => this.Text;
    public ProgressBar ProgressBarView => this.ProgressBar;
}