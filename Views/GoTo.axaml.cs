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
            num = "0";
        if (int.TryParse(num, out var index))
        {
            if (index < 0)
                return;
            if (MainWindow is { FileData: not null })
            {
                var key = EntryName.Text ?? "";
                if (key.Length == 0)
                {
                    if (!MainWindow.FileData.StringIds.ContainsKey(index))
                        return;
                    foreach (var (sKey, scs) in MainWindow.FileData.Strings)
                    {
                        if (!scs.ContainsKey(index))
                            continue;
                        key = sKey;
                        index = scs.OrderBy(x => x.Key).Index().First(x => x.Item.Key == index).Index;
                        break;
                    }
                }
                if (MainWindow.FileData.Strings.TryGetValue(key, out var entry))
                {
                    var parent = MainWindow.EtvKeys.First(v => v.Value == key).Key;
                    var node = MainWindow.EtvTargetChildren[parent];
                    if (index >= entry.Count)
                        return;
                    Close();
                    var n = node.OrderBy(x => x.Key).ElementAt(index).Value;
                    // todo: this doesn't work properly due to an avalonia bug
                    MainWindow.EntryTreeView.ExpandSubTree(parent);
                    MainWindow.EntryTreeView.ScrollIntoView(n);
                    n.Focus();
                    MainWindow.EntryTreeView.SelectedItem = n;
                    MainWindow.EntryTreeView_OnSelectionChanged(null, null);
                }
            }
        }
    }
}
