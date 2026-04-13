using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace DialogueHelper.Views;

public partial class GroupEditor : Window
{
    public MainWindow MainWindow = null!;
    
    public GroupEditor() => InitializeComponent();

    public GroupEditor(MainWindow mainWindow) : this()
    {
        MainWindow = mainWindow;
        
    }

    void AddGroup_OnClick(object? sender, RoutedEventArgs e)
    {
        
    }

    private void RemoveGroup_OnClick(object? sender, RoutedEventArgs e)
    {
        
    }
}