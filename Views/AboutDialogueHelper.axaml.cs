using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DialogueHelper.Views;

public partial class AboutDialogueHelper : Window
{
    public AboutDialogueHelper()
    {
        InitializeComponent();
        var ver = typeof(AboutDialogueHelper).Assembly.GetName().Version!;
        Version.Text = $"{ver.Major}.{ver.Minor}";
        if (ver.Revision > 0 || ver.Build > 0)
        {
            Version.Text += $".{ver.Build}";
            if (ver.Revision > 0)
                Version.Text += $".{ver.Revision}";
        }
    }

    void Close_OnClick(object? sender, RoutedEventArgs e) => Close();

    void OpenProgRepo_OnClick(object? sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo()
    {
        FileName = "https://git.crystal-server.net/ryi3r/DialogueHelper",
        UseShellExecute = true,
    });
}
