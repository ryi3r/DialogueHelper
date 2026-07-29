using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using DialogueHelper.External;
using DialogueHelper.StylesParser;

namespace DialogueHelper.Views;

public partial class Settings : Window
{
    public readonly MainWindow? MainWindow;
    public readonly List<StyleParser> StyleData = [];
    public readonly List<string> BackgroundData = [];
    public List<string> IgnoreStyles = [];
    
    const string UseProgramDefaultLabel = "Use program default";

    public bool Init;

    public Settings() => InitializeComponent();

    public Settings(MainWindow mainWindow)
    {
        MainWindow = mainWindow;

        InitializeComponent();

        AuthorGroup.Text = mainWindow.AuthorGroup;
        AuthorName.Text = mainWindow.AuthorName;

        EnableGit.IsChecked = mainWindow.GitOptions.IsEnabled;
        RepoUrl.Text = mainWindow.GitOptions.RepoUrl;
        SelectedBranch.Text = mainWindow.GitOptions.Branch;
        GroupOnlyEntries.IsChecked = mainWindow.ShowOnlyGroupEntries;
        
        UiFontComboBox.ItemsSource = new[] { UseProgramDefaultLabel }.Concat(FontManager.Current.SystemFonts.Select(f => f.Name).OrderBy(n => n).ToList()).ToList();
        UiFontComboBox.SelectedItem = mainWindow.UiFontFamily ?? UseProgramDefaultLabel;
        UiFontSizeBox.Value = (decimal)mainWindow.UiFontSize;
        AffPathBox.Text = mainWindow.DictionaryAffPath;
        DicPathBox.Text = mainWindow.DictionaryDicPath;
        UpdateDownloadStatusIcons();

        StyleComboBox.Items.Clear();
        BackgroundComboBox.Items.Clear();
    }

    async void Ok_OnClick(object? sender, RoutedEventArgs e)
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
            MainWindow.ShowOnlyGroupEntries = GroupOnlyEntries.IsChecked ?? false;

            var selectedFont = UiFontComboBox.SelectedItem as string;
            MainWindow.UiFontFamily = selectedFont == UseProgramDefaultLabel ? null : selectedFont;
            MainWindow.UiFontSize = (double)(UiFontSizeBox.Value ?? 14);
            MainWindow.ApplyEditorFont();
            MainWindow.DictionaryAffPath = AffPathBox.Text ?? "";
            MainWindow.DictionaryDicPath = DicPathBox.Text ?? "";
            
            var selStyle = StyleData[StyleComboBox.SelectedIndex];
            MainWindow.ScriptNeedsInitialization = false;
            var abs = Path.GetFullPath(selStyle.Folder);
            if (MainWindow.SelectedStyle != abs)
            {
                MainWindow.StyleData = selStyle;
                MainWindow.SelectedStyle = abs;
                MainWindow.ScriptNeedsInitialization = true;
            }
            MainWindow._lastBgndPath = "";
            MainWindow.BgndPath = BackgroundData[BackgroundComboBox.SelectedIndex];
            MainWindow.SaveSettings();
            await MainWindow.LoadBackground();
        }
        Close();
    }

    void Cancel_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!Init)
            Close();
    }
    
    async void DownloadDictionary_OnClick(object? sender, RoutedEventArgs e)
    {
        DownloadStatusText.ClearValue(TextBlock.ForegroundProperty);
        var affPath = AffPathBox.Text?.Trim().TrimStart('/');
        var dicPath = DicPathBox.Text?.Trim().TrimStart('/');
        if (string.IsNullOrEmpty(affPath) || string.IsNullOrEmpty(dicPath))
        {
            DownloadStatusText.Foreground = Brushes.Red;
            DownloadStatusText.Text = "Enter both an .aff and a .dic path first.";
            return;
        }

        DownloadStatusText.Text = "Downloading...";
        try
        {
            var dictFolder = Path.Combine(AppContext.BaseDirectory, "SpellCheckDictionaries");
            Directory.CreateDirectory(dictFolder);
            using var http = new HttpClient();

            await DownloadOne(http, affPath, dictFolder);
            await DownloadOne(http, dicPath, dictFolder);

            MainWindow?.ReloadSpellChecker();
            DownloadStatusText.Text = "Downloaded successfully. Ready for use.";
        }
        catch (Exception ex)
        {
            DownloadStatusText.Foreground = Brushes.Red;
            DownloadStatusText.Text = $"Download failed: {ex.Message}";
        }
        UpdateDownloadStatusIcons();
    }

    static async Task DownloadOne(HttpClient http, string repoPath, string destFolder)
    {
        var url = $"https://raw.githubusercontent.com/LibreOffice/dictionaries/master/{repoPath}";
        var bytes = await http.GetByteArrayAsync(url);
        var fileName = repoPath[(repoPath.LastIndexOf('/') + 1)..];
        await File.WriteAllBytesAsync(Path.Combine(destFolder, fileName), bytes);
    }
    
    void PathBox_OnTextChanged(object? sender, TextChangedEventArgs e) => UpdateDownloadStatusIcons();

    void UpdateDownloadStatusIcons()
    {
        var dictFolder = Path.Combine(AppContext.BaseDirectory, "SpellCheckDictionaries");
        AffStatusIcon.IsVisible = FileAlreadyDownloaded(dictFolder, AffPathBox.Text);
        DicStatusIcon.IsVisible = FileAlreadyDownloaded(dictFolder, DicPathBox.Text);
    }

    static bool FileAlreadyDownloaded(string dictFolder, string? repoPath)
    {
        repoPath = repoPath?.Trim().TrimStart('/');
        if (string.IsNullOrEmpty(repoPath))
            return false;
        var fileName = repoPath[(repoPath.LastIndexOf('/') + 1)..];
        return File.Exists(Path.Combine(dictFolder, fileName));
    }
    
    async void OpenDictionariesRepo_OnClick(object? sender, RoutedEventArgs e) =>
        await Launcher.LaunchUriAsync(new Uri("https://github.com/LibreOffice/dictionaries"));

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

        if (!Design.IsDesignMode)
        {
            var baseDir = AppContext.BaseDirectory;
            {
                var curDir = Environment.CurrentDirectory;
                if (Path.Exists(Path.Combine(curDir, "Styles")) && Path.Exists(Path.Combine(curDir, "Backgrounds")))
                    baseDir = curDir;
            }
            foreach (var f in Directory.EnumerateDirectories(Path.Combine(baseDir, "Styles")))
            {
                var folder = Path.GetFullPath(f);
                if (IgnoreStyles.Contains(folder))
                    continue;
                try
                {
                    StyleData.Add(new StyleParser(folder));
                }
                catch (Exception ex)
                {
                    var sErr = new StyleError { ErrorBox = { Text = ex.ToString() } };
                    sErr.MessageBlock.Text = sErr.MessageBlock.Text!.Replace("StyleName", folder);
                    await sErr.ShowDialog(this);
                }
            }
            await Task.WhenAll(StyleData.Select(style => Task.Run(async () =>
            {
                try
                {
                    await style.CompileCode();
                }
                catch (Exception ex)
                {
                    var sErr = new StyleError { ErrorBox = { Text = ex.ToString() }, Title = "Compilation Error" };
                    sErr.MessageBlock.Text = sErr.MessageBlock.Text!.Replace("StyleName", style.Folder);
                    await sErr.ShowDialog(this);
                }
            })));
            foreach (var style in StyleData)
            {
                try
                {
                    StyleComboBox.Items.Add(new TextBlock { Text = $"{style.Metadata.Name} by {style.Metadata.Author}" });
                    MainWindow!.StyleSettings.TryAdd(style.Folder, []);
                    var res = await (style.Script?.RegisterCustomSettings() ?? Task.FromResult(Enumerable.Empty<ICustomProperty>()));
                    var props = MainWindow.StyleSettings[style.Folder];
                    foreach (var prop in res)
                    {
                        var p = props.FirstOrDefault(x => x.Name == prop.Name);
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
                    var sErr = new StyleError { ErrorBox = { Text = ex.ToString() }, Title = "Compilation Error" };
                    sErr.MessageBlock.Text = sErr.MessageBlock.Text!.Replace("StyleName", style.Folder);
                    await sErr.ShowDialog(this);
                }
            }
            foreach (var f in Directory.EnumerateDirectories(Path.Combine(baseDir, "Backgrounds")))
            {
                var folder = Path.GetFullPath(f);
                if (IgnoreStyles.Contains(folder))
                    continue;
                try
                {
                    var style = JsonSerializer.Deserialize<BackgroundConfig>(File.ReadAllText(Path.Combine(folder, "Config.jsonc")), new JsonSerializerOptions { IncludeFields = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })!;
                    BackgroundData.Add(folder);
                    BackgroundComboBox.Items.Add(new TextBlock { Text = $"{style.Name} by {style.Author}" });
                }
                catch (Exception ex)
                {
                    var sErr = new StyleError { ErrorBox = { Text = ex.ToString() } };
                    sErr.MessageBlock.Text = sErr.MessageBlock.Text!.Replace("StyleName", folder);
                    await sErr.ShowDialog(this);
                }
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
                var styleIndex = StyleData.FindIndex(style => Path.GetFullPath(style.Folder) == Path.GetFullPath(MainWindow.SelectedStyle));
                if (styleIndex != -1)
                    StyleComboBox.SelectedIndex = styleIndex;
            }

            var bgndIndex = BackgroundData.FindIndex(folder => Path.GetFullPath(folder) == Path.GetFullPath(MainWindow.BgndPath));
            if (bgndIndex != -1)
                BackgroundComboBox.SelectedIndex = bgndIndex;
        }
    }
}
