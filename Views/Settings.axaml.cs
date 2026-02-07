using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DialogueHelper.External;
using DialogueHelper.StylesParser;

namespace DialogueHelper.Views;

public partial class Settings : Window
{
    public readonly MainWindow? MainWindow;
    public readonly List<StyleParser> StyleData = [];
    public List<string> IgnoreStyles = [];

    public bool Init;

    public Settings()
    {
        InitializeComponent();
    }

    public Settings(MainWindow mainWindow)
    {
        MainWindow = mainWindow;

        InitializeComponent();

        AuthorGroup.Text = mainWindow.AuthorGroup;
        AuthorName.Text = mainWindow.AuthorName;

        EnableGit.IsChecked = mainWindow.GitOptions.IsEnabled;
        RepoUrl.Text = mainWindow.GitOptions.RepoUrl;
        SelectedBranch.Text = mainWindow.GitOptions.Branch;

        StyleComboBox.Items.Clear();
    }

    void Ok_OnClick(object? sender, RoutedEventArgs e)
    {
        if (StyleComboBox.SelectedItem is null)
            return;
        if ((bool)EnableGit.IsChecked!)
        {
            if (RepoUrl.Text!.Length <= 0)
                return;
        }
        if ((AuthorName.Text ?? "").Length <= 0)
            return;

        if (MainWindow is not null)
        {
            MainWindow.GitOptions.IsEnabled = (bool)EnableGit.IsChecked;
            MainWindow.GitOptions.RepoUrl = RepoUrl.Text!;
            MainWindow.GitOptions.Branch = SelectedBranch.Text!;

            MainWindow.AuthorGroup = AuthorGroup.Text!;
            MainWindow.AuthorName = AuthorName.Text!;

            var selStyle = StyleData[StyleComboBox.SelectedIndex];
            MainWindow.ScriptNeedsInitialization = false;
            if (MainWindow.SelectedStyle != selStyle.Folder)
            {
                MainWindow.StyleData = selStyle;
                MainWindow.SelectedStyle = selStyle.Folder;
                MainWindow.ScriptNeedsInitialization = true;
            }
            MainWindow.SaveSettings();
        }
        Close();
    }

    void Cancel_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!Init)
            Close();
    }

    async void OnOpened(object? sender, EventArgs e)
    {
        if (Init)
        {
            Closing += (_, args) =>
            {
                if (!args.IsProgrammatic)
                    args.Cancel = true;
            };
        }

        foreach (var folder in Directory.EnumerateDirectories("Styles"))
        {
            if (IgnoreStyles.Contains(folder))
                continue;
            try
            {
                var style = new StyleParser(folder);
                await Task.Run(() => style.CompileCode());
                StyleData.Add(style);
                StyleComboBox.Items.Add(new TextBlock()
                {
                    Text = $"{style.Metadata.Name} by {style.Metadata.Author}",
                });
                if (!MainWindow!.StyleSettings.ContainsKey(folder))
                    MainWindow.StyleSettings.Add(folder, []);
                var res = await (style.Script?.RegisterCustomSettings() ?? Task.FromResult(Enumerable.Empty<CustomProperty>()));
                var props = MainWindow.StyleSettings[folder];
                foreach (var prop in res)
                {
                    var p = props.FirstOrDefault(p => p.Name == prop.Name);
                    if (p is not null)
                    {
                        var val = p.ValueToString();
                        if (val.Length > 0)
                            prop.StringToValue(val);
                        var ind = props.IndexOf(p);
                        props.RemoveAt(ind);
                        props.Insert(ind, prop);
                    }
                    else
                        props.Add(prop);
                }
            }
            catch (Exception ex)
            {
                var sErr = new StyleError
                {
                    ErrorBox =
                    {
                        Text = ex.ToString()
                    },
                };
                sErr.MessageBlock.Text = sErr.MessageBlock.Text!.Replace("StyleName", folder);
                await sErr.ShowDialog(this);
            }
        }

        if (MainWindow is not null)
        {
            StyleComboBox.SelectionChanged += (_, _) =>
            {
                var ind = StyleComboBox.SelectedIndex;
                if (ind < 0 || ind >= StyleData.Count)
                    return;
                CustomSettingsPanel.Children.Clear();
                var sel = StyleData[ind];
                var set = MainWindow.StyleSettings[sel.Folder];
                CustomSettingsNone.IsVisible = set.Count <= 0;
                MainWindow!.UpdateCustomProperties(CustomSettingsPanel, set);
            };
            if (MainWindow.SelectedStyle is not null)
            {
                var styleIndex = StyleData.FindIndex(style => style.Folder == MainWindow.SelectedStyle);
                if (styleIndex != -1)
                    StyleComboBox.SelectedIndex = styleIndex;
            }
        }
    }
}
