using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DialogueHelper.FileFormat;

namespace DialogueHelper.Views;

public partial class GoTo : Window
{
    public readonly MainWindow? MainWindow;

    public GoTo() => InitializeComponent();

    public GoTo(MainWindow mainWindow) : this() => MainWindow = mainWindow;

    async void GoToEntry_OnClick(object? sender, RoutedEventArgs e)
    {
        var num = SubentryIndex.Text ?? "";
        if (num.Length <= 0)
            num = "0";
        if (int.TryParse(num, out var numIdx))
        {
            if (numIdx < 0)
                return;
            StringContainer? idx = null;
            if (MainWindow is { FileData: not null })
            {
                var key = EntryName.Text ?? "";
                if (key.Length == 0)
                {
                    foreach (var (sKey, scs) in MainWindow.FileData.GetStringKeysValues())
                    {
                        var found = false;
                        foreach (var k in scs.OrderBy(x => x.Id))
                        {
                            if (k.Id == numIdx)
                            {
                                key = sKey;
                                idx = k;
                                found = true;
                                break;
                            }
                        }
                        if (found)
                            break;
                    }
                }
                if (MainWindow.FileData.TryGetStringValue(key, out var entry))
                {
                    var parent = MainWindow.EtvKeys.First(v => v.Value == key).Key;
                    var node = MainWindow.EtvTargetChildren[parent];
                    var n = idx is not null ? node[idx] : node.OrderBy(x => x.Key.Id).ElementAtOrDefault(numIdx).Value;
                    if (n is not null)
                    {
                        // todo: this doesn't work properly due to an avalonia bug
                        MainWindow.EntryTreeView.ExpandSubTree(parent);
                        MainWindow.EntryTreeView.ScrollIntoView(n);
                        n.Focus();
                        MainWindow.EntryTreeView.SelectedItem = n;
                        MainWindow.EntryTreeView_OnSelectionChanged(null, null);
                        Close();
                    }
                }
            }
        }
    }
}
