using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DialogueHelper.Views;

public partial class GoTo : Window
{
    public readonly MainWindow? MainWindow;

    public GoTo() => InitializeComponent();

    public GoTo(MainWindow mainWindow) : this() => MainWindow = mainWindow;

    void GoToEntry_OnClick(object? sender, RoutedEventArgs e)
    {
        var num = SubentryIndex.Text ?? "";
        if (num.Length <= 0)
            num = "1";
        if (int.TryParse(num, out var index))
        {
            if (index <= 0)
                return;
            if (MainWindow is { FileData: not null })
            {
                var key = EntryName.Text ?? "";
                if (MainWindow.FileData.Strings.TryGetValue(key, out var entry))
                {
                    var parent = MainWindow.EtvKeys.First(v => v.Value == key).Key;
                    var node = MainWindow.EtvTargetChildren[parent];
                    if (index - 1 >= entry.Count)
                        return;
                    Close();
                    var n = node.ElementAt(index - 1);
                    // todo: this doesn't work properly due to an avalonia bug
                    MainWindow.EntryTreeView.ExpandSubTree(parent);
                    MainWindow.EntryTreeView.ScrollIntoView(n);
                    n.Value.Focus();
                    MainWindow.EntryTreeView.SelectedItem = n;
                    MainWindow.EntryTreeView_OnSelectionChanged(null, null);
                }
            }
        }
    }
}
