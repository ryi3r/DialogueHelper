using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;

namespace DialogueHelper.Views;

public partial class AboutDialogueHelper : Window
{
    public AboutDialogueHelper()
    {
        InitializeComponent();
        Version.Text = typeof(AboutDialogueHelper).Assembly.GetName().Version!.ToString();
    }

    void Close_OnClick(object? sender, RoutedEventArgs e) => Close();

    void OpenProgRepo_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = "https://git.crystal-server.co/ryi3r/DialogueHelper",
            UseShellExecute = true,
        });
    }
}
