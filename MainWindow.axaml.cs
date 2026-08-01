using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.SpellChecker;
using Avalonia.Threading;
using DialogueHelper.External;
using DialogueHelper.FileFormat;
using DialogueHelper.Options;
using DialogueHelper.StylesParser;
using DialogueHelper.Views;
using SkiaSharp;
using Views;
using static DialogueHelper.PreviewRenderer.PreviewRenderer;
using FileInfo = DialogueHelper.Views.FileInfo;
using Avalonia.VisualTree;  // needed for MainWindow_OnKeyDown

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
    public readonly Dictionary<string, SKBitmap> LoadedImages = [];
    public readonly Dictionary<(FontMetadata Font, char Char), SKBitmap> RasterizedGlyphs = [];
    public bool ScriptNeedsInitialization;
    public string AuthorName = "";
    public string AuthorGroup = "";
    public bool ShowOnlyGroupEntries;
    public readonly Dictionary<string, List<ICustomProperty>> StyleSettings = [];
    public StringContainer SelectedString = new();
    public bool UpdatingEntry;
    public readonly Dictionary<TreeViewItem, string> EtvKeys = [];
    public readonly Dictionary<string, TreeViewItem> EtvReverseKeys = [];
    public readonly Dictionary<TreeViewItem, StringContainer> EtvIndexes = [];
    public readonly Dictionary<StringContainer, TreeViewItem> EtvReverseIndexes = [];
    public readonly Dictionary<TreeViewItem, Dictionary<StringContainer, TreeViewItem>> EtvTargetChildren = [];
    public readonly Dictionary<TreeViewItem, TreeViewItem> EtvParents = []; // <child, parent>
    public List<WeakReference<TreeViewItem>> EtvTargetUpdates = [];
    public List<(WeakReference<StringContainer> StringContainer, bool ModifySimilarStrings)> EqStringUpdates = [];
    public string? LastSavePath;
    readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(0.02) };
    readonly DispatcherTimer _backupTimer = new() { Interval = TimeSpan.FromMinutes(3) };
    readonly DispatcherTimer _redrawTimer = new() { Interval = TimeSpan.FromSeconds(1d / 30d) };
    public string BgndPath = "Backgrounds/Default";
    public BackgroundConfig BgndConf = new() { Name = "Default", Author = "ryi3r" };
    public List<Image> BgndImages = [];
    internal string _lastBgndPath = "";
    public bool RedrawRenderer;
    public SpellCheckerConfig? SpellCheckerConfig;
    public string? UiFontFamily;
    public double UiFontSize = 14;
    public string DictionaryAffPath = "";
    public string DictionaryDicPath = "";


    public MainWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, MainWindow_OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        if (Design.IsDesignMode)
            return;
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

        // no dnd on linux :( unable to test.
        /*AddHandler(DragDrop.DragOverEvent, (obj, args) =>
        {
            using var f = args.DataTransfer.TryGetFile();
            if (!args.DataTransfer.Contains(DataFormat.File) || f is null || (!f.Name.EndsWith(".dh", StringComparison.InvariantCultureIgnoreCase)))
            {
                args.DragEffects = DragDropEffects.None;
                return;
            }
            args.DragEffects = DragDropEffects.Copy | DragDropEffects.Link;
        });
        AddHandler(DragDrop.DropEvent, (obj, args) =>
        {
            using var f = args.DataTransfer.TryGetFile();
            if (f is null)
                return;
            LoadFileToProject(f.Path.AbsolutePath);
        });*/
        _timer.Tick += async (_, _) =>
        {
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
                    await UpdateEtvItem(tn);
            }
            {
                (WeakReference<StringContainer> StringContainer, bool ModifySimilarStrings)? n = null;
                lock (EqStringUpdates)
                {
                    if (EqStringUpdates.Count > 0)
                    {
                        n = EqStringUpdates[0];
                        EqStringUpdates.RemoveAt(0);
                    }
                }
                if (n is not null && n.Value.StringContainer.TryGetTarget(out var sc) && FileData is not null)
                {
                    if (!n.Value.ModifySimilarStrings)
                    {
                        sc.EqStrings.Remove(sc);
                        sc.EqStrings = [];
                        foreach (var str in FileData.IterAllStrings())
                        {
                            if (str.OriginalText != sc.OriginalText)
                                continue;
                            str.EqStrings.Add(sc);
                            sc.EqStrings = str.EqStrings;
                            break;
                        }
                    }
                    if (SelectedString?.EqStrings == sc.EqStrings)
                        ReloadEqStringsField();
                    if (EtvReverseIndexes.TryGetValue(sc, out var item) && !EtvKeys.ContainsKey(item) && EtvParents.TryGetValue(item, out var p))
                    {
                        await UpdateEtvItem(p);
                        UpdateModifySimilarStrings(sc, item, false, n.Value.ModifySimilarStrings);
                    }
                }
            }
        };
        _timer.Start();
        _backupTimer.Tick += (_, _) =>
        {
            if (FileData is null)
                return;
            using var f = File.Create("backup.dh");
            f.Write(Encoding.UTF8.GetBytes(FileData.OutputString()));
            f.Close();
        };
        _backupTimer.Start();
        _redrawTimer.Tick += async (_, _) =>
        {
            if (!RedrawRenderer)
                return;
            RedrawRenderer = false;
            await UpdateEditableText(UpdateKind.None);
            await UpdateOriginalText(UpdateKind.None);
        };
        _redrawTimer.Start();
        SizeChanged += async (_, _) => await LoadBackground();
        ReloadSpellChecker();
        EditableText.PropertyChanged += TextBox_OnSelectionPropertyChanged;
        OriginalText.PropertyChanged += TextBox_OnSelectionPropertyChanged;
        Task.Run(async () =>
        {
            // todo: should this be a setting?
            var ver = typeof(AboutDialogueHelper).Assembly.GetName().Version!;
            var curVer = $"{ver.Major}.{ver.Minor}";
            if (ver.Revision > 0 || ver.Build > 0)
            {
                curVer += $".{ver.Build}";
                if (ver.Revision > 0)
                    curVer += $".{ver.Revision}";
            }

            var http = new HttpClient();
            var lastShown = (Version?)null;
            while (true)
            {
                try
                {
                    var json = await http.GetFromJsonAsync<JsonDocument>("https://git.crystal-server.net/api/v1/repos/ryi3r/DialogueHelper/releases/latest");
                    var gotVer = Version.Parse(json!.RootElement.GetProperty("tag_name").GetString()!);
                    var targetUrl = json.RootElement.GetProperty("html_url").GetString();
                    if ((targetUrl?.Length ?? 0) > 0 && gotVer != lastShown && gotVer > ver)
                    {
                        var ret = await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            var nextVer = $"{gotVer.Major}.{gotVer.Minor}";
                            if (gotVer.Revision > 0 || gotVer.Build > 0)
                            {
                                nextVer += $".{gotVer.Build}";
                                if (gotVer.Revision > 0)
                                    nextVer += $".{gotVer.Revision}";
                            }

                            var qw = new QuestionWindow("") { Width = 550 };
                            qw.Text.Inlines!.Add("Do you wish to open the release link for this new ");
                            qw.Text.Inlines.Add(new Run("Dialogue Helper") { FontWeight = FontWeight.Bold });
                            qw.Text.Inlines.Add($" release?\nYou currently have version ");
                            qw.Text.Inlines.Add(new Run(curVer) { FontWeight = FontWeight.Bold });
                            qw.Text.Inlines.Add(", and the latest version is ");
                            qw.Text.Inlines.Add(new Run(nextVer) { FontWeight = FontWeight.Bold });
                            qw.Text.Inlines.Add(".\n\n");
                            qw.Text.Inlines.Add(new Run("If you click no, you won't be asked again for the shown version on this session.") { FontWeight = FontWeight.Light, Foreground = new SolidColorBrush(Colors.Gray) });
                            return await qw.ShowDialog<bool>(this);
                        });
                        if (ret)
                            await Launcher.LaunchUriAsync(new(targetUrl!));
                        lastShown = gotVer;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"exception while trying to get the latest release ! : {ex}");
                }
                await Task.Delay(TimeSpan.FromHours(2));
            }
        });
    }

    void MainWindow_OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Tab:
                var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
                var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

                if (ctrl && shift)
                {
                    RestrictTabIndicator.IsChecked = !RestrictTabIndicator.IsChecked;
                    SaveSettings();
                    e.Handled = true;
                    break;
                }

                if (ctrl || shift)
                    break;

                if (RestrictTabIndicator.IsChecked ?? false)
                {
                    var focused = (Visual?)GetTopLevel(this)?.FocusManager.GetFocusedElement();
                    var focusInTree = focused is not null && (Equals(focused, EntryTreeView) || EntryTreeView.IsVisualAncestorOf(focused));

                    e.Handled = true;

                    Dispatcher.UIThread.Post(() =>
                    {
                        if (focusInTree)
                        {
                            EditableText.Focus(NavigationMethod.Tab);
                            // move the cursor to the beginning, because that makes sense when translating without using the mouse
                            EditableText.CaretIndex = 0;
                            EditableText.SelectionStart = 0;
                            EditableText.SelectionEnd = 0;
                        }
                        else
                        {
                            var container = EntryTreeView.SelectedItem is not null
                                ? EntryTreeView.TreeContainerFromItem(EntryTreeView.SelectedItem)
                                : null;
                            (container ?? EntryTreeView).Focus(NavigationMethod.Tab);
                        }
                    });
                }
                else
                {
                    var textBox = GetFocusedTargetTextBox();
                    if (textBox is not null)
                    {
                        InsertTabCharacter(textBox);
                        e.Handled = true;
                    }
                }
                break;
                /* switch in case we want to us this method for anything else later,
                   maybe make the mouse side back/forward buttons cycle between entries */
        }
    }

    TextBox? GetFocusedTargetTextBox()
    {
        TextBox? textBox = null;
        if (GetTopLevel(this)?.FocusManager.GetFocusedElement() is not Visual focused)
            return textBox;

        foreach (var candidate in new[] { EditableText, OriginalText, CommentText })
        {
            if (!Equals(focused, candidate) && !candidate.IsVisualAncestorOf(focused))
                continue;

            textBox = candidate;
            break;
        }

        return textBox;
    }

    static void InsertTabCharacter(TextBox textBox)
    {
        if (textBox.IsReadOnly)
            return;
        var text = textBox.Text ?? "";
        var start = Math.Min(textBox.SelectionStart, textBox.SelectionEnd);
        var end = Math.Max(textBox.SelectionStart, textBox.SelectionEnd);
        textBox.Text = text[..start] + "\t" + text[end..];
        textBox.CaretIndex = start + 1;
    }

    public void ApplyEditorFont()
    {
        var fontFamily = string.IsNullOrEmpty(UiFontFamily) ? FontFamily.Default : new(UiFontFamily);
        UpdatingEntry = true;
        foreach (var node in new[] { EditableText, OriginalText, CommentText })
        {
            node.FontFamily = fontFamily;
            if (UiFontSize <= 0)
                node.ClearValue(FontSizeProperty);
            else
                node.FontSize = UiFontSize;
        }
        // todo: fix a bug with `EditableText` not updating until you type again
        UpdatingEntry = false;
    }

    public async Task LoadBackground()
    {
        if (!Path.Exists(BgndPath))
            BgndPath = Path.GetFullPath("Backgrounds/Default");
        if (_lastBgndPath != BgndPath)
        {
            try
            {
                BgndConf = JsonSerializer.Deserialize<BackgroundConfig>(File.ReadAllText(Path.Combine(BgndPath, "Config.jsonc")), new JsonSerializerOptions { IncludeFields = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })!;
            }
            catch (Exception ex)
            {
                var se = new StyleError
                {
                    Title = "Load error",
                    MessageBlock = { Text = "DialogueHelper has encountered an error while loading the background config file" },
                    ErrorBox = { Text = ex.ToString() },
                };
                await se.ShowDialog(this);
                BgndConf = new() { Name = "Default", Author = "ryi3r" };
            }
            BgndCanvas.Children.Clear();
            BgndImages.Clear();
            Background = Brush.Parse(BgndConf.BgndColor);
        }
        foreach (var (i, simg) in BgndConf.Images.Index())
        {
            try
            {
                var img = _lastBgndPath != BgndPath ? new Image
                {
                    Source = new Bitmap(Path.Combine(BgndPath, simg.Image)),
                    Opacity = simg.Opacity,
                    StretchDirection = Enum.Parse<StretchDirection>(simg.StretchDirection),
                    Stretch = Enum.Parse<Stretch>(simg.StretchMode),
                    BlendMode = Enum.Parse<BitmapBlendingMode>(simg.BlendMode),
                } : BgndImages[i];
                Canvas.SetLeft(img, simg.ClientX ?? ((float)ClientSize.Width * simg.X));
                Canvas.SetTop(img, simg.ClientY ?? ((float)ClientSize.Height * simg.Y));
                img.Width = simg.ClientW ?? ((float)ClientSize.Width * simg.W);
                img.Height = simg.ClientH ?? ((float)ClientSize.Height * simg.H);
                if (_lastBgndPath != BgndPath)
                {
                    RenderOptions.SetBitmapInterpolationMode(img, Enum.Parse<BitmapInterpolationMode>(simg.Interpolation));
                    BgndCanvas.Children.Add(img);
                    BgndImages.Add(img);
                }
            }
            catch (Exception ex)
            {
                var se = new StyleError
                {
                    Title = "Load error",
                    MessageBlock = { Text = "DialogueHelper has encountered an error while loading the background config file" },
                    ErrorBox = { Text = ex.ToString() },
                };
                await se.ShowDialog(this);
            }
        }
        _lastBgndPath = BgndPath;
    }

    async void OpenFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (IsFileModified)
        {
            var unsaved = new UnsavedChanges();
            if (await unsaved.ShowDialog<bool>(this))
                return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(
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

        if (files.Count == 0)
            return;
        var file = files[0];

        EntryTreeView.Items.Clear();
        SimilarStringsPanel.Items.Clear();
        SimilarStringsNone.IsVisible = true;

        await LoadFileToProject(file: file);
    }

    public async Task LoadFileToProject(string? path = null, IStorageFile? file = null, bool markAsModified = false)
    {
        if (path is null && file is null)
            throw new("path and file cannot be null to load a project from a file");
        LastSavePath = file?.Path.LocalPath ?? path;
        var lWin = new LoadingWindow { CanResize = false };
        lWin.Closing += (_, ev) =>
        {
            if (!ev.IsProgrammatic)
                ev.Cancel = true;
        };
        lWin.ProgressBar.Value = 0;
        lWin.Show(this);
        lWin.Text.Text = "Loading the file...";
        using var stream = new StreamReader(file is not null ? await file.OpenReadAsync() : File.OpenRead(path!));
        await LoadStringToProject(await stream.ReadToEndAsync(), lWin, markAsModified);
    }

    public async Task LoadStringToProject(string str, LoadingWindow? lWin = null, bool markAsModified = true)
    {
        if (lWin is null)
        {
            lWin = new LoadingWindow { CanResize = false };
            lWin.Closing += (_, ev) =>
            {
                if (!ev.IsProgrammatic)
                    ev.Cancel = true;
            };
            lWin.ProgressBar.Value = 0;
            lWin.Show(this);
        }
        try
        {
            lWin.Text.Text = "Parsing the file...";
            var fParse = FileParser.ParseString(str);
            lWin.Text.Text = "Loading data...";
            await LoadRawToProject(FileData.LoadString(fParse, lWin), lWin, markAsModified);
        }
        catch (Exception ex)
        {
            var se = new StyleError
            {
                Title = "Load error",
                MessageBlock = { Text = "DialogueHelper has encountered an error while loading a file" },
                ErrorBox = { Text = ex.ToString() },
            };
            await se.ShowDialog(this);
        }
    }

    public async Task LoadRawToProject(FileData fData, LoadingWindow? lWin = null, bool markAsModified = true)
    {
        if (lWin is null)
        {
            lWin = new LoadingWindow { CanResize = false };
            lWin.Closing += (_, ev) =>
            {
                if (!ev.IsProgrammatic)
                    ev.Cancel = true;
            };
            lWin.ProgressBar.Value = 0;
            lWin.Show(this);
        }
        try
        {
            FileData = fData;
            lWin.Text.Text = "Caching base entries...";
            lWin.ProgressBar.Value = 0d;
            lWin.ProgressBar.Maximum = FileData.GetCountString();

            var eqStr = new Dictionary<string, List<StringContainer>>();
            var cur = 0;
            foreach (var strCont in FileData!.IterAllStrings())
            {
                if (eqStr.TryGetValue(strCont.OriginalText, out var arr))
                {
                    strCont.EqStrings = arr;
                    arr.Add(strCont);
                }
                else
                {
                    arr = [strCont];
                    eqStr.Add(strCont.OriginalText, arr);
                    strCont.EqStrings = arr;
                }

                lWin.ProgressBar.Value = cur++;
            }

            lWin.Text.Text = "Updating view...";
            await PopulateEntryTreeView();
        }
        catch (Exception ex)
        {
            var se = new StyleError
            {
                Title = "Load error",
                MessageBlock = { Text = "DialogueHelper has encountered an error while loading a file" },
                ErrorBox = { Text = ex.ToString() },
            };
            await se.ShowDialog(this);
        }

        lWin.Close();
        IsFileModified = markAsModified;
        Title = $"Dialogue Helper{(markAsModified ? "*" : "")}";
    }

    public async Task PopulateEntryTreeView()
    {
        EntryTreeView.Items.Clear();
        EtvKeys.Clear();
        EtvIndexes.Clear();
        EtvReverseIndexes.Clear();
        EtvTargetChildren.Clear();
        EtvReverseKeys.Clear();
        EtvParents.Clear();
        if (!await EnsureFileOpen())
            return;
        foreach (var (key, strs) in FileData!.GetStringKeysValues().ToList())
        {
            var cont = new TreeViewItem { Header = key };
            EntryTreeView.Items.Add(cont);
            EtvKeys.Add(cont, key);
            EtvReverseKeys.Add(key, cont);
            var data = new Dictionary<StringContainer, TreeViewItem>();
            EtvTargetChildren.Add(cont, data);
            var i = 0;
            var modified = 0;
            foreach (var str in strs)
            {
                var tBlock = new TreeViewItem { Header = str.Text ?? str.OriginalText };
                ToolTip.SetTip(tBlock, $"String ID: {str.Id}");
                EtvParents.Add(tBlock, cont);
                ColorTreeItem(tBlock, str.Text, str.OriginalText, str.MarkAsModified);
                if (str.Text is not null && str.Text != str.OriginalText)
                    tBlock.Header = str.Text;
                if ((str.Text is not null && str.Text != str.OriginalText) || str.MarkAsModified)
                    modified++;
                i++;
                EtvIndexes.Add(tBlock, str);
                EtvReverseIndexes.Add(str, tBlock);
                data.Add(str, tBlock);
            }

            foreach (var v in data.OrderBy(v => strs.IndexOf(v.Key)).Select(v => v.Value))
                cont.Items.Add(v);
            cont.Foreground = SolidColorBrush.Parse(modified != 0 ? (modified >= i ? "LightGreen" : "MediumPurple") : "LightCoral");
        }
        await UpdateSearchFiltering();
    }

    public async Task UpdateEtvItem(TreeViewItem item)
    {
        if (!await EnsureFileOpen() || !EtvKeys.ContainsKey(item) || !EtvTargetChildren.ContainsKey(item))
            return;
        var strs = FileData!.GetString(EtvKeys[item])!;

        var i = 0;
        var modified = 0;
        var list = EtvTargetChildren[item];
        if (strs.Count != list.Count)
        {
            Console.WriteLine("etv mismatch, repopulating...");
            await PopulateEntryTreeView();
            return;
        }
        foreach (var str in strs)
        {
            if (!list.TryGetValue(str, out var tvi))
            {
                Console.WriteLine("etv inst mismatch, repopulating...");
                await PopulateEntryTreeView();
                return;
            }
            ColorTreeItem(tvi, str.Text, str.OriginalText, str.MarkAsModified);
            if (str.MarkAsModified)
                modified++;
            if (str.Text is not null)
            {
                tvi.Header = str.Text;
                if (str.Text != str.OriginalText && !str.MarkAsModified)
                    modified++;
            }
            i++;
        }

        item.Foreground = SolidColorBrush.Parse(modified != 0 ? (modified >= i ? "LightGreen" : "MediumPurple") : "LightCoral");
        await UpdateSearchFiltering(new() { [item] = EtvTargetChildren[item] });
    }

    public static void ColorTreeItem(TreeViewItem item, string? text, string ogText, bool markAsModified) => item.Foreground = SolidColorBrush.Parse((text is not null && text != ogText) || markAsModified ? "LightGreen" : "White");

    void OnLayoutUpdated(object? sender, EventArgs e)
    {
        EntryTreeView.Height = double.Max(LeftStackPanel.Bounds.Height - EntryTextBox.DesiredSize.Height - 11, 0);
        //ScrollViewer.MaxHeight = LeftStackPanel.Bounds.Height - TextPanel.DesiredSize.Height;
    }

    public async void EntryTreeView_OnSelectionChanged(object? sender, SelectionChangedEventArgs? e)
    {
        if (!await EnsureFileOpen(false))
            return;
        if (EntryTreeView.SelectedItem is not TreeViewItem tvi)
            return;
        RenameBlock.IsVisible = EtvKeys.ContainsKey(tvi);
        if (EtvIndexes.TryGetValue(tvi, out var sc))
        {
            //Console.WriteLine("============================");

            UpdatingEntry = true;
            AllowEditingOriginal.IsChecked = false;
            AllowEditingOriginal_OnClick(null, null!);
            var lastSel = SelectedString;
            SelectedString = sc;

            MarkAsModified.IsChecked = sc.MarkAsModified;
            OriginalText.IsUndoEnabled = false;
            OriginalText.Text = sc.OriginalText;
            OriginalText.IsUndoEnabled = true;
            EditableText.IsUndoEnabled = false;
            EditableText.Text = sc.Text ?? sc.OriginalText;
            EditableText.IsUndoEnabled = true;
            StringKey.IsUndoEnabled = false;
            StringKey.Text = sc.Key;
            StringKey.IsUndoEnabled = true;
            CommentText.IsUndoEnabled = false;
            CommentText.Text = sc.Comment;
            CommentText.IsUndoEnabled = true;


            await PopulateCustomPropertiesValues();
            if (lastSel.EqStrings != sc.EqStrings)
                ReloadEqStringsField();

            UpdateCustomScriptFields();

            {
                var targetSel = sc;
                //Console.WriteLine("updating");
                if (CopyViewingStrings.IsChecked ?? false)
                    targetSel = lastSel;
                var markAsModified = false;

                if (sc.BoxStyle != targetSel.BoxStyle)
                    markAsModified = true;
                //Console.WriteLine($"update box style->{BoxComboBox.SelectedIndex},{targetSel.BoxStyle}");
                sc.BoxStyle = targetSel.BoxStyle;
                BoxComboBox.SelectedIndex = targetSel.BoxStyle;
                if (sc.BoxScale != targetSel.BoxScale)
                    markAsModified = true;
                //Console.WriteLine($"update box scale->{BoxScale.Value},{targetSel.BoxScale}");
                sc.BoxScale = targetSel.BoxScale;
                BoxScale.Value = (decimal)targetSel.BoxScale;

                if (sc.FontStyle != targetSel.FontStyle)
                    markAsModified = true;
                //Console.WriteLine($"update font style->{FontComboBox.SelectedIndex},{targetSel.FontStyle}");
                sc.FontStyle = targetSel.FontStyle;
                FontComboBox.SelectedIndex = targetSel.FontStyle;
                if (sc.FontScale != targetSel.FontScale)
                    markAsModified = true;
                //Console.WriteLine($"update font scale->{FontScale.Value},{targetSel.FontScale}");
                sc.FontScale = targetSel.FontScale;
                FontScale.Value = (decimal)targetSel.FontScale;

                if (sc.PreviewScale != targetSel.PreviewScale)
                    markAsModified = true;
                //Console.WriteLine($"update preview scale->{PreviewScale.Value},{targetSel.PreviewScale}");
                sc.PreviewScale = targetSel.PreviewScale;
                PreviewScale.Value = (decimal)targetSel.PreviewScale;

                if (sc.BoxColor.Rgba != targetSel.BoxColor.Rgba)
                    markAsModified = true;
                //Console.WriteLine($"update box color->{BoxColor.Color.ToUInt32():X6},{targetSel.BoxColor.Argb:X6}");
                sc.BoxColor.Rgba = targetSel.BoxColor.Rgba;
                BoxColor.Color = Avalonia.Media.Color.FromUInt32(targetSel.BoxColor.Argb);

                if (sc.TextColor.Rgba != targetSel.TextColor.Rgba)
                    markAsModified = true;
                //Console.WriteLine($"update text color->{InitialTextColor.Color.ToUInt32():X6},{targetSel.TextColor.Argb:X6}");
                sc.TextColor.Rgba = targetSel.TextColor.Rgba;
                InitialTextColor.Color = Avalonia.Media.Color.FromUInt32(targetSel.TextColor.Argb);

                if (markAsModified)
                {
                    //Console.WriteLine("marked as modified");
                    if (FileData is not null)
                    {
                        IsFileModified = true;
                        Title = "Dialogue Helper*";
                    }
                }
                //Console.WriteLine("OK");
            }

            UpdatingEntry = false;

            await UpdateEditableText(UpdateKind.ChangedEntry);
            await UpdateOriginalText(UpdateKind.ChangedEntry);
        }
        else
            SelectedString = new();
    }

    public async Task PopulateCustomPropertiesValues()
    {
        if (SelectedString is null)
            return;
        // todo: catch any errors that may arise from doing this
        try
        {
            foreach (var prop in CustomProperties)
            {
                if (SelectedString.CustomProperties.TryGetValue(prop.Name, out var value))
                    prop.StringToValue(value);
                else if (prop is ICustomPropertyList pl)
                    pl.SelectedIndex = pl.DefaultIndex;
                else
                    prop.Value = prop.DefaultValue;
            }
        }
        catch (Exception ex)
        {
            var se = new StyleError
            {
                Title = "Save error",
                MessageBlock = { Text = "DialogueHelper has encountered an error while saving a file" },
                ErrorBox = { Text = ex.ToString() },
            };
            await se.ShowDialog(this);
        }
    }

    public void ReloadEqStringsField()
    {
        SimilarStringsNone.IsVisible = (SelectedString?.EqStrings.Count ?? 0) <= 0;
        SimilarStringsPanel.SelectedIndex = -1;
        SimilarStringsPanel.Items.Clear();
        if (SelectedString is not null && FileData is not null)
        {
            foreach (var sstr in SelectedString.EqStrings)
                SimilarStringsPanel.Items.Add($"({sstr.Id}) {((sstr.Text is not null && sstr.Text != sstr.OriginalText) ? sstr.Text : sstr.OriginalText)}");
        }
    }

    void UpdateBoxBounds()
    {
        if (ScrollViewerOriginal is null || ScrollViewer is null)
            return;

        ScrollViewerOriginal.Height = double.Max(PanelOriginal.Bounds.Height - OriginalLabel.DesiredSize.Height, 0);

        ScrollViewer.Height = double.Max(Panel.Bounds.Height - EditableLabel.DesiredSize.Height - OuterGridSplitter.Bounds.Height - 5, 0);
        Panel.Height = double.Max(MiddleStackPanel.Bounds.Height - PanelOriginal.Bounds.Height - InnerGridSplitter.Bounds.Height, 0);

        OuterMiddleStackPanel.MaxHeight = double.Max(MainGrid.Bounds.Height - 10, 0);
    }

    async void Settings_OnClick(object? sender, RoutedEventArgs e)
    {
        var settings = new Settings(this);
        await settings.ShowDialog(this);

        if (ScriptNeedsInitialization)
        {
            await InitializeCustomScript();

            UpdatingEntry = true;
            await UpdateEditableText(UpdateKind.InitializedStyle);
            await UpdateOriginalText(UpdateKind.InitializedStyle);
            UpdatingEntry = false;
        }
    }

    public async Task LoadSettings()
    {
        var file = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/DialogueHelper/settings.txt";
        if (!File.Exists(file))
            return;
        var f = File.ReadAllLines(file);

        GitOptions.IsEnabled = f[0] == "1";
        GitOptions.RepoUrl = WebUtility.UrlDecode(f[1]);
        GitOptions.Branch = WebUtility.UrlDecode(f[2]);

        AuthorGroup = WebUtility.UrlDecode(f[3]);
        AuthorName = WebUtility.UrlDecode(f[4]);

        SelectedStyle = f[5].Length <= 0 ? null : WebUtility.UrlDecode(f[5]);

        BgndPath = WebUtility.UrlDecode(f[6]);
        if (!Path.Exists(BgndPath))
            BgndPath = Path.GetFullPath("Backgrounds/Default");

        var i = 7;
        var entryType = "";
        while (i < f.Length)
        {
            if (entryType == "")
                entryType = f[i];
            else if (f[i] == "@@end@@")
                entryType = "";
            else
            {
                var idx = entryType.IndexOf("@@", StringComparison.InvariantCulture);
                switch (entryType[..idx])
                {
                    case "StyleSettings":
                        {
                            var folder = WebUtility.UrlDecode(entryType[(idx + 2)..]);
                            StyleSettings.TryAdd(folder, []);
                            var spl = f[i].Split("@@");
                            // the visual name & type value will get overwritten once the style loads
                            StyleSettings[folder].Add(new CustomProperty<string>("<null>", null, WebUtility.UrlDecode(spl[0]), WebUtility.UrlDecode(spl[1])));
                        }
                        break;
                    case "RestrictTab":
                        RestrictTabIndicator.IsChecked = bool.Parse(f[i]);
                        break;
                    case "DictPaths":
                        if (DictionaryAffPath.Length == 0)
                            DictionaryAffPath = f[i];
                        else
                            DictionaryDicPath = f[i];
                        break;
                    case "EditorFontFamily":
                        UiFontFamily = f[i].Length > 0 ? f[i] : null;
                        break;
                    case "EditorFontSize":
                        if (double.TryParse(f[i], CultureInfo.InvariantCulture, out var parsedSize))
                            UiFontSize = parsedSize;
                        break;

                }
            }
            i++;
        }

        await LoadBackground();
    }

    public void SaveSettings()
    {
        {
            var dir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DialogueHelper/");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
        var l = new List<string>() {
            GitOptions.IsEnabled ? "1" : "0",
            WebUtility.UrlEncode(GitOptions.RepoUrl),
            WebUtility.UrlEncode(GitOptions.Branch),
            WebUtility.UrlEncode(AuthorGroup),
            WebUtility.UrlEncode(AuthorName),
            WebUtility.UrlEncode(SelectedStyle ?? ""),
            WebUtility.UrlEncode(BgndPath),
            "RestrictTab@@",
            (RestrictTabIndicator.IsChecked ?? false).ToString(CultureInfo.InvariantCulture),
            "@@end@@",
            "EditorFontFamily@@",
            UiFontFamily ?? "",
            "@@end@@",
            "EditorFontSize@@",
            UiFontSize.ToString(CultureInfo.InvariantCulture),
            "@@end@@",
            "DictPaths@@",
            DictionaryAffPath,
            DictionaryDicPath,
            "@@end@@"
        };
        foreach (var ss in StyleSettings)
        {
            l.Add($"StyleSettings@@{WebUtility.UrlEncode(ss.Key)}");
            l.AddRange(ss.Value.Select(prop => $"{WebUtility.UrlEncode(prop.Name)}@@{WebUtility.UrlEncode(prop.ValueToString())}"));
            l.Add("@@end@@");
        }
        File.WriteAllLines($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/DialogueHelper/settings.txt", l);
    }

    async void OnOpened(object? sender, EventArgs e)
    {
        var w = new LoadingWindow
        {
            ProgressBar = {
                IsIndeterminate = true,
                ShowProgressText = false,
            },
            Text = { Text = "Initializing..." },
        };
        w.Show(this);
        if (Directory.Exists("RuntimeComp"))
        {
            // clear previously compiled files as it's unnecessary in this run because it'll be recompiled.
            foreach (var f in Directory.EnumerateFiles("RuntimeComp").Where(x => x.Contains(".dll") && x.Contains("IL-")))
                File.Delete(f);
        }
        MiddleStackPanel.RowDefinitions[0].Height = new GridLength(235 * ClientSize.Height / 720);
        MiddleStackPanel.RowDefinitions[2].Height = new GridLength(230 * ClientSize.Height / 720);
        await LoadSettings();
        ApplyEditorFont();
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
                await InitializeCustomScript();
            }
            catch (Exception ex)
            {
                w.Hide();

                var sErr = new StyleError { ErrorBox = { Text = ex.ToString() } };
                sErr.MessageBlock.Text = sErr.MessageBlock.Text!.Replace("StyleName", SelectedStyle);
                await sErr.ShowDialog(this);
                var settings = new Settings(this) { Init = true, IgnoreStyles = [SelectedStyle] };
                await settings.ShowDialog(this);

                w.Show(this);
            }
        }

        await InitializeCustomScript();
        await UpdateEditableText(UpdateKind.InitializedStyle);
        await UpdateOriginalText(UpdateKind.InitializedStyle);

        w.Close();
        var lastArg = Environment.GetCommandLineArgs()[^1];
        if (lastArg.EndsWith(".dh"))
            await LoadFileToProject(lastArg);
    }

    public async Task InitializeCustomScript()
    {
        foreach (var img in LoadedImages.Values)
            img.Dispose();
        LoadedImages.Clear();

        foreach (var img in RasterizedGlyphs.Values)
            img.Dispose();
        RasterizedGlyphs.Clear();

        try
        {
            await (StyleData!.Script?.Init(new(this, StyleData)) ?? Task.CompletedTask);
            CustomTools.Clear();
            CustomProperties.Clear();

            var lastBoxSelected = int.Clamp(BoxComboBox.SelectedIndex, 0, StyleData.BoxMetadata.Count - 1);
            var lastFontSelected = int.Clamp(FontComboBox.SelectedIndex, 0, StyleData.FontMetadata.Count - 1);
            BoxComboBox.Items.Clear();
            FontComboBox.Items.Clear();

            foreach (var box in StyleData.BoxMetadata)
                BoxComboBox.Items.Add(new ComboBoxItem { Content = box.Name });

            foreach (var font in StyleData.FontMetadata)
                FontComboBox.Items.Add(new ComboBoxItem { Content = font.Name });

            BoxComboBox.SelectedIndex = lastBoxSelected;
            FontComboBox.SelectedIndex = lastFontSelected;

            CustomTools = [.. await (StyleData.Script?.RegisterCustomTools() ?? Task.FromResult(Enumerable.Empty<CustomTool>()))];
            CustomProperties = [.. await (StyleData.Script?.RegisterCustomProperties() ?? Task.FromResult(Enumerable.Empty<ICustomProperty>()))];

            StyleSettings.TryAdd(SelectedStyle!, []);
            var res = await (StyleData.Script?.RegisterCustomSettings() ?? Task.FromResult(Enumerable.Empty<ICustomProperty>()));
            var props = StyleSettings[SelectedStyle!];
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
            // ensure that no custom tool (broken) data may trigger more errors down the line
            CustomTools.Clear();
            CustomProperties.Clear();

            var se = new StyleError
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
            CustomToolsMenu.Items.Add(new MenuItem { IsEnabled = false, Header = "Empty" });
        else
        {
            foreach (var tool in CustomTools)
            {
                var item = new MenuItem { Header = tool.Name };
                ToolTip.SetTip(item, tool.Tooltip);
                if (tool.HotKey is not null)
                {
                    try
                    {
                        item.HotKey = KeyGesture.Parse(tool.HotKey);
                        item.InputGesture = item.HotKey;
                    }
                    catch (Exception)
                    {
                        // invalid user hotkey
                    }
                }
                item.Click += async (_, _) => await RunCustomTool(tool);
                CustomToolsMenu.Items.Add(item);
            }
        }
        CustomPropertiesPanel.Children.Clear();
        if (CustomProperties.Count <= 0)
            CustomPropertiesNone.IsVisible = true;
        else
        {
            CustomPropertiesNone.IsVisible = false;
            UpdateCustomProperties(CustomPropertiesPanel, CustomProperties.AsEnumerable(), true);
        }
    }

    async Task RunCustomTool(CustomTool tool)
    {
        try
        {
            await tool.Func(new CustomToolWindowHandler(this, StyleData!));
        }
        catch (Exception ex)
        {
            var se = new StyleError
            {
                MessageBlock = { Text = "The custom tool has encountered an error" },
                ErrorBox = { Text = ex.ToString() },
            };
            await se.ShowDialog(this);
        }
    }

    public void UpdateCustomProperties(Panel panel, IEnumerable<ICustomProperty> props, bool isString = false)
    {
        foreach (var tool in props)
        {
            var item = new StackPanel { Orientation = Orientation.Horizontal, Margin = new(5) };
            item.Children.Add(new TextBlock { Margin = new(7), Text = tool.VisualName });
            // todo: probably could remove most of these cases and compact them into 1
            switch (tool.Type)
            {
                case not null when tool.Type == typeof(bool):
                    {
                        item.Children.Clear();
                        var node = new CheckBox
                        {
                            IsChecked = (tool.Value as bool?) ?? false,
                            IsThreeState = false,
                            Content = tool.VisualName,
                            IsEnabled = !tool.ReadOnly,
                        };
                        node.IsCheckedChanged += async (_, _) =>
                        {
                            tool.StringToValue((node.IsChecked ?? false) ? "true" : "false");
                            if (isString)
                            {
                                await UpdateEditableText(UpdateKind.CustomPropertyChanged);
                                await UpdateOriginalText(UpdateKind.CustomPropertyChanged);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;

                case not null when tool.Type == typeof(byte):
                    {
                        var node = new NumericUpDown
                        {
                            Value = (tool.Value as byte?) ?? 0,
                            Minimum = 0,
                            Maximum = 0xff,
                            FormatString = "0",
                            Increment = 1,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += async (_, _) =>
                        {
                            tool.StringToValue((node.Value ?? 0).ToString());
                            if (isString)
                            {
                                await UpdateEditableText(UpdateKind.CustomPropertyChanged);
                                await UpdateOriginalText(UpdateKind.CustomPropertyChanged);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;
                case not null when tool.Type == typeof(short):
                    {
                        var node = new NumericUpDown
                        {
                            Value = (tool.Value as short?) ?? 0,
                            Minimum = -0x8000,
                            Maximum = 0x7fff,
                            FormatString = "0",
                            Increment = 1,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += async (_, _) =>
                        {
                            tool.StringToValue((node.Value ?? 0).ToString());
                            if (isString)
                            {
                                await UpdateEditableText(UpdateKind.CustomPropertyChanged);
                                await UpdateOriginalText(UpdateKind.CustomPropertyChanged);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;
                case not null when tool.Type == typeof(int):
                    {
                        var node = new NumericUpDown
                        {
                            Value = (tool.Value as int?) ?? 0,
                            Minimum = -0x80000000,
                            Maximum = 0x7fffffff,
                            FormatString = "0",
                            Increment = 1,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += async (_, _) =>
                        {
                            tool.StringToValue((node.Value ?? 0).ToString());
                            if (isString)
                            {
                                await UpdateEditableText(UpdateKind.CustomPropertyChanged);
                                await UpdateOriginalText(UpdateKind.CustomPropertyChanged);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;
                case not null when tool.Type == typeof(long):
                    {
                        var node = new NumericUpDown
                        {
                            Value = (tool.Value as long?) ?? 0,
                            Minimum = -0x8000000000000000,
                            Maximum = 0x7fffffffffffffff,
                            FormatString = "0",
                            Increment = 1,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += async (_, _) =>
                        {
                            tool.StringToValue((node.Value ?? 0).ToString());
                            if (isString)
                            {
                                await UpdateEditableText(UpdateKind.CustomPropertyChanged);
                                await UpdateOriginalText(UpdateKind.CustomPropertyChanged);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;

                case not null when tool.Type == typeof(sbyte):
                    {
                        var node = new NumericUpDown
                        {
                            Value = (tool.Value as sbyte?) ?? 0,
                            Minimum = -0x80,
                            Maximum = 0x7f,
                            FormatString = "0",
                            Increment = 1,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += async (_, _) =>
                        {
                            tool.StringToValue((node.Value ?? 0).ToString());
                            if (isString)
                            {
                                await UpdateEditableText(UpdateKind.CustomPropertyChanged);
                                await UpdateOriginalText(UpdateKind.CustomPropertyChanged);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;
                case not null when tool.Type == typeof(ushort):
                    {
                        var node = new NumericUpDown
                        {
                            Value = (tool.Value as ushort?) ?? 0,
                            Minimum = 0,
                            Maximum = 0xffff,
                            FormatString = "0",
                            Increment = 1,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += async (_, _) =>
                        {
                            tool.StringToValue((node.Value ?? 0).ToString());
                            if (isString)
                            {
                                await UpdateEditableText(UpdateKind.CustomPropertyChanged);
                                await UpdateOriginalText(UpdateKind.CustomPropertyChanged);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;
                case not null when tool.Type == typeof(uint):
                    {
                        var node = new NumericUpDown
                        {
                            Value = (tool.Value as uint?) ?? 0,
                            Minimum = 0,
                            Maximum = 0xffffffff,
                            FormatString = "0",
                            Increment = 1,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += async (_, _) =>
                        {
                            tool.StringToValue((node.Value ?? 0).ToString());
                            if (isString)
                            {
                                await UpdateEditableText(UpdateKind.CustomPropertyChanged);
                                await UpdateOriginalText(UpdateKind.CustomPropertyChanged);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;
                case not null when tool.Type == typeof(ulong):
                    {
                        var node = new NumericUpDown
                        {
                            Value = (tool.Value as ulong?) ?? 0,
                            Minimum = 0,
                            Maximum = 0xffffffffffffffff,
                            FormatString = "0",
                            Increment = 1,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += async (_, _) =>
                        {
                            tool.StringToValue((node.Value ?? 0).ToString());
                            if (isString)
                            {
                                await UpdateEditableText(UpdateKind.CustomPropertyChanged);
                                await UpdateOriginalText(UpdateKind.CustomPropertyChanged);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;

                case not null when tool.Type == typeof(float):
                    {
                        var node = new NumericUpDown
                        {
                            Value = (decimal)((tool.Value as float?) ?? 0),
                            FormatString = "0.00",
                            Increment = (decimal)0.01f,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += async (_, _) =>
                        {
                            tool.StringToValue((node.Value ?? 0).ToString());
                            if (isString)
                            {
                                await UpdateEditableText(UpdateKind.CustomPropertyChanged);
                                await UpdateOriginalText(UpdateKind.CustomPropertyChanged);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;
                case not null when tool.Type == typeof(double):
                    {
                        var node = new NumericUpDown
                        {
                            Value = (decimal)((tool.Value as double?) ?? 0),
                            FormatString = "0.00",
                            Increment = (decimal)0.01,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.ValueChanged += async (_, _) =>
                        {
                            tool.StringToValue((node.Value ?? 0).ToString());
                            if (isString)
                            {
                                await UpdateEditableText(UpdateKind.CustomPropertyChanged);
                                await UpdateOriginalText(UpdateKind.CustomPropertyChanged);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;

                case not null when tool.Type == typeof(string):
                    {
                        var node = new TextBox
                        {
                            Text = tool.Value as string,
                            IsReadOnly = tool.ReadOnly,
                        };
                        node.TextChanged += async (_, _) =>
                        {
                            tool.StringToValue(node.Text ?? "");
                            if (isString)
                            {
                                await UpdateEditableText(UpdateKind.CustomPropertyChanged);
                                await UpdateOriginalText(UpdateKind.CustomPropertyChanged);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;

                case not null when tool.Type == typeof(Color):
                    {
                        var col = (Color)tool.Value!;
                        var node = new ColorPicker
                        {
                            IsEnabled = !tool.ReadOnly,
                            Color = new(col.R, col.G, col.B, col.A),
                            IsAlphaEnabled = col.IsAlphaEnabled,
                            IsAlphaVisible = col.IsAlphaVisible,
                        };
                        node.ColorChanged += async (_, _) =>
                        {
                            tool.StringToValue(new Color { Argb = node.Color.ToUInt32() }.Rgba.ToString() ?? "");
                            if (isString)
                            {
                                await UpdateEditableText(UpdateKind.CustomPropertyChanged);
                                await UpdateOriginalText(UpdateKind.CustomPropertyChanged);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;

                case not null when tool.Type == typeof(ICustomPropertyList):
                    {
                        var pl = (ICustomPropertyList)tool;
                        var d = new ComboBoxItem[pl.Data?.Count ?? 0];
                        for (var i = 0; i < (pl.Data?.Count ?? 0); i++)
                        {
                            d[i] = new() { Content = pl.Data?[i]?.ToString() ?? "<null>" };
                            ToolTip.SetTip(d[i], i < (pl.ItemTooltips?.Count ?? 0) ? pl.ItemTooltips?[i] : null);
                        }
                        var node = new ComboBox
                        {
                            IsEnabled = !tool.ReadOnly,
                            ItemsSource = d,
                            SelectedIndex = pl.SelectedIndex,
                        };
                        node.SelectionChanged += async (_, _) =>
                        {
                            tool.StringToValue(node.SelectedIndex.ToString());
                            if (isString)
                            {
                                await UpdateEditableText(UpdateKind.CustomPropertyChanged);
                                await UpdateOriginalText(UpdateKind.CustomPropertyChanged);
                            }
                        };
                        tool.Node = node;
                        item.Children.Add(node);
                    }
                    break;
            }
            if (tool.Node is not null)
                ToolTip.SetTip(tool.Node, tool.Tooltip);
            panel.Children.Add(item);
        }
    }

    async void EditableText_OnTextChanging(object? sender, TextChangingEventArgs? e) => await UpdateEditableText(UpdatingEntry ? UpdateKind.None : UpdateKind.EditableTextChanged);

    //System.Diagnostics.Stopwatch _sw = System.Diagnostics.Stopwatch.StartNew();

    async Task UpdateEditableText(UpdateKind updateKind)
    {
        if (PreviewRenderer is null)
            return;
        //Console.WriteLine($"{_sw.ElapsedMilliseconds / 1000d:0.00}@redraw ed->{updateKind}");
        if (!UpdatingEntry && updateKind is UpdateKind.EditableTextChanged or UpdateKind.CustomPropertyChanged or UpdateKind.SelectedBoxChanged or UpdateKind.SelectedFontChanged or UpdateKind.ScaleChanged or UpdateKind.ColorChanged)
        {
            //Console.WriteLine($"{_sw.ElapsedMilliseconds / 1000d:0.00}@updated ed entry->{updateKind}");
            if (FileData is not null)
            {
                IsFileModified = true;
                Title = "Dialogue Helper*";
            }

            SelectedString.BoxStyle = BoxComboBox.SelectedIndex;
            SelectedString.BoxScale = ((float?)BoxScale.Value) ?? 0;
            SelectedString.FontStyle = FontComboBox.SelectedIndex;
            SelectedString.FontScale = ((float?)FontScale.Value) ?? 0;
            SelectedString.PreviewScale = ((float?)PreviewScale.Value) ?? 0;
            SelectedString.TextColor = new() { Argb = InitialTextColor.Color.ToUInt32() };
            SelectedString.BoxColor = new() { Argb = BoxColor.Color.ToUInt32() };

            SelectedString.Text = EditableText.Text ?? "<null>";
            foreach (var prop in CustomProperties)
            {
                if (prop is ICustomPropertyList pl)
                {
                    if (pl.SelectedIndex != pl.DefaultIndex)
                        SelectedString.CustomProperties[prop.Name] = prop.ValueToString();
                    else
                        SelectedString.CustomProperties.Remove(prop.Name);
                }
                else if (!Equals(prop.Value, prop.DefaultValue))
                    SelectedString.CustomProperties[prop.Name] = prop.ValueToString();
                else
                    SelectedString.CustomProperties.Remove(prop.Name);
            }

            if (FileData is not null && SelectedString.Text != SelectedString.OriginalText)
            {
                if (!FileData.AuthorList.Contains(AuthorName))
                    FileData.AuthorList.Add(AuthorName);
                SelectedString.LastEdited.AuthorId = FileData.AuthorList.IndexOf(AuthorName);
                SelectedString.LastEdited.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            UpdateModifySimilarStrings(null, null);
        }
        PreviewRenderer.CreateRender(
            updateKind,
            StyleData!,
            SelectedString,
            EditableText.Text ?? "",
            DisplayEditableCanvas
        );
        UpdateBoxBounds();
    }

    async void OriginalText_OnTextChanging(object? sender, TextChangingEventArgs? e) => await UpdateOriginalText(UpdatingEntry ? UpdateKind.None : UpdateKind.OriginalTextChanged);

    async Task UpdateOriginalText(UpdateKind updateKind)
    {
        if (PreviewRenderer is null)
            return;
        //Console.WriteLine($"{_sw.ElapsedMilliseconds / 1000d:0.00}@redraw og->{updateKind}");
        if (!UpdatingEntry && FileData is not null && updateKind is UpdateKind.OriginalTextChanged)
        {
            //Console.WriteLine($"{_sw.ElapsedMilliseconds / 1000d:0.00}@updated og entry->{updateKind}");
            if (!FileData.AuthorList.Contains(AuthorName))
                FileData.AuthorList.Add(AuthorName);
            SelectedString.LastEdited.AuthorId = FileData.AuthorList.IndexOf(AuthorName);
            SelectedString.LastEdited.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            SelectedString.Text ??= SelectedString.OriginalText;
            SelectedString.OriginalText = OriginalText.Text ?? "";
            lock (EqStringUpdates)
            {
                if (!EqStringUpdates.Any(x => x.StringContainer.TryGetTarget(out var t) && t == SelectedString))
                    EqStringUpdates.Add((new(SelectedString), ModifySimilarStrings.IsChecked!.Value));
            }
            IsFileModified = true;
            Title = "Dialogue Helper*";
        }
        PreviewRenderer.CreateRender(
            updateKind,
            StyleData!,
            SelectedString,
            OriginalText.Text ?? "",
            DisplayOriginalCanvas
        );
        UpdateBoxBounds();
    }

    void CommentText_OnTextChanging(object? sender, TextChangingEventArgs? e)
    {
        if (SelectedString is not null && !UpdatingEntry && FileData is not null && e is not null)
        {
            SelectedString.Comment = CommentText.Text ?? "";
            IsFileModified = true;
            Title = "Dialogue Helper*";
        }
    }

    async void ForceStyleReload_OnClick(object? sender, RoutedEventArgs e)
    {
        var lWin = new LoadingWindow { Title = "Reloading style...", CanResize = false, ProgressBar = { IsIndeterminate = true } };
        lWin.Closing += (_, ev) =>
        {
            if (!ev.IsProgrammatic)
                ev.Cancel = true;
        };
        lWin.Show(this);
        lWin.Text.Text = "Reloading style...";

        try
        {
            StyleData = new StyleParser(StyleData!.Folder);
            await StyleData.CompileCode();
            await InitializeCustomScript();
        }
        catch (Exception ex)
        {
            var sErr = new StyleError { ErrorBox = { Text = ex.ToString() } };
            sErr.MessageBlock.Text = sErr.MessageBlock.Text!.Replace("StyleName", SelectedStyle);
            await sErr.ShowDialog(this);
        }
        await PopulateCustomPropertiesValues();
        UpdateCustomScriptFields();
        UpdatingEntry = true;
        await UpdateEditableText(UpdateKind.ReloadedStyle);
        await UpdateOriginalText(UpdateKind.ReloadedStyle);
        UpdatingEntry = false;

        lWin.Close();
    }

    async void OnScaleValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        await UpdateEditableText(UpdateKind.ScaleChanged);
        await UpdateOriginalText(UpdateKind.ScaleChanged);
    }

    void TextBox_OnSelectionPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != TextBox.SelectionStartProperty && e.Property != TextBox.SelectionEndProperty)
            return;
        if (sender is not TextBox textBox)
            return;

        var length = int.Abs(textBox.SelectionEnd - textBox.SelectionStart);
        SelectionCharCountLabel.Text = length > 0 ? $"{length} character{(length == 1 ? "" : "s")} selected" : "";
    }

    async void ShowSpellcheckUnderline_OnClick(object? sender, RoutedEventArgs e)
    {
        await UpdateEditableText(UpdateKind.None);
        await UpdateOriginalText(UpdateKind.None);
    }

    public void ReloadSpellChecker()
    {
        var isFirstInit = SpellCheckerConfig is null;

        var dictPath = Path.Combine(AppContext.BaseDirectory, "SpellCheckDictionaries");
        var conf = new SpellCheckerConfig
        {
            DictionariesFolder = dictPath,
            EnabledLanguages = [.. Directory.EnumerateFiles(dictPath).Where(x => x.EndsWith(".dic")).Select(x => x[(x.Replace('\\', '/').LastIndexOf('/') + 1)..x.LastIndexOf(".dic")])],
        };
        SpellCheckerConfig = conf;

        var field = typeof(SpellChecker).GetField("dictionaryManager", BindingFlags.NonPublic | BindingFlags.Static);
        field?.SetValue(null, null);
        _ = new SpellChecker(conf);

        if (isFirstInit)
        {
            new TextBoxSpellChecker(conf).Initialize(EditableText);
            new TextBoxSpellChecker(conf).Initialize(OriginalText);
            new TextBoxSpellChecker(conf).Initialize(CommentText);
        }
        else
        {
            foreach (var textBox in new[] { EditableText, OriginalText, CommentText })
            {
                textBox.GetVisualDescendants()
                    .OfType<SpellCheckerTextPresenter>()
                    .FirstOrDefault()?
                    .ForceInvalidateTextLayout();
            }
        }
    }

    async void SaveFile_OnClick(object? sender, RoutedEventArgs e)
    {
        var styleSave = StyleData?.Script?.OnFileSave;
        if (styleSave is not null)
        {
            try
            {
                await RunCustomTool(styleSave);
                IsFileModified = false;
                Title = "Dialogue Helper";
            }
            catch (Exception ex)
            {
                var se = new StyleError
                {
                    Title = "Save error",
                    MessageBlock = { Text = "DialogueHelper has encountered an error while saving a file" },
                    ErrorBox = { Text = ex.ToString() }
                };
                await se.ShowDialog(this);
            }
            return;
        }

        if (LastSavePath is null || LastSavePath.Length == 0)
        {
            SaveFileAs_OnClick(sender, e);
            return;
        }

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
            await using var stream = File.Create(LastSavePath);
            stream.Write(Encoding.UTF8.GetBytes(FileData!.OutputString()));
            IsFileModified = false;
            Title = "Dialogue Helper";
        }
        catch (Exception ex)
        {
            var se = new StyleError
            {
                Title = "Save error",
                MessageBlock = { Text = "DialogueHelper has encountered an error while saving a file" },
                ErrorBox = { Text = ex.ToString() },
            };
            await se.ShowDialog(this);
        }
        lWin.Close();
    }

    async void SaveFileAs_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureFileOpen())
            return;
        var file = await StorageProvider.SaveFilePickerAsync(new()
        {
            Title = "Save Dialogue Helper file...",
            FileTypeChoices = [new("DH File") { Patterns = ["*.dh"] }],
        });

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
        LastSavePath = file.Path.LocalPath;
        try
        {
            await using var stream = await file.OpenWriteAsync();
            stream.Write(Encoding.UTF8.GetBytes(FileData!.OutputString()));
            IsFileModified = false;
            Title = "Dialogue Helper";
        }
        catch (Exception ex)
        {
            var se = new StyleError
            {
                Title = "Save error",
                MessageBlock = { Text = "DialogueHelper has encountered an error while saving a file" },
                ErrorBox = { Text = ex.ToString() },
            };
            await se.ShowDialog(this);
        }
        lWin.Close();
    }

    async void BoxComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (BoxComboBox is not null)
            SelectedString?.BoxStyle = BoxComboBox.SelectedIndex;
        await UpdateEditableText(UpdateKind.SelectedBoxChanged);
        await UpdateOriginalText(UpdateKind.SelectedBoxChanged);
    }

    async void FontComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FontComboBox is not null)
            SelectedString?.FontStyle = FontComboBox.SelectedIndex;
        await UpdateEditableText(UpdateKind.SelectedFontChanged);
        await UpdateOriginalText(UpdateKind.SelectedFontChanged);
    }

    async void EntryTextBox_OnTextChanged(object? sender, TextChangedEventArgs? e) => await UpdateSearchFiltering();

    enum SearchExactness
    {
        Contains,
        StartsWith,
        EndsWith,
        Exact,
    }

    async Task UpdateSearchFiltering(Dictionary<TreeViewItem, Dictionary<StringContainer, TreeViewItem>>? delta = null)
    {
        if (!await EnsureFileOpen(false))
            return;
        var matches = new List<string>();
        {
            var sb = new StringBuilder();
            var enclosedQuote = false;
            var t = EntryTextBox.Text ?? "";
            for (var i = 0; i < t.Length; i++)
            {
                var @char = t[i];
                switch (@char)
                {
                    case '\\':
                        if (i + 1 < t.Length && t[i + 1] is '"' or '\\')
                            sb.Append(t[++i]);
                        else
                            sb.Append(@char);
                        break;
                    case '"':
                        enclosedQuote = !enclosedQuote;
                        break;
                    case ' ':
                        if (!enclosedQuote)
                        {
                            if (sb.Length > 0)
                            {
                                matches.Add(sb.ToString());
                                sb.Clear();
                            }
                        }
                        else
                            sb.Append(@char);
                        break;
                    default:
                        if (!enclosedQuote && char.IsWhiteSpace(@char))
                        {
                            matches.Add(sb.ToString());
                            sb.Clear();
                        }
                        else
                            sb.Append(@char);
                        break;
                }

                if (i == t.Length - 1 && sb.Length > 0)
                    matches.Add(sb.ToString());
            }
        }
        var targetCasing = matches
                .Where(m => m.StartsWith("Casing:", StringComparison.InvariantCultureIgnoreCase))
                .Select(m => m[7..])
                .Where(m => m.Length > 0)
                .Any(casing => casing.Equals("sensible", StringComparison.InvariantCultureIgnoreCase))
            ? StringComparison.InvariantCulture
            : StringComparison.InvariantCultureIgnoreCase;

        var searchExact = matches
                .Where(m => m.StartsWith("Exact:", StringComparison.InvariantCultureIgnoreCase))
                .Select(m => m[6..])
                .Where(m => m.Length > 0)
                .Select(casing =>
                {
                    if (casing.Equals("StartsWith", StringComparison.InvariantCultureIgnoreCase))
                        return SearchExactness.StartsWith;
                    if (casing.Equals("EndsWith", StringComparison.InvariantCultureIgnoreCase))
                        return SearchExactness.EndsWith;
                    if (casing.Equals("Exact", StringComparison.InvariantCultureIgnoreCase))
                        return SearchExactness.Exact;
                    return SearchExactness.Contains;
                })
                .FirstOrDefault(x => true, SearchExactness.Contains);

        var searchGroup = matches
                .Where(m => m.StartsWith("Group:", StringComparison.InvariantCultureIgnoreCase))
                .Select(m => m[6..])
                .ToList();
        var searchOgText = matches
                .Where(m => m.StartsWith("OgText:", StringComparison.InvariantCultureIgnoreCase))
                .Select(m => m[7..])
                .ToList();
        var headerText = matches
                .Where(m => m.StartsWith("Header:", StringComparison.InvariantCultureIgnoreCase))
                .Select(m => m[7..])
                .ToList();
        var stateText = matches
                .Where(m => m.StartsWith("State:", StringComparison.InvariantCultureIgnoreCase))
                .Select(m => m[6..])
                .ToList();
        var idText = matches
                .Where(m => m.StartsWith("Id:", StringComparison.InvariantCultureIgnoreCase))
                .Select(m => m[3..])
                .Select(m =>
                    m.Split(',')
                        .Select(x => int.TryParse(x.Trim(), out var n) ? n : (int?)null)
                        .Where(x => x is not null)
                        .Cast<int>()
                        .ToArray()
                )
                .ToList();
        var keyText = matches
                .Where(m => m.StartsWith("Key:", StringComparison.InvariantCultureIgnoreCase))
                .Select(m => m[4..])
                .ToList();

        var commentText = matches
            .Where(m => m.StartsWith("Comment:", StringComparison.InvariantCultureIgnoreCase))
            .Select(m => m[8..])
            .ToList();


        var searchText = matches
                .Where(m =>
                    !m.StartsWith("Casing:", StringComparison.InvariantCultureIgnoreCase)
                    && !m.StartsWith("Group:", StringComparison.InvariantCultureIgnoreCase)
                    && !m.StartsWith("OgText:", StringComparison.InvariantCultureIgnoreCase)
                    && !m.StartsWith("Header:", StringComparison.InvariantCultureIgnoreCase)
                    && !m.StartsWith("State:", StringComparison.InvariantCultureIgnoreCase)
                    && !m.StartsWith("Id:", StringComparison.InvariantCultureIgnoreCase)
                    && !m.StartsWith("Key:", StringComparison.InvariantCultureIgnoreCase)
                    && !m.StartsWith("Exact:", StringComparison.InvariantCultureIgnoreCase)
                    && !m.StartsWith("Comment:", StringComparison.InvariantCultureIgnoreCase)
                ).ToList();

        foreach (var value in delta ?? EtvTargetChildren)
        {
            var key = EtvKeys[value.Key];
            if (!FileData!.TryGetStringValue(key, out var list))
                continue;
            var foundMatch = false;
            if (headerText.All(header => key.Contains(header, targetCasing)) && !headerText.Any(header => header.Length == 0 && key.Length > 0))
            {
                if (value.Value.Count <= 0)
                    foundMatch = true;
                foreach (var entry in value.Value)
                {
                    var item = EtvIndexes[entry.Value];
                    if (idText.Any(ids => !ids.Contains(item.Id)))
                        goto NotFound;
                    if (keyText.Any(key => !(item.Key ?? "").Contains(key, targetCasing)) || keyText.Any(key => key.Length == 0 && (item.Key?.Length) > 0))
                        goto NotFound;
                    if (searchGroup.Any(group => !item.AuthorGroups.Any(ag => ag.Contains(group, targetCasing))) || searchGroup.Any(group => item.AuthorGroups.Any(ag => ag.Length > 0 && group.Length == 0)))
                        goto NotFound;
                    if (searchOgText.Any(ogText => searchExact switch
                    {
                        SearchExactness.StartsWith => !item.OriginalText.StartsWith(ogText, targetCasing),
                        SearchExactness.EndsWith => !item.OriginalText.EndsWith(ogText, targetCasing),
                        SearchExactness.Exact => !item.OriginalText.Equals(ogText, targetCasing),
                        _ => !item.OriginalText.Contains(ogText, targetCasing),
                    }) || searchOgText.Any(ogText => ogText.Length == 0 && item.OriginalText.Length > 0))
                        goto NotFound;
                    if (commentText.Any(comment => searchExact switch
                    {
                        SearchExactness.StartsWith => !(item.Comment ?? "").StartsWith(comment, targetCasing),
                        SearchExactness.EndsWith => !(item.Comment ?? "").EndsWith(comment, targetCasing),
                        SearchExactness.Exact => !(item.Comment ?? "").Equals(comment, targetCasing),
                        _ => !(item.Comment ?? "").Contains(comment, targetCasing),
                    }) || commentText.Any(comment => comment.Length == 0 && (item.Comment?.Length ?? 0) > 0))
                        goto NotFound;
                    if (searchText.Any(text =>
                    {
                        var textBase = item.Text ?? item.OriginalText;
                        return searchExact switch
                        {
                            SearchExactness.StartsWith => !textBase.StartsWith(text, targetCasing),
                            SearchExactness.EndsWith => !textBase.EndsWith(text, targetCasing),
                            SearchExactness.Exact => !textBase.Equals(text, targetCasing),
                            _ => !textBase.Contains(text, targetCasing),
                        };
                    }) || searchText.Any(text => text.Length == 0 && (item.Text ?? item.OriginalText).Length > 0))
                        goto NotFound;
                    foreach (var state in stateText)
                    {
                        switch (state.ToLowerInvariant())
                        {
                            case "modified":
                                if ((item.Text is null || item.Text == item.OriginalText) && !item.MarkAsModified)
                                    goto NotFound;
                                break;
                            case "base" or "original":
                                if ((item.Text is not null && item.Text != item.OriginalText) || item.MarkAsModified)
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

    void ModifySimilarStrings_OnClick(object? sender, RoutedEventArgs e) => UpdateModifySimilarStrings(null, null);

    void UpdateModifySimilarStrings(StringContainer? sc, TreeViewItem? si, bool updateText = true, bool updateOgText = false)
    {
        sc ??= SelectedString;
        if (sc is null || FileData is null)
            return;
        {
            if ((si ?? EntryTreeView.SelectedItem) is TreeViewItem tvi && EtvParents.TryGetValue(tvi, out var target))
            {
                lock (EtvTargetUpdates)
                {
                    if (!EtvTargetUpdates.Any(x => x.TryGetTarget(out var t) && t == target))
                        EtvTargetUpdates.Add(new(target));
                }
            }
        }
        if (ModifySimilarStrings.IsChecked ?? false)
        {
            foreach (var s in sc.EqStrings)
            {
                if (s.Id == sc.Id)
                    continue;
                if (updateText)
                {
                    s.Text = sc.Text;
                    s.LastEdited.AuthorId = sc.LastEdited.AuthorId;
                    s.LastEdited.Timestamp = sc.LastEdited.Timestamp;
                    s.MarkAsModified = sc.MarkAsModified;
                }
                if (updateOgText)
                    s.OriginalText = sc.OriginalText;

                if (EtvReverseIndexes.TryGetValue(s, out var ri) && EtvParents.TryGetValue(ri, out var target))
                {
                    lock (EtvTargetUpdates)
                    {
                        if (!EtvTargetUpdates.Any(x => x.TryGetTarget(out var t) && t == target))
                            EtvTargetUpdates.Add(new(target));
                    }
                }
            }
            if (SelectedString == sc)
            {
                SimilarStringsTitle.Text = $"Similar strings ({sc.EqStrings.Count})";
                foreach (var (i, sstr) in sc.EqStrings.Index())
                    SimilarStringsPanel.Items[i] = $"({sstr.Id}) {((sstr.Text is not null && sstr.Text != sstr.OriginalText) ? sstr.Text : sstr.OriginalText)}";
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

        lock (EqStringUpdates)
            EqStringUpdates.Clear();
        lock (EtvTargetUpdates)
            EtvTargetUpdates.Clear();
        SelectedString = new();
        FileData = null;
        EntryTreeView.Items.Clear();
        EtvKeys.Clear();
        EtvTargetChildren.Clear();
        EtvIndexes.Clear();
        EtvReverseIndexes.Clear();
        LastSavePath = null;
        IsFileModified = false;
        Title = "Dialogue Helper";
    }

    async void CreateFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (IsFileModified && await new UnsavedChanges().ShowDialog<bool>(this))
            return;

        lock (EqStringUpdates)
            EqStringUpdates.Clear();
        lock (EtvTargetUpdates)
            EtvTargetUpdates.Clear();
        FileData = new();
        SelectedString = new();
        EntryTreeView.Items.Clear();
        EtvKeys.Clear();
        EtvTargetChildren.Clear();
        EtvIndexes.Clear();
        EtvReverseIndexes.Clear();
        LastSavePath = null;
        IsFileModified = false;
        Title = "Dialogue Helper";
    }

    async void GoTo_OnClick(object? sender, RoutedEventArgs e) => await new GoTo(this).ShowDialog(this);

    async void CopyClipboardSelectedTreeItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (EntryTreeView.SelectedItem is null)
        {
            await new InfoWindow("You need to select an item before trying to copy it to the clipboard.").ShowDialog(this);
            return;
        }
        await Clipboard!.SetTextAsync(((TreeViewItem)EntryTreeView.SelectedItem).Header!.ToString() ?? "<null>");
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
        var ce = new CreateEntry { TextBox = { AcceptsReturn = false } };
        if (await ce.ShowDialog<bool?>(this) ?? false)
        {
            var t = ce.TextBox.Text ?? "";
            if (!FileData!.ContainsKeyString(t))
            {
                var tvi = new TreeViewItem { Header = t };
                var idx = EntryTreeView.Items.IndexOf(EntryTreeView.SelectedItem);
                if (idx >= 0 && (CreateAfterSelectedItem.IsChecked ?? false))
                    idx++;
                if (idx < 0 || idx >= EntryTreeView.Items.Count)
                {
                    EntryTreeView.Items.Add(tvi);
                    FileData.AddString(t, []);
                }
                else
                {
                    EntryTreeView.Items.Insert(idx, tvi);
                    FileData.InsertString(idx, t, []);
                }
                EtvTargetChildren.Add(tvi, []);
                EtvKeys.Add(tvi, t);
                EtvReverseKeys.Add(t, tvi);
            }

            var item = EtvReverseKeys[t];
            EntryTreeView.ExpandSubTree(item);
            EntryTreeView.SelectedItem = item;
            EntryTreeView.ScrollIntoView(item);
            item.Focus();
            await UpdateEtvItem(item);
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
        var s = (TreeViewItem)EntryTreeView.SelectedItem;
        if (!EtvParents.TryGetValue(s, out var parent))
            parent = s;
        if (!EtvKeys.TryGetValue(parent, out var key))
            return;
        var ce = new CreateEntry
        {
            Title = "Create String",
            EntryText = { Text = "Set the string content" },
            AcceptEmpty = true,
        };
        if (await ce.ShowDialog<bool?>(this) ?? false)
        {
            var t = ce.TextBox.Text ?? "";
            var tvi = new TreeViewItem { Header = t };
            EtvParents.Add(tvi, parent);
            var sc = new StringContainer
            {
                Id = FileData!.LastStringId++,
                OriginalText = t,
                EqStrings = FileData.IterAllStrings().FirstOrDefault(v => v.OriginalText == t)?.EqStrings ?? [],
            };
            var strArr = FileData.GetString(key)!;
            EtvTargetChildren[parent].Add(sc, tvi);
            EtvIndexes.Add(tvi, sc);
            EtvReverseIndexes.Add(sc, tvi);
            sc.EqStrings.Add(sc);
            if (!FileData.AuthorList.Contains(AuthorName))
                FileData.AuthorList.Add(AuthorName);
            sc.LastEdited.AuthorId = FileData.AuthorList.IndexOf(AuthorName);
            sc.LastEdited.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var idx = !EtvParents.ContainsKey(s) ? -1 : parent.Items.IndexOf(s);
            if (idx >= 0 && (CreateAfterSelectedItem.IsChecked ?? false))
                idx++;
            if (idx < 0 || idx >= parent.Items.Count)
            {
                parent.Items.Add(tvi);
                strArr.Add(sc);
            }
            else
            {
                parent.Items.Insert(idx, tvi);
                strArr.Insert(idx, sc);
            }

            EntryTreeView.ExpandSubTree(parent);
            EntryTreeView.SelectedItem = tvi;
            EntryTreeView.ScrollIntoView(tvi);
            tvi.Focus();
            await UpdateEtvItem(parent);
            EntryTreeView_OnSelectionChanged(null, null);

            IsFileModified = true;
            Title = "Dialogue Helper*";
        }
    }

    async void MarkAsFinished_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureFileOpen())
            return;
        if (EntryTreeView.SelectedItem is null || EntryTreeView.SelectedItem is not TreeViewItem tvi || (!EtvKeys.ContainsKey(tvi) && !EtvIndexes.ContainsKey(tvi)))
        {
            await new InfoWindow("You need to select an item before trying to mark it as finished.").ShowDialog(this);
            return;
        }
        if (EtvIndexes.TryGetValue(tvi, out var scS))
        {
            scS.MarkAsModified = true;
            await UpdateEtvItem(EtvParents[tvi]);
        }
        else
        {
            foreach (var sc in FileData!.GetString(EtvKeys[tvi])!)
                sc.MarkAsModified = true;
            await UpdateEtvItem(tvi);
        }
        IsFileModified = true;
        Title = "Dialogue Helper*";
    }

    async void MarkAsUnfinished_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureFileOpen())
            return;
        if (EntryTreeView.SelectedItem is null || EntryTreeView.SelectedItem is not TreeViewItem tvi || (!EtvKeys.ContainsKey(tvi) && !EtvIndexes.ContainsKey(tvi)))
        {
            await new InfoWindow("You need to select an item before trying to mark it as unfinished.").ShowDialog(this);
            return;
        }
        if (EtvIndexes.TryGetValue(tvi, out var scS))
        {
            scS.MarkAsModified = false;
            await UpdateEtvItem(EtvParents[tvi]);
        }
        else
        {
            foreach (var sc in FileData!.GetString(EtvKeys[tvi])!)
                sc.MarkAsModified = false;
            await UpdateEtvItem(tvi);
        }
        IsFileModified = true;
        Title = "Dialogue Helper*";
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
        var q = new QuestionWindow { Text = { Text = "Are you sure that you want to delete this item?\nThis action is irreversible." } };
        if (await q.ShowDialog<bool?>(this) ?? false)
        {
            var item = (TreeViewItem)EntryTreeView.SelectedItem;
            if (EtvKeys.TryGetValue(item, out var key))
            {
                EntryTreeView.Items.Remove(item);
                foreach (var entry in FileData!.GetString(key)!)
                    entry.EqStrings.Remove(entry);
                FileData.RemoveString(key);
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
                    var str = FileData!.GetString(key)!;
                    index.EqStrings.Remove(index);
                    str.Remove(index);
                    parent.Items.Remove(item);
                    SelectedString = new();
                    var idx = EtvIndexes[item];
                    EtvTargetChildren[parent].Remove(idx);
                    EtvIndexes.Remove(item);
                    EtvReverseIndexes.Remove(idx);
                    EtvParents.Remove(item);
                    await UpdateEtvItem(parent);
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
        await MarkItemAsModified(SelectedString, (TreeViewItem)EntryTreeView.SelectedItem, MarkAsModified.IsChecked ?? false, true);
    }

    public async Task MarkItemAsModified(StringContainer sc, TreeViewItem si, bool markAsModified, bool modifySimilar)
    {
        if (EtvKeys.ContainsKey(si))
            return;
        sc.MarkAsModified = markAsModified;
        IsFileModified = true;
        Title = "Dialogue Helper*";
        UpdateModifySimilarStrings(sc, si, modifySimilar);
        await UpdateEtvItem(EtvParents[si]);
    }

    void AllowEditingOriginal_OnClick(object? sender, RoutedEventArgs e) => OriginalText.IsReadOnly = !AllowEditingOriginal.IsChecked!.Value;

    async void ShowEntryInfo_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureFileOpen())
            return;
        if (SelectedString is null || FileData is null)
            return;
        var ts = DateTime.UnixEpoch.AddSeconds(SelectedString.LastEdited.Timestamp).ToLocalTime();
        var ei = new EntryInfo
        {
            AuthorText = { Text = FileData.AuthorList.ElementAtOrDefault(SelectedString.LastEdited.AuthorId) ?? "", },
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
        var modi = FileData!.IterAllStrings().Count(i =>
            i.MarkAsModified || (i.Text is not null && i.OriginalText != i.Text)
        );
        var total = FileData.GetAllCountString();
        var fi = new FileInfo
        {
            EditInfo = { Text = $"Edited {modi} strings out of {total} ({modi / (double)total:P} completion)" },
            Authors = { Text = $"Authors: {string.Join(", ", new HashSet<string>(FileData.AuthorList).Order())}" },
        };
        await fi.ShowDialog(this);
    }

    async void SimilarStringsPanel_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SelectedString is null || SimilarStringsPanel.SelectedItem is null || FileData is null || SimilarStringsPanel.SelectedIndex < 0 || SimilarStringsPanel.SelectedIndex >= SelectedString.EqStrings.Count)
            return;
        var eqStr = SelectedString.EqStrings[SimilarStringsPanel.SelectedIndex];
        var key = EtvTargetChildren.FirstOrDefault(s => s.Value.ContainsKey(eqStr)).Key;
        if (key is not null)
        {
            var child = EtvTargetChildren[key][eqStr];
            // todo: this doesn't work properly due to an avalonia bug
            EntryTreeView.ExpandSubTree(key);
            EntryTreeView.ScrollIntoView(child);
            child.Focus();
            EntryTreeView.SelectedItem = child;
            EntryTreeView_OnSelectionChanged(null, null);
        }
    }

    void GoToParentEntry_OnClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedString is null || FileData is null)
            return;
        var key = EtvTargetChildren.FirstOrDefault(s => s.Value.ContainsKey(SelectedString)).Key;
        if (key is null)
            return;
        EntryTreeView.SelectedItem = key;
        EntryTreeView.ScrollIntoView(key);
        key.Focus();
    }

    ///<returns>returns true if file is open</returns>
    public async Task<bool> EnsureFileOpen(bool showMessage = true, bool showMessageOpen = false)
    {
        if (FileData is null)
        {
            if (showMessage)
                await new InfoWindow("You need to have a file open to use this functionality.").ShowDialog(this);
            return false;
        }
        else if (showMessageOpen)
            await new InfoWindow("You need to have your open file closed to use this functionality.").ShowDialog(this);
        return true;
    }

    async void InitialTextColor_OnColorChanged(object? sender, ColorChangedEventArgs e)
    {
        await UpdateEditableText(UpdateKind.ColorChanged);
        await UpdateOriginalText(UpdateKind.ColorChanged);
    }

    async void GroupEditorSelect_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureFileOpen())
            return;
        await new GroupEditor(this).ShowDialog(this);
    }

    void StringKey_OnTextChanging(object? sender, TextChangingEventArgs? e)
    {
        if (SelectedString is null || FileData is null || UpdatingEntry || e is null)
            return;
        SelectedString.Key = StringKey.Text;

        if (!FileData.AuthorList.Contains(AuthorName))
            FileData.AuthorList.Add(AuthorName);
        SelectedString.LastEdited.AuthorId = FileData.AuthorList.IndexOf(AuthorName);
        SelectedString.LastEdited.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    async void SplitFileIntoParts_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureFileOpen())
            return;
        var qw = new QuestionNumberInputWindow("Please select your target options.\n", "Target Split Count") { NumberSel = { Maximum = 1024, Value = 1, Minimum = 1, Increment = 1, FormatString = "0" } };
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock { Margin = new(5, 7, 10, 5), Text = "Filename Template" });
        var tif = new TextBox { Text = "Split_%%" };
        ToolTip.SetTip(tif, new TextBlock { Text = "% is the character to replace for a sequential number, the more % in a row the more 0's it'll have for padding. Note that, the limit is 4 and it'll only target the last set of %'s." });
        sp.Children.Add(tif);
        qw.Panel.Children.Add(sp);
        var confirmed = await qw.ShowDialog<bool>(this);
        if (!confirmed)
            return;
        var split = (int)(qw.NumberSel.Value ?? 0);
        if (split == 0)
            return;
        var folders = await StorageProvider.OpenFolderPickerAsync(new());
        if (folders is null || folders.Count == 0)
            return;
        var folder = folders[0].Path.LocalPath;
        var max = FileData!.GetAllCountString();
        var splitProg = max / split;
        var pg = new LoadingWindow { Text = { Text = $"Splitting texts... (0/{max})" }, ProgressBar = { IsIndeterminate = false, Value = 0, Maximum = max } };
        pg.Show();
        var arr = new List<Dictionary<string, List<StringContainer>>>();
        var cur = 0;
        try
        {
            var template = tif.Text;
            if (!template.Contains('%'))
                template += '%';
            if (!template.EndsWith(".dh"))
                template += ".dh";
            var rendp = template.LastIndexOf('%'); // will never be -1
            var rstartp = rendp;
            while (rstartp > 0 && template[rstartp - 1] == '%')
                rstartp--;
            var padLen = rendp + 1 - rstartp;
            template = template.Remove(rstartp, padLen);
            foreach (var (k, v) in FileData.GetStringKeysValues())
            {
                foreach (var sc in v)
                {
                    if ((cur % splitProg) == 0 && arr.Count < split)
                        arr.Add([]);

                    var a = arr[^1];
                    a.TryAdd(k, []);
                    a[k].Add(sc);

                    cur++;
                    pg.Text.Text = $"Splitting texts... ({cur}/{max})";
                    pg.ProgressBar.Value = cur / (float)max;
                }
            }

            pg.ProgressBar.Value = 0;
            max += arr.Count;
            cur = 0;
            pg.ProgressBar.Maximum = max;
            pg.Text.Text = $"Saving files... (0/{max})";
            foreach (var (i, scl) in arr.Index())
            {
                var f = new FileData();

                foreach (var (k, scar) in scl)
                {
                    var sca = new List<StringContainer>();
                    f.AddString(k, sca);
                    foreach (var sc in scar)
                    {
                        var scd = new StringContainer(sc.OutputFormatEntry())
                        {
                            EqStrings = [], // not needed
                            Id = f.LastStringId++,
                        };
                        if (scd.LastEdited.AuthorId != -1)
                        {
                            var ta = FileData.AuthorList[scd.LastEdited.AuthorId];
                            if (!f.AuthorList.Contains(ta))
                                f.AuthorList.Add(ta);
                            scd.LastEdited.AuthorId = f.AuthorList.IndexOf(ta);
                        }
                        sca.Add(scd);
                    }
                }

                await File.WriteAllTextAsync(Path.Combine(folder, template.Insert(rstartp, (i + 1).ToString($"D{padLen}"))), f.OutputString());

                cur++;
                pg.Text.Text = $"Saving files... ({cur}/{max})";
                pg.ProgressBar.Value = cur / (float)max;
            }
        }
        finally
        {
            pg.Close();
        }
    }

    async void MergePartsIntoFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (await EnsureFileOpen(false, true))
            return;
        var fs = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose the Dialogue Helper files...",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new("DH File") { Patterns = ["*.dh"] },
                new("Legacy DH File") { Patterns = ["*.txt"] },
            ],
        });
        if (fs is null)
            return;
        var iw = new LoadingWindow { Text = { Text = "Loading all dh files..." }, ProgressBar = { Maximum = 1d, } };
        iw.Closing += (_, ev) =>
        {
            if (!ev.IsProgrammatic)
                ev.Cancel = true;
        };
        iw.ProgressBar.Value = 0d;
        iw.Show(this);
        var loaded = new List<FileData>();
        foreach (var f in fs)
        {
            try
            {
                var rw = new StreamReader(await f.OpenReadAsync());
                var str = await rw.ReadToEndAsync();
                rw.Dispose();
                loaded.Add(FileData.LoadString(FileParser.ParseString(str)));
            }
            catch (Exception ex)
            {
                var se = new StyleError
                {
                    Title = "Load error",
                    MessageBlock = { Text = "DialogueHelper has encountered an error while loading a file" },
                    ErrorBox = { Text = ex.ToString() },
                };
                await se.ShowDialog(this);
            }
        }
        var final = new FileData();
        foreach (var l in loaded)
        {
            foreach (var a in l.AuthorList)
            {
                if (!final.AuthorList.Contains(a))
                    final.AuthorList.Add(a);
            }
            foreach (var (k, s) in l.GetStringKeysValues())
            {
                if (!final.ContainsKeyString(k))
                    final.AddString(k, []);
                var arr = final.GetString(k)!;
                foreach (var n in s)
                {
                    n.Id = final.LastStringId++;
                    if (n.LastEdited.AuthorId != -1)
                    {
                        var t = l.AuthorList[n.LastEdited.AuthorId];
                        n.LastEdited.AuthorId = final.AuthorList.IndexOf(t);
                    }
                    arr.Add(n);
                }
            }
        }
        iw.Close();
        await LoadStringToProject(final.OutputString());
        await new InfoWindow("Merged all files successfully!").ShowDialog(this);
    }

    async void MoveUp_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureFileOpen())
            return;
        if (EntryTreeView.SelectedItem is null || EntryTreeView.SelectedItem is not TreeViewItem tvi || (!EtvKeys.ContainsKey(tvi) && !EtvIndexes.ContainsKey(tvi)))
        {
            await new InfoWindow("You need to select a block before trying to move it up.").ShowDialog(this);
            return;
        }
        if (EtvParents.TryGetValue(tvi, out var parent))
        {
            var sc = EtvIndexes[tvi];
            var k = EtvKeys[parent];
            var idx = FileData!.GetIndexString(k);
            var entry = FileData.GetAtString(idx)!;
            var scIdx = entry.IndexOf(sc);
            var moveToAnotherBlock = false;
            if (scIdx == 0 && idx > 0)
            {
                var q = new QuestionWindow();
                q.Text.Inlines!.Add("Do you wish to move this string to the ");
                q.Text.Inlines.Add(new Run(FileData.GetAtStringKey(idx - 1)) { FontWeight = FontWeight.Bold });
                q.Text.Inlines!.Add(" block?");
                if (await q.ShowDialog<bool>(this))
                    moveToAnotherBlock = true;
            }
            if (moveToAnotherBlock)
            {
                parent.Items.Remove(tvi);
                entry.RemoveAt(scIdx);
                EtvTargetChildren[parent].Remove(sc);
                (k, var nb) = FileData.GetAtStringKeyValue(idx - 1)!.Value;
                parent = EtvReverseKeys[k];
                EtvParents[tvi] = parent;
                EtvTargetChildren[parent].Add(sc, tvi);
                nb.Add(sc);
                parent.Items.Add(tvi);
            }
            else
            {
                entry.RemoveAt(scIdx);
                entry.Insert(int.Max(scIdx - 1, 0), sc);
                idx = parent.Items.IndexOf(tvi);
                parent.Items.RemoveAt(idx);
                parent.Items.Insert(int.Max(idx - 1, 0), tvi);
            }
        }
        else
        {
            var k = EtvKeys[tvi];
            var idx = FileData!.GetIndexString(k);
            var entry = FileData.GetAtString(idx)!;
            FileData.RemoveAtString(idx);
            FileData.InsertString(int.Max(idx - 1, 0), k, entry);
            idx = EntryTreeView.Items.IndexOf(tvi);
            EntryTreeView.Items.RemoveAt(idx);
            EntryTreeView.Items.Insert(int.Max(idx - 1, 0), tvi);
        }
        IsFileModified = true;
        Title = "Dialogue Helper*";
    }

    async void MoveDown_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureFileOpen())
            return;
        if (EntryTreeView.SelectedItem is null || EntryTreeView.SelectedItem is not TreeViewItem tvi || (!EtvKeys.ContainsKey(tvi) && !EtvIndexes.ContainsKey(tvi)))
        {
            await new InfoWindow("You need to select a block before trying to move it down.").ShowDialog(this);
            return;
        }
        if (EtvParents.TryGetValue(tvi, out var parent))
        {
            var sc = EtvIndexes[tvi];
            var k = EtvKeys[parent];
            var idx = FileData!.GetIndexString(k);
            var entry = FileData.GetAtString(idx)!;
            var scIdx = entry.IndexOf(sc);
            var moveToAnotherBlock = false;
            if (scIdx == entry.Count - 1 && idx < FileData.GetCountString())
            {
                var q = new QuestionWindow();
                q.Text.Inlines!.Add("Do you wish to move this string to the ");
                q.Text.Inlines.Add(new Run(FileData.GetAtStringKey(idx + 1)) { FontWeight = FontWeight.Bold });
                q.Text.Inlines!.Add(" block?");
                if (await q.ShowDialog<bool>(this))
                    moveToAnotherBlock = true;
            }
            if (moveToAnotherBlock)
            {
                parent.Items.Remove(tvi);
                entry.RemoveAt(scIdx);
                EtvTargetChildren[parent].Remove(sc);
                (k, var nb) = FileData.GetAtStringKeyValue(idx + 1)!.Value;
                parent = EtvReverseKeys[k];
                EtvParents[tvi] = parent;
                EtvTargetChildren[parent].Add(sc, tvi);
                if (nb.Count > 0)
                {
                    nb.Insert(0, sc);
                    parent.Items.Insert(0, tvi);
                }
                else
                {
                    nb.Add(sc);
                    parent.Items.Add(tvi);
                }
            }
            else
            {
                entry.RemoveAt(scIdx);
                if (scIdx + 1 >= entry.Count)
                    entry.Add(sc);
                else
                    entry.Insert(scIdx + 1, sc);
                idx = parent.Items.IndexOf(tvi);
                parent.Items.RemoveAt(idx);
                if (idx + 1 >= parent.Items.Count)
                    parent.Items.Add(tvi);
                else
                    parent.Items.Insert(idx + 1, tvi);
            }
        }
        else
        {
            var k = EtvKeys[tvi];
            var idx = FileData!.GetIndexString(k);
            var entry = FileData.GetAtString(idx)!;
            FileData.RemoveAtString(idx);
            FileData.InsertString(idx + 1, k, entry);
            idx = EntryTreeView.Items.IndexOf(tvi);
            EntryTreeView.Items.RemoveAt(idx);
            if (idx + 1 >= EntryTreeView.Items.Count)
                EntryTreeView.Items.Add(tvi);
            else
                EntryTreeView.Items.Insert(idx + 1, tvi);
        }
        IsFileModified = true;
        Title = "Dialogue Helper*";
    }

    async void RenameBlock_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureFileOpen())
            return;
        if (EntryTreeView.SelectedItem is null || EntryTreeView.SelectedItem is not TreeViewItem tvi || !EtvKeys.TryGetValue(tvi, out var k))
        {
            await new InfoWindow("You need to select a block before trying to rename it.").ShowDialog(this);
            return;
        }
        var qw = new QuestionWindow("To which name do you wish to rename this block to?");
        var sp = new StackPanel { Margin = new(0, 10, 0, 0), Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock { Margin = new(5, 7, 10, 5), Text = "Name" });
        var nn = new TextBox { Text = k, MinWidth = 250 };
        sp.Children.Add(nn);
        qw.Panel.Children.Add(sp);
        qw.YesButton.Content = "Accept";
        qw.NoButton.Content = "Cancel";
        qw.ValidityCheck = (accept) =>
        {
            if (!accept)
                return true;
            return nn.Text.Length > 0 && (!EtvReverseKeys.ContainsKey(nn.Text) || nn.Text == k);
        };
        if (await qw.ShowDialog<bool>(this) && nn.Text != k)
        {
            EtvKeys[tvi] = nn.Text;
            EtvReverseKeys.Remove(k);
            EtvReverseKeys.Add(nn.Text, tvi);
            var idx = FileData!.GetIndexString(k);
            var scl = FileData.GetAtString(idx)!;
            FileData.RemoveAtString(idx);
            FileData.InsertString(idx, nn.Text, scl);
            tvi.Header = nn.Text;

            IsFileModified = true;
            Title = "Dialogue Helper*";
        }
    }

    void SelectSearch_OnClick(object? sender, RoutedEventArgs e)
    {
        EntryTextBox.Focus();
        EntryTextBox.SelectionStart = EntryTextBox.Text?.Length ?? 0;
        EntryTextBox.SelectionEnd = EntryTextBox.SelectionStart;
    }

    void RestrictTabIndicator_OnClick(object? sender, RoutedEventArgs e) => SaveSettings();
}
