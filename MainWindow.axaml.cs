using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DialogueHelper.External;
using DialogueHelper.FileFormat;
using DialogueHelper.Options;
using DialogueHelper.StylesParser;
using DialogueHelper.Views;
using FileInfo = DialogueHelper.Views.FileInfo;

namespace DialogueHelper;

public partial class MainWindow : Window
{
    public FileData? FileData;
    public bool IsFileModified;
    public readonly PreviewRenderer.PreviewRenderer? PreviewRenderer;
    public readonly GitOptions GitOptions = new();
    public string? SelectedStyle;
    public StyleParser? StyleData;
    public List<ICustomProperty> CustomProperties = [];
    public List<CustomTool> CustomTools = [];
    public readonly Dictionary<string, Bitmap> LoadedImages = [];
    public bool ScriptNeedsInitialization;
    public string AuthorName = "";
    public string AuthorGroup = "";
    public readonly Dictionary<string, List<ICustomProperty>> StyleSettings = [];
    public StringContainer? SelectedString;
    public bool UpdatingEntry;
    public readonly Dictionary<TreeViewItem, string> EtvKeys = [];
    public readonly Dictionary<TreeViewItem, int> EtvIndexes = [];
    public readonly Dictionary<int, TreeViewItem> EtvReverseIndexes = [];
    public readonly Dictionary<TreeViewItem, Dictionary<int, TreeViewItem>> EtvTargetChildren = [];
    // <child, parent>
    public readonly Dictionary<TreeViewItem, TreeViewItem> EtvParents = [];
    public bool EditableTextIgnoreUpdateOnce;
    public List<WeakReference<TreeViewItem>> EtvTargetUpdates = [];
    readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(0.02) };

    public MainWindow()
    {
        InitializeComponent();
        PreviewRenderer = new(this);
        Closing += async (_, ev) =>
        {
            if (IsFileModified)
            {
                ev.Cancel = true;
                if (!await new UnsavedChanges().ShowDialog<bool>(this))
                    Environment.Exit(0);
            }
            else
                Environment.Exit(0);
        };
        _timer.Tick += (_, _) =>
        {
            WeakReference<TreeViewItem>? n = null;
            lock (EtvTargetUpdates)
            {
                if (EtvTargetUpdates.Count > 0)
                {
                    n = EtvTargetUpdates[0];
                    EtvTargetUpdates.RemoveAt(0);
                }
            }
            if (n is not null && n.TryGetTarget(out var tn))
                UpdateEtvItem(tn);
        };
        _timer.Start();
    }

    async void OpenFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (IsFileModified)
        {
            var unsaved = new UnsavedChanges();
            if (await unsaved.ShowDialog<bool>(this))
                return;
        }

        var file = await StorageProvider.OpenFilePickerAsync(
            new()
            {
                Title = "Open Dialogue Helper file...",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new("DH File") { Patterns = ["*.dh"] },
                    new("Legacy DH File") { Patterns = ["*.txt"] },
                ],
            }
        );

        if (file.Count == 0)
            return;
        var fStorage = file[0];

        EntryTreeView.Items.Clear();
        SimilarStringsPanel.Items.Clear();
        SimilarStringsNone.IsVisible = true;

        LoadFileToProject(file: fStorage);
    }

    public async void LoadFileToProject(string? path = null, IStorageFile? file = null, bool markAsModified = false)
    {
        if (path is null && file is null)
            throw new("path and file cannot be null to load a project from a file");
        var lWin = new LoadingWindow
        {
            CanResize = false
        };
        lWin.Closing += (_, ev) =>
        {
            if (!ev.IsProgrammatic)
                ev.Cancel = true;
        };
        lWin.ProgressBar.Value = 0.0;
        lWin.Show(this);
        lWin.Text.Text = "Loading the file...";
        using var stream = new StreamReader(file is not null ? await file.OpenReadAsync() : File.OpenRead(path!));
        LoadStringToProject(await stream.ReadToEndAsync(), lWin, markAsModified);
    }

    public async void LoadStringToProject(string str, LoadingWindow? lWin = null, bool markAsModified = true)
    {
        if (lWin is null)
        {
            lWin = new LoadingWindow
            {
                CanResize = false
            };
            lWin.Closing += (_, ev) =>
            {
                if (!ev.IsProgrammatic)
                    ev.Cancel = true;
            };
            lWin.ProgressBar.Value = 0.0;
            lWin.Show(this);
        }
        try
        {
            lWin.Text.Text = "Parsing the file...";
            var fParse = FileParser.ParseString(str);
            lWin.Text.Text = "Loading data...";
            FileData = FileData.LoadString(fParse, lWin);
            lWin.Text.Text = "Caching base entries...";
            lWin.ProgressBar.Value = 0.0;
            lWin.ProgressBar.Maximum = FileData.Strings.Count;

            var eqStr = new Dictionary<string, List<int>>();
            var cur = 0;
            foreach (var strCont in FileData!.StringIds.Values)
            {
                if (eqStr.TryGetValue(strCont.OriginalText, out var arr))
                {
                    strCont.EqStrings = arr;
                    arr.Add(strCont.Id);
                }
                else
                {
                    arr = [strCont.Id];
                    eqStr.Add(strCont.OriginalText, arr);
                    strCont.EqStrings = arr;
                }

                lWin.ProgressBar.Value = cur++;
            }

            lWin.Text.Text = "Updating view...";
            PopulateEntryTreeView();
        }
        catch (Exception ex)
        {
            var se = new StyleError()
            {
                Title = "Load error",
                MessageBlock =
                {
                    Text = "DialogueHelper has encountered an error while loading a file",
                },
                ErrorBox =
                {
                    Text = ex.ToString(),
                }
            };
            await se.ShowDialog(this);
        }

        lWin.Close();
        IsFileModified = markAsModified;
        Title = $"Dialogue Helper{(markAsModified ? "*" : "")}";
    }

    public async void PopulateEntryTreeView()
    {
        EntryTreeView.Items.Clear();
        EtvKeys.Clear();
        EtvIndexes.Clear();
        EtvReverseIndexes.Clear();
        EtvTargetChildren.Clear();
        if (!await EnsureFileOpen())
            return;
        foreach (var (key, strs) in new Dictionary<string, Dictionary<int, StringContainer>>(FileData!.Strings))
        {
            if (strs.Count <= 0)
            {
                FileData.Strings.Remove(key);
                foreach (var str in strs)
                    FileData.StringIds.Remove(str.Key);
                continue;
            }
            var cont = new TreeViewItem() { Header = key };
            EntryTreeView.Items.Add(cont);
            EtvKeys.Add(cont, key);
            var data = new Dictionary<int, TreeViewItem>();
            EtvTargetChildren.Add(cont, data);
            var i = 0;
            var modified = 0;
            foreach (var str in strs)
            {
                var tBlock = new TreeViewItem() { Header = str.Value.Text ?? str.Value.OriginalText };
                EtvParents.Add(tBlock, cont);
                ColorTreeItem(
                    tBlock,
                    str.Value.Text,
                    str.Value.OriginalText,
                    str.Value.MarkAsModified
                );
                if (str.Value.Text is not null && str.Value.Text != str.Value.OriginalText)
                    tBlock.Header = str.Value.Text;
                if (
                    (str.Value.Text is not null && str.Value.Text != str.Value.OriginalText)
                    || str.Value.MarkAsModified
                )
                    modified++;
                i++;
                EtvIndexes.Add(tBlock, str.Value.Id);
                EtvReverseIndexes.Add(str.Value.Id, tBlock);
                data.Add(str.Value.Id, tBlock);
            }

            cont.ItemsSource = data.OrderBy(v => strs[v.Key].Id).Select(v => v.Value).ToArray();
            cont.Foreground = SolidColorBrush.Parse(
                modified != 0 ? (modified >= i ? "LightGreen" : "MediumPurple") : "LightCoral"
            );
        }
        await UpdateSearchFiltering();
    }

    public async void UpdateEtvItem(TreeViewItem item)
    {
        if (!await EnsureFileOpen())
            return;
        var key = EtvKeys[item];
        var strs = FileData!.Strings[key];

        var i = 0;
        var modified = 0;
        var list = EtvTargetChildren[item];
        foreach (var str in strs)
        {
            ColorTreeItem(
                list[str.Key],
                str.Value.Text,
                str.Value.OriginalText,
                str.Value.MarkAsModified
            );
            if (str.Value.Text is not null)
            {
                list[str.Key].Header = str.Value.Text;
                if (str.Value.Text != str.Value.OriginalText || str.Value.MarkAsModified)
                    modified++;
            }
            i++;
        }

        item.Foreground = SolidColorBrush.Parse(
            modified != 0 ? (modified >= i ? "LightGreen" : "MediumPurple") : "LightCoral"
        );
        await UpdateSearchFiltering(new() { [item] = EtvTargetChildren[item] });
    }

    public static void ColorTreeItem(
        TreeViewItem item,
        string? text,
        string ogText,
        bool markAsModified
    )
    {
        item.Foreground = SolidColorBrush.Parse(
            (text is not null && text != ogText) || markAsModified ? "LightGreen" : "White"
        );
    }

    void OnLayoutUpdated(object? sender, EventArgs e)
    {
        EntryTreeView.Height = double.Max(LeftStackPanel.Bounds.Height - EntryTextBox.DesiredSize.Height - 11, 0);
        //ScrollViewer.MaxHeight = LeftStackPanel.Bounds.Height - TextPanel.DesiredSize.Height;
    }

    public async void EntryTreeView_OnSelectionChanged(object? sender, SelectionChangedEventArgs? e)
    {
        if (!await EnsureFileOpen())
            return;
        /*if (EntryTreeView.SelectedItem is null)
        {
            await new InfoWindow(
                "You need to select an item before trying to change item selection."
            ).ShowDialog(this);
            return;
        }*/
        if (EntryTreeView.SelectedItem is TreeViewItem tBlock && EtvParents.TryGetValue(tBlock, out var parent))
        {
            var lastSel = SelectedString;
            SelectedString = FileData!.Strings[EtvKeys[parent]][EtvIndexes[tBlock]];
            MarkAsModified.IsChecked = SelectedString.MarkAsModified;
            if (OriginalText.Text != SelectedString.OriginalText)
                OriginalText.Text = SelectedString.OriginalText;
            {
                var t = SelectedString.Text ?? SelectedString.OriginalText;
                if (EditableText.Text != t)
                {
                    EditableTextIgnoreUpdateOnce = true;
                    EditableText.Text = t;
                }
            }

            foreach (var prop in CustomProperties)
            {
                if (SelectedString.CustomProperties.TryGetValue(prop.Name, out var value))
                    prop.StringToValue(value);
                else
                    prop.Value = prop.DefaultValue;
            }

            if (lastSel is null || lastSel.EqStrings != SelectedString.EqStrings)
            {
                SimilarStringsNone.IsVisible = SelectedString.EqStrings.Count <= 0;
                SimilarStringsPanel.SelectedIndex = -1;
                SimilarStringsPanel.Items.Clear();
                foreach (var sstr in SelectedString.EqStrings)
                {
                    var s = FileData.StringIds[sstr];
                    SimilarStringsPanel.Items.Add($"({sstr}) {((s.Text is not null && s.Text != s.OriginalText) ? s.Text : s.OriginalText)}");
                }
            }

            UpdateCustomScriptFields();
        }
        else
            SelectedString = null;
    }

    void UpdateBoxBounds()
    {
        if (ScrollViewerOriginal is null || ScrollViewer is null)
            return;

        ScrollViewerOriginal.Height = double.Max(
            PanelOriginal.Bounds.Height - OriginalLabel.DesiredSize.Height,
            0
        );

        ScrollViewer.Height = double.Max(
            Panel.Bounds.Height
                - EditableLabel.DesiredSize.Height
                - OuterGridSplitter.Bounds.Height
                - 5,
            0
        );
        Panel.Height = double.Max(
            MiddleStackPanel.Bounds.Height
                - PanelOriginal.Bounds.Height
                - InnerGridSplitter.Bounds.Height,
            0
        );

        OuterMiddleStackPanel.MaxHeight = double.Max(MainGrid.Bounds.Height - 10, 0);
    }

    async void Settings_OnClick(object? sender, RoutedEventArgs e)
    {
        var settings = new Settings(this);
        await settings.ShowDialog(this);
        if (ScriptNeedsInitialization)
            InitializeCustomScript();
    }

    public void LoadSettings()
    {
        var file =
            $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/DialogueHelper/settings.txt";
        if (!File.Exists(file))
            return;
        var f = File.ReadAllLines(file);

        GitOptions.IsEnabled = f[0] == "1";
        GitOptions.RepoUrl = f[1];
        GitOptions.Branch = f[2];

        AuthorGroup = f[3];
        AuthorName = f[4];

        SelectedStyle = f[5].Length <= 0 ? null : f[5];
        var i = 6;
        var entryType = "";
        while (i < f.Length)
        {
            if (entryType == "")
                entryType = f[i];
            else
            {
                switch (entryType[..entryType.IndexOf("@@", StringComparison.InvariantCulture)])
                {
                    case "StyleSettings":
                        if (f[i] == "@@end@@")
                            entryType = "";
                        else
                        {
                            var folder = WebUtility.UrlDecode(
                                entryType[
                                    (
                                        entryType.IndexOf("@@", StringComparison.InvariantCulture)
                                        + 2
                                    )..
                                ]
                            );
                            if (!StyleSettings.ContainsKey(folder))
                                StyleSettings.Add(folder, []);
                            var spl = f[i].Split("@@");
                            // the visual name & type value will get overwritten once the style loads
                            StyleSettings[folder].Add(new CustomProperty<string>(
                                "<null>",
                                WebUtility.UrlDecode(spl[0]),
                                WebUtility.UrlDecode(spl[1])
                            ));
                        }
                        break;
                }
            }
            i++;
        }
    }

    public void SaveSettings()
    {
        {
            var dir =
                $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/DialogueHelper/";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
        List<string> l =
        [
            GitOptions.IsEnabled ? "1" : "0",
            GitOptions.RepoUrl,
            GitOptions.Branch,
            AuthorGroup,
            AuthorName,
            SelectedStyle ?? "",
        ];
        foreach (var ss in StyleSettings)
        {
            l.Add($"StyleSettings@@{WebUtility.UrlEncode(ss.Key)}");
            l.AddRange(
                ss.Value.Select(prop =>
                    $"{WebUtility.UrlEncode(prop.Name)}@@{WebUtility.UrlEncode(prop.ValueToString())}"
                )
            );
            l.Add("@@end@@");
        }
        File.WriteAllLines(
            $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/DialogueHelper/settings.txt",
            l
        );
    }

    async void OnOpened(object? sender, EventArgs e)
    {
        var w = new LoadingWindow()
        {
            ProgressBar = {
                IsIndeterminate = true,
                ShowProgressText = false,
            },
            Text = {
                Text = "Initializing...",
            },
        };
        w.Show(this);
        if (Directory.Exists("RuntimeComp"))
        {
            // clear previously compiled files as it's unnecessary in this run because it'll be recompiled.
            foreach (var f in Directory.EnumerateFiles("RuntimeComp").Where(x => x.Contains(".dll") && x.Contains("IL-")))
                File.Delete(f);
        }
        MiddleStackPanel.RowDefinitions[0].Height = new GridLength(
            235.0 * ClientSize.Height / 720.0
        );
        MiddleStackPanel.RowDefinitions[2].Height = new GridLength(
            235.0 * ClientSize.Height / 720.0
        );
        LoadSettings();
        if (SelectedStyle is null)
        {
            w.Hide();
            var settings = new Settings(this) { Init = true };
            await settings.ShowDialog(this);
            w.Show(this);
        }
        else
        {
            try
            {
                StyleData = new StyleParser(SelectedStyle);
                await Task.Run(() => StyleData.CompileCode());
                if (!StyleSettings.ContainsKey(SelectedStyle))
                    StyleSettings.Add(SelectedStyle, []);
                if (StyleData.Script is not null)
                {
                    var res = await StyleData.Script.RegisterCustomSettings();
                    var props = StyleSettings[SelectedStyle];
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
            }
            catch (Exception ex)
            {
                w.Hide();

                var sErr = new StyleError { ErrorBox = { Text = ex.ToString() } };
                sErr.MessageBlock.Text = sErr.MessageBlock.Text!.Replace(
                    "StyleName",
                    SelectedStyle
                );
                await sErr.ShowDialog(this);
                var settings = new Settings(this) { Init = true, IgnoreStyles = [SelectedStyle] };
                await settings.ShowDialog(this);

                w.Show(this);
            }
        }

        InitializeCustomScript();
        EditableText_OnTextChanged(null, null);
        OriginalText_OnTextChanged(null, null);

        w.Hide();
    }

    public async void InitializeCustomScript()
    {
        foreach (var img in LoadedImages.Values)
            img.Dispose();
        LoadedImages.Clear();

        try
        {
            await (StyleData!.Script?.Init(new(this, StyleData)) ?? Task.CompletedTask);
            CustomTools.Clear();
            CustomProperties.Clear();

            var lastBoxSelected = int.Clamp(
                BoxComboBox.SelectedIndex,
                0,
                StyleData.BoxMetadata.Count - 1
            );
            var lastFontSelected = int.Clamp(
                FontComboBox.SelectedIndex,
                0,
                StyleData.FontMetadata.Count - 1
            );
            BoxComboBox.Items.Clear();
            FontComboBox.Items.Clear();

            foreach (var box in StyleData.BoxMetadata)
                BoxComboBox.Items.Add(new ComboBoxItem() { Content = box.Name });

            foreach (var font in StyleData.FontMetadata)
                FontComboBox.Items.Add(new ComboBoxItem() { Content = font.Name });

            BoxComboBox.SelectedIndex = lastBoxSelected;
            FontComboBox.SelectedIndex = lastFontSelected;

            CustomTools = [.. await (StyleData.Script?.RegisterCustomTools() ?? Task.FromResult(Enumerable.Empty<CustomTool>()))];
            CustomProperties = [.. await (StyleData.Script?.RegisterCustomProperties() ?? Task.FromResult(Enumerable.Empty<ICustomProperty>()))];
        }
        catch (Exception ex)
        {
            // ensure that no custom tool (broken) data may trigger more errors down the line
            CustomTools.Clear();
            CustomProperties.Clear();

            var se = new StyleError()
            {
                MessageBlock = { Text = "While trying to initialize the style, the program encountered an error" },
                ErrorBox = { Text = ex.ToString() },
            };
            await se.ShowDialog(this);
        }

        UpdateCustomScriptFields();
        StyleData?.InitializedScript = true;
    }

    void BoxImages_OnLayoutUpdated(object? sender, EventArgs e) => UpdateBoxBounds();

    public void UpdateCustomScriptFields()
    {
        CustomToolsMenu.Items.Clear();
        if (CustomTools.Count <= 0)
            CustomToolsMenu.Items.Add(new MenuItem() { IsEnabled = false, Header = "Empty" });
        else
        {
            foreach (var tool in CustomTools)
            {
                var item = new MenuItem() { Header = tool.Name };
                item.Click += async (_, _) =>
                {
                    try
                    {
                        await tool.Func(new CustomToolWindowHandler(this, StyleData!));
                    }
                    catch (Exception ex)
                    {
                        var se = new StyleError()
                        {
                            MessageBlock = { Text = "The custom tool has encountered an error" },
                            ErrorBox = { Text = ex.ToString() },
                        };
                        await se.ShowDialog(this);
                    }
                };
                CustomToolsMenu.Items.Add(item);
            }
        }
        CustomPropertiesPanel.Children.Clear();
        if (CustomProperties.Count <= 0)
            CustomPropertiesNone.IsVisible = true;
        else
        {
            CustomPropertiesNone.IsVisible = false;
            UpdateCustomProperties(
                CustomPropertiesPanel,
                CustomProperties.AsEnumerable(),
                true
            );
        }
    }

    public void UpdateCustomProperties(Panel panel, IEnumerable<ICustomProperty> props, bool isString = false)
    {
        foreach (var tool in props)
        {
            var item = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new(5) };
            item.Children.Add(new TextBlock() { Margin = new(7), Text = tool.VisualName });
            switch (tool.Type)
            {
                case not null when tool.Type == typeof(bool):
                    {
                        item.Children.Clear();
                        var node = new CheckBox()
                        {
                            IsChecked = ((bool?)tool.Value) ?? false,
                            IsThreeState = false,
                            Content = tool.VisualName,
                            IsEnabled = !tool.ReadOnly,
                        };
                        node.IsCheckedChanged += (_, _) =>
                        {
                            tool.StringToValue((node.IsChecked ?? false) ? "true" : "false");
                            if (isString)
                            {
                                EditableText_OnTextChanged(null, null);
                                OriginalText_OnTextChanged(null, null);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;

                case not null when tool.Type == typeof(byte):
                    {
                        var node = new NumericUpDown()
                        {
                            Value = ((decimal?)tool.Value) ?? 0,
                            Minimum = 0,
                            Maximum = 0xff,
                            FormatString = "0",
                            Increment = 1,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += (_, _) =>
                        {
                            tool.StringToValue(
                                (node.Value ?? 0).ToString(CultureInfo.InvariantCulture)
                            );
                            if (isString)
                            {
                                EditableText_OnTextChanged(null, null);
                                OriginalText_OnTextChanged(null, null);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;
                case not null when tool.Type == typeof(short):
                    {
                        var node = new NumericUpDown()
                        {
                            Value = ((decimal?)tool.Value) ?? 0,
                            Minimum = -0x8000,
                            Maximum = 0x7fff,
                            FormatString = "0",
                            Increment = 1,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += (_, _) =>
                        {
                            tool.StringToValue(
                                (node.Value ?? 0).ToString(CultureInfo.InvariantCulture)
                            );
                            if (isString)
                            {
                                EditableText_OnTextChanged(null, null);
                                OriginalText_OnTextChanged(null, null);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;
                case not null when tool.Type == typeof(int):
                    {
                        var node = new NumericUpDown()
                        {
                            Value = ((decimal?)tool.Value) ?? 0,
                            Minimum = -0x80000000,
                            Maximum = 0x7fffffff,
                            FormatString = "0",
                            Increment = 1,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += (_, _) =>
                        {
                            tool.StringToValue(
                                (node.Value ?? 0).ToString(CultureInfo.InvariantCulture)
                            );
                            if (isString)
                            {
                                EditableText_OnTextChanged(null, null);
                                OriginalText_OnTextChanged(null, null);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;
                case not null when tool.Type == typeof(long):
                    {
                        var node = new NumericUpDown()
                        {
                            Value = ((decimal?)tool.Value) ?? 0,
                            Minimum = -0x8000000000000000,
                            Maximum = 0x7fffffffffffffff,
                            FormatString = "0",
                            Increment = 1,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += (_, _) =>
                        {
                            tool.StringToValue(
                                (node.Value ?? 0).ToString(CultureInfo.InvariantCulture)
                            );
                            if (isString)
                            {
                                EditableText_OnTextChanged(null, null);
                                OriginalText_OnTextChanged(null, null);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;

                case not null when tool.Type == typeof(sbyte):
                    {
                        var node = new NumericUpDown()
                        {
                            Value = ((decimal?)tool.Value) ?? 0,
                            Minimum = -0x80,
                            Maximum = 0x7f,
                            FormatString = "0",
                            Increment = 1,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += (_, _) =>
                        {
                            tool.StringToValue(
                                (node.Value ?? 0).ToString(CultureInfo.InvariantCulture)
                            );
                            if (isString)
                            {
                                EditableText_OnTextChanged(null, null);
                                OriginalText_OnTextChanged(null, null);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;
                case not null when tool.Type == typeof(ushort):
                    {
                        var node = new NumericUpDown()
                        {
                            Value = ((decimal?)tool.Value) ?? 0,
                            Minimum = 0,
                            Maximum = 0xffff,
                            FormatString = "0",
                            Increment = 1,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += (_, _) =>
                        {
                            tool.StringToValue(
                                (node.Value ?? 0).ToString(CultureInfo.InvariantCulture)
                            );
                            if (isString)
                            {
                                EditableText_OnTextChanged(null, null);
                                OriginalText_OnTextChanged(null, null);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;
                case not null when tool.Type == typeof(uint):
                    {
                        var node = new NumericUpDown()
                        {
                            Value = ((decimal?)tool.Value) ?? 0,
                            Minimum = 0,
                            Maximum = 0xffffffff,
                            FormatString = "0",
                            Increment = 1,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += (_, _) =>
                        {
                            tool.StringToValue(
                                (node.Value ?? 0).ToString(CultureInfo.InvariantCulture)
                            );
                            if (isString)
                            {
                                EditableText_OnTextChanged(null, null);
                                OriginalText_OnTextChanged(null, null);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;
                case not null when tool.Type == typeof(ulong):
                    {
                        var node = new NumericUpDown()
                        {
                            Value = ((decimal?)tool.Value) ?? 0,
                            Minimum = 0,
                            Maximum = 0xffffffffffffffff,
                            FormatString = "0",
                            Increment = 1,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += (_, _) =>
                        {
                            tool.StringToValue(
                                (node.Value ?? 0).ToString(CultureInfo.InvariantCulture)
                            );
                            if (isString)
                            {
                                EditableText_OnTextChanged(null, null);
                                OriginalText_OnTextChanged(null, null);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;

                case not null when tool.Type == typeof(float):
                    {
                        var node = new NumericUpDown()
                        {
                            Value = ((decimal?)tool.Value) ?? 0,
                            FormatString = "0.00",
                            Increment = (decimal)0.01f,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += (_, _) =>
                        {
                            tool.StringToValue(
                                (node.Value ?? 0).ToString(CultureInfo.InvariantCulture)
                            );
                            if (isString)
                            {
                                EditableText_OnTextChanged(null, null);
                                OriginalText_OnTextChanged(null, null);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;
                case not null when tool.Type == typeof(double):
                    {
                        var node = new NumericUpDown()
                        {
                            Value = ((decimal?)tool.Value) ?? 0,
                            FormatString = "0.00",
                            Increment = (decimal)0.01,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += (_, _) =>
                        {
                            tool.StringToValue(
                                (node.Value ?? 0).ToString(CultureInfo.InvariantCulture)
                            );
                            if (isString)
                            {
                                EditableText_OnTextChanged(null, null);
                                OriginalText_OnTextChanged(null, null);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;

                case not null when tool.Type == typeof(string):
                    {
                        var node = new TextBox()
                        {
                            Text = (string?)tool.Value,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.TextChanged += (_, _) =>
                        {
                            tool.StringToValue(node.Text ?? "");
                            if (isString)
                            {
                                EditableText_OnTextChanged(null, null);
                                OriginalText_OnTextChanged(null, null);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;
            }
            panel.Children.Add(item);
        }
    }

    void EditableText_OnTextChanged(object? sender, TextChangedEventArgs? e)
    {
        if (PreviewRenderer is null)
            return;
        if (SelectedString is not null && !UpdatingEntry && FileData is not null)
        {
            if (!EditableTextIgnoreUpdateOnce)
            {
                IsFileModified = true;
                Title = "Dialogue Helper*";
                SelectedString.Text = EditableText.Text ?? "<null>";
                foreach (var prop in CustomProperties)
                {
                    if (prop.Value != prop.DefaultValue)
                        SelectedString.CustomProperties[prop.Name] = prop.ValueToString();
                    else
                        SelectedString.CustomProperties.Remove(prop.Name);
                }

                if (SelectedString.Text != SelectedString.OriginalText)
                {
                    if (!FileData.AuthorList.Contains(AuthorName))
                        FileData.AuthorList.Add(AuthorName);
                    SelectedString.LastEdited.AuthorId = FileData.AuthorList.IndexOf(AuthorName);
                    SelectedString.LastEdited.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }
                UpdateModifySimilarStrings();
            }
            else
                EditableTextIgnoreUpdateOnce = false;
        }
        EditableText.Text ??= "";
        PreviewRenderer.CreateRender(
            StyleData!,
            BoxComboBox.SelectedIndex,
            FontComboBox.SelectedIndex,
            (float?)BoxScale.Value ?? 0f,
            (float?)FontScale.Value ?? 0f,
            (float?)PreviewScale.Value ?? 0f,
            EditableText.Text,
            DisplayEditableCanvas
        );
        UpdateBoxBounds();
    }

    void OriginalText_OnTextChanged(object? sender, TextChangedEventArgs? e)
    {
        if (PreviewRenderer is null)
            return;
        PreviewRenderer.CreateRender(
            StyleData!,
            BoxComboBox.SelectedIndex,
            FontComboBox.SelectedIndex,
            (float?)BoxScale.Value ?? 0.0f,
            (float?)FontScale.Value ?? 0.0f,
            (float?)PreviewScale.Value ?? 0.0f,
            OriginalText.Text ?? "",
            DisplayOriginalCanvas
        );
        UpdateBoxBounds();
    }

    async void ForceStyleReload_OnClick(object? sender, RoutedEventArgs e)
    {
        StyleData = new StyleParser(StyleData!.Folder);
        await StyleData.CompileCode();
        InitializeCustomScript();
        UpdatingEntry = true;
        EditableText_OnTextChanged(null, null);
        OriginalText_OnTextChanged(null, null);
        UpdatingEntry = false;
    }

    void OnScaleValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        EditableText_OnTextChanged(null, null);
        OriginalText_OnTextChanged(null, null);
    }

    async void SaveFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureFileOpen())
            return;
        var file = await StorageProvider.SaveFilePickerAsync(
            new()
            {
                Title = "Save Dialogue Helper file...",
                FileTypeChoices = [new("DH File") { Patterns = ["*.dh"] }],
            }
        );

        if (file is null)
            return;

        var lWin = new LoadingWindow { CanResize = false };
        lWin.Closing += (_, ev) =>
        {
            if (!ev.IsProgrammatic)
                ev.Cancel = true;
        };
        lWin.ProgressBar.Value = 0;
        lWin.Show(this);
        lWin.Text.Text = "Saving the file...";
        try
        {
            await using var stream = await file.OpenWriteAsync();
            stream.Write(Encoding.UTF8.GetBytes(FileData!.OutputString()));
            IsFileModified = false;
            Title = "Dialogue Helper";
        }
        catch (Exception ex)
        {
            var se = new StyleError()
            {
                Title = "Save error",
                MessageBlock =
                {
                    Text = "DialogueHelper has encountered an error while saving a file",
                },
                ErrorBox = { Text = ex.ToString() },
            };
            await se.ShowDialog(this);
        }
        lWin.Close();
    }

    void BoxComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedString?.BoxStyle = BoxComboBox.SelectedIndex;
        EditableText_OnTextChanged(null, null);
        OriginalText_OnTextChanged(null, null);
    }

    void FontComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedString?.FontStyle = FontComboBox.SelectedIndex;
        EditableText_OnTextChanged(null, null);
        OriginalText_OnTextChanged(null, null);
    }

    async void EntryTextBox_OnTextChanged(object? sender, TextChangedEventArgs? e) => await UpdateSearchFiltering();
    async Task UpdateSearchFiltering(Dictionary<TreeViewItem, Dictionary<int, TreeViewItem>>? delta = null)
    {
        if (!await EnsureFileOpen())
            return;
        var matches = new List<string>();
        {
            var sb = new StringBuilder();
            var updateRaw = false;
            var enclosedQuote = false;
            var t = EntryTextBox.Text ?? "";
            foreach (var (i, chr) in t.Index())
            {
                if (updateRaw)
                {
                    updateRaw = false;
                    sb.Append(chr);
                }
                else
                {
                    switch (chr)
                    {
                        case '\\':
                            updateRaw = true;
                            break;
                        case '"':
                            enclosedQuote = !enclosedQuote;
                            break;
                        case ' ':
                            if (!enclosedQuote)
                            {
                                matches.Add(sb.ToString());
                                sb.Clear();
                            }
                            else
                                sb.Append(chr);
                            break;
                        default:
                            sb.Append(chr);
                            break;
                    }
                }

                if (i == t.Length - 1)
                    matches.Add(sb.ToString());
            }
        }
        var searchCasing = new List<string>(
            matches
                .Where(m => m.StartsWith("Casing:", StringComparison.InvariantCultureIgnoreCase))
                .Select(m => m[7..])
                .Where(m => m.Length > 0)
        );
        var targetCasing = searchCasing.Any(casing =>
            casing.Equals("sensible", StringComparison.InvariantCultureIgnoreCase)
        )
            ? StringComparison.InvariantCulture
            : StringComparison.InvariantCultureIgnoreCase;

        var searchGroup = new List<string>(
            matches
                .Where(m => m.StartsWith("Group:", StringComparison.InvariantCultureIgnoreCase))
                .Select(m => m[6..])
                .Where(m => m.Length > 0)
        );
        var searchOgText = new List<string>(
            matches
                .Where(m => m.StartsWith("OgText:", StringComparison.InvariantCultureIgnoreCase))
                .Select(m => m[7..])
                .Where(m => m.Length > 0)
        );
        var headerText = new List<string>(
            matches
                .Where(m => m.StartsWith("Header:", StringComparison.InvariantCultureIgnoreCase))
                .Select(m => m[7..])
                .Where(m => m.Length > 0)
        );
        var stateText = new List<string>(
            matches
                .Where(m => m.StartsWith("State:", StringComparison.InvariantCultureIgnoreCase))
                .Select(m => m[6..])
                .Where(m => m.Length > 0)
        );

        var searchText = new List<string>(
            matches
                .Where(m =>
                    !m.StartsWith("Casing:", StringComparison.InvariantCultureIgnoreCase)
                    && !m.StartsWith("Group:", StringComparison.InvariantCultureIgnoreCase)
                    && !m.StartsWith("OgText:", StringComparison.InvariantCultureIgnoreCase)
                    && !m.StartsWith("Header:", StringComparison.InvariantCultureIgnoreCase)
                    && !m.StartsWith("State:", StringComparison.InvariantCultureIgnoreCase)
                )
                .Where(m => m.Length > 0)
        );

        foreach (var value in delta ?? EtvTargetChildren)
        {
            var key = EtvKeys[value.Key];
            if (!FileData!.Strings.TryGetValue(key, out var list))
                continue;
            var foundMatch = false;
            if (headerText.All(header => key.Contains(header, targetCasing)))
            {
                if (value.Value.Count <= 0)
                    foundMatch = true;
                foreach (var entry in value.Value)
                {
                    var item = list[EtvIndexes[entry.Value]];
                    if (searchGroup.Any(group => !item.AuthorGroups.Any(ag => ag.Contains(group, targetCasing))))
                        goto NotFound;
                    if (searchOgText.Any(ogText => !item.OriginalText.Contains(ogText, targetCasing)))
                        goto NotFound;
                    if (searchText.Any(text => !(item.Text ?? item.OriginalText).Contains(text, targetCasing)))
                        goto NotFound;
                    foreach (var state in stateText)
                    {
                        switch (state.ToLowerInvariant())
                        {
                            case "modified":
                                if (item.Text is null || item.Text == item.OriginalText)
                                    goto NotFound;
                                break;
                            case "base" or "original":
                                if (item.Text is not null && item.Text != item.OriginalText)
                                    goto NotFound;
                                break;
                        }
                    }

                    foundMatch = true;
                    if (searchGroup.Count > 0 && searchOgText.Count > 0 && searchText.Count > 0)
                        entry.Value.Foreground = SolidColorBrush.Parse("Goldenrod");
                    else
                        ColorTreeItem(entry.Value, item.Text, item.OriginalText, item.MarkAsModified);
                    entry.Value.IsVisible = true;
                    continue;

                NotFound:
                    entry.Value.IsVisible = false;
                }
            }
            value.Key.IsVisible = foundMatch;
        }
    }

    void ModifySimilarStrings_OnClick(object? sender, RoutedEventArgs e) => UpdateModifySimilarStrings();

    void UpdateModifySimilarStrings()
    {
        if (SelectedString is null || FileData is null)
            return;
        if (EntryTreeView.SelectedItem is TreeViewItem tvi)
        {
            lock (EtvTargetUpdates)
            {
                var alreadyHas = false;
                var target = EtvParents[tvi];
                foreach (var n in EtvTargetUpdates)
                {
                    if (n.TryGetTarget(out var t) && t == target)
                        alreadyHas = true;
                }
                if (!alreadyHas)
                    EtvTargetUpdates.Add(new(target));
            }
        }
        if (ModifySimilarStrings.IsChecked ?? false)
        {
            foreach (var id in SelectedString.EqStrings)
            {
                var s = FileData.StringIds[id];
                if (s.Id == SelectedString.Id)
                    continue;
                s.Text = SelectedString.Text;
                s.LastEdited.AuthorId = SelectedString.LastEdited.AuthorId;
                s.LastEdited.Timestamp = SelectedString.LastEdited.Timestamp;
                s.MarkAsModified = SelectedString.MarkAsModified;

                lock (EtvTargetUpdates)
                {
                    var alreadyHas = false;
                    var target = EtvParents[EtvReverseIndexes[id]];
                    foreach (var n in EtvTargetUpdates)
                    {
                        if (n.TryGetTarget(out var t) && t == target)
                            alreadyHas = true;
                    }
                    if (!alreadyHas)
                        EtvTargetUpdates.Add(new(target));
                }
            }
            foreach (var (i, sstr) in SelectedString.EqStrings.Index())
            {
                var s = FileData.StringIds[sstr];
                SimilarStringsPanel.Items[i] = $"({sstr}) {((s.Text is not null && s.Text != s.OriginalText) ? s.Text : s.OriginalText)}";
            }
        }
    }

    async void AboutDialogueHelper_OnClick(object? sender, RoutedEventArgs e) => await new AboutDialogueHelper().ShowDialog(this);

    void Exit_OnClick(object? sender, RoutedEventArgs e) => Close();

    async void CloseFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (IsFileModified)
        {
            var unsaved = new UnsavedChanges();
            if (await unsaved.ShowDialog<bool>(this))
                return;
        }

        SelectedString = null;
        FileData = null;
        EntryTreeView.Items.Clear();
        EtvKeys.Clear();
        EtvTargetChildren.Clear();
        EtvIndexes.Clear();
        EtvReverseIndexes.Clear();
        IsFileModified = false;
        Title = "Dialogue Helper";
    }

    async void CreateFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (IsFileModified && await new UnsavedChanges().ShowDialog<bool>(this))
            return;

        SelectedString = null;
        FileData = new();
    }

    async void GoTo_OnClick(object? sender, RoutedEventArgs e) => await new GoTo(this).ShowDialog(this);

    async void CopyClipboardSelectedTreeItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (EntryTreeView.SelectedItem is null)
        {
            await new InfoWindow("You need to select an item before trying to copy it to the clipboard.").ShowDialog(this);
            return;
        }
        await Clipboard!.SetTextAsync(
            ((TreeViewItem)EntryTreeView.SelectedItem).Header!.ToString() ?? "<null>"
        );
    }

    void ExpandTreeItems_OnClick(object? sender, RoutedEventArgs e)
    {
        foreach (var item in EntryTreeView.Items)
            EntryTreeView.ExpandSubTree((TreeViewItem)item!);
    }

    void CollapseTreeItems_OnClick(object? sender, RoutedEventArgs e)
    {
        foreach (var item in EntryTreeView.Items)
            EntryTreeView.CollapseSubTree((TreeViewItem)item!);
    }

    async void CreateEntry_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureFileOpen())
            return;
        var ce = new CreateEntry() { TextBox = { AcceptsReturn = false } };
        if (await ce.ShowDialog<bool?>(this) ?? false)
        {
            var t = ce.TextBox.Text ?? "<null>";
            if (!FileData!.Strings.ContainsKey(t))
            {
                var tvi = new TreeViewItem() { Header = t };
                EntryTreeView.Items.Add(tvi);
                FileData.Strings.Add(t, []);
                EtvTargetChildren.Add(tvi, []);
                EtvKeys.Add(tvi, t);
            }

            var item = EtvKeys.First(v => v.Value == t).Key;
            EntryTreeView.ExpandSubTree(item);
            EntryTreeView.SelectedItem = item;
            EntryTreeView.ScrollIntoView(item);
            item.Focus();
            UpdateEtvItem(item);
            EntryTreeView_OnSelectionChanged(null, null);

            IsFileModified = true;
            Title = "Dialogue Helper*";
        }
    }

    async void CreateString_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureFileOpen())
            return;
        if (EntryTreeView.SelectedItem is null)
        {
            await new InfoWindow("You need to select an item before trying to create a string.").ShowDialog(this);
            return;
        }
        var ce = new CreateEntry()
        {
            Title = "Create String",
            EntryText = { Text = "Set the string content" },
            AcceptEmpty = true,
        };
        TreeViewItem parent;
        {
            var s = (TreeViewItem)EntryTreeView.SelectedItem;
            parent = EtvKeys.ContainsKey(s) ? s : EtvParents[s];
        }
        if (!EtvKeys.TryGetValue(parent, out var key))
            return;
        if (await ce.ShowDialog<bool?>(this) ?? false)
        {
            var t = ce.TextBox.Text ?? "<null>";
            var tvi = new TreeViewItem() { Header = t };
            parent.ItemsSource = new List<TreeViewItem>(parent.ItemsSource?.Cast<TreeViewItem>() ?? []) { tvi }.ToArray();
            EtvParents.Add(tvi, parent);
            var sc = new StringContainer()
            {
                Id = FileData!.LastStringId++,
                OriginalText = t,
                EqStrings = FileData.StringIds.Values.FirstOrDefault(v => v.OriginalText == t)?.EqStrings ?? [],
            };
            FileData.Strings[key].Add(sc.Id, sc);
            FileData.StringIds.Add(sc.Id, sc);
            EtvTargetChildren[parent].Add(sc.Id, tvi);
            EtvIndexes.Add(tvi, sc.Id);
            EtvReverseIndexes.Add(sc.Id, tvi);
            sc.EqStrings.Add(sc.Id);
            sc.LastEdited.AuthorId = FileData.AuthorList.IndexOf(AuthorName);
            sc.LastEdited.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            EntryTreeView.ExpandSubTree(parent);
            EntryTreeView.SelectedItem = tvi;
            EntryTreeView.ScrollIntoView(tvi);
            tvi.Focus();
            UpdateEtvItem(parent);
            EntryTreeView_OnSelectionChanged(null, null);

            IsFileModified = true;
            Title = "Dialogue Helper*";
        }
    }

    async void DeleteSelectedItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureFileOpen())
            return;
        if (EntryTreeView.SelectedItem is null)
        {
            await new InfoWindow("You need to select an item before trying to delete an item.").ShowDialog(this);
            return;
        }
        var q = new QuestionWindow
        {
            InnerText = { Text = "Are you sure that you want to delete this item?\nThis action is irreversible." },
        };
        if (await q.ShowDialog<bool?>(this) ?? false)
        {
            var item = (TreeViewItem)EntryTreeView.SelectedItem;
            if (EtvKeys.TryGetValue(item, out var key))
            {
                EntryTreeView.Items.Remove(item);
                foreach (var entry in FileData!.Strings[key])
                    FileData.StringIds.Remove(entry.Key);
                FileData.Strings.Remove(key);
                EtvKeys.Remove(item);
                foreach (var node in EtvTargetChildren[item])
                {
                    EtvIndexes.Remove(node.Value);
                    EtvReverseIndexes.Remove(node.Key);
                    EtvParents.Remove(node.Value);
                }
                EtvTargetChildren.Remove(item);
            }
            else
            {
                var parent = EtvParents[item];
                if (EtvKeys.TryGetValue(parent, out key) && EtvIndexes.TryGetValue(item, out var index))
                {
                    FileData!.Strings[key].Remove(index);
                    FileData.StringIds.Remove(index);
                    {
                        var items = new List<TreeViewItem>(parent.ItemsSource?.Cast<TreeViewItem>() ?? []);
                        items.Remove(item);
                        parent.ItemsSource = items.ToArray();
                    }
                    SelectedString = null;
                    var idx = EtvIndexes[item];
                    EtvTargetChildren[parent].Remove(idx);
                    EtvIndexes.Remove(item);
                    EtvReverseIndexes.Remove(idx);
                    EtvParents.Remove(item);
                    UpdateEtvItem(parent);
                }
            }
            IsFileModified = true;
            Title = "Dialogue Helper*";
        }
    }

    async void MarkAsModified_OnClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedString is null || EntryTreeView.SelectedItem is null)
        {
            await new InfoWindow("You need to select an item before trying to mark it as modified.").ShowDialog(this);
            return;
        }
        var item = (TreeViewItem)EntryTreeView.SelectedItem;
        if (EtvKeys.ContainsKey(item))
            return;
        SelectedString.MarkAsModified = MarkAsModified.IsChecked ?? false;
        IsFileModified = true;
        Title = "Dialogue Helper*";
        UpdateEtvItem(EtvParents[item]);
        UpdateModifySimilarStrings();
    }

    async void ShowEntryInfo_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureFileOpen())
            return;
        if (SelectedString is null)
            return;
        var ts = DateTime.UnixEpoch.AddSeconds(SelectedString.LastEdited.Timestamp).ToLocalTime();
        var ei = new EntryInfo()
        {
            AuthorText =
            {
                Text = SelectedString.LastEdited.AuthorId < 0 || SelectedString.LastEdited.AuthorId >= FileData!.AuthorList.Count
                        ? "<null>"
                        : FileData.AuthorList[SelectedString.LastEdited.AuthorId],
            },
            TimeDate = { SelectedDate = ts },
            TimeTime = { SelectedTime = ts.TimeOfDay },
            StringId = { Text = $"String ID: {SelectedString.Id}" },
        };
        await ei.ShowDialog(this);
    }

    async void ShowFileInfo_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureFileOpen())
            return;
        var modi = FileData!.StringIds.Count(i =>
            i.Value.MarkAsModified || (i.Value.Text is not null && i.Value.OriginalText != i.Value.Text)
        );
        var total = FileData.StringIds.Count;
        var fi = new FileInfo()
        {
            EditInfo = { Text = $"Edited {modi} strings out of {total} ({modi / (double)total:P} completion)" },
            Authors = { Text = $"Authors: {string.Join(", ", FileData.AuthorList)}" },
        };
        await fi.ShowDialog(this);
    }

    void SimilarStringsPanel_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        SimilarStringsPanel.MaxHeight = double.Max(SimilarStringsBase.Bounds.Height - (SimilarStringsTitle.Bounds.Height - 7), 0);
    }

    void SimilarStringsPanel_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SelectedString is null || SimilarStringsPanel.SelectedItem is null || FileData is null || SimilarStringsPanel.SelectedIndex < 0 || SimilarStringsPanel.SelectedIndex >= SelectedString.EqStrings.Count)
            return;
        var eqStr = FileData.StringIds[SelectedString.EqStrings[SimilarStringsPanel.SelectedIndex]];
        var key = EtvTargetChildren.FirstOrDefault(s => s.Value.ContainsKey(eqStr.Id)).Key;
        if (key is not null)
        {
            var child = EtvTargetChildren[key][eqStr.Id];
            EntryTreeView.ExpandSubTree(key);
            EntryTreeView.SelectedItem = child;
            EntryTreeView.ScrollIntoView(child);
            child.Focus();
            EntryTreeView_OnSelectionChanged(null, null);
        }
    }

    ///<returns>returns true if file is open</returns>
    public async Task<bool> EnsureFileOpen()
    {
        if (FileData is null)
        {
            await new InfoWindow("You need to have a file open to use this functionality.").ShowDialog(this);
            return false;
        }
        return true;
    }
}
