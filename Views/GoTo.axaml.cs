using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DialogueHelper.Views;

public partial class GoTo : Window
{
    public readonly MainWindow? MainWindow;
    
    public GoTo()
    {
        InitializeComponent();
    }
    
    public GoTo(MainWindow mainWindow)
    {
        MainWindow = mainWindow;
        InitializeComponent();
    }

    void GoToEntry_OnClick(object? sender, RoutedEventArgs e)
    {
        var num = SubentryIndex.Text ?? "";
        if (num.Length <= 0)
            num = "0";
        if (int.TryParse(num, out var index))
        {
            if (index < 0)
                return;
            if (MainWindow is { FileData: not null })
            {
                var key = EntryName.Text ?? "";
                if (MainWindow.FileData.Strings.TryGetValue(key, out var entry))
                {
                    var parent = MainWindow.EtvKeys.First(v => v.Value == key).Key;
                    var node = MainWindow.EtvTargetChildren[parent];
                    if (index >= entry.Count)
                        return;
                    Close();
                    MainWindow.EntryTreeView.ExpandSubTree(parent);
                    MainWindow.EntryTreeView.SelectedItem = node[index];
                    MainWindow.EntryTreeView.ScrollIntoView(MainWindow.EntryTreeView.SelectedItem);
                    node[index].Focus();
                    MainWindow.EntryTreeView_OnSelectionChanged(null, null);
                }
            }
        }
    }
}
